using Entities.Concrete;
using System;

namespace Entities.Dtos
{
    /// <summary>
    /// Sponsored analysis detail response with tier metadata and permissions
    /// Wraps filtered PlantAnalysis entity with tier information for mobile UI logic
    /// </summary>
    public class SponsoredAnalysisDetailDto
    {
        /// <summary>
        /// Filtered plant analysis data based on sponsor's tier
        /// Fields will be null/empty based on access level (30%, 60%, 100%)
        /// </summary>
        public PlantAnalysis Analysis { get; set; }

        /// <summary>
        /// Tier information and permissions for UI logic
        /// </summary>
        public AnalysisTierMetadata TierMetadata { get; set; }
    }

    /// <summary>
    /// Tier-based metadata for analysis detail view
    /// Tells mobile app what features are available and what data to show/hide
    /// </summary>
    public class AnalysisTierMetadata
    {
        /// <summary>
        /// Tier name: "S/M", "L", "XL"
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// Access percentage: 30, 60, or 100
        /// </summary>
        public int AccessPercentage { get; set; }

        /// <summary>
        /// Can sponsor message farmer? (M, L, XL tiers)
        /// </summary>
        public bool CanMessage { get; set; }

        /// <summary>
        /// Can sponsor's logo be displayed? (All tiers on result screen)
        /// </summary>
        public bool CanViewLogo { get; set; }

        /// <summary>
        /// Sponsor company information for branding
        /// </summary>
        public SponsorDisplayInfoDto SponsorInfo { get; set; }

        /// <summary>
        /// Which fields are accessible at this tier level
        /// Helps mobile app know what to display
        /// </summary>
        public AccessibleFieldsInfo AccessibleFields { get; set; }
    }

    /// <summary>
    /// Information about which field groups are accessible
    /// Mobile app can use this to conditionally render UI sections
    /// </summary>
    public class AccessibleFieldsInfo
    {
        /// <summary>
        /// Basic plant info (30% access)
        /// </summary>
        public bool CanViewBasicInfo { get; set; }

        /// <summary>
        /// Health scores and species info (30% access)
        /// </summary>
        public bool CanViewHealthScore { get; set; }

        /// <summary>
        /// Plant images (30% access)
        /// </summary>
        public bool CanViewImages { get; set; }

        /// <summary>
        /// Detailed health assessment (60% access)
        /// </summary>
        public bool CanViewDetailedHealth { get; set; }

        /// <summary>
        /// Disease and pest information (60% access)
        /// </summary>
        public bool CanViewDiseases { get; set; }

        /// <summary>
        /// Nutrient analysis (60% access)
        /// </summary>
        public bool CanViewNutrients { get; set; }

        /// <summary>
        /// Treatment recommendations (60% access)
        /// </summary>
        public bool CanViewRecommendations { get; set; }

        /// <summary>
        /// Location and GPS data (60% access)
        /// </summary>
        public bool CanViewLocation { get; set; }

        /// <summary>
        /// Farmer contact information (100% access)
        /// </summary>
        public bool CanViewFarmerContact { get; set; }

        /// <summary>
        /// Field management data (100% access)
        /// </summary>
        public bool CanViewFieldData { get; set; }

        /// <summary>
        /// Processing metadata and costs (100% access)
        /// </summary>
        public bool CanViewProcessingData { get; set; }
    }
}
