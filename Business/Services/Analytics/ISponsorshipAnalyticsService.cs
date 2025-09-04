using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Analytics
{
    public interface ISponsorshipAnalyticsService
    {
        Task<IDataResult<SponsorshipDashboard>> GetSponsorDashboardAsync(int sponsorId);
        Task<IDataResult<LinkPerformanceMetrics>> GetLinkPerformanceAsync(int sponsorId, string timeRange = "30d");
        Task<IDataResult<RedemptionAnalytics>> GetRedemptionAnalyticsAsync(int sponsorId, string timeRange = "30d");
        Task<IDataResult<GeographicDistribution>> GetGeographicAnalyticsAsync(int sponsorId);
        Task<IDataResult<MessagePerformanceAnalytics>> GetMessagePerformanceAsync(int sponsorId, string timeRange = "30d");
        Task<IDataResult<ConversionFunnelAnalytics>> GetConversionFunnelAsync(int sponsorId, string timeRange = "30d");
        Task<IDataResult<CompetitiveAnalytics>> GetCompetitiveAnalyticsAsync(int sponsorId);
    }

    public class SponsorshipDashboard
    {
        public SponsorOverview Overview { get; set; }
        public RecentActivity RecentActivity { get; set; }
        public QuickStats QuickStats { get; set; }
        public List<ChartData> LinkPerformanceChart { get; set; }
        public List<ChartData> RedemptionTrendChart { get; set; }
        public List<PlatformUsage> PlatformBreakdown { get; set; }
        public List<TopPerformingCode> TopCodes { get; set; }
    }

    public class SponsorOverview
    {
        public int SponsorId { get; set; }
        public string SponsorName { get; set; }
        public string CompanyType { get; set; }
        public DateTime JoinDate { get; set; }
        public string CurrentTier { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalCodesGenerated { get; set; }
        public int TotalCodesRedeemed { get; set; }
        public double RedemptionRate { get; set; }
        public string Status { get; set; }
    }

    public class RecentActivity
    {
        public List<ActivityItem> Activities { get; set; }
        public int TotalActivities { get; set; }
        public DateTime LastActivityDate { get; set; }
    }

    public class ActivityItem
    {
        public string Type { get; set; } // "purchase", "link_sent", "code_redeemed", "message_sent"
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public object Metadata { get; set; }
    }

    public class QuickStats
    {
        public int ActiveCodes { get; set; }
        public int TodaysRedemptions { get; set; }
        public int PendingLinks { get; set; }
        public decimal ThisMonthSpending { get; set; }
        public double ClickThroughRate { get; set; }
        public double ConversionRate { get; set; }
        public int UniqueFarmersReached { get; set; }
        public string TopPerformingChannel { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; } // Date, category, etc.
        public decimal Value { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class PlatformUsage
    {
        public string Platform { get; set; } // iOS, Android, Web, SMS, WhatsApp
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }

    public class TopPerformingCode
    {
        public string Code { get; set; }
        public int ClickCount { get; set; }
        public int RedemptionCount { get; set; }
        public double ConversionRate { get; set; }
        public string Channel { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }
    }

    public class LinkPerformanceMetrics
    {
        public int TotalLinksSent { get; set; }
        public int TotalClicks { get; set; }
        public int UniqueClicks { get; set; }
        public double OverallCTR { get; set; } // Click-through rate
        public Dictionary<string, ChannelPerformance> ChannelBreakdown { get; set; }
        public List<DailyPerformance> DailyTrends { get; set; }
        public List<HourlyPerformance> BestSendingTimes { get; set; }
        public DeviceAnalytics DeviceStats { get; set; }
    }

    public class ChannelPerformance
    {
        public string Channel { get; set; } // SMS, WhatsApp, Email
        public int LinksSent { get; set; }
        public int Clicks { get; set; }
        public double CTR { get; set; }
        public decimal AverageCost { get; set; }
        public string Status { get; set; }
    }

    public class DailyPerformance
    {
        public DateTime Date { get; set; }
        public int LinksSent { get; set; }
        public int Clicks { get; set; }
        public int Redemptions { get; set; }
        public double CTR { get; set; }
        public double ConversionRate { get; set; }
    }

    public class HourlyPerformance
    {
        public int Hour { get; set; }
        public int Clicks { get; set; }
        public double CTR { get; set; }
        public string Recommendation { get; set; }
    }

    public class DeviceAnalytics
    {
        public Dictionary<string, int> PlatformBreakdown { get; set; }
        public Dictionary<string, int> BrowserBreakdown { get; set; }
        public int MobilePercentage { get; set; }
        public int DesktopPercentage { get; set; }
    }

    public class RedemptionAnalytics
    {
        public int TotalRedemptions { get; set; }
        public int SuccessfulRedemptions { get; set; }
        public int FailedRedemptions { get; set; }
        public double SuccessRate { get; set; }
        public decimal TotalValueRedeemed { get; set; }
        public Dictionary<string, int> TierBreakdown { get; set; }
        public List<RedemptionTrend> TrendData { get; set; }
        public List<FailureReason> FailureReasons { get; set; }
        public AverageRedemptionTime AverageTimings { get; set; }
    }

    public class RedemptionTrend
    {
        public DateTime Date { get; set; }
        public int Redemptions { get; set; }
        public int UniqueUsers { get; set; }
        public decimal Value { get; set; }
    }

    public class FailureReason
    {
        public string Reason { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string Recommendation { get; set; }
    }

    public class AverageRedemptionTime
    {
        public double ClickToRedemption { get; set; } // Minutes
        public double SendToClick { get; set; } // Minutes
        public double SendToRedemption { get; set; } // Minutes
        public string FastestChannel { get; set; }
        public string SlowestChannel { get; set; }
    }

    public class GeographicDistribution
    {
        public Dictionary<string, CityData> Cities { get; set; }
        public Dictionary<string, RegionData> Regions { get; set; }
        public List<MapDataPoint> MapData { get; set; }
        public string TopCity { get; set; }
        public string TopRegion { get; set; }
    }

    public class CityData
    {
        public string CityName { get; set; }
        public int Clicks { get; set; }
        public int Redemptions { get; set; }
        public double ConversionRate { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    public class RegionData
    {
        public string RegionName { get; set; }
        public int Clicks { get; set; }
        public int Redemptions { get; set; }
        public double ConversionRate { get; set; }
        public int Cities { get; set; }
    }

    public class MapDataPoint
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int Value { get; set; }
        public string Label { get; set; }
        public string Color { get; set; }
    }

    public class MessagePerformanceAnalytics
    {
        public Dictionary<string, TemplatePerformance> Templates { get; set; }
        public List<KeywordAnalysis> KeywordPerformance { get; set; }
        public MessageTimingAnalysis TimingAnalysis { get; set; }
        public List<ABTestResult> ABTests { get; set; }
        public SentimentAnalysis Sentiment { get; set; }
    }

    public class TemplatePerformance
    {
        public string TemplateName { get; set; }
        public int TimesSent { get; set; }
        public int Clicks { get; set; }
        public double CTR { get; set; }
        public double ConversionRate { get; set; }
        public string BestPerformingVariation { get; set; }
    }

    public class KeywordAnalysis
    {
        public string Keyword { get; set; }
        public int Occurrences { get; set; }
        public double Impact { get; set; }
        public string Sentiment { get; set; }
    }

    public class MessageTimingAnalysis
    {
        public Dictionary<string, double> BestDays { get; set; }
        public Dictionary<int, double> BestHours { get; set; }
        public string OptimalSendTime { get; set; }
        public string WorstSendTime { get; set; }
    }

    public class ABTestResult
    {
        public string TestName { get; set; }
        public string VariationA { get; set; }
        public string VariationB { get; set; }
        public double VariationAPerformance { get; set; }
        public double VariationBPerformance { get; set; }
        public string Winner { get; set; }
        public double ConfidenceLevel { get; set; }
        public string Status { get; set; }
    }

    public class SentimentAnalysis
    {
        public double PositiveSentiment { get; set; }
        public double NeutralSentiment { get; set; }
        public double NegativeSentiment { get; set; }
        public List<string> PositiveKeywords { get; set; }
        public List<string> NegativeKeywords { get; set; }
    }

    public class ConversionFunnelAnalytics
    {
        public List<FunnelStage> Stages { get; set; }
        public double OverallConversionRate { get; set; }
        public FunnelStage BiggestDropOff { get; set; }
        public List<FunnelOptimizationSuggestion> Suggestions { get; set; }
    }

    public class FunnelStage
    {
        public string StageName { get; set; }
        public int Users { get; set; }
        public double ConversionRate { get; set; }
        public double DropOffRate { get; set; }
        public string Description { get; set; }
    }

    public class FunnelOptimizationSuggestion
    {
        public string Stage { get; set; }
        public string Issue { get; set; }
        public string Suggestion { get; set; }
        public double PotentialImprovement { get; set; }
        public string Priority { get; set; }
    }

    public class CompetitiveAnalytics
    {
        public IndustryBenchmarks Benchmarks { get; set; }
        public RankingData Ranking { get; set; }
        public List<CompetitiveInsight> Insights { get; set; }
        public MarketTrends Trends { get; set; }
    }

    public class IndustryBenchmarks
    {
        public double AverageCTR { get; set; }
        public double AverageConversionRate { get; set; }
        public decimal AverageCostPerRedemption { get; set; }
        public double AverageRedemptionTime { get; set; }
        public string Industry { get; set; }
    }

    public class RankingData
    {
        public int CTRRank { get; set; }
        public int ConversionRateRank { get; set; }
        public int VolumeRank { get; set; }
        public int OverallRank { get; set; }
        public int TotalCompetitors { get; set; }
    }

    public class CompetitiveInsight
    {
        public string Metric { get; set; }
        public string Comparison { get; set; } // "above_average", "below_average", "average"
        public double Difference { get; set; }
        public string Recommendation { get; set; }
    }

    public class MarketTrends
    {
        public List<TrendItem> RecentTrends { get; set; }
        public List<SeasonalPattern> SeasonalPatterns { get; set; }
        public List<string> EmergingChannels { get; set; }
    }

    public class TrendItem
    {
        public string Name { get; set; }
        public string Direction { get; set; } // "up", "down", "stable"
        public double ChangePercentage { get; set; }
        public string Period { get; set; }
    }

    public class SeasonalPattern
    {
        public string Season { get; set; }
        public double PerformanceMultiplier { get; set; }
        public string Description { get; set; }
    }
}