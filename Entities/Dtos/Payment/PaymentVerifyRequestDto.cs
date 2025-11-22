using System.ComponentModel.DataAnnotations;

namespace Entities.Dtos.Payment
{
    /// <summary>
    /// Request DTO for verifying a payment after user completes payment
    /// Called from mobile app deep link callback or web callback
    /// </summary>
    public class PaymentVerifyRequestDto
    {
        /// <summary>
        /// iyzico payment token (returned from initialize)
        /// </summary>
        [Required]
        public string PaymentToken { get; set; }
    }
}
