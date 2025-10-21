using Entities.Concrete;
using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO containing messaging status information for a sponsor-farmer conversation
    /// </summary>
    public class MessagingStatusDto
    {
        /// <summary>
        /// Whether any messages exist in this conversation
        /// </summary>
        public bool HasMessages { get; set; }

        /// <summary>
        /// Total number of messages exchanged (both directions)
        /// </summary>
        public int TotalMessageCount { get; set; }

        /// <summary>
        /// Number of unread messages from farmer to sponsor
        /// </summary>
        public int UnreadCount { get; set; }

        /// <summary>
        /// Date/time of the most recent message (either direction)
        /// </summary>
        public DateTime? LastMessageDate { get; set; }

        /// <summary>
        /// Preview of last message (first 50 characters)
        /// </summary>
        public string LastMessagePreview { get; set; }

        /// <summary>
        /// Who sent the last message: "sponsor" or "farmer"
        /// </summary>
        public string LastMessageBy { get; set; }

        /// <summary>
        /// Whether farmer has sent at least one reply
        /// </summary>
        public bool HasFarmerResponse { get; set; }

        /// <summary>
        /// Date/time of farmer's most recent message
        /// </summary>
        public DateTime? LastFarmerResponseDate { get; set; }

        /// <summary>
        /// Current status of the conversation
        /// </summary>
        public ConversationStatus ConversationStatus { get; set; }
    }
}
