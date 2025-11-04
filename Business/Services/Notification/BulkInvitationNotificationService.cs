using Business.Hubs;
using Entities.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    public interface IBulkInvitationNotificationService
    {
        Task NotifyProgressAsync(BulkInvitationProgressDto progress);
        Task NotifyCompletedAsync(int bulkJobId, int sponsorId, string status, int successCount, int failedCount);
    }

    public class BulkInvitationNotificationService : IBulkInvitationNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<BulkInvitationNotificationService> _logger;

        public BulkInvitationNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<BulkInvitationNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyProgressAsync(BulkInvitationProgressDto progress)
        {
            try
            {
                var sponsorGroup = $"sponsor_{progress.SponsorId}";

                await _hubContext.Clients.Group(sponsorGroup).SendAsync(
                    "BulkInvitationProgress",
                    progress);

                _logger.LogInformation(
                    "📊 Progress notification sent - SponsorId: {SponsorId}, Progress: {Progress}%",
                    progress.SponsorId, progress.ProgressPercentage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ Failed to send progress notification - SponsorId: {SponsorId}",
                    progress.SponsorId);
            }
        }

        public async Task NotifyCompletedAsync(
            int bulkJobId,
            int sponsorId,
            string status,
            int successCount,
            int failedCount)
        {
            try
            {
                var sponsorGroup = $"sponsor_{sponsorId}";

                var completedData = new
                {
                    BulkJobId = bulkJobId,
                    Status = status,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    CompletedAt = DateTime.Now
                };

                await _hubContext.Clients.Group(sponsorGroup).SendAsync(
                    "BulkInvitationCompleted",
                    completedData);

                _logger.LogInformation(
                    "✅ Completion notification sent - SponsorId: {SponsorId}, Status: {Status}",
                    sponsorId, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ Failed to send completion notification - SponsorId: {SponsorId}",
                    sponsorId);
            }
        }
    }
}
