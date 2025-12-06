namespace Entities.Dtos.Payment
{
    /// <summary>
    /// Response DTO for payment initialization
    /// Contains payment page URL and transaction details
    /// </summary>
    public class PaymentInitializeResponseDto
    {
        /// <summary>
        /// Payment transaction ID (our database ID)
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// iyzico payment token (unique identifier)
        /// </summary>
        public string PaymentToken { get; set; }

        /// <summary>
        /// Payment page URL (iyzico hosted payment page)
        /// Mobile app should open this URL in WebView or browser
        /// </summary>
        public string PaymentPageUrl { get; set; }

        /// <summary>
        /// Deep link URL for returning to app after payment
        /// Format: ziraai://payment-callback?token={token}
        /// </summary>
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Payment amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Payment token expiration timestamp (ISO 8601)
        /// </summary>
        public string ExpiresAt { get; set; }

        /// <summary>
        /// Payment status (should be 'Initialized')
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Conversation ID (for tracking)
        /// </summary>
        public string ConversationId { get; set; }
    }
}
