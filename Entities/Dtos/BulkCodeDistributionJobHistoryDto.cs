using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for bulk code distribution job history with sponsor information
    /// </summary>
    public class BulkCodeDistributionJobHistoryDto
    {
        public int JobId { get; set; }
        public int SponsorId { get; set; }
        public string SponsorName { get; set; }
        public string SponsorEmail { get; set; }
        public int PurchaseId { get; set; }
        public string DeliveryMethod { get; set; }
        public int TotalFarmers { get; set; }
        public int ProcessedFarmers { get; set; }
        public int SuccessfulDistributions { get; set; }
        public int FailedDistributions { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string OriginalFileName { get; set; }
        public int FileSize { get; set; }
        public string ResultFileUrl { get; set; }
        public int TotalCodesDistributed { get; set; }
        public int TotalSmsSent { get; set; }
    }

    /// <summary>
    /// Paginated response for bulk job history
    /// </summary>
    public class BulkCodeDistributionJobHistoryResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public System.Collections.Generic.List<BulkCodeDistributionJobHistoryDto> Jobs { get; set; }
    }
}
