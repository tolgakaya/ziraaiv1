using System;
using System.Threading.Tasks;
using Business.Constants;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
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

        public SubscriptionValidationService(
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionUsageLogRepository usageLogRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _userSubscriptionRepository = userSubscriptionRepository;
            _usageLogRepository = usageLogRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IDataResult> CheckSubscriptionStatusAsync(int userId)
        {
            var subscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
            
            if (subscription == null)
            {
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

            // Check if subscription is expired
            if (subscription.EndDate <= DateTime.Now)
            {
                subscription.IsActive = false;
                subscription.Status = "Expired";
                _userSubscriptionRepository.Update(subscription);
                await _userSubscriptionRepository.SaveChangesAsync();

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

            // Reset daily usage if needed
            if (subscription.LastUsageResetDate?.Date < DateTime.Now.Date)
            {
                subscription.CurrentDailyUsage = 0;
                subscription.LastUsageResetDate = DateTime.Now;
            }

            // Reset monthly usage if needed
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (subscription.MonthlyUsageResetDate == null || subscription.MonthlyUsageResetDate < currentMonth)
            {
                subscription.CurrentMonthlyUsage = 0;
                subscription.MonthlyUsageResetDate = currentMonth;
            }

            var tier = subscription.SubscriptionTier;
            var dailyRemaining = tier.DailyRequestLimit - subscription.CurrentDailyUsage;
            var monthlyRemaining = tier.MonthlyRequestLimit - subscription.CurrentMonthlyUsage;

            var canMakeRequest = dailyRemaining > 0 && monthlyRemaining > 0;
            string limitMessage = null;

            if (dailyRemaining <= 0)
            {
                limitMessage = $"Daily request limit reached ({tier.DailyRequestLimit} requests). Resets at midnight.";
            }
            else if (monthlyRemaining <= 0)
            {
                limitMessage = $"Monthly request limit reached ({tier.MonthlyRequestLimit} requests). Resets on the 1st of next month.";
            }

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
                CanMakeRequest = canMakeRequest,
                LimitExceededMessage = limitMessage,
                NextDailyReset = DateTime.Now.Date.AddDays(1),
                NextMonthlyReset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1),
                SubscriptionEndDate = subscription.EndDate
            };

            if (canMakeRequest)
            {
                try
                {
                    Console.WriteLine($"[CheckSubscriptionStatusAsync] Updating subscription counters and saving changes...");
                    
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
                    Console.WriteLine($"[CheckSubscriptionStatusAsync] ✅ Subscription updated successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CheckSubscriptionStatusAsync] ❌ ERROR saving subscription changes:");
                    Console.WriteLine($"[CheckSubscriptionStatusAsync] Exception: {ex.Message}");
                    Console.WriteLine($"[CheckSubscriptionStatusAsync] Exception Type: {ex.GetType().FullName}");
                    
                    var innerEx = ex.InnerException;
                    var level = 1;
                    while (innerEx != null)
                    {
                        Console.WriteLine($"[CheckSubscriptionStatusAsync] Inner Exception {level}: {innerEx.Message}");
                        Console.WriteLine($"[CheckSubscriptionStatusAsync] Inner Exception {level} Type: {innerEx.GetType().FullName}");
                        innerEx = innerEx.InnerException;
                        level++;
                    }
                    
                    // Don't throw - let the request continue without counter update
                    Console.WriteLine($"[CheckSubscriptionStatusAsync] ⚠️ Continuing without counter update to avoid blocking user");
                }
            }

            return new SuccessDataResult(status);
        }

        public async Task<IResult> ValidateAndLogUsageAsync(int userId, string endpoint, string method)
        {
            try
            {
                Console.WriteLine($"[ValidateAndLogUsageAsync] 🔍 Starting validation for userId: {userId}, endpoint: {endpoint}");
                
                var statusResult = await CheckSubscriptionStatusAsync(userId);
                Console.WriteLine($"[ValidateAndLogUsageAsync] CheckSubscriptionStatusAsync result: Success={statusResult.Success}");
                
                if (!statusResult.Success || !statusResult.Data.CanMakeRequest)
                {
                    Console.WriteLine($"[ValidateAndLogUsageAsync] ❌ Validation failed, attempting to log failed usage...");
                    // Log failed attempt
                    await LogUsageAsync(userId, endpoint, method, false, statusResult.Message);
                    Console.WriteLine($"[ValidateAndLogUsageAsync] ✅ Failed usage logged successfully");
                    return new ErrorResult(statusResult.Data?.LimitExceededMessage ?? statusResult.Message);
                }

                Console.WriteLine($"[ValidateAndLogUsageAsync] ✅ Validation successful");
                // Log successful validation (actual usage will be logged after successful request)
                return new SuccessResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ValidateAndLogUsageAsync] ❌ EXCEPTION in ValidateAndLogUsageAsync:");
                Console.WriteLine($"[ValidateAndLogUsageAsync] Exception: {ex.Message}");
                Console.WriteLine($"[ValidateAndLogUsageAsync] Exception Type: {ex.GetType().FullName}");
                Console.WriteLine($"[ValidateAndLogUsageAsync] Stack trace: {ex.StackTrace}");
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
            var subscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
            
            if (subscription == null)
            {
                return new ErrorResult("No active subscription found");
            }

            // Increment usage counters
            await _userSubscriptionRepository.UpdateUsageCountersAsync(subscription.Id, 1, 1);

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
            try 
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var subscription = subscriptionId.HasValue 
                    ? await _userSubscriptionRepository.GetAsync(s => s.Id == subscriptionId.Value)
                    : await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

                // Only log if we have a valid subscription to avoid foreign key constraint violations
                if (subscription == null)
                {
                    Console.WriteLine($"[UsageLog] Warning: No active subscription found for userId {userId}, skipping usage log");
                    return;
                }

                // Use DateTime.Now instead of DateTime.UtcNow to avoid timezone issues with PostgreSQL
                var now = DateTime.Now;
                
                var usageLog = new SubscriptionUsageLog
                {
                    UserId = userId,
                    UserSubscriptionId = subscription.Id, // Now guaranteed to be valid
                    PlantAnalysisId = plantAnalysisId,
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

                await _usageLogRepository.LogUsageAsync(usageLog);
            }
            catch (Exception ex)
            {
                // Log the error but don't let usage logging failures break the main flow
                Console.WriteLine($"[UsageLog] ❌ CRITICAL ERROR logging usage for userId {userId}:");
                Console.WriteLine($"[UsageLog] Exception: {ex.Message}");
                Console.WriteLine($"[UsageLog] Exception Type: {ex.GetType().FullName}");
                
                var innerEx = ex.InnerException;
                var level = 1;
                while (innerEx != null)
                {
                    Console.WriteLine($"[UsageLog] Inner Exception {level}: {innerEx.Message}");
                    Console.WriteLine($"[UsageLog] Inner Exception {level} Type: {innerEx.GetType().FullName}");
                    innerEx = innerEx.InnerException;
                    level++;
                }
                
                Console.WriteLine($"[UsageLog] Stack trace: {ex.StackTrace}");
                
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

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.IsActive = false;
                subscription.Status = "Expired";
                subscription.UpdatedDate = now;
                
                _userSubscriptionRepository.Update(subscription);
            }

            await _userSubscriptionRepository.SaveChangesAsync();
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