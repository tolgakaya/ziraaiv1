using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Temporal analytics for sponsors showing trends over time
    /// Provides time-series data for codes, redemptions, analyses, farmers, and messaging
    /// Cache TTL: 1 hour
    /// Authorization: Sponsor, Admin
    /// </summary>
    public class SponsorTemporalAnalyticsDto
    {
        /// <summary>
        /// Grouping period used for this data (Day, Week, Month)
        /// </summary>
        public string GroupBy { get; set; }

        /// <summary>
        /// Time series data for each period
        /// </summary>
        public List<TimePeriodData> TimeSeries { get; set; }

        /// <summary>
        /// Trend summary with growth metrics
        /// </summary>
        public TrendSummary TrendAnalysis { get; set; }

        /// <summary>
        /// Peak performance indicators
        /// </summary>
        public PeakPerformance PeakMetrics { get; set; }

        /// <summary>
        /// Start date of the analysis period
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the analysis period
        /// </summary>
        public DateTime EndDate { get; set; }

        public SponsorTemporalAnalyticsDto()
        {
            TimeSeries = new List<TimePeriodData>();
            TrendAnalysis = new TrendSummary();
            PeakMetrics = new PeakPerformance();
        }
    }

    /// <summary>
    /// Data for a single time period
    /// </summary>
    public class TimePeriodData
    {
        /// <summary>
        /// Period identifier (date, week number, month name)
        /// </summary>
        public string Period { get; set; }

        /// <summary>
        /// Start date of this period
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// End date of this period
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Codes distributed in this period
        /// </summary>
        public int CodesDistributed { get; set; }

        /// <summary>
        /// Codes redeemed in this period
        /// </summary>
        public int CodesRedeemed { get; set; }

        /// <summary>
        /// Analyses performed in this period
        /// </summary>
        public int AnalysesPerformed { get; set; }

        /// <summary>
        /// New farmers acquired in this period
        /// </summary>
        public int NewFarmers { get; set; }

        /// <summary>
        /// Active farmers in this period (farmers who performed at least one analysis)
        /// </summary>
        public int ActiveFarmers { get; set; }

        /// <summary>
        /// Messages sent by sponsor in this period
        /// </summary>
        public int MessagesSent { get; set; }

        /// <summary>
        /// Messages received from farmers in this period
        /// </summary>
        public int MessagesReceived { get; set; }

        /// <summary>
        /// Redemption rate for this period
        /// Formula: (CodesRedeemed / CodesDistributed) * 100
        /// </summary>
        public double RedemptionRate { get; set; }

        /// <summary>
        /// Engagement rate for this period
        /// Formula: (ActiveFarmers / TotalFarmers) * 100
        /// </summary>
        public double EngagementRate { get; set; }
    }

    /// <summary>
    /// Trend analysis summary
    /// </summary>
    public class TrendSummary
    {
        /// <summary>
        /// Overall trend direction: "Up", "Down", "Stable"
        /// </summary>
        public string Direction { get; set; }

        /// <summary>
        /// Code redemption growth percentage (last period vs previous)
        /// </summary>
        public double RedemptionGrowth { get; set; }

        /// <summary>
        /// Analysis growth percentage (last period vs previous)
        /// </summary>
        public double AnalysisGrowth { get; set; }

        /// <summary>
        /// Farmer growth percentage (last period vs previous)
        /// </summary>
        public double FarmerGrowth { get; set; }

        /// <summary>
        /// Engagement growth percentage (last period vs previous)
        /// </summary>
        public double EngagementGrowth { get; set; }

        /// <summary>
        /// Average period-over-period growth rate
        /// </summary>
        public double AverageGrowthRate { get; set; }

        /// <summary>
        /// Number of periods analyzed
        /// </summary>
        public int PeriodsAnalyzed { get; set; }
    }

    /// <summary>
    /// Peak performance metrics
    /// </summary>
    public class PeakPerformance
    {
        /// <summary>
        /// Date with highest analysis count
        /// </summary>
        public DateTime? PeakAnalysisDate { get; set; }

        /// <summary>
        /// Analysis count on peak day
        /// </summary>
        public int PeakAnalysisCount { get; set; }

        /// <summary>
        /// Date with highest redemption count
        /// </summary>
        public DateTime? PeakRedemptionDate { get; set; }

        /// <summary>
        /// Redemption count on peak day
        /// </summary>
        public int PeakRedemptionCount { get; set; }

        /// <summary>
        /// Date with highest engagement
        /// </summary>
        public DateTime? PeakEngagementDate { get; set; }

        /// <summary>
        /// Active farmers on peak engagement day
        /// </summary>
        public int PeakEngagementFarmers { get; set; }

        /// <summary>
        /// Period with best overall performance
        /// </summary>
        public string BestPeriod { get; set; }

        /// <summary>
        /// Period with worst overall performance
        /// </summary>
        public string WorstPeriod { get; set; }
    }
}
