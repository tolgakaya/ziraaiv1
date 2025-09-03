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
    public class SponsorAnalysisAccessRepository : EfEntityRepositoryBase<SponsorAnalysisAccess, ProjectDbContext>, ISponsorAnalysisAccessRepository
    {
        public SponsorAnalysisAccessRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<SponsorAnalysisAccess> GetBySponsorAndAnalysisAsync(int sponsorId, int plantAnalysisId)
        {
            return await Context.SponsorAnalysisAccess
                .Include(x => x.Sponsor)
                .Include(x => x.PlantAnalysis)
                .Include(x => x.Farmer)
                .Include(x => x.SponsorshipCode)
                .FirstOrDefaultAsync(x => x.SponsorId == sponsorId && x.PlantAnalysisId == plantAnalysisId);
        }

        public async Task<List<SponsorAnalysisAccess>> GetBySponsorIdAsync(int sponsorId)
        {
            return await Context.SponsorAnalysisAccess
                .Include(x => x.PlantAnalysis)
                .Include(x => x.Farmer)
                .Include(x => x.SponsorshipCode)
                .Where(x => x.SponsorId == sponsorId)
                .OrderByDescending(x => x.FirstViewedDate)
                .ToListAsync();
        }

        public async Task<List<SponsorAnalysisAccess>> GetByFarmerIdAsync(int farmerId)
        {
            return await Context.SponsorAnalysisAccess
                .Include(x => x.Sponsor)
                .Include(x => x.PlantAnalysis)
                .Include(x => x.SponsorshipCode)
                .Where(x => x.FarmerId == farmerId)
                .OrderByDescending(x => x.FirstViewedDate)
                .ToListAsync();
        }

        public async Task<List<SponsorAnalysisAccess>> GetByAnalysisIdAsync(int plantAnalysisId)
        {
            return await Context.SponsorAnalysisAccess
                .Include(x => x.Sponsor)
                .Include(x => x.Farmer)
                .Include(x => x.SponsorshipCode)
                .Where(x => x.PlantAnalysisId == plantAnalysisId)
                .OrderByDescending(x => x.FirstViewedDate)
                .ToListAsync();
        }

        public async Task UpdateViewCountAsync(int accessId)
        {
            var access = await GetAsync(a => a.Id == accessId);
            if (access != null)
            {
                access.ViewCount++;
                access.LastViewedDate = DateTime.Now;
                access.UpdatedDate = DateTime.Now;
                
                Context.SponsorAnalysisAccess.Update(access);
                await Context.SaveChangesAsync();
            }
        }

        public async Task RecordDownloadAsync(int accessId, DateTime downloadDate)
        {
            var access = await GetAsync(a => a.Id == accessId);
            if (access != null)
            {
                access.HasDownloaded = true;
                access.DownloadedDate = downloadDate;
                access.UpdatedDate = DateTime.Now;
                
                Context.SponsorAnalysisAccess.Update(access);
                await Context.SaveChangesAsync();
            }
        }

        public async Task<int> GetAccessCountBySponsorAsync(int sponsorId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = Context.SponsorAnalysisAccess.Where(x => x.SponsorId == sponsorId);
            
            if (fromDate.HasValue)
                query = query.Where(x => x.FirstViewedDate >= fromDate.Value);
            
            if (toDate.HasValue)
                query = query.Where(x => x.FirstViewedDate <= toDate.Value);
            
            return await query.CountAsync();
        }

        public async Task<List<SponsorAnalysisAccess>> GetRecentAccessesAsync(int sponsorId, int count = 10)
        {
            return await Context.SponsorAnalysisAccess
                .Include(x => x.PlantAnalysis)
                .Include(x => x.Farmer)
                .Where(x => x.SponsorId == sponsorId)
                .OrderByDescending(x => x.LastViewedDate ?? x.FirstViewedDate)
                .Take(count)
                .ToListAsync();
        }
    }
}