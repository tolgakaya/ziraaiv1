namespace Entities.Dtos
{
    /// <summary>
    /// Anonymized industry-wide benchmark metrics
    /// All competitor data is fully anonymized with no sponsor identification
    /// </summary>
    public class IndustryBenchmarksDto
    {
        /// <summary>
        /// Industry average number of farmers per sponsor
        /// </summary>
        public decimal IndustryAvgFarmers { get; set; }

        /// <summary>
        /// Industry average number of analyses per farmer
        /// </summary>
        public decimal IndustryAvgAnalyses { get; set; }

        /// <summary>
        /// Industry average message response rate percentage (0-100)
        /// </summary>
        public decimal IndustryAvgResponseRate { get; set; }

        /// <summary>
        /// Industry average farmer retention rate percentage (0-100)
        /// </summary>
        public decimal IndustryAvgRetentionRate { get; set; }

        /// <summary>
        /// Industry average engagement score (0-100)
        /// </summary>
        public decimal IndustryAvgEngagementScore { get; set; }

        /// <summary>
        /// Top performer (90th percentile) farmer count
        /// </summary>
        public decimal TopPerformerFarmers { get; set; }

        /// <summary>
        /// Top performer (90th percentile) analyses per farmer
        /// </summary>
        public decimal TopPerformerAnalyses { get; set; }

        /// <summary>
        /// Top performer (90th percentile) message response rate
        /// </summary>
        public decimal TopPerformerResponseRate { get; set; }

        /// <summary>
        /// Top performer (90th percentile) retention rate
        /// </summary>
        public decimal TopPerformerRetentionRate { get; set; }

        /// <summary>
        /// Top performer (90th percentile) engagement score
        /// </summary>
        public decimal TopPerformerEngagementScore { get; set; }
    }
}
