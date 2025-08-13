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
        Task<bool> HasActiveSubscriptionAsync(int userId);
        Task UpdateUsageCountersAsync(int subscriptionId, int dailyIncrement = 1, int monthlyIncrement = 1);
        Task ResetDailyUsageAsync();
        Task ResetMonthlyUsageAsync();
    }
}