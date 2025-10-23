using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Repository implementation for AdminOperationLog entity
    /// Provides specialized queries for audit trail operations
    /// </summary>
    public class AdminOperationLogRepository : EfEntityRepositoryBase<AdminOperationLog, ProjectDbContext>, IAdminOperationLogRepository
    {
        public AdminOperationLogRepository(ProjectDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Get filtered and paginated admin operation logs
        /// </summary>
        public async Task<(List<AdminOperationLogDto> Logs, int TotalCount)> GetFilteredLogsAsync(AdminOperationLogFilterDto filter)
        {
            var query = Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .AsQueryable();

            // Apply filters
            if (filter.AdminUserId.HasValue)
                query = query.Where(x => x.AdminUserId == filter.AdminUserId.Value);

            if (filter.TargetUserId.HasValue)
                query = query.Where(x => x.TargetUserId == filter.TargetUserId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(x => x.Action == filter.Action);

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(x => x.EntityType == filter.EntityType);

            if (filter.EntityId.HasValue)
                query = query.Where(x => x.EntityId == filter.EntityId.Value);

            if (filter.IsOnBehalfOf.HasValue)
                query = query.Where(x => x.IsOnBehalfOf == filter.IsOnBehalfOf.Value);

            if (!string.IsNullOrWhiteSpace(filter.IpAddress))
                query = query.Where(x => x.IpAddress == filter.IpAddress);

            if (filter.StartDate.HasValue)
                query = query.Where(x => x.Timestamp >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(x => x.Timestamp <= filter.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(filter.ReasonSearch))
                query = query.Where(x => x.Reason.Contains(filter.ReasonSearch));

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortDescending
                ? query.OrderByDescending(x => EF.Property<object>(x, filter.SortBy))
                : query.OrderBy(x => EF.Property<object>(x, filter.SortBy));

            // Apply pagination
            var logs = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new AdminOperationLogDto
                {
                    Id = x.Id,
                    AdminUserId = x.AdminUserId,
                    AdminUserName = x.AdminUser.FullName,
                    AdminUserEmail = x.AdminUser.Email,
                    TargetUserId = x.TargetUserId,
                    TargetUserName = x.TargetUser != null ? x.TargetUser.FullName : null,
                    TargetUserEmail = x.TargetUser != null ? x.TargetUser.Email : null,
                    Action = x.Action,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    IsOnBehalfOf = x.IsOnBehalfOf,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,
                    RequestPath = x.RequestPath,
                    RequestPayload = x.RequestPayload != null && x.RequestPayload.Length > 500 
                        ? x.RequestPayload.Substring(0, 500) + "..." 
                        : x.RequestPayload,
                    ResponseStatus = x.ResponseStatus,
                    Duration = x.Duration,
                    Timestamp = x.Timestamp,
                    Reason = x.Reason,
                    BeforeState = x.BeforeState != null && x.BeforeState.Length > 500 
                        ? x.BeforeState.Substring(0, 500) + "..." 
                        : x.BeforeState,
                    AfterState = x.AfterState != null && x.AfterState.Length > 500 
                        ? x.AfterState.Substring(0, 500) + "..." 
                        : x.AfterState
                })
                .ToListAsync();

            return (logs, totalCount);
        }

        /// <summary>
        /// Get all operations by a specific admin user
        /// </summary>
        public async Task<List<AdminOperationLogDto>> GetByAdminUserIdAsync(int adminUserId, int limit = 100)
        {
            return await Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .Where(x => x.AdminUserId == adminUserId)
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .Select(x => MapToDto(x))
                .ToListAsync();
        }

        /// <summary>
        /// Get all operations targeting a specific user
        /// </summary>
        public async Task<List<AdminOperationLogDto>> GetByTargetUserIdAsync(int targetUserId, int limit = 100)
        {
            return await Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .Where(x => x.TargetUserId == targetUserId)
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .Select(x => MapToDto(x))
                .ToListAsync();
        }

        /// <summary>
        /// Get operations for a specific entity
        /// </summary>
        public async Task<List<AdminOperationLogDto>> GetByEntityAsync(string entityType, int entityId, int limit = 50)
        {
            return await Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .Where(x => x.EntityType == entityType && x.EntityId == entityId)
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .Select(x => MapToDto(x))
                .ToListAsync();
        }

        /// <summary>
        /// Get on-behalf-of operations only
        /// </summary>
        public async Task<List<AdminOperationLogDto>> GetOnBehalfOfOperationsAsync(int? adminUserId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .Where(x => x.IsOnBehalfOf);

            if (adminUserId.HasValue)
                query = query.Where(x => x.AdminUserId == adminUserId.Value);

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Take(100)
                .Select(x => MapToDto(x))
                .ToListAsync();
        }

        /// <summary>
        /// Get operations by action type
        /// </summary>
        public async Task<List<AdminOperationLogDto>> GetByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            var query = Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .Where(x => x.Action == action);

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .Select(x => MapToDto(x))
                .ToListAsync();
        }

        /// <summary>
        /// Get recent operations (last N hours)
        /// </summary>
        public async Task<List<AdminOperationLogDto>> GetRecentOperationsAsync(int hours = 24, int limit = 100)
        {
            var cutoffTime = DateTime.Now.AddHours(-hours);

            return await Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .Where(x => x.Timestamp >= cutoffTime)
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .Select(x => MapToDto(x))
                .ToListAsync();
        }

        /// <summary>
        /// Get operations within date range
        /// </summary>
        public async Task<List<AdminOperationLogDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int? adminUserId = null)
        {
            var query = Context.AdminOperationLogs
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate);

            if (adminUserId.HasValue)
                query = query.Where(x => x.AdminUserId == adminUserId.Value);

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Select(x => MapToDto(x))
                .ToListAsync();
        }

        /// <summary>
        /// Get statistics for admin operations
        /// </summary>
        public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = Context.AdminOperationLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            var stats = await query
                .GroupBy(x => x.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);

            return stats;
        }

        /// <summary>
        /// Get admin user activity summary
        /// </summary>
        public async Task<Dictionary<string, object>> GetAdminActivitySummaryAsync(int adminUserId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = Context.AdminOperationLogs
                .Where(x => x.AdminUserId == adminUserId);

            if (startDate.HasValue)
                query = query.Where(x => x.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(x => x.Timestamp <= endDate.Value);

            var totalOperations = await query.CountAsync();
            var onBehalfOfCount = await query.CountAsync(x => x.IsOnBehalfOf);
            var actionBreakdown = await query
                .GroupBy(x => x.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Action, x => x.Count);

            var firstOperation = await query.OrderBy(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefaultAsync();
            var lastOperation = await query.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefaultAsync();

            return new Dictionary<string, object>
            {
                { "TotalOperations", totalOperations },
                { "OnBehalfOfOperations", onBehalfOfCount },
                { "ActionBreakdown", actionBreakdown },
                { "FirstOperation", firstOperation },
                { "LastOperation", lastOperation }
            };
        }

        /// <summary>
        /// Helper method to map entity to DTO
        /// </summary>
        private static AdminOperationLogDto MapToDto(AdminOperationLog x)
        {
            return new AdminOperationLogDto
            {
                Id = x.Id,
                AdminUserId = x.AdminUserId,
                AdminUserName = x.AdminUser?.FullName,
                AdminUserEmail = x.AdminUser?.Email,
                TargetUserId = x.TargetUserId,
                TargetUserName = x.TargetUser?.FullName,
                TargetUserEmail = x.TargetUser?.Email,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                IsOnBehalfOf = x.IsOnBehalfOf,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                RequestPath = x.RequestPath,
                RequestPayload = x.RequestPayload,
                ResponseStatus = x.ResponseStatus,
                Duration = x.Duration,
                Timestamp = x.Timestamp,
                Reason = x.Reason,
                BeforeState = x.BeforeState,
                AfterState = x.AfterState
            };
        }
    }
}
