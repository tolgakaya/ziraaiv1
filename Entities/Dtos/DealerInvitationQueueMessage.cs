using System;

namespace Entities.Dtos
{
    /// <summary>
    /// RabbitMQ message for dealer invitation processing
    /// One message per dealer invitation
    /// </summary>
    public class DealerInvitationQueueMessage
    {
        // Tracking
        public string CorrelationId { get; set; }  // BulkInvitationJob.Id
        public int RowNumber { get; set; }          // Excel row number for error reporting

        // Bulk Job Reference
        public int BulkJobId { get; set; }
        public int SponsorId { get; set; }

        // Dealer Information
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }

        // Invitation Configuration
        public string InvitationType { get; set; }  // "Invite" or "AutoCreate"
        public string PackageTier { get; set; }     // S, M, L, XL (nullable)
        public int CodeCount { get; set; }

        // Settings
        public bool SendSms { get; set; }

        // Timestamp
        public DateTime QueuedAt { get; set; }
    }
}
