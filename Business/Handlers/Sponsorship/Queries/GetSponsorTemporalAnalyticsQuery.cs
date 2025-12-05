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
    /// Get temporal analytics for sponsor showing trends over time
    /// Supports different grouping periods: Day, Week, Month
    /// Cache TTL: 1 hour for relatively fresh temporal data
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetSponsorTemporalAnalyticsQuery : IRequest<IDataResult<SponsorTemporalAnalyticsDto>>
    {
        public int SponsorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string GroupBy { get; set; } = "Day"; // Day, Week, Month

        public class GetSponsorTemporalAnalyticsQueryHandler : IRequestHandler<GetSponsorTemporalAnalyticsQuery, IDataResult<SponsorTemporalAnalyticsDto>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetSponsorTemporalAnalyticsQueryHandler> _logger;

            private const string CacheKeyPrefix = "SponsorTemporalAnalytics";
            private const int CacheDurationMinutes = 60; // 1 hour as per spec

            public GetSponsorTemporalAnalyticsQueryHandler(
                IPlantAnalysisRepository analysisRepository,
                ISponsorshipCodeRepository codeRepository,
                IAnalysisMessageRepository messageRepository,
                ICacheManager cacheManager,
                ILogger<GetSponsorTemporalAnalyticsQueryHandler> logger)
            {
                _analysisRepository = analysisRepository;
                _codeRepository = codeRepository;
                _messageRepository = messageRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            /// <summary>
            /// Generate cache key including sponsor ID, date range, and grouping
            /// </summary>
            private string GetCacheKey(int sponsorId, DateTime? startDate, DateTime? endDate, string groupBy)
            {
                var dateKey = startDate.HasValue && endDate.HasValue
                    ? $":{startDate.Value:yyyyMMdd}-{endDate.Value:yyyyMMdd}"
                    : ":all";
                return $"{CacheKeyPrefix}:{sponsorId}{dateKey}:{groupBy}";
            }

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<SponsorTemporalAnalyticsDto>> Handle(
                GetSponsorTemporalAnalyticsQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Validate GroupBy parameter
                    if (!new[] { "Day", "Week", "Month" }.Contains(request.GroupBy))
                    {
                        return new ErrorDataResult<SponsorTemporalAnalyticsDto>(
                            "Invalid GroupBy parameter. Use 'Day', 'Week', or 'Month'");
                    }

                    // Check cache first
                    var cacheKey = GetCacheKey(request.SponsorId, request.StartDate, request.EndDate, request.GroupBy);
                    var cachedData = _cacheManager.Get<SponsorTemporalAnalyticsDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[TemporalAnalytics] Cache HIT for sponsor {SponsorId}", request.SponsorId);
                        return new SuccessDataResult<SponsorTemporalAnalyticsDto>(
                            cachedData,
                            "Temporal analytics retrieved from cache");
                    }

                    _logger.LogInformation("[TemporalAnalytics] Cache MISS for sponsor {SponsorId} - fetching from database", request.SponsorId);

                    // Set default date range if not provided (last 30 days)
                    var endDate = request.EndDate ?? DateTime.Now;
                    var startDate = request.StartDate ?? endDate.AddDays(-30);

                    // ⚡ OPTIMIZED: Filter in DATABASE, not in-memory
                    // Get codes filtered by sponsor AND date range (SQL filtering)
                    var codesInRange = (await _codeRepository.GetListAsync(c =>
                        c.SponsorId == request.SponsorId &&
                        ((c.DistributionDate.HasValue && c.DistributionDate.Value >= startDate && c.DistributionDate.Value <= endDate) ||
                         (c.UsedDate.HasValue && c.UsedDate.Value >= startDate && c.UsedDate.Value <= endDate))))
                        .ToList();

                    // ⚡ OPTIMIZED: Filter in DATABASE, not in-memory
                    // Get analyses filtered by sponsor AND date range (SQL filtering)
                    var analysesInRange = (await _analysisRepository.GetListAsync(
                        a => a.SponsorCompanyId.HasValue &&
                             a.SponsorCompanyId.Value == request.SponsorId &&
                             a.AnalysisDate >= startDate &&
                             a.AnalysisDate <= endDate))
                        .ToList();

                    // Get messages
                    var analysisIds = analysesInRange.Select(a => a.Id).ToList();
                    var allMessages = new List<Entities.Concrete.AnalysisMessage>();
                    
                    if (analysisIds.Any())
                    {
                        var messages = await _messageRepository.GetListAsync(m => analysisIds.Contains(m.PlantAnalysisId));
                        allMessages = messages
                            .Where(m => m.SentDate >= startDate && m.SentDate <= endDate)
                            .ToList();
                    }

                    // Group data by period
                    var timeSeries = new List<TimePeriodData>();
                    var currentDate = startDate.Date;

                    while (currentDate <= endDate.Date)
                    {
                        DateTime periodStart, periodEnd;
                        string periodLabel;

                        switch (request.GroupBy)
                        {
                            case "Week":
                                periodStart = currentDate;
                                periodEnd = currentDate.AddDays(6);
                                periodLabel = $"Week {GetWeekOfYear(currentDate)} - {currentDate:yyyy}";
                                currentDate = currentDate.AddDays(7);
                                break;

                            case "Month":
                                periodStart = new DateTime(currentDate.Year, currentDate.Month, 1);
                                periodEnd = periodStart.AddMonths(1).AddDays(-1);
                                periodLabel = currentDate.ToString("MMM yyyy");
                                currentDate = currentDate.AddMonths(1);
                                break;

                            default: // Day
                                periodStart = currentDate;
                                periodEnd = currentDate;
                                periodLabel = currentDate.ToString("yyyy-MM-dd");
                                currentDate = currentDate.AddDays(1);
                                break;
                        }

                        // Don't include future periods or periods beyond end date
                        if (periodStart > endDate)
                            break;

                        // Calculate metrics for this period
                        var periodCodes = codesInRange.Where(c => 
                            c.DistributionDate.HasValue && 
                            c.DistributionDate.Value >= periodStart && 
                            c.DistributionDate.Value <= periodEnd).ToList();

                        var periodRedemptions = codesInRange.Where(c => 
                            c.UsedDate.HasValue && 
                            c.UsedDate.Value >= periodStart && 
                            c.UsedDate.Value <= periodEnd).ToList();

                        var periodAnalyses = analysesInRange.Where(a => 
                            a.AnalysisDate >= periodStart && 
                            a.AnalysisDate <= periodEnd).ToList();

                        var periodMessages = allMessages.Where(m => 
                            m.SentDate >= periodStart && 
                            m.SentDate <= periodEnd).ToList();

                        // Calculate new farmers for this period
                        var allFarmerIds = analysesInRange
                            .Where(a => a.UserId.HasValue)
                            .Select(a => a.UserId.Value)
                            .Distinct()
                            .ToList();

                        var newFarmersThisPeriod = periodAnalyses
                            .Where(a => a.UserId.HasValue)
                            .Select(a => a.UserId.Value)
                            .Distinct()
                            .Where(farmerId =>
                            {
                                var firstAnalysis = analysesInRange
                                    .Where(a => a.UserId == farmerId)
                                    .MinBy(a => a.AnalysisDate);
                                return firstAnalysis != null &&
                                       firstAnalysis.AnalysisDate >= periodStart &&
                                       firstAnalysis.AnalysisDate <= periodEnd;
                            })
                            .Count();

                        var activeFarmersThisPeriod = periodAnalyses
                            .Where(a => a.UserId.HasValue)
                            .Select(a => a.UserId.Value)
                            .Distinct()
                            .Count();

                        var codesDistributed = periodCodes.Count;
                        var codesRedeemed = periodRedemptions.Count;
                        var redemptionRate = codesDistributed > 0 
                            ? (double)codesRedeemed / codesDistributed * 100 
                            : 0;

                        var engagementRate = allFarmerIds.Count > 0 
                            ? (double)activeFarmersThisPeriod / allFarmerIds.Count * 100 
                            : 0;

                        timeSeries.Add(new TimePeriodData
                        {
                            Period = periodLabel,
                            PeriodStart = periodStart,
                            PeriodEnd = periodEnd,
                            CodesDistributed = codesDistributed,
                            CodesRedeemed = codesRedeemed,
                            AnalysesPerformed = periodAnalyses.Count,
                            NewFarmers = newFarmersThisPeriod,
                            ActiveFarmers = activeFarmersThisPeriod,
                            MessagesSent = periodMessages.Count(m => m.FromUserId == request.SponsorId),
                            MessagesReceived = periodMessages.Count(m => m.ToUserId == request.SponsorId),
                            RedemptionRate = Math.Round(redemptionRate, 2),
                            EngagementRate = Math.Round(engagementRate, 2)
                        });
                    }

                    // Calculate trend analysis
                    var trendAnalysis = CalculateTrendAnalysis(timeSeries);

                    // Calculate peak performance
                    var peakMetrics = CalculatePeakPerformance(timeSeries, analysesInRange, codesInRange);

                    // Build final DTO
                    var analyticsDto = new SponsorTemporalAnalyticsDto
                    {
                        GroupBy = request.GroupBy,
                        TimeSeries = timeSeries,
                        TrendAnalysis = trendAnalysis,
                        PeakMetrics = peakMetrics,
                        StartDate = startDate,
                        EndDate = endDate
                    };

                    // Cache for 1 hour
                    _cacheManager.Add(cacheKey, analyticsDto, CacheDurationMinutes);
                    _logger.LogInformation(
                        "[TemporalAnalytics] Cached data for sponsor {SponsorId} (TTL: 1h)", 
                        request.SponsorId);

                    return new SuccessDataResult<SponsorTemporalAnalyticsDto>(
                        analyticsDto,
                        "Temporal analytics retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "[TemporalAnalytics] Error fetching analytics for sponsor {SponsorId}", 
                        request.SponsorId);
                    return new ErrorDataResult<SponsorTemporalAnalyticsDto>(
                        $"Error retrieving temporal analytics: {ex.Message}");
                }
            }

            private TrendSummary CalculateTrendAnalysis(List<TimePeriodData> timeSeries)
            {
                if (timeSeries.Count < 2)
                {
                    return new TrendSummary
                    {
                        Direction = "Stable",
                        PeriodsAnalyzed = timeSeries.Count
                    };
                }

                var lastPeriod = timeSeries[timeSeries.Count - 1];
                var previousPeriod = timeSeries[timeSeries.Count - 2];

                var redemptionGrowth = CalculateGrowth(previousPeriod.CodesRedeemed, lastPeriod.CodesRedeemed);
                var analysisGrowth = CalculateGrowth(previousPeriod.AnalysesPerformed, lastPeriod.AnalysesPerformed);
                var farmerGrowth = CalculateGrowth(previousPeriod.ActiveFarmers, lastPeriod.ActiveFarmers);
                var engagementGrowth = CalculateGrowth(previousPeriod.EngagementRate, lastPeriod.EngagementRate);

                var averageGrowth = (redemptionGrowth + analysisGrowth + farmerGrowth + engagementGrowth) / 4;

                string direction;
                if (averageGrowth > 5)
                    direction = "Up";
                else if (averageGrowth < -5)
                    direction = "Down";
                else
                    direction = "Stable";

                return new TrendSummary
                {
                    Direction = direction,
                    RedemptionGrowth = Math.Round(redemptionGrowth, 2),
                    AnalysisGrowth = Math.Round(analysisGrowth, 2),
                    FarmerGrowth = Math.Round(farmerGrowth, 2),
                    EngagementGrowth = Math.Round(engagementGrowth, 2),
                    AverageGrowthRate = Math.Round(averageGrowth, 2),
                    PeriodsAnalyzed = timeSeries.Count
                };
            }

            private double CalculateGrowth(double previous, double current)
            {
                if (previous == 0)
                    return current > 0 ? 100 : 0;
                return ((current - previous) / previous) * 100;
            }

            private PeakPerformance CalculatePeakPerformance(
                List<TimePeriodData> timeSeries, 
                List<Entities.Concrete.PlantAnalysis> analyses,
                List<Entities.Concrete.SponsorshipCode> codes)
            {
                var peakMetrics = new PeakPerformance();

                if (!analyses.Any())
                    return peakMetrics;

                // Find peak analysis day
                var dailyAnalyses = analyses
                    .GroupBy(a => a.AnalysisDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault();

                if (dailyAnalyses != null)
                {
                    peakMetrics.PeakAnalysisDate = dailyAnalyses.Date;
                    peakMetrics.PeakAnalysisCount = dailyAnalyses.Count;
                }

                // Find peak redemption day
                var dailyRedemptions = codes
                    .Where(c => c.UsedDate.HasValue)
                    .GroupBy(c => c.UsedDate.Value.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault();

                if (dailyRedemptions != null)
                {
                    peakMetrics.PeakRedemptionDate = dailyRedemptions.Date;
                    peakMetrics.PeakRedemptionCount = dailyRedemptions.Count;
                }

                // Find peak engagement day
                var dailyEngagement = analyses
                    .Where(a => a.UserId.HasValue)
                    .GroupBy(a => a.AnalysisDate.Date)
                    .Select(g => new { Date = g.Key, Farmers = g.Select(a => a.UserId.Value).Distinct().Count() })
                    .OrderByDescending(x => x.Farmers)
                    .FirstOrDefault();

                if (dailyEngagement != null)
                {
                    peakMetrics.PeakEngagementDate = dailyEngagement.Date;
                    peakMetrics.PeakEngagementFarmers = dailyEngagement.Farmers;
                }

                // Find best and worst periods
                if (timeSeries.Any())
                {
                    var bestPeriod = timeSeries.OrderByDescending(p => p.AnalysesPerformed).FirstOrDefault();
                    var worstPeriod = timeSeries.OrderBy(p => p.AnalysesPerformed).FirstOrDefault();

                    peakMetrics.BestPeriod = bestPeriod?.Period;
                    peakMetrics.WorstPeriod = worstPeriod?.Period;
                }

                return peakMetrics;
            }

            private int GetWeekOfYear(DateTime date)
            {
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                return culture.Calendar.GetWeekOfYear(
                    date, 
                    System.Globalization.CalendarWeekRule.FirstDay, 
                    DayOfWeek.Monday);
            }
        }
    }
}
