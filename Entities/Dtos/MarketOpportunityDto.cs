namespace Entities.Dtos
{
    /// <summary>
    /// Market opportunity identified from crop-disease correlation analysis
    /// </summary>
    public class MarketOpportunityDto
    {
        /// <summary>
        /// Crop-disease combination (e.g., "Domates + Alternaria")
        /// </summary>
        public string Combination { get; set; }

        /// <summary>
        /// Total number of cases for this combination
        /// </summary>
        public int TotalCases { get; set; }

        /// <summary>
        /// Average severity level across all cases
        /// </summary>
        public string AverageSeverity { get; set; }

        /// <summary>
        /// Geographic concentration description
        /// </summary>
        public string GeographicConcentration { get; set; }

        /// <summary>
        /// Estimated market value for this opportunity
        /// </summary>
        public decimal MarketValue { get; set; }

        /// <summary>
        /// Actionable business insight for sponsors
        /// </summary>
        public string ActionableInsight { get; set; }
    }
}
