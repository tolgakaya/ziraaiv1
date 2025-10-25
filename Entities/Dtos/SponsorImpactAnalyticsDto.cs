using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Comprehensive impact analytics for sponsors
    /// Provides insights into farmer reach, agricultural impact, geographic coverage, and severity distribution
    /// Cache TTL: 6 hours (360 minutes)
    /// Authorization: Sponsor, Admin
    /// </summary>
    public class SponsorImpactAnalyticsDto
    {
        // Farmer Impact Metrics
        /// <summary>
        /// Total unique farmers reached through sponsorship
        /// </summary>
        public int TotalFarmersReached { get; set; }

        /// <summary>
        /// Farmers active in last 30 days
        /// </summary>
        public int ActiveFarmersLast30Days { get; set; }

        /// <summary>
        /// Month-over-month farmer retention rate percentage
        /// Formula: (retained farmers / last month farmers) * 100
        /// </summary>
        public double FarmerRetentionRate { get; set; }

        /// <summary>
        /// Average farmer lifetime in days (first analysis to last analysis)
        /// </summary>
        public double AverageFarmerLifetimeDays { get; set; }

        // Agricultural Impact Metrics
        /// <summary>
        /// Total crops analyzed across all farmers
        /// </summary>
        public int TotalCropsAnalyzed { get; set; }

        /// <summary>
        /// Number of unique crop types analyzed
        /// </summary>
        public int UniqueCropTypes { get; set; }

        /// <summary>
        /// Total diseases detected
        /// </summary>
        public int DiseasesDetected { get; set; }

        /// <summary>
        /// Critical issues resolved (HealthSeverity = Critical)
        /// </summary>
        public int CriticalIssuesResolved { get; set; }

        // Geographic Reach Metrics
        /// <summary>
        /// Number of cities reached
        /// </summary>
        public int CitiesReached { get; set; }

        /// <summary>
        /// Number of districts reached
        /// </summary>
        public int DistrictsReached { get; set; }

        /// <summary>
        /// Top 10 cities by analysis count
        /// </summary>
        public List<CityImpact> TopCities { get; set; }

        // Severity Distribution
        /// <summary>
        /// Distribution of health severity levels
        /// </summary>
        public SeverityStats SeverityDistribution { get; set; }

        // Top Crops and Diseases
        /// <summary>
        /// Top 10 crops by analysis count
        /// </summary>
        public List<CropStat> TopCrops { get; set; }

        /// <summary>
        /// Top 10 diseases by occurrence count
        /// </summary>
        public List<DiseaseStat> TopDiseases { get; set; }

        /// <summary>
        /// Data collection period start date
        /// </summary>
        public DateTime DataStartDate { get; set; }

        /// <summary>
        /// Data collection period end date
        /// </summary>
        public DateTime DataEndDate { get; set; }

        public SponsorImpactAnalyticsDto()
        {
            TopCities = new List<CityImpact>();
            TopCrops = new List<CropStat>();
            TopDiseases = new List<DiseaseStat>();
            SeverityDistribution = new SeverityStats();
        }
    }

    /// <summary>
    /// City-level impact statistics
    /// </summary>
    public class CityImpact
    {
        /// <summary>
        /// City name
        /// </summary>
        public string CityName { get; set; }

        /// <summary>
        /// District name (if available)
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// Number of unique farmers in this city
        /// </summary>
        public int FarmerCount { get; set; }

        /// <summary>
        /// Total analyses performed in this city
        /// </summary>
        public int AnalysisCount { get; set; }

        /// <summary>
        /// Percentage of total analyses
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// Most common crop in this city
        /// </summary>
        public string MostCommonCrop { get; set; }

        /// <summary>
        /// Most common disease in this city
        /// </summary>
        public string MostCommonDisease { get; set; }
    }

    /// <summary>
    /// Health severity distribution statistics
    /// </summary>
    public class SeverityStats
    {
        /// <summary>
        /// Count of low severity issues
        /// </summary>
        public int LowSeverityCount { get; set; }

        /// <summary>
        /// Count of moderate severity issues
        /// </summary>
        public int ModerateSeverityCount { get; set; }

        /// <summary>
        /// Count of high severity issues
        /// </summary>
        public int HighSeverityCount { get; set; }

        /// <summary>
        /// Count of critical severity issues
        /// </summary>
        public int CriticalSeverityCount { get; set; }

        /// <summary>
        /// Percentage of low severity issues
        /// </summary>
        public double LowPercentage { get; set; }

        /// <summary>
        /// Percentage of moderate severity issues
        /// </summary>
        public double ModeratePercentage { get; set; }

        /// <summary>
        /// Percentage of high severity issues
        /// </summary>
        public double HighPercentage { get; set; }

        /// <summary>
        /// Percentage of critical severity issues
        /// </summary>
        public double CriticalPercentage { get; set; }
    }

    /// <summary>
    /// Crop type statistics
    /// </summary>
    public class CropStat
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
        public double Percentage { get; set; }

        /// <summary>
        /// Number of unique farmers growing this crop
        /// </summary>
        public int UniqueFarmers { get; set; }

        /// <summary>
        /// Average health score for this crop (if available)
        /// </summary>
        public double? AverageHealthScore { get; set; }
    }

    /// <summary>
    /// Disease statistics
    /// </summary>
    public class DiseaseStat
    {
        /// <summary>
        /// Disease name
        /// </summary>
        public string DiseaseName { get; set; }

        /// <summary>
        /// Disease category (if available)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Number of occurrences
        /// </summary>
        public int OccurrenceCount { get; set; }

        /// <summary>
        /// Percentage of total diseases
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// List of affected crop types
        /// </summary>
        public List<string> AffectedCrops { get; set; }

        /// <summary>
        /// Most common severity level for this disease
        /// </summary>
        public string MostCommonSeverity { get; set; }

        /// <summary>
        /// Cities where this disease is most prevalent
        /// </summary>
        public List<string> TopCities { get; set; }

        public DiseaseStat()
        {
            AffectedCrops = new List<string>();
            TopCities = new List<string>();
        }
    }
}
