using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Sponsorship-specific tier comparison DTO for purchase selection UI
    /// Extends SubscriptionTierDto with sponsor-specific features
    /// Used in GET /api/v1/sponsorship/tiers-for-purchase endpoint
    /// </summary>
    public class SponsorshipTierComparisonDto
    {
        // Basic Tier Info
        public int Id { get; set; }
        public string TierName { get; set; } // S, M, L, XL
        public string DisplayName { get; set; }
        public string Description { get; set; }

        // Pricing
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public string Currency { get; set; }

        // Purchase Limits
        public int MinPurchaseQuantity { get; set; }
        public int MaxPurchaseQuantity { get; set; }
        public int RecommendedQuantity { get; set; }

        // Subscription Quotas
        public int DailyRequestLimit { get; set; }
        public int MonthlyRequestLimit { get; set; }

        // Sponsorship-Specific Features
        public SponsorshipFeaturesDto SponsorshipFeatures { get; set; }

        // Display Metadata
        public bool IsPopular { get; set; } // Highlight M/L tiers
        public bool IsRecommended { get; set; } // Recommend based on business logic
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Sponsorship-specific features (NOT subscription features)
    /// These determine what sponsors can see and do with sponsored farmers
    /// </summary>
    public class SponsorshipFeaturesDto
    {
        // Data Access
        public int DataAccessPercentage { get; set; } // 30, 60, 100
        public FarmerDataAccessDto DataAccess { get; set; }

        // Logo Display
        public LogoVisibilityDto LogoVisibility { get; set; }

        // Communication
        public CommunicationFeaturesDto Communication { get; set; }

        // Smart Links (XL exclusive)
        public SmartLinksFeaturesDto SmartLinks { get; set; }

        // Support
        public SupportFeaturesDto Support { get; set; }
    }

    /// <summary>
    /// What farmer data the sponsor can access based on tier
    /// Tier-based visibility: S=30%, M=60%, L/XL=100%
    /// </summary>
    public class FarmerDataAccessDto
    {
        public bool FarmerNameContact { get; set; }
        public bool LocationCity { get; set; }
        public bool LocationDistrict { get; set; }
        public bool LocationCoordinates { get; set; }
        public bool CropTypes { get; set; }
        public bool DiseaseCategories { get; set; }
        public bool FullAnalysisDetails { get; set; }
        public bool AnalysisImages { get; set; }
        public bool AiRecommendations { get; set; }
    }

    /// <summary>
    /// Where sponsor logo appears in farmer's app
    /// Screen visibility increases with tier level
    /// </summary>
    public class LogoVisibilityDto
    {
        public bool StartScreen { get; set; }
        public bool ResultScreen { get; set; }
        public bool AnalysisDetailsScreen { get; set; }
        public bool FarmerProfileScreen { get; set; }

        // For UI display - list of screen names where logo is visible
        public List<string> VisibleScreens { get; set; } = new List<string>();
    }

    /// <summary>
    /// Communication capabilities with farmers
    /// Only available in L and XL tiers
    /// </summary>
    public class CommunicationFeaturesDto
    {
        public bool MessagingEnabled { get; set; }
        public bool ViewConversations { get; set; }
        public int? MessageRateLimitPerDay { get; set; } // null if disabled, 10 if enabled
    }

    /// <summary>
    /// Smart Links capabilities (XL tier exclusive)
    /// AI-powered product recommendations in analysis results
    /// </summary>
    public class SmartLinksFeaturesDto
    {
        public bool Enabled { get; set; }
        public int Quota { get; set; } // 0 for non-XL, 50 for XL
        public bool AnalyticsAccess { get; set; }
    }

    /// <summary>
    /// Support tier features
    /// Priority support and faster response times for higher tiers
    /// </summary>
    public class SupportFeaturesDto
    {
        public bool PrioritySupport { get; set; }
        public int ResponseTimeHours { get; set; } // 48, 24, 12
    }
}
