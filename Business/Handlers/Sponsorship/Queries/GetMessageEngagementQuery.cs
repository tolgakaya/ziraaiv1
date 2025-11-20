using System;
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
    /// Query to get message engagement analytics for sponsors
    /// Analyzes sponsor-farmer messaging effectiveness and patterns
    /// </summary>
    public class GetMessageEngagementQuery : IRequest<IDataResult<MessageEngagementDto>>
    {
        public int? SponsorId { get; set; }
    }

    public class GetMessageEngagementQueryHandler : IRequestHandler<GetMessageEngagementQuery, IDataResult<MessageEngagementDto>>
    {
        private readonly IAnalysisMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheManager _cacheManager;
        private readonly ILogger<GetMessageEngagementQueryHandler> _logger;

        public GetMessageEngagementQueryHandler(
            IAnalysisMessageRepository messageRepository,
            IUserRepository userRepository,
            ICacheManager cacheManager,
            ILogger<GetMessageEngagementQueryHandler> logger)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _cacheManager = cacheManager;
            _logger = logger;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<MessageEngagementDto>> Handle(
            GetMessageEngagementQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("[MessageEngagement] Retrieving message engagement analytics for sponsor {SponsorId}",
                    request.SponsorId?.ToString() ?? "ALL");

                // Check cache first
                var cacheKey = GetCacheKey(request.SponsorId);
                var cachedData = _cacheManager.Get<MessageEngagementDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("[MessageEngagement] Returning cached data");
                    return new SuccessDataResult<MessageEngagementDto>(cachedData, "Cached message engagement retrieved");
                }

                // Get all messages filtered by sponsor if specified
                var allMessages = request.SponsorId.HasValue
                    ? await _messageRepository.GetListAsync(m =>
                        m.FromUserId == request.SponsorId.Value || m.ToUserId == request.SponsorId.Value)
                    : await _messageRepository.GetListAsync(m => true);

                if (!allMessages.Any())
                {
                    _logger.LogWarning("[MessageEngagement] No messages found");
                    return new SuccessDataResult<MessageEngagementDto>(
                        new MessageEngagementDto { SponsorId = request.SponsorId },
                        "No message data available");
                }

                // Separate sponsor messages (sent) vs farmer messages (received)
                var sponsorMessages = request.SponsorId.HasValue
                    ? allMessages.Where(m => m.FromUserId == request.SponsorId.Value).ToList()
                    : allMessages.Where(m => m.SenderRole == "Sponsor").ToList();

                var farmerMessages = request.SponsorId.HasValue
                    ? allMessages.Where(m => m.ToUserId == request.SponsorId.Value && m.SenderRole == "Farmer").ToList()
                    : allMessages.Where(m => m.SenderRole == "Farmer").ToList();

                // Calculate response rate and average response time
                var messagesWithResponses = sponsorMessages
                    .Where(sm => farmerMessages.Any(fm =>
                        fm.ParentMessageId == sm.Id ||
                        (fm.PlantAnalysisId == sm.PlantAnalysisId && fm.SentDate > sm.SentDate)))
                    .ToList();

                var responseRate = sponsorMessages.Any()
                    ? (decimal)messagesWithResponses.Count / sponsorMessages.Count * 100
                    : 0;

                var avgResponseTime = CalculateAverageResponseTime(sponsorMessages, farmerMessages);
                var engagementScore = CalculateEngagementScore(responseRate, avgResponseTime);

                // Message breakdown by type
                var messageBreakdown = CalculateMessageBreakdown(sponsorMessages, farmerMessages);

                // Best performing messages
                var bestPerformingMessages = AnalyzeBestPerformingMessages(sponsorMessages, farmerMessages);

                // Time of day analysis
                var timeOfDayAnalysis = AnalyzeTimeOfDay(sponsorMessages, farmerMessages);

                var result = new MessageEngagementDto
                {
                    TotalMessagesSent = sponsorMessages.Count,
                    TotalMessagesReceived = farmerMessages.Count,
                    ResponseRate = responseRate,
                    AverageResponseTime = avgResponseTime,
                    EngagementScore = engagementScore,
                    MessageBreakdown = messageBreakdown,
                    BestPerformingMessages = bestPerformingMessages,
                    TimeOfDayAnalysis = timeOfDayAnalysis,
                    SponsorId = request.SponsorId,
                    GeneratedAt = DateTime.Now
                };

                // Cache for 6 hours
                _cacheManager.Add(cacheKey, result, 360);

                _logger.LogInformation("[MessageEngagement] Analytics generated successfully with {MessageCount} messages analyzed",
                    allMessages.Count());

                return new SuccessDataResult<MessageEngagementDto>(result, "Message engagement analytics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageEngagement] Error retrieving message engagement for sponsor {SponsorId}",
                    request.SponsorId);
                return new ErrorDataResult<MessageEngagementDto>("An error occurred while retrieving message engagement analytics");
            }
        }

        private string GetCacheKey(int? sponsorId)
        {
            return sponsorId.HasValue
                ? $"message_engagement_{sponsorId.Value}"
                : "message_engagement_all";
        }

        private decimal CalculateAverageResponseTime(List<Entities.Concrete.AnalysisMessage> sponsorMessages,
            List<Entities.Concrete.AnalysisMessage> farmerMessages)
        {
            var responseTimes = new List<double>();

            foreach (var sponsorMsg in sponsorMessages)
            {
                var responses = farmerMessages
                    .Where(fm => (fm.ParentMessageId == sponsorMsg.Id ||
                                 (fm.PlantAnalysisId == sponsorMsg.PlantAnalysisId && fm.SentDate > sponsorMsg.SentDate)) &&
                                fm.SentDate > sponsorMsg.SentDate)
                    .OrderBy(fm => fm.SentDate)
                    .ToList();

                if (responses.Any())
                {
                    var firstResponse = responses.First();
                    var responseTime = (firstResponse.SentDate - sponsorMsg.SentDate).TotalHours;
                    responseTimes.Add(responseTime);
                }
            }

            return responseTimes.Any() ? (decimal)responseTimes.Average() : 0;
        }

        private decimal CalculateEngagementScore(decimal responseRate, decimal avgResponseTime)
        {
            // Engagement score formula:
            // - 70% weight on response rate (0-100% → 0-7 points)
            // - 30% weight on response speed (faster = better, <2h = 3 points, >24h = 0 points)

            var responseRateScore = (responseRate / 100m) * 7m;

            decimal responseSpeedScore;
            if (avgResponseTime < 2) responseSpeedScore = 3.0m;
            else if (avgResponseTime < 4) responseSpeedScore = 2.5m;
            else if (avgResponseTime < 8) responseSpeedScore = 2.0m;
            else if (avgResponseTime < 12) responseSpeedScore = 1.5m;
            else if (avgResponseTime < 24) responseSpeedScore = 1.0m;
            else responseSpeedScore = 0.5m;

            return Math.Round(responseRateScore + responseSpeedScore, 1);
        }

        private MessageBreakdownDto CalculateMessageBreakdown(
            List<Entities.Concrete.AnalysisMessage> sponsorMessages,
            List<Entities.Concrete.AnalysisMessage> farmerMessages)
        {
            var breakdown = new MessageBreakdownDto();

            // Product recommendations
            var productRecommendations = sponsorMessages
                .Where(m => !string.IsNullOrEmpty(m.LinkedProducts) ||
                           (m.MessageType != null && m.MessageType.Contains("Recommendation")))
                .ToList();
            breakdown.ProductRecommendations = CalculateCategoryStats(productRecommendations, farmerMessages);

            // General queries
            var generalQueries = sponsorMessages
                .Where(m => string.IsNullOrEmpty(m.LinkedProducts) &&
                           (m.MessageType == null || m.MessageType.Contains("Question") || m.MessageType.Contains("Information")))
                .ToList();
            breakdown.GeneralQueries = CalculateCategoryStats(generalQueries, farmerMessages);

            // Follow-ups
            var followUps = sponsorMessages
                .Where(m => m.ParentMessageId.HasValue)
                .ToList();
            breakdown.FollowUps = CalculateCategoryStats(followUps, farmerMessages);

            return breakdown;
        }

        private MessageCategoryStatsDto CalculateCategoryStats(
            List<Entities.Concrete.AnalysisMessage> categoryMessages,
            List<Entities.Concrete.AnalysisMessage> farmerMessages)
        {
            var responded = categoryMessages.Count(cm =>
                farmerMessages.Any(fm =>
                    fm.ParentMessageId == cm.Id ||
                    (fm.PlantAnalysisId == cm.PlantAnalysisId && fm.SentDate > cm.SentDate)));

            var conversionRate = categoryMessages.Any()
                ? (decimal)responded / categoryMessages.Count * 100
                : 0;

            return new MessageCategoryStatsDto
            {
                Sent = categoryMessages.Count,
                Responded = responded,
                ConversionRate = conversionRate
            };
        }

        private List<MessageTemplatePerformanceDto> AnalyzeBestPerformingMessages(
            List<Entities.Concrete.AnalysisMessage> sponsorMessages,
            List<Entities.Concrete.AnalysisMessage> farmerMessages)
        {
            // Group by message type and analyze performance
            var performanceByType = sponsorMessages
                .Where(m => !string.IsNullOrEmpty(m.MessageType))
                .GroupBy(m => m.MessageType)
                .Select(g =>
                {
                    var messages = g.ToList();
                    var withResponses = messages.Count(m =>
                        farmerMessages.Any(fm =>
                            fm.ParentMessageId == m.Id ||
                            (fm.PlantAnalysisId == m.PlantAnalysisId && fm.SentDate > m.SentDate)));

                    var responseTimes = messages
                        .Select(m =>
                        {
                            var response = farmerMessages
                                .Where(fm => (fm.ParentMessageId == m.Id ||
                                            (fm.PlantAnalysisId == m.PlantAnalysisId && fm.SentDate > m.SentDate)) &&
                                           fm.SentDate > m.SentDate)
                                .OrderBy(fm => fm.SentDate)
                                .FirstOrDefault();

                            return response != null ? (response.SentDate - m.SentDate).TotalHours : (double?)null;
                        })
                        .Where(rt => rt.HasValue)
                        .Select(rt => rt.Value)
                        .ToList();

                    var avgResponseTime = responseTimes.Any() ? (decimal)responseTimes.Average() : 0;
                    var responseRate = messages.Any() ? (decimal)withResponses / messages.Count : 0;

                    return new MessageTemplatePerformanceDto
                    {
                        MessageType = g.Key,
                        Template = $"{g.Key} template",
                        ResponseRate = responseRate,
                        AvgResponseTime = avgResponseTime,
                        ConversionRate = responseRate,
                        UsageCount = messages.Count
                    };
                })
                .OrderByDescending(p => p.ResponseRate)
                .ThenBy(p => p.AvgResponseTime)
                .Take(5)
                .ToList();

            return performanceByType;
        }

        private Dictionary<string, TimeSlotAnalysisDto> AnalyzeTimeOfDay(
            List<Entities.Concrete.AnalysisMessage> sponsorMessages,
            List<Entities.Concrete.AnalysisMessage> farmerMessages)
        {
            var timeSlots = new Dictionary<string, TimeSlotAnalysisDto>();

            // Define time slots
            var slots = new[]
            {
                new { Key = "06:00-09:00", Start = 6, End = 9 },
                new { Key = "09:00-12:00", Start = 9, End = 12 },
                new { Key = "12:00-14:00", Start = 12, End = 14 },
                new { Key = "14:00-18:00", Start = 14, End = 18 },
                new { Key = "18:00-21:00", Start = 18, End = 21 },
                new { Key = "21:00-00:00", Start = 21, End = 24 }
            };

            foreach (var slot in slots)
            {
                var slotMessages = sponsorMessages
                    .Where(m => m.SentDate.Hour >= slot.Start && m.SentDate.Hour < slot.End)
                    .ToList();

                if (!slotMessages.Any())
                {
                    timeSlots[slot.Key] = new TimeSlotAnalysisDto
                    {
                        MessagesSent = 0,
                        ResponseRate = 0,
                        BestFor = "Insufficient data"
                    };
                    continue;
                }

                var responded = slotMessages.Count(sm =>
                    farmerMessages.Any(fm =>
                        fm.ParentMessageId == sm.Id ||
                        (fm.PlantAnalysisId == sm.PlantAnalysisId && fm.SentDate > sm.SentDate)));

                var responseRate = (decimal)responded / slotMessages.Count;

                // Determine best use case
                string bestFor;
                if (responseRate > 0.8m) bestFor = "Product recommendations";
                else if (responseRate > 0.6m) bestFor = "General queries";
                else if (responseRate > 0.4m) bestFor = "Follow-up messages";
                else bestFor = "Not recommended - low engagement";

                timeSlots[slot.Key] = new TimeSlotAnalysisDto
                {
                    MessagesSent = slotMessages.Count,
                    ResponseRate = responseRate,
                    BestFor = bestFor
                };
            }

            return timeSlots;
        }
    }
}
