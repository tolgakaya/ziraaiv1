using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for farmer segmentation analytics
    /// Provides behavioral segmentation of farmers into actionable groups
    /// </summary>
    public class FarmerSegmentationDto
    {
        /// <summary>
        /// List of farmer segments (Heavy Users, Regular Users, At-Risk, Dormant)
        /// </summary>
        public List<SegmentDto> Segments { get; set; }

        /// <summary>
        /// Total number of farmers analyzed
        /// </summary>
        public int TotalFarmers { get; set; }

        /// <summary>
        /// Sponsor ID for the analysis (null if admin view)
        /// </summary>
        public int? SponsorId { get; set; }

        /// <summary>
        /// Timestamp when the segmentation was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        public FarmerSegmentationDto()
        {
            Segments = new List<SegmentDto>();
            GeneratedAt = DateTime.Now;
        }
    }
}
