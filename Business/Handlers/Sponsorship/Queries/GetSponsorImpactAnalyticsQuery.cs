using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Utilities;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get comprehensive impact analytics for sponsor
    /// Includes farmer reach, agricultural impact, geographic coverage, and severity distribution
    /// Cache TTL: 6 hours for relatively stable impact data
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetSponsorImpactAnalyticsQuery : IRequest<IDataResult<SponsorImpactAnalyticsDto>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorImpactAnalyticsQueryHandler : IRequestHandler<GetSponsorImpactAnalyticsQuery, IDataResult<SponsorImpactAnalyticsDto>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetSponsorImpactAnalyticsQueryHandler> _logger;

            private const string CacheKeyPrefix = "SponsorImpactAnalytics";
            private const int CacheDurationMinutes = 360; // 6 hours as per spec

            public GetSponsorImpactAnalyticsQueryHandler(
                IPlantAnalysisRepository analysisRepository,
                IUserSubscriptionRepository subscriptionRepository,
                ICacheManager cacheManager,
                ILogger<GetSponsorImpactAnalyticsQueryHandler> logger)
            {
                _analysisRepository = analysisRepository;
                _subscriptionRepository = subscriptionRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            /// <summary>
            /// Generate cache key for sponsor
            /// </summary>
            private string GetCacheKey(int sponsorId) => $"{CacheKeyPrefix}:{sponsorId}";

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<SponsorImpactAnalyticsDto>> Handle(
                GetSponsorImpactAnalyticsQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Check cache first
                    var cacheKey = GetCacheKey(request.SponsorId);
                    var cachedData = _cacheManager.Get<SponsorImpactAnalyticsDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[ImpactAnalytics] Cache HIT for sponsor {SponsorId}", request.SponsorId);
                        return new SuccessDataResult<SponsorImpactAnalyticsDto>(
                            cachedData,
                            "Impact analytics retrieved from cache");
                    }

                    _logger.LogInformation("[ImpactAnalytics] Cache MISS for sponsor {SponsorId} - fetching from database", request.SponsorId);

                    // Get all sponsored analyses
                    var allAnalyses = await _analysisRepository.GetListAsync(
                        a => a.SponsorCompanyId.HasValue && a.SponsorCompanyId.Value == request.SponsorId);

                    var analysesList = allAnalyses.ToList();

                    if (!analysesList.Any())
                    {
                        return new SuccessDataResult<SponsorImpactAnalyticsDto>(
                            new SponsorImpactAnalyticsDto
                            {
                                DataStartDate = DateTime.Now.AddMonths(-1),
                                DataEndDate = DateTime.Now
                            },
                            "No sponsored analyses found");
                    }

                    // Calculate Farmer Impact Metrics
                    var uniqueFarmers = analysesList
                        .Where(a => a.UserId.HasValue)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .ToList();

                    var totalFarmersReached = uniqueFarmers.Count;

                    var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                    var activeFarmersLast30Days = analysesList
                        .Where(a => a.UserId.HasValue && a.AnalysisDate >= thirtyDaysAgo)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .Count();

                    // Calculate farmer retention rate (month-over-month)
                    var currentMonth = DateTime.Now;
                    var lastMonth = currentMonth.AddMonths(-1);
                    var twoMonthsAgo = currentMonth.AddMonths(-2);

                    var farmersThisMonth = analysesList
                        .Where(a => a.UserId.HasValue && 
                                   a.AnalysisDate.Year == currentMonth.Year && 
                                   a.AnalysisDate.Month == currentMonth.Month)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .ToList();

                    var farmersLastMonth = analysesList
                        .Where(a => a.UserId.HasValue && 
                                   a.AnalysisDate.Year == lastMonth.Year && 
                                   a.AnalysisDate.Month == lastMonth.Month)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .ToList();

                    var retainedFarmers = farmersThisMonth.Intersect(farmersLastMonth).Count();
                    var farmerRetentionRate = farmersLastMonth.Count > 0
                        ? (double)retainedFarmers / farmersLastMonth.Count * 100
                        : 0;

                    // Calculate average farmer lifetime
                    var farmerLifetimes = new List<double>();
                    foreach (var farmerId in uniqueFarmers)
                    {
                        var farmerAnalyses = analysesList.Where(a => a.UserId == farmerId).ToList();
                        if (farmerAnalyses.Count > 1)
                        {
                            var firstAnalysis = farmerAnalyses.Min(a => a.AnalysisDate);
                            var lastAnalysis = farmerAnalyses.Max(a => a.AnalysisDate);
                            var lifetime = (lastAnalysis - firstAnalysis).TotalDays;
                            farmerLifetimes.Add(lifetime);
                        }
                    }

                    var averageFarmerLifetimeDays = farmerLifetimes.Any() 
                        ? farmerLifetimes.Average() 
                        : 0;

                    // Calculate Agricultural Impact Metrics
                    var totalCropsAnalyzed = analysesList.Count;
                    var uniqueCropTypes = analysesList
                        .Where(a => !string.IsNullOrEmpty(a.CropType))
                        .Select(a => a.CropType)
                        .Distinct()
                        .Count();

                    var diseasesDetected = analysesList
                        .Count(a => !string.IsNullOrEmpty(a.PrimaryIssue) && a.PrimaryIssue != "Unknown");

                    var criticalIssuesResolved = analysesList
                        .Count(a => a.HealthSeverity == "Critical");

                    // Calculate Geographic Reach Metrics
                    var cityData = new Dictionary<string, List<Entities.Concrete.PlantAnalysis>>();
                    var districtSet = new HashSet<string>();

                    foreach (var analysis in analysesList)
                    {
                        var (city, district) = LocationParser.Parse(analysis.Location);
                        var normalizedCity = LocationParser.NormalizeCity(city);

                        if (!cityData.ContainsKey(normalizedCity))
                            cityData[normalizedCity] = new List<Entities.Concrete.PlantAnalysis>();

                        cityData[normalizedCity].Add(analysis);

                        if (!string.IsNullOrEmpty(district))
                            districtSet.Add(district);
                    }

                    var citiesReached = cityData.Count;
                    var districtsReached = districtSet.Count;

                    // Build Top 10 Cities
                    var topCities = cityData
                        .Select(kvp => new CityImpact
                        {
                            CityName = kvp.Key,
                            FarmerCount = kvp.Value.Where(a => a.UserId.HasValue).Select(a => a.UserId.Value).Distinct().Count(),
                            AnalysisCount = kvp.Value.Count,
                            Percentage = totalCropsAnalyzed > 0 
                                ? Math.Round((double)kvp.Value.Count / totalCropsAnalyzed * 100, 2) 
                                : 0,
                            MostCommonCrop = kvp.Value
                                .Where(a => !string.IsNullOrEmpty(a.CropType))
                                .GroupBy(a => a.CropType)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault()?.Key ?? "Unknown",
                            MostCommonDisease = kvp.Value
                                .Where(a => !string.IsNullOrEmpty(a.PrimaryIssue))
                                .GroupBy(a => a.PrimaryIssue)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault()?.Key ?? "Unknown"
                        })
                        .OrderByDescending(c => c.AnalysisCount)
                        .Take(10)
                        .ToList();

                    // Calculate Severity Distribution
                    var lowCount = analysesList.Count(a => a.HealthSeverity == "Low" || a.HealthSeverity == "Düşük");
                    var moderateCount = analysesList.Count(a => a.HealthSeverity == "Moderate" || a.HealthSeverity == "Orta");
                    var highCount = analysesList.Count(a => a.HealthSeverity == "High" || a.HealthSeverity == "Yüksek");
                    var criticalCount = analysesList.Count(a => a.HealthSeverity == "Critical" || a.HealthSeverity == "Kritik");

                    var severityDistribution = new SeverityStats
                    {
                        LowSeverityCount = lowCount,
                        ModerateSeverityCount = moderateCount,
                        HighSeverityCount = highCount,
                        CriticalSeverityCount = criticalCount,
                        LowPercentage = totalCropsAnalyzed > 0 ? Math.Round((double)lowCount / totalCropsAnalyzed * 100, 2) : 0,
                        ModeratePercentage = totalCropsAnalyzed > 0 ? Math.Round((double)moderateCount / totalCropsAnalyzed * 100, 2) : 0,
                        HighPercentage = totalCropsAnalyzed > 0 ? Math.Round((double)highCount / totalCropsAnalyzed * 100, 2) : 0,
                        CriticalPercentage = totalCropsAnalyzed > 0 ? Math.Round((double)criticalCount / totalCropsAnalyzed * 100, 2) : 0
                    };

                    // Calculate Top 10 Crops
                    var topCrops = analysesList
                        .Where(a => !string.IsNullOrEmpty(a.CropType))
                        .GroupBy(a => a.CropType)
                        .Select(g => new CropStat
                        {
                            CropType = g.Key,
                            AnalysisCount = g.Count(),
                            Percentage = totalCropsAnalyzed > 0 
                                ? Math.Round((double)g.Count() / totalCropsAnalyzed * 100, 2) 
                                : 0,
                            UniqueFarmers = g.Where(a => a.UserId.HasValue).Select(a => a.UserId.Value).Distinct().Count(),
                            AverageHealthScore = null // Not available in current data model
                        })
                        .OrderByDescending(c => c.AnalysisCount)
                        .Take(10)
                        .ToList();

                    // Calculate Top 10 Diseases
                    var topDiseases = analysesList
                        .Where(a => !string.IsNullOrEmpty(a.PrimaryIssue) && a.PrimaryIssue != "Unknown")
                        .GroupBy(a => a.PrimaryIssue)
                        .Select(g => new DiseaseStat
                        {
                            DiseaseName = g.Key,
                            Category = g.FirstOrDefault()?.HealthSeverity ?? "Unknown",
                            OccurrenceCount = g.Count(),
                            Percentage = diseasesDetected > 0 
                                ? Math.Round((double)g.Count() / diseasesDetected * 100, 2) 
                                : 0,
                            AffectedCrops = g.Where(a => !string.IsNullOrEmpty(a.CropType))
                                .Select(a => a.CropType)
                                .Distinct()
                                .ToList(),
                            MostCommonSeverity = g.GroupBy(a => a.HealthSeverity)
                                .OrderByDescending(sg => sg.Count())
                                .FirstOrDefault()?.Key ?? "Unknown",
                            TopCities = g.Select(a => LocationParser.ParseCity(a.Location))
                                .Where(c => c != "Unknown")
                                .GroupBy(c => c)
                                .OrderByDescending(cg => cg.Count())
                                .Take(3)
                                .Select(cg => cg.Key)
                                .ToList()
                        })
                        .OrderByDescending(d => d.OccurrenceCount)
                        .Take(10)
                        .ToList();

                    // Build final DTO
                    var analyticsDto = new SponsorImpactAnalyticsDto
                    {
                        // Farmer Impact
                        TotalFarmersReached = totalFarmersReached,
                        ActiveFarmersLast30Days = activeFarmersLast30Days,
                        FarmerRetentionRate = Math.Round(farmerRetentionRate, 2),
                        AverageFarmerLifetimeDays = Math.Round(averageFarmerLifetimeDays, 1),

                        // Agricultural Impact
                        TotalCropsAnalyzed = totalCropsAnalyzed,
                        UniqueCropTypes = uniqueCropTypes,
                        DiseasesDetected = diseasesDetected,
                        CriticalIssuesResolved = criticalIssuesResolved,

                        // Geographic Reach
                        CitiesReached = citiesReached,
                        DistrictsReached = districtsReached,
                        TopCities = topCities,

                        // Severity & Distribution
                        SeverityDistribution = severityDistribution,
                        TopCrops = topCrops,
                        TopDiseases = topDiseases,

                        // Data Range
                        DataStartDate = analysesList.Min(a => a.AnalysisDate),
                        DataEndDate = analysesList.Max(a => a.AnalysisDate)
                    };

                    // Cache for 6 hours
                    _cacheManager.Add(cacheKey, analyticsDto, CacheDurationMinutes);
                    _logger.LogInformation(
                        "[ImpactAnalytics] Cached data for sponsor {SponsorId} (TTL: 6h)", 
                        request.SponsorId);

                    return new SuccessDataResult<SponsorImpactAnalyticsDto>(
                        analyticsDto,
                        "Impact analytics retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "[ImpactAnalytics] Error fetching analytics for sponsor {SponsorId}", 
                        request.SponsorId);
                    return new ErrorDataResult<SponsorImpactAnalyticsDto>(
                        $"Error retrieving impact analytics: {ex.Message}");
                }
            }
        }
    }
}
