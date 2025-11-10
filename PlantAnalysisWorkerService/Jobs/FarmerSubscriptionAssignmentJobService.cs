using Business.Services.Logging;
using Business.Services.Messaging;
using Business.Services.Messaging.Factories;
using Core.Entities.Concrete;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PlantAnalysisWorkerService.Jobs
{
    /// <summary>
    /// Job service for processing farmer subscription assignment requests
    /// Pattern: Admin assigns subscriptions to farmers via bulk upload
    /// </summary>
    public interface IFarmerSubscriptionAssignmentJobService
    {
        Task ProcessFarmerSubscriptionAssignmentAsync(FarmerSubscriptionAssignmentQueueMessage message, string correlationId);
    }

    public class FarmerSubscriptionAssignmentJobService : IFarmerSubscriptionAssignmentJobService
    {
        private readonly IBulkSubscriptionAssignmentJobRepository _bulkJobRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;
        private readonly IMessagingServiceFactory _messagingFactory;
        private readonly ISmsLoggingService _smsLoggingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FarmerSubscriptionAssignmentJobService> _logger;

        public FarmerSubscriptionAssignmentJobService(
            IBulkSubscriptionAssignmentJobRepository bulkJobRepository,
            IUserRepository userRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionTierRepository subscriptionTierRepository,
            IMessagingServiceFactory messagingFactory,
            ISmsLoggingService smsLoggingService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<FarmerSubscriptionAssignmentJobService> logger)
        {
            _bulkJobRepository = bulkJobRepository;
            _userRepository = userRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _subscriptionTierRepository = subscriptionTierRepository;
            _messagingFactory = messagingFactory;
            _smsLoggingService = smsLoggingService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ProcessFarmerSubscriptionAssignmentAsync(FarmerSubscriptionAssignmentQueueMessage message, string correlationId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[FARMER_SUBSCRIPTION_ASSIGNMENT_JOB_START] Processing subscription assignment - BulkJobId: {BulkJobId}, Row: {RowNumber}, Email: {Email}, Phone: {Phone}",
                message.BulkJobId, message.RowNumber, message.Email, message.Phone);

            bool success = false;
            string errorMessage = null;
            int subscriptionsCreated = 0;
            bool isNewSubscription = false;

            try
            {
                // Step 1: Find or create user by email or phone
                User user = null;

                if (!string.IsNullOrWhiteSpace(message.Email))
                {
                    user = await _userRepository.GetAsync(u => u.Email == message.Email);
                }

                if (user == null && !string.IsNullOrWhiteSpace(message.Phone))
                {
                    var normalizedPhone = FormatPhoneNumber(message.Phone);
                    user = await _userRepository.GetAsync(u => u.MobilePhones == normalizedPhone);
                }

                if (user == null)
                {
                    // User doesn't exist - create new user account
                    // This allows admins to pre-create subscriptions for future users
                    var fullName = !string.IsNullOrWhiteSpace(message.FirstName) || !string.IsNullOrWhiteSpace(message.LastName)
                        ? $"{message.FirstName ?? ""} {message.LastName ?? ""}".Trim()
                        : "Farmer User";

                    user = new User
                    {
                        Email = message.Email,
                        MobilePhones = !string.IsNullOrWhiteSpace(message.Phone) ? FormatPhoneNumber(message.Phone) : null,
                        FullName = fullName,
                        RecordDate = DateTime.Now,
                        UpdateContactDate = DateTime.Now,
                        Status = true
                    };

                    _userRepository.Add(user);
                    await _userRepository.SaveChangesAsync();

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_NEW_USER] Created new user - Email: {Email}, Phone: {Phone}, UserId: {UserId}",
                        message.Email, message.Phone, user.UserId);
                }

                // Step 2: Check if user already has a subscription
                var existingSubscription = await _userSubscriptionRepository.GetAsync(
                    s => s.UserId == user.UserId && s.IsActive);

                UserSubscription subscription = null;

                if (existingSubscription != null)
                {
                    // Update existing subscription
                    existingSubscription.SubscriptionTierId = message.SubscriptionTierId;
                    existingSubscription.StartDate = DateTime.Now;
                    existingSubscription.EndDate = DateTime.Now.AddDays(message.DurationDays);
                    existingSubscription.Status = message.AutoActivate ? "Active" : "Pending";
                    existingSubscription.IsActive = message.AutoActivate;
                    existingSubscription.UpdatedDate = DateTime.Now;

                    // Reset usage counters for new subscription period
                    existingSubscription.CurrentDailyUsage = 0;
                    existingSubscription.CurrentMonthlyUsage = 0;
                    existingSubscription.LastUsageResetDate = DateTime.Now;
                    existingSubscription.MonthlyUsageResetDate = DateTime.Now;

                    _userSubscriptionRepository.Update(existingSubscription);
                    await _userSubscriptionRepository.SaveChangesAsync();
                    subscription = existingSubscription;
                    isNewSubscription = false;
                    subscriptionsCreated = 0; // Updated, not created

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_UPDATED] Updated existing subscription - UserId: {UserId}, SubscriptionId: {SubscriptionId}, TierId: {TierId}",
                        user.UserId, subscription.Id, message.SubscriptionTierId);
                }
                else
                {
                    // Create new subscription
                    subscription = new UserSubscription
                    {
                        UserId = user.UserId,
                        SubscriptionTierId = message.SubscriptionTierId,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddDays(message.DurationDays),
                        Status = message.AutoActivate ? "Active" : "Pending",
                        IsActive = message.AutoActivate,
                        AutoRenew = false,
                        CreatedDate = DateTime.Now,
                        CurrentDailyUsage = 0,
                        CurrentMonthlyUsage = 0,
                        LastUsageResetDate = DateTime.Now,
                        MonthlyUsageResetDate = DateTime.Now
                    };

                    _userSubscriptionRepository.Add(subscription);
                    await _userSubscriptionRepository.SaveChangesAsync();
                    isNewSubscription = true;
                    subscriptionsCreated = 1;

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_CREATED] Created new subscription - UserId: {UserId}, SubscriptionId: {SubscriptionId}, TierId: {TierId}",
                        user.UserId, subscription.Id, message.SubscriptionTierId);
                }

                success = true;

                // Step 3: Send notification (if enabled)
                bool notificationSent = false;
                if (message.SendNotification)
                {
                    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == message.SubscriptionTierId);
                    var tierName = tier?.DisplayName ?? tier?.TierName ?? "Subscription";

                    if (message.NotificationMethod == "SMS" && !string.IsNullOrWhiteSpace(message.Phone))
                    {
                        notificationSent = await SendSmsNotificationAsync(user, tierName, message.DurationDays, message.AdminId);
                    }
                    else if (message.NotificationMethod == "Email" && !string.IsNullOrWhiteSpace(message.Email))
                    {
                        // Email notification implementation would go here
                        // For now, we'll log it
                        _logger.LogInformation(
                            "[FARMER_SUBSCRIPTION_EMAIL_NOTIFICATION] Email notification requested but not implemented yet - Email: {Email}",
                            message.Email);
                    }
                }

                // Step 4: Atomic progress update
                var bulkJob = await _bulkJobRepository.IncrementProgressAsync(
                    message.BulkJobId,
                    success,
                    subscriptionsCreated,
                    notificationSent);

                if (bulkJob == null)
                {
                    _logger.LogWarning(
                        "[FARMER_SUBSCRIPTION_BULK_NOT_FOUND] BulkJob not found - BulkJobId: {BulkJobId}",
                        message.BulkJobId);
                    return;
                }

                // Check if all assignments are complete
                bool isComplete = await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);

                if (isComplete)
                {
                    // Reload to get updated status
                    bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_BULK_COMPLETED] BulkJob completed - BulkJobId: {BulkJobId}, Status: {Status}, Success: {Success}, Failed: {Failed}",
                        message.BulkJobId, bulkJob.Status, bulkJob.SuccessfulAssignments, bulkJob.FailedAssignments);
                }

                // Step 5: HTTP callback for progress notification
                var progressDto = new BulkSubscriptionAssignmentProgressDto
                {
                    JobId = bulkJob.Id,
                    Status = bulkJob.Status,
                    TotalFarmers = bulkJob.TotalFarmers,
                    ProcessedFarmers = bulkJob.ProcessedFarmers,
                    SuccessfulAssignments = bulkJob.SuccessfulAssignments,
                    FailedAssignments = bulkJob.FailedAssignments,
                    NewSubscriptionsCreated = bulkJob.NewSubscriptionsCreated,
                    ExistingSubscriptionsUpdated = bulkJob.ExistingSubscriptionsUpdated,
                    TotalNotificationsSent = bulkJob.TotalNotificationsSent,
                    CreatedDate = bulkJob.CreatedDate,
                    StartedDate = bulkJob.StartedDate,
                    CompletedDate = bulkJob.CompletedDate,
                    ResultFileUrl = bulkJob.ResultFileUrl
                };

                await SendProgressNotificationViaHttp(progressDto);

                // Send completion notification if job is done
                if (isComplete)
                {
                    await SendCompletionNotificationViaHttp(
                        bulkJob.Id,
                        bulkJob.AdminId,
                        bulkJob.Status,
                        bulkJob.SuccessfulAssignments,
                        bulkJob.FailedAssignments);

                    _logger.LogInformation(
                        "✅ Completion notification sent - AdminId: {AdminId}, Status: {Status}",
                        bulkJob.AdminId, bulkJob.Status);
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[FARMER_SUBSCRIPTION_ASSIGNMENT_JOB_COMPLETED] Processing completed - Duration: {Duration}ms, Success: {Success}",
                    stopwatch.ElapsedMilliseconds, success);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[FARMER_SUBSCRIPTION_ASSIGNMENT_JOB_ERROR] Error processing assignment - BulkJobId: {BulkJobId}, Email: {Email}, Duration: {Duration}ms",
                    message.BulkJobId, message.Email, stopwatch.ElapsedMilliseconds);

                // Update bulk job with failure using atomic operations
                try
                {
                    await _bulkJobRepository.IncrementProgressAsync(
                        message.BulkJobId,
                        success: false,
                        subscriptionsCreated: 0,
                        notificationSent: false);
                    await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "[FARMER_SUBSCRIPTION_UPDATE_ERROR] Failed to update bulk job after error");
                }

                throw; // Re-throw for Hangfire retry
            }
        }

        /// <summary>
        /// Send SMS notification to farmer about subscription assignment
        /// </summary>
        private async Task<bool> SendSmsNotificationAsync(User user, string tierName, int durationDays, int adminId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.MobilePhones))
                {
                    _logger.LogWarning("[FARMER_SUBSCRIPTION_NO_PHONE] No phone number for user - UserId: {UserId}", user.UserId);
                    return false;
                }

                var normalizedPhone = FormatPhoneNumber(user.MobilePhones);
                var farmerName = string.IsNullOrWhiteSpace(user.FullName) ? "Değerli Üyemiz" : user.FullName.Split(' ')[0];
                var smsMessage = BuildSubscriptionSmsMessage(farmerName, tierName, durationDays);

                var smsService = _messagingFactory.GetSmsService();
                var smsResult = await smsService.SendSmsAsync(normalizedPhone, smsMessage);

                if (smsResult.Success)
                {
                    // Log SMS to database
                    await _smsLoggingService.LogCodeDistributeAsync(
                        phone: normalizedPhone,
                        message: smsMessage,
                        code: $"SUB-{tierName}",
                        sponsorId: adminId,
                        senderUserId: adminId,
                        additionalData: new
                        {
                            userId = user.UserId,
                            subscriptionTier = tierName,
                            durationDays
                        });

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_SMS_SENT] SMS sent - Phone: {Phone}, Tier: {Tier}",
                        normalizedPhone, tierName);

                    return true;
                }
                else
                {
                    _logger.LogWarning(
                        "[FARMER_SUBSCRIPTION_SMS_FAILED] SMS failed - Phone: {Phone}, Error: {Error}",
                        normalizedPhone, smsResult.Message);

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FARMER_SUBSCRIPTION_SMS_ERROR] Error sending SMS notification");
                return false;
            }
        }

        /// <summary>
        /// Build SMS message for subscription assignment
        /// </summary>
        private string BuildSubscriptionSmsMessage(string farmerName, string tierName, int durationDays)
        {
            var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
            var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

            return $@"🎉 Tebrikler {farmerName}!

Size {tierName} aboneliği tanımlandı.
Süre: {durationDays} gün

Hemen kullanmaya başlayın:
{playStoreLink}

ZiraAI ile tarımda başarı!";
        }

        /// <summary>
        /// Send progress notification to WebAPI via HTTP (cross-process communication)
        /// </summary>
        private async Task SendProgressNotificationViaHttp(BulkSubscriptionAssignmentProgressDto progress)
        {
            try
            {
                var internalSecret = _configuration.GetValue<string>("WebAPI:InternalSecret")
                                    ?? "ZiraAI_Internal_Secret_2025";

                var httpClient = _httpClientFactory.CreateClient("WebAPI");
                var endpoint = "/api/internal/signalr/bulk-subscription-assignment-progress";

                var requestBody = new
                {
                    internalSecret,
                    progress
                };

                _logger.LogInformation(
                    "📤 Sending progress notification to WebAPI - Endpoint: {Endpoint}, JobId: {JobId}, Progress: {Progress}%",
                    endpoint, progress.JobId, progress.ProgressPercentage);

                var response = await httpClient.PostAsJsonAsync(endpoint, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Progress notification sent successfully to WebAPI");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ Failed to send progress notification - StatusCode: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send progress notification to WebAPI");
                // Don't throw - notification failure shouldn't stop job processing
            }
        }

        /// <summary>
        /// Send completion notification to WebAPI via HTTP (cross-process communication)
        /// </summary>
        private async Task SendCompletionNotificationViaHttp(
            int jobId,
            int adminId,
            string status,
            int successCount,
            int failedCount)
        {
            try
            {
                var internalSecret = _configuration.GetValue<string>("WebAPI:InternalSecret")
                                    ?? "ZiraAI_Internal_Secret_2025";

                var httpClient = _httpClientFactory.CreateClient("WebAPI");
                var endpoint = "/api/internal/signalr/bulk-subscription-assignment-completed";

                var requestBody = new
                {
                    internalSecret,
                    jobId,
                    adminId,
                    status,
                    successCount,
                    failedCount
                };

                _logger.LogInformation(
                    "📤 Sending completion notification to WebAPI - Endpoint: {Endpoint}, JobId: {JobId}, Status: {Status}",
                    endpoint, jobId, status);

                var response = await httpClient.PostAsJsonAsync(endpoint, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Completion notification sent successfully to WebAPI");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ Failed to send completion notification - StatusCode: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send completion notification to WebAPI");
                // Don't throw - notification failure shouldn't stop job processing
            }
        }

        /// <summary>
        /// Format phone number - add Turkey country code and + prefix
        /// </summary>
        private string FormatPhoneNumber(string phone)
        {
            // Remove all non-numeric characters
            var cleaned = new string(phone.Where(char.IsDigit).ToArray());

            // Add Turkey country code if not present
            if (!cleaned.StartsWith("90") && cleaned.Length == 10)
            {
                cleaned = "90" + cleaned;
            }

            // Add + prefix
            if (!cleaned.StartsWith("+"))
            {
                cleaned = "+" + cleaned;
            }

            return cleaned;
        }
    }
}
