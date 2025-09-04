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
    public class DeepLinkRepository : EfEntityRepositoryBase<DeepLink, ProjectDbContext>, IDeepLinkRepository
    {
        public DeepLinkRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<DeepLink> GetByLinkIdAsync(string linkId)
        {
            return await Context.DeepLinks
                .FirstOrDefaultAsync(dl => dl.LinkId == linkId && dl.IsActive);
        }

        public async Task<List<DeepLink>> GetBySponsorIdAsync(string sponsorId)
        {
            return await Context.DeepLinks
                .Where(dl => dl.SponsorId == sponsorId && dl.IsActive)
                .OrderByDescending(dl => dl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<DeepLink>> GetByTypeAsync(string type)
        {
            return await Context.DeepLinks
                .Where(dl => dl.Type == type && dl.IsActive)
                .OrderByDescending(dl => dl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<DeepLink>> GetExpiredLinksAsync()
        {
            return await Context.DeepLinks
                .Where(dl => dl.ExpiryDate < DateTime.Now && dl.IsActive)
                .ToListAsync();
        }

        public async Task AddClickAsync(DeepLinkClickRecord clickRecord)
        {
            await Context.DeepLinkClickRecords.AddAsync(clickRecord);
            await Context.SaveChangesAsync();
        }

        public async Task<List<DeepLinkClickRecord>> GetClicksAsync(string linkId)
        {
            return await Context.DeepLinkClickRecords
                .Where(c => c.LinkId == linkId)
                .OrderByDescending(c => c.ClickDate)
                .ToListAsync();
        }

        public async Task<List<DeepLinkClickRecord>> GetClicksByDeviceAsync(string linkId, string deviceId)
        {
            return await Context.DeepLinkClickRecords
                .Where(c => c.LinkId == linkId && c.DeviceId == deviceId)
                .ToListAsync();
        }

        public async Task<List<DeepLinkClickRecord>> GetClicksByDateRangeAsync(string linkId, DateTime startDate, DateTime endDate)
        {
            return await Context.DeepLinkClickRecords
                .Where(c => c.LinkId == linkId && c.ClickDate >= startDate && c.ClickDate <= endDate)
                .OrderByDescending(c => c.ClickDate)
                .ToListAsync();
        }

        public async Task<int> GetTotalClicksAsync(string linkId)
        {
            return await Context.DeepLinkClickRecords
                .CountAsync(c => c.LinkId == linkId);
        }

        public async Task<int> GetUniqueDevicesAsync(string linkId)
        {
            return await Context.DeepLinkClickRecords
                .Where(c => c.LinkId == linkId && !string.IsNullOrEmpty(c.DeviceId))
                .Select(c => c.DeviceId)
                .Distinct()
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetPlatformBreakdownAsync(string linkId)
        {
            return await Context.DeepLinkClickRecords
                .Where(c => c.LinkId == linkId)
                .GroupBy(c => c.Platform ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<Dictionary<string, int>> GetSourceBreakdownAsync(string linkId)
        {
            return await Context.DeepLinkClickRecords
                .Where(c => c.LinkId == linkId)
                .GroupBy(c => c.Source ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<Dictionary<string, int>> GetCountryBreakdownAsync(string linkId)
        {
            return await Context.DeepLinkClickRecords
                .Where(c => c.LinkId == linkId)
                .GroupBy(c => c.Country ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<int> CleanupExpiredLinksAsync()
        {
            var expiredLinks = await GetExpiredLinksAsync();
            
            foreach (var link in expiredLinks)
            {
                link.IsActive = false;
            }
            
            if (expiredLinks.Any())
            {
                Context.DeepLinks.UpdateRange(expiredLinks);
                await Context.SaveChangesAsync();
            }
            
            return expiredLinks.Count;
        }

        public async Task<int> CleanupOldClickRecordsAsync(int daysToKeep = 365)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            
            var oldRecords = await Context.DeepLinkClickRecords
                .Where(c => c.ClickDate < cutoffDate)
                .ToListAsync();
            
            if (oldRecords.Any())
            {
                Context.DeepLinkClickRecords.RemoveRange(oldRecords);
                await Context.SaveChangesAsync();
            }
            
            return oldRecords.Count;
        }
    }
}