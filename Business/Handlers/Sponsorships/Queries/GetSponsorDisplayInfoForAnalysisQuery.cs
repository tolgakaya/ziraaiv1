using Business.BusinessAspects;
using Business.Helpers;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.Sponsorships.Queries
{
    public class GetSponsorDisplayInfoForAnalysisQuery : IRequest<IDataResult<SponsorDisplayInfoDto>>
    {
        public int PlantAnalysisId { get; set; }
        public string Screen { get; set; } = "result";

        public class GetSponsorDisplayInfoForAnalysisQueryHandler : IRequestHandler<GetSponsorDisplayInfoForAnalysisQuery, IDataResult<SponsorDisplayInfoDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;

            public GetSponsorDisplayInfoForAnalysisQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                ISponsorProfileRepository sponsorProfileRepository,
                ISponsorshipCodeRepository sponsorshipCodeRepository,
                ISubscriptionTierRepository subscriptionTierRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _sponsorProfileRepository = sponsorProfileRepository;
                _sponsorshipCodeRepository = sponsorshipCodeRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
            }

            [CacheAspect(60)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorDisplayInfoDto>> Handle(GetSponsorDisplayInfoForAnalysisQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    // Get plant analysis with related data
                    var plantAnalysis = await _plantAnalysisRepository.GetAsync(p => p.Id == request.PlantAnalysisId);
                    
                    if (plantAnalysis == null)
                    {
                        return new ErrorDataResult<SponsorDisplayInfoDto>("Plant analysis not found");
                    }

                    // Check if this analysis has a sponsor
                    if (!plantAnalysis.SponsorUserId.HasValue || !plantAnalysis.SponsorshipCodeId.HasValue)
                    {
                        return new ErrorDataResult<SponsorDisplayInfoDto>("This analysis is not sponsored");
                    }

                    // Get sponsor profile using the Sponsor ID (which is the User ID in the SponsorProfile)
                    var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == plantAnalysis.SponsorUserId.Value);
                    
                    if (sponsorProfile == null || !sponsorProfile.IsActive)
                    {
                        return new ErrorDataResult<SponsorDisplayInfoDto>("Sponsor profile not found or inactive");
                    }

                    // Get sponsorship code to determine tier
                    var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(sc => sc.Id == plantAnalysis.SponsorshipCodeId.Value);
                    
                    if (sponsorshipCode == null)
                    {
                        return new ErrorDataResult<SponsorDisplayInfoDto>("Sponsorship code not found");
                    }

                    // Get subscription tier details
                    var subscriptionTier = await _subscriptionTierRepository.GetAsync(st => st.Id == sponsorshipCode.SubscriptionTierId);
                    
                    if (subscriptionTier == null)
                    {
                        return new ErrorDataResult<SponsorDisplayInfoDto>("Subscription tier not found");
                    }

                    // Determine if logo can be displayed based on tier and screen
                    bool canDisplay = DetermineLogoVisibility(subscriptionTier.TierName, request.Screen);

                    var displayInfo = new SponsorDisplayInfoDto
                    {
                        SponsorLogoUrl = sponsorProfile.SponsorLogoUrl,
                        CompanyName = sponsorProfile.CompanyName,
                        WebsiteUrl = sponsorProfile.WebsiteUrl,
                        TierName = subscriptionTier.TierName,
                        CanDisplay = canDisplay,
                        SponsorId = sponsorProfile.Id,
                        PlantAnalysisId = request.PlantAnalysisId,
                        Screen = request.Screen
                    };

                    if (!canDisplay)
                    {
                        // If cannot display, return limited info
                        displayInfo.Reason = $"{subscriptionTier.TierName} tier cannot display logo on {request.Screen} screen";
                        displayInfo.SponsorLogoUrl = null;
                        displayInfo.WebsiteUrl = null;
                    }

                    return new SuccessDataResult<SponsorDisplayInfoDto>(displayInfo, 
                        canDisplay ? "Sponsor display info retrieved successfully" : "Logo cannot be displayed on this screen");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GetSponsorDisplayInfoForAnalysis] Exception: {ex.Message}");
                    return new ErrorDataResult<SponsorDisplayInfoDto>($"Error retrieving sponsor display info: {ex.Message}");
                }
            }

            private bool DetermineLogoVisibility(string tierName, string screen)
            {
                // Business rules for logo visibility based on tier
                screen = screen?.ToLower() ?? "result";
                tierName = tierName?.ToUpper() ?? "";

                switch (tierName)
                {
                    case "S":
                        // S tier: Only result screen
                        return screen == "result";
                        
                    case "M":
                        // M tier: Start and result screens
                        return screen == "start" || screen == "result";
                        
                    case "L":
                    case "XL":
                        // L and XL tiers: All screens
                        return screen == "start" || screen == "result" || screen == "analysis" || screen == "profile";
                        
                    default:
                        return false;
                }
            }
        }
    }

    public class SponsorDisplayInfoDto
    {
        public int SponsorId { get; set; }
        public int PlantAnalysisId { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string CompanyName { get; set; }
        public string WebsiteUrl { get; set; }
        public string TierName { get; set; }
        public bool CanDisplay { get; set; }
        public string Screen { get; set; }
        public string Reason { get; set; }
    }
}