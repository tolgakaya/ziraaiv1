using System;

namespace Entities.Dtos
{
    /// <summary>
    /// Sponsored analysis summary for list view with tier-based field access
    /// Fields are populated based on sponsor's tier level (30%, 60%, 100%)
    /// </summary>
    public class SponsoredAnalysisSummaryDto
    {
        // Core fields (always available)
        public int AnalysisId { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string AnalysisStatus { get; set; }
        public string CropType { get; set; }

        // 30% Access Fields (S & M tiers)
        public decimal? OverallHealthScore { get; set; }
        public string PlantSpecies { get; set; }
        public string PlantVariety { get; set; }
        public string GrowthStage { get; set; }
        public string ImageUrl { get; set; } // Analysis image URL for display in list

        // 60% Access Fields (L tier)
        public decimal? VigorScore { get; set; }
        public string HealthSeverity { get; set; }
        public string PrimaryConcern { get; set; }
        public string Location { get; set; }
        // Note: Recommendations removed from list view (too large, use detail endpoint instead)

        // 100% Access Fields (XL tier)
        public string FarmerName { get; set; }
        public string FarmerPhone { get; set; }
        public string FarmerEmail { get; set; }

        // Tier & Permission Info
        public string TierName { get; set; }
        public int AccessPercentage { get; set; }
        public bool CanMessage { get; set; }
        public bool CanViewLogo { get; set; }

        // Sponsor Display Info
        public SponsorDisplayInfoDto SponsorInfo { get; set; }

        /// <summary>
        /// Messaging status information for this analysis
        /// </summary>
        public MessagingStatusDto MessagingStatus { get; set; }
    }

    /// <summary>
    /// Sponsor display information for logo and branding
    /// </summary>
    public class SponsorDisplayInfoDto
    {
        public int SponsorId { get; set; }
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
    }

    /// <summary>
    /// Paginated response for sponsored analyses list
    /// </summary>
    public class SponsoredAnalysesListResponseDto
    {
        public SponsoredAnalysisSummaryDto[] Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        // Summary statistics
        public SponsoredAnalysesListSummaryDto Summary { get; set; }
    }

    /// <summary>
    /// Summary statistics for the filtered list
    /// </summary>
    public class SponsoredAnalysesListSummaryDto
    {
        public int TotalAnalyses { get; set; }
        public decimal AverageHealthScore { get; set; }
        public string[] TopCropTypes { get; set; }
        public int AnalysesThisMonth { get; set; }

        /// <summary>
        /// Number of analyses where sponsor has sent at least one message
        /// </summary>
        public int ContactedAnalyses { get; set; }

        /// <summary>
        /// Number of analyses where sponsor has not sent any messages
        /// </summary>
        public int NotContactedAnalyses { get; set; }

        /// <summary>
        /// Number of active conversations (two-way, recent activity)
        /// </summary>
        public int ActiveConversations { get; set; }

        /// <summary>
        /// Number of conversations waiting for farmer response
        /// </summary>
        public int PendingResponses { get; set; }

        /// <summary>
        /// Total unread messages across all conversations
        /// </summary>
        public int TotalUnreadMessages { get; set; }
    }
}
