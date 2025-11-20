namespace Entities.Dtos
{
    /// <summary>
    /// Percentile ranking across all performance metrics
    /// Shows where the sponsor ranks compared to all other sponsors
    /// Higher percentile = better performance (e.g., 90th percentile = top 10%)
    /// </summary>
    public class PercentileRankingDto
    {
        /// <summary>
        /// Overall percentile ranking across all metrics (0-100)
        /// E.g., 68 = top 32% of all sponsors
        /// </summary>
        public int OverallPercentile { get; set; }

        /// <summary>
        /// Percentile for farmer count metric (0-100)
        /// </summary>
        public int FarmersPercentile { get; set; }

        /// <summary>
        /// Percentile for analyses per farmer metric (0-100)
        /// </summary>
        public int AnalysesPercentile { get; set; }

        /// <summary>
        /// Percentile for message response rate metric (0-100)
        /// </summary>
        public int ResponseRatePercentile { get; set; }

        /// <summary>
        /// Percentile for farmer retention rate metric (0-100)
        /// </summary>
        public int RetentionRatePercentile { get; set; }

        /// <summary>
        /// Percentile for engagement score metric (0-100)
        /// </summary>
        public int EngagementScorePercentile { get; set; }

        /// <summary>
        /// Human-readable ranking description (e.g., "Top 10%", "Top 25%", "Average")
        /// </summary>
        public string RankingDescription { get; set; }
    }
}
