using Business.Services.Notification.Models;
using Core.Utilities.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    /// <summary>
    /// WhatsApp Business API service interface for sending messages and managing templates
    /// </summary>
    public interface IWhatsAppService
    {
        /// <summary>
        /// Send a template message via WhatsApp Business API
        /// </summary>
        /// <param name="phoneNumber">Recipient phone number in international format (e.g., 905551234567)</param>
        /// <param name="templateName">Name of the approved WhatsApp template</param>
        /// <param name="templateParameters">Dynamic parameters for the template</param>
        /// <param name="languageCode">Template language code (default: tr)</param>
        /// <returns>Result containing message ID if successful</returns>
        Task<IDataResult<string>> SendTemplateMessageAsync(
            string phoneNumber, 
            string templateName, 
            Dictionary<string, object> templateParameters, 
            string languageCode = "tr");

        /// <summary>
        /// Send a simple text message via WhatsApp Business API
        /// </summary>
        /// <param name="phoneNumber">Recipient phone number in international format</param>
        /// <param name="message">Message text (max 4096 characters)</param>
        /// <param name="previewUrl">Enable URL preview in message</param>
        /// <returns>Result containing message ID if successful</returns>
        Task<IDataResult<string>> SendTextMessageAsync(
            string phoneNumber, 
            string message, 
            bool previewUrl = true);

        /// <summary>
        /// Send bulk template messages to multiple recipients
        /// </summary>
        /// <param name="recipients">List of recipients with phone numbers and parameters</param>
        /// <param name="templateName">Template name to use for all messages</param>
        /// <param name="languageCode">Template language code</param>
        /// <returns>Result containing list of delivery results</returns>
        Task<IDataResult<List<NotificationResultDto>>> SendBulkTemplateMessagesAsync(
            List<BulkWhatsAppRecipientDto> recipients,
            string templateName,
            string languageCode = "tr");

        /// <summary>
        /// Get message delivery status by message ID
        /// </summary>
        /// <param name="messageId">WhatsApp message ID</param>
        /// <returns>Current delivery status (sent, delivered, read, failed)</returns>
        Task<IDataResult<string>> GetMessageStatusAsync(string messageId);

        /// <summary>
        /// Get list of approved WhatsApp templates
        /// </summary>
        /// <returns>List of available templates with their parameters</returns>
        Task<IDataResult<List<WhatsAppTemplateInfoDto>>> GetApprovedTemplatesAsync();

        /// <summary>
        /// Validate phone number format for WhatsApp
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>Validation result with normalized number</returns>
        IDataResult<string> ValidatePhoneNumber(string phoneNumber);

        /// <summary>
        /// Check WhatsApp Business API health and connectivity
        /// </summary>
        /// <returns>Health check result</returns>
        Task<IResult> HealthCheckAsync();

        /// <summary>
        /// Process incoming webhook from WhatsApp (delivery status updates)
        /// </summary>
        /// <param name="webhookData">Webhook payload from WhatsApp</param>
        /// <returns>Processing result</returns>
        Task<IResult> ProcessWebhookAsync(WhatsAppWebhookDto webhookData);

        /// <summary>
        /// Mark message as read (for analytics)
        /// </summary>
        /// <param name="messageId">WhatsApp message ID</param>
        /// <returns>Success result</returns>
        Task<IResult> MarkMessageAsReadAsync(string messageId);

        /// <summary>
        /// Get media URL for media messages (future feature)
        /// </summary>
        /// <param name="mediaId">WhatsApp media ID</param>
        /// <returns>Media download URL</returns>
        Task<IDataResult<string>> GetMediaUrlAsync(string mediaId);
    }

    /// <summary>
    /// Bulk WhatsApp message recipient information
    /// </summary>
    public class BulkWhatsAppRecipientDto
    {
        /// <summary>
        /// Recipient phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Recipient name for personalization
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Template parameters specific to this recipient
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// User ID for tracking purposes
        /// </summary>
        public int? UserId { get; set; }
    }

    /// <summary>
    /// WhatsApp template information
    /// </summary>
    public class WhatsAppTemplateInfoDto
    {
        /// <summary>
        /// Template name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Template language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Template status (APPROVED, PENDING, REJECTED)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Template category (MARKETING, UTILITY, AUTHENTICATION)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Template components description
        /// </summary>
        public List<string> Components { get; set; } = new();

        /// <summary>
        /// Required parameters for this template
        /// </summary>
        public List<string> RequiredParameters { get; set; } = new();
    }
}