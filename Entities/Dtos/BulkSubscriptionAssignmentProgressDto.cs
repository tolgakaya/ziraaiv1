using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for tracking bulk subscription assignment job progress
    /// </summary>
    public class BulkSubscriptionAssignmentProgressDto
    {
        public int JobId { get; set; }
        public string Status { get; set; }
        public int TotalFarmers { get; set; }
        public int ProcessedFarmers { get; set; }
        public int SuccessfulAssignments { get; set; }
        public int FailedAssignments { get; set; }
        public int NewSubscriptionsCreated { get; set; }
        public int ExistingSubscriptionsUpdated { get; set; }
        public int TotalNotificationsSent { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string ResultFileUrl { get; set; }
        public decimal ProgressPercentage
        {
            get
            {
                if (TotalFarmers == 0) return 0;
                return Math.Round((decimal)ProcessedFarmers / TotalFarmers * 100, 2);
            }
        }
    }
}
