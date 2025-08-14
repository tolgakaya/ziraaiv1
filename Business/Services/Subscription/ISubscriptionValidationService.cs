using System.Threading.Tasks;
using Entities.Dtos;
using IResult = Core.Utilities.Results.IResult;
using IDataResult = Core.Utilities.Results.IDataResult<Entities.Dtos.SubscriptionUsageStatusDto>;

namespace Business.Services.Subscription
{
    public interface ISubscriptionValidationService
    {
        Task<IDataResult> CheckSubscriptionStatusAsync(int userId);
        Task<IResult> ValidateAndLogUsageAsync(int userId, string endpoint, string method);
        Task<bool> CanUserMakeRequestAsync(int userId);
        Task<IResult> IncrementUsageAsync(int userId, int? plantAnalysisId = null);
        Task<Core.Utilities.Results.IDataResult<string>> GetSubscriptionSponsorAsync(int userId);
        Task<Core.Utilities.Results.IDataResult<Entities.Dtos.SponsorshipDetailsDto>> GetSponsorshipDetailsAsync(int userId);
        Task ResetDailyUsageForAllUsersAsync();
        Task ResetMonthlyUsageForAllUsersAsync();
        Task ProcessExpiredSubscriptionsAsync();
        Task ProcessAutoRenewalsAsync();
    }
}