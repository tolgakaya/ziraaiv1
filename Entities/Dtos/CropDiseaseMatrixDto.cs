using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for crop-disease correlation matrix analytics
    /// Provides analysis of disease patterns across different crop types
    /// </summary>
    public class CropDiseaseMatrixDto
    {
        /// <summary>
        /// Matrix of crop types with their disease breakdowns
        /// </summary>
        public List<CropAnalysisDto> Matrix { get; set; }

        /// <summary>
        /// Top market opportunities based on disease-crop combinations
        /// </summary>
        public List<MarketOpportunityDto> TopOpportunities { get; set; }

        /// <summary>
        /// Sponsor ID for the analysis (null if admin view)
        /// </summary>
        public int? SponsorId { get; set; }

        /// <summary>
        /// Timestamp when the matrix was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        public CropDiseaseMatrixDto()
        {
            Matrix = new List<CropAnalysisDto>();
            TopOpportunities = new List<MarketOpportunityDto>();
            GeneratedAt = DateTime.Now;
        }
    }
}
