namespace Entities.Dtos
{
    /// <summary>
    /// Current sponsor's performance metrics for competitive benchmarking
    /// </summary>
    public class SponsorPerformanceDto
    {
        /// <summary>
        /// Average number of active farmers per month
        /// </summary>
        public decimal AvgFarmersPerMonth { get; set; }

        /// <summary>
        /// Average number of analyses per farmer
        /// </summary>
        public decimal AvgAnalysesPerFarmer { get; set; }

        /// <summary>
        /// Message response rate percentage (0-100)
        /// Percentage of messages that received a response from farmers
        /// </summary>
        public decimal MessageResponseRate { get; set; }

        /// <summary>
        /// Farmer retention rate percentage (0-100)
        /// Percentage of farmers who remain active month-over-month
        /// </summary>
        public decimal FarmerRetentionRate { get; set; }

        /// <summary>
        /// Average engagement score across all farmers (0-100)
        /// </summary>
        public decimal AvgEngagementScore { get; set; }

        /// <summary>
        /// Total number of farmers sponsored
        /// </summary>
        public int TotalFarmers { get; set; }

        /// <summary>
        /// Total number of analyses in the time period
        /// </summary>
        public int TotalAnalyses { get; set; }

        /// <summary>
        /// Total number of messages sent
        /// </summary>
        public int TotalMessagesSent { get; set; }
    }
}
