using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Code-level analysis statistics showing which codes generated how many analyses
    /// </summary>
    public class CodeAnalysisStatisticsDto
    {
        /// <summary>
        /// Total codes that have been redeemed
        /// </summary>
        public int TotalRedeemedCodes { get; set; }

        /// <summary>
        /// Total analyses performed by sponsored farmers
        /// </summary>
        public int TotalAnalysesPerformed { get; set; }

        /// <summary>
        /// Average analyses per redeemed code
        /// </summary>
        public decimal AverageAnalysesPerCode { get; set; }

        /// <summary>
        /// Total farmers using sponsored subscriptions
        /// </summary>
        public int TotalActiveFarmers { get; set; }

        /// <summary>
        /// Code-level breakdown with analysis details
        /// </summary>
        public List<CodeAnalysisBreakdown> CodeBreakdowns { get; set; }

        /// <summary>
        /// Top performing codes (most analyses)
        /// </summary>
        public List<CodeAnalysisBreakdown> TopPerformingCodes { get; set; }

        /// <summary>
        /// Crop type distribution across all analyses
        /// </summary>
        public List<CropTypeStatistic> CropTypeDistribution { get; set; }

        /// <summary>
        /// Disease distribution across all analyses
        /// </summary>
        public List<DiseaseStatistic> DiseaseDistribution { get; set; }
    }

    public class CodeAnalysisBreakdown
    {
        /// <summary>
        /// Sponsorship code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Subscription tier name
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// Farmer who redeemed the code
        /// </summary>
        public int FarmerId { get; set; }

        /// <summary>
        /// Farmer name (visible based on tier)
        /// </summary>
        public string FarmerName { get; set; }

        /// <summary>
        /// Farmer email (visible based on tier)
        /// </summary>
        public string FarmerEmail { get; set; }

        /// <summary>
        /// Farmer phone (visible based on tier)
        /// </summary>
        public string FarmerPhone { get; set; }

        /// <summary>
        /// Farmer location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Code redemption date
        /// </summary>
        public DateTime RedeemedDate { get; set; }

        /// <summary>
        /// Subscription status (Active, Expired)
        /// </summary>
        public string SubscriptionStatus { get; set; }

        /// <summary>
        /// Subscription end date
        /// </summary>
        public DateTime? SubscriptionEndDate { get; set; }

        /// <summary>
        /// Total analyses performed with this code
        /// </summary>
        public int TotalAnalyses { get; set; }

        /// <summary>
        /// List of analysis summaries
        /// </summary>
        public List<SponsoredAnalysisSummary> Analyses { get; set; }

        /// <summary>
        /// Most recent analysis date
        /// </summary>
        public DateTime? LastAnalysisDate { get; set; }

        /// <summary>
        /// Days since last analysis
        /// </summary>
        public int? DaysSinceLastAnalysis { get; set; }
    }

    public class SponsoredAnalysisSummary
    {
        /// <summary>
        /// Plant analysis ID (for drill-down)
        /// </summary>
        public int AnalysisId { get; set; }

        /// <summary>
        /// Analysis date
        /// </summary>
        public DateTime AnalysisDate { get; set; }

        /// <summary>
        /// Crop type
        /// </summary>
        public string CropType { get; set; }

        /// <summary>
        /// Disease/issue detected
        /// </summary>
        public string Disease { get; set; }

        /// <summary>
        /// Disease category (Fungal, Bacterial, Pest, etc.)
        /// </summary>
        public string DiseaseCategory { get; set; }

        /// <summary>
        /// Severity (Low, Moderate, High, Critical)
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// Location (city, district)
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Analysis status (Completed, Pending, Failed)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Whether sponsor logo was displayed
        /// </summary>
        public bool SponsorLogoDisplayed { get; set; }

        /// <summary>
        /// Link to view full analysis details
        /// </summary>
        public string AnalysisDetailsUrl { get; set; }
    }

    public class CropTypeStatistic
    {
        /// <summary>
        /// Crop type name
        /// </summary>
        public string CropType { get; set; }

        /// <summary>
        /// Number of analyses for this crop
        /// </summary>
        public int AnalysisCount { get; set; }

        /// <summary>
        /// Percentage of total analyses
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Number of unique farmers analyzing this crop
        /// </summary>
        public int UniqueFarmers { get; set; }
    }

    public class DiseaseStatistic
    {
        /// <summary>
        /// Disease name
        /// </summary>
        public string Disease { get; set; }

        /// <summary>
        /// Disease category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Number of occurrences
        /// </summary>
        public int OccurrenceCount { get; set; }

        /// <summary>
        /// Percentage of total analyses
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Affected crop types
        /// </summary>
        public List<string> AffectedCrops { get; set; }

        /// <summary>
        /// Geographic distribution (cities)
        /// </summary>
        public List<string> GeographicDistribution { get; set; }
    }
}
