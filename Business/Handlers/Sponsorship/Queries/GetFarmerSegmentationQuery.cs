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
    /// Get farmer segmentation analytics for sponsor
    /// Segments farmers into Heavy Users, Regular Users, At-Risk, and Dormant
    /// Cache TTL: 6 hours for relatively stable segmentation data
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetFarmerSegmentationQuery : IRequest<IDataResult<FarmerSegmentationDto>>
    {
        /// <summary>
        /// Sponsor ID (null for admin view of all farmers)
        /// </summary>
        public int? SponsorId { get; set; }

        public class GetFarmerSegmentationQueryHandler : IRequestHandler<GetFarmerSegmentationQuery, IDataResult<FarmerSegmentationDto>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IUserRepository _userRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetFarmerSegmentationQueryHandler> _logger;

            private const string CacheKeyPrefix = "FarmerSegmentation";
            private const int CacheDurationMinutes = 360; // 6 hours as per spec

            public GetFarmerSegmentationQueryHandler(
                IPlantAnalysisRepository analysisRepository,
                IUserSubscriptionRepository subscriptionRepository,
                IUserRepository userRepository,
                ISponsorshipCodeRepository codeRepository,
                ICacheManager cacheManager,
                ILogger<GetFarmerSegmentationQueryHandler> logger)
            {
                _analysisRepository = analysisRepository;
                _subscriptionRepository = subscriptionRepository;
                _userRepository = userRepository;
                _codeRepository = codeRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            /// <summary>
            /// Generate cache key for sponsor (or "all" for admin view)
            /// </summary>
            private string GetCacheKey(int? sponsorId) => $"{CacheKeyPrefix}:{sponsorId?.ToString() ?? "all"}";

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<FarmerSegmentationDto>> Handle(
                GetFarmerSegmentationQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Check cache first
                    var cacheKey = GetCacheKey(request.SponsorId);
                    var cachedData = _cacheManager.Get<FarmerSegmentationDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[FarmerSegmentation] Cache HIT for sponsor {SponsorId}", request.SponsorId ?? 0);
                        return new SuccessDataResult<FarmerSegmentationDto>(
                            cachedData,
                            "Farmer segmentation retrieved from cache");
                    }

                    _logger.LogInformation("[FarmerSegmentation] Cache MISS for sponsor {SponsorId} - computing segmentation", request.SponsorId ?? 0);

                    // Get all analyses (filtered by sponsor if specified)
                    var allAnalyses = request.SponsorId.HasValue
                        ? await _analysisRepository.GetListAsync(a => a.SponsorCompanyId == request.SponsorId.Value)
                        : await _analysisRepository.GetListAsync(a => true);

                    var analysesList = allAnalyses.ToList();

                    if (!analysesList.Any())
                    {
                        return new SuccessDataResult<FarmerSegmentationDto>(
                            new FarmerSegmentationDto
                            {
                                SponsorId = request.SponsorId,
                                TotalFarmers = 0,
                                Segments = new List<SegmentDto>()
                            },
                            "No analyses found for segmentation");
                    }

                    // Get unique farmers
                    var farmerIds = analysesList
                        .Where(a => a.UserId.HasValue)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .ToList();

                    var totalFarmers = farmerIds.Count;

                    if (totalFarmers == 0)
                    {
                        return new SuccessDataResult<FarmerSegmentationDto>(
                            new FarmerSegmentationDto
                            {
                                SponsorId = request.SponsorId,
                                TotalFarmers = 0,
                                Segments = new List<SegmentDto>()
                            },
                            "No farmers found for segmentation");
                    }

                    // Compute farmer metrics
                    var now = DateTime.Now;
                    var farmerMetrics = new List<FarmerMetric>();

                    foreach (var farmerId in farmerIds)
                    {
                        var farmerAnalyses = analysesList.Where(a => a.UserId == farmerId).OrderByDescending(a => a.AnalysisDate).ToList();

                        if (!farmerAnalyses.Any()) continue;

                        var lastAnalysisDate = farmerAnalyses.First().AnalysisDate;
                        var firstAnalysisDate = farmerAnalyses.Last().AnalysisDate;
                        var daysSinceLastAnalysis = (int)(now - lastAnalysisDate).TotalDays;

                        // Calculate analyses per month
                        var totalDays = Math.Max(1, (now - firstAnalysisDate).TotalDays);
                        var monthsActive = totalDays / 30.0;
                        var analysesPerMonth = farmerAnalyses.Count / Math.Max(0.5, monthsActive);

                        // Get subscription status
                        var subscription = await _subscriptionRepository.GetAsync(s => s.UserId == farmerId && s.IsActive);
                        var hasActiveSubscription = subscription != null && subscription.EndDate >= now;
                        var subscriptionExpired = subscription != null && subscription.EndDate < now;

                        farmerMetrics.Add(new FarmerMetric
                        {
                            FarmerId = farmerId,
                            AnalysesPerMonth = (decimal)analysesPerMonth,
                            DaysSinceLastAnalysis = daysSinceLastAnalysis,
                            TotalAnalyses = farmerAnalyses.Count,
                            HasActiveSubscription = hasActiveSubscription,
                            SubscriptionExpired = subscriptionExpired,
                            SubscriptionTier = subscription?.SubscriptionTier?.TierName,
                            MostCommonCrop = farmerAnalyses
                                .Where(a => !string.IsNullOrEmpty(a.CropType))
                                .GroupBy(a => a.CropType)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault()?.Key,
                            MostCommonDisease = farmerAnalyses
                                .Where(a => !string.IsNullOrEmpty(a.PrimaryIssue))
                                .GroupBy(a => a.PrimaryIssue)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault()?.Key
                        });
                    }

                    // Segment farmers based on behavior
                    var segments = new List<SegmentDto>();

                    // Heavy Users: avgAnalysesPerMonth >= 6 AND daysSinceLastAnalysis <= 7
                    var heavyUsers = farmerMetrics
                        .Where(m => m.AnalysesPerMonth >= 6 && m.DaysSinceLastAnalysis <= 7)
                        .ToList();

                    if (heavyUsers.Any())
                    {
                        segments.Add(BuildSegment("Heavy Users", heavyUsers, totalFarmers));
                    }

                    // Regular Users: avgAnalysesPerMonth >= 2 AND daysSinceLastAnalysis <= 30
                    var regularUsers = farmerMetrics
                        .Where(m => m.AnalysesPerMonth >= 2 && m.DaysSinceLastAnalysis <= 30 &&
                                   !(m.AnalysesPerMonth >= 6 && m.DaysSinceLastAnalysis <= 7))
                        .ToList();

                    if (regularUsers.Any())
                    {
                        segments.Add(BuildSegment("Regular Users", regularUsers, totalFarmers));
                    }

                    // At-Risk Users: avgAnalysesPerMonth >= 1 AND daysSinceLastAnalysis BETWEEN 31-60
                    var atRiskUsers = farmerMetrics
                        .Where(m => m.AnalysesPerMonth >= 1 && m.DaysSinceLastAnalysis > 30 && m.DaysSinceLastAnalysis <= 60)
                        .ToList();

                    if (atRiskUsers.Any())
                    {
                        segments.Add(BuildSegment("At-Risk Users", atRiskUsers, totalFarmers));
                    }

                    // Dormant Users: daysSinceLastAnalysis > 60 OR subscriptionExpired
                    var dormantUsers = farmerMetrics
                        .Where(m => m.DaysSinceLastAnalysis > 60 || m.SubscriptionExpired)
                        .ToList();

                    if (dormantUsers.Any())
                    {
                        segments.Add(BuildSegment("Dormant Users", dormantUsers, totalFarmers));
                    }

                    var result = new FarmerSegmentationDto
                    {
                        SponsorId = request.SponsorId,
                        TotalFarmers = totalFarmers,
                        Segments = segments,
                        GeneratedAt = now
                    };

                    // Cache for 6 hours
                    _cacheManager.Add(cacheKey, result, CacheDurationMinutes);
                    _logger.LogInformation(
                        "[FarmerSegmentation] Cached data for sponsor {SponsorId} (TTL: 6h)",
                        request.SponsorId ?? 0);

                    return new SuccessDataResult<FarmerSegmentationDto>(
                        result,
                        "Farmer segmentation computed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[FarmerSegmentation] Error computing segmentation for sponsor {SponsorId}",
                        request.SponsorId ?? 0);
                    return new ErrorDataResult<FarmerSegmentationDto>(
                        $"Error computing farmer segmentation: {ex.Message}");
                }
            }

            /// <summary>
            /// Build segment DTO from farmer metrics
            /// </summary>
            private SegmentDto BuildSegment(string segmentName, List<FarmerMetric> farmers, int totalFarmers)
            {
                var characteristics = new SegmentCharacteristics
                {
                    AvgAnalysesPerMonth = farmers.Average(f => f.AnalysesPerMonth),
                    AvgDaysSinceLastAnalysis = (int)farmers.Average(f => f.DaysSinceLastAnalysis),
                    MedianDaysSinceLastAnalysis = GetMedian(farmers.Select(f => f.DaysSinceLastAnalysis).ToList()),
                    MostCommonTier = farmers
                        .Where(f => !string.IsNullOrEmpty(f.SubscriptionTier))
                        .GroupBy(f => f.SubscriptionTier)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? "Trial",
                    ActiveSubscriptionRate = farmers.Count > 0
                        ? (decimal)farmers.Count(f => f.HasActiveSubscription) / farmers.Count * 100
                        : 0,
                    AvgEngagementScore = CalculateEngagementScore(farmers),
                    TopCrop = farmers
                        .Where(f => !string.IsNullOrEmpty(f.MostCommonCrop))
                        .GroupBy(f => f.MostCommonCrop)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? "Unknown",
                    TopDisease = farmers
                        .Where(f => !string.IsNullOrEmpty(f.MostCommonDisease))
                        .GroupBy(f => f.MostCommonDisease)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? "Unknown"
                };

                var avatar = BuildSegmentAvatar(segmentName, characteristics);
                var actions = GetRecommendedActions(segmentName);

                return new SegmentDto
                {
                    SegmentName = segmentName,
                    FarmerCount = farmers.Count,
                    Percentage = totalFarmers > 0 ? Math.Round((decimal)farmers.Count / totalFarmers * 100, 2) : 0,
                    Characteristics = characteristics,
                    FarmerAvatar = avatar,
                    FarmerIds = farmers.Select(f => f.FarmerId).ToList(),
                    RecommendedActions = actions
                };
            }

            /// <summary>
            /// Build typical farmer avatar for segment
            /// </summary>
            private SegmentAvatar BuildSegmentAvatar(string segmentName, SegmentCharacteristics chars)
            {
                return segmentName switch
                {
                    "Heavy Users" => new SegmentAvatar
                    {
                        Profile = $"Active farmer, analyzes {Math.Round(chars.AvgAnalysesPerMonth)} times/month, primarily grows {chars.TopCrop}",
                        BehaviorPattern = "Frequent analyses, typically within a week of last check-in. High engagement with platform.",
                        PainPoints = $"Faces recurring issues with {chars.TopDisease}. Needs proactive prevention strategies.",
                        EngagementStyle = "Reads all messages, responds quickly, actively uses recommendations."
                    },
                    "Regular Users" => new SegmentAvatar
                    {
                        Profile = $"Consistent farmer, analyzes {Math.Round(chars.AvgAnalysesPerMonth)} times/month, grows {chars.TopCrop}",
                        BehaviorPattern = "Steady analysis pattern, checks crops regularly during growing season.",
                        PainPoints = $"Encounters {chars.TopDisease} occasionally. May benefit from seasonal advice.",
                        EngagementStyle = "Reads most messages, clicks product links, sometimes asks follow-up questions."
                    },
                    "At-Risk Users" => new SegmentAvatar
                    {
                        Profile = $"Declining engagement, {chars.AvgDaysSinceLastAnalysis} days since last analysis, grows {chars.TopCrop}",
                        BehaviorPattern = "Usage dropping off, may be experiencing issues or found alternative solutions.",
                        PainPoints = "May feel platform isn't providing enough value or facing usability challenges.",
                        EngagementStyle = "Rarely opens messages, low click-through rate, minimal interaction."
                    },
                    "Dormant Users" => new SegmentAvatar
                    {
                        Profile = $"Inactive farmer, {chars.AvgDaysSinceLastAnalysis} days since last analysis. Previous crop: {chars.TopCrop}",
                        BehaviorPattern = "No recent activity. May have abandoned platform or facing technical issues.",
                        PainPoints = "Lost interest, found alternative solution, or subscription expired without renewal.",
                        EngagementStyle = "No engagement with messages or content. Likely unaware of new features."
                    },
                    _ => new SegmentAvatar
                    {
                        Profile = "Farmer profile unavailable",
                        BehaviorPattern = "Unknown behavior pattern",
                        PainPoints = "Unknown pain points",
                        EngagementStyle = "Unknown engagement style"
                    }
                };
            }

            /// <summary>
            /// Get recommended actions for segment
            /// </summary>
            private List<string> GetRecommendedActions(string segmentName)
            {
                return segmentName switch
                {
                    "Heavy Users" => new List<string>
                    {
                        "Reward loyalty with exclusive tips or priority support",
                        "Offer premium subscription upgrade with advanced features",
                        "Request testimonials and case studies",
                        "Invite to beta test new features"
                    },
                    "Regular Users" => new List<string>
                    {
                        "Send seasonal farming tips and best practices",
                        "Promote relevant products based on crop/disease patterns",
                        "Encourage sharing platform with other farmers",
                        "Offer tier upgrade incentives"
                    },
                    "At-Risk Users" => new List<string>
                    {
                        "Send re-engagement message with value proposition",
                        "Offer limited-time discount on subscription renewal",
                        "Provide personalized tips based on their crop history",
                        "Survey to understand barriers to continued use"
                    },
                    "Dormant Users" => new List<string>
                    {
                        "Win-back campaign with special offer",
                        "Highlight new features and improvements since last use",
                        "Survey to understand reasons for churn",
                        "SMS reminder about platform benefits"
                    },
                    _ => new List<string> { "No specific actions recommended" }
                };
            }

            /// <summary>
            /// Calculate engagement score (0-100) based on behavior metrics
            /// </summary>
            private decimal CalculateEngagementScore(List<FarmerMetric> farmers)
            {
                if (!farmers.Any()) return 0;

                var avgScore = farmers.Average(f =>
                {
                    decimal score = 0;

                    // Frequency score (40 points max)
                    score += Math.Min(40, (decimal)f.AnalysesPerMonth * 4);

                    // Recency score (30 points max)
                    if (f.DaysSinceLastAnalysis <= 7) score += 30;
                    else if (f.DaysSinceLastAnalysis <= 14) score += 25;
                    else if (f.DaysSinceLastAnalysis <= 30) score += 15;
                    else if (f.DaysSinceLastAnalysis <= 60) score += 5;

                    // Subscription score (30 points max)
                    if (f.HasActiveSubscription) score += 30;
                    else if (!f.SubscriptionExpired) score += 15;

                    return Math.Min(100, score);
                });

                return Math.Round(avgScore, 1);
            }

            /// <summary>
            /// Get median value from list
            /// </summary>
            private int GetMedian(List<int> values)
            {
                if (!values.Any()) return 0;

                var sorted = values.OrderBy(v => v).ToList();
                var mid = sorted.Count / 2;

                return sorted.Count % 2 == 0
                    ? (sorted[mid - 1] + sorted[mid]) / 2
                    : sorted[mid];
            }
        }

        /// <summary>
        /// Internal class for tracking farmer metrics
        /// </summary>
        private class FarmerMetric
        {
            public int FarmerId { get; set; }
            public decimal AnalysesPerMonth { get; set; }
            public int DaysSinceLastAnalysis { get; set; }
            public int TotalAnalyses { get; set; }
            public bool HasActiveSubscription { get; set; }
            public bool SubscriptionExpired { get; set; }
            public string SubscriptionTier { get; set; }
            public string MostCommonCrop { get; set; }
            public string MostCommonDisease { get; set; }
        }
    }
}
