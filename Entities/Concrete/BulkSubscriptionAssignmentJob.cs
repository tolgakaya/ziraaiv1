using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Tracks bulk farmer subscription assignment jobs processed through RabbitMQ
    /// Each job represents a single Excel upload with multiple farmer subscription assignments
    /// Admins can assign subscriptions directly to farmers without sponsorship codes
    /// </summary>
    public class BulkSubscriptionAssignmentJob : IEntity
    {
        public int Id { get; set; }

        // Admin Information
        public int AdminId { get; set; }

        // Configuration - Optional Defaults
        /// <summary>
        /// Optional default subscription tier ID to use when not specified in Excel
        /// </summary>
        public int? DefaultTierId { get; set; }

        /// <summary>
        /// Optional default subscription duration in days when not specified in Excel
        /// </summary>
        public int? DefaultDurationDays { get; set; }

        /// <summary>
        /// Whether to send notifications (SMS/Email) to farmers about subscription activation
        /// </summary>
        public bool SendNotification { get; set; }

        /// <summary>
        /// Notification method: "SMS", "Email", "Both"
        /// SMS = Send via SMS only
        /// Email = Send via Email only
        /// Both = Send via both SMS and Email
        /// </summary>
        public string NotificationMethod { get; set; }

        /// <summary>
        /// Whether to automatically activate subscriptions immediately
        /// If false, subscriptions are created but not activated (admin must activate manually)
        /// </summary>
        public bool AutoActivate { get; set; }

        // Progress Tracking
        public int TotalFarmers { get; set; }
        public int ProcessedFarmers { get; set; }
        public int SuccessfulAssignments { get; set; }
        public int FailedAssignments { get; set; }

        // Status: Pending, Processing, Completed, PartialSuccess, Failed
        public string Status { get; set; } = "Pending";

        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        // File Information
        public string OriginalFileName { get; set; }
        public int FileSize { get; set; } // in bytes

        // Results
        /// <summary>
        /// URL to download result file (Excel with success/error status per row)
        /// Generated after job completion
        /// </summary>
        public string ResultFileUrl { get; set; }

        /// <summary>
        /// JSON array of error details per failed farmer
        /// Format: [{"rowNumber": 12, "email": "farmer@example.com", "error": "message", "timestamp": "2025-11-05T15:30:00Z"}]
        /// </summary>
        public string ErrorSummary { get; set; }

        // Statistics
        public int NewSubscriptionsCreated { get; set; }
        public int ExistingSubscriptionsUpdated { get; set; }
        public int TotalNotificationsSent { get; set; }
    }
}
