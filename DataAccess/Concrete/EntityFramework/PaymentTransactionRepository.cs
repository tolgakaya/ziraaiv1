using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Entity Framework implementation of IPaymentTransactionRepository
    /// Handles all payment transaction data access operations
    /// </summary>
    public class PaymentTransactionRepository : EfEntityRepositoryBase<PaymentTransaction, ProjectDbContext>, IPaymentTransactionRepository
    {
        public PaymentTransactionRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<PaymentTransaction> GetByIyzicoTokenAsync(string iyzicoToken)
        {
            return await Context.PaymentTransactions
                .Include(pt => pt.User)
                .FirstOrDefaultAsync(pt => pt.IyzicoToken == iyzicoToken);
        }

        public async Task<PaymentTransaction> GetByConversationIdAsync(string conversationId)
        {
            return await Context.PaymentTransactions
                .Include(pt => pt.User)
                .FirstOrDefaultAsync(pt => pt.ConversationId == conversationId);
        }

        public async Task<PaymentTransaction> GetWithRelationsAsync(int transactionId)
        {
            return await Context.PaymentTransactions
                .Include(pt => pt.User)
                .Include(pt => pt.SponsorshipPurchase)
                .Include(pt => pt.UserSubscription)
                .FirstOrDefaultAsync(pt => pt.Id == transactionId);
        }

        public async Task<List<PaymentTransaction>> GetByUserIdAsync(int userId)
        {
            return await Context.PaymentTransactions
                .Where(pt => pt.UserId == userId)
                .OrderByDescending(pt => pt.InitializedAt)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetByStatusAsync(string status)
        {
            return await Context.PaymentTransactions
                .Include(pt => pt.User)
                .Where(pt => pt.Status == status)
                .OrderByDescending(pt => pt.InitializedAt)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetExpiredTransactionsAsync()
        {
            var now = DateTime.Now;

            return await Context.PaymentTransactions
                .Where(pt => pt.ExpiresAt < now &&
                            (pt.Status == PaymentStatus.Initialized || pt.Status == PaymentStatus.Pending))
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetByFlowTypeAsync(string flowType, int? userId = null)
        {
            var query = Context.PaymentTransactions
                .Include(pt => pt.User)
                .Where(pt => pt.FlowType == flowType);

            if (userId.HasValue)
            {
                query = query.Where(pt => pt.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(pt => pt.InitializedAt)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetSuccessfulTransactionsByUserAsync(int userId)
        {
            return await Context.PaymentTransactions
                .Include(pt => pt.SponsorshipPurchase)
                .Include(pt => pt.UserSubscription)
                .Where(pt => pt.UserId == userId && pt.Status == PaymentStatus.Success)
                .OrderByDescending(pt => pt.CompletedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalPaidAmountByUserAsync(int userId)
        {
            return await Context.PaymentTransactions
                .Where(pt => pt.UserId == userId && pt.Status == PaymentStatus.Success)
                .SumAsync(pt => pt.Amount);
        }

        public async Task UpdateStatusAsync(int transactionId, string status, string errorMessage = null)
        {
            var transaction = await Context.PaymentTransactions.FindAsync(transactionId);

            if (transaction != null)
            {
                transaction.Status = status;
                transaction.UpdatedDate = DateTime.Now;

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    transaction.ErrorMessage = errorMessage;
                }

                // If status is Failed or Expired, don't set CompletedAt
                // CompletedAt should only be set for Success status
                if (status == PaymentStatus.Failed || status == PaymentStatus.Expired)
                {
                    transaction.CompletedAt = null;
                }

                Context.PaymentTransactions.Update(transaction);
                await Context.SaveChangesAsync();
            }
        }

        public async Task MarkAsCompletedAsync(int transactionId, string iyzicoPaymentId, string verifyResponse)
        {
            var transaction = await Context.PaymentTransactions.FindAsync(transactionId);

            if (transaction != null)
            {
                transaction.Status = PaymentStatus.Success;
                transaction.IyzicoPaymentId = iyzicoPaymentId;
                transaction.VerifyResponse = verifyResponse;
                transaction.CompletedAt = DateTime.Now;
                transaction.UpdatedDate = DateTime.Now;

                Context.PaymentTransactions.Update(transaction);
                await Context.SaveChangesAsync();
            }
        }
    }
}
