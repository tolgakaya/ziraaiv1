using Business.Handlers.Sponsorship.Commands;
using Business.Services.Notification;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
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
        private readonly IBulkInvitationNotificationService _notificationService;
        private readonly ILogger<DealerInvitationJobService> _logger;

        public DealerInvitationJobService(
            IMediator mediator,
            IBulkInvitationJobRepository bulkJobRepository,
            IBulkInvitationNotificationService notificationService,
            ILogger<DealerInvitationJobService> logger)
        {
            _mediator = mediator;
            _bulkJobRepository = bulkJobRepository;
            _notificationService = notificationService;
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

                // 2. Update bulk job progress
                var bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);

                if (bulkJob == null)
                {
                    _logger.LogWarning(
                        "[DEALER_INVITATION_JOB_BULK_NOT_FOUND] BulkJob not found - BulkJobId: {BulkJobId}",
                        message.BulkJobId);
                    return;
                }

                // Atomic increment (use lock or transaction in production)
                bulkJob.ProcessedDealers++;

                if (result.Success)
                {
                    bulkJob.SuccessfulInvitations++;
                    _logger.LogInformation(
                        "[DEALER_INVITATION_JOB_SUCCESS] Invitation successful - Email: {Email}, InvitationId: {InvitationId}",
                        message.Email, result.Data?.InvitationId);
                }
                else
                {
                    bulkJob.FailedInvitations++;
                    _logger.LogWarning(
                        "[DEALER_INVITATION_JOB_FAILED] Invitation failed - Email: {Email}, Error: {Error}",
                        message.Email, result.Message);
                }

                // Check if job is complete
                if (bulkJob.ProcessedDealers >= bulkJob.TotalDealers)
                {
                    bulkJob.Status = bulkJob.FailedInvitations == 0
                        ? "Completed"
                        : bulkJob.SuccessfulInvitations > 0
                            ? "PartialSuccess"
                            : "Failed";
                    bulkJob.CompletedDate = DateTime.Now;

                    _logger.LogInformation(
                        "[DEALER_INVITATION_JOB_BULK_COMPLETED] BulkJob completed - BulkJobId: {BulkJobId}, Status: {Status}, Success: {Success}, Failed: {Failed}",
                        message.BulkJobId, bulkJob.Status, bulkJob.SuccessfulInvitations, bulkJob.FailedInvitations);
                }

                _bulkJobRepository.Update(bulkJob);
                await _bulkJobRepository.SaveChangesAsync();

                // 3. Send progress notification via SignalR
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

                await _notificationService.NotifyProgressAsync(progressDto);

                // 4. Send completion notification if done
                if (bulkJob.ProcessedDealers >= bulkJob.TotalDealers)
                {
                    await _notificationService.NotifyCompletedAsync(
                        bulkJob.Id,
                        bulkJob.SponsorId,
                        bulkJob.Status,
                        bulkJob.SuccessfulInvitations,
                        bulkJob.FailedInvitations);
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

                // Update bulk job with failure
                try
                {
                    var bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);
                    if (bulkJob != null)
                    {
                        bulkJob.ProcessedDealers++;
                        bulkJob.FailedInvitations++;
                        _bulkJobRepository.Update(bulkJob);
                        await _bulkJobRepository.SaveChangesAsync();
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "[DEALER_INVITATION_JOB_UPDATE_ERROR] Failed to update bulk job after error");
                }

                throw; // Re-throw for Hangfire retry
            }
        }
    }
}
