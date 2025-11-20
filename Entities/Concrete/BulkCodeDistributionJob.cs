using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Tracks bulk farmer code distribution jobs processed through RabbitMQ
    /// Each job represents a single Excel upload with multiple farmer code distributions
    /// </summary>
    public class BulkCodeDistributionJob : IEntity
    {
        public int Id { get; set; }

        // Sponsor Information
        public int SponsorId { get; set; }

        // Configuration
        /// <summary>
        /// Which sponsorship package to use for code selection
        /// </summary>
        public int PurchaseId { get; set; }

        /// <summary>
        /// Whether to send codes via SMS to farmers
        /// </summary>
        public bool SendSms { get; set; }

        /// <summary>
        /// Delivery method: "Direct", "SMS", "Both"
        /// Direct = Mark as distributed only
        /// SMS = Send via SMS only
        /// Both = Mark as distributed AND send via SMS
        /// </summary>
        public string DeliveryMethod { get; set; }

        // Progress Tracking
        public int TotalFarmers { get; set; }
        public int ProcessedFarmers { get; set; }
        public int SuccessfulDistributions { get; set; }
        public int FailedDistributions { get; set; }

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
        /// Format: [{"rowNumber": 12, "phone": "905551234567", "error": "message", "timestamp": "2025-11-05T15:30:00Z"}]
        /// </summary>
        public string ErrorSummary { get; set; }

        // Statistics
        public int TotalCodesDistributed { get; set; }
        public int TotalSmsSent { get; set; }
    }
}
