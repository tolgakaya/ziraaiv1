using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for bulk code distribution job progress and status updates
    /// Used for status polling and SignalR notifications
    /// </summary>
    public class BulkCodeDistributionProgressDto
    {
        public int JobId { get; set; }
        public int SponsorId { get; set; }
        public string Status { get; set; }

        // Progress Metrics
        public int TotalFarmers { get; set; }
        public int ProcessedFarmers { get; set; }
        public int SuccessfulDistributions { get; set; }
        public int FailedDistributions { get; set; }
        public int ProgressPercentage { get; set; }

        // Statistics
        public int TotalCodesDistributed { get; set; }
        public int TotalSmsSent { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string EstimatedTimeRemaining { get; set; }

        // Results (populated when completed)
        public string ResultFileUrl { get; set; }
        public string ErrorSummary { get; set; }
    }
}
