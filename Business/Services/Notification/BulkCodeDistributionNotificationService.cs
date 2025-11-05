using Business.Hubs;
using Entities.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    public interface IBulkCodeDistributionNotificationService
    {
        Task NotifyProgressAsync(BulkCodeDistributionProgressDto progress);
        Task NotifyCompletedAsync(int jobId, int sponsorId, string status, int successCount, int failedCount);
    }

    public class BulkCodeDistributionNotificationService : IBulkCodeDistributionNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<BulkCodeDistributionNotificationService> _logger;

        public BulkCodeDistributionNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<BulkCodeDistributionNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyProgressAsync(BulkCodeDistributionProgressDto progress)
        {
            try
            {
                var sponsorGroup = $"sponsor_{progress.SponsorId}";

                await _hubContext.Clients.Group(sponsorGroup).SendAsync(
                    "BulkCodeDistributionProgress",
                    progress);

                _logger.LogInformation(
                    "📊 Code distribution progress notification sent - SponsorId: {SponsorId}, Progress: {Progress}%",
                    progress.SponsorId, progress.ProgressPercentage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ Failed to send code distribution progress notification - SponsorId: {SponsorId}",
                    progress.SponsorId);
            }
        }

        public async Task NotifyCompletedAsync(
            int jobId,
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
                    JobId = jobId,
                    Status = status,
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    CompletedAt = DateTime.Now
                };

                await _hubContext.Clients.Group(sponsorGroup).SendAsync(
                    "BulkCodeDistributionCompleted",
                    completedData);

                _logger.LogInformation(
                    "✅ Code distribution completion notification sent - SponsorId: {SponsorId}, Status: {Status}",
                    sponsorId, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "⚠️ Failed to send code distribution completion notification - SponsorId: {SponsorId}",
                    sponsorId);
            }
        }
    }
}
