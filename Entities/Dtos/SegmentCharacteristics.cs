namespace Entities.Dtos
{
    /// <summary>
    /// Statistical characteristics of a farmer segment
    /// </summary>
    public class SegmentCharacteristics
    {
        /// <summary>
        /// Average number of analyses per month for this segment
        /// </summary>
        public decimal AvgAnalysesPerMonth { get; set; }

        /// <summary>
        /// Average days since last analysis
        /// </summary>
        public int AvgDaysSinceLastAnalysis { get; set; }

        /// <summary>
        /// Median days since last analysis
        /// </summary>
        public int MedianDaysSinceLastAnalysis { get; set; }

        /// <summary>
        /// Most common subscription tier
        /// </summary>
        public string MostCommonTier { get; set; }

        /// <summary>
        /// Percentage with active subscriptions (0-100)
        /// </summary>
        public decimal ActiveSubscriptionRate { get; set; }

        /// <summary>
        /// Average engagement score (0-100)
        /// </summary>
        public decimal AvgEngagementScore { get; set; }

        /// <summary>
        /// Most common crop analyzed by this segment
        /// </summary>
        public string TopCrop { get; set; }

        /// <summary>
        /// Most common disease encountered by this segment
        /// </summary>
        public string TopDisease { get; set; }
    }
}
