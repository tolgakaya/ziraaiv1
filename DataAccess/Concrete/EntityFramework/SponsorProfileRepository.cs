using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class SponsorProfileRepository : EfEntityRepositoryBase<SponsorProfile, ProjectDbContext>, ISponsorProfileRepository
    {
        public SponsorProfileRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<SponsorProfile> GetBySponsorIdAsync(int sponsorId)
        {
            return await Context.SponsorProfiles
                .Include(x => x.Sponsor)
                .Include(x => x.CurrentSubscriptionTier)
                .FirstOrDefaultAsync(x => x.SponsorId == sponsorId);
        }

        public async Task<SponsorProfile> GetByCompanyNameAsync(string companyName)
        {
            return await Context.SponsorProfiles
                .Include(x => x.Sponsor)
                .Include(x => x.CurrentSubscriptionTier)
                .FirstOrDefaultAsync(x => x.CompanyName == companyName);
        }

        public async Task<bool> IsSponsorVerifiedAsync(int sponsorId)
        {
            var profile = await Context.SponsorProfiles
                .FirstOrDefaultAsync(x => x.SponsorId == sponsorId);
            
            return profile?.IsVerified == true;
        }

        public async Task UpdateStatisticsAsync(int sponsorId, int totalSponsored, int activeSponsored, decimal totalInvestment)
        {
            var profile = await GetBySponsorIdAsync(sponsorId);
            if (profile != null)
            {
                profile.TotalSponsored = totalSponsored;
                profile.ActiveSponsored = activeSponsored;
                profile.TotalInvestment = totalInvestment;
                profile.UpdatedDate = System.DateTime.Now;
                
                Context.SponsorProfiles.Update(profile);
                await Context.SaveChangesAsync();
            }
        }
    }
}