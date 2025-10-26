using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Comprehensive messaging analytics for sponsors
    /// Provides detailed insights into sponsor-farmer messaging interactions
    /// Cache TTL: 15 minutes
    /// Authorization: Sponsor, Admin
    /// </summary>
    public class SponsorMessagingAnalyticsDto
    {
        /// <summary>
        /// Total number of messages sent by the sponsor
        /// </summary>
        public int TotalMessagesSent { get; set; }

        /// <summary>
        /// Total number of messages received from farmers
        /// </summary>
        public int TotalMessagesReceived { get; set; }

        /// <summary>
        /// Current count of unread messages from farmers
        /// </summary>
        public int UnreadMessagesCount { get; set; }

        /// <summary>
        /// Average response time from sponsor to farmer messages in hours
        /// Calculated from time between farmer message and sponsor's first response
        /// </summary>
        public double AverageResponseTimeHours { get; set; }

        /// <summary>
        /// Percentage of farmer messages that received a sponsor response
        /// Formula: (MessagesWithResponse / TotalMessagesReceived) * 100
        /// </summary>
        public double ResponseRate { get; set; }

        /// <summary>
        /// Total number of unique conversations (distinct AnalysisId values)
        /// </summary>
        public int TotalConversations { get; set; }

        /// <summary>
        /// Number of conversations with messages in last 7 days
        /// </summary>
        public int ActiveConversations { get; set; }

        /// <summary>
        /// Count of text-only messages
        /// </summary>
        public int TextMessageCount { get; set; }

        /// <summary>
        /// Count of voice messages
        /// </summary>
        public int VoiceMessageCount { get; set; }

        /// <summary>
        /// Count of messages with attachments (images, files)
        /// </summary>
        public int AttachmentCount { get; set; }

        /// <summary>
        /// Average message rating from farmers (1-5 scale)
        /// Null if no ratings exist
        /// </summary>
        public double? AverageMessageRating { get; set; }

        /// <summary>
        /// Count of messages with rating >= 4
        /// </summary>
        public int PositiveRatingsCount { get; set; }

        /// <summary>
        /// Top 10 most active conversations sorted by message volume
        /// Includes farmer info based on tier-based privacy rules
        /// </summary>
        public List<ConversationSummary> MostActiveConversations { get; set; }

        /// <summary>
        /// Date range for the analytics data
        /// </summary>
        public DateTime DataStartDate { get; set; }

        /// <summary>
        /// End date for the analytics data
        /// </summary>
        public DateTime DataEndDate { get; set; }

        public SponsorMessagingAnalyticsDto()
        {
            MostActiveConversations = new List<ConversationSummary>();
        }
    }

    /// <summary>
    /// Summary of a single conversation between sponsor and farmer
    /// Privacy: Farmer details shown based on sponsor tier (S/M = Anonymous, L/XL = Full details)
    /// </summary>
    public class ConversationSummary
    {
        /// <summary>
        /// Plant analysis ID (conversation identifier)
        /// </summary>
        public int AnalysisId { get; set; }

        /// <summary>
        /// Farmer's user ID
        /// Always shown regardless of tier
        /// </summary>
        public int FarmerId { get; set; }

        /// <summary>
        /// Farmer's full name
        /// Shown for L/XL tiers, "Anonymous Farmer" for S/M tiers
        /// </summary>
        public string FarmerName { get; set; }

        /// <summary>
        /// Total messages in this conversation
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Messages sent by sponsor in this conversation
        /// </summary>
        public int SponsorMessageCount { get; set; }

        /// <summary>
        /// Messages sent by farmer in this conversation
        /// </summary>
        public int FarmerMessageCount { get; set; }

        /// <summary>
        /// Date of the last message in this conversation
        /// </summary>
        public DateTime LastMessageDate { get; set; }

        /// <summary>
        /// Indicates if conversation has unread messages from farmer
        /// </summary>
        public bool HasUnreadMessages { get; set; }

        /// <summary>
        /// Plant/crop type from the analysis
        /// </summary>
        public string CropType { get; set; }

        /// <summary>
        /// Disease detected in the analysis (if any)
        /// </summary>
        public string Disease { get; set; }

        /// <summary>
        /// Average rating for messages in this conversation
        /// Null if no ratings exist
        /// </summary>
        public double? AverageRating { get; set; }
    }
}
