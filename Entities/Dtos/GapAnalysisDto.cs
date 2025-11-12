namespace Entities.Dtos
{
    /// <summary>
    /// Gap analysis for a specific metric comparing sponsor performance to industry standards
    /// </summary>
    public class GapAnalysisDto
    {
        /// <summary>
        /// Metric name (e.g., "Farmer Count", "Analyses Per Farmer")
        /// </summary>
        public string MetricName { get; set; }

        /// <summary>
        /// Sponsor's current value for this metric
        /// </summary>
        public decimal YourValue { get; set; }

        /// <summary>
        /// Industry average value for this metric
        /// </summary>
        public decimal IndustryAvg { get; set; }

        /// <summary>
        /// Top performer (90th percentile) value for this metric
        /// </summary>
        public decimal TopPerformer { get; set; }

        /// <summary>
        /// Gap compared to industry average (e.g., "+33.7%", "-12.5%")
        /// Positive means above average, negative means below average
        /// </summary>
        public string GapVsIndustry { get; set; }

        /// <summary>
        /// Gap compared to top performer (e.g., "-29.4%")
        /// </summary>
        public string GapVsTopPerformer { get; set; }

        /// <summary>
        /// Performance status: "Above Average", "Average", "Below Average"
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Recommended actions to improve this metric
        /// </summary>
        public string Recommendation { get; set; }
    }
}
