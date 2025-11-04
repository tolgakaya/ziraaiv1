using Business.Handlers.Sponsorship.Commands;
using Business.Services.Notification;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
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
    public interface IDealerInvitationJobService
    {
        Task ProcessDealerInvitationAsync(DealerInvitationQueueMessage message, string correlationId);
    }

    public class DealerInvitationJobService : IDealerInvitationJobService
    {
        private readonly IMediator _mediator;
        private readonly IBulkInvitationJobRepository _bulkJobRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DealerInvitationJobService> _logger;

        public DealerInvitationJobService(
            IMediator mediator,
            IBulkInvitationJobRepository bulkJobRepository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<DealerInvitationJobService> logger)
        {
            _mediator = mediator;
            _bulkJobRepository = bulkJobRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ProcessDealerInvitationAsync(DealerInvitationQueueMessage message, string correlationId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[DEALER_INVITATION_JOB_START] Processing dealer invitation - BulkJobId: {BulkJobId}, Row: {RowNumber}, Email: {Email}",
                message.BulkJobId, message.RowNumber, message.Email);

            try
            {
                // 1. Create dealer invitation using existing handler
                var command = new CreateDealerInvitationCommand
                {
                    SponsorId = message.SponsorId,
                    Email = message.Email,
                    Phone = message.Phone,
                    DealerName = message.DealerName,
                    InvitationType = message.InvitationType,
                    PackageTier = message.PackageTier,
                    CodeCount = message.CodeCount,
                    PurchaseId = null // We're using tier-based selection
                };

                var result = await _mediator.Send(command);

                // 2. Atomically update bulk job progress (prevents race conditions)
                var bulkJob = await _bulkJobRepository.IncrementProgressAsync(message.BulkJobId, result.Success);

                if (bulkJob == null)
                {
                    _logger.LogWarning(
                        "[DEALER_INVITATION_JOB_BULK_NOT_FOUND] BulkJob not found - BulkJobId: {BulkJobId}",
                        message.BulkJobId);
                    return;
                }

                // Log success/failure
                if (result.Success)
                {
                    _logger.LogInformation(
                        "[DEALER_INVITATION_JOB_SUCCESS] Invitation successful - Email: {Email}, InvitationId: {InvitationId}",
                        message.Email, result.Data?.InvitationId);
                }
                else
                {
                    _logger.LogWarning(
                        "[DEALER_INVITATION_JOB_FAILED] Invitation failed - Email: {Email}, Error: {Error}",
                        message.Email, result.Message);
                }

                // 3. Check if all invitations are complete and mark as done
                bool isComplete = await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);

                if (isComplete)
                {
                    // Reload to get updated status
                    bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);

                    _logger.LogInformation(
                        "[DEALER_INVITATION_JOB_BULK_COMPLETED] BulkJob completed - BulkJobId: {BulkJobId}, Status: {Status}, Success: {Success}, Failed: {Failed}",
                        message.BulkJobId, bulkJob.Status, bulkJob.SuccessfulInvitations, bulkJob.FailedInvitations);
                }

                // 4. Send progress notification via HTTP to WebAPI (cross-process communication)
                var progressDto = new BulkInvitationProgressDto
                {
                    BulkJobId = bulkJob.Id,
                    SponsorId = bulkJob.SponsorId,
                    Status = bulkJob.Status,
                    TotalDealers = bulkJob.TotalDealers,
                    ProcessedDealers = bulkJob.ProcessedDealers,
                    SuccessfulInvitations = bulkJob.SuccessfulInvitations,
                    FailedInvitations = bulkJob.FailedInvitations,
                    ProgressPercentage = Math.Round((decimal)bulkJob.ProcessedDealers / bulkJob.TotalDealers * 100, 2),
                    LatestDealerEmail = message.Email,
                    LatestDealerSuccess = result.Success,
                    LatestDealerError = result.Success ? null : result.Message,
                    LastUpdateTime = DateTime.Now
                };

                await SendProgressNotificationViaHttp(progressDto);

                // 5. Send completion notification if job is done
                if (isComplete)
                {
                    await SendCompletionNotificationViaHttp(
                        bulkJob.Id,
                        bulkJob.SponsorId,
                        bulkJob.Status,
                        bulkJob.SuccessfulInvitations,
                        bulkJob.FailedInvitations);

                    _logger.LogInformation(
                        "✅ Completion notification sent - SponsorId: {SponsorId}, Status: {Status}",
                        bulkJob.SponsorId, bulkJob.Status);
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "[DEALER_INVITATION_JOB_COMPLETED] Processing completed - Duration: {Duration}ms, Success: {Success}",
                    stopwatch.ElapsedMilliseconds, result.Success);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[DEALER_INVITATION_JOB_ERROR] Error processing invitation - BulkJobId: {BulkJobId}, Email: {Email}, Duration: {Duration}ms",
                    message.BulkJobId, message.Email, stopwatch.ElapsedMilliseconds);

                // Update bulk job with failure using atomic operations
                try
                {
                    await _bulkJobRepository.IncrementProgressAsync(message.BulkJobId, success: false);
                    await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "[DEALER_INVITATION_JOB_UPDATE_ERROR] Failed to update bulk job after error");
                }

                throw; // Re-throw for Hangfire retry
            }
        }

        /// <summary>
        /// Send progress notification to WebAPI via HTTP (cross-process communication)
        /// WebAPI will broadcast via SignalR Hub
        /// </summary>
        private async Task SendProgressNotificationViaHttp(BulkInvitationProgressDto progress)
        {
            try
            {
                var internalSecret = _configuration.GetValue<string>("WebAPI:InternalSecret")
                                    ?? "ZiraAI_Internal_Secret_2025";

                // Use named HttpClient (pre-configured with BaseAddress and Timeout)
                var httpClient = _httpClientFactory.CreateClient("WebAPI");

                var endpoint = "/api/internal/signalr/bulk-invitation-progress";

                var requestBody = new
                {
                    internalSecret,
                    progress
                };

                _logger.LogInformation(
                    "📤 Sending progress notification to WebAPI - Endpoint: {Endpoint}, BulkJobId: {BulkJobId}, Progress: {Progress}%",
                    endpoint, progress.BulkJobId, progress.ProgressPercentage);

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
            int bulkJobId,
            int sponsorId,
            string status,
            int successCount,
            int failedCount)
        {
            try
            {
                var internalSecret = _configuration.GetValue<string>("WebAPI:InternalSecret")
                                    ?? "ZiraAI_Internal_Secret_2025";

                // Use named HttpClient (pre-configured with BaseAddress and Timeout)
                var httpClient = _httpClientFactory.CreateClient("WebAPI");

                var endpoint = "/api/internal/signalr/bulk-invitation-completed";

                var requestBody = new
                {
                    internalSecret,
                    bulkJobId,
                    sponsorId,
                    status,
                    successCount,
                    failedCount
                };

                _logger.LogInformation(
                    "📤 Sending completion notification to WebAPI - Endpoint: {Endpoint}, BulkJobId: {BulkJobId}, Status: {Status}",
                    endpoint, bulkJobId, status);

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
