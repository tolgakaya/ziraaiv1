using Business.Handlers.Sponsorship.Commands;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PlantAnalysisWorkerService.Jobs
{
    public interface IFarmerInvitationJobService
    {
        Task ProcessFarmerInvitationAsync(FarmerInvitationQueueMessage message, string correlationId);
    }

    public class FarmerInvitationJobService : IFarmerInvitationJobService
    {
        private readonly IMediator _mediator;
        private readonly IBulkInvitationJobRepository _bulkJobRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FarmerInvitationJobService> _logger;

        public FarmerInvitationJobService(
            IMediator mediator,
            IBulkInvitationJobRepository bulkJobRepository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<FarmerInvitationJobService> logger)
        {
            _mediator = mediator;
            _bulkJobRepository = bulkJobRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ProcessFarmerInvitationAsync(FarmerInvitationQueueMessage message, string correlationId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "[FARMER_INVITATION_JOB_START] Processing farmer invitation - BulkJobId: {BulkJobId}, Row: {RowNumber}, Phone: {Phone}, Channel: {Channel}",
                message.BulkJobId, message.RowNumber, message.Phone, message.Channel);

            try
            {
                // 1. Get bulk job
                var bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);
                if (bulkJob == null)
                {
                    _logger.LogWarning(
                        "[FARMER_INVITATION_JOB_BULK_NOT_FOUND] BulkJob not found - BulkJobId: {BulkJobId}",
                        message.BulkJobId);
                    return;
                }

                // CRITICAL: Validate InvitationType to prevent processing dealer jobs
                if (bulkJob.InvitationType != "FarmerInvite")
                {
                    _logger.LogError(
                        "[FARMER_INVITATION_JOB_TYPE_MISMATCH] InvitationType mismatch - BulkJobId: {BulkJobId}, Expected: FarmerInvite, Actual: {ActualType}",
                        message.BulkJobId, bulkJob.InvitationType);
                    return;
                }

                // 2. Send invitation via CreateFarmerInvitationCommand
                // NOTE: Channel and CustomMessage are bulk-only features not supported by CreateFarmerInvitationCommand
                // The command always uses SMS with default template
                var command = new CreateFarmerInvitationCommand
                {
                    SponsorId = message.SponsorId,
                    Phone = message.Phone,
                    FarmerName = message.FarmerName,
                    Email = message.Email,
                    CodeCount = 1,  // Always 1 for farmer invitations
                    PackageTier = message.PackageTier,
                    Notes = message.Notes
                };

                _logger.LogInformation(
                    "[FARMER_INVITATION_SENDING] Sending invitation - Phone: {Phone}, Channel: {Channel}",
                    message.Phone, message.Channel);

                var result = await _mediator.Send(command);

                // 3. Atomically update bulk job progress (prevents race conditions)
                bulkJob = await _bulkJobRepository.IncrementProgressAsync(message.BulkJobId, result.Success);

                if (bulkJob == null)
                {
                    _logger.LogWarning(
                        "[FARMER_INVITATION_JOB_BULK_NOT_FOUND] BulkJob not found after increment - BulkJobId: {BulkJobId}",
                        message.BulkJobId);
                    return;
                }

                // Log success/failure
                if (result.Success)
                {
                    _logger.LogInformation(
                        "[FARMER_INVITATION_JOB_SUCCESS] Invitation successful - Phone: {Phone}, InvitationToken: {Token}",
                        message.Phone, result.Data?.InvitationToken);
                }
                else
                {
                    _logger.LogWarning(
                        "[FARMER_INVITATION_JOB_FAILED] Invitation failed - Phone: {Phone}, Error: {Error}",
                        message.Phone, result.Message);
                }

                // 4. Check if all invitations are complete and mark as done
                bool isComplete = await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);

                if (isComplete)
                {
                    // Reload to get updated status
                    bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);

                    _logger.LogInformation(
                        "[FARMER_INVITATION_JOB_BULK_COMPLETED] BulkJob completed - BulkJobId: {BulkJobId}, Status: {Status}, Success: {Success}, Failed: {Failed}",
                        message.BulkJobId, bulkJob.Status, bulkJob.SuccessfulInvitations, bulkJob.FailedInvitations);
                }

                // 5. Send progress notification via HTTP to WebAPI (cross-process communication)
                var progressDto = new BulkInvitationProgressDto
                {
                    BulkJobId = bulkJob.Id,
                    SponsorId = bulkJob.SponsorId,
                    Status = bulkJob.Status,
                    TotalDealers = bulkJob.TotalDealers,  // Note: Using TotalDealers for backward compatibility (represents total farmers for this job type)
                    ProcessedDealers = bulkJob.ProcessedDealers,  // Note: Represents processed farmers
                    SuccessfulInvitations = bulkJob.SuccessfulInvitations,
                    FailedInvitations = bulkJob.FailedInvitations,
                    ProgressPercentage = Math.Round((decimal)bulkJob.ProcessedDealers / bulkJob.TotalDealers * 100, 2),
                    LatestDealerEmail = message.Phone,  // Note: Using Email field for phone (backward compatible DTO)
                    LatestDealerSuccess = result.Success,
                    LatestDealerError = result.Success ? null : result.Message,
                    LastUpdateTime = DateTime.Now
                };

                await SendProgressNotificationViaHttp(progressDto);

                // 6. Send completion notification if job is done
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
                    "[FARMER_INVITATION_JOB_COMPLETED] Processing completed - Duration: {Duration}ms, Success: {Success}",
                    stopwatch.ElapsedMilliseconds, result.Success);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[FARMER_INVITATION_JOB_ERROR] Error processing invitation - BulkJobId: {BulkJobId}, Phone: {Phone}, Duration: {Duration}ms",
                    message.BulkJobId, message.Phone, stopwatch.ElapsedMilliseconds);

                // Update bulk job with failure using atomic operations
                try
                {
                    await _bulkJobRepository.IncrementProgressAsync(message.BulkJobId, success: false);
                    await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "[FARMER_INVITATION_JOB_UPDATE_ERROR] Failed to update bulk job after error");
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
