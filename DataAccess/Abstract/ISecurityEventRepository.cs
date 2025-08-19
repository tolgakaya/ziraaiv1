using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ISecurityEventRepository : IEntityRepository<SecurityEvent>
    {
        Task<List<SecurityEvent>> GetEventsByTypeAsync(string eventType);
        Task<List<SecurityEvent>> GetEventsBySeverityAsync(string severity);
        Task<List<SecurityEvent>> GetUnprocessedEventsAsync();
        Task<List<SecurityEvent>> GetEventsByUserIdAsync(string userId);
        Task<List<SecurityEvent>> GetEventsByIpAddressAsync(string ipAddress);
        Task<List<SecurityEvent>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task MarkAsProcessedAsync(int eventId, string processedBy);
        Task<Dictionary<string, int>> GetEventCountsByTypeAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, int>> GetEventCountsBySeverityAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> CleanupOldEventsAsync(int daysToKeep = 90);
    }
}