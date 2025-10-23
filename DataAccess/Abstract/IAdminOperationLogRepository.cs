using Core.DataAccess;
using Entities.Concrete;
using Entities.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for AdminOperationLog entity
    /// Provides specialized queries for audit trail operations
    /// </summary>
    public interface IAdminOperationLogRepository : IEntityRepository<AdminOperationLog>
    {
        /// <summary>
        /// Get filtered and paginated admin operation logs
        /// </summary>
        Task<(List<AdminOperationLogDto> Logs, int TotalCount)> GetFilteredLogsAsync(AdminOperationLogFilterDto filter);
        
        /// <summary>
        /// Get all operations by a specific admin user
        /// </summary>
        Task<List<AdminOperationLogDto>> GetByAdminUserIdAsync(int adminUserId, int limit = 100);
        
        /// <summary>
        /// Get all operations targeting a specific user
        /// </summary>
        Task<List<AdminOperationLogDto>> GetByTargetUserIdAsync(int targetUserId, int limit = 100);
        
        /// <summary>
        /// Get operations for a specific entity
        /// </summary>
        Task<List<AdminOperationLogDto>> GetByEntityAsync(string entityType, int entityId, int limit = 50);
        
        /// <summary>
        /// Get on-behalf-of operations only
        /// </summary>
        Task<List<AdminOperationLogDto>> GetOnBehalfOfOperationsAsync(int? adminUserId = null, DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Get operations by action type
        /// </summary>
        Task<List<AdminOperationLogDto>> GetByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        
        /// <summary>
        /// Get recent operations (last N hours)
        /// </summary>
        Task<List<AdminOperationLogDto>> GetRecentOperationsAsync(int hours = 24, int limit = 100);
        
        /// <summary>
        /// Get operations within date range
        /// </summary>
        Task<List<AdminOperationLogDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int? adminUserId = null);
        
        /// <summary>
        /// Get statistics for admin operations
        /// </summary>
        Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Get admin user activity summary
        /// </summary>
        Task<Dictionary<string, object>> GetAdminActivitySummaryAsync(int adminUserId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
