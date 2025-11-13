using System;

namespace Entities.Dtos
{
    /// <summary>
    /// Individual event in farmer's journey timeline
    /// Represents a significant touchpoint or milestone
    /// </summary>
    public class TimelineEventDto
    {
        /// <summary>
        /// Date and time when event occurred
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Event type: Code Redeemed, First Analysis, Message Sent, High Activity Period,
        /// Decreased Activity, Reengagement, Subscription Renewed, etc.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Human-readable event description
        /// Example: "ZIRA-XL-A1B2C3 activated via WhatsApp"
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Crop type (for analysis events)
        /// </summary>
        public string CropType { get; set; }

        /// <summary>
        /// Disease detected (for analysis events)
        /// </summary>
        public string Disease { get; set; }

        /// <summary>
        /// Severity level: Low, Moderate, High, Critical (for analysis events)
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// Subscription tier at time of event (S, M, L, XL)
        /// </summary>
        public string Tier { get; set; }

        /// <summary>
        /// Communication channel: In-app, WhatsApp, SMS, Email (for message events)
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// What triggered this event: User Action, Automated Campaign, Disease Outbreak, Retention Campaign
        /// </summary>
        public string Trigger { get; set; }

        /// <summary>
        /// Alert level for concerning events: Info, Warning, Critical
        /// Used for decreased activity, churn risk, etc.
        /// </summary>
        public string AlertLevel { get; set; }

        /// <summary>
        /// Associated metadata for extensibility (JSON serialized)
        /// For future custom event properties
        /// </summary>
        public string Metadata { get; set; }
    }
}
