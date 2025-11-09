using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for detailed non-sponsored farmer information
    /// Used by admin to analyze farmer profile and identify sponsorship opportunities
    /// </summary>
    public class NonSponsoredFarmerDetailDto
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Farmer's full name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Mobile phone number
        /// </summary>
        public string MobilePhone { get; set; }

        /// <summary>
        /// Account status
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// Registration date
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// Total number of analyses
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
        /// Date of first analysis
        /// </summary>
        public DateTime? FirstAnalysisDate { get; set; }

        /// <summary>
        /// Date of most recent analysis
        /// </summary>
        public DateTime? LastAnalysisDate { get; set; }

        /// <summary>
        /// Average health score across all analyses
        /// </summary>
        public int AverageHealthScore { get; set; }

        /// <summary>
        /// List of crop types analyzed by this farmer
        /// </summary>
        public List<string> CropTypes { get; set; }

        /// <summary>
        /// Most common concerns identified in analyses
        /// </summary>
        public List<string> CommonConcerns { get; set; }

        /// <summary>
        /// Recent analyses (last 5)
        /// </summary>
        public List<NonSponsoredAnalysisDto> RecentAnalyses { get; set; }
    }
}
