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
            private readonly IMediator _mediator;

            public GetFilteredAnalysisForSponsorQueryHandler(
                ISponsorDataAccessService dataAccessService,
                ISponsorProfileRepository sponsorProfileRepository,
                IMediator mediator)
            {
                _dataAccessService = dataAccessService;
                _sponsorProfileRepository = sponsorProfileRepository;
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

                // Get sponsor's tier information first
                var accessPercentage = await _dataAccessService.GetDataAccessPercentageAsync(request.SponsorId);

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

                // Apply tier-based filtering to the rich DTO
                var filteredDetail = ApplyTierBasedFiltering(detailResult.Data, accessPercentage);

                // Get sponsor profile for branding info
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);

                // Build response with tier metadata
                var response = new SponsoredAnalysisDetailDto
                {
                    Analysis = filteredDetail,
                    TierMetadata = new AnalysisTierMetadata
                    {
                        TierName = GetTierName(accessPercentage),
                        AccessPercentage = accessPercentage,
                        CanMessage = accessPercentage >= 30, // M, L, XL tiers
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

            /// <summary>
            /// Apply tier-based filtering to PlantAnalysisDetailDto
            /// Nullifies fields based on sponsor's access level
            /// </summary>
            private PlantAnalysisDetailDto ApplyTierBasedFiltering(PlantAnalysisDetailDto detail, int accessPercentage)
            {
                // 30% Access: Basic info, health score, images
                // Fields available: PlantIdentification (basic), Summary.OverallHealthScore, ImageInfo

                // 60% Access: + Detailed health, nutrients, recommendations, location
                if (accessPercentage < 60)
                {
                    // Remove 60% fields
                    detail.HealthAssessment = null;
                    detail.NutrientStatus = null;
                    detail.PestDisease = null;
                    detail.EnvironmentalStress = null;
                    detail.Recommendations = null;
                    detail.CrossFactorInsights = null;
                    detail.RiskAssessment = null;
                    detail.Location = null;
                    detail.Latitude = null;
                    detail.Longitude = null;
                    detail.WeatherConditions = null;
                    detail.Temperature = null;
                    detail.Humidity = null;
                    detail.SoilType = null;
                }

                // 100% Access: + Farmer contact, field data, processing info
                if (accessPercentage < 100)
                {
                    // Remove 100% fields
                    detail.ContactPhone = null;
                    detail.ContactEmail = null;
                    detail.FieldId = null;
                    detail.PlantingDate = null;
                    detail.ExpectedHarvestDate = null;
                    detail.LastFertilization = null;
                    detail.LastIrrigation = null;
                    detail.PreviousTreatments = null;
                    detail.UrgencyLevel = null;
                    detail.Notes = null;
                    detail.AdditionalInfo = null;
                    detail.ProcessingInfo = null;
                    detail.TokenUsage = null;
                    detail.RequestMetadata = null;
                }

                return detail;
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