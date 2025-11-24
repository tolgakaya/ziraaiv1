using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Business.Constants;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using IResult = Core.Utilities.Results.IResult;
using IDataResult = Core.Utilities.Results.IDataResult<Entities.Dtos.SubscriptionUsageStatusDto>;
using SuccessResult = Core.Utilities.Results.SuccessResult;
using ErrorResult = Core.Utilities.Results.ErrorResult;
using SuccessDataResult = Core.Utilities.Results.SuccessDataResult<Entities.Dtos.SubscriptionUsageStatusDto>;
using ErrorDataResult = Core.Utilities.Results.ErrorDataResult<Entities.Dtos.SubscriptionUsageStatusDto>;

namespace Business.Services.Subscription
{
    public class SubscriptionValidationService : ISubscriptionValidationService
    {
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionUsageLogRepository _usageLogRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SubscriptionValidationService> _logger;

        public SubscriptionValidationService(
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionUsageLogRepository usageLogRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SubscriptionValidationService> logger)
        {
            _userSubscriptionRepository = userSubscriptionRepository;
            _usageLogRepository = usageLogRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            
            _logger.LogInformation("SubscriptionValidationService initialized successfully");
        }

        public async Task<IDataResult> CheckSubscriptionStatusAsync(int userId)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            _logger.LogInformation("[SUBSCRIPTION_CHECK_START] Starting subscription validation - UserId: {UserId}, CorrelationId: {CorrelationId}", 
                userId, correlationId);

