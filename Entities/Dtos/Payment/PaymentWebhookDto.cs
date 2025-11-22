namespace Entities.Dtos.Payment
{
    /// <summary>
    /// DTO for iyzico webhook callback
    /// iyzico sends POST request to our webhook endpoint after payment status changes
    /// </summary>
    public class PaymentWebhookDto
    {
        /// <summary>
        /// iyzico payment token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Payment status from iyzico
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// iyzico payment ID
        /// </summary>
        public string PaymentId { get; set; }

        /// <summary>
        /// Conversation ID (our tracking ID)
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// Payment amount
        /// </summary>
        public string Price { get; set; }

        /// <summary>
        /// Paid amount
        /// </summary>
        public string PaidPrice { get; set; }

        /// <summary>
        /// Currency code
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Timestamp of webhook (ISO 8601)
        /// </summary>
        public string PaymentCreatedDate { get; set; }

        /// <summary>
        /// Error code if payment failed
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Error message if payment failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
