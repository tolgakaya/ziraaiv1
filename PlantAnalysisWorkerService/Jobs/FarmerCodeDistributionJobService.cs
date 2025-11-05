using Business.Services.Messaging;
using Business.Services.Messaging.Factories;
using Business.Services.Sponsorship;
using DataAccess.Abstract;
using Entities.Concrete;
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
    /// Job service for processing farmer code distribution requests
    /// Pattern: Based on DealerInvitationJobService
    /// </summary>
    public interface IFarmerCodeDistributionJobService
    {
        Task ProcessFarmerCodeDistributionAsync(FarmerCodeDistributionQueueMessage message, string correlationId);
    }

    public class FarmerCodeDistributionJobService : IFarmerCodeDistributionJobService
    {
        private readonly IBulkCodeDistributionJobRepository _bulkJobRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly IMessagingServiceFactory _messagingFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FarmerCodeDistributionJobService> _logger;

        public FarmerCodeDistributionJobService(
            IBulkCodeDistributionJobRepository bulkJobRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IUserRepository userRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            IMessagingServiceFactory messagingFactory,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<FarmerCodeDistributionJobService> logger)
        {
            _bulkJobRepository = bulkJobRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _userRepository = userRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _messagingFactory = messagingFactory;
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
                // Step 4.7: Code Allocation Logic
                // 1. Find user by email
                var user = await _userRepository.GetAsync(u => u.Email == message.Email);
                if (user == null)
                {
                    errorMessage = $"User not found with email: {message.Email}";
                    _logger.LogWarning("[FARMER_CODE_DISTRIBUTION_USER_NOT_FOUND] {Error}", errorMessage);
                    success = false;
                }
                else
                {
                    // 2. Allocate 1 available code from sponsor's purchase
                    var allCodes = await _sponsorshipCodeRepository.GetByPurchaseIdAsync(message.PurchaseId);
                    var code = allCodes.FirstOrDefault(c => !c.IsUsed && c.DistributionDate == null);

                    if (code == null)
                    {
                        errorMessage = $"No available codes for PurchaseId: {message.PurchaseId}";
                        _logger.LogWarning("[FARMER_CODE_DISTRIBUTION_NO_CODES] {Error}", errorMessage);
                        success = false;
                    }
                    else
                    {
                        // 3. Mark code as used by farmer
                        code.IsUsed = true;
                        code.UsedByUserId = user.UserId;
                        code.UsedDate = DateTime.Now;
                        code.DealerId = null; // Direct distribution, no dealer

                        _sponsorshipCodeRepository.Update(code);

                        allocatedCode = code.Code;

                        // 4. Send SMS with code
                        if (!string.IsNullOrEmpty(user.MobilePhones))
                        {
                            var smsMessage = $"ZiraAI Sponsorship Code: {code.Code}. Use this code in the app to activate your subscription.";
                            var smsService = _messagingFactory.GetSmsService();
                            var smsResult = await smsService.SendSmsAsync(user.MobilePhones, smsMessage);

                            if (smsResult.Success)
                            {
                                _logger.LogInformation(
                                    "[FARMER_CODE_DISTRIBUTION_SMS_SENT] SMS sent - Phone: {Phone}, Code: {Code}",
                                    user.MobilePhones, code.Code);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "[FARMER_CODE_DISTRIBUTION_SMS_FAILED] SMS failed - Phone: {Phone}, Error: {Error}",
                                    user.MobilePhones, smsResult.Message);
                            }
                        }

                        success = true;
                        _logger.LogInformation(
                            "[FARMER_CODE_DISTRIBUTION_SUCCESS] Code allocated - Email: {Email}, Code: {Code}, UserId: {UserId}",
                            message.Email, code.Code, user.UserId);
                    }
                }

                // Step 4.8: Progress Update Logic (Atomic)
                var bulkJob = await _bulkJobRepository.IncrementProgressAsync(message.BulkJobId, success);

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

                // Step 4.9: HTTP Callback Logic
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
                    await _bulkJobRepository.IncrementProgressAsync(message.BulkJobId, success: false);
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
    }
}
