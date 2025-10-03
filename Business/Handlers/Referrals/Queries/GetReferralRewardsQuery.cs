using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.Referral;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Referrals.Queries
{
    /// <summary>
    /// Get reward history for a user
    /// </summary>
    public class GetReferralRewardsQuery : IRequest<IDataResult<List<ReferralRewardDto>>>
    {
        public int UserId { get; set; }

        public class GetReferralRewardsQueryHandler : IRequestHandler<GetReferralRewardsQuery, IDataResult<List<ReferralRewardDto>>>
        {
            private readonly IReferralRewardService _rewardService;
            private readonly IUserRepository _userRepository;
            private readonly ILogger<GetReferralRewardsQueryHandler> _logger;

            public GetReferralRewardsQueryHandler(
                IReferralRewardService rewardService,
                IUserRepository userRepository,
                ILogger<GetReferralRewardsQueryHandler> logger)
            {
                _rewardService = rewardService;
                _userRepository = userRepository;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [CacheAspect(duration: 15)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<ReferralRewardDto>>> Handle(GetReferralRewardsQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Getting reward history for user {UserId}", request.UserId);

                    var rewardsResult = await _rewardService.GetRewardsByReferrerAsync(request.UserId);
                    if (!rewardsResult.Success)
                        return new ErrorDataResult<List<ReferralRewardDto>>(rewardsResult.Message);

                    var rewards = rewardsResult.Data;

                    var rewardDtos = new List<ReferralRewardDto>();
                    foreach (var reward in rewards)
                    {
                        var refereeUser = await _userRepository.GetAsync(u => u.UserId == reward.RefereeUserId);
                        
                        rewardDtos.Add(new ReferralRewardDto
                        {
                            Id = reward.Id,
                            RefereeUserName = refereeUser?.FullName ?? "Unknown User",
                            CreditAmount = reward.CreditAmount,
                            AwardedAt = reward.AwardedAt
                        });
                    }

                    return new SuccessDataResult<List<ReferralRewardDto>>(rewardDtos.OrderByDescending(r => r.AwardedAt).ToList());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting reward history for user {UserId}", request.UserId);
                    return new ErrorDataResult<List<ReferralRewardDto>>("Failed to retrieve reward history");
                }
            }
        }
    }
}
