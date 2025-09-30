using Business.Services.Notification;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Internal SignalR notification controller for cross-service communication
    /// Used by PlantAnalysisWorkerService to send SignalR notifications
    /// </summary>
    [Route("api/internal/signalr")]
    [ApiController]
    [AllowAnonymous] // Internal endpoint, can add IP whitelist or secret key validation
    public class SignalRNotificationController : ControllerBase
    {
        private readonly IPlantAnalysisNotificationService _notificationService;
        private readonly ILogger<SignalRNotificationController> _logger;
        private const string INTERNAL_SECRET = "ZiraAI_Internal_Secret_2025"; // TODO: Move to configuration

        public SignalRNotificationController(
            IPlantAnalysisNotificationService notificationService,
            ILogger<SignalRNotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Send analysis completed notification via SignalR
        /// Called by PlantAnalysisWorkerService when analysis completes
        /// </summary>
        /// <param name="request">Notification request with userId and notification data</param>
        /// <returns>Success or error response</returns>
        [HttpPost("analysis-completed")]
        public async Task<IActionResult> SendAnalysisCompletedNotification(
            [FromBody] InternalNotificationRequest request)
        {
            try
            {
                // Validate internal secret (basic security)
                if (request.InternalSecret != INTERNAL_SECRET)
                {
                    _logger.LogWarning("⚠️ Invalid internal secret from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { message = "Invalid internal secret" });
                }

                _logger.LogInformation(
                    "📨 Received internal notification request - UserId: {UserId}, AnalysisId: {AnalysisId}",
                    request.UserId,
                    request.Notification.AnalysisId);

                // Send notification via SignalR
                await _notificationService.NotifyAnalysisCompleted(request.UserId, request.Notification);

                _logger.LogInformation(
                    "✅ Successfully sent notification - UserId: {UserId}, AnalysisId: {AnalysisId}",
                    request.UserId,
                    request.Notification.AnalysisId);

                return Ok(new { success = true, message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send notification - UserId: {UserId}", request.UserId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Send analysis failed notification via SignalR
        /// </summary>
        [HttpPost("analysis-failed")]
        public async Task<IActionResult> SendAnalysisFailedNotification(
            [FromBody] InternalNotificationFailedRequest request)
        {
            try
            {
                // Validate internal secret
                if (request.InternalSecret != INTERNAL_SECRET)
                {
                    _logger.LogWarning("⚠️ Invalid internal secret from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { message = "Invalid internal secret" });
                }

                _logger.LogInformation(
                    "📨 Received analysis failed notification - UserId: {UserId}, AnalysisId: {AnalysisId}",
                    request.UserId,
                    request.AnalysisId);

                // Send failure notification
                await _notificationService.NotifyAnalysisFailed(request.UserId, request.AnalysisId, request.ErrorMessage);

                return Ok(new { success = true, message = "Failure notification sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send failure notification");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint for Worker Service
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Request model for internal notification
    /// </summary>
    public class InternalNotificationRequest
    {
        public string InternalSecret { get; set; }
        public int UserId { get; set; }
        public PlantAnalysisNotificationDto Notification { get; set; }
    }

    /// <summary>
    /// Request model for internal failure notification
    /// </summary>
    public class InternalNotificationFailedRequest
    {
        public string InternalSecret { get; set; }
        public int UserId { get; set; }
        public int AnalysisId { get; set; }
        public string ErrorMessage { get; set; }
    }
}