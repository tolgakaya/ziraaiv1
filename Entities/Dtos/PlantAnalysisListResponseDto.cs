using Core.Entities;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for paginated plant analysis list with pagination metadata
    /// Optimized for mobile app with efficient data transfer
    /// </summary>
    public class PlantAnalysisListResponseDto : IDto
    {
        public List<PlantAnalysisListItemDto> Analyses { get; set; } = new List<PlantAnalysisListItemDto>();
        
        // Pagination metadata
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        
        // Summary statistics
        public int CompletedCount => Analyses?.FindAll(a => a.Status == "Completed")?.Count ?? 0;
        public int ProcessingCount => Analyses?.FindAll(a => a.Status == "Processing")?.Count ?? 0;
        public int FailedCount => Analyses?.FindAll(a => a.Status == "Failed")?.Count ?? 0;
        public int SponsoredCount => Analyses?.FindAll(a => a.IsSponsored)?.Count ?? 0;
        
        // Mobile-friendly pagination info
        public string PaginationInfo => $"Page {Page} of {TotalPages} ({TotalCount} total)";
        public bool IsEmpty => Analyses == null || Analyses.Count == 0;
        public bool IsLastPage => !HasNextPage;
        public bool IsFirstPage => !HasPreviousPage;
    }
}