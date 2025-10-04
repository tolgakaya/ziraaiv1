using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Tracks the complete referral journey from click to reward.
    /// Status flow: Clicked → Registered → Validated → Rewarded
    /// </summary>
    public class ReferralTracking : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The referral code that was used
        /// </summary>
        public int ReferralCodeId { get; set; }

        /// <summary>
        /// User who used the referral code (new user, referee)
        /// Nullable until registration completes
        /// </summary>
        public int? RefereeUserId { get; set; }

        /// <summary>
        /// When the referral link was first clicked
        /// </summary>
        public DateTime? ClickedAt { get; set; }

        /// <summary>
        /// When the referee completed registration
        /// </summary>
        public DateTime? RegisteredAt { get; set; }

        /// <summary>
        /// When the referee completed their first plant analysis (validation gate)
        /// </summary>
        public DateTime? FirstAnalysisAt { get; set; }

        /// <summary>
        /// When the reward was processed and credits awarded to referrer
        /// </summary>
        public DateTime? RewardProcessedAt { get; set; }

        /// <summary>
        /// Current status of this referral: 0=Clicked, 1=Registered, 2=Validated, 3=Rewarded
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Phone number of the referee (for tracking before registration)
        /// </summary>
        public string RefereeMobilePhone { get; set; }

        /// <summary>
        /// IP address of the click for anti-abuse tracking
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Device identifier for duplicate click prevention
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Reason if the referral failed or was rejected
        /// </summary>
        public string FailureReason { get; set; }
    }

    /// <summary>
    /// Referral tracking status enumeration
    /// </summary>
    public enum ReferralTrackingStatus
    {
        Clicked = 0,
        Registered = 1,
        Validated = 2,
        Rewarded = 3
    }
}
