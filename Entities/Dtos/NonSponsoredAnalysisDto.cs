using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for non-sponsored plant analysis with farmer information
    /// Used by admin to view analyses not associated with any sponsor
    /// </summary>
    public class NonSponsoredAnalysisDto
    {
        /// <summary>
        /// Plant analysis ID
        /// </summary>
        public int PlantAnalysisId { get; set; }

        /// <summary>
        /// Analysis date
        /// </summary>
        public DateTime AnalysisDate { get; set; }

        /// <summary>
        /// Analysis status (pending, completed, failed)
        /// </summary>
        public string AnalysisStatus { get; set; }

        /// <summary>
        /// Crop type analyzed
        /// </summary>
        public string CropType { get; set; }

        /// <summary>
        /// Location of the analysis
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// User ID who requested the analysis
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// User's full name
        /// </summary>
        public string UserFullName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// User's phone number
        /// </summary>
        public string UserPhone { get; set; }

        /// <summary>
        /// Image URL of the analyzed plant
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Overall health score (0-100)
        /// </summary>
        public int OverallHealthScore { get; set; }

        /// <summary>
        /// Primary concern identified in the analysis
        /// </summary>
        public string PrimaryConcern { get; set; }

        /// <summary>
        /// Indicates if analysis was created by admin on behalf of farmer
        /// </summary>
        public bool IsOnBehalfOf { get; set; }

        /// <summary>
        /// Admin ID who created this analysis (if IsOnBehalfOf is true)
        /// </summary>
        public int? CreatedByAdminId { get; set; }
    }
}
