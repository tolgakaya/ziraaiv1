using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using System;
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

                // Get sponsor profile for branding info
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);

                // Build response with tier metadata (static values)
                var response = new SponsoredAnalysisDetailDto
                {
                    Analysis = filteredDetail,
                    TierMetadata = new AnalysisTierMetadata
                    {
                        // 🎯 Static tier values - all sponsors have full access
                        TierName = "Standard",
                        AccessPercentage = 100,
                        CanMessage = true,
                        CanReply = true, // Add this if DTO has it
                        CanViewLogo = true,
                        SponsorInfo = sponsorProfile != null ? new SponsorDisplayInfoDto
                        {
                            SponsorId = sponsorProfile.SponsorId,
                            CompanyName = sponsorProfile.CompanyName,
                            LogoUrl = sponsorProfile.SponsorLogoUrl,
                            WebsiteUrl = sponsorProfile.WebsiteUrl
                        } : null,
                        AccessibleFields = new AccessibleFieldsInfo
                        {
                            // 🎯 All fields accessible (no tier-based restrictions)
                            CanViewBasicInfo = true,
                            CanViewHealthScore = true,
                            CanViewImages = true,
                            CanViewDetailedHealth = true,
                            CanViewDiseases = true,
                            CanViewNutrients = true,
                            CanViewRecommendations = true,
                            CanViewLocation = true,
                            CanViewFarmerContact = true,
                            CanViewFieldData = true,
                            CanViewProcessingData = true
                        }
                    }
                };

                return new SuccessDataResult<SponsoredAnalysisDetailDto>(response);
            }

            // 🎯 REMOVED: ApplyTierBasedFiltering and GetTierName methods
            // All analysis fields are now shown regardless of tier
            // Static tier values returned to all sponsors
        }
    }
}