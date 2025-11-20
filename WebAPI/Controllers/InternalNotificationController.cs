using Business.Services.Notification;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IConfiguration _configuration;
        private readonly string _internalSecret;

        public SignalRNotificationController(
            IPlantAnalysisNotificationService notificationService,
            ILogger<SignalRNotificationController> logger,
            IConfiguration configuration)
        {
            _notificationService = notificationService;
            _logger = logger;
            _configuration = configuration;

            // Use .NET Configuration API (automatically reads Railway env vars with __ pattern)
            _internalSecret = _configuration["WebAPI:InternalSecret"]
                             ?? "ZiraAI_Internal_Secret_2025"; // Fallback for local development

            if (_internalSecret == "ZiraAI_Internal_Secret_2025")
            {
                _logger.LogWarning("⚠️ Using default internal secret - NOT SAFE FOR PRODUCTION!");
            }
            else
            {
                var secretPreview = _internalSecret.Length > 10 
                    ? $"{_internalSecret.Substring(0, 5)}...{_internalSecret.Substring(_internalSecret.Length - 5)}" 
                    : "***";
                _logger.LogInformation("✅ Internal secret loaded - Length: {Length}, Preview: {Preview}", 
                    _internalSecret.Length, secretPreview);
            }
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
                if (request.InternalSecret != _internalSecret)
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
                if (request.InternalSecret != _internalSecret)
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
        /// <summary>
        /// Send bulk invitation progress notification via SignalR
        /// Called by PlantAnalysisWorkerService when processing dealer invitations
        /// </summary>
        [HttpPost("bulk-invitation-progress")]
        public async Task<IActionResult> SendBulkInvitationProgressNotification(
            [FromBody] InternalBulkInvitationProgressRequest request)
        {
            try
            {
                // Validate internal secret
                if (request.InternalSecret != _internalSecret)
                {
                    _logger.LogWarning("⚠️ Invalid internal secret from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { message = "Invalid internal secret" });
                }

                _logger.LogInformation(
                    "📨 Received bulk invitation progress - BulkJobId: {BulkJobId}, Progress: {Progress}%",
                    request.Progress.BulkJobId,
                    request.Progress.ProgressPercentage);

                // Get notification service from DI container
                var bulkNotificationService = HttpContext.RequestServices
                    .GetRequiredService<Business.Services.Notification.IBulkInvitationNotificationService>();

                // Send notification via SignalR
                await bulkNotificationService.NotifyProgressAsync(request.Progress);

                _logger.LogInformation(
                    "✅ Successfully sent progress notification - BulkJobId: {BulkJobId}",
                    request.Progress.BulkJobId);

                return Ok(new { success = true, message = "Progress notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send progress notification - BulkJobId: {BulkJobId}", 
                    request.Progress?.BulkJobId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Send bulk invitation completion notification via SignalR
        /// Called by PlantAnalysisWorkerService when all invitations are processed
        /// </summary>
        [HttpPost("bulk-invitation-completed")]
        public async Task<IActionResult> SendBulkInvitationCompletedNotification(
            [FromBody] InternalBulkInvitationCompletedRequest request)
        {
            try
            {
                // Validate internal secret
                if (request.InternalSecret != _internalSecret)
                {
                    _logger.LogWarning("⚠️ Invalid internal secret from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { message = "Invalid internal secret" });
                }

                _logger.LogInformation(
                    "📨 Received bulk invitation completion - BulkJobId: {BulkJobId}, Status: {Status}",
                    request.BulkJobId,
                    request.Status);

                // Get notification service from DI container
                var bulkNotificationService = HttpContext.RequestServices
                    .GetRequiredService<Business.Services.Notification.IBulkInvitationNotificationService>();

                // Send notification via SignalR
                await bulkNotificationService.NotifyCompletedAsync(
                    request.BulkJobId,
                    request.SponsorId,
                    request.Status,
                    request.SuccessCount,
                    request.FailedCount);

                _logger.LogInformation(
                    "✅ Successfully sent completion notification - BulkJobId: {BulkJobId}",
                    request.BulkJobId);

                return Ok(new { success = true, message = "Completion notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send completion notification - BulkJobId: {BulkJobId}", 
                    request.BulkJobId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Send bulk code distribution progress notification via SignalR
        /// Called by PlantAnalysisWorkerService when processing farmer code distributions
        /// </summary>
        [HttpPost("bulk-code-distribution-progress")]
        public async Task<IActionResult> SendBulkCodeDistributionProgressNotification(
            [FromBody] InternalBulkCodeDistributionProgressRequest request)
        {
            try
            {
                // Validate internal secret
                if (request.InternalSecret != _internalSecret)
                {
                    _logger.LogWarning("⚠️ Invalid internal secret from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { message = "Invalid internal secret" });
                }

                _logger.LogInformation(
                    "📨 Received bulk code distribution progress - JobId: {JobId}, Progress: {Progress}%",
                    request.Progress.JobId,
                    request.Progress.ProgressPercentage);

                // Get notification service from DI container
                var bulkNotificationService = HttpContext.RequestServices
                    .GetRequiredService<Business.Services.Notification.IBulkCodeDistributionNotificationService>();

                // Send notification via SignalR
                await bulkNotificationService.NotifyProgressAsync(request.Progress);

                _logger.LogInformation(
                    "✅ Successfully sent code distribution progress notification - JobId: {JobId}",
                    request.Progress.JobId);

                return Ok(new { success = true, message = "Progress notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send code distribution progress notification - JobId: {JobId}", 
                    request.Progress?.JobId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Send bulk code distribution completion notification via SignalR
        /// Called by PlantAnalysisWorkerService when all code distributions are processed
        /// </summary>
        [HttpPost("bulk-code-distribution-completed")]
        public async Task<IActionResult> SendBulkCodeDistributionCompletedNotification(
            [FromBody] InternalBulkCodeDistributionCompletedRequest request)
        {
            try
            {
                // Validate internal secret
                if (request.InternalSecret != _internalSecret)
                {
                    _logger.LogWarning("⚠️ Invalid internal secret from IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { message = "Invalid internal secret" });
                }

                _logger.LogInformation(
                    "📨 Received bulk code distribution completion - JobId: {JobId}, Status: {Status}",
                    request.JobId,
                    request.Status);

                // Get notification service from DI container
                var bulkNotificationService = HttpContext.RequestServices
                    .GetRequiredService<Business.Services.Notification.IBulkCodeDistributionNotificationService>();

                // Send notification via SignalR
                await bulkNotificationService.NotifyCompletedAsync(
                    request.JobId,
                    request.SponsorId,
                    request.Status,
                    request.SuccessCount,
                    request.FailedCount);

                _logger.LogInformation(
                    "✅ Successfully sent code distribution completion notification - JobId: {JobId}",
                    request.JobId);

                return Ok(new { success = true, message = "Completion notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send code distribution completion notification - JobId: {JobId}", 
                    request.JobId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

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


    /// <summary>
    /// Request model for internal bulk invitation progress notification
    /// </summary>
    public class InternalBulkInvitationProgressRequest
    {
        public string InternalSecret { get; set; }
        public BulkInvitationProgressDto Progress { get; set; }
    }

    /// <summary>
    /// Request model for internal bulk invitation completion notification
    /// </summary>
    public class InternalBulkInvitationCompletedRequest
    {
        public string InternalSecret { get; set; }
        public int BulkJobId { get; set; }
        public int SponsorId { get; set; }
        public string Status { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
    }

    /// <summary>
    /// Request model for internal bulk code distribution progress notification
    /// </summary>
    public class InternalBulkCodeDistributionProgressRequest
    {
        public string InternalSecret { get; set; }
        public BulkCodeDistributionProgressDto Progress { get; set; }
    }

    /// <summary>
    /// Request model for internal bulk code distribution completion notification
    /// </summary>
    public class InternalBulkCodeDistributionCompletedRequest
    {
        public string InternalSecret { get; set; }
        public int JobId { get; set; }
        public int SponsorId { get; set; }
        public string Status { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
    }
}