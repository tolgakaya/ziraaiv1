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
                .Include(x => x.SponsorshipPurchases)
                    .ThenInclude(p => p.SubscriptionTier)
                .FirstOrDefaultAsync(x => x.SponsorId == sponsorId);
        }

        public async Task<SponsorProfile> GetByCompanyNameAsync(string companyName)
        {
            return await Context.SponsorProfiles
                .Include(x => x.Sponsor)
                .Include(x => x.SponsorshipPurchases)
                .FirstOrDefaultAsync(x => x.CompanyName == companyName);
        }

        public async Task<bool> IsSponsorVerifiedAsync(int sponsorId)
        {
            var profile = await Context.SponsorProfiles
                .FirstOrDefaultAsync(x => x.SponsorId == sponsorId);
            
            return profile?.IsVerifiedCompany == true;
        }

        public async Task UpdateStatisticsAsync(int sponsorId, int totalPurchases, int totalCodesGenerated, int totalCodesRedeemed, decimal totalInvestment)
        {
            var profile = await GetBySponsorIdAsync(sponsorId);
            if (profile != null)
            {
                profile.TotalPurchases = totalPurchases;
                profile.TotalCodesGenerated = totalCodesGenerated;
                profile.TotalCodesRedeemed = totalCodesRedeemed;
                profile.TotalInvestment = totalInvestment;
                profile.UpdatedDate = System.DateTime.Now;
                
                Context.SponsorProfiles.Update(profile);
                await Context.SaveChangesAsync();
            }
        }
    }
}