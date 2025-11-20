using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for message engagement analytics
    /// Tracks sponsor-farmer messaging effectiveness and patterns
    /// </summary>
    public class MessageEngagementDto
    {
        /// <summary>
        /// Total messages sent by sponsor
        /// </summary>
        public int TotalMessagesSent { get; set; }

        /// <summary>
        /// Total messages received from farmers
        /// </summary>
        public int TotalMessagesReceived { get; set; }

        /// <summary>
        /// Percentage of messages that received responses (0-100)
        /// </summary>
        public decimal ResponseRate { get; set; }

        /// <summary>
        /// Average time to receive response in hours
        /// </summary>
        public decimal AverageResponseTime { get; set; }

        /// <summary>
        /// Overall engagement score (0-10) based on response rate and speed
        /// </summary>
        public decimal EngagementScore { get; set; }

        /// <summary>
        /// Message breakdown by type
        /// </summary>
        public MessageBreakdownDto MessageBreakdown { get; set; }

        /// <summary>
        /// Best performing message templates
        /// </summary>
        public List<MessageTemplatePerformanceDto> BestPerformingMessages { get; set; }

        /// <summary>
        /// Time-of-day analysis for message effectiveness
        /// </summary>
        public Dictionary<string, TimeSlotAnalysisDto> TimeOfDayAnalysis { get; set; }

        /// <summary>
        /// Sponsor ID for the analysis (null if admin view)
        /// </summary>
        public int? SponsorId { get; set; }

        /// <summary>
        /// Timestamp when the analysis was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        public MessageEngagementDto()
        {
            MessageBreakdown = new MessageBreakdownDto();
            BestPerformingMessages = new List<MessageTemplatePerformanceDto>();
            TimeOfDayAnalysis = new Dictionary<string, TimeSlotAnalysisDto>();
            GeneratedAt = DateTime.Now;
        }
    }
}
