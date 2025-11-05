using Business.Services.Logging;
using Business.Services.Messaging;
using Business.Services.Messaging.Factories;
using Business.Services.Sponsorship;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PlantAnalysisWorkerService.Jobs
{
    /// <summary>
    /// Job service for processing farmer code distribution requests
    /// Pattern: SAME AS SendSponsorshipLinkCommand - distribute codes without user lookup
    /// </summary>
    public interface IFarmerCodeDistributionJobService
    {
        Task ProcessFarmerCodeDistributionAsync(FarmerCodeDistributionQueueMessage message, string correlationId);
    }

    public class FarmerCodeDistributionJobService : IFarmerCodeDistributionJobService
    {
        private readonly IBulkCodeDistributionJobRepository _bulkJobRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly IMessagingServiceFactory _messagingFactory;
        private readonly ISmsLoggingService _smsLoggingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FarmerCodeDistributionJobService> _logger;

        public FarmerCodeDistributionJobService(
            IBulkCodeDistributionJobRepository bulkJobRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            IMessagingServiceFactory messagingFactory,
            ISmsLoggingService smsLoggingService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<FarmerCodeDistributionJobService> logger)
        {
            _bulkJobRepository = bulkJobRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _messagingFactory = messagingFactory;
            _smsLoggingService = smsLoggingService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ProcessFarmerCodeDistributionAsync(FarmerCodeDistributionQueueMessage message, string correlationId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[FARMER_CODE_DISTRIBUTION_JOB_START] Processing farmer code distribution - BulkJobId: {BulkJobId}, Row: {RowNumber}, Email: {Email}",
                message.BulkJobId, message.RowNumber, message.Email);

            bool success = false;
            string errorMessage = null;
            string allocatedCode = null;

            try
            {
                // Step 1: Get available code from sponsor's purchase (SAME AS SendSponsorshipLinkCommand)
                var allCodes = await _sponsorshipCodeRepository.GetByPurchaseIdAsync(message.PurchaseId);
                var code = allCodes.FirstOrDefault(c => !c.IsUsed && c.DistributionDate == null && c.ExpiryDate > DateTime.Now);

                if (code == null)
                {
                    errorMessage = $"No available codes for PurchaseId: {message.PurchaseId}";
                    _logger.LogWarning("[FARMER_CODE_DISTRIBUTION_NO_CODES] {Error}", errorMessage);
                    success = false;
                }
                else
                {
                    allocatedCode = code.Code;

                    // Step 2: Send SMS with code (if SendSms is enabled)
                    // NOTE: User doesn't need to exist in system - they'll register when redeeming
                    // This is EXACTLY like SendSponsorshipLinkCommand behavior
                    if (message.SendSms && !string.IsNullOrEmpty(message.Phone))
                    {
                        // Get sponsor profile information for SMS template
                        var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == message.SponsorId);
                        var sponsorCompanyName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor";

                        // Get Play Store package name from configuration
                        var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
                        var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

                        // Generate redemption deep link
                        var baseUrl = _configuration["WebAPI:BaseUrl"]
                            ?? _configuration["Referral:FallbackDeepLinkBaseUrl"]?.TrimEnd('/').Replace("/ref", "")
                            ?? "https://ziraai.com";
                        var deepLink = $"{baseUrl.TrimEnd('/')}/redeem/{code.Code}";

                        // Build SMS message (same format as SendSponsorshipLinkCommand)
                        var farmerName = message.FarmerName ?? "Değerli Üyemiz";
                        var smsMessage = BuildSmsMessage(farmerName, sponsorCompanyName, code.Code, playStoreLink, deepLink);

                        // Normalize phone number before sending (same as SendSponsorshipLinkCommand.FormatPhoneNumber)
                        var normalizedPhone = FormatPhoneNumber(message.Phone);

                        var smsService = _messagingFactory.GetSmsService();
                        var smsResult = await smsService.SendSmsAsync(normalizedPhone, smsMessage);

                        if (smsResult.Success)
                        {
                            // Update code entity with distribution info (SAME AS SendSponsorshipLinkCommand)
                            // NOTE: We do NOT set IsUsed/UsedByUserId/UsedDate here!
                            // Code is only DISTRIBUTED, not REDEEMED yet
                            // Farmer will redeem it later through the app
                            code.RedemptionLink = deepLink;
                            code.RecipientPhone = normalizedPhone;
                            code.RecipientName = farmerName;
                            code.LinkSentDate = DateTime.Now;
                            code.LinkSentVia = "SMS";
                            code.LinkDelivered = true;
                            code.DistributionChannel = "SMS";
                            code.DistributionDate = DateTime.Now;
                            code.DistributedTo = $"{farmerName} ({normalizedPhone})";

                            _sponsorshipCodeRepository.Update(code);

                            success = true;

                            // Log SMS to database (if enabled via configuration)
                            await _smsLoggingService.LogCodeDistributeAsync(
                                phone: normalizedPhone,
                                message: smsMessage,
                                code: code.Code,
                                sponsorId: message.SponsorId,
                                senderUserId: message.SponsorId,
                                additionalData: new
                                {
                                    farmerName,
                                    farmerEmail = message.Email,
                                    bulkJobId = message.BulkJobId,
                                    rowNumber = message.RowNumber,
                                    purchaseId = message.PurchaseId
                                });

                            _logger.LogInformation(
                                "[FARMER_CODE_DISTRIBUTION_SMS_SENT] SMS sent - Phone: {Phone}, Code: {Code}",
                                normalizedPhone, code.Code);
                        }
                        else
                        {
                            errorMessage = $"SMS failed: {smsResult.Message}";
                            _logger.LogWarning(
                                "[FARMER_CODE_DISTRIBUTION_SMS_FAILED] SMS failed - Phone: {Phone}, Error: {Error}",
                                normalizedPhone, smsResult.Message);
                            success = false;
                        }
                    }
                    else if (!message.SendSms)
                    {
                        // Code allocated but SMS not requested - still success
                        _logger.LogInformation(
                            "[FARMER_CODE_DISTRIBUTION_NO_SMS] Code allocated without SMS - Email: {Email}, Code: {Code}",
                            message.Email, code.Code);
                        success = true;
                    }
                    else
                    {
                        // No phone number provided
                        errorMessage = "Phone number is required when SendSms is enabled";
                        _logger.LogWarning("[FARMER_CODE_DISTRIBUTION_NO_PHONE] {Error}", errorMessage);
                        success = false;
                    }
                }

                // Step 3: Progress Update Logic (Atomic)
                // Each farmer gets exactly 1 code (same as single distribution)
                var codesDistributed = success ? 1 : 0;
                var smsSent = success && message.SendSms;
                
                var bulkJob = await _bulkJobRepository.IncrementProgressAsync(
                    message.BulkJobId, 
                    success, 
                    codesDistributed, 
                    smsSent);

                if (bulkJob == null)
                {
                    _logger.LogWarning(
                        "[FARMER_CODE_DISTRIBUTION_BULK_NOT_FOUND] BulkJob not found - BulkJobId: {BulkJobId}",
                        message.BulkJobId);
                    return;
                }

                // Check if all distributions are complete
                bool isComplete = await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);

                if (isComplete)
                {
                    // Reload to get updated status
                    bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);

                    _logger.LogInformation(
                        "[FARMER_CODE_DISTRIBUTION_BULK_COMPLETED] BulkJob completed - BulkJobId: {BulkJobId}, Status: {Status}, Success: {Success}, Failed: {Failed}",
                        message.BulkJobId, bulkJob.Status, bulkJob.SuccessfulDistributions, bulkJob.FailedDistributions);
                }

                // Step 4: HTTP Callback Logic
                // Send progress notification to WebAPI for SignalR broadcasting
                var progressDto = new Entities.Dtos.BulkCodeDistributionProgressDto
                {
                    JobId = bulkJob.Id,
                    SponsorId = bulkJob.SponsorId,
                    Status = bulkJob.Status,
                    TotalFarmers = bulkJob.TotalFarmers,
                    ProcessedFarmers = bulkJob.ProcessedFarmers,
                    SuccessfulDistributions = bulkJob.SuccessfulDistributions,
                    FailedDistributions = bulkJob.FailedDistributions,
                    ProgressPercentage = (int)Math.Round((decimal)bulkJob.ProcessedFarmers / bulkJob.TotalFarmers * 100, 2),
                    TotalCodesDistributed = bulkJob.TotalCodesDistributed,
                    TotalSmsSent = bulkJob.TotalSmsSent
                };

                await SendProgressNotificationViaHttp(progressDto);

                // Send completion notification if job is done
                if (isComplete)
                {
                    await SendCompletionNotificationViaHttp(
                        bulkJob.Id,
                        bulkJob.SponsorId,
                        bulkJob.Status,
                        bulkJob.SuccessfulDistributions,
                        bulkJob.FailedDistributions);

                    _logger.LogInformation(
                        "✅ Completion notification sent - SponsorId: {SponsorId}, Status: {Status}",
                        bulkJob.SponsorId, bulkJob.Status);
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[FARMER_CODE_DISTRIBUTION_JOB_COMPLETED] Processing completed - Duration: {Duration}ms, Success: {Success}",
                    stopwatch.ElapsedMilliseconds, success);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[FARMER_CODE_DISTRIBUTION_JOB_ERROR] Error processing distribution - BulkJobId: {BulkJobId}, Email: {Email}, Duration: {Duration}ms",
                    message.BulkJobId, message.Email, stopwatch.ElapsedMilliseconds);

                // Update bulk job with failure using atomic operations
                try
                {
                    await _bulkJobRepository.IncrementProgressAsync(
                        message.BulkJobId, 
                        success: false, 
                        codesDistributed: 0, 
                        smsSent: false);
                    await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "[FARMER_CODE_DISTRIBUTION_UPDATE_ERROR] Failed to update bulk job after error");
                }

                throw; // Re-throw for Hangfire retry
            }
        }

        /// <summary>
        /// Send progress notification to WebAPI via HTTP (cross-process communication)
        /// WebAPI will broadcast via SignalR Hub
        /// </summary>
        private async Task SendProgressNotificationViaHttp(Entities.Dtos.BulkCodeDistributionProgressDto progress)
        {
            try
            {
                var internalSecret = _configuration.GetValue<string>("WebAPI:InternalSecret")
                                    ?? "ZiraAI_Internal_Secret_2025";

                var httpClient = _httpClientFactory.CreateClient("WebAPI");
                var endpoint = "/api/internal/signalr/bulk-code-distribution-progress";

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
        /// WebAPI will broadcast via SignalR Hub
        /// </summary>
        private async Task SendCompletionNotificationViaHttp(
            int jobId,
            int sponsorId,
            string status,
            int successCount,
            int failedCount)
        {
            try
            {
                var internalSecret = _configuration.GetValue<string>("WebAPI:InternalSecret")
                                    ?? "ZiraAI_Internal_Secret_2025";

                var httpClient = _httpClientFactory.CreateClient("WebAPI");
                var endpoint = "/api/internal/signalr/bulk-code-distribution-completed";

                var requestBody = new
                {
                    internalSecret,
                    jobId,
                    sponsorId,
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
        /// Build SMS message with sponsor info, code, and deep link (SAME AS SendSponsorshipLinkCommand)
        /// </summary>
        private string BuildSmsMessage(string farmerName, string sponsorCompany, string sponsorCode, string playStoreLink, string deepLink)
        {
            // SMS-based deferred deep linking: Mobile app will read SMS and auto-extract AGRI-XXXXX code
            // Deep link allows users to tap and open app directly with code pre-filled
            return $@"🎁 {sponsorCompany} size sponsorluk paketi hediye etti!

Sponsorluk Kodunuz: {sponsorCode}

Hemen kullanmak için tıklayın:
{deepLink}

Veya uygulamayı indirin:
{playStoreLink}";
        }

        /// <summary>
        /// Format phone number (SAME AS SendSponsorshipLinkCommand.FormatPhoneNumber)
        /// Add Turkey country code and + prefix
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
