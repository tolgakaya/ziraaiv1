namespace Entities.Dtos
{
    /// <summary>
    /// DTO for comparison metrics between sponsored and non-sponsored analyses
    /// </summary>
    public class ComparisonMetricsDto
    {
        /// <summary>
        /// Difference in average health score (sponsored - non-sponsored)
        /// </summary>
        public int AverageHealthScoreDifference { get; set; }

        /// <summary>
        /// Completion rate for sponsored analyses (percentage)
        /// </summary>
        public double CompletionRateSponsored { get; set; }

        /// <summary>
        /// Completion rate for non-sponsored analyses (percentage)
        /// </summary>
        public double CompletionRateNonSponsored { get; set; }

        /// <summary>
        /// Difference in completion rates (sponsored - non-sponsored)
        /// </summary>
        public double CompletionRateDifference { get; set; }

        /// <summary>
        /// Ratio of unique users (sponsored / non-sponsored)
        /// </summary>
        public double UserEngagementRatio { get; set; }
    }
}
