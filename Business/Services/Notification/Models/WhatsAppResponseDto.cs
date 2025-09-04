using System.Collections.Generic;

namespace Business.Services.Notification.Models
{
    /// <summary>
    /// WhatsApp API response structure
    /// </summary>
    public class WhatsAppResponseDto
    {
        /// <summary>
        /// Messaging product (always "whatsapp")
        /// </summary>
        public string MessagingProduct { get; set; }

        /// <summary>
        /// List of message status information
        /// </summary>
        public List<WhatsAppMessageStatusDto> Messages { get; set; } = new();

        /// <summary>
        /// List of contacts information
        /// </summary>
        public List<WhatsAppContactDto> Contacts { get; set; } = new();

        /// <summary>
        /// Error information if request failed
        /// </summary>
        public WhatsAppErrorDto Error { get; set; }
    }

    /// <summary>
    /// WhatsApp message status information
    /// </summary>
    public class WhatsAppMessageStatusDto
    {
        /// <summary>
        /// Message ID assigned by WhatsApp
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Message status (sent, delivered, read, failed)
        /// </summary>
        public string MessageStatus { get; set; }
    }

    /// <summary>
    /// WhatsApp contact information
    /// </summary>
    public class WhatsAppContactDto
    {
        /// <summary>
        /// Contact's WhatsApp ID
        /// </summary>
        public string WaId { get; set; }

        /// <summary>
        /// Contact's phone number input
        /// </summary>
        public string Input { get; set; }
    }

    /// <summary>
    /// WhatsApp API error information
    /// </summary>
    public class WhatsAppErrorDto
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Error type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Error code
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Error details
        /// </summary>
        public object ErrorData { get; set; }

        /// <summary>
        /// Facebook trace ID for debugging
        /// </summary>
        public string FbTraceId { get; set; }
    }

    /// <summary>
    /// Webhook delivery status update from WhatsApp
    /// </summary>
    public class WhatsAppWebhookDto
    {
        /// <summary>
        /// Webhook object type
        /// </summary>
        public string Object { get; set; }

        /// <summary>
        /// Entry list containing the status updates
        /// </summary>
        public List<WhatsAppWebhookEntryDto> Entry { get; set; } = new();
    }

    /// <summary>
    /// WhatsApp webhook entry containing status changes
    /// </summary>
    public class WhatsAppWebhookEntryDto
    {
        /// <summary>
        /// WhatsApp Business Account ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Changes list containing status updates
        /// </summary>
        public List<WhatsAppWebhookChangeDto> Changes { get; set; } = new();
    }

    /// <summary>
    /// WhatsApp webhook change containing message status
    /// </summary>
    public class WhatsAppWebhookChangeDto
    {
        /// <summary>
        /// Change field (usually "messages")
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Value containing the actual status data
        /// </summary>
        public WhatsAppWebhookValueDto Value { get; set; }
    }

    /// <summary>
    /// WhatsApp webhook value containing status information
    /// </summary>
    public class WhatsAppWebhookValueDto
    {
        /// <summary>
        /// Messaging product (always "whatsapp")
        /// </summary>
        public string MessagingProduct { get; set; }

        /// <summary>
        /// Metadata about the phone number
        /// </summary>
        public WhatsAppMetadataDto Metadata { get; set; }

        /// <summary>
        /// List of message status updates
        /// </summary>
        public List<WhatsAppWebhookStatusDto> Statuses { get; set; } = new();

        /// <summary>
        /// List of incoming messages (for replies)
        /// </summary>
        public List<WhatsAppIncomingMessageDto> Messages { get; set; } = new();
    }

    /// <summary>
    /// WhatsApp webhook metadata
    /// </summary>
    public class WhatsAppMetadataDto
    {
        /// <summary>
        /// Display phone number
        /// </summary>
        public string DisplayPhoneNumber { get; set; }

        /// <summary>
        /// Phone number ID
        /// </summary>
        public string PhoneNumberId { get; set; }
    }

    /// <summary>
    /// WhatsApp message status update from webhook
    /// </summary>
    public class WhatsAppWebhookStatusDto
    {
        /// <summary>
        /// Message ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Status (sent, delivered, read, failed)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Timestamp of status update
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Recipient WhatsApp ID
        /// </summary>
        public string RecipientId { get; set; }

        /// <summary>
        /// Error information if status is failed
        /// </summary>
        public WhatsAppWebhookErrorDto Error { get; set; }
    }

    /// <summary>
    /// WhatsApp webhook error information
    /// </summary>
    public class WhatsAppWebhookErrorDto
    {
        /// <summary>
        /// Error code
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Error title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Error message details
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Error details
        /// </summary>
        public string ErrorData { get; set; }
    }

    /// <summary>
    /// Incoming WhatsApp message from webhook
    /// </summary>
    public class WhatsAppIncomingMessageDto
    {
        /// <summary>
        /// Message ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Sender WhatsApp ID
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Message timestamp
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Message type (text, image, etc.)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Text content for text messages
        /// </summary>
        public WhatsAppTextDto Text { get; set; }
    }
}