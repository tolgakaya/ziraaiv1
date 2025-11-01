using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface ISponsorshipService
    {
        Task<IDataResult<Entities.Dtos.SponsorshipPurchaseResponseDto>> PurchaseBulkSubscriptionsAsync(
            int sponsorId,
            int tierId,
            int quantity,
            decimal amount,
            string paymentMethod,
            string paymentReference,
            string companyName = null,
            string invoiceAddress = null,
            string taxNumber = null);
        Task<IDataResult<List<SponsorshipCode>>> GenerateCodesForPurchaseAsync(int purchaseId);
        Task<IDataResult<UserSubscription>> RedeemSponsorshipCodeAsync(string code, int userId);
        Task<IDataResult<SponsorshipCode>> ValidateCodeAsync(string code);

        // Paginated code retrieval methods
        Task<IDataResult<SponsorshipCodesPaginatedDto>> GetSponsorCodesAsync(int sponsorId, int page = 1, int pageSize = 50, bool excludeDealerTransferred = false);
        Task<IDataResult<SponsorshipCodesPaginatedDto>> GetUnusedSponsorCodesAsync(int sponsorId, int page = 1, int pageSize = 50, bool excludeDealerTransferred = false);
        Task<IDataResult<SponsorshipCodesPaginatedDto>> GetUnsentSponsorCodesAsync(int sponsorId, int page = 1, int pageSize = 50, bool excludeDealerTransferred = false);
        Task<IDataResult<SponsorshipCodesPaginatedDto>> GetSentButUnusedSponsorCodesAsync(int sponsorId, int sentDaysAgo, int page = 1, int pageSize = 50, bool excludeDealerTransferred = false);
        Task<IDataResult<SponsorshipCodesPaginatedDto>> GetSentExpiredCodesAsync(int sponsorId, int page = 1, int pageSize = 50, bool excludeDealerTransferred = false);

        Task<IDataResult<List<SponsorshipPurchase>>> GetSponsorPurchasesAsync(int sponsorId);
        Task<IDataResult<object>> GetSponsorshipStatisticsAsync(int sponsorId);
        Task<IDataResult<List<object>>> GetSponsoredFarmersAsync(int sponsorId);
        Task<IResult> DeactivateCodeAsync(string code, int sponsorId);
        Task<IDataResult<bool>> IsCodeValidAsync(string code);
    }
}