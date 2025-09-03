using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class SponsorshipDashboard
    {
        public int TotalSponsors { get; set; }
        public int ActiveCodes { get; set; }
        public int RedeemedCodes { get; set; }
        public int TotalRedemptions { get; set; }
        public decimal ConversionRate { get; set; }
        public List<TierPerformance> TierPerformance { get; set; }
        public List<RecentActivity> RecentActivity { get; set; }
        public Dictionary<string, int> RedemptionsByMonth { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class TierPerformance
    {
        public string TierName { get; set; }
        public int CodesGenerated { get; set; }
        public int CodesRedeemed { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentActivity
    {
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public string SponsorName { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LinkPerformanceMetrics
    {
        public int TotalLinks { get; set; }
        public int TotalClicks { get; set; }
        public int UniqueClicks { get; set; }
        public decimal ClickThroughRate { get; set; }
        public Dictionary<string, int> ClicksBySource { get; set; }
        public Dictionary<string, int> ClicksByPlatform { get; set; }
        public Dictionary<string, int> ClicksByCountry { get; set; }
        public List<TopPerformingLink> TopLinks { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class TopPerformingLink
    {
        public string LinkId { get; set; }
        public string Type { get; set; }
        public int Clicks { get; set; }
        public int UniqueClicks { get; set; }
        public decimal ConversionRate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RedemptionAnalytics
    {
        public int TotalRedemptions { get; set; }
        public int SuccessfulRedemptions { get; set; }
        public int FailedRedemptions { get; set; }
        public decimal SuccessRate { get; set; }
        public Dictionary<string, int> RedemptionsByTier { get; set; }
        public Dictionary<string, int> RedemptionsBySource { get; set; }
        public Dictionary<string, int> RedemptionsByDay { get; set; }
        public List<RedemptionTrend> Trends { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class RedemptionTrend
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Rate { get; set; }
    }

    public class GeographicData
    {
        public string Country { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TimeBasedMetrics
    {
        public Dictionary<string, int> HourlyDistribution { get; set; }
        public Dictionary<string, int> DailyDistribution { get; set; }
        public Dictionary<string, int> WeeklyDistribution { get; set; }
        public Dictionary<string, int> MonthlyDistribution { get; set; }
        public string PeakHour { get; set; }
        public string PeakDay { get; set; }
    }

    public class UserBehaviorInsights
    {
        public double AverageSessionDuration { get; set; }
        public double AverageActionsPerSession { get; set; }
        public Dictionary<string, int> MostUsedFeatures { get; set; }
        public Dictionary<string, double> FeatureEngagementRates { get; set; }
        public List<string> CommonUserJourneys { get; set; }
        public double RetentionRate { get; set; }
    }

    public class PerformanceComparison
    {
        public string MetricName { get; set; }
        public double CurrentPeriod { get; set; }
        public double PreviousPeriod { get; set; }
        public double ChangePercentage { get; set; }
        public string ChangeDirection { get; set; } // "up", "down", "stable"
        public string Interpretation { get; set; }
    }

    public class AdvancedAnalytics
    {
        public UserBehaviorInsights UserBehavior { get; set; }
        public TimeBasedMetrics TimeMetrics { get; set; }
        public List<GeographicData> GeographicBreakdown { get; set; }
        public List<PerformanceComparison> Comparisons { get; set; }
        public Dictionary<string, object> Predictions { get; set; }
        public List<string> Recommendations { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class CustomAnalyticsRequest
    {
        public List<string> Metrics { get; set; }
        public List<string> Dimensions { get; set; }
        public List<AnalyticsFilter> Filters { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Granularity { get; set; } // "hour", "day", "week", "month"
    }

    public class AnalyticsFilter
    {
        public string Field { get; set; }
        public string Operator { get; set; } // "equals", "contains", "greater_than", etc.
        public object Value { get; set; }
    }

    public class CustomAnalyticsResponse
    {
        public List<string> Headers { get; set; }
        public List<List<object>> Data { get; set; }
        public Dictionary<string, object> Summary { get; set; }
        public int TotalRows { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}