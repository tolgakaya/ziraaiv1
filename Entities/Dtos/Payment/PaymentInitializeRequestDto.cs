using System.ComponentModel.DataAnnotations;

namespace Entities.Dtos.Payment
{
    /// <summary>
    /// Request DTO for initializing a payment transaction
    /// Used by both sponsor bulk purchase and farmer subscription flows
    /// </summary>
    public class PaymentInitializeRequestDto
    {
        /// <summary>
        /// Payment flow type: 'SponsorBulkPurchase' or 'FarmerSubscription'
        /// </summary>
        [Required]
        public string FlowType { get; set; }

        /// <summary>
        /// Flow-specific data (varies by flow type)
        /// For SponsorBulkPurchase: { SubscriptionTierId, Quantity }
        /// For FarmerSubscription: { SubscriptionTierId, Duration }
        /// </summary>
        [Required]
        public object FlowData { get; set; }

        /// <summary>
        /// Currency code (optional, defaults to TRY from configuration)
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Platform from which payment is initiated: "iOS", "Android", "Web"
        /// Used to determine callback redirect URL (deep link for mobile, web URL for web)
        /// Defaults to "iOS" for backward compatibility
        /// </summary>
        public string Platform { get; set; } = "iOS";
    }

    /// <summary>
    /// Flow-specific data for sponsor bulk purchase
    /// </summary>
    public class SponsorBulkPurchaseFlowData
    {
        [Required]
        public int SubscriptionTierId { get; set; }

        [Required]
        [Range(1, 10000)]
        public int Quantity { get; set; }

        /// <summary>
        /// Company name for invoice (optional for personal purchases)
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Tax number for invoice (optional for personal purchases)
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// Invoice address (optional for personal purchases)
        /// </summary>
        public string InvoiceAddress { get; set; }
    }

    /// <summary>
    /// Flow-specific data for farmer subscription
    /// </summary>
    public class FarmerSubscriptionFlowData
    {
        [Required]
        public int SubscriptionTierId { get; set; }

        /// <summary>
        /// Subscription duration in months
        /// </summary>
        [Range(1, 12)]
        public int DurationMonths { get; set; } = 1;
    }
}
