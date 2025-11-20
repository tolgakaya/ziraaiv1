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
        /// Messaging status information for this analysis (DEPRECATED: Use flat fields below)
        /// </summary>
        [Obsolete("Use flat messaging fields (UnreadMessageCount, TotalMessageCount, etc.) instead")]
        public MessagingStatusDto MessagingStatus { get; set; }

        // ==========================================
        // 🆕 Messaging Fields (Flat Structure)
        // Mobile team preferred flat fields over nested MessagingStatusDto
        // All fields nullable for backward compatibility
        // ==========================================

        /// <summary>
        /// Number of unread messages from farmer (not read by sponsor)
        /// NULL if messaging feature not available or no messages exist
        /// </summary>
        public int? UnreadMessageCount { get; set; }

        /// <summary>
        /// Total number of messages in this conversation (sponsor + farmer)
        /// NULL if messaging feature not available
        /// </summary>
        public int? TotalMessageCount { get; set; }

        /// <summary>
        /// Date/time of the last message sent (by either party)
        /// NULL if no messages exist
        /// </summary>
        public DateTime? LastMessageDate { get; set; }

        /// <summary>
        /// Preview of the last message (max 100 characters)
        /// NULL if no messages exist
        /// </summary>
        public string LastMessagePreview { get; set; }

        /// <summary>
        /// Role of the last message sender: "sponsor" or "farmer"
        /// NULL if no messages exist
        /// </summary>
        public string LastMessageSenderRole { get; set; }

        /// <summary>
        /// Whether there are unread messages from farmer
        /// NULL if messaging feature not available
        /// </summary>
        public bool? HasUnreadFromFarmer { get; set; }

        /// <summary>
        /// Conversation status: "None", "Active" (&lt; 7 days), "Idle" (&gt;= 7 days)
        /// NULL if messaging feature not available
        /// </summary>
        public string ConversationStatus { get; set; }
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

        /// <summary>
        /// Number of analyses that have unread messages
        /// Mobile team requirement for UI filtering
        /// </summary>
        public int AnalysesWithUnread { get; set; }
    }
}
