namespace Entities.Dtos
{
    /// <summary>
    /// DTO for comprehensive comparison analytics between sponsored and non-sponsored analyses
    /// </summary>
    public class SponsorshipComparisonAnalyticsDto
    {
        /// <summary>
        /// Date range applied to the analytics query
        /// </summary>
        public DateRangeDto DateRange { get; set; }

        /// <summary>
        /// Total number of analyses across both segments
        /// </summary>
        public int TotalAnalyses { get; set; }

        /// <summary>
        /// Percentage of analyses that are sponsored
        /// </summary>
        public double SponsorshipRate { get; set; }

        /// <summary>
        /// Analytics for sponsored analyses
        /// </summary>
        public AnalyticsSegmentDto SponsoredAnalytics { get; set; }

        /// <summary>
        /// Analytics for non-sponsored analyses
        /// </summary>
        public AnalyticsSegmentDto NonSponsoredAnalytics { get; set; }

        /// <summary>
        /// Comparison metrics between the two segments
        /// </summary>
        public ComparisonMetricsDto ComparisonMetrics { get; set; }
    }
}
