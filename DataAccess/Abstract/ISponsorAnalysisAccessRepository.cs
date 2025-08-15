using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ISponsorAnalysisAccessRepository : IEntityRepository<SponsorAnalysisAccess>
    {
        Task<SponsorAnalysisAccess> GetBySponsorAndAnalysisAsync(int sponsorId, int plantAnalysisId);
        Task<List<SponsorAnalysisAccess>> GetBySponsorIdAsync(int sponsorId);
        Task<List<SponsorAnalysisAccess>> GetByFarmerIdAsync(int farmerId);
        Task<List<SponsorAnalysisAccess>> GetByAnalysisIdAsync(int plantAnalysisId);
        Task UpdateViewCountAsync(int accessId);
        Task RecordDownloadAsync(int accessId, DateTime downloadDate);
        Task<int> GetAccessCountBySponsorAsync(int sponsorId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<SponsorAnalysisAccess>> GetRecentAccessesAsync(int sponsorId, int count = 10);
    }
}