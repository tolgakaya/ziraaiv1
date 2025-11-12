using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Analysis of a specific crop type with disease breakdown
    /// </summary>
    public class CropAnalysisDto
    {
        /// <summary>
        /// Crop type name (e.g., "Domates", "Biber")
        /// </summary>
        public string CropType { get; set; }

        /// <summary>
        /// Total number of analyses for this crop
        /// </summary>
        public int TotalAnalyses { get; set; }

        /// <summary>
        /// Breakdown of diseases affecting this crop
        /// </summary>
        public List<DiseaseBreakdownDto> DiseaseBreakdown { get; set; }

        public CropAnalysisDto()
        {
            DiseaseBreakdown = new List<DiseaseBreakdownDto>();
        }
    }
}
