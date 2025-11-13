using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Complete farmer journey analytics with timeline, behavioral patterns, and recommendations
    /// Tracks individual farmer's lifecycle from code redemption through ongoing engagement
    /// </summary>
    public class FarmerJourneyDto
    {
        /// <summary>
        /// Farmer's user ID
        /// </summary>
        public int FarmerId { get; set; }

        /// <summary>
        /// Farmer's full name
        /// </summary>
        public string FarmerName { get; set; }

        /// <summary>
        /// High-level journey summary statistics
        /// </summary>
        public JourneySummaryDto JourneySummary { get; set; }

        /// <summary>
        /// Chronological timeline of all farmer events
        /// Sorted by date descending (most recent first)
        /// </summary>
        public List<TimelineEventDto> Timeline { get; set; }

        /// <summary>
        /// Behavioral patterns and preferences discovered from journey data
        /// </summary>
        public BehavioralPatternsDto BehavioralPatterns { get; set; }

        /// <summary>
        /// AI-driven recommended actions for sponsor
        /// </summary>
        public List<string> RecommendedActions { get; set; }
    }
}
