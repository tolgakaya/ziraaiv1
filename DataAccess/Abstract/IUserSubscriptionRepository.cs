using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    public interface IUserSubscriptionRepository : IEntityRepository<UserSubscription>
    {
        Task<UserSubscription> GetActiveSubscriptionByUserIdAsync(int userId);
        Task<List<UserSubscription>> GetUserSubscriptionHistoryAsync(int userId);
        Task<UserSubscription> GetSubscriptionWithTierAsync(int subscriptionId);
        Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(DateTime expiryDateThreshold);
        /// <summary>
        /// Gets the user's active non-trial subscription (paid subscriptions only)
        /// Returns CreditCard, BankTransfer, or Sponsorship subscriptions (NOT Trial)
        /// Orders by priority: CreditCard > BankTransfer > Sponsorship > Others
        /// Returns null if only trial subscription exists or no active subscription
        /// </summary>
        Task<UserSubscription> GetActiveNonTrialSubscriptionAsync(int userId);

        /// <summary>
        /// Gets ALL active subscriptions for a user (for validation and debugging)
        /// Useful for detecting multiple active subscription conflicts
        /// </summary>
        Task<List<UserSubscription>> GetAllActiveSubscriptionsAsync(int userId);

        Task<bool> HasActiveSubscriptionAsync(int userId);
        Task UpdateUsageCountersAsync(int subscriptionId, int dailyIncrement = 1, int monthlyIncrement = 1);
        Task ResetDailyUsageAsync();
        Task ResetMonthlyUsageAsync();
    }
}