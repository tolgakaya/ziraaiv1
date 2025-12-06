namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for subscription validation
    /// Contains payment initialization instructions for mobile/frontend teams
    /// </summary>
    public class SubscribeResponseDto
    {
        /// <summary>
        /// Subscription tier ID
        /// </summary>
        public int SubscriptionTierId { get; set; }

        /// <summary>
        /// Subscription tier internal name (e.g., "Trial", "S", "M", "L", "XL")
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// Subscription tier display name (e.g., "Deneme", "Küçük", "Orta", "Büyük", "Ekstra Büyük")
        /// </summary>
        public string TierDisplayName { get; set; }

        /// <summary>
        /// Total amount to be paid
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (default: "TRY")
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Subscription duration in months
        /// </summary>
        public int DurationMonths { get; set; }

        /// <summary>
        /// Next step instruction for mobile/frontend teams
        /// </summary>
        public string NextStep { get; set; }

        /// <summary>
        /// Payment initialization endpoint URL
        /// Mobile app should call this endpoint to initialize iyzico payment
        /// </summary>
        public string PaymentInitializeUrl { get; set; }

        /// <summary>
        /// Payment flow type for payment initialization request
        /// Value: "FarmerSubscription"
        /// </summary>
        public string PaymentFlowType { get; set; }
    }
}
