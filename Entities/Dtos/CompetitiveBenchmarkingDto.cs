using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Competitive benchmarking analytics response
    /// Provides sponsor performance comparison with industry averages and percentile rankings
    /// </summary>
    public class CompetitiveBenchmarkingDto
    {
        /// <summary>
        /// Current sponsor's performance metrics
        /// </summary>
        public SponsorPerformanceDto YourPerformance { get; set; }

        /// <summary>
        /// Anonymized industry-wide benchmark metrics
        /// </summary>
        public IndustryBenchmarksDto IndustryBenchmarks { get; set; }

        /// <summary>
        /// Gap analysis comparing sponsor performance to industry standards
        /// </summary>
        public List<GapAnalysisDto> Gaps { get; set; }

        /// <summary>
        /// Percentile ranking across all metrics
        /// </summary>
        public PercentileRankingDto Ranking { get; set; }

        /// <summary>
        /// Sponsor ID for this benchmark (null for admin viewing all)
        /// </summary>
        public int? SponsorId { get; set; }

        /// <summary>
        /// Total number of sponsors included in benchmark calculations
        /// </summary>
        public int TotalSponsorsInBenchmark { get; set; }

        /// <summary>
        /// Timestamp when this benchmark was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Time period for the benchmark data (e.g., "Last 30 days", "Last 90 days")
        /// </summary>
        public string TimePeriod { get; set; }
    }
}
