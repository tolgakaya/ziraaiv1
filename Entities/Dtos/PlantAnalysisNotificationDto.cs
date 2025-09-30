using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for real-time plant analysis completion notifications
    /// Sent via SignalR when async analysis completes
    /// </summary>
    public class PlantAnalysisNotificationDto
    {
        /// <summary>
        /// ID of the completed analysis
        /// </summary>
        public int AnalysisId { get; set; }

        /// <summary>
        /// ID of the user who requested the analysis
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Current status of the analysis (e.g., "Completed", "Failed")
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Timestamp when the analysis was completed
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Type of crop analyzed (e.g., "Tomato", "Wheat")
        /// </summary>
        public string CropType { get; set; }

        /// <summary>
        /// Primary concern identified in the analysis
        /// </summary>
        public string PrimaryConcern { get; set; }

        /// <summary>
        /// Overall health score (0-100)
        /// </summary>
        public int? OverallHealthScore { get; set; }

        /// <summary>
        /// URL to the analyzed plant image
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Deep link for mobile app navigation (e.g., "app://analysis/123")
        /// </summary>
        public string DeepLink { get; set; }

        /// <summary>
        /// Sponsor ID if this analysis is sponsored
        /// </summary>
        public string SponsorId { get; set; }

        /// <summary>
        /// Additional message to display to the user
        /// </summary>
        public string Message { get; set; }
    }
}