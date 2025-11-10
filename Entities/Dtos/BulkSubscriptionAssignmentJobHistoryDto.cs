using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for admin bulk subscription assignment job history listing
    /// </summary>
    public class BulkSubscriptionAssignmentJobHistoryDto
    {
        public int JobId { get; set; }
        public string OriginalFileName { get; set; }
        public int FileSizeKB => FileSize / 1024;
        public int FileSize { get; set; }
        public int TotalFarmers { get; set; }
        public int ProcessedFarmers { get; set; }
        public int SuccessfulAssignments { get; set; }
        public int FailedAssignments { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public TimeSpan? ProcessingDuration
        {
            get
            {
                if (CompletedDate.HasValue)
                {
                    return CompletedDate.Value - CreatedDate;
                }
                return null;
            }
        }
    }
}
