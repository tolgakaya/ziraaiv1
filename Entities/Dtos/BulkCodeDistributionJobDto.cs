using System;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for bulk code distribution job creation
    /// Returned to client after Excel upload
    /// </summary>
    public class BulkCodeDistributionJobDto
    {
        public int JobId { get; set; }
        public int TotalFarmers { get; set; }
        public int TotalCodesRequired { get; set; }
        public int AvailableCodes { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string StatusCheckUrl { get; set; }
    }
}
