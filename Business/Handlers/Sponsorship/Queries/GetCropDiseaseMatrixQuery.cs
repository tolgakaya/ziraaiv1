using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get crop-disease correlation matrix analytics for sponsor
    /// Analyzes disease patterns across different crop types
    /// Cache TTL: 6 hours for relatively stable correlation data
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetCropDiseaseMatrixQuery : IRequest<IDataResult<CropDiseaseMatrixDto>>
    {
        /// <summary>
        /// Sponsor ID (null for admin view of all analyses)
        /// </summary>
        public int? SponsorId { get; set; }

        public class GetCropDiseaseMatrixQueryHandler : IRequestHandler<GetCropDiseaseMatrixQuery, IDataResult<CropDiseaseMatrixDto>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetCropDiseaseMatrixQueryHandler> _logger;

            private const string CacheKeyPrefix = "CropDiseaseMatrix";
            private const int CacheDurationMinutes = 360; // 6 hours as per spec

            public GetCropDiseaseMatrixQueryHandler(
                IPlantAnalysisRepository analysisRepository,
                ICacheManager cacheManager,
                ILogger<GetCropDiseaseMatrixQueryHandler> logger)
            {
                _analysisRepository = analysisRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            /// <summary>
            /// Generate cache key for sponsor (or "all" for admin view)
            /// </summary>
            private string GetCacheKey(int? sponsorId) => $"{CacheKeyPrefix}:{sponsorId?.ToString() ?? "all"}";

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<CropDiseaseMatrixDto>> Handle(
                GetCropDiseaseMatrixQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Check cache first
                    var cacheKey = GetCacheKey(request.SponsorId);
                    var cachedData = _cacheManager.Get<CropDiseaseMatrixDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[CropDiseaseMatrix] Cache HIT for sponsor {SponsorId}", request.SponsorId ?? 0);
                        return new SuccessDataResult<CropDiseaseMatrixDto>(
                            cachedData,
                            "Crop-disease matrix retrieved from cache");
                    }

                    _logger.LogInformation("[CropDiseaseMatrix] Cache MISS for sponsor {SponsorId} - computing matrix", request.SponsorId ?? 0);

                    // Get all analyses (filtered by sponsor if specified)
                    var allAnalyses = request.SponsorId.HasValue
                        ? await _analysisRepository.GetListAsync(a => a.SponsorCompanyId == request.SponsorId.Value)
                        : await _analysisRepository.GetListAsync(a => true);

                    var analysesList = allAnalyses
                        .Where(a => !string.IsNullOrEmpty(a.CropType) && !string.IsNullOrEmpty(a.PrimaryIssue))
                        .ToList();

                    if (!analysesList.Any())
                    {
                        return new SuccessDataResult<CropDiseaseMatrixDto>(
                            new CropDiseaseMatrixDto
                            {
                                SponsorId = request.SponsorId,
                                Matrix = new List<CropAnalysisDto>(),
                                TopOpportunities = new List<MarketOpportunityDto>()
                            },
                            "No analyses found for crop-disease matrix");
                    }

                    // Group by crop type
                    var cropGroups = analysesList.GroupBy(a => a.CropType).ToList();
                    var matrix = new List<CropAnalysisDto>();

                    foreach (var cropGroup in cropGroups)
                    {
                        var cropType = cropGroup.Key;
                        var cropAnalyses = cropGroup.ToList();
                        var totalAnalysesForCrop = cropAnalyses.Count;

                        // Group by disease within this crop
                        var diseaseGroups = cropAnalyses
                            .GroupBy(a => a.PrimaryIssue)
                            .OrderByDescending(g => g.Count())
                            .ToList();

                        var diseaseBreakdown = new List<DiseaseBreakdownDto>();

                        foreach (var diseaseGroup in diseaseGroups)
                        {
                            var disease = diseaseGroup.Key;
                            var diseaseAnalyses = diseaseGroup.ToList();
                            var occurrences = diseaseAnalyses.Count;
                            var percentage = (decimal)occurrences / totalAnalysesForCrop * 100;

                            // Calculate average severity
                            var severityCounts = diseaseAnalyses
                                .Where(a => !string.IsNullOrEmpty(a.HealthSeverity))
                                .GroupBy(a => a.HealthSeverity)
                                .OrderByDescending(g => g.Count())
                                .ToList();

                            var averageSeverity = severityCounts.Any() ? severityCounts.First().Key : "Unknown";

                            // Determine seasonal peak (based on analysis dates)
                            var seasonalPeak = DetermineSeasonalPeak(diseaseAnalyses);

                            // Get affected regions
                            var affectedRegions = diseaseAnalyses
                                .Where(a => !string.IsNullOrEmpty(a.Location))
                                .GroupBy(a => a.Location)
                                .OrderByDescending(g => g.Count())
                                .Take(5)
                                .Select(g => g.Key)
                                .ToList();

                            // Generate recommended products (simplified - can be enhanced with real product data)
                            var recommendedProducts = GenerateRecommendedProducts(disease, occurrences);

                            diseaseBreakdown.Add(new DiseaseBreakdownDto
                            {
                                Disease = disease,
                                Occurrences = occurrences,
                                Percentage = Math.Round(percentage, 2),
                                AverageSeverity = averageSeverity,
                                SeasonalPeak = seasonalPeak,
                                AffectedRegions = affectedRegions,
                                RecommendedProducts = recommendedProducts
                            });
                        }

                        matrix.Add(new CropAnalysisDto
                        {
                            CropType = cropType,
                            TotalAnalyses = totalAnalysesForCrop,
                            DiseaseBreakdown = diseaseBreakdown
                        });
                    }

                    // Sort matrix by total analyses descending
                    matrix = matrix.OrderByDescending(c => c.TotalAnalyses).ToList();

                    // Generate top market opportunities
                    var topOpportunities = GenerateTopOpportunities(matrix, analysesList);

                    var result = new CropDiseaseMatrixDto
                    {
                        SponsorId = request.SponsorId,
                        Matrix = matrix,
                        TopOpportunities = topOpportunities,
                        GeneratedAt = DateTime.Now
                    };

                    // Cache the result
                    _cacheManager.Add(cacheKey, result, CacheDurationMinutes);
                    _logger.LogInformation("[CropDiseaseMatrix] Result cached for sponsor {SponsorId}", request.SponsorId ?? 0);

                    return new SuccessDataResult<CropDiseaseMatrixDto>(
                        result,
                        "Crop-disease matrix computed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CropDiseaseMatrix] Error computing matrix for sponsor {SponsorId}", request.SponsorId ?? 0);
                    return new ErrorDataResult<CropDiseaseMatrixDto>(
                        "An error occurred while computing crop-disease matrix");
                }
            }

            /// <summary>
            /// Determine seasonal peak based on analysis dates
            /// </summary>
            private string DetermineSeasonalPeak(List<Entities.Concrete.PlantAnalysis> analyses)
            {
                var monthCounts = analyses
                    .GroupBy(a => a.AnalysisDate.Month)
                    .OrderByDescending(g => g.Count())
                    .ToList();

                if (!monthCounts.Any()) return "Unknown";

                var peakMonth = monthCounts.First().Key;
                var peakMonthName = GetMonthName(peakMonth);

                // Check if there's a second peak month close in count
                if (monthCounts.Count > 1 && monthCounts[1].Count() >= monthCounts[0].Count() * 0.8)
                {
                    var secondPeakMonth = monthCounts[1].Key;
                    var secondPeakMonthName = GetMonthName(secondPeakMonth);
                    return $"{peakMonthName}-{secondPeakMonthName}";
                }

                return peakMonthName;
            }

            /// <summary>
            /// Get month name from month number
            /// </summary>
            private string GetMonthName(int month)
            {
                var monthNames = new[] { "January", "February", "March", "April", "May", "June",
                                        "July", "August", "September", "October", "November", "December" };
                return month >= 1 && month <= 12 ? monthNames[month - 1] : "Unknown";
            }

            /// <summary>
            /// Generate recommended products based on disease type
            /// </summary>
            private List<RecommendedProductDto> GenerateRecommendedProducts(string disease, int occurrences)
            {
                var products = new List<RecommendedProductDto>();

                // Simplified product recommendation logic
                // In production, this should query a product catalog database
                var estimatedMarketSize = occurrences * 250.00m; // Estimated 250 TL per case

                if (disease.Contains("Fungus") || disease.Contains("Blight") || disease.Contains("Mildew") ||
                    disease.Contains("Rust") || disease.Contains("Spot"))
                {
                    products.Add(new RecommendedProductDto
                    {
                        ProductCategory = "Fungicide",
                        EstimatedMarketSize = Math.Round(estimatedMarketSize, 2)
                    });
                }
                else if (disease.Contains("Insect") || disease.Contains("Pest") || disease.Contains("Worm") ||
                         disease.Contains("Aphid") || disease.Contains("Mite"))
                {
                    products.Add(new RecommendedProductDto
                    {
                        ProductCategory = "Insecticide",
                        EstimatedMarketSize = Math.Round(estimatedMarketSize, 2)
                    });
                }
                else if (disease.Contains("Virus") || disease.Contains("Bacterial"))
                {
                    products.Add(new RecommendedProductDto
                    {
                        ProductCategory = "Bactericide/Antiviral",
                        EstimatedMarketSize = Math.Round(estimatedMarketSize, 2)
                    });
                }
                else
                {
                    // General treatment products
                    products.Add(new RecommendedProductDto
                    {
                        ProductCategory = "General Treatment",
                        EstimatedMarketSize = Math.Round(estimatedMarketSize, 2)
                    });
                }

                return products;
            }

            /// <summary>
            /// Generate top market opportunities from crop-disease combinations
            /// </summary>
            private List<MarketOpportunityDto> GenerateTopOpportunities(
                List<CropAnalysisDto> matrix,
                List<Entities.Concrete.PlantAnalysis> allAnalyses)
            {
                var opportunities = new List<MarketOpportunityDto>();

                foreach (var crop in matrix)
                {
                    foreach (var disease in crop.DiseaseBreakdown.Take(3)) // Top 3 diseases per crop
                    {
                        var combination = $"{crop.CropType} + {disease.Disease}";
                        var totalCases = disease.Occurrences;
                        var avgSeverity = disease.AverageSeverity;

                        // Calculate geographic concentration
                        var topRegion = disease.AffectedRegions.FirstOrDefault() ?? "Unknown";
                        var geographicConcentration = disease.AffectedRegions.Any()
                            ? $"{topRegion} ({disease.AffectedRegions.Count} regions)"
                            : "Widespread";

                        // Calculate market value
                        var marketValue = disease.RecommendedProducts.Sum(p => p.EstimatedMarketSize);

                        // Generate actionable insight
                        var insight = GenerateActionableInsight(crop.CropType, disease.Disease, totalCases, topRegion, avgSeverity);

                        opportunities.Add(new MarketOpportunityDto
                        {
                            Combination = combination,
                            TotalCases = totalCases,
                            AverageSeverity = avgSeverity,
                            GeographicConcentration = geographicConcentration,
                            MarketValue = Math.Round(marketValue, 2),
                            ActionableInsight = insight
                        });
                    }
                }

                // Sort by market value descending and take top 10
                return opportunities
                    .OrderByDescending(o => o.MarketValue)
                    .Take(10)
                    .ToList();
            }

            /// <summary>
            /// Generate actionable business insight for opportunity
            /// </summary>
            private string GenerateActionableInsight(string crop, string disease, int cases, string topRegion, string severity)
            {
                var insights = new List<string>();

                if (cases > 100)
                {
                    insights.Add($"High volume ({cases} cases) indicates significant market opportunity");
                }

                if (topRegion != "Unknown")
                {
                    insights.Add($"Geographic concentration in {topRegion} - consider regional campaign");
                }

                if (severity == "High" || severity == "Severe")
                {
                    insights.Add("High severity - farmers urgently need effective solutions");
                }

                if (!insights.Any())
                {
                    insights.Add("Monitor trend and consider targeted product offerings");
                }

                return string.Join(". ", insights) + ".";
            }
        }
    }
}
