using Entities.Concrete;
using Entities.Dtos;
using System.Collections.Generic;
using System.Linq;

namespace Business.Services.Sponsorship
{
    /// <summary>
    /// Maps SubscriptionTier entities to SponsorshipTierComparisonDto
    /// with tier-specific sponsorship features
    /// </summary>
    public interface ISponsorshipTierMappingService
    {
        SponsorshipTierComparisonDto MapToComparisonDto(SubscriptionTier tier);
        List<SponsorshipTierComparisonDto> MapToComparisonDtos(List<SubscriptionTier> tiers);
    }

    public class SponsorshipTierMappingService : ISponsorshipTierMappingService
    {
        public SponsorshipTierComparisonDto MapToComparisonDto(SubscriptionTier tier)
        {
            return new SponsorshipTierComparisonDto
            {
                Id = tier.Id,
                TierName = tier.TierName,
                DisplayName = tier.DisplayName,
                Description = tier.Description,
                MonthlyPrice = tier.MonthlyPrice,
                YearlyPrice = tier.YearlyPrice,
                Currency = tier.Currency,
                MinPurchaseQuantity = tier.MinPurchaseQuantity,
                MaxPurchaseQuantity = tier.MaxPurchaseQuantity,
                RecommendedQuantity = tier.RecommendedQuantity,
                DailyRequestLimit = tier.DailyRequestLimit,
                MonthlyRequestLimit = tier.MonthlyRequestLimit,
                SponsorshipFeatures = GetSponsorshipFeatures(tier.TierName),
                IsPopular = tier.TierName == "M" || tier.TierName == "L",
                IsRecommended = tier.TierName == "M",
                DisplayOrder = tier.DisplayOrder
            };
        }

        public List<SponsorshipTierComparisonDto> MapToComparisonDtos(List<SubscriptionTier> tiers)
        {
            return tiers.Select(MapToComparisonDto)
                       .OrderBy(t => t.DisplayOrder)
                       .ToList();
        }

        /// <summary>
        /// Maps tier name to sponsorship-specific features
        /// Based on SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md constraint matrix
        /// </summary>
        private SponsorshipFeaturesDto GetSponsorshipFeatures(string tierName)
        {
            return tierName switch
            {
                "S" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 30,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = false,
                        LocationCity = true,
                        LocationDistrict = false,
                        LocationCoordinates = false,
                        CropTypes = false,
                        DiseaseCategories = false,
                        FullAnalysisDetails = false,
                        AnalysisImages = false,
                        AiRecommendations = false
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = false,
                        AnalysisDetailsScreen = false,
                        FarmerProfileScreen = false,
                        VisibleScreens = new List<string> { "Start Screen" }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = false,
                        ViewConversations = false,
                        MessageRateLimitPerDay = null
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = false,
                        Quota = 0,
                        AnalyticsAccess = false
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = false,
                        ResponseTimeHours = 48
                    }
                },

                "M" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 60,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = false,
                        LocationCity = true,
                        LocationDistrict = true,
                        LocationCoordinates = false,
                        CropTypes = true,
                        DiseaseCategories = true,
                        FullAnalysisDetails = false,
                        AnalysisImages = false,
                        AiRecommendations = false
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = true,
                        AnalysisDetailsScreen = false,
                        FarmerProfileScreen = false,
                        VisibleScreens = new List<string> { "Start Screen", "Result Screen" }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = false,
                        ViewConversations = false,
                        MessageRateLimitPerDay = null
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = false,
                        Quota = 0,
                        AnalyticsAccess = false
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = false,
                        ResponseTimeHours = 48
                    }
                },

                "L" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 100,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = true,
                        LocationCity = true,
                        LocationDistrict = true,
                        LocationCoordinates = true,
                        CropTypes = true,
                        DiseaseCategories = true,
                        FullAnalysisDetails = true,
                        AnalysisImages = true,
                        AiRecommendations = true
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = true,
                        AnalysisDetailsScreen = true,
                        FarmerProfileScreen = true,
                        VisibleScreens = new List<string>
                        {
                            "Start Screen", "Result Screen",
                            "Analysis Details", "Farmer Profile"
                        }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = true,
                        ViewConversations = true,
                        MessageRateLimitPerDay = 10
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = false,
                        Quota = 0,
                        AnalyticsAccess = false
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = true,
                        ResponseTimeHours = 24
                    }
                },

                "XL" => new SponsorshipFeaturesDto
                {
                    DataAccessPercentage = 100,
                    DataAccess = new FarmerDataAccessDto
                    {
                        FarmerNameContact = true,
                        LocationCity = true,
                        LocationDistrict = true,
                        LocationCoordinates = true,
                        CropTypes = true,
                        DiseaseCategories = true,
                        FullAnalysisDetails = true,
                        AnalysisImages = true,
                        AiRecommendations = true
                    },
                    LogoVisibility = new LogoVisibilityDto
                    {
                        StartScreen = true,
                        ResultScreen = true,
                        AnalysisDetailsScreen = true,
                        FarmerProfileScreen = true,
                        VisibleScreens = new List<string>
                        {
                            "Start Screen", "Result Screen",
                            "Analysis Details", "Farmer Profile"
                        }
                    },
                    Communication = new CommunicationFeaturesDto
                    {
                        MessagingEnabled = true,
                        ViewConversations = true,
                        MessageRateLimitPerDay = 10
                    },
                    SmartLinks = new SmartLinksFeaturesDto
                    {
                        Enabled = true,
                        Quota = 50,
                        AnalyticsAccess = true
                    },
                    Support = new SupportFeaturesDto
                    {
                        PrioritySupport = true,
                        ResponseTimeHours = 12
                    }
                },

                _ => throw new System.ArgumentException($"Unknown tier name: {tierName}")
            };
        }
    }
}
