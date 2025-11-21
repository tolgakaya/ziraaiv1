using Core.Utilities.Results;
using Entities.Dtos.Payment;
using System.Threading.Tasks;

namespace Business.Services.Payment
{
    /// <summary>
    /// Service interface for iyzico payment gateway integration
    /// Implements PWI (Pay With iyzico) flow for mobile payments
    /// </summary>
    public interface IIyzicoPaymentService
    {
        /// <summary>
        /// Initialize PWI (Pay With iyzico) payment
        /// Creates payment transaction and returns payment page URL for mobile app
        /// </summary>
        /// <param name="userId">User ID initiating the payment</param>
        /// <param name="request">Payment initialization request with flow type and data</param>
        /// <returns>Payment page URL, token, and transaction details</returns>
        Task<IDataResult<PaymentInitializeResponseDto>> InitializePaymentAsync(int userId, PaymentInitializeRequestDto request);

        /// <summary>
        /// Verify payment after user completes payment on iyzico page
        /// Called from mobile app callback or webhook
        /// </summary>
        /// <param name="request">Verification request with payment token</param>
        /// <returns>Payment verification result with final status</returns>
        Task<IDataResult<PaymentVerifyResponseDto>> VerifyPaymentAsync(PaymentVerifyRequestDto request);

        /// <summary>
        /// Process webhook callback from iyzico
        /// iyzico sends POST request when payment status changes
        /// </summary>
        /// <param name="webhook">Webhook data from iyzico</param>
        /// <returns>Processing result</returns>
        Task<IResult> ProcessWebhookAsync(PaymentWebhookDto webhook);

        /// <summary>
        /// Mark expired transactions as Expired status
        /// Should be called by background job periodically
        /// </summary>
        /// <returns>Number of transactions marked as expired</returns>
        Task<IDataResult<int>> MarkExpiredTransactionsAsync();

        /// <summary>
        /// Get payment transaction details by token
        /// </summary>
        /// <param name="token">iyzico payment token</param>
        /// <returns>Payment transaction details</returns>
        Task<IDataResult<PaymentVerifyResponseDto>> GetPaymentStatusAsync(string token);
    }
}
