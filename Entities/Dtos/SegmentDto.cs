using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Represents a behavioral segment of farmers
    /// </summary>
    public class SegmentDto
    {
        /// <summary>
        /// Segment name: Heavy Users, Regular Users, At-Risk Users, Dormant Users
        /// </summary>
        public string SegmentName { get; set; }

        /// <summary>
        /// Number of farmers in this segment
        /// </summary>
        public int FarmerCount { get; set; }

        /// <summary>
        /// Percentage of total farmers in this segment (0-100)
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Statistical characteristics of this segment
        /// </summary>
        public SegmentCharacteristics Characteristics { get; set; }

        /// <summary>
        /// Typical farmer profile for this segment
        /// </summary>
        public SegmentAvatar FarmerAvatar { get; set; }

        /// <summary>
        /// List of farmer IDs in this segment (for drill-down analysis)
        /// </summary>
        public List<int> FarmerIds { get; set; }

        /// <summary>
        /// Recommended actions for this segment
        /// </summary>
        public List<string> RecommendedActions { get; set; }

        public SegmentDto()
        {
            FarmerIds = new List<int>();
            RecommendedActions = new List<string>();
        }
    }
}
