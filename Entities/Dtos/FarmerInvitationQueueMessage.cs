using System;

namespace Entities.Dtos
{
    /// <summary>
    /// RabbitMQ message for farmer invitation processing
    /// One message per farmer invitation
    /// Pattern: Same as DealerInvitationQueueMessage
    /// </summary>
    public class FarmerInvitationQueueMessage
    {
        // Tracking
        public string CorrelationId { get; set; }  // BulkInvitationJob.Id
        public int RowNumber { get; set; }          // Excel row number for error reporting

        // Bulk Job Reference
        public int BulkJobId { get; set; }
        public int SponsorId { get; set; }

        // Farmer Information
        public string Phone { get; set; }
        public string FarmerName { get; set; }
        public string Email { get; set; }

        // Invitation Configuration
        public string PackageTier { get; set; }     // S, M, L, XL (nullable)
        public string Notes { get; set; }

        // Messaging Settings
        public string Channel { get; set; }         // SMS or WhatsApp
        public string CustomMessage { get; set; }   // Optional custom SMS message

        // NOTE: CodeCount is always 1 for farmer invitations (hardcoded in handler)
        // Not included in DTO to avoid confusion

        // Timestamp
        public DateTime QueuedAt { get; set; }
    }
}
