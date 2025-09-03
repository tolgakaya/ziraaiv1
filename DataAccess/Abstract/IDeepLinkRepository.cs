using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IDeepLinkRepository : IEntityRepository<DeepLink>
    {
        Task<DeepLink> GetByLinkIdAsync(string linkId);
        Task<List<DeepLink>> GetBySponsorIdAsync(string sponsorId);
        Task<List<DeepLink>> GetByTypeAsync(string type);
        Task<List<DeepLink>> GetExpiredLinksAsync();
        
        // Click tracking
        Task AddClickAsync(DeepLinkClickRecord clickRecord);
        Task<List<DeepLinkClickRecord>> GetClicksAsync(string linkId);
        Task<List<DeepLinkClickRecord>> GetClicksByDeviceAsync(string linkId, string deviceId);
        Task<List<DeepLinkClickRecord>> GetClicksByDateRangeAsync(string linkId, DateTime startDate, DateTime endDate);
        
        // Analytics
        Task<int> GetTotalClicksAsync(string linkId);
        Task<int> GetUniqueDevicesAsync(string linkId);
        Task<Dictionary<string, int>> GetPlatformBreakdownAsync(string linkId);
        Task<Dictionary<string, int>> GetSourceBreakdownAsync(string linkId);
        Task<Dictionary<string, int>> GetCountryBreakdownAsync(string linkId);
        
        // Maintenance
        Task<int> CleanupExpiredLinksAsync();
        Task<int> CleanupOldClickRecordsAsync(int daysToKeep = 365);
    }
}