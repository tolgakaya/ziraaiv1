using Core.Utilities.Results;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface ISponsorshipService
    {
        Task<IDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>> PurchaseBulkSubscriptionsAsync(int sponsorId, int tierId, int quantity, decimal amount, string paymentReference);
        Task<IDataResult<List<SponsorshipCode>>> GenerateCodesForPurchaseAsync(int purchaseId);
        Task<IDataResult<UserSubscription>> RedeemSponsorshipCodeAsync(string code, int userId);
        Task<IDataResult<SponsorshipCode>> ValidateCodeAsync(string code);
        Task<IDataResult<List<SponsorshipCode>>> GetSponsorCodesAsync(int sponsorId);
        Task<IDataResult<List<SponsorshipCode>>> GetUnusedSponsorCodesAsync(int sponsorId);
        Task<IDataResult<List<SponsorshipCode>>> GetUnsentSponsorCodesAsync(int sponsorId);
        Task<IDataResult<List<SponsorshipCode>>> GetSentButUnusedSponsorCodesAsync(int sponsorId, int? sentDaysAgo = null);
        Task<IDataResult<List<SponsorshipPurchase>>> GetSponsorPurchasesAsync(int sponsorId);
        Task<IDataResult<object>> GetSponsorshipStatisticsAsync(int sponsorId);
        Task<IDataResult<List<object>>> GetSponsoredFarmersAsync(int sponsorId);
        Task<IResult> DeactivateCodeAsync(string code, int sponsorId);
        Task<IDataResult<bool>> IsCodeValidAsync(string code);
    }
}