using Business.Services.Sponsorship;
using Business.Services.Subscription;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    public class GetFilteredAnalysisForSponsorQuery : IRequest<IDataResult<SponsoredAnalysisDetailDto>>
    {
        public int SponsorId { get; set; }
        public int PlantAnalysisId { get; set; }

        public class GetFilteredAnalysisForSponsorQueryHandler : IRequestHandler<GetFilteredAnalysisForSponsorQuery, IDataResult<SponsoredAnalysisDetailDto>>
        {
            private readonly ISponsorDataAccessService _dataAccessService;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly IPlantAnalysisRepository _plantAnalysisRepository; // NEW: For fetching analysis entity
            private readonly IUserSubscriptionRepository _userSubscriptionRepository; // NEW: For analysis tier lookup
            private readonly ITierFeatureService _tierFeatureService; // NEW: For database-driven feature checks
            private readonly IMediator _mediator;

            public GetFilteredAnalysisForSponsorQueryHandler(
                ISponsorDataAccessService dataAccessService,
                ISponsorProfileRepository sponsorProfileRepository,
                ISubscriptionTierRepository subscriptionTierRepository,
                IPlantAnalysisRepository plantAnalysisRepository, // NEW
                IUserSubscriptionRepository userSubscriptionRepository, // NEW
                ITierFeatureService tierFeatureService, // NEW
                IMediator mediator)
            {
                _dataAccessService = dataAccessService;
                _sponsorProfileRepository = sponsorProfileRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
                _plantAnalysisRepository = plantAnalysisRepository; // NEW
                _userSubscriptionRepository = userSubscriptionRepository; // NEW
                _tierFeatureService = tierFeatureService; // NEW
                _mediator = mediator;
            }

            [CacheAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsoredAnalysisDetailDto>> Handle(GetFilteredAnalysisForSponsorQuery request, CancellationToken cancellationToken)
            {
                // Check if sponsor has access to this analysis
                if (!await _dataAccessService.HasAccessToAnalysisAsync(request.SponsorId, request.PlantAnalysisId))
                {
                    return new ErrorDataResult<SponsoredAnalysisDetailDto>("Access denied to this analysis");
                }

                // 🎯 REMOVED: Access percentage no longer used
                // All sponsors get full data access

                // Get RICH analysis detail (same as farmer endpoint)
                var detailQuery = new GetPlantAnalysisDetailQuery { Id = request.PlantAnalysisId };
                var detailResult = await _mediator.Send(detailQuery, cancellationToken);

                if (!detailResult.Success || detailResult.Data == null)
                    return new ErrorDataResult<SponsoredAnalysisDetailDto>("Analysis not found or access denied");

                // Record access for messaging validation (CRITICAL FIX)
                try
                {
                    var farmerId = detailResult.Data.UserId ?? 0;
                    await _dataAccessService.RecordAccessAsync(request.SponsorId, request.PlantAnalysisId, farmerId);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request if access recording fails
                    Console.WriteLine($"[GetFilteredAnalysisForSponsorQuery] Warning: Could not record access: {ex.Message}");
                }

                // 🎯 NO field filtering - show all analysis data
                // Tier controls feature access (messaging, farmer contact), not field visibility
                var filteredDetail = detailResult.Data;

                // Clean up sponsorshipMetadata from base query (farmer view)
                // We'll add proper tierMetadata for sponsor view
                filteredDetail.SponsorshipMetadata = null;

                // Get sponsor profile for branding info
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);

                // ✅ FIX: Get analysis entity to retrieve tier from ActiveSponsorshipId
                var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == request.PlantAnalysisId);
                
                // ✅ FIX: Get tier from analysis (via ActiveSponsorshipId), not from user
                var analysisTier = await GetAnalysisUsedTierAsync(analysis);
                var tierName = analysisTier?.TierName ?? "Unknown";
                var tierId = analysisTier?.Id ?? 0;

                // ✅ DATABASE-DRIVEN: Query actual tier features from database
                var canMessage = tierId > 0 ? await _tierFeatureService.HasFeatureAccessAsync(tierId, "messaging") : false;
                var canViewLogo = tierId > 0 ? await _tierFeatureService.HasFeatureAccessAsync(tierId, "sponsor_visibility") : false;
                // Farmer contact is a business rule (XL tier only), not a database feature
                var canViewFarmerContact = tierName?.ToUpper() == "XL";

                // Build response with tier metadata
                var response = new SponsoredAnalysisDetailDto
                {
                    Analysis = filteredDetail,
                    TierMetadata = new AnalysisTierMetadata
                    {
                        // 🎯 Real tier from purchases
                        TierName = tierName,
                        AccessPercentage = 100, // Always 100 - no field/count restrictions
                        CanMessage = canMessage,
                        CanReply = canMessage, // Same as CanMessage
                        CanViewLogo = canViewLogo, // Database-driven: sponsor_visibility feature
                        SponsorInfo = sponsorProfile != null ? new SponsorDisplayInfoDto
                        {
                            SponsorId = sponsorProfile.SponsorId,
                            CompanyName = sponsorProfile.CompanyName,
                            LogoUrl = sponsorProfile.SponsorLogoUrl,
                            WebsiteUrl = sponsorProfile.WebsiteUrl
                        } : null,
                        AccessibleFields = new AccessibleFieldsInfo
                        {
                            // 🎯 All analysis fields accessible (no tier-based field restrictions)
                            CanViewBasicInfo = true,
                            CanViewHealthScore = true,
                            CanViewImages = true,
                            CanViewDetailedHealth = true,
                            CanViewDiseases = true,
                            CanViewNutrients = true,
                            CanViewRecommendations = true,
                            CanViewLocation = true,
                            CanViewFarmerContact = canViewFarmerContact, // XL tier only
                            CanViewFieldData = true,
                            CanViewProcessingData = true
                        }
                    }
                };

                return new SuccessDataResult<SponsoredAnalysisDetailDto>(response);
            }

            /// <summary>
            /// Get sponsor's highest tier from their active purchases
            /// </summary>
            private async Task<SubscriptionTier> GetSponsorHighestTierAsync(SponsorProfile sponsorProfile)
            {
                if (sponsorProfile?.SponsorshipPurchases == null || !sponsorProfile.SponsorshipPurchases.Any())
                    return null;

                var activePurchases = sponsorProfile.SponsorshipPurchases
                    .Where(p => p.PaymentStatus == "Completed")
                    .ToList();

                if (!activePurchases.Any())
                    return null;

                var tierIds = activePurchases.Select(p => p.SubscriptionTierId).Distinct().ToList();
                var tiers = await _subscriptionTierRepository.GetListAsync(t => tierIds.Contains(t.Id));

                return tiers.OrderByDescending(t => GetTierPriority(t.TierName)).FirstOrDefault();
            }

            /// <summary>
            /// Get the tier that was used for a specific analysis
            /// This is the CORRECT approach - tier comes from the analysis, not from user
            /// Analysis → ActiveSponsorshipId → UserSubscription → SubscriptionTier
            /// </summary>
            private async Task<Entities.Concrete.SubscriptionTier> GetAnalysisUsedTierAsync(Entities.Concrete.PlantAnalysis analysis)
            {
                if (!analysis.ActiveSponsorshipId.HasValue)
                    return null;

                var subscription = await _userSubscriptionRepository.GetAsync(
                    s => s.Id == analysis.ActiveSponsorshipId.Value);

                if (subscription == null)
                    return null;

                return await _subscriptionTierRepository.GetAsync(
                    t => t.Id == subscription.SubscriptionTierId);
            }

            /// <summary>
            /// Get tier priority for sorting (XL > L > M > S > Trial)
            /// </summary>
            private int GetTierPriority(string tierName)
            {
                return tierName?.ToUpper() switch
                {
                    "XL" => 5,
                    "L" => 4,
                    "M" => 3,
                    "S" => 2,
                    "TRIAL" => 1,
                    _ => 0
                };
            }

            // ✅ REMOVED: Hard-coded tier methods replaced with database-driven TierFeatureService
            // Feature permissions now queried from TierFeatures table dynamically
        }
    }
}