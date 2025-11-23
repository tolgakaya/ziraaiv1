using System;
using System.ComponentModel.DataAnnotations;

namespace Entities.Dtos
{
    /// <summary>
    /// Request DTO for subscribing to a subscription tier
    /// This endpoint validates subscription eligibility and returns payment initialization instructions
    /// Actual subscription is created after successful payment via payment callback
    /// </summary>
    public class SubscribeRequestDto
    {
        /// <summary>
        /// Subscription tier ID to subscribe to
        /// </summary>
        [Required]
        public int SubscriptionTierId { get; set; }

        /// <summary>
        /// Subscription duration in months (1 for monthly, 12 for yearly)
        /// </summary>
        [Range(1, 12)]
        public int? DurationMonths { get; set; } = 1;

        /// <summary>
        /// Enable auto-renewal (optional, defaults to false for manual purchases)
        /// </summary>
        public bool AutoRenew { get; set; }

        /// <summary>
        /// Subscription start date (optional, defaults to now)
        /// </summary>
        public DateTime? StartDate { get; set; }
    }
}