            try
            {
                var subscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
                
                if (subscription == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning("[SUBSCRIPTION_CHECK_NO_SUBSCRIPTION] No active subscription found - UserId: {UserId}, CorrelationId: {CorrelationId}, CheckTime: {CheckTime}ms", 
                        userId, correlationId, stopwatch.ElapsedMilliseconds);
                    
                    return new ErrorDataResult(
                        new SubscriptionUsageStatusDto
                        {
                            HasActiveSubscription = false,
                            SubscriptionStatus = "No Active Subscription",
                            CanMakeRequest = false,
                            LimitExceededMessage = "You need an active subscription to make analysis requests. Please subscribe to one of our plans."
                        },
                        "No active subscription found");
                }

                _logger.LogInformation("[SUBSCRIPTION_CHECK_FOUND] Active subscription found - UserId: {UserId}, CorrelationId: {CorrelationId}, SubscriptionId: {SubscriptionId}, TierName: {TierName}, Status: {Status}", 
                    userId, correlationId, subscription.Id, subscription.SubscriptionTier?.TierName ?? "Unknown", subscription.Status);

                // Check if subscription is expired
                if (subscription.EndDate <= DateTime.Now)
                {
                    _logger.LogWarning("[SUBSCRIPTION_CHECK_EXPIRED] Subscription expired - UserId: {UserId}, CorrelationId: {CorrelationId}, SubscriptionId: {SubscriptionId}, ExpiredDate: {ExpiredDate}", 
                        userId, correlationId, subscription.Id, subscription.EndDate);
                    
                    subscription.IsActive = false;
                    subscription.Status = "Expired";
                    _userSubscriptionRepository.Update(subscription);
                    await _userSubscriptionRepository.SaveChangesAsync();

                    stopwatch.Stop();
                    _logger.LogInformation("[SUBSCRIPTION_CHECK_EXPIRED_UPDATED] Subscription marked as expired - UserId: {UserId}, CorrelationId: {CorrelationId}, CheckTime: {CheckTime}ms", 
                        userId, correlationId, stopwatch.ElapsedMilliseconds);

                    return new ErrorDataResult(
                        new SubscriptionUsageStatusDto
                        {
                            HasActiveSubscription = false,
                            SubscriptionStatus = "Expired",
                            CanMakeRequest = false,
                            LimitExceededMessage = "Your subscription has expired. Please renew to continue using the service.",
                            SubscriptionEndDate = subscription.EndDate
                        },
                        "Subscription expired");
                }

                var hasResetDaily = false;
                var hasResetMonthly = false;

                // Reset daily usage if needed
                if (subscription.LastUsageResetDate?.Date < DateTime.Now.Date)
                {
                    var previousDailyUsage = subscription.CurrentDailyUsage;
                    subscription.CurrentDailyUsage = 0;
                    subscription.LastUsageResetDate = DateTime.Now;
                    hasResetDaily = true;
                    
                    _logger.LogInformation("[SUBSCRIPTION_CHECK_DAILY_RESET] Daily usage reset - UserId: {UserId}, CorrelationId: {CorrelationId}, PreviousUsage: {PreviousUsage}, ResetDate: {ResetDate}", 
                        userId, correlationId, previousDailyUsage, subscription.LastUsageResetDate);
                }

                // Reset monthly usage if needed
                var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                if (subscription.MonthlyUsageResetDate == null || subscription.MonthlyUsageResetDate < currentMonth)
                {
                    var previousMonthlyUsage = subscription.CurrentMonthlyUsage;
                    subscription.CurrentMonthlyUsage = 0;
                    subscription.MonthlyUsageResetDate = currentMonth;
                    hasResetMonthly = true;
                    
                    _logger.LogInformation("[SUBSCRIPTION_CHECK_MONTHLY_RESET] Monthly usage reset - UserId: {UserId}, CorrelationId: {CorrelationId}, PreviousUsage: {PreviousUsage}, ResetDate: {ResetDate}", 
                        userId, correlationId, previousMonthlyUsage, subscription.MonthlyUsageResetDate);
                }

                // Save resets if any occurred
                if (hasResetDaily || hasResetMonthly)
                {
                    try
                    {
                        _userSubscriptionRepository.Update(subscription);
                        await _userSubscriptionRepository.SaveChangesAsync();
                        
                        _logger.LogInformation("[SUBSCRIPTION_CHECK_USAGE_RESET_SAVED] Usage reset saved successfully - UserId: {UserId}, CorrelationId: {CorrelationId}, DailyReset: {DailyReset}, MonthlyReset: {MonthlyReset}", 
                            userId, correlationId, hasResetDaily, hasResetMonthly);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SUBSCRIPTION_CHECK_USAGE_RESET_ERROR] Failed to save usage reset - UserId: {UserId}, CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}", 
                            userId, correlationId, ex.GetType().Name);
                        // Continue without throwing - usage reset will happen on next check
                    }
                }

                var tier = subscription.SubscriptionTier;
                
                // ✅ REFERRAL CREDITS PRIORITY: Check referral credits first before subscription quota
                var referralCredits = subscription.ReferralCredits;
                var hasReferralCredits = referralCredits > 0;
                
                var dailyRemaining = tier.DailyRequestLimit - subscription.CurrentDailyUsage;
                var monthlyRemaining = tier.MonthlyRequestLimit - subscription.CurrentMonthlyUsage;
                
                // User can make request if they have referral credits OR subscription quota
                var canMakeRequest = hasReferralCredits || (dailyRemaining > 0 && monthlyRemaining > 0);
                string limitMessage = null;

                // Only show limit messages if user has no referral credits
                if (!hasReferralCredits)
                {
                    if (dailyRemaining <= 0)
                    {
                        limitMessage = $"Daily request limit reached ({tier.DailyRequestLimit} requests). Resets at midnight.";
                        _logger.LogWarning("[SUBSCRIPTION_CHECK_DAILY_LIMIT_EXCEEDED] Daily limit reached - UserId: {UserId}, CorrelationId: {CorrelationId}, DailyUsed: {DailyUsed}, DailyLimit: {DailyLimit}, ReferralCredits: {ReferralCredits}", 
                            userId, correlationId, subscription.CurrentDailyUsage, tier.DailyRequestLimit, referralCredits);
                    }
                    else if (monthlyRemaining <= 0)
                    {
                        limitMessage = $"Monthly request limit reached ({tier.MonthlyRequestLimit} requests). Resets on the 1st of next month.";
                        _logger.LogWarning("[SUBSCRIPTION_CHECK_MONTHLY_LIMIT_EXCEEDED] Monthly limit reached - UserId: {UserId}, CorrelationId: {CorrelationId}, MonthlyUsed: {MonthlyUsed}, MonthlyLimit: {MonthlyLimit}, ReferralCredits: {ReferralCredits}", 
                            userId, correlationId, subscription.CurrentMonthlyUsage, tier.MonthlyRequestLimit, referralCredits);
                    }
                }
                else
                {
                    _logger.LogInformation("[SUBSCRIPTION_CHECK_USING_REFERRAL_CREDITS] User has referral credits available - UserId: {UserId}, CorrelationId: {CorrelationId}, ReferralCredits: {ReferralCredits}", 
                        userId, correlationId, referralCredits);
                }

                _logger.LogInformation("[SUBSCRIPTION_CHECK_QUOTA_STATUS] Quota status calculated - UserId: {UserId}, CorrelationId: {CorrelationId}, CanMakeRequest: {CanMakeRequest}, DailyRemaining: {DailyRemaining}, MonthlyRemaining: {MonthlyRemaining}, ReferralCredits: {ReferralCredits}", 
                    userId, correlationId, canMakeRequest, dailyRemaining, monthlyRemaining, referralCredits);

                var status = new SubscriptionUsageStatusDto
                {
                    HasActiveSubscription = true,
                    SubscriptionStatus = subscription.Status,
                    TierName = tier.TierName,
                    DailyUsed = subscription.CurrentDailyUsage,
                    DailyLimit = tier.DailyRequestLimit,
                    DailyRemaining = dailyRemaining,
                    MonthlyUsed = subscription.CurrentMonthlyUsage,
                    MonthlyLimit = tier.MonthlyRequestLimit,
                    MonthlyRemaining = monthlyRemaining,
                    ReferralCredits = referralCredits, // ✅ Add referral credits to response
                    CanMakeRequest = canMakeRequest,
                    LimitExceededMessage = limitMessage,
                    NextDailyReset = DateTime.Now.Date.AddDays(1),
                    NextMonthlyReset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1),
                    SubscriptionEndDate = subscription.EndDate
                };

                // Only attempt database update if request can be made
                if (canMakeRequest)
                {
                    try
                    {
                        _logger.LogDebug("[SUBSCRIPTION_CHECK_UPDATE_START] Updating subscription counters - UserId: {UserId}, CorrelationId: {CorrelationId}", 
                            userId, correlationId);
                        
                        // CRITICAL FIX: Manually set all DateTime fields to prevent PostgreSQL timezone issues
                        var now = DateTime.Now;
                        subscription.UpdatedDate = now;
                        
                        // Ensure all DateTime fields use DateTime.Now (not UtcNow)
                        if (subscription.LastUsageResetDate?.Date < now.Date)
                        {
                            subscription.LastUsageResetDate = now;
                        }
                        
                        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
                        if (subscription.MonthlyUsageResetDate == null || subscription.MonthlyUsageResetDate < currentMonthStart)
                        {
                            subscription.MonthlyUsageResetDate = currentMonthStart;
                        }
                        
                        _userSubscriptionRepository.Update(subscription);
                        await _userSubscriptionRepository.SaveChangesAsync();
                        
                        _logger.LogInformation("[SUBSCRIPTION_CHECK_UPDATE_SUCCESS] Subscription updated successfully - UserId: {UserId}, CorrelationId: {CorrelationId}", 
                            userId, correlationId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SUBSCRIPTION_CHECK_UPDATE_ERROR] Failed to save subscription changes - UserId: {UserId}, CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}, Message: {ErrorMessage}", 
                            userId, correlationId, ex.GetType().Name, ex.Message);
                        
                        var innerEx = ex.InnerException;
                        var level = 1;
                        while (innerEx != null && level <= 3) // Limit to 3 levels
                        {
                            _logger.LogError("[SUBSCRIPTION_CHECK_INNER_EXCEPTION] Inner Exception Level {Level} - UserId: {UserId}, CorrelationId: {CorrelationId}, Type: {InnerExceptionType}, Message: {InnerMessage}", 
                                level, userId, correlationId, innerEx.GetType().Name, innerEx.Message);
                            innerEx = innerEx.InnerException;
                            level++;
                        }
                        
                        _logger.LogWarning("[SUBSCRIPTION_CHECK_CONTINUE_WITHOUT_UPDATE] Continuing without counter update to avoid blocking user - UserId: {UserId}, CorrelationId: {CorrelationId}", 
                            userId, correlationId);
                    }
                }

                stopwatch.Stop();
                _logger.LogInformation("[SUBSCRIPTION_CHECK_COMPLETED] Subscription check completed - UserId: {UserId}, CorrelationId: {CorrelationId}, TotalTime: {TotalTime}ms, CanMakeRequest: {CanMakeRequest}", 
                    userId, correlationId, stopwatch.ElapsedMilliseconds, canMakeRequest);

                return new SuccessDataResult(status);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[SUBSCRIPTION_CHECK_GENERAL_ERROR] Unexpected error during subscription check - UserId: {UserId}, CorrelationId: {CorrelationId}, ElapsedTime: {ElapsedTime}ms, ExceptionType: {ExceptionType}", 
                    userId, correlationId, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
                
                // Return error result with safe fallback
                return new ErrorDataResult(
                    new SubscriptionUsageStatusDto
                    {
                        HasActiveSubscription = false,
                        SubscriptionStatus = "Error",
                        CanMakeRequest = false,
                        LimitExceededMessage = "Unable to verify subscription status. Please try again."
                    }, 
                    $"Subscription check failed: {ex.Message}");
            }
        }

        public async Task<IResult> ValidateAndLogUsageAsync(int userId, string endpoint, string method)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            _logger.LogInformation("[USAGE_VALIDATION_START] Starting usage validation - UserId: {UserId}, CorrelationId: {CorrelationId}, Endpoint: {Endpoint}, Method: {Method}", 
                userId, correlationId, endpoint, method);

            try
            {
                // ✨ EVENT-DRIVEN QUEUE ACTIVATION: Process expired subscriptions and activate queued ones
                await ProcessExpiredSubscriptionsAsync();
                
                var statusResult = await CheckSubscriptionStatusAsync(userId);
                
                _logger.LogInformation("[USAGE_VALIDATION_STATUS_CHECK] Subscription status checked - UserId: {UserId}, CorrelationId: {CorrelationId}, Success: {Success}, CanMakeRequest: {CanMakeRequest}", 
                    userId, correlationId, statusResult.Success, statusResult.Data?.CanMakeRequest ?? false);
                
                if (!statusResult.Success || !statusResult.Data.CanMakeRequest)
                {
                    _logger.LogWarning("[USAGE_VALIDATION_FAILED] Usage validation failed - UserId: {UserId}, CorrelationId: {CorrelationId}, Reason: {Reason}", 
                        userId, correlationId, statusResult.Message);
                    
                    // Log failed attempt
                    await LogUsageAsync(userId, endpoint, method, false, statusResult.Message);
                    
                    stopwatch.Stop();
                    _logger.LogInformation("[USAGE_VALIDATION_FAILED_LOGGED] Failed usage logged - UserId: {UserId}, CorrelationId: {CorrelationId}, ValidationTime: {ValidationTime}ms", 
                        userId, correlationId, stopwatch.ElapsedMilliseconds);
                    
                    return new ErrorResult(statusResult.Data?.LimitExceededMessage ?? statusResult.Message);
                }

                stopwatch.Stop();
                _logger.LogInformation("[USAGE_VALIDATION_SUCCESS] Usage validation successful - UserId: {UserId}, CorrelationId: {CorrelationId}, ValidationTime: {ValidationTime}ms", 
                    userId, correlationId, stopwatch.ElapsedMilliseconds);
                
                return new SuccessResult();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[USAGE_VALIDATION_EXCEPTION] Exception during usage validation - UserId: {UserId}, CorrelationId: {CorrelationId}, ElapsedTime: {ElapsedTime}ms, ExceptionType: {ExceptionType}, Message: {ErrorMessage}", 
                    userId, correlationId, stopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        public async Task<bool> CanUserMakeRequestAsync(int userId)
        {
            var statusResult = await CheckSubscriptionStatusAsync(userId);
            return statusResult.Success && statusResult.Data.CanMakeRequest;
        }

        public async Task<IResult> IncrementUsageAsync(int userId, int? plantAnalysisId = null)
        {
            _logger.LogInformation("[INCREMENT_USAGE_START] Starting usage increment - UserId: {UserId}, PlantAnalysisId: {PlantAnalysisId}",
                userId, plantAnalysisId);

            var subscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

            if (subscription == null)
            {
                _logger.LogWarning("[INCREMENT_USAGE_NO_SUBSCRIPTION] No active subscription found - UserId: {UserId}",
                    userId);
                return new ErrorResult("No active subscription found");
            }

            _logger.LogInformation("[INCREMENT_USAGE_SUBSCRIPTION_FOUND] Subscription found - UserId: {UserId}, SubscriptionId: {SubscriptionId}, TierName: {TierName}, ReferralCredits: {ReferralCredits}",
                userId, subscription.Id, subscription.SubscriptionTier?.TierName, subscription.ReferralCredits);

            // ✅ PRIORITY: Use referral credits first, then subscription quota
            var referralCredits = subscription.ReferralCredits;
            
            if (referralCredits > 0)
            {
                // Decrement referral credits
                subscription.ReferralCredits -= 1;
                _userSubscriptionRepository.Update(subscription);
                await _userSubscriptionRepository.SaveChangesAsync();
                
                _logger.LogInformation("[INCREMENT_USAGE_REFERRAL_CREDIT_USED] Referral credit used - UserId: {UserId}, SubscriptionId: {SubscriptionId}, RemainingCredits: {RemainingCredits}",
                    userId, subscription.Id, subscription.ReferralCredits);
            }
            else
            {
                // Use subscription quota
                _logger.LogInformation("[INCREMENT_USAGE_COUNTERS] Updating subscription quota counters - UserId: {UserId}, SubscriptionId: {SubscriptionId}",
                    userId, subscription.Id);

                await _userSubscriptionRepository.UpdateUsageCountersAsync(subscription.Id, 1, 1);

            _logger.LogInformation("[INCREMENT_USAGE_COUNTERS_UPDATED] Subscription quota counters updated - UserId: {UserId}, SubscriptionId: {SubscriptionId}",
                    userId, subscription.Id);
            }

            // Log the usage
            var httpContext = _httpContextAccessor.HttpContext;
            await LogUsageAsync(
                userId,
                httpContext?.Request.Path.Value ?? "Unknown",
                httpContext?.Request.Method ?? "Unknown",
                true,
                "Request processed successfully",
                subscription.Id,
                plantAnalysisId
            );

            return new SuccessResult("Usage incremented successfully");
        }

        private async Task LogUsageAsync(int userId, string endpoint, string method, bool isSuccessful,
            string responseStatus, int? subscriptionId = null, int? plantAnalysisId = null)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..8];

            _logger.LogInformation("[USAGE_LOG_START] Starting usage logging - UserId: {UserId}, CorrelationId: {CorrelationId}, Endpoint: {Endpoint}, Method: {Method}, IsSuccessful: {IsSuccessful}, SubscriptionId: {SubscriptionId}, PlantAnalysisId: {PlantAnalysisId}",
                userId, correlationId, endpoint, method, isSuccessful, subscriptionId, plantAnalysisId);

            try
            {
                _logger.LogInformation("[USAGE_LOG_SUBSCRIPTION_LOOKUP] Looking up subscription - UserId: {UserId}, CorrelationId: {CorrelationId}, SubscriptionId: {SubscriptionId}",
                    userId, correlationId, subscriptionId);

                var httpContext = _httpContextAccessor.HttpContext;
                var subscription = subscriptionId.HasValue
                    ? await _userSubscriptionRepository.GetAsync(s => s.Id == subscriptionId.Value)
                    : await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

                if (subscription == null)
                {
                    _logger.LogWarning("[USAGE_LOG_NO_SUBSCRIPTION] No active subscription found, skipping usage log - UserId: {UserId}, CorrelationId: {CorrelationId}",
                        userId, correlationId);
                    return;
                }

                _logger.LogInformation("[USAGE_LOG_SUBSCRIPTION_FOUND] Subscription found - UserId: {UserId}, CorrelationId: {CorrelationId}, SubscriptionId: {SubscriptionId}, TierName: {TierName}",
                    userId, correlationId, subscription.Id, subscription.SubscriptionTier?.TierName);

                // Use DateTime.Now instead of DateTime.UtcNow to avoid timezone issues with PostgreSQL
                var now = DateTime.Now;

                _logger.LogInformation("[USAGE_LOG_CREATING] Creating usage log object - UserId: {UserId}, CorrelationId: {CorrelationId}, UserSubscriptionId: {UserSubscriptionId}",
                    userId, correlationId, subscription.Id);

                var usageLog = new SubscriptionUsageLog
                {
                    UserId = userId,
                    UserSubscriptionId = subscription.Id, // Now guaranteed to be valid
                    PlantAnalysisId = null, // Hotfix: Temporarily disable to avoid foreign key constraint violation
                    UsageType = "PlantAnalysis",
                    UsageDate = now,
                    RequestEndpoint = endpoint,
                    RequestMethod = method,
                    IsSuccessful = isSuccessful,
                    ResponseStatus = responseStatus,
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    DailyQuotaUsed = subscription.CurrentDailyUsage,
                    DailyQuotaLimit = subscription.SubscriptionTier?.DailyRequestLimit ?? 0,
                    MonthlyQuotaUsed = subscription.CurrentMonthlyUsage,
                    MonthlyQuotaLimit = subscription.SubscriptionTier?.MonthlyRequestLimit ?? 0,
                    CreatedDate = now
                };

                _logger.LogInformation("[USAGE_LOG_SAVING] About to save usage log - UserId: {UserId}, CorrelationId: {CorrelationId}",
                    userId, correlationId);

                await _usageLogRepository.LogUsageAsync(usageLog);
                
                _logger.LogInformation("[USAGE_LOG_SUCCESS] Usage logged successfully - UserId: {UserId}, CorrelationId: {CorrelationId}, SubscriptionId: {SubscriptionId}, PlantAnalysisId: {PlantAnalysisId}", 
                    userId, correlationId, subscription.Id, plantAnalysisId);
            }
            catch (Exception ex)
            {
                // Log the error but don't let usage logging failures break the main flow
                _logger.LogError(ex, "[USAGE_LOG_ERROR] Critical error logging usage - UserId: {UserId}, CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}, Message: {ErrorMessage}", 
                    userId, correlationId, ex.GetType().Name, ex.Message);
                
                var innerEx = ex.InnerException;
                var level = 1;
                while (innerEx != null && level <= 3) // Limit to 3 levels
                {
                    _logger.LogError("[USAGE_LOG_INNER_EXCEPTION] Inner Exception Level {Level} - UserId: {UserId}, CorrelationId: {CorrelationId}, Type: {InnerExceptionType}, Message: {InnerMessage}", 
                        level, userId, correlationId, innerEx.GetType().Name, innerEx.Message);
                    innerEx = innerEx.InnerException;
                    level++;
                }
                
                _logger.LogError("[USAGE_LOG_STACK_TRACE] Stack trace - UserId: {UserId}, CorrelationId: {CorrelationId}, StackTrace: {StackTrace}", 
                    userId, correlationId, ex.StackTrace?.Split('\n').Take(10).ToArray().Aggregate((a, b) => $"{a}\n{b}") ?? "No stack trace available");
                
                // Re-throw the exception to see it in the main error log
                throw;
            }
        }

        public async Task ResetDailyUsageForAllUsersAsync()
        {
            await _userSubscriptionRepository.ResetDailyUsageAsync();
        }

        public async Task ResetMonthlyUsageForAllUsersAsync()
        {
            await _userSubscriptionRepository.ResetMonthlyUsageAsync();
        }

        public async Task ProcessExpiredSubscriptionsAsync()
        {
            // Use DateTime.Now instead of DateTime.UtcNow to avoid timezone issues with PostgreSQL
            var now = DateTime.Now;
            
            var expiredSubscriptions = await _userSubscriptionRepository.GetListAsync(
                s => s.IsActive && s.EndDate <= now);

            var expiredList = expiredSubscriptions.ToList();

            foreach (var subscription in expiredList)
            {
                subscription.IsActive = false;
                subscription.QueueStatus = SubscriptionQueueStatus.Expired;
                subscription.Status = "Expired";
                subscription.UpdatedDate = now;
                
                _userSubscriptionRepository.Update(subscription);
            }

            await _userSubscriptionRepository.SaveChangesAsync();

            // Event-driven queue activation: activate queued sponsorships waiting for expired ones
            await ActivateQueuedSponsorshipsAsync(expiredList);
        }

        /// <summary>
        /// Activate queued sponsorships when their previous sponsorship expires (event-driven)
        /// </summary>
        private async Task ActivateQueuedSponsorshipsAsync(List<UserSubscription> expiredSubscriptions)
        {
            foreach (var expired in expiredSubscriptions)
            {
                // ✅ FIXED: Check for queued subscriptions waiting for ANY expired subscription
                // Not just sponsorships - CreditCard, BankTransfer, etc. can also have queued subscriptions
                
                _logger.LogInformation("🔍 [QueueActivation] Checking for queued subscriptions waiting for ID: {ExpiredId} ({PaymentMethod})",
                    expired.Id, expired.PaymentMethod);

                // Find queued sponsorship waiting for this subscription
                var queued = await _userSubscriptionRepository.GetAsync(s =>
                    s.QueueStatus == SubscriptionQueueStatus.Pending &&
                    s.PreviousSponsorshipId == expired.Id);  // ✅ Now references ANY subscription type

                if (queued != null)
                {
                    _logger.LogInformation("🔄 [QueueActivation] Found queued subscription ID: {QueuedId}", queued.Id);
                    _logger.LogInformation("🔄 [QueueActivation] Activating queued subscription for UserId: {UserId}", queued.UserId);

                    // Activate the queued subscription
                    queued.QueueStatus = SubscriptionQueueStatus.Active;
                    queued.ActivatedDate = DateTime.Now;
                    queued.StartDate = DateTime.Now;
                    queued.EndDate = DateTime.Now.AddDays(30);  // 30 days for sponsorships
                    queued.IsActive = true;
                    queued.Status = "Active";
                    queued.PreviousSponsorshipId = null;  // Clear reference
                    queued.UpdatedDate = DateTime.Now;
                    queued.SponsorshipNotes = $"{queued.SponsorshipNotes} | Activated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} after {expired.PaymentMethod} subscription expired";

                    _userSubscriptionRepository.Update(queued);
                    
                    _logger.LogInformation("✅ [QueueActivation] Activated subscription ID: {Id} for UserId: {UserId}", 
                        queued.Id, queued.UserId);
                }
                else
                {
                    _logger.LogInformation("ℹ️ [QueueActivation] No queued subscriptions found for expired ID: {ExpiredId}", expired.Id);
                }
            }

            await _userSubscriptionRepository.SaveChangesAsync();
            _logger.LogInformation("✅ [QueueActivation] Queue activation complete");
        }

        public async Task<Core.Utilities.Results.IDataResult<string>> GetSubscriptionSponsorAsync(int userId)
        {
            try
            {
                var activeSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
                
                if (activeSubscription == null)
                {
                    return new Core.Utilities.Results.ErrorDataResult<string>("No active subscription found");
                }

                // Check if subscription is sponsored (payment method is "Sponsorship")
                if (activeSubscription.PaymentMethod != "Sponsorship")
                {
                    return new Core.Utilities.Results.ErrorDataResult<string>("Subscription is not sponsored");
                }

                // Extract sponsor ID from payment reference (format: SPONSOR-{CODE})
                if (!string.IsNullOrEmpty(activeSubscription.PaymentReference) && 
                    activeSubscription.PaymentReference.StartsWith("SPONSOR-"))
                {
                    // Find the sponsorship code to get sponsor info
                    var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(
                        c => c.Code == activeSubscription.PaymentReference.Replace("SPONSOR-", ""));
                    
                    if (sponsorshipCode?.SponsorId != null)
                    {
                        return new Core.Utilities.Results.SuccessDataResult<string>(
                            $"S{sponsorshipCode.SponsorId:D3}", 
                            "Sponsor found successfully");
                    }
                }

                return new Core.Utilities.Results.ErrorDataResult<string>("Sponsor information not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetSubscriptionSponsorAsync] Error for userId {userId}: {ex.Message}");
                return new Core.Utilities.Results.ErrorDataResult<string>("Error retrieving sponsor information");
            }
        }

        public async Task ProcessAutoRenewalsAsync()
        {
            var expiringSubscriptions = await _userSubscriptionRepository.GetExpiringSubscriptionsAsync(
                DateTime.Now.AddDays(1)); // Process subscriptions expiring in next 24 hours

            foreach (var subscription in expiringSubscriptions)
            {
                if (subscription.AutoRenew && subscription.NextPaymentDate <= DateTime.Now)
                {
                    // Here you would integrate with payment processing
                    // For now, we'll just extend the subscription
                    subscription.EndDate = subscription.EndDate.AddMonths(1);
                    subscription.LastPaymentDate = DateTime.Now;
                    subscription.NextPaymentDate = subscription.EndDate;
                    subscription.UpdatedDate = DateTime.Now;
                    
                    // Reset usage counters for new period
                    subscription.CurrentDailyUsage = 0;
                    subscription.CurrentMonthlyUsage = 0;
                    subscription.LastUsageResetDate = DateTime.Now;
                    subscription.MonthlyUsageResetDate = DateTime.Now;
                    
                    _userSubscriptionRepository.Update(subscription);
                }
            }

            await _userSubscriptionRepository.SaveChangesAsync();
        }

        public async Task<Core.Utilities.Results.IDataResult<Entities.Dtos.SponsorshipDetailsDto>> GetSponsorshipDetailsAsync(int userId)
        {
            try
            {
                var activeSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
                
                if (activeSubscription == null)
                {
                    return new Core.Utilities.Results.SuccessDataResult<Entities.Dtos.SponsorshipDetailsDto>(
                        new Entities.Dtos.SponsorshipDetailsDto { HasSponsor = false }, 
                        "No active subscription found");
                }

                // Check if subscription is sponsored (payment method is "Sponsorship")
                if (activeSubscription.PaymentMethod != "Sponsorship")
                {
                    return new Core.Utilities.Results.SuccessDataResult<Entities.Dtos.SponsorshipDetailsDto>(
                        new Entities.Dtos.SponsorshipDetailsDto { HasSponsor = false }, 
                        "Subscription is not sponsored");
                }

                // Extract sponsor details from payment reference (format: SPONSOR-{CODE} or direct code)
                string codeToFind = activeSubscription.PaymentReference;
                if (codeToFind.StartsWith("SPONSOR-"))
                {
                    codeToFind = codeToFind.Replace("SPONSOR-", "");
                }

                // Find the sponsorship code to get sponsor info
                var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(
                    c => c.Code == codeToFind);
                
                if (sponsorshipCode?.SponsorId != null)
                {
                    var details = new Entities.Dtos.SponsorshipDetailsDto
                    {
                        HasSponsor = true,
                        SponsorId = $"S{sponsorshipCode.SponsorId:D3}",  // Format: S001, S002, etc.
                        SponsorUserId = sponsorshipCode.SponsorId,  // Actual sponsor user ID
                        SponsorshipCodeId = sponsorshipCode.Id,           // SponsorshipCode table ID
                        SponsorshipCode = sponsorshipCode.Code            // The actual code used
                    };

                    return new Core.Utilities.Results.SuccessDataResult<Entities.Dtos.SponsorshipDetailsDto>(
                        details, "Sponsorship details found successfully");
                }

                return new Core.Utilities.Results.SuccessDataResult<Entities.Dtos.SponsorshipDetailsDto>(
                    new Entities.Dtos.SponsorshipDetailsDto { HasSponsor = false }, 
                    "Sponsor information not found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetSponsorshipDetailsAsync] Error for userId {userId}: {ex.Message}");
                return new Core.Utilities.Results.ErrorDataResult<Entities.Dtos.SponsorshipDetailsDto>(
                    "Error retrieving sponsorship details");
            }
        }
    }
}