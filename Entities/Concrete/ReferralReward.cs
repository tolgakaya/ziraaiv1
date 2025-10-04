using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Records of referral rewards (analysis credits) awarded to referrers.
    /// Credits are unlimited and never expire (per user requirements).
    /// </summary>
    public class ReferralReward : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Link to the tracking record that triggered this reward
        /// </summary>
        public int ReferralTrackingId { get; set; }

        /// <summary>
        /// User who receives the reward (referrer, original code owner)
        /// </summary>
        public int ReferrerUserId { get; set; }

        /// <summary>
        /// User who triggered the reward (referee, new user)
        /// </summary>
        public int RefereeUserId { get; set; }

        /// <summary>
        /// Number of analysis credits awarded (configurable, default: 10)
        /// Retrieved from ReferralConfigurations.CreditPerReferral
        /// </summary>
        public int CreditAmount { get; set; }

        /// <summary>
        /// When the reward was awarded
        /// </summary>
        public DateTime AwardedAt { get; set; }

        /// <summary>
        /// The subscription that received the credits
        /// Nullable to handle edge cases where subscription might be deleted
        /// </summary>
        public int? SubscriptionId { get; set; }

        /// <summary>
        /// When these credits expire (NULL = never expires per user requirements)
        /// Reserved for future use if expiry policy changes
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
