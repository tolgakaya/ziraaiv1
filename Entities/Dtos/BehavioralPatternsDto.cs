using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Behavioral patterns and preferences discovered from farmer's journey data
    /// Used for personalized engagement and predictive recommendations
    /// </summary>
    public class BehavioralPatternsDto
    {
        /// <summary>
        /// Preferred time range for contact (e.g., "06:00-09:00")
        /// Determined by analysis of message response times
        /// </summary>
        public string PreferredContactTime { get; set; }

        /// <summary>
        /// Average number of days between consecutive analyses
        /// Used to predict next expected analysis date
        /// </summary>
        public decimal AverageDaysBetweenAnalyses { get; set; }

        /// <summary>
        /// Season with highest activity: Spring, Summer, Fall, Winter
        /// Based on analysis frequency patterns
        /// </summary>
        public string MostActiveSeason { get; set; }

        /// <summary>
        /// List of crops farmer most frequently analyzes
        /// Ordered by frequency descending
        /// </summary>
        public List<string> PreferredCrops { get; set; }

        /// <summary>
        /// Common issues farmer faces
        /// Example: "Fungal diseases", "Nutrient deficiency", "Pest infestation"
        /// </summary>
        public List<string> CommonIssues { get; set; }

        /// <summary>
        /// Percentage of sponsor messages that farmer responds to (0-100)
        /// </summary>
        public decimal MessageResponseRate { get; set; }

        /// <summary>
        /// Average response time to sponsor messages (in hours)
        /// </summary>
        public decimal AverageMessageResponseTimeHours { get; set; }

        /// <summary>
        /// Day of week with most analyses: Monday, Tuesday, etc.
        /// </summary>
        public string MostActiveWeekday { get; set; }

        /// <summary>
        /// Engagement trend over time: Increasing, Stable, Decreasing
        /// Based on last 90 days of activity
        /// </summary>
        public string EngagementTrend { get; set; }

        /// <summary>
        /// Churn risk score (0-100, higher = more risk)
        /// Based on activity patterns, subscription status, and engagement metrics
        /// </summary>
        public decimal ChurnRiskScore { get; set; }
    }
}
