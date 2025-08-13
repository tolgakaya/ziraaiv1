using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ISponsorshipPurchaseRepository : IEntityRepository<SponsorshipPurchase>
    {
        Task<List<SponsorshipPurchase>> GetBySponsorIdAsync(int sponsorId);
        Task<SponsorshipPurchase> GetWithCodesAsync(int purchaseId);
        Task<List<SponsorshipPurchase>> GetActivePurchasesAsync(int sponsorId);
        Task<decimal> GetTotalSpentBySponsorAsync(int sponsorId);
        Task<int> GetTotalCodesPurchasedAsync(int sponsorId);
        Task<int> GetTotalCodesUsedAsync(int sponsorId);
        Task UpdateCodesUsedCountAsync(int purchaseId);
        Task<Dictionary<int, int>> GetUsageStatisticsByTierAsync(int sponsorId);
    }
}