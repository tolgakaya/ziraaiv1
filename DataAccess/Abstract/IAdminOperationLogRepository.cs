using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for AdminOperationLog entity
    /// Provides CRUD operations and specialized queries for admin audit trail
    /// </summary>
    public interface IAdminOperationLogRepository : IEntityRepository<AdminOperationLog>
    {
        /// <summary>
        /// Get logs by admin user ID with pagination
        /// </summary>
        Task<List<AdminOperationLog>> GetByAdminUserIdAsync(int adminUserId, int page, int pageSize);

        /// <summary>
        /// Get logs by target user ID with pagination
        /// </summary>
        Task<List<AdminOperationLog>> GetByTargetUserIdAsync(int targetUserId, int page, int pageSize);

        /// <summary>
        /// Get logs by action type with pagination
        /// </summary>
        Task<List<AdminOperationLog>> GetByActionAsync(string action, int page, int pageSize);

        /// <summary>
        /// Get all on-behalf-of logs with pagination
        /// </summary>
        Task<List<AdminOperationLog>> GetOnBehalfOfLogsAsync(int page, int pageSize);

        /// <summary>
        /// Search logs with filters
        /// </summary>
        Task<List<AdminOperationLog>> SearchLogsAsync(
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? adminUserId = null,
            int? targetUserId = null,
            bool? isOnBehalfOf = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Get total count of logs matching filters
        /// </summary>
        Task<int> GetCountAsync(
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? adminUserId = null,
            int? targetUserId = null,
            bool? isOnBehalfOf = null);
    }
}
