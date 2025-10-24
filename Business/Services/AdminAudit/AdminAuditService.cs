using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.AdminAudit
{
    /// <summary>
    /// Implementation of admin audit service
    /// Handles logging and querying of all admin operations
    /// </summary>
    public class AdminAuditService : IAdminAuditService
    {
        private readonly IAdminOperationLogRepository _logRepository;
        private readonly ILogger<AdminAuditService> _logger;

        public AdminAuditService(
            IAdminOperationLogRepository logRepository,
            ILogger<AdminAuditService> logger)
        {
            _logRepository = logRepository;
            _logger = logger;
        }

        public async Task LogAsync(AdminOperationLog entry)
        {
            try
            {
                entry.Timestamp = DateTime.Now;
                _logRepository.Add(entry);
                await _logRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin action logged: {Action} by Admin {AdminId} for User {TargetId}",
                    entry.Action, entry.AdminUserId, entry.TargetUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log admin action: {Action}", entry.Action);
                // Don't throw - audit logging failure shouldn't break the flow
            }
        }

        public async Task LogAsync(
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
            object afterState = null)
        {
            var entry = new AdminOperationLog
            {
                Action = action,
                AdminUserId = adminUserId,
                TargetUserId = targetUserId,
                EntityType = entityType,
                EntityId = entityId,
                IsOnBehalfOf = isOnBehalfOf,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RequestPath = requestPath,
                RequestPayload = requestPayload != null ? JsonSerializer.Serialize(requestPayload) : null,
                ResponseStatus = responseStatus,
                Duration = duration,
                Reason = reason,
                BeforeState = beforeState != null ? JsonSerializer.Serialize(beforeState) : null,
                AfterState = afterState != null ? JsonSerializer.Serialize(afterState) : null,
                Timestamp = DateTime.Now
            };

            await LogAsync(entry);
        }

        public async Task<List<AdminOperationLog>> GetLogsByAdminAsync(int adminUserId, int page, int pageSize)
        {
            return await _logRepository.GetByAdminUserIdAsync(adminUserId, page, pageSize);
        }

        public async Task<List<AdminOperationLog>> GetLogsByTargetUserAsync(int targetUserId, int page, int pageSize)
        {
            return await _logRepository.GetByTargetUserIdAsync(targetUserId, page, pageSize);
        }

        public async Task<List<AdminOperationLog>> GetOnBehalfOfLogsAsync(int page, int pageSize)
        {
            return await _logRepository.GetOnBehalfOfLogsAsync(page, pageSize);
        }

        public async Task<List<AdminOperationLog>> SearchLogsAsync(
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? adminUserId = null,
            int? targetUserId = null,
            bool? isOnBehalfOf = null,
            int page = 1,
            int pageSize = 50)
        {
            return await _logRepository.SearchLogsAsync(
                action, fromDate, toDate, adminUserId, targetUserId, isOnBehalfOf, page, pageSize);
        }

        public async Task<int> GetCountAsync(
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? adminUserId = null,
            int? targetUserId = null,
            bool? isOnBehalfOf = null)
        {
            return await _logRepository.GetCountAsync(
                action, fromDate, toDate, adminUserId, targetUserId, isOnBehalfOf);
        }
    }
}
