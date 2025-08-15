using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ISponsorProfileRepository : IEntityRepository<SponsorProfile>
    {
        Task<SponsorProfile> GetBySponsorIdAsync(int sponsorId);
        Task<SponsorProfile> GetByCompanyNameAsync(string companyName);
        Task<bool> IsSponsorVerifiedAsync(int sponsorId);
        Task UpdateStatisticsAsync(int sponsorId, int totalPurchases, int totalCodesGenerated, int totalCodesRedeemed, decimal totalInvestment);
    }
}