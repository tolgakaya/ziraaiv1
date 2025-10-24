using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.AdminAudit
{
    /// <summary>
    /// Service interface for admin audit logging
    /// Provides methods to log and query admin operations
    /// </summary>
    public interface IAdminAuditService
    {
        /// <summary>
        /// Log an admin operation with all details
        /// </summary>
        Task LogAsync(AdminOperationLog entry);

        /// <summary>
        /// Log an admin operation with individual parameters
        /// </summary>
        Task LogAsync(
            string action,
            int adminUserId,
            int? targetUserId = null,
            string entityType = null,
            int? entityId = null,
            bool isOnBehalfOf = false,
            string ipAddress = null,
            string userAgent = null,
            string requestPath = null,
            object requestPayload = null,
            int? responseStatus = null,
            int? duration = null,
            string reason = null,
            object beforeState = null,
            object afterState = null);

        /// <summary>
        /// Get logs by admin user ID with pagination
        /// </summary>
        Task<List<AdminOperationLog>> GetLogsByAdminAsync(int adminUserId, int page, int pageSize);

        /// <summary>
        /// Get logs by target user ID with pagination
        /// </summary>
        Task<List<AdminOperationLog>> GetLogsByTargetUserAsync(int targetUserId, int page, int pageSize);

        /// <summary>
        /// Get on-behalf-of logs with pagination
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
        /// Get count of logs matching filters
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
