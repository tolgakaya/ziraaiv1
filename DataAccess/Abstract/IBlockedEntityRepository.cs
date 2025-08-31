using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IBlockedEntityRepository : IEntityRepository<BlockedEntity>
    {
        Task<BlockedEntity> GetByEntityValueAsync(string entityType, string entityValue);
        Task<List<BlockedEntity>> GetByEntityTypeAsync(string entityType);
        Task<List<BlockedEntity>> GetActiveBlocksAsync();
        Task<List<BlockedEntity>> GetExpiredBlocksAsync();
        Task<List<BlockedEntity>> GetBySeverityAsync(string severity);
        Task<bool> IsEntityBlockedAsync(string entityType, string entityValue);
        Task<int> CleanupExpiredBlocksAsync();
        Task UnblockEntityAsync(string entityType, string entityValue, string unblockedBy);
        Task ExtendBlockAsync(int blockId, DateTime newExpiryDate);
        Task<Dictionary<string, int>> GetBlockCountsByTypeAsync();
        Task<Dictionary<string, int>> GetBlockCountsBySeverityAsync();
        Task<List<BlockedEntity>> GetBlocksByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}