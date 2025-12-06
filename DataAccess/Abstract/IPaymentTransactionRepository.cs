using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for PaymentTransaction entity
    /// Provides data access methods for payment transaction operations
    /// </summary>
    public interface IPaymentTransactionRepository : IEntityRepository<PaymentTransaction>
    {
        /// <summary>
        /// Get payment transaction by iyzico token
        /// </summary>
        /// <param name="iyzicoToken">iyzico payment token (unique)</param>
        /// <returns>PaymentTransaction or null if not found</returns>
        Task<PaymentTransaction> GetByIyzicoTokenAsync(string iyzicoToken);

        /// <summary>
        /// Get payment transaction by conversation ID
        /// </summary>
        /// <param name="conversationId">Conversation ID (unique)</param>
        /// <returns>PaymentTransaction or null if not found</returns>
        Task<PaymentTransaction> GetByConversationIdAsync(string conversationId);

        /// <summary>
        /// Get payment transaction with related entities (User, SponsorshipPurchase, UserSubscription)
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <returns>PaymentTransaction with navigations or null</returns>
        Task<PaymentTransaction> GetWithRelationsAsync(int transactionId);

        /// <summary>
        /// Get all payment transactions for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of payment transactions</returns>
        Task<List<PaymentTransaction>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Get payment transactions by status
        /// </summary>
        /// <param name="status">Payment status (Initialized, Pending, Success, Failed, Expired)</param>
        /// <returns>List of payment transactions</returns>
        Task<List<PaymentTransaction>> GetByStatusAsync(string status);

        /// <summary>
        /// Get expired payment transactions (ExpiresAt < now AND Status = Initialized or Pending)
        /// </summary>
        /// <returns>List of expired transactions</returns>
        Task<List<PaymentTransaction>> GetExpiredTransactionsAsync();

        /// <summary>
        /// Get payment transactions by flow type
        /// </summary>
        /// <param name="flowType">Flow type (SponsorBulkPurchase, FarmerSubscription)</param>
        /// <param name="userId">Optional user ID filter</param>
        /// <returns>List of payment transactions</returns>
        Task<List<PaymentTransaction>> GetByFlowTypeAsync(string flowType, int? userId = null);

        /// <summary>
        /// Get successful payment transactions for a user (for analytics)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of successful transactions</returns>
        Task<List<PaymentTransaction>> GetSuccessfulTransactionsByUserAsync(int userId);

        /// <summary>
        /// Get total payment amount by user (successful transactions only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Total amount paid</returns>
        Task<decimal> GetTotalPaidAmountByUserAsync(int userId);

        /// <summary>
        /// Update transaction status
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="status">New status</param>
        /// <param name="errorMessage">Optional error message</param>
        Task UpdateStatusAsync(int transactionId, string status, string errorMessage = null);

        /// <summary>
        /// Mark transaction as completed
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="iyzicoPaymentId">iyzico payment ID</param>
        /// <param name="verifyResponse">Verify response JSON</param>
        Task MarkAsCompletedAsync(int transactionId, string iyzicoPaymentId, string verifyResponse);
    }
}
