namespace Entities.Dtos.Payment
{
    /// <summary>
    /// Response DTO for payment verification
    /// Contains final payment status and transaction details
    /// </summary>
    public class PaymentVerifyResponseDto
    {
        /// <summary>
        /// Payment transaction ID (our database ID)
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Payment status: Success, Failed, Pending, Expired
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// iyzico payment ID (unique identifier from iyzico)
        /// </summary>
        public string PaymentId { get; set; }

        /// <summary>
        /// Payment token
        /// </summary>
        public string PaymentToken { get; set; }

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Paid amount (actual amount charged)
        /// </summary>
        public decimal PaidAmount { get; set; }

        /// <summary>
        /// Payment completion timestamp (ISO 8601)
        /// </summary>
        public string CompletedAt { get; set; }

        /// <summary>
        /// Error message if payment failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Flow type (for client routing)
        /// </summary>
        public string FlowType { get; set; }

        /// <summary>
        /// Flow-specific result data
        /// For SponsorBulkPurchase: { PurchaseId, CodesGenerated }
        /// For FarmerSubscription: { SubscriptionId, EndDate }
        /// </summary>
        public object FlowResult { get; set; }
    }

    /// <summary>
    /// Flow result for sponsor bulk purchase
    /// </summary>
    public class SponsorBulkPurchaseResult
    {
        public int PurchaseId { get; set; }
        public int CodesGenerated { get; set; }
        public string SubscriptionTierName { get; set; }
    }

    /// <summary>
    /// Flow result for farmer subscription
    /// </summary>
    public class FarmerSubscriptionResult
    {
        public int SubscriptionId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string SubscriptionTierName { get; set; }
    }
}
