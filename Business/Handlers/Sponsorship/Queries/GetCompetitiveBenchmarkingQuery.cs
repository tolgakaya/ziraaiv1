using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get competitive benchmarking analytics comparing sponsor performance with industry standards
    /// Provides percentile rankings, gap analysis, and anonymized competitor comparisons
    /// Cache TTL: 24 hours for relatively stable benchmark data
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetCompetitiveBenchmarkingQuery : IRequest<IDataResult<CompetitiveBenchmarkingDto>>
    {
        /// <summary>
        /// Sponsor ID for benchmarking (null for admin to see all sponsors aggregated)
        /// </summary>
        public int? SponsorId { get; set; }

        /// <summary>
        /// Time period in days for benchmark calculation (default: 90 days)
        /// </summary>
        public int TimePeriodDays { get; set; } = 90;

        public class GetCompetitiveBenchmarkingQueryHandler : IRequestHandler<GetCompetitiveBenchmarkingQuery, IDataResult<CompetitiveBenchmarkingDto>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserRepository _userRepository;
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetCompetitiveBenchmarkingQueryHandler> _logger;

            private const string CacheKeyPrefix = "CompetitiveBenchmarking";
            private const int CacheDurationMinutes = 1440; // 24 hours

            public GetCompetitiveBenchmarkingQueryHandler(
                IPlantAnalysisRepository analysisRepository,
                IUserRepository userRepository,
                IAnalysisMessageRepository messageRepository,
                ICacheManager cacheManager,
                ILogger<GetCompetitiveBenchmarkingQueryHandler> logger)
            {
                _analysisRepository = analysisRepository;
                _userRepository = userRepository;
                _messageRepository = messageRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            /// <summary>
            /// Generate cache key for sponsor
            /// </summary>
            private string GetCacheKey(int? sponsorId, int timePeriodDays)
            {
                var sponsorKey = sponsorId.HasValue ? sponsorId.Value.ToString() : "all";
                return $"{CacheKeyPrefix}:{sponsorKey}:{timePeriodDays}";
            }

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<CompetitiveBenchmarkingDto>> Handle(
                GetCompetitiveBenchmarkingQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Check cache first
                    var cacheKey = GetCacheKey(request.SponsorId, request.TimePeriodDays);
                    var cachedData = _cacheManager.Get<CompetitiveBenchmarkingDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[CompetitiveBenchmarking] Cache HIT for sponsor {SponsorId}",
                            request.SponsorId?.ToString() ?? "All");
                        return new SuccessDataResult<CompetitiveBenchmarkingDto>(
                            cachedData,
                            "Competitive benchmarking retrieved from cache");
                    }

                    _logger.LogInformation("[CompetitiveBenchmarking] Cache MISS for sponsor {SponsorId} - computing benchmarks",
                        request.SponsorId?.ToString() ?? "All");

                    var cutoffDate = DateTime.Now.AddDays(-request.TimePeriodDays);

                    // Get all sponsored analyses in the time period
                    var allAnalyses = await _analysisRepository.GetListAsync(
                        a => a.SponsorCompanyId.HasValue && a.CreatedDate >= cutoffDate);

                    var analysesList = allAnalyses.ToList();

                    if (!analysesList.Any())
                    {
                        return new ErrorDataResult<CompetitiveBenchmarkingDto>(
                            "No sponsored analyses found for benchmarking period");
                    }

                    // Group analyses by sponsor
                    var analysesBySponsor = analysesList
                        .GroupBy(a => a.SponsorCompanyId.Value)
                        .ToList();

                    var totalSponsorsInBenchmark = analysesBySponsor.Count;

                    if (totalSponsorsInBenchmark < 1) // TODO: Production'da 3 olacak
                    {
                        return new ErrorDataResult<CompetitiveBenchmarkingDto>(
                            "Insufficient sponsors for benchmarking (minimum 3 required for anonymization)");
                    }

                    // Calculate performance metrics for all sponsors
                    var allSponsorMetrics = new List<SponsorPerformanceMetrics>();

                    foreach (var sponsorGroup in analysesBySponsor)
                    {
                        var sponsorId = sponsorGroup.Key;
                        var sponsorAnalyses = sponsorGroup.ToList();

                        var metrics = await CalculateSponsorMetrics(sponsorId, sponsorAnalyses, cutoffDate);
                        allSponsorMetrics.Add(metrics);
                    }

                    // Calculate industry benchmarks
                    var industryBenchmarks = CalculateIndustryBenchmarks(allSponsorMetrics);

                    // If specific sponsor requested, calculate their performance and gaps
                    SponsorPerformanceDto yourPerformance = null;
                    PercentileRankingDto ranking = null;
                    List<GapAnalysisDto> gaps = null;

                    if (request.SponsorId.HasValue)
                    {
                        var yourMetrics = allSponsorMetrics.FirstOrDefault(m => m.SponsorId == request.SponsorId.Value);

                        if (yourMetrics == null)
                        {
                            return new ErrorDataResult<CompetitiveBenchmarkingDto>(
                                $"No data found for sponsor {request.SponsorId.Value} in the specified period");
                        }

                        yourPerformance = MapToPerformanceDto(yourMetrics);
                        ranking = CalculatePercentileRanking(yourMetrics, allSponsorMetrics);
                        gaps = CalculateGapAnalysis(yourMetrics, industryBenchmarks);
                    }

                    var result = new CompetitiveBenchmarkingDto
                    {
                        YourPerformance = yourPerformance,
                        IndustryBenchmarks = industryBenchmarks,
                        Gaps = gaps,
                        Ranking = ranking,
                        SponsorId = request.SponsorId,
                        TotalSponsorsInBenchmark = totalSponsorsInBenchmark,
                        GeneratedAt = DateTime.Now,
                        TimePeriod = GetTimePeriodDescription(request.TimePeriodDays)
                    };

                    // Cache the result
                    _cacheManager.Add(cacheKey, result, CacheDurationMinutes);

                    _logger.LogInformation("[CompetitiveBenchmarking] Benchmarks computed and cached for sponsor {SponsorId}",
                        request.SponsorId?.ToString() ?? "All");

                    return new SuccessDataResult<CompetitiveBenchmarkingDto>(
                        result,
                        "Competitive benchmarking computed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CompetitiveBenchmarking] Error computing benchmarks for sponsor {SponsorId}",
                        request.SponsorId?.ToString() ?? "All");
                    return new ErrorDataResult<CompetitiveBenchmarkingDto>(
                        $"Failed to compute competitive benchmarking: {ex.Message}");
                }
            }

            /// <summary>
            /// Calculate performance metrics for a specific sponsor
            /// </summary>
            private async Task<SponsorPerformanceMetrics> CalculateSponsorMetrics(
                int sponsorId,
                List<PlantAnalysis> sponsorAnalyses,
                DateTime cutoffDate)
            {
                // Get unique farmers
                var uniqueFarmerIds = sponsorAnalyses
                    .Where(a => a.UserId.HasValue)
                    .Select(a => a.UserId.Value)
                    .Distinct()
                    .ToList();

                var totalFarmers = uniqueFarmerIds.Count;
                var totalAnalyses = sponsorAnalyses.Count;

                // Calculate analyses per farmer
                var avgAnalysesPerFarmer = totalFarmers > 0
                    ? (decimal)totalAnalyses / totalFarmers
                    : 0;

                // Calculate average farmers per month
                var daysSinceStart = (DateTime.Now - cutoffDate).Days;
                var monthsInPeriod = daysSinceStart / 30.0;
                var avgFarmersPerMonth = monthsInPeriod > 0
                    ? (decimal)(totalFarmers / monthsInPeriod)
                    : totalFarmers;

                // Get messages sent by this sponsor (via PlantAnalysisIds)
                var analysisIds = sponsorAnalyses.Select(a => a.Id).ToList();
                var messages = await _messageRepository.GetListAsync(
                    m => analysisIds.Contains(m.PlantAnalysisId) && m.CreatedDate >= cutoffDate);

                var messagesList = messages.ToList();
                var totalMessagesSent = messagesList.Count;

                // Calculate message response rate (messages with farmer response)
                var messagesWithResponse = messagesList.Count(m => m.IsRead == true);
                var messageResponseRate = totalMessagesSent > 0
                    ? (decimal)messagesWithResponse / totalMessagesSent * 100
                    : 0;

                // Calculate farmer retention rate (farmers active in both halves of period)
                var midpoint = cutoffDate.AddDays(daysSinceStart / 2.0);
                var farmersFirstHalf = sponsorAnalyses
                    .Where(a => a.CreatedDate < midpoint && a.UserId.HasValue)
                    .Select(a => a.UserId.Value)
                    .Distinct()
                    .ToList();

                var farmersSecondHalf = sponsorAnalyses
                    .Where(a => a.CreatedDate >= midpoint && a.UserId.HasValue)
                    .Select(a => a.UserId.Value)
                    .Distinct()
                    .ToList();

                var retainedFarmers = farmersFirstHalf.Intersect(farmersSecondHalf).Count();
                var farmerRetentionRate = farmersFirstHalf.Any()
                    ? (decimal)retainedFarmers / farmersFirstHalf.Count * 100
                    : 0;

                // Calculate engagement score (simplified - based on activity patterns)
                var avgEngagementScore = CalculateEngagementScore(
                    avgAnalysesPerFarmer,
                    messageResponseRate,
                    farmerRetentionRate);

                return new SponsorPerformanceMetrics
                {
                    SponsorId = sponsorId,
                    AvgFarmersPerMonth = Math.Round(avgFarmersPerMonth, 1),
                    AvgAnalysesPerFarmer = Math.Round(avgAnalysesPerFarmer, 1),
                    MessageResponseRate = Math.Round(messageResponseRate, 1),
                    FarmerRetentionRate = Math.Round(farmerRetentionRate, 1),
                    AvgEngagementScore = Math.Round(avgEngagementScore, 1),
                    TotalFarmers = totalFarmers,
                    TotalAnalyses = totalAnalyses,
                    TotalMessagesSent = totalMessagesSent
                };
            }

            /// <summary>
            /// Calculate engagement score (0-100) based on multiple factors
            /// </summary>
            private decimal CalculateEngagementScore(
                decimal analysesPerFarmer,
                decimal messageResponseRate,
                decimal retentionRate)
            {
                // Weighted scoring: Activity (40%), Response (30%), Retention (30%)
                var activityScore = Math.Min(40, analysesPerFarmer * 10); // Max 40 points
                var responseScore = messageResponseRate * 0.3m; // Max 30 points
                var retentionScore = retentionRate * 0.3m; // Max 30 points

                return Math.Min(100, activityScore + responseScore + retentionScore);
            }

            /// <summary>
            /// Calculate industry-wide benchmarks from all sponsor metrics
            /// </summary>
            private IndustryBenchmarksDto CalculateIndustryBenchmarks(
                List<SponsorPerformanceMetrics> allMetrics)
            {
                return new IndustryBenchmarksDto
                {
                    IndustryAvgFarmers = Math.Round(allMetrics.Average(m => m.AvgFarmersPerMonth), 1),
                    IndustryAvgAnalyses = Math.Round(allMetrics.Average(m => m.AvgAnalysesPerFarmer), 1),
                    IndustryAvgResponseRate = Math.Round(allMetrics.Average(m => m.MessageResponseRate), 1),
                    IndustryAvgRetentionRate = Math.Round(allMetrics.Average(m => m.FarmerRetentionRate), 1),
                    IndustryAvgEngagementScore = Math.Round(allMetrics.Average(m => m.AvgEngagementScore), 1),

                    // 90th percentile (top performers)
                    TopPerformerFarmers = Math.Round(GetPercentile(allMetrics.Select(m => m.AvgFarmersPerMonth).ToList(), 90), 1),
                    TopPerformerAnalyses = Math.Round(GetPercentile(allMetrics.Select(m => m.AvgAnalysesPerFarmer).ToList(), 90), 1),
                    TopPerformerResponseRate = Math.Round(GetPercentile(allMetrics.Select(m => m.MessageResponseRate).ToList(), 90), 1),
                    TopPerformerRetentionRate = Math.Round(GetPercentile(allMetrics.Select(m => m.FarmerRetentionRate).ToList(), 90), 1),
                    TopPerformerEngagementScore = Math.Round(GetPercentile(allMetrics.Select(m => m.AvgEngagementScore).ToList(), 90), 1)
                };
            }

            /// <summary>
            /// Calculate percentile value from a list of values
            /// </summary>
            private decimal GetPercentile(List<decimal> values, int percentile)
            {
                if (!values.Any()) return 0;

                var sortedValues = values.OrderBy(v => v).ToList();
                var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
                index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));

                return sortedValues[index];
            }

            /// <summary>
            /// Calculate percentile ranking for a specific sponsor
            /// </summary>
            private PercentileRankingDto CalculatePercentileRanking(
                SponsorPerformanceMetrics yourMetrics,
                List<SponsorPerformanceMetrics> allMetrics)
            {
                var farmersPercentile = CalculatePercentile(yourMetrics.AvgFarmersPerMonth,
                    allMetrics.Select(m => m.AvgFarmersPerMonth).ToList());
                var analysesPercentile = CalculatePercentile(yourMetrics.AvgAnalysesPerFarmer,
                    allMetrics.Select(m => m.AvgAnalysesPerFarmer).ToList());
                var responseRatePercentile = CalculatePercentile(yourMetrics.MessageResponseRate,
                    allMetrics.Select(m => m.MessageResponseRate).ToList());
                var retentionRatePercentile = CalculatePercentile(yourMetrics.FarmerRetentionRate,
                    allMetrics.Select(m => m.FarmerRetentionRate).ToList());
                var engagementScorePercentile = CalculatePercentile(yourMetrics.AvgEngagementScore,
                    allMetrics.Select(m => m.AvgEngagementScore).ToList());

                // Overall percentile (average of all metrics)
                var overallPercentile = (int)Math.Round(
                    (farmersPercentile + analysesPercentile + responseRatePercentile +
                     retentionRatePercentile + engagementScorePercentile) / 5.0);

                var rankingDescription = GetRankingDescription(overallPercentile);

                return new PercentileRankingDto
                {
                    OverallPercentile = overallPercentile,
                    FarmersPercentile = farmersPercentile,
                    AnalysesPercentile = analysesPercentile,
                    ResponseRatePercentile = responseRatePercentile,
                    RetentionRatePercentile = retentionRatePercentile,
                    EngagementScorePercentile = engagementScorePercentile,
                    RankingDescription = rankingDescription
                };
            }

            /// <summary>
            /// Calculate what percentile a value falls into
            /// </summary>
            private int CalculatePercentile(decimal value, List<decimal> allValues)
            {
                if (!allValues.Any()) return 50;

                var countBelow = allValues.Count(v => v < value);
                var percentile = (int)Math.Round((double)countBelow / allValues.Count * 100);

                return Math.Max(0, Math.Min(100, percentile));
            }

            /// <summary>
            /// Get human-readable ranking description
            /// </summary>
            private string GetRankingDescription(int percentile)
            {
                if (percentile >= 90) return "Top 10%";
                if (percentile >= 75) return "Top 25%";
                if (percentile >= 50) return "Above Average";
                if (percentile >= 25) return "Average";
                return "Below Average";
            }

            /// <summary>
            /// Calculate gap analysis comparing sponsor to industry benchmarks
            /// </summary>
            private List<GapAnalysisDto> CalculateGapAnalysis(
                SponsorPerformanceMetrics yourMetrics,
                IndustryBenchmarksDto benchmarks)
            {
                var gaps = new List<GapAnalysisDto>();

                // Farmer Count Gap
                gaps.Add(CreateGapAnalysis(
                    "Farmer Count",
                    yourMetrics.AvgFarmersPerMonth,
                    benchmarks.IndustryAvgFarmers,
                    benchmarks.TopPerformerFarmers,
                    "Increase marketing efforts and farmer acquisition campaigns"));

                // Analyses Per Farmer Gap
                gaps.Add(CreateGapAnalysis(
                    "Analyses Per Farmer",
                    yourMetrics.AvgAnalysesPerFarmer,
                    benchmarks.IndustryAvgAnalyses,
                    benchmarks.TopPerformerAnalyses,
                    "Encourage more frequent platform usage through targeted messaging"));

                // Message Response Rate Gap
                gaps.Add(CreateGapAnalysis(
                    "Message Response Rate",
                    yourMetrics.MessageResponseRate,
                    benchmarks.IndustryAvgResponseRate,
                    benchmarks.TopPerformerResponseRate,
                    "Improve message relevance and personalization to increase engagement"));

                // Farmer Retention Rate Gap
                gaps.Add(CreateGapAnalysis(
                    "Farmer Retention Rate",
                    yourMetrics.FarmerRetentionRate,
                    benchmarks.IndustryAvgRetentionRate,
                    benchmarks.TopPerformerRetentionRate,
                    "Focus on at-risk farmers with targeted re-engagement campaigns"));

                // Engagement Score Gap
                gaps.Add(CreateGapAnalysis(
                    "Engagement Score",
                    yourMetrics.AvgEngagementScore,
                    benchmarks.IndustryAvgEngagementScore,
                    benchmarks.TopPerformerEngagementScore,
                    "Implement multi-channel engagement strategy (messages, tips, offers)"));

                return gaps;
            }

            /// <summary>
            /// Create gap analysis for a single metric
            /// </summary>
            private GapAnalysisDto CreateGapAnalysis(
                string metricName,
                decimal yourValue,
                decimal industryAvg,
                decimal topPerformer,
                string recommendation)
            {
                var gapVsIndustry = industryAvg > 0
                    ? ((yourValue - industryAvg) / industryAvg * 100)
                    : 0;

                var gapVsTopPerformer = topPerformer > 0
                    ? ((yourValue - topPerformer) / topPerformer * 100)
                    : 0;

                var status = gapVsIndustry >= 10 ? "Above Average" :
                             gapVsIndustry >= -10 ? "Average" :
                             "Below Average";

                return new GapAnalysisDto
                {
                    MetricName = metricName,
                    YourValue = Math.Round(yourValue, 1),
                    IndustryAvg = Math.Round(industryAvg, 1),
                    TopPerformer = Math.Round(topPerformer, 1),
                    GapVsIndustry = FormatGap(gapVsIndustry),
                    GapVsTopPerformer = FormatGap(gapVsTopPerformer),
                    Status = status,
                    Recommendation = recommendation
                };
            }

            /// <summary>
            /// Format gap percentage as string (e.g., "+33.7%", "-12.5%")
            /// </summary>
            private string FormatGap(decimal gap)
            {
                var sign = gap >= 0 ? "+" : "";
                return $"{sign}{Math.Round(gap, 1)}%";
            }

            /// <summary>
            /// Map internal metrics to DTO
            /// </summary>
            private SponsorPerformanceDto MapToPerformanceDto(SponsorPerformanceMetrics metrics)
            {
                return new SponsorPerformanceDto
                {
                    AvgFarmersPerMonth = metrics.AvgFarmersPerMonth,
                    AvgAnalysesPerFarmer = metrics.AvgAnalysesPerFarmer,
                    MessageResponseRate = metrics.MessageResponseRate,
                    FarmerRetentionRate = metrics.FarmerRetentionRate,
                    AvgEngagementScore = metrics.AvgEngagementScore,
                    TotalFarmers = metrics.TotalFarmers,
                    TotalAnalyses = metrics.TotalAnalyses,
                    TotalMessagesSent = metrics.TotalMessagesSent
                };
            }

            /// <summary>
            /// Get human-readable time period description
            /// </summary>
            private string GetTimePeriodDescription(int days)
            {
                if (days >= 365) return $"Last {days / 365} year(s)";
                if (days >= 30) return $"Last {days / 30} month(s)";
                return $"Last {days} day(s)";
            }
        }

        /// <summary>
        /// Internal class for holding sponsor performance metrics during calculation
        /// </summary>
        private class SponsorPerformanceMetrics
        {
            public int SponsorId { get; set; }
            public decimal AvgFarmersPerMonth { get; set; }
            public decimal AvgAnalysesPerFarmer { get; set; }
            public decimal MessageResponseRate { get; set; }
            public decimal FarmerRetentionRate { get; set; }
            public decimal AvgEngagementScore { get; set; }
            public int TotalFarmers { get; set; }
            public int TotalAnalyses { get; set; }
            public int TotalMessagesSent { get; set; }
        }
    }
}
