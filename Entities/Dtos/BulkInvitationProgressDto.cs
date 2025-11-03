using System;

namespace Entities.Dtos
{
    /// <summary>
    /// SignalR notification for bulk invitation progress
    /// Sent to sponsor after each dealer invitation is processed
    /// </summary>
    public class BulkInvitationProgressDto
    {
        public int BulkJobId { get; set; }
        public int SponsorId { get; set; }
        public string Status { get; set; }  // "Processing", "Completed", "PartialSuccess"

        public int TotalDealers { get; set; }
        public int ProcessedDealers { get; set; }
        public int SuccessfulInvitations { get; set; }
        public int FailedInvitations { get; set; }
        public decimal ProgressPercentage { get; set; }

        // Latest processed dealer info
        public string LatestDealerEmail { get; set; }
        public bool LatestDealerSuccess { get; set; }
        public string LatestDealerError { get; set; }

        public DateTime LastUpdateTime { get; set; }
    }
}
