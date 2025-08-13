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
    public class SubscriptionUsageLogRepository : EfEntityRepositoryBase<SubscriptionUsageLog, ProjectDbContext>, ISubscriptionUsageLogRepository
    {
        public SubscriptionUsageLogRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<SubscriptionUsageLog>> GetUserUsageLogsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await Context.SubscriptionUsageLogs
                .Where(x => x.UserId == userId 
                    && x.UsageDate >= startDate 
                    && x.UsageDate <= endDate)
                .OrderByDescending(x => x.UsageDate)
                .ToListAsync();
        }

        public async Task<int> GetDailyUsageCountAsync(int userId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await Context.SubscriptionUsageLogs
                .CountAsync(x => x.UserId == userId 
                    && x.UsageDate >= startOfDay 
                    && x.UsageDate < endOfDay 
                    && x.IsSuccessful);
        }

        public async Task<int> GetMonthlyUsageCountAsync(int userId, int year, int month)
        {
            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await Context.SubscriptionUsageLogs
                .CountAsync(x => x.UserId == userId 
                    && x.UsageDate >= startOfMonth 
                    && x.UsageDate < endOfMonth 
                    && x.IsSuccessful);
        }

        public async Task<SubscriptionUsageLog> LogUsageAsync(SubscriptionUsageLog usageLog)
        {
            // Use DateTime.Now instead of DateTime.UtcNow to avoid timezone issues with PostgreSQL
            // Only set if not already provided by the caller
            if (usageLog.CreatedDate == default)
                usageLog.CreatedDate = DateTime.Now;
            if (usageLog.UsageDate == default)
                usageLog.UsageDate = DateTime.Now;
            
            Context.SubscriptionUsageLogs.Add(usageLog);
            await Context.SaveChangesAsync();
            
            return usageLog;
        }

        public async Task<Dictionary<string, int>> GetUsageStatisticsByEndpointAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await Context.SubscriptionUsageLogs
                .Where(x => x.UserId == userId 
                    && x.UsageDate >= startDate 
                    && x.UsageDate <= endDate 
                    && x.IsSuccessful)
                .GroupBy(x => x.RequestEndpoint)
                .Select(g => new { Endpoint = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Endpoint ?? "Unknown", x => x.Count);
        }
    }
}