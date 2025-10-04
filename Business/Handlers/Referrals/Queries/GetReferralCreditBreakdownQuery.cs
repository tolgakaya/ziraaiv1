using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.Referral;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Referrals.Queries
{
    /// <summary>
    /// Get referral credit breakdown (earned, used, balance)
    /// </summary>
    public class GetReferralCreditBreakdownQuery : IRequest<IDataResult<ReferralCreditBreakdownDto>>
    {
        public int UserId { get; set; }

        public class GetReferralCreditBreakdownQueryHandler : IRequestHandler<GetReferralCreditBreakdownQuery, IDataResult<ReferralCreditBreakdownDto>>
        {
            private readonly IReferralRewardService _rewardService;
            private readonly ILogger<GetReferralCreditBreakdownQueryHandler> _logger;

            public GetReferralCreditBreakdownQueryHandler(
                IReferralRewardService rewardService,
                ILogger<GetReferralCreditBreakdownQueryHandler> logger)
            {
                _rewardService = rewardService;
                _logger = logger;
            }

            // Cache removed - referral data must be real-time
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<ReferralCreditBreakdownDto>> Handle(GetReferralCreditBreakdownQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Getting credit breakdown for user {UserId}", request.UserId);

                    var result = await _rewardService.GetCreditBreakdownAsync(request.UserId);
                    
                    if (!result.Success)
                        return new ErrorDataResult<ReferralCreditBreakdownDto>(result.Message);

                    var breakdown = new ReferralCreditBreakdownDto
                    {
                        TotalEarned = result.Data.TotalEarned,
                        TotalUsed = result.Data.TotalUsed,
                        CurrentBalance = result.Data.CurrentBalance
                    };

                    return new SuccessDataResult<ReferralCreditBreakdownDto>(breakdown);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting credit breakdown for user {UserId}", request.UserId);
                    return new ErrorDataResult<ReferralCreditBreakdownDto>("Failed to retrieve credit breakdown");
                }
            }
        }
    }
}
