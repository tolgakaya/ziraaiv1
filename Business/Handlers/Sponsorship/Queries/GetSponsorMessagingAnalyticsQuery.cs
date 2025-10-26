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
    /// Get comprehensive messaging analytics for sponsor
    /// Includes message volumes, response metrics, conversation metrics, content types, and satisfaction ratings
    /// Cache TTL: 15 minutes for real-time messaging insights
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetSponsorMessagingAnalyticsQuery : IRequest<IDataResult<SponsorMessagingAnalyticsDto>>
    {
        public int SponsorId { get; set; }
        public DateTime? StartDate { get; set; } // Optional: filter by date range
        public DateTime? EndDate { get; set; } // Optional: filter by date range

        public class GetSponsorMessagingAnalyticsQueryHandler : IRequestHandler<GetSponsorMessagingAnalyticsQuery, IDataResult<SponsorMessagingAnalyticsDto>>
        {
            private readonly IAnalysisMessageRepository _messageRepository;
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IUserRepository _userRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetSponsorMessagingAnalyticsQueryHandler> _logger;

            private const string CacheKeyPrefix = "SponsorMessagingAnalytics";
            private const int CacheDurationMinutes = 15; // 15 minutes as per spec

            public GetSponsorMessagingAnalyticsQueryHandler(
                IAnalysisMessageRepository messageRepository,
                IPlantAnalysisRepository analysisRepository,
                ISponsorshipCodeRepository codeRepository,
                IUserRepository userRepository,
                ISubscriptionTierRepository tierRepository,
                ICacheManager cacheManager,
                ILogger<GetSponsorMessagingAnalyticsQueryHandler> logger)
            {
                _messageRepository = messageRepository;
                _analysisRepository = analysisRepository;
                _codeRepository = codeRepository;
                _userRepository = userRepository;
                _tierRepository = tierRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            /// <summary>
            /// Generate cache key including sponsor ID and optional date filters
            /// </summary>
            private string GetCacheKey(int sponsorId, DateTime? startDate, DateTime? endDate)
            {
                var dateKey = startDate.HasValue && endDate.HasValue
                    ? $":{startDate.Value:yyyyMMdd}-{endDate.Value:yyyyMMdd}"
                    : ":all";
                return $"{CacheKeyPrefix}:{sponsorId}{dateKey}";
            }

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<SponsorMessagingAnalyticsDto>> Handle(
                GetSponsorMessagingAnalyticsQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Check cache first
                    var cacheKey = GetCacheKey(request.SponsorId, request.StartDate, request.EndDate);
                    var cachedData = _cacheManager.Get<SponsorMessagingAnalyticsDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[MessagingAnalytics] Cache HIT for sponsor {SponsorId}", request.SponsorId);
                        return new SuccessDataResult<SponsorMessagingAnalyticsDto>(
                            cachedData,
                            "Messaging analytics retrieved from cache");
                    }

                    _logger.LogInformation("[MessagingAnalytics] Cache MISS for sponsor {SponsorId} - fetching from database", request.SponsorId);

                    // Get all plant analyses sponsored by this sponsor
                    var sponsoredAnalyses = await _analysisRepository.GetListAsync(
                        a => a.SponsorCompanyId.HasValue && a.SponsorCompanyId.Value == request.SponsorId);
                    
                    var sponsoredAnalysesList = sponsoredAnalyses.ToList();

                    if (!sponsoredAnalysesList.Any())
                    {
                        return new SuccessDataResult<SponsorMessagingAnalyticsDto>(
                            new SponsorMessagingAnalyticsDto
                            {
                                DataStartDate = request.StartDate ?? DateTime.Now.AddMonths(-1),
                                DataEndDate = request.EndDate ?? DateTime.Now
                            },
                            "No sponsored analyses found");
                    }

                    var analysisIds = sponsoredAnalysesList.Select(a => a.Id).ToList();

                    // Get all messages for these analyses
                    var allMessages = await _messageRepository.GetListAsync(
                        m => analysisIds.Contains(m.PlantAnalysisId));

                    var allMessagesList = allMessages.ToList();

                    // Apply date filters if provided
                    if (request.StartDate.HasValue)
                    {
                        allMessagesList = allMessagesList
                            .Where(m => m.SentDate >= request.StartDate.Value)
                            .ToList();
                    }

                    if (request.EndDate.HasValue)
                    {
                        allMessagesList = allMessagesList
                            .Where(m => m.SentDate <= request.EndDate.Value)
                            .ToList();
                    }

                    if (!allMessagesList.Any())
                    {
                        return new SuccessDataResult<SponsorMessagingAnalyticsDto>(
                            new SponsorMessagingAnalyticsDto
                            {
                                DataStartDate = request.StartDate ?? DateTime.Now.AddMonths(-1),
                                DataEndDate = request.EndDate ?? DateTime.Now
                            },
                            "No messages found in specified date range");
                    }

                    // Calculate message volumes
                    var totalMessagesSent = allMessagesList.Count(m => m.FromUserId == request.SponsorId);
                    var totalMessagesReceived = allMessagesList.Count(m => m.ToUserId == request.SponsorId);
                    var unreadMessagesCount = allMessagesList.Count(m => 
                        m.ToUserId == request.SponsorId && !m.IsRead);

                    // Calculate response metrics
                    var farmerMessages = allMessagesList
                        .Where(m => m.ToUserId == request.SponsorId)
                        .OrderBy(m => m.SentDate)
                        .ToList();

                    var responseTimes = new List<double>();
                    var messagesWithResponse = 0;

                    foreach (var farmerMsg in farmerMessages)
                    {
                        // Find first sponsor response after this farmer message
                        var sponsorResponse = allMessagesList
                            .Where(m => m.FromUserId == request.SponsorId &&
                                       m.PlantAnalysisId == farmerMsg.PlantAnalysisId &&
                                       m.SentDate > farmerMsg.SentDate)
                            .OrderBy(m => m.SentDate)
                            .FirstOrDefault();

                        if (sponsorResponse != null)
                        {
                            messagesWithResponse++;
                            var responseTime = (sponsorResponse.SentDate - farmerMsg.SentDate).TotalHours;
                            responseTimes.Add(responseTime);
                        }
                    }

                    var averageResponseTimeHours = responseTimes.Any() ? responseTimes.Average() : 0;
                    var responseRate = farmerMessages.Count > 0
                        ? (double)messagesWithResponse / farmerMessages.Count * 100
                        : 0;

                    // Calculate conversation metrics
                    var totalConversations = analysisIds.Count;
                    var sevenDaysAgo = DateTime.Now.AddDays(-7);
                    var activeConversations = allMessagesList
                        .Where(m => m.SentDate >= sevenDaysAgo)
                        .Select(m => m.PlantAnalysisId)
                        .Distinct()
                        .Count();

                    // Calculate content types
                    var textMessageCount = allMessagesList.Count(m => 
                        string.IsNullOrEmpty(m.VoiceMessageUrl) && 
                        !m.HasAttachments);
                    var voiceMessageCount = allMessagesList.Count(m => !string.IsNullOrEmpty(m.VoiceMessageUrl));
                    var attachmentCount = allMessagesList.Count(m => m.HasAttachments);

                    // Calculate satisfaction metrics
                    var ratedMessages = allMessagesList.Where(m => m.Rating.HasValue).ToList();
                    var averageMessageRating = ratedMessages.Any()
                        ? (double?)ratedMessages.Average(m => m.Rating.Value)
                        : null;
                    var positiveRatingsCount = ratedMessages.Count(m => m.Rating >= 4);

                    // Build top 10 most active conversations
                    var conversationGroups = allMessagesList
                        .GroupBy(m => m.PlantAnalysisId)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .ToList();

                    var mostActiveConversations = new List<ConversationSummary>();

                    foreach (var group in conversationGroups)
                    {
                        var analysisId = group.Key;
                        var analysis = sponsoredAnalysesList.FirstOrDefault(a => a.Id == analysisId);
                        if (analysis == null) continue;

                        var farmerId = analysis.UserId;
                        if (!farmerId.HasValue) continue;

                        var farmer = await _userRepository.GetAsync(u => u.UserId == farmerId.Value);
                        if (farmer == null) continue;

                        // Determine tier for privacy rules
                        var subscription = await _analysisRepository.GetAsync(a => a.Id == analysisId);
                        var tierName = "S"; // Default to most restrictive

                        if (subscription?.ActiveSponsorshipId != null)
                        {
                            var code = await _codeRepository.GetAsync(c => 
                                c.CreatedSubscriptionId.HasValue && 
                                c.CreatedSubscriptionId.Value == subscription.ActiveSponsorshipId.Value);
                            if (code != null)
                            {
                                var tier = await _tierRepository.GetAsync(t => t.Id == code.SubscriptionTierId);
                                tierName = tier?.TierName ?? "S";
                            }
                        }

                        // Apply tier-based privacy
                        var farmerName = "Anonymous Farmer";
                        if (tierName == "L" || tierName == "XL")
                        {
                            farmerName = farmer.FullName ?? "Unknown Farmer";
                        }

                        var messages = group.ToList();
                        var sponsorMessageCount = messages.Count(m => m.FromUserId == request.SponsorId);
                        var farmerMessageCount = messages.Count(m => m.FromUserId == farmerId.Value);
                        var hasUnreadMessages = messages.Any(m => m.ToUserId == request.SponsorId && !m.IsRead);
                        var lastMessage = messages.OrderByDescending(m => m.SentDate).First();
                        var conversationRatings = messages.Where(m => m.Rating.HasValue).ToList();
                        var avgRating = conversationRatings.Any()
                            ? (double?)conversationRatings.Average(m => m.Rating.Value)
                            : null;

                        mostActiveConversations.Add(new ConversationSummary
                        {
                            AnalysisId = analysisId,
                            FarmerId = farmerId.Value,
                            FarmerName = farmerName,
                            MessageCount = messages.Count,
                            SponsorMessageCount = sponsorMessageCount,
                            FarmerMessageCount = farmerMessageCount,
                            LastMessageDate = lastMessage.SentDate,
                            HasUnreadMessages = hasUnreadMessages,
                            CropType = analysis.CropType ?? "Unknown",
                            Disease = analysis.PrimaryIssue ?? analysis.PrimaryConcern ?? "Unknown",
                            AverageRating = avgRating
                        });
                    }

                    // Build final DTO
                    var analyticsDto = new SponsorMessagingAnalyticsDto
                    {
                        TotalMessagesSent = totalMessagesSent,
                        TotalMessagesReceived = totalMessagesReceived,
                        UnreadMessagesCount = unreadMessagesCount,
                        AverageResponseTimeHours = Math.Round(averageResponseTimeHours, 2),
                        ResponseRate = Math.Round(responseRate, 2),
                        TotalConversations = totalConversations,
                        ActiveConversations = activeConversations,
                        TextMessageCount = textMessageCount,
                        VoiceMessageCount = voiceMessageCount,
                        AttachmentCount = attachmentCount,
                        AverageMessageRating = averageMessageRating.HasValue 
                            ? Math.Round(averageMessageRating.Value, 2) 
                            : null,
                        PositiveRatingsCount = positiveRatingsCount,
                        MostActiveConversations = mostActiveConversations,
                        DataStartDate = request.StartDate ?? allMessagesList.Min(m => m.SentDate),
                        DataEndDate = request.EndDate ?? allMessagesList.Max(m => m.SentDate)
                    };

                    // Cache for 15 minutes
                    _cacheManager.Add(cacheKey, analyticsDto, CacheDurationMinutes);
                    _logger.LogInformation(
                        "[MessagingAnalytics] Cached data for sponsor {SponsorId} (TTL: 15min)", 
                        request.SponsorId);

                    return new SuccessDataResult<SponsorMessagingAnalyticsDto>(
                        analyticsDto,
                        "Messaging analytics retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "[MessagingAnalytics] Error fetching analytics for sponsor {SponsorId}", 
                        request.SponsorId);
                    return new ErrorDataResult<SponsorMessagingAnalyticsDto>(
                        $"Error retrieving messaging analytics: {ex.Message}");
                }
            }
        }
    }
}
