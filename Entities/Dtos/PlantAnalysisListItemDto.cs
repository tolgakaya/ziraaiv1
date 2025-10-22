using Core.Entities;
using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace Entities.Dtos
{
    /// <summary>
    /// Lightweight DTO for plant analysis list items (mobile app listing)
    /// Contains essential information for displaying analysis history without full detail
    /// </summary>
    public class PlantAnalysisListItemDto : IDto
    {
        public int Id { get; set; }
        public string AnalysisId { get; set; }
        public string ImagePath { get; set; }
        public string ThumbnailUrl { get; set; } // Optimized thumbnail for list view
        public DateTime AnalysisDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } // "Completed", "Processing", "Failed"
        
        // Basic analysis info
        public string CropType { get; set; }
        public string Location { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        
        // Sponsorship info
        public string FarmerId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string SponsorId { get; set; }
        public int? SponsorUserId { get; set; }
        public int? SponsorshipCodeId { get; set; }
        
        // Summary results (for quick preview)
        public int? OverallHealthScore { get; set; }
        public string PrimaryConcern { get; set; }
        public string Prognosis { get; set; }
        public int? ConfidenceLevel { get; set; }
        
        // Plant identification
        public string PlantSpecies { get; set; }
        public string PlantVariety { get; set; }
        public string GrowthStage { get; set; }
        
        // Processing metadata
        public decimal? TotalTokens { get; set; }
        public decimal? TotalCostTry { get; set; }
        public string AiModel { get; set; }
        
        // ==========================================
        // 🆕 Messaging Fields (Flat Structure)
        // For farmers to see messaging status with their sponsors
        // All fields nullable for backward compatibility
        // ==========================================

        /// <summary>
        /// Number of unread messages from sponsor (not read by farmer)
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
        /// Whether there are unread messages from sponsor
        /// Useful for showing notification badges in farmer's list
        /// NULL if messaging feature not available
        /// </summary>
        public bool? HasUnreadFromSponsor { get; set; }

        /// <summary>
        /// Conversation status: "None", "Active" (&lt; 7 days), "Idle" (&gt;= 7 days)
        /// NULL if messaging feature not available
        /// </summary>
        public string ConversationStatus { get; set; }
        
        // Mobile-friendly properties
        public bool IsSponsored => !string.IsNullOrEmpty(SponsorId);
        public bool HasResults => OverallHealthScore.HasValue;
        public string StatusIcon => Status switch
        {
            "Completed" => "✅",
            "Processing" => "⏳",
            "Failed" => "❌",
            _ => "❓"
        };
        public string HealthScoreText => OverallHealthScore.HasValue ? $"{OverallHealthScore}/10" : "N/A";
        public string FormattedDate => CreatedDate.ToString("dd/MM/yyyy HH:mm");
    }
}