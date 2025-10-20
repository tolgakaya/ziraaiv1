using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    /// <summary>
    /// Get paginated list of analyses for sponsor with tier-based filtering
    /// </summary>
    public class GetSponsoredAnalysesListQuery : IRequest<IDataResult<SponsoredAnalysesListResponseDto>>
    {
        public int SponsorId { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Sorting
        public string SortBy { get; set; } = "date"; // date, healthScore, cropType
        public string SortOrder { get; set; } = "desc"; // asc, desc

        // Filters
        public string FilterByTier { get; set; } // S, M, L, XL
        public string FilterByCropType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetSponsoredAnalysesListQueryHandler : IRequestHandler<GetSponsoredAnalysesListQuery, IDataResult<SponsoredAnalysesListResponseDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly ISponsorDataAccessService _dataAccessService;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly IUserRepository _userRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;

            public GetSponsoredAnalysesListQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                ISponsorDataAccessService dataAccessService,
                ISponsorProfileRepository sponsorProfileRepository,
                IUserRepository userRepository,
                ISubscriptionTierRepository subscriptionTierRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _dataAccessService = dataAccessService;
                _sponsorProfileRepository = sponsorProfileRepository;
                _userRepository = userRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsoredAnalysesListResponseDto>> Handle(GetSponsoredAnalysesListQuery request, CancellationToken cancellationToken)
            {
                // Validate sponsor profile
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(request.SponsorId);
                if (sponsorProfile == null || !sponsorProfile.IsActive)
                {
                    return new ErrorDataResult<SponsoredAnalysesListResponseDto>("Sponsor profile not found or inactive");
                }

                // Get sponsor's access percentage
                var accessPercentage = await _dataAccessService.GetDataAccessPercentageAsync(request.SponsorId);

                // Build query: Get all analyses where sponsor has sponsored the farmer
                var query = _plantAnalysisRepository.GetListAsync(a =>
                    a.SponsorUserId == request.SponsorId &&
                    a.AnalysisStatus != null
                );

                var allAnalyses = await query;
                var analysesQuery = allAnalyses.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.FilterByCropType))
                {
                    analysesQuery = analysesQuery.Where(a =>
                        a.CropType != null &&
                        a.CropType.Contains(request.FilterByCropType, StringComparison.OrdinalIgnoreCase));
                }

                if (request.StartDate.HasValue)
                {
                    analysesQuery = analysesQuery.Where(a => a.AnalysisDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    analysesQuery = analysesQuery.Where(a => a.AnalysisDate <= request.EndDate.Value);
                }

                // Apply sorting
                analysesQuery = request.SortBy?.ToLower() switch
                {
                    "healthscore" => request.SortOrder?.ToLower() == "asc"
                        ? analysesQuery.OrderBy(a => a.OverallHealthScore)
                        : analysesQuery.OrderByDescending(a => a.OverallHealthScore),
                    "croptype" => request.SortOrder?.ToLower() == "asc"
                        ? analysesQuery.OrderBy(a => a.CropType)
                        : analysesQuery.OrderByDescending(a => a.CropType),
                    _ => request.SortOrder?.ToLower() == "asc"
                        ? analysesQuery.OrderBy(a => a.AnalysisDate)
                        : analysesQuery.OrderByDescending(a => a.AnalysisDate)
                };

                var filteredAnalyses = analysesQuery.ToList();
                var totalCount = filteredAnalyses.Count;

                // Calculate pagination
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;
                var pagedAnalyses = filteredAnalyses.Skip(skip).Take(request.PageSize).ToList();

                // Map to DTOs with tier-based filtering
                var items = pagedAnalyses.Select(analysis => MapToSummaryDto(
                    analysis,
                    accessPercentage,
                    sponsorProfile
                )).ToArray();

                // Calculate summary statistics
                var summary = new SponsoredAnalysesListSummaryDto
                {
                    TotalAnalyses = totalCount,
                    AverageHealthScore = filteredAnalyses.Any()
                        ? (decimal)filteredAnalyses.Average(a => a.OverallHealthScore)
                        : 0,
                    TopCropTypes = filteredAnalyses
                        .Where(a => !string.IsNullOrEmpty(a.CropType))
                        .GroupBy(a => a.CropType)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => g.Key)
                        .ToArray(),
                    AnalysesThisMonth = filteredAnalyses
                        .Count(a => a.AnalysisDate.Month == DateTime.Now.Month &&
                                    a.AnalysisDate.Year == DateTime.Now.Year)
                };

                var response = new SponsoredAnalysesListResponseDto
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1,
                    Summary = summary
                };

                return new SuccessDataResult<SponsoredAnalysesListResponseDto>(
                    response,
                    $"Retrieved {items.Length} analyses (page {request.Page} of {totalPages})"
                );
            }

            private SponsoredAnalysisSummaryDto MapToSummaryDto(
                Entities.Concrete.PlantAnalysis analysis,
                int accessPercentage,
                Entities.Concrete.SponsorProfile sponsorProfile)
            {
                var dto = new SponsoredAnalysisSummaryDto
                {
                    // Core fields (always available)
                    AnalysisId = analysis.Id,
                    AnalysisDate = analysis.AnalysisDate,
                    AnalysisStatus = analysis.AnalysisStatus,
                    CropType = analysis.CropType,

                    // Tier info
                    TierName = GetTierName(accessPercentage),
                    AccessPercentage = accessPercentage,
                    CanMessage = accessPercentage >= 30, // M, L, XL
                    CanViewLogo = true, // All tiers on result screen

                    // Sponsor info
                    SponsorInfo = new SponsorDisplayInfoDto
                    {
                        SponsorId = sponsorProfile.SponsorId,
                        CompanyName = sponsorProfile.CompanyName,
                        LogoUrl = sponsorProfile.SponsorLogoUrl,
                        WebsiteUrl = sponsorProfile.WebsiteUrl
                    }
                };

                // 30% Access Fields (S & M tiers)
                if (accessPercentage >= 30)
                {
                    dto.OverallHealthScore = analysis.OverallHealthScore;
                    dto.PlantSpecies = analysis.PlantSpecies;
                    dto.PlantVariety = analysis.PlantVariety;
                    dto.GrowthStage = analysis.GrowthStage;
                    // Use ImageUrl (original) or ImagePath (thumbnail) - prefer original for better quality
                    dto.ImageUrl = !string.IsNullOrEmpty(analysis.ImageUrl)
                        ? analysis.ImageUrl
                        : analysis.ImagePath;
                }

                // 60% Access Fields (L tier)
                if (accessPercentage >= 60)
                {
                    dto.VigorScore = analysis.VigorScore;
                    dto.HealthSeverity = analysis.HealthSeverity;
                    dto.PrimaryConcern = analysis.PrimaryConcern;
                    dto.Location = analysis.Location;
                    // Recommendations removed from list view - too large for list display
                    // Use GET /api/v1/sponsorship/analyses/{id} for full details including recommendations
                }

                // 100% Access Fields (XL tier)
                if (accessPercentage >= 100)
                {
                    // Fetch farmer info from User entity if available
                    if (analysis.UserId.HasValue)
                    {
                        var farmer = _userRepository.Get(u => u.UserId == analysis.UserId.Value);
                        if (farmer != null)
                        {
                            dto.FarmerName = farmer.FullName;
                            dto.FarmerPhone = farmer.MobilePhones ?? analysis.ContactPhone;
                            dto.FarmerEmail = farmer.Email ?? analysis.ContactEmail;
                        }
                    }

                    // Fallback to analysis contact info if User not found
                    if (string.IsNullOrEmpty(dto.FarmerName))
                    {
                        dto.FarmerPhone = analysis.ContactPhone;
                        dto.FarmerEmail = analysis.ContactEmail;
                    }
                }

                return dto;
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
