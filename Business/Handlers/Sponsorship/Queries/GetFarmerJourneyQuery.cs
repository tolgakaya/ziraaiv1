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
    /// Get complete farmer journey analytics with timeline and behavioral patterns
    /// Tracks individual farmer's lifecycle from code redemption through ongoing engagement
    /// Cache TTL: 1 hour for balance between performance and data freshness
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetFarmerJourneyQuery : IRequest<IDataResult<FarmerJourneyDto>>
    {
        /// <summary>
        /// Farmer's user ID to retrieve journey for
        /// </summary>
        public int FarmerId { get; set; }

        /// <summary>
        /// Sponsor ID making the request (for authorization validation)
        /// Null for admin requests
        /// </summary>
        public int? RequestingSponsorId { get; set; }

        public class GetFarmerJourneyQueryHandler : IRequestHandler<GetFarmerJourneyQuery, IDataResult<FarmerJourneyDto>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IUserRepository _userRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetFarmerJourneyQueryHandler> _logger;

            private const string CacheKeyPrefix = "FarmerJourney";
            private const int CacheDurationMinutes = 60; // 1 hour
            private const decimal AvgAnalysisValue = 50.00m; // Average value per analysis for sponsor

            public GetFarmerJourneyQueryHandler(
                IPlantAnalysisRepository analysisRepository,
                IUserSubscriptionRepository subscriptionRepository,
                IUserRepository userRepository,
                ISponsorshipCodeRepository codeRepository,
                IAnalysisMessageRepository messageRepository,
                ISubscriptionTierRepository tierRepository,
                ICacheManager cacheManager,
                ILogger<GetFarmerJourneyQueryHandler> logger)
            {
                _analysisRepository = analysisRepository;
                _subscriptionRepository = subscriptionRepository;
                _userRepository = userRepository;
                _codeRepository = codeRepository;
                _messageRepository = messageRepository;
                _tierRepository = tierRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            private string GetCacheKey(int farmerId, int? sponsorId)
                => $"{CacheKeyPrefix}:{farmerId}:{sponsorId?.ToString() ?? "admin"}";

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<FarmerJourneyDto>> Handle(
                GetFarmerJourneyQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Check cache first
                    var cacheKey = GetCacheKey(request.FarmerId, request.RequestingSponsorId);
                    var cachedData = _cacheManager.Get<FarmerJourneyDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[FarmerJourney] Cache HIT for farmer {FarmerId}", request.FarmerId);
                        return new SuccessDataResult<FarmerJourneyDto>(cachedData, "Farmer journey retrieved from cache");
                    }

                    _logger.LogInformation("[FarmerJourney] Cache MISS for farmer {FarmerId} - computing journey", request.FarmerId);

                    // Get farmer info
                    var farmer = await _userRepository.GetAsync(u => u.UserId == request.FarmerId);
                    if (farmer == null)
                    {
                        return new ErrorDataResult<FarmerJourneyDto>("Farmer not found");
                    }

                    // Get all farmer's analyses
                    var allAnalyses = await _analysisRepository.GetListAsync(a => a.UserId == request.FarmerId);
                    var analysesList = allAnalyses.OrderByDescending(a => a.CreatedDate).ToList();

                    // If sponsor-specific request, filter to only this sponsor's analyses
                    var relevantAnalyses = request.RequestingSponsorId.HasValue
                        ? analysesList.Where(a => a.SponsorCompanyId == request.RequestingSponsorId.Value).ToList()
                        : analysesList;

                    if (!relevantAnalyses.Any())
                    {
                        return new ErrorDataResult<FarmerJourneyDto>(
                            "No analyses found for this farmer" +
                            (request.RequestingSponsorId.HasValue ? " from your sponsorship" : ""));
                    }

                    // Get farmer's subscriptions
                    var subscriptions = await _subscriptionRepository.GetListAsync(s => s.UserId == request.FarmerId);
                    var subscriptionsList = subscriptions.OrderByDescending(s => s.CreatedDate).ToList();

                    // Get farmer's redeemed codes
                    var redeemedCodes = await _codeRepository.GetListAsync(c => c.UsedByUserId == request.FarmerId && c.IsUsed);
                    var redeemedCodesList = redeemedCodes.OrderBy(c => c.UsedDate).ToList();

                    // Get farmer's messages
                    var analysisIds = relevantAnalyses.Select(a => a.Id).ToList();
                    var messages = await _messageRepository.GetListAsync(m => analysisIds.Contains(m.PlantAnalysisId));
                    var messagesList = messages.OrderByDescending(m => m.CreatedDate).ToList();

                    // Build journey summary
                    var journeySummary = await BuildJourneySummary(
                        farmer, relevantAnalyses, subscriptionsList, redeemedCodesList);

                    // Build timeline
                    var timeline = await BuildTimeline(
                        relevantAnalyses, subscriptionsList, redeemedCodesList, messagesList);

                    // Analyze behavioral patterns
                    var behavioralPatterns = AnalyzeBehavioralPatterns(
                        relevantAnalyses, messagesList, subscriptionsList);

                    // Generate recommended actions
                    var recommendedActions = GenerateRecommendedActions(
                        journeySummary, behavioralPatterns, relevantAnalyses);

                    var result = new FarmerJourneyDto
                    {
                        FarmerId = request.FarmerId,
                        FarmerName = farmer.FullName ?? "Unknown",
                        JourneySummary = journeySummary,
                        Timeline = timeline,
                        BehavioralPatterns = behavioralPatterns,
                        RecommendedActions = recommendedActions
                    };

                    // Cache the result
                    _cacheManager.Add(cacheKey, result, CacheDurationMinutes);

                    _logger.LogInformation("[FarmerJourney] Successfully computed journey for farmer {FarmerId} with {EventCount} timeline events",
                        request.FarmerId, timeline.Count);

                    return new SuccessDataResult<FarmerJourneyDto>(result, "Farmer journey analytics retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FarmerJourney] Error computing journey for farmer {FarmerId}", request.FarmerId);
                    return new ErrorDataResult<FarmerJourneyDto>($"Farmer journey retrieval failed: {ex.Message}");
                }
            }

            private async Task<JourneySummaryDto> BuildJourneySummary(
                Core.Entities.Concrete.User farmer,
                List<Entities.Concrete.PlantAnalysis> analyses,
                List<Entities.Concrete.UserSubscription> subscriptions,
                List<Entities.Concrete.SponsorshipCode> redeemedCodes)
            {
                var firstRedemption = redeemedCodes.FirstOrDefault()?.UsedDate;
                var totalDays = firstRedemption.HasValue
                    ? (DateTime.Now - firstRedemption.Value).Days
                    : 0;

                var activeSubscription = subscriptions
                    .FirstOrDefault(s => s.IsActive && s.EndDate >= DateTime.Now);

                var daysUntilRenewal = activeSubscription?.EndDate != null
                    ? (int?)(activeSubscription.EndDate - DateTime.Now).Days
                    : null;

                // Determine lifecycle stage
                var daysSinceLastAnalysis = analyses.Any()
                    ? (DateTime.Now - analyses.First().CreatedDate).Days
                    : int.MaxValue;

                string lifecycleStage;
                if (daysSinceLastAnalysis <= 7) lifecycleStage = "Active";
                else if (daysSinceLastAnalysis <= 30) lifecycleStage = "At-Risk";
                else if (daysSinceLastAnalysis <= 60) lifecycleStage = "Dormant";
                else lifecycleStage = "Churned";

                return new JourneySummaryDto
                {
                    FirstCodeRedemption = firstRedemption,
                    TotalDaysAsCustomer = totalDays,
                    TotalAnalyses = analyses.Count,
                    TotalSpent = 0m, // Sponsored farmers don't pay
                    TotalValueGenerated = analyses.Count * AvgAnalysisValue,
                    CurrentTier = await GetTierName(activeSubscription),
                    LifecycleStage = lifecycleStage,
                    NextRenewalDate = activeSubscription?.EndDate,
                    DaysUntilRenewal = daysUntilRenewal
                };
            }

            private async Task<List<TimelineEventDto>> BuildTimeline(
                List<Entities.Concrete.PlantAnalysis> analyses,
                List<Entities.Concrete.UserSubscription> subscriptions,
                List<Entities.Concrete.SponsorshipCode> redeemedCodes,
                List<Entities.Concrete.AnalysisMessage> messages)
            {
                var timeline = new List<TimelineEventDto>();

                // Add code redemption events
                foreach (var code in redeemedCodes)
                {
                    if (code.UsedDate.HasValue)
                    {
                        var codeTierName = await GetTierNameById(code.SubscriptionTierId);
                        timeline.Add(new TimelineEventDto
                        {
                            Date = code.UsedDate.Value,
                            EventType = "Code Redeemed",
                            Details = $"Code {code.Code} activated",
                            Tier = codeTierName,
                            Trigger = "User Action"
                        });
                    }
                }

                // Add analysis events
                foreach (var analysis in analyses)
                {
                    var isFirstAnalysis = analyses.OrderBy(a => a.CreatedDate).First().Id == analysis.Id;

                    timeline.Add(new TimelineEventDto
                    {
                        Date = analysis.CreatedDate,
                        EventType = isFirstAnalysis ? "First Analysis" : "Analysis",
                        Details = $"{analysis.CropType ?? "Unknown crop"} - {analysis.Diseases ?? "Analysis performed"}",
                        CropType = analysis.CropType,
                        Disease = analysis.Diseases,
                        Severity = analysis.HealthSeverity,
                        Trigger = "User Action"
                    });
                }

                // Add message events
                foreach (var message in messages)
                {
                    timeline.Add(new TimelineEventDto
                    {
                        Date = message.CreatedDate,
                        EventType = "Message Sent",
                        Details = "Sponsor sent follow-up message",
                        Channel = "In-app",
                        Trigger = "Sponsor Action"
                    });
                }

                // Add subscription events
                foreach (var subscription in subscriptions)
                {
                    var subTierName = await GetTierNameById(subscription.SubscriptionTierId);
                    timeline.Add(new TimelineEventDto
                    {
                        Date = subscription.CreatedDate,
                        EventType = "Subscription Created",
                        Details = $"{subTierName} tier subscription activated",
                        Tier = subTierName,
                        Trigger = "System Action"
                    });
                }

                // Detect activity patterns
                DetectActivityPatterns(analyses, timeline);

                return timeline.OrderByDescending(t => t.Date).ToList();
            }

            private void DetectActivityPatterns(
                List<Entities.Concrete.PlantAnalysis> analyses,
                List<TimelineEventDto> timeline)
            {
                if (!analyses.Any()) return;

                var sortedAnalyses = analyses.OrderBy(a => a.CreatedDate).ToList();

                // Detect high activity periods (more than 10 analyses in 7 days)
                for (int i = 0; i < sortedAnalyses.Count; i++)
                {
                    var currentDate = sortedAnalyses[i].CreatedDate;
                    var weekAnalyses = sortedAnalyses
                        .Where(a => a.CreatedDate >= currentDate && a.CreatedDate <= currentDate.AddDays(7))
                        .ToList();

                    if (weekAnalyses.Count >= 10)
                    {
                        timeline.Add(new TimelineEventDto
                        {
                            Date = currentDate.AddDays(3), // Middle of the period
                            EventType = "High Activity Period",
                            Details = $"{weekAnalyses.Count} analyses in 7 days",
                            Trigger = "Activity Pattern",
                            AlertLevel = "Info"
                        });
                    }
                }

                // Detect decreased activity periods (no analyses for 21+ days after being active)
                for (int i = 0; i < sortedAnalyses.Count - 1; i++)
                {
                    var daysBetween = (sortedAnalyses[i + 1].CreatedDate - sortedAnalyses[i].CreatedDate).Days;

                    if (daysBetween >= 21)
                    {
                        timeline.Add(new TimelineEventDto
                        {
                            Date = sortedAnalyses[i].CreatedDate.AddDays(21),
                            EventType = "Decreased Activity",
                            Details = $"No analyses in {daysBetween} days",
                            AlertLevel = "Warning",
                            Trigger = "Inactivity Detected"
                        });
                    }
                }
            }

            private BehavioralPatternsDto AnalyzeBehavioralPatterns(
                List<Entities.Concrete.PlantAnalysis> analyses,
                List<Entities.Concrete.AnalysisMessage> messages,
                List<Entities.Concrete.UserSubscription> subscriptions)
            {
                if (!analyses.Any())
                {
                    return new BehavioralPatternsDto
                    {
                        PreferredContactTime = "Unknown",
                        AverageDaysBetweenAnalyses = 0,
                        MostActiveSeason = "Unknown",
                        PreferredCrops = new List<string>(),
                        CommonIssues = new List<string>(),
                        MessageResponseRate = 0,
                        AverageMessageResponseTimeHours = 0,
                        MostActiveWeekday = "Unknown",
                        EngagementTrend = "Unknown",
                        ChurnRiskScore = 100
                    };
                }

                var sortedAnalyses = analyses.OrderBy(a => a.CreatedDate).ToList();

                // Calculate average days between analyses
                var daysBetweenList = new List<int>();
                for (int i = 0; i < sortedAnalyses.Count - 1; i++)
                {
                    var days = (sortedAnalyses[i + 1].CreatedDate - sortedAnalyses[i].CreatedDate).Days;
                    if (days > 0) daysBetweenList.Add(days);
                }
                var avgDaysBetween = daysBetweenList.Any() ? (decimal)daysBetweenList.Average() : 0;

                // Determine most active season
                var seasonCounts = new Dictionary<string, int>
                {
                    { "Spring", analyses.Count(a => a.CreatedDate.Month >= 3 && a.CreatedDate.Month <= 5) },
                    { "Summer", analyses.Count(a => a.CreatedDate.Month >= 6 && a.CreatedDate.Month <= 8) },
                    { "Fall", analyses.Count(a => a.CreatedDate.Month >= 9 && a.CreatedDate.Month <= 11) },
                    { "Winter", analyses.Count(a => a.CreatedDate.Month == 12 || a.CreatedDate.Month <= 2) }
                };
                var mostActiveSeason = seasonCounts.OrderByDescending(s => s.Value).First().Key;

                // Preferred crops (top 3)
                var preferredCrops = analyses
                    .Where(a => !string.IsNullOrEmpty(a.CropType))
                    .GroupBy(a => a.CropType)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList();

                // Common issues (top 5 diseases)
                var commonIssues = analyses
                    .Where(a => !string.IsNullOrEmpty(a.Diseases))
                    .GroupBy(a => a.Diseases)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList();

                // Message response rate (assume IsRead means responded)
                var totalMessages = messages.Count;
                var respondedMessages = messages.Count(m => m.IsRead == true);
                var messageResponseRate = totalMessages > 0 ? (decimal)respondedMessages / totalMessages * 100 : 0;

                // Most active weekday
                var weekdayCounts = analyses
                    .GroupBy(a => a.CreatedDate.DayOfWeek.ToString())
                    .Select(g => new { Day = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();
                var mostActiveWeekday = weekdayCounts.Any() ? weekdayCounts.First().Day : "Unknown";

                // Engagement trend (last 90 days vs previous 90 days)
                var now = DateTime.Now;
                var last90Days = analyses.Count(a => a.CreatedDate >= now.AddDays(-90));
                var previous90Days = analyses.Count(a => a.CreatedDate >= now.AddDays(-180) && a.CreatedDate < now.AddDays(-90));

                string engagementTrend;
                if (last90Days > previous90Days * 1.2m) engagementTrend = "Increasing";
                else if (last90Days < previous90Days * 0.8m) engagementTrend = "Decreasing";
                else engagementTrend = "Stable";

                // Churn risk score (0-100, higher = more risk)
                var daysSinceLastAnalysis = (DateTime.Now - sortedAnalyses.First().CreatedDate).Days;
                var churnRiskScore = CalculateChurnRiskScore(
                    daysSinceLastAnalysis, avgDaysBetween, engagementTrend, subscriptions);

                // Preferred contact time (based on analysis creation times)
                var hourCounts = analyses
                    .GroupBy(a => a.CreatedDate.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var preferredContactTime = "Unknown";
                if (hourCounts.Any())
                {
                    var mostActiveHour = hourCounts.First().Hour;
                    preferredContactTime = $"{mostActiveHour:D2}:00-{(mostActiveHour + 3):D2}:00";
                }

                return new BehavioralPatternsDto
                {
                    PreferredContactTime = preferredContactTime,
                    AverageDaysBetweenAnalyses = avgDaysBetween,
                    MostActiveSeason = mostActiveSeason,
                    PreferredCrops = preferredCrops,
                    CommonIssues = commonIssues,
                    MessageResponseRate = messageResponseRate,
                    AverageMessageResponseTimeHours = 0, // Would need message timestamps to calculate
                    MostActiveWeekday = mostActiveWeekday,
                    EngagementTrend = engagementTrend,
                    ChurnRiskScore = churnRiskScore
                };
            }

            private decimal CalculateChurnRiskScore(
                int daysSinceLastAnalysis,
                decimal avgDaysBetween,
                string engagementTrend,
                List<Entities.Concrete.UserSubscription> subscriptions)
            {
                decimal riskScore = 0;

                // Factor 1: Days since last analysis (40% weight)
                if (daysSinceLastAnalysis > 60) riskScore += 40;
                else if (daysSinceLastAnalysis > 30) riskScore += 30;
                else if (daysSinceLastAnalysis > 14) riskScore += 20;
                else if (daysSinceLastAnalysis > 7) riskScore += 10;

                // Factor 2: Engagement trend (30% weight)
                if (engagementTrend == "Decreasing") riskScore += 30;
                else if (engagementTrend == "Stable") riskScore += 10;

                // Factor 3: Subscription status (30% weight)
                var activeSubscription = subscriptions
                    .FirstOrDefault(s => s.IsActive && s.EndDate >= DateTime.Now);

                if (activeSubscription == null)
                {
                    riskScore += 30;
                }
                else
                {
                    var daysUntilExpiry = (activeSubscription.EndDate - DateTime.Now).Days;
                    if (daysUntilExpiry <= 7) riskScore += 20;
                    else if (daysUntilExpiry <= 30) riskScore += 10;
                }

                return Math.Min(100, riskScore);
            }

            private List<string> GenerateRecommendedActions(
                JourneySummaryDto summary,
                BehavioralPatternsDto patterns,
                List<Entities.Concrete.PlantAnalysis> analyses)
            {
                var recommendations = new List<string>();

                // Based on lifecycle stage
                if (summary.LifecycleStage == "At-Risk")
                {
                    recommendations.Add($"Send reengagement message (farmer inactive for {patterns.AverageDaysBetweenAnalyses:F0}+ days)");
                }
                else if (summary.LifecycleStage == "Dormant" || summary.LifecycleStage == "Churned")
                {
                    recommendations.Add("Launch win-back campaign with special offer");
                }

                // Based on renewal date
                if (summary.DaysUntilRenewal.HasValue && summary.DaysUntilRenewal.Value <= 30)
                {
                    recommendations.Add($"Offer early renewal discount (expires in {summary.DaysUntilRenewal.Value} days)");
                }

                // Based on activity patterns
                if (patterns.AverageDaysBetweenAnalyses > 0)
                {
                    recommendations.Add($"Schedule follow-up in {patterns.AverageDaysBetweenAnalyses:F0} days (typical cycle)");
                }

                // Based on common issues
                if (patterns.CommonIssues.Any())
                {
                    var topIssue = patterns.CommonIssues.First();
                    recommendations.Add($"Recommend products for {topIssue}");
                }

                // Based on preferred crops
                if (patterns.PreferredCrops.Any())
                {
                    recommendations.Add($"Share seasonal tips for {string.Join(", ", patterns.PreferredCrops)}");
                }

                // Based on engagement trend
                if (patterns.EngagementTrend == "Increasing")
                {
                    recommendations.Add("Consider upselling to higher tier (engagement is growing)");
                }

                // Default recommendation if list is empty
                if (!recommendations.Any())
                {
                    recommendations.Add("Continue monitoring farmer activity");
                }

                return recommendations.Take(5).ToList(); // Return top 5 recommendations
            }

            /// <summary>
            /// Helper method to get tier name from UserSubscription
            /// </summary>
            private async Task<string> GetTierName(Entities.Concrete.UserSubscription subscription)
            {
                if (subscription == null) return "None";

                var tier = await _tierRepository.GetAsync(t => t.Id == subscription.SubscriptionTierId);
                return tier?.TierName ?? "Unknown";
            }

            /// <summary>
            /// Helper method to get tier name by tier ID
            /// </summary>
            private async Task<string> GetTierNameById(int tierId)
            {
                var tier = await _tierRepository.GetAsync(t => t.Id == tierId);
                return tier?.TierName ?? "Unknown";
            }
        }
    }
}
