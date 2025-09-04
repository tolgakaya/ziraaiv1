using System;

namespace Business.Services.Notification.Models
{
    /// <summary>
    /// User notification preferences for channel selection and timing
    /// </summary>
    public class NotificationPreferencesDto
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Preferred notification channel
        /// </summary>
        public NotificationChannel PreferredChannel { get; set; } = NotificationChannel.WhatsApp;

        /// <summary>
        /// Enable plant analysis completion notifications
        /// </summary>
        public bool ReceiveAnalysisAlerts { get; set; } = true;

        /// <summary>
        /// Enable subscription usage alerts
        /// </summary>
        public bool ReceiveUsageAlerts { get; set; } = true;

        /// <summary>
        /// Enable subscription expiry warnings
        /// </summary>
        public bool ReceiveExpiryWarnings { get; set; } = true;

        /// <summary>
        /// Enable sponsorship-related notifications
        /// </summary>
        public bool ReceiveSponsorshipAlerts { get; set; } = true;

        /// <summary>
        /// Enable marketing and promotional messages
        /// </summary>
        public bool ReceiveMarketingMessages { get; set; } = false;

        /// <summary>
        /// Enable system maintenance notifications
        /// </summary>
        public bool ReceiveSystemAlerts { get; set; } = true;

        /// <summary>
        /// Preferred language for notifications (tr, en)
        /// </summary>
        public string PreferredLanguage { get; set; } = "tr";

        /// <summary>
        /// Start of quiet hours (no notifications)
        /// </summary>
        public TimeSpan? QuietHoursStart { get; set; }

        /// <summary>
        /// End of quiet hours
        /// </summary>
        public TimeSpan? QuietHoursEnd { get; set; }

        /// <summary>
        /// Allow notifications on weekends
        /// </summary>
        public bool AllowWeekendNotifications { get; set; } = true;

        /// <summary>
        /// Maximum number of notifications per day
        /// </summary>
        public int MaxNotificationsPerDay { get; set; } = 10;

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Last update date
        /// </summary>
        public DateTime? UpdatedDate { get; set; }
    }

    /// <summary>
    /// Notification request with channel preference
    /// </summary>
    public class NotificationRequestDto
    {
        /// <summary>
        /// Target user ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Recipient phone number (international format)
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Recipient full name
        /// </summary>
        public string RecipientName { get; set; }

        /// <summary>
        /// Notification type identifier
        /// </summary>
        public string NotificationType { get; set; }

        /// <summary>
        /// Preferred channel (will use user preference if not specified)
        /// </summary>
        public NotificationChannel? PreferredChannel { get; set; }

        /// <summary>
        /// Template name for template-based notifications
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// Template parameters for dynamic content
        /// </summary>
        public object TemplateParameters { get; set; }

        /// <summary>
        /// Plain text message for simple notifications
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Message priority (low, normal, high, urgent)
        /// </summary>
        public string Priority { get; set; } = "normal";

        /// <summary>
        /// Schedule notification for later delivery
        /// </summary>
        public DateTime? ScheduledFor { get; set; }

        /// <summary>
        /// Maximum retry attempts for failed delivery
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Enable fallback to SMS if WhatsApp fails
        /// </summary>
        public bool EnableFallback { get; set; } = true;
    }

    /// <summary>
    /// Notification delivery result
    /// </summary>
    public class NotificationResultDto
    {
        /// <summary>
        /// Delivery success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message ID from delivery provider
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Channel used for delivery
        /// </summary>
        public NotificationChannel Channel { get; set; }

        /// <summary>
        /// Delivery status message
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Error details if delivery failed
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Timestamp of delivery attempt
        /// </summary>
        public DateTime DeliveryAttemptAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Estimated delivery time
        /// </summary>
        public DateTime? EstimatedDeliveryAt { get; set; }

        /// <summary>
        /// Delivery cost (for analytics)
        /// </summary>
        public decimal? DeliveryCost { get; set; }

        /// <summary>
        /// Whether fallback was used
        /// </summary>
        public bool UsedFallback { get; set; }
    }
}