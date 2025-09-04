using Business.Services.Notification;
using Business.Services.Notification.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class NotificationController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        #region WhatsApp Notifications

        /// <summary>
        /// Send WhatsApp template message
        /// </summary>
        /// <param name="request">WhatsApp template request</param>
        /// <returns>Send result</returns>
        [HttpPost("whatsapp/send-template")]
        [Authorize(Roles = "Admin,Sponsor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendWhatsAppTemplate([FromBody] SendWhatsAppTemplateRequest request)
        {
            try
            {
                var result = await _notificationService.SendTemplateNotificationAsync(
                    request.UserId,
                    request.PhoneNumber,
                    request.TemplateName,
                    request.Parameters ?? new Dictionary<string, object>(),
                    NotificationChannel.WhatsApp);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Send bulk WhatsApp template messages
        /// </summary>
        /// <param name="request">Bulk WhatsApp request</param>
        /// <returns>Bulk send results</returns>
        [HttpPost("whatsapp/send-bulk")]
        [Authorize(Roles = "Admin,Sponsor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendBulkWhatsAppTemplates([FromBody] SendBulkWhatsAppTemplateRequest request)
        {
            try
            {
                var recipients = request.Recipients.Select(r => new BulkNotificationRecipientDto
                {
                    UserId = r.UserId,
                    PhoneNumber = r.PhoneNumber,
                    Name = r.Name,
                    Parameters = r.Parameters ?? new Dictionary<string, object>()
                }).ToList();

                var result = await _notificationService.SendBulkTemplateNotificationsAsync(
                    recipients,
                    request.TemplateName,
                    NotificationChannel.WhatsApp);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Send test WhatsApp message
        /// </summary>
        /// <param name="request">Test message request</param>
        /// <returns>Test result</returns>
        [HttpPost("whatsapp/test")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendTestWhatsApp([FromBody] SendTestNotificationRequest request)
        {
            try
            {
                var result = await _notificationService.SendTestNotificationAsync(
                    request.PhoneNumber,
                    NotificationChannel.WhatsApp,
                    request.TestMessage);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Plant Analysis Notifications

        /// <summary>
        /// Send notification about completed plant analysis
        /// </summary>
        /// <param name="request">Notification request</param>
        /// <returns>Notification status</returns>
        [HttpPost("plant-analysis-completed")]
        [Authorize(Roles = "Admin,System")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> NotifyAnalysisCompleted([FromBody] AnalysisCompletedNotificationDto request)
        {
            try
            {
                var result = await _notificationService.SendAnalysisCompletedNotificationAsync(
                    int.Parse(request.FarmerId),
                    request.ContactPhone,
                    int.Parse(request.AnalysisId),
                    request.CropType,
                    request.HealthScore,
                    request.PrimaryConcern,
                    $"https://ziraai.com/dashboard/analysis/{request.AnalysisId}");

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Failed to send notification: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Send urgent plant health alert
        /// </summary>
        /// <param name="request">Urgent alert request</param>
        /// <returns>Alert result</returns>
        [HttpPost("urgent-health-alert")]
        [Authorize(Roles = "Admin,System")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendUrgentHealthAlert([FromBody] UrgentHealthAlertRequest request)
        {
            try
            {
                var result = await _notificationService.SendUrgentHealthAlertNotificationAsync(
                    request.UserId,
                    request.PhoneNumber,
                    request.CropType,
                    request.UrgentIssue,
                    request.RecommendedAction);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Subscription Notifications

        /// <summary>
        /// Send subscription usage alert
        /// </summary>
        /// <param name="request">Usage alert request</param>
        /// <returns>Alert result</returns>
        [HttpPost("usage-alert")]
        [Authorize(Roles = "Admin,System")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendUsageAlert([FromBody] UsageAlertRequest request)
        {
            try
            {
                var result = await _notificationService.SendUsageAlertNotificationAsync(
                    request.UserId,
                    request.PhoneNumber,
                    request.UsagePercentage,
                    request.LimitType,
                    request.ResetDate);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Management & Monitoring

        /// <summary>
        /// Get notification health status
        /// </summary>
        /// <returns>Channel health status</returns>
        [HttpGet("health")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationHealth()
        {
            try
            {
                var result = await _notificationService.CheckChannelHealthAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get user notification preferences
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User preferences</returns>
        [HttpGet("preferences/{userId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserPreferences(int userId)
        {
            try
            {
                // Check if user can access this data
                var currentUserId = GetUserId();
                if (currentUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid("You can only access your own preferences");
                }

                var result = await _notificationService.GetUserNotificationPreferencesAsync(userId);
                return result.Success ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update user notification preferences
        /// </summary>
        /// <param name="preferences">Updated preferences</param>
        /// <returns>Update result</returns>
        [HttpPut("preferences")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUserPreferences([FromBody] NotificationPreferencesDto preferences)
        {
            try
            {
                // Check if user can update this data
                var currentUserId = GetUserId();
                if (currentUserId != preferences.UserId && !User.IsInRole("Admin"))
                {
                    return Forbid("You can only update your own preferences");
                }

                var result = await _notificationService.UpdateUserNotificationPreferencesAsync(preferences);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region WhatsApp Webhook

        /// <summary>
        /// WhatsApp webhook for delivery status updates
        /// </summary>
        /// <param name="webhookData">Webhook payload from WhatsApp</param>
        /// <returns>Processing result</returns>
        [HttpPost("/api/v{version:apiVersion}/webhooks/whatsapp")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> WhatsAppWebhook([FromBody] object webhookData)
        {
            try
            {
                // TODO: Implement webhook processing
                // This would process delivery status updates, read receipts, etc.
                
                return Ok(new { success = true, message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// WhatsApp webhook verification (GET request from WhatsApp)
        /// </summary>
        /// <param name="hub_mode">Webhook mode</param>
        /// <param name="hub_verify_token">Verification token</param>
        /// <param name="hub_challenge">Challenge string</param>
        /// <returns>Challenge string if verified</returns>
        [HttpGet("/api/v{version:apiVersion}/webhooks/whatsapp")]
        [AllowAnonymous]
        public IActionResult VerifyWhatsAppWebhook(
            [FromQuery(Name = "hub.mode")] string hub_mode,
            [FromQuery(Name = "hub.verify_token")] string hub_verify_token,
            [FromQuery(Name = "hub.challenge")] string hub_challenge)
        {
            var verifyToken = "ziraai_webhook_verification_token"; // Should come from configuration
            
            if (hub_mode == "subscribe" && hub_verify_token == verifyToken)
            {
                return Ok(hub_challenge);
            }
            
            return BadRequest("Webhook verification failed");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get current user ID from JWT claims
        /// </summary>
        /// <returns>User ID if found, null otherwise</returns>
        private int? GetUserId()
        {
            var userIdClaim = User?.FindFirst("UserId")?.Value ??
                              User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        #endregion
    }

    #region Request DTOs

    public class SendWhatsAppTemplateRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        public string TemplateName { get; set; }
        
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class SendBulkWhatsAppTemplateRequest
    {
        [Required]
        public string TemplateName { get; set; }
        
        [Required]
        public List<BulkRecipientRequest> Recipients { get; set; } = new();
    }

    public class BulkRecipientRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
        
        public string Name { get; set; }
        
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class SendTestNotificationRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        public string TestMessage { get; set; }
    }

    public class AnalysisCompletedNotificationDto
    {
        public string AnalysisId { get; set; }
        public string FarmerId { get; set; }
        public string CropType { get; set; }
        public int HealthScore { get; set; }
        public string PrimaryConcern { get; set; }
        public string UrgencyLevel { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public class UrgentHealthAlertRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        public string CropType { get; set; }
        
        [Required]
        public string UrgentIssue { get; set; }
        
        [Required]
        public string RecommendedAction { get; set; }
    }

    public class UsageAlertRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        public int UsagePercentage { get; set; }
        
        [Required]
        public string LimitType { get; set; }
        
        [Required]
        public string ResetDate { get; set; }
    }

    #endregion
}