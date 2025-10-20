using System.Linq;
using System.Threading.Tasks;
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Entity Framework implementation of farmer-sponsor blocking repository
    /// </summary>
    public class FarmerSponsorBlockRepository : EfEntityRepositoryBase<FarmerSponsorBlock, ProjectDbContext>, IFarmerSponsorBlockRepository
    {
        public FarmerSponsorBlockRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<bool> IsBlockedAsync(int farmerId, int sponsorId)
        {
            return await Context.FarmerSponsorBlocks
                .AnyAsync(b => b.FarmerId == farmerId && b.SponsorId == sponsorId && b.IsBlocked);
        }

        public async Task<bool> IsMutedAsync(int farmerId, int sponsorId)
        {
            return await Context.FarmerSponsorBlocks
                .AnyAsync(b => b.FarmerId == farmerId && b.SponsorId == sponsorId && b.IsMuted);
        }

        public async Task<FarmerSponsorBlock> GetBlockRecordAsync(int farmerId, int sponsorId)
        {
            return await Context.FarmerSponsorBlocks
                .FirstOrDefaultAsync(b => b.FarmerId == farmerId && b.SponsorId == sponsorId);
        }
    }
}
