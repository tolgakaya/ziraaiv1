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
    /// Get comprehensive referral statistics for a user
    /// </summary>
    public class GetReferralStatsQuery : IRequest<IDataResult<ReferralStatsResponse>>
    {
        public int UserId { get; set; }

        public class GetReferralStatsQueryHandler : IRequestHandler<GetReferralStatsQuery, IDataResult<ReferralStatsResponse>>
        {
            private readonly IReferralTrackingService _trackingService;
            private readonly IReferralRewardService _rewardService;
            private readonly ILogger<GetReferralStatsQueryHandler> _logger;

            public GetReferralStatsQueryHandler(
                IReferralTrackingService trackingService,
                IReferralRewardService rewardService,
                ILogger<GetReferralStatsQueryHandler> logger)
            {
                _trackingService = trackingService;
                _rewardService = rewardService;
                _logger = logger;
            }

            // Cache removed - referral data must be real-time
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<ReferralStatsResponse>> Handle(GetReferralStatsQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Getting referral stats for user {UserId}", request.UserId);

                    var statsResult = await _trackingService.GetStatsByReferrerAsync(request.UserId);
                    if (!statsResult.Success)
                        return new ErrorDataResult<ReferralStatsResponse>(statsResult.Message);

                    var stats = statsResult.Data;

                    var totalCreditsResult = await _rewardService.GetTotalCreditsEarnedAsync(request.UserId);
                    var totalCreditsEarned = totalCreditsResult.Success ? totalCreditsResult.Data : 0;

                    // Extract stats from dictionary
                    stats.TryGetValue("Total", out int total);
                    stats.TryGetValue("Clicked", out int clicked);
                    stats.TryGetValue("Registered", out int registered);
                    stats.TryGetValue("Validated", out int validated);
                    stats.TryGetValue("Rewarded", out int rewarded);

                    var response = new ReferralStatsResponse
                    {
                        TotalReferrals = total,
                        SuccessfulReferrals = rewarded,
                        PendingReferrals = registered - rewarded,
                        TotalCreditsEarned = totalCreditsEarned,
                        ReferralBreakdown = new ReferralBreakdownDto
                        {
                            Clicked = clicked,
                            Registered = registered,
                            Validated = validated,
                            Rewarded = rewarded
                        }
                    };

                    return new SuccessDataResult<ReferralStatsResponse>(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting referral stats for user {UserId}", request.UserId);
                    return new ErrorDataResult<ReferralStatsResponse>("Failed to retrieve referral statistics");
                }
            }
        }
    }
}
