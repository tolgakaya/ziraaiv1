using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for analytics statistics for a specific segment (sponsored or non-sponsored)
    /// </summary>
    public class AnalyticsSegmentDto
    {
        /// <summary>
        /// Total number of analyses in this segment
        /// </summary>
        public int TotalAnalyses { get; set; }

        /// <summary>
        /// Number of completed analyses
        /// </summary>
        public int CompletedAnalyses { get; set; }

        /// <summary>
        /// Number of pending analyses
        /// </summary>
        public int PendingAnalyses { get; set; }

        /// <summary>
        /// Number of failed analyses
        /// </summary>
        public int FailedAnalyses { get; set; }

        /// <summary>
        /// Average health score across all analyses
        /// </summary>
        public int AverageHealthScore { get; set; }

        /// <summary>
        /// Number of unique users in this segment
        /// </summary>
        public int UniqueUsers { get; set; }

        /// <summary>
        /// Top crop types and their counts
        /// </summary>
        public Dictionary<string, int> TopCropTypes { get; set; }
    }
}
