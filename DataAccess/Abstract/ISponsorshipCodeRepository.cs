using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ISponsorshipCodeRepository : IEntityRepository<SponsorshipCode>
    {
        Task<SponsorshipCode> GetByCodeAsync(string code);
        Task<SponsorshipCode> GetUnusedCodeAsync(string code);
        Task<List<SponsorshipCode>> GetBySponsorIdAsync(int sponsorId);
        Task<List<SponsorshipCode>> GetByPurchaseIdAsync(int purchaseId);
        Task<List<SponsorshipCode>> GetUnusedCodesBySponsorAsync(int sponsorId);
        Task<List<SponsorshipCode>> GetUsedCodesBySponsorAsync(int sponsorId);
        Task<List<SponsorshipCode>> GetUnsentCodesBySponsorAsync(int sponsorId);
        Task<List<SponsorshipCode>> GetSentButUnusedCodesBySponsorAsync(int sponsorId, int? sentDaysAgo = null);
        Task<int> GetUsedCountByPurchaseAsync(int purchaseId);
        Task<bool> IsCodeValidAsync(string code);
        Task<bool> MarkAsUsedAsync(string code, int userId, int subscriptionId);
        Task<List<SponsorshipCode>> GenerateCodesAsync(int purchaseId, int sponsorId, int tierId, int quantity, string prefix, int validityDays);
    }
}