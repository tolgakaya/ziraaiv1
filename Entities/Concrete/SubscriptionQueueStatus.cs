namespace Entities.Concrete
{
    /// <summary>
    /// Queue status for sponsorship subscriptions
    /// Used to manage the sponsorship queue system where users can only have one active sponsorship at a time
    /// </summary>
    public enum SubscriptionQueueStatus
    {
        /// <summary>
        /// Subscription is queued, waiting for previous sponsorship to expire
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Subscription is currently active and usable
        /// </summary>
        Active = 1,

        /// <summary>
        /// Subscription has expired (past end date)
        /// </summary>
        Expired = 2,

        /// <summary>
        /// Subscription was manually cancelled by user or admin
        /// </summary>
        Cancelled = 3
    }
}
