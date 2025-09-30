using Business.Hubs;
using Entities.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    /// <summary>
    /// Implementation of plant analysis notification service using SignalR
    /// Broadcasts real-time notifications to connected clients
    /// </summary>
    public class PlantAnalysisNotificationService : IPlantAnalysisNotificationService
    {
        private readonly IHubContext<PlantAnalysisHub> _hubContext;
        private readonly ILogger<PlantAnalysisNotificationService> _logger;

        public PlantAnalysisNotificationService(
            IHubContext<PlantAnalysisHub> hubContext,
            ILogger<PlantAnalysisNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Notify user that their plant analysis has completed successfully
        /// Sends notification to all connected devices of the user
        /// </summary>
        public async Task NotifyAnalysisCompleted(int userId, PlantAnalysisNotificationDto notification)
        {
            try
            {
                // Send to specific user (all their connected devices)
                // SignalR will use the userId claim from JWT to target the correct connections
                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("AnalysisCompleted", notification);

                _logger.LogInformation(
                    "✅ Sent AnalysisCompleted notification - UserId: {UserId}, AnalysisId: {AnalysisId}, Status: {Status}",
                    userId,
                    notification.AnalysisId,
                    notification.Status);
            }
            catch (Exception ex)
            {
                // Don't throw - notification failure shouldn't break the analysis flow
                // Log error and continue gracefully
                _logger.LogError(
                    ex,
                    "❌ Failed to send AnalysisCompleted notification - UserId: {UserId}, AnalysisId: {AnalysisId}",
                    userId,
                    notification.AnalysisId);
            }
        }

        /// <summary>
        /// Notify user that their plant analysis has failed
        /// </summary>
        public async Task NotifyAnalysisFailed(int userId, int analysisId, string errorMessage)
        {
            try
            {
                var failureNotification = new
                {
                    analysisId,
                    status = "Failed",
                    errorMessage,
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("AnalysisFailed", failureNotification);

                _logger.LogInformation(
                    "⚠️ Sent AnalysisFailed notification - UserId: {UserId}, AnalysisId: {AnalysisId}, Error: {ErrorMessage}",
                    userId,
                    analysisId,
                    errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to send AnalysisFailed notification - UserId: {UserId}, AnalysisId: {AnalysisId}",
                    userId,
                    analysisId);
            }
        }

        /// <summary>
        /// Notify user about analysis progress (future enhancement)
        /// Can be used to show real-time progress updates during analysis
        /// </summary>
        public async Task NotifyAnalysisProgress(int userId, int analysisId, int progressPercentage, string currentStep)
        {
            try
            {
                var progressNotification = new
                {
                    analysisId,
                    progressPercentage,
                    currentStep,
                    timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("AnalysisProgress", progressNotification);

                _logger.LogDebug(
                    "📊 Sent AnalysisProgress notification - UserId: {UserId}, AnalysisId: {AnalysisId}, Progress: {Progress}%",
                    userId,
                    analysisId,
                    progressPercentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Failed to send AnalysisProgress notification - UserId: {UserId}, AnalysisId: {AnalysisId}",
                    userId,
                    analysisId);
            }
        }
    }
}