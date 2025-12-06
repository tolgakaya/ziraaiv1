using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    /// <summary>
    /// Payment transaction tracking for both sponsor bulk purchases and farmer subscriptions
    /// Integrates with iyzico payment gateway (PWI - Pay With iyzico)
    /// </summary>
    public class PaymentTransaction : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// User initiating the payment (Sponsor or Farmer)
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Payment flow type: 'SponsorBulkPurchase' or 'FarmerSubscription'
        /// </summary>
        public string FlowType { get; set; }

        /// <summary>
        /// Flow-specific data stored as JSON
        /// For SponsorBulkPurchase: SubscriptionTierId, Quantity, PackageDetails
        /// For FarmerSubscription: SubscriptionTierId, Duration, etc.
        /// </summary>
        public string FlowDataJson { get; set; }

        /// <summary>
        /// Foreign key to SponsorshipPurchase (for sponsor flow)
        /// </summary>
        public int? SponsorshipPurchaseId { get; set; }

        /// <summary>
        /// Foreign key to UserSubscription (for farmer flow)
        /// </summary>
        public int? UserSubscriptionId { get; set; }

        /// <summary>
        /// Unique iyzico payment token (returned from initialize PWI)
        /// Used for payment page URL and verification
        /// </summary>
        public string IyzicoToken { get; set; }

        /// <summary>
        /// iyzico payment ID (returned after successful payment verification)
        /// </summary>
        public string IyzicoPaymentId { get; set; }

        /// <summary>
        /// Unique conversation ID for this transaction (for iyzico API calls)
        /// Format: {FlowType}_{UserId}_{Timestamp}
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Payment amount (total price)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (TRY, USD, EUR)
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Payment status: Initialized, Pending, Success, Failed, Expired
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Platform from which payment was initiated: "iOS", "Android", "Web"
        /// Used to determine callback redirect URL (deep link for mobile, web URL for web)
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// When payment was initialized (PWI request sent)
        /// </summary>
        public DateTime InitializedAt { get; set; }

        /// <summary>
        /// When payment was completed (verified successfully)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Payment expiration timestamp (token validity)
        /// Default: InitializedAt + 30 minutes
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Full iyzico initialize PWI response (JSON)
        /// Stored for debugging and audit purposes
        /// </summary>
        public string InitializeResponse { get; set; }

        /// <summary>
        /// Full iyzico verify payment response (JSON)
        /// Stored for debugging and audit purposes
        /// </summary>
        public string VerifyResponse { get; set; }

        /// <summary>
        /// Error message if payment failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Audit: Record creation timestamp
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Audit: Last update timestamp
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        // Navigation properties

        /// <summary>
        /// User who initiated the payment
        /// </summary>
        [JsonIgnore]
        public virtual User User { get; set; }

        /// <summary>
        /// Related sponsorship purchase (if sponsor flow)
        /// </summary>
        [JsonIgnore]
        public virtual SponsorshipPurchase SponsorshipPurchase { get; set; }

        /// <summary>
        /// Related user subscription (if farmer flow)
        /// </summary>
        [JsonIgnore]
        public virtual UserSubscription UserSubscription { get; set; }
    }

    /// <summary>
    /// Payment flow types
    /// </summary>
    public static class PaymentFlowType
    {
        public const string SponsorBulkPurchase = "SponsorBulkPurchase";
        public const string FarmerSubscription = "FarmerSubscription";
    }

    /// <summary>
    /// Payment status values
    /// </summary>
    public static class PaymentStatus
    {
        /// <summary>
        /// Payment initialized, PWI token generated, waiting for user to pay
        /// </summary>
        public const string Initialized = "Initialized";

        /// <summary>
        /// User is on payment page, payment in progress
        /// </summary>
        public const string Pending = "Pending";

        /// <summary>
        /// Payment completed and verified successfully
        /// </summary>
        public const string Success = "Success";

        /// <summary>
        /// Payment failed (declined card, insufficient funds, etc.)
        /// </summary>
        public const string Failed = "Failed";

        /// <summary>
        /// Payment token expired (user did not complete payment within timeout)
        /// </summary>
        public const string Expired = "Expired";
    }

    /// <summary>
    /// Platform types for payment initialization
    /// </summary>
    public static class PaymentPlatform
    {
        /// <summary>
        /// iOS mobile app
        /// </summary>
        public const string iOS = "iOS";

        /// <summary>
        /// Android mobile app
        /// </summary>
        public const string Android = "Android";

        /// <summary>
        /// Web application (Angular)
        /// </summary>
        public const string Web = "Web";

        /// <summary>
        /// Get all valid platforms
        /// </summary>
        public static string[] ValidPlatforms => new[] { iOS, Android, Web };
    }
}
