using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    public interface ISubscriptionUsageLogRepository : IEntityRepository<SubscriptionUsageLog>
    {
        Task<List<SubscriptionUsageLog>> GetUserUsageLogsAsync(int userId, DateTime startDate, DateTime endDate);
        Task<int> GetDailyUsageCountAsync(int userId, DateTime date);
        Task<int> GetMonthlyUsageCountAsync(int userId, int year, int month);
        Task<SubscriptionUsageLog> LogUsageAsync(SubscriptionUsageLog usageLog);
        Task<Dictionary<string, int>> GetUsageStatisticsByEndpointAsync(int userId, DateTime startDate, DateTime endDate);
    }
}