using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Tracks bulk dealer invitation jobs processed through RabbitMQ
    /// Each job represents a single Excel upload with multiple dealer invitations
    /// </summary>
    public class BulkInvitationJob : IEntity
    {
        public int Id { get; set; }
        
        // Sponsor Information
        public int SponsorId { get; set; }
        
        // Invitation Configuration
        /// <summary>
        /// Invite: Email/SMS invitation with registration link
        /// AutoCreate: Automatic sponsor account creation with generated password
        /// </summary>
        public string InvitationType { get; set; }
        
        /// <summary>
        /// Default tier for code selection: S, M, L, XL
        /// Can be overridden per row in Excel
        /// </summary>
        public string DefaultTier { get; set; }
        
        public int DefaultCodeCount { get; set; } // Default code count per dealer
        public bool SendSms { get; set; } // Whether to send SMS notifications
        
        // Progress Tracking
        public int TotalDealers { get; set; }
        public int ProcessedDealers { get; set; }
        public int SuccessfulInvitations { get; set; }
        public int FailedInvitations { get; set; }
        
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
        /// JSON array of error details per failed dealer
        /// Format: [{"rowNumber": 12, "email": "test@email.com", "error": "message", "timestamp": "2025-11-03T15:30:00Z"}]
        /// </summary>
        public string ErrorSummary { get; set; }
    }
}
