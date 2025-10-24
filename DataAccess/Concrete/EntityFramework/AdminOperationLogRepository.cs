using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Entity Framework implementation of AdminOperationLog repository
    /// </summary>
    public class AdminOperationLogRepository : EfEntityRepositoryBase<AdminOperationLog, ProjectDbContext>,
        IAdminOperationLogRepository
    {
        public AdminOperationLogRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<AdminOperationLog>> GetByAdminUserIdAsync(int adminUserId, int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.AdminUserId == adminUserId)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .ToListAsync();
        }

        public async Task<List<AdminOperationLog>> GetByTargetUserIdAsync(int targetUserId, int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.TargetUserId == targetUserId)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .ToListAsync();
        }

        public async Task<List<AdminOperationLog>> GetByActionAsync(string action, int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.Action == action)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .ToListAsync();
        }

        public async Task<List<AdminOperationLog>> GetOnBehalfOfLogsAsync(int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.IsOnBehalfOf)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .ToListAsync();
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
            var query = Context.AdminOperationLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action == action);

            if (fromDate.HasValue)
                query = query.Where(x => x.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.Timestamp <= toDate.Value);

            if (adminUserId.HasValue)
                query = query.Where(x => x.AdminUserId == adminUserId.Value);

            if (targetUserId.HasValue)
                query = query.Where(x => x.TargetUserId == targetUserId.Value);

            if (isOnBehalfOf.HasValue)
                query = query.Where(x => x.IsOnBehalfOf == isOnBehalfOf.Value);

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync(
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? adminUserId = null,
            int? targetUserId = null,
            bool? isOnBehalfOf = null)
        {
            var query = Context.AdminOperationLogs.AsQueryable();

            // Apply same filters as SearchLogsAsync
            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action == action);

            if (fromDate.HasValue)
                query = query.Where(x => x.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.Timestamp <= toDate.Value);

            if (adminUserId.HasValue)
                query = query.Where(x => x.AdminUserId == adminUserId.Value);

            if (targetUserId.HasValue)
                query = query.Where(x => x.TargetUserId == targetUserId.Value);

            if (isOnBehalfOf.HasValue)
                query = query.Where(x => x.IsOnBehalfOf == isOnBehalfOf.Value);

            return await query.CountAsync();
        }
    }
}
