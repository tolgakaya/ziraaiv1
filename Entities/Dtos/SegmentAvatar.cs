namespace Entities.Dtos
{
    /// <summary>
    /// Represents a typical farmer profile for a segment
    /// Provides a concrete example to help sponsors understand the segment
    /// </summary>
    public class SegmentAvatar
    {
        /// <summary>
        /// Descriptive profile of a typical farmer in this segment
        /// Example: "Ahmet from Izmir, grows tomatoes, analyzes 8-10 times/month"
        /// </summary>
        public string Profile { get; set; }

        /// <summary>
        /// Typical behavioral pattern
        /// Example: "Analyzes during planting season (March-May), responds to messages within 24h"
        /// </summary>
        public string BehaviorPattern { get; set; }

        /// <summary>
        /// Typical pain points or needs
        /// Example: "Needs proactive disease prevention advice, struggles with fungal infections"
        /// </summary>
        public string PainPoints { get; set; }

        /// <summary>
        /// Engagement style with sponsor content
        /// Example: "Reads all messages, clicks product links, asks follow-up questions"
        /// </summary>
        public string EngagementStyle { get; set; }
    }
}
