namespace Entities.Concrete
{
    /// <summary>
    /// Represents the status of a conversation between sponsor and farmer
    /// </summary>
    public enum ConversationStatus
    {
        /// <summary>
        /// No messages have been sent yet
        /// </summary>
        NoContact = 0,

        /// <summary>
        /// Sponsor sent message(s), waiting for farmer reply
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Two-way conversation with recent activity (within 7 days)
        /// </summary>
        Active = 2,

        /// <summary>
        /// Conversation exists but no recent activity (7+ days)
        /// </summary>
        Idle = 3
    }
}
