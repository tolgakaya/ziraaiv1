using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Detailed breakdown of a specific disease for a crop
    /// </summary>
    public class DiseaseBreakdownDto
    {
        /// <summary>
        /// Disease name
        /// </summary>
        public string Disease { get; set; }

        /// <summary>
        /// Number of times this disease occurred
        /// </summary>
        public int Occurrences { get; set; }

        /// <summary>
        /// Percentage of total analyses for this crop
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Average severity level (e.g., "Low", "Moderate", "High")
        /// </summary>
        public string AverageSeverity { get; set; }

        /// <summary>
        /// Peak season for this disease (e.g., "May-June")
        /// </summary>
        public string SeasonalPeak { get; set; }

        /// <summary>
        /// Geographic regions most affected
        /// </summary>
        public List<string> AffectedRegions { get; set; }

        /// <summary>
        /// Recommended product categories for treatment
        /// </summary>
        public List<RecommendedProductDto> RecommendedProducts { get; set; }

        public DiseaseBreakdownDto()
        {
            AffectedRegions = new List<string>();
            RecommendedProducts = new List<RecommendedProductDto>();
        }
    }
}
