using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
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

            public GetFilteredAnalysisForSponsorQueryHandler(
                ISponsorDataAccessService dataAccessService,
                ISponsorProfileRepository sponsorProfileRepository)
            {
                _dataAccessService = dataAccessService;
                _sponsorProfileRepository = sponsorProfileRepository;
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

                // Get filtered analysis data based on tier
                var filteredAnalysis = await _dataAccessService.GetFilteredAnalysisDataAsync(request.SponsorId, request.PlantAnalysisId);

                if (filteredAnalysis == null)
                    return new ErrorDataResult<SponsoredAnalysisDetailDto>("Analysis not found or access denied");

                // Get sponsor's tier information
                var accessPercentage = await _dataAccessService.GetDataAccessPercentageAsync(request.SponsorId);

                // Get sponsor profile for branding info
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);

                // Build response with tier metadata
                var response = new SponsoredAnalysisDetailDto
                {
                    Analysis = filteredAnalysis,
                    TierMetadata = new AnalysisTierMetadata
                    {
                        TierName = GetTierName(accessPercentage),
                        AccessPercentage = accessPercentage,
                        CanMessage = accessPercentage >= 30, // M, L, XL tiers (updated: M tier also has messaging)
                        CanViewLogo = true, // All tiers can see logo on result screen
                        SponsorInfo = sponsorProfile != null ? new SponsorDisplayInfoDto
                        {
                            SponsorId = sponsorProfile.SponsorId,
                            CompanyName = sponsorProfile.CompanyName,
                            LogoUrl = sponsorProfile.SponsorLogoUrl,
                            WebsiteUrl = sponsorProfile.WebsiteUrl
                        } : null,
                        AccessibleFields = new AccessibleFieldsInfo
                        {
                            // 30% Access
                            CanViewBasicInfo = accessPercentage >= 30,
                            CanViewHealthScore = accessPercentage >= 30,
                            CanViewImages = accessPercentage >= 30,

                            // 60% Access
                            CanViewDetailedHealth = accessPercentage >= 60,
                            CanViewDiseases = accessPercentage >= 60,
                            CanViewNutrients = accessPercentage >= 60,
                            CanViewRecommendations = accessPercentage >= 60,
                            CanViewLocation = accessPercentage >= 60,

                            // 100% Access
                            CanViewFarmerContact = accessPercentage >= 100,
                            CanViewFieldData = accessPercentage >= 100,
                            CanViewProcessingData = accessPercentage >= 100
                        }
                    }
                };

                return new SuccessDataResult<SponsoredAnalysisDetailDto>(response);
            }

            private string GetTierName(int accessPercentage)
            {
                return accessPercentage switch
                {
                    30 => "S/M",
                    60 => "L",
                    100 => "XL",
                    _ => "Unknown"
                };
            }
        }
    }
}