using Business.BusinessAspects;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorships.Queries
{
    public class GetLogoPermissionsForAnalysisQuery : IRequest<IDataResult<LogoPermissionsDto>>
    {
        public int PlantAnalysisId { get; set; }
        public string Screen { get; set; } = "result";

        public class GetLogoPermissionsForAnalysisQueryHandler : IRequestHandler<GetLogoPermissionsForAnalysisQuery, IDataResult<LogoPermissionsDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;

            public GetLogoPermissionsForAnalysisQueryHandler(
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
            public async Task<IDataResult<LogoPermissionsDto>> Handle(GetLogoPermissionsForAnalysisQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    // Get plant analysis with related data
                    var plantAnalysis = await _plantAnalysisRepository.GetAsync(p => p.Id == request.PlantAnalysisId);
                    
                    if (plantAnalysis == null)
                    {
                        return new ErrorDataResult<LogoPermissionsDto>("Plant analysis not found");
                    }

                    // Check if this analysis has a sponsor
                    if (!plantAnalysis.SponsorUserId.HasValue || !plantAnalysis.SponsorshipCodeId.HasValue)
                    {
                        return new ErrorDataResult<LogoPermissionsDto>("This analysis is not sponsored");
                    }

                    // Get sponsor profile
                    var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == plantAnalysis.SponsorUserId.Value);
                    
                    if (sponsorProfile == null || !sponsorProfile.IsActive)
                    {
                        return new ErrorDataResult<LogoPermissionsDto>("Sponsor profile not found or inactive");
                    }

                    // Get sponsorship code to determine tier
                    var sponsorshipCode = await _sponsorshipCodeRepository.GetAsync(sc => sc.Id == plantAnalysis.SponsorshipCodeId.Value);
                    
                    if (sponsorshipCode == null)
                    {
                        return new ErrorDataResult<LogoPermissionsDto>("Sponsorship code not found");
                    }

                    // Get subscription tier details
                    var subscriptionTier = await _subscriptionTierRepository.GetAsync(st => st.Id == sponsorshipCode.SubscriptionTierId);
                    
                    if (subscriptionTier == null)
                    {
                        return new ErrorDataResult<LogoPermissionsDto>("Subscription tier not found");
                    }

                    // Determine if logo can be displayed based on tier and screen
                    bool canDisplay = DetermineLogoVisibility(subscriptionTier.TierName, request.Screen);

                    var permissions = new LogoPermissionsDto
                    {
                        PlantAnalysisId = request.PlantAnalysisId,
                        SponsorId = sponsorProfile.Id,
                        TierName = subscriptionTier.TierName,
                        Screen = request.Screen,
                        CanDisplayLogo = canDisplay,
                        CompanyName = sponsorProfile.CompanyName,
                        LogoUrl = canDisplay ? sponsorProfile.SponsorLogoUrl : null,
                        WebsiteUrl = canDisplay ? sponsorProfile.WebsiteUrl : null
                    };

                    if (!canDisplay)
                    {
                        permissions.Reason = $"{subscriptionTier.TierName} tier cannot display logo on {request.Screen} screen";
                    }

                    return new SuccessDataResult<LogoPermissionsDto>(permissions, 
                        canDisplay ? "Logo permissions retrieved successfully" : "Logo display not permitted for this tier and screen");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GetLogoPermissionsForAnalysis] Exception: {ex.Message}");
                    return new ErrorDataResult<LogoPermissionsDto>($"Error retrieving logo permissions: {ex.Message}");
                }
            }

            private bool DetermineLogoVisibility(string tierName, string screen)
            {
                // Business rules for logo visibility based on tier - same as display-info
                screen = NormalizeScreenParameter(screen?.ToLower() ?? "result");
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

            private string NormalizeScreenParameter(string screen)
            {
                // Handle plural forms and common variations - same as display-info
                switch (screen)
                {
                    case "results":
                        return "result";
                    case "analyses":
                        return "analysis";
                    case "profiles":
                        return "profile";
                    case "starts":
                        return "start";
                    default:
                        return screen;
                }
            }
        }
    }

    public class LogoPermissionsDto
    {
        public int PlantAnalysisId { get; set; }
        public int SponsorId { get; set; }
        public string TierName { get; set; }
        public string Screen { get; set; }
        public bool CanDisplayLogo { get; set; }
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string Reason { get; set; }
    }
}