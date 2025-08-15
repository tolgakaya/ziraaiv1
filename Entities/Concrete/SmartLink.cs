using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class SmartLink : IEntity
    {
        public int Id { get; set; }
        
        // Link Owner
        public int SponsorId { get; set; } // Sponsor who owns this link
        public string SponsorName { get; set; } // Cached for display
        
        // Link Information
        public string LinkUrl { get; set; } // Target URL
        public string LinkText { get; set; } // Display text for the link
        public string LinkDescription { get; set; } // Detailed description
        public string LinkType { get; set; } // Product, Service, Information, Contact
        
        // Targeting and Matching
        public string Keywords { get; set; } // JSON array of keywords for matching
        public string ProductCategory { get; set; } // Fertilizer, Pesticide, Equipment, Service
        public string TargetCropTypes { get; set; } // JSON array of crop types
        public string TargetDiseases { get; set; } // JSON array of disease names
        public string TargetPests { get; set; } // JSON array of pest names
        public string TargetNutrientDeficiencies { get; set; } // JSON array of nutrients
        public string TargetGrowthStages { get; set; } // JSON array of growth stages
        public string TargetRegions { get; set; } // JSON array of regions/cities
        
        // Display Settings
        public int Priority { get; set; } // Display priority (1-100, higher = more priority)
        public string DisplayPosition { get; set; } // Top, Bottom, Inline, Sidebar
        public string DisplayStyle { get; set; } // Button, Text, Card, Banner
        public string IconUrl { get; set; } // Optional icon for the link
        public string BackgroundColor { get; set; } // Hex color code
        public string TextColor { get; set; } // Hex color code
        public bool IsBold { get; set; }
        public bool IsHighlighted { get; set; }
        
        // Product Information
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal? ProductPrice { get; set; }
        public string ProductCurrency { get; set; }
        public string ProductUnit { get; set; } // kg, L, piece
        public bool IsPromotional { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? PromotionStartDate { get; set; }
        public DateTime? PromotionEndDate { get; set; }
        
        // Analytics and Tracking
        public int ClickCount { get; set; }
        public int UniqueClickCount { get; set; }
        public DateTime? LastClickDate { get; set; }
        public int DisplayCount { get; set; }
        public decimal ClickThroughRate { get; set; } // CTR percentage
        public int ConversionCount { get; set; } // Number of purchases/actions
        public decimal ConversionRate { get; set; } // Conversion percentage
        public string ClickHistory { get; set; } // JSON array of click events
        
        // A/B Testing
        public string TestVariant { get; set; } // A, B, C for testing different versions
        public int TestGroupSize { get; set; } // Number of users in test group
        public decimal TestPerformanceScore { get; set; }
        
        // Scheduling and Availability
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; } // When to start showing the link
        public DateTime? EndDate { get; set; } // When to stop showing the link
        public string ActiveDays { get; set; } // JSON array of days (Monday, Tuesday, etc.)
        public string ActiveHours { get; set; } // JSON array of hours (9-17, 18-22, etc.)
        public int? MaxDisplayCount { get; set; } // Maximum times to show per user
        public int? MaxClickCount { get; set; } // Maximum total clicks allowed
        
        // Budget and Cost
        public decimal? CostPerClick { get; set; } // CPC if applicable
        public decimal? TotalBudget { get; set; } // Total budget for this link
        public decimal? SpentBudget { get; set; } // Amount spent so far
        public string BillingType { get; set; } // CPC, CPM, Fixed
        
        // Compliance and Moderation
        public bool IsApproved { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string ApprovalNotes { get; set; }
        public bool IsCompliant { get; set; } // Regulatory compliance
        public string ComplianceNotes { get; set; }
        
        // AI Integration
        public decimal? RelevanceScore { get; set; } // AI-calculated relevance score
        public string AiRecommendations { get; set; } // JSON object of AI suggestions
        public DateTime? LastAiAnalysis { get; set; }
        public bool UseAiOptimization { get; set; }
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual User Sponsor { get; set; }
        
        [JsonIgnore]
        public virtual User ApprovedByUser { get; set; }
        
        [JsonIgnore]
        public virtual User CreatedByUser { get; set; }
        
        [JsonIgnore]
        public virtual User UpdatedByUser { get; set; }
    }
}