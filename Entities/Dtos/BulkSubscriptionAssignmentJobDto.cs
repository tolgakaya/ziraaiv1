using System;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for bulk subscription assignment job creation
    /// Returned to client after Excel upload
    /// </summary>
    public class BulkSubscriptionAssignmentJobDto
    {
        public int JobId { get; set; }
        public int TotalFarmers { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string StatusCheckUrl { get; set; }
    }
}
