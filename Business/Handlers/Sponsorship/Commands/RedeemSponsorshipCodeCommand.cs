using Business.Services.Sponsorship;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class RedeemSponsorshipCodeCommand : IRequest<IDataResult<UserSubscription>>
    {
        [JsonPropertyName("code")]
        [Required(ErrorMessage = "Sponsorship code is required")]
        public string Code { get; set; }

        public int UserId { get; set; }
        public string UserEmail { get; set; } // For logging
        public string UserFullName { get; set; } // For logging

        public class RedeemSponsorshipCodeCommandHandler : IRequestHandler<RedeemSponsorshipCodeCommand, IDataResult<UserSubscription>>
        {
            private readonly ISponsorshipService _sponsorshipService;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ICacheManager _cacheManager;
            private readonly Business.Services.AdminAnalytics.IAdminStatisticsCacheService _adminCacheService;

            public RedeemSponsorshipCodeCommandHandler(
                ISponsorshipService sponsorshipService,
                ISponsorshipCodeRepository codeRepository,
                ICacheManager cacheManager,
                Business.Services.AdminAnalytics.IAdminStatisticsCacheService adminCacheService)
            {
                _sponsorshipService = sponsorshipService;
                _codeRepository = codeRepository;
                _cacheManager = cacheManager;
                _adminCacheService = adminCacheService;
            }

            public async Task<IDataResult<UserSubscription>> Handle(RedeemSponsorshipCodeCommand request, CancellationToken cancellationToken)
            {
                // Log the redemption attempt
                System.Console.WriteLine($"[SponsorshipRedeem] User {request.UserEmail} attempting to redeem code: {request.Code}");

                // Get the code to retrieve sponsor ID for cache invalidation
                var codeEntity = await _codeRepository.GetAsync(c => c.Code == request.Code);

                var result = await _sponsorshipService.RedeemSponsorshipCodeAsync(request.Code, request.UserId);

                if (result.Success && codeEntity != null)
                {
                    System.Console.WriteLine($"[SponsorshipRedeem] ✅ Code {request.Code} successfully redeemed by user {request.UserEmail}");

                    // Invalidate sponsor analytics caches (code redemption affects temporal and ROI analytics)
                    var sponsorId = codeEntity.SponsorId;
                    _cacheManager.RemoveByPattern($"SponsorTemporalAnalytics:{sponsorId}*");
                    _cacheManager.RemoveByPattern($"SponsorROIAnalytics:{sponsorId}");
                    System.Console.WriteLine($"[SponsorAnalyticsCache] 🗑️ Invalidated temporal and ROI analytics cache for sponsor {sponsorId}");

                    // Invalidate admin statistics cache (subscription statistics changed)
                    await _adminCacheService.InvalidateAllStatisticsAsync();
                    System.Console.WriteLine($"[AdminStatsCache] 🗑️ Invalidated admin statistics cache after code redemption");
                }
                else
                {
                    System.Console.WriteLine($"[SponsorshipRedeem] ❌ Failed to redeem code {request.Code} for user {request.UserEmail}: {result.Message}");
                }

                return result;
            }
        }
    }
}