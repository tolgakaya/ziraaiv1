using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework
{
    public class UserSubscriptionRepository : EfEntityRepositoryBase<UserSubscription, ProjectDbContext>, IUserSubscriptionRepository
    {
        public UserSubscriptionRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<UserSubscription> GetActiveSubscriptionByUserIdAsync(int userId)
        {
            return await Context.UserSubscriptions
                .Include(x => x.SubscriptionTier)
                .FirstOrDefaultAsync(x => x.UserId == userId 
                    && x.IsActive 
                    && x.Status == "Active" 
                    && x.EndDate > DateTime.UtcNow);
        }

        public async Task<List<UserSubscription>> GetUserSubscriptionHistoryAsync(int userId)
        {
            return await Context.UserSubscriptions
                .Include(x => x.SubscriptionTier)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<UserSubscription> GetSubscriptionWithTierAsync(int subscriptionId)
        {
            return await Context.UserSubscriptions
                .Include(x => x.SubscriptionTier)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId);
        }

        public async Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(DateTime expiryDateThreshold)
        {
            return await Context.UserSubscriptions
                .Include(x => x.SubscriptionTier)
                .Where(x => x.IsActive 
                    && x.Status == "Active" 
                    && x.EndDate <= expiryDateThreshold 
                    && x.AutoRenew)
                .ToListAsync();
        }

        public async Task<bool> HasActiveSubscriptionAsync(int userId)
        {
            return await Context.UserSubscriptions
                .AnyAsync(x => x.UserId == userId 
                    && x.IsActive 
                    && x.Status == "Active" 
                    && x.EndDate > DateTime.UtcNow);
        }

        public async Task UpdateUsageCountersAsync(int subscriptionId, int dailyIncrement = 1, int monthlyIncrement = 1)
        {
            var subscription = await Context.UserSubscriptions.FindAsync(subscriptionId);
            if (subscription != null)
            {
                // Check if we need to reset daily counter
                if (subscription.LastUsageResetDate?.Date < DateTime.UtcNow.Date)
                {
                    subscription.CurrentDailyUsage = 0;
                    subscription.LastUsageResetDate = DateTime.UtcNow;
                }

                // Check if we need to reset monthly counter
                var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                if (subscription.MonthlyUsageResetDate == null || subscription.MonthlyUsageResetDate < currentMonth)
                {
                    subscription.CurrentMonthlyUsage = 0;
                    subscription.MonthlyUsageResetDate = currentMonth;
                }

                subscription.CurrentDailyUsage += dailyIncrement;
                subscription.CurrentMonthlyUsage += monthlyIncrement;
                subscription.UpdatedDate = DateTime.UtcNow;

                await Context.SaveChangesAsync();
            }
        }

        public async Task ResetDailyUsageAsync()
        {
            var activeSubscriptions = await Context.UserSubscriptions
                .Where(x => x.IsActive && x.Status == "Active")
                .ToListAsync();

            foreach (var subscription in activeSubscriptions)
            {
                subscription.CurrentDailyUsage = 0;
                subscription.LastUsageResetDate = DateTime.UtcNow;
            }

            await Context.SaveChangesAsync();
        }

        public async Task ResetMonthlyUsageAsync()
        {
            var activeSubscriptions = await Context.UserSubscriptions
                .Where(x => x.IsActive && x.Status == "Active")
                .ToListAsync();

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            
            foreach (var subscription in activeSubscriptions)
            {
                subscription.CurrentMonthlyUsage = 0;
                subscription.MonthlyUsageResetDate = currentMonth;
            }

            await Context.SaveChangesAsync();
        }
    }
}