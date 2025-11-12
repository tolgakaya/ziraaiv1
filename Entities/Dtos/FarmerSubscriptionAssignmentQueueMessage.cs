using System;

namespace Entities.Dtos
{
    /// <summary>
    /// RabbitMQ message for farmer subscription assignment processing
    /// One message per farmer subscription assignment
    /// </summary>
    public class FarmerSubscriptionAssignmentQueueMessage
    {
        // Tracking
        public string CorrelationId { get; set; }  // BulkSubscriptionAssignmentJob.Id
        public int RowNumber { get; set; }          // Excel row number for error reporting

        // Bulk Job Reference
        public int BulkJobId { get; set; }
        public int AdminId { get; set; }

        // Farmer Information
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Subscription Configuration
        public int SubscriptionTierId { get; set; }
        public int DurationDays { get; set; }

        // Settings
        public bool SendNotification { get; set; }
        public string NotificationMethod { get; set; }
        public bool AutoActivate { get; set; }

        // Optional
        public string Notes { get; set; }

        // Timestamp
        public DateTime QueuedAt { get; set; }
    }
}
