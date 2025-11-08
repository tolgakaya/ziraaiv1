using System;
using System.Collections.Generic;
using Core.Entities.Concrete;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSubscriptions.Queries
{
    /// <summary>
    /// Get detailed subscription information with user details, usage stats, and analysis counts
    /// </summary>
    public class GetSubscriptionDetailsQuery : IRequest<IDataResult<IEnumerable<SubscriptionDetailDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int? UserId { get; set; }
        public int? SponsorId { get; set; }
        public string Status { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsSponsoredSubscription { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }

        public class GetSubscriptionDetailsQueryHandler : IRequestHandler<GetSubscriptionDetailsQuery, IDataResult<IEnumerable<SubscriptionDetailDto>>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserRepository _userRepository;

            public GetSubscriptionDetailsQueryHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IPlantAnalysisRepository analysisRepository,
                IUserRepository userRepository)
            {
                _subscriptionRepository = subscriptionRepository;
                _analysisRepository = analysisRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]


            [PerformanceAspect(5)]


            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<SubscriptionDetailDto>>> Handle(
                GetSubscriptionDetailsQuery request, 
                CancellationToken cancellationToken)
            {
                var now = DateTime.Now;

                // Build query with navigation properties
                var query = _subscriptionRepository.Query()
                    .Include(s => s.SubscriptionTier)
                    .Include(s => s.Sponsor)
                    .Include(s => s.PreviousSponsorship)
                        .ThenInclude(ps => ps.SubscriptionTier)
                    .AsQueryable();

                // Apply filters
                if (request.UserId.HasValue)
                {
                    query = query.Where(s => s.UserId == request.UserId.Value);
                }

                if (request.SponsorId.HasValue)
                {
                    query = query.Where(s => s.SponsorId == request.SponsorId.Value);
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(s => s.Status == request.Status);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == request.IsActive.Value);
                }

                if (request.IsSponsoredSubscription.HasValue)
                {
                    query = query.Where(s => s.IsSponsoredSubscription == request.IsSponsoredSubscription.Value);
                }

                if (request.StartDateFrom.HasValue)
                {
                    query = query.Where(s => s.StartDate >= request.StartDateFrom.Value);
                }

                if (request.StartDateTo.HasValue)
                {
                    query = query.Where(s => s.StartDate <= request.StartDateTo.Value);
                }

                // Execute paginated query
                var subscriptions = await query
                    .OrderByDescending(s => s.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                if (!subscriptions.Any())
                {
                    return new SuccessDataResult<IEnumerable<SubscriptionDetailDto>>(
                        new List<SubscriptionDetailDto>(), 
                        "No subscriptions found");
                }

                // Get user IDs and fetch users
                var userIds = subscriptions.Select(s => s.UserId).Distinct().ToList();
                var users = await _userRepository.Query()
                    .Where(u => userIds.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => u, cancellationToken);

                // Aggregate analysis counts for all users (single query)
                var analysisStats = await _analysisRepository.Query()
                    .Where(a => a.UserId.HasValue && userIds.Contains(a.UserId.Value))
                    .GroupBy(a => a.UserId.Value)
                    .Select(g => new UserAnalysisStats
                    {
                        UserId = g.Key,
                        TotalCount = g.Count(),
                        Last7DaysCount = g.Count(a => a.AnalysisDate >= now.AddDays(-7)),
                        Last30DaysCount = g.Count(a => a.AnalysisDate >= now.AddDays(-30)),
                        LastAnalysisDate = g.Max(a => a.AnalysisDate)
                    })
                    .ToDictionaryAsync(x => x.UserId, x => x, cancellationToken);

                // Get subscription-specific analysis counts (single query)
                var subscriptionAnalysisCounts = await _analysisRepository.Query()
                    .Where(a => a.ActiveSponsorshipId.HasValue && 
                               subscriptions.Select(s => s.Id).Contains(a.ActiveSponsorshipId.Value))
                    .GroupBy(a => a.ActiveSponsorshipId.Value)
                    .Select(g => new
                    {
                        SubscriptionId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.SubscriptionId, x => x.Count, cancellationToken);

                // Map to DTOs
                var result = subscriptions.Select(s => MapToDetailDto(s, users, analysisStats, subscriptionAnalysisCounts, now)).ToList();

                return new SuccessDataResult<IEnumerable<SubscriptionDetailDto>>(result, "Detailed subscriptions retrieved successfully");
            }

            private SubscriptionDetailDto MapToDetailDto(
                UserSubscription subscription,
                Dictionary<int, User> users,
                Dictionary<int, UserAnalysisStats> analysisStats,
                Dictionary<int, int> subscriptionAnalysisCounts,
                DateTime now)
            {
                var tier = subscription.SubscriptionTier;
                var user = users.ContainsKey(subscription.UserId) ? users[subscription.UserId] : null;
                var sponsor = subscription.Sponsor;

                // Calculate time metrics
                var totalDays = (subscription.EndDate - subscription.StartDate).Days;
                var remainingDays = (subscription.EndDate - now).Days;
                remainingDays = remainingDays < 0 ? 0 : remainingDays;
                var timeUsagePercentage = totalDays > 0 ? ((totalDays - remainingDays) / (double)totalDays) * 100 : 0;

                // Calculate usage metrics
                var remainingDaily = tier.DailyRequestLimit - subscription.CurrentDailyUsage;
                remainingDaily = remainingDaily < 0 ? 0 : remainingDaily;
                var remainingMonthly = tier.MonthlyRequestLimit - subscription.CurrentMonthlyUsage;
                remainingMonthly = remainingMonthly < 0 ? 0 : remainingMonthly;

                var dailyUsagePercentage = tier.DailyRequestLimit > 0 
                    ? (subscription.CurrentDailyUsage / (double)tier.DailyRequestLimit) * 100 
                    : 0;
                var monthlyUsagePercentage = tier.MonthlyRequestLimit > 0 
                    ? (subscription.CurrentMonthlyUsage / (double)tier.MonthlyRequestLimit) * 100 
                    : 0;

                // Get analysis stats for this user
                var userAnalysisStats = analysisStats.ContainsKey(subscription.UserId) 
                    ? analysisStats[subscription.UserId] 
                    : null;

                var totalAnalysisCount = userAnalysisStats?.TotalCount ?? 0;
                var currentSubscriptionAnalysisCount = subscriptionAnalysisCounts.ContainsKey(subscription.Id) 
                    ? subscriptionAnalysisCounts[subscription.Id] 
                    : 0;
                var last7DaysCount = userAnalysisStats?.Last7DaysCount ?? 0;
                var last30DaysCount = userAnalysisStats?.Last30DaysCount ?? 0;
                var lastAnalysisDate = userAnalysisStats?.LastAnalysisDate;

                var averagePerDay = totalDays > 0 
                    ? currentSubscriptionAnalysisCount / (double)totalDays 
                    : 0;

                return new SubscriptionDetailDto
                {
                    // Basic Information
                    Id = subscription.Id,
                    UserId = subscription.UserId,
                    SubscriptionTierId = subscription.SubscriptionTierId,
                    TierName = tier?.TierName,
                    TierDisplayName = tier?.DisplayName,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    Status = subscription.Status,
                    IsSponsoredSubscription = subscription.IsSponsoredSubscription,
                    QueueStatus = subscription.QueueStatus,
                    AutoRenew = subscription.AutoRenew,
                    CreatedDate = subscription.CreatedDate,

                    // Limits
                    DailyRequestLimit = tier?.DailyRequestLimit ?? 0,
                    MonthlyRequestLimit = tier?.MonthlyRequestLimit ?? 0,

                    // Current Usage
                    CurrentDailyUsage = subscription.CurrentDailyUsage,
                    CurrentMonthlyUsage = subscription.CurrentMonthlyUsage,

                    // Calculated Metrics
                    RemainingDailyRequests = remainingDaily,
                    RemainingMonthlyRequests = remainingMonthly,
                    DailyUsagePercentage = Math.Round(dailyUsagePercentage, 2),
                    MonthlyUsagePercentage = Math.Round(monthlyUsagePercentage, 2),

                    // Time Metrics
                    RemainingDays = remainingDays,
                    TotalDurationDays = totalDays,
                    TimeUsagePercentage = Math.Round(timeUsagePercentage, 2),

                    // Referral Credits
                    ReferralCredits = subscription.ReferralCredits,

                    // User Information
                    User = user != null ? new UserSummaryDto
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        MobilePhones = user.MobilePhones,
                        AvatarThumbnailUrl = user.AvatarThumbnailUrl,
                        IsActive = user.IsActive,
                        RecordDate = user.RecordDate
                    } : null,

                    // Sponsor Information
                    Sponsor = sponsor != null && subscription.IsSponsoredSubscription ? new SponsorSummaryDto
                    {
                        SponsorId = sponsor.UserId,
                        SponsorName = sponsor.FullName,
                        SponsorEmail = sponsor.Email,
                        SponsorPhone = sponsor.MobilePhones
                    } : null,

                    // Analysis Statistics
                    AnalysisStats = new AnalysisStatsDto
                    {
                        TotalAnalysisCount = totalAnalysisCount,
                        CurrentSubscriptionAnalysisCount = currentSubscriptionAnalysisCount,
                        LastAnalysisDate = lastAnalysisDate,
                        Last7DaysAnalysisCount = last7DaysCount,
                        Last30DaysAnalysisCount = last30DaysCount,
                        AverageAnalysesPerDay = Math.Round(averagePerDay, 2)
                    },

                    // Queue Information
                    QueueInfo = subscription.QueueStatus == SubscriptionQueueStatus.Pending ? new QueueInfoDto
                    {
                        IsQueued = true,
                        QueuedDate = subscription.QueuedDate,
                        EstimatedActivationDate = subscription.PreviousSponsorship?.EndDate,
                        PreviousSponsorshipId = subscription.PreviousSponsorshipId,
                        PreviousSponsorshipTierName = subscription.PreviousSponsorship?.SubscriptionTier?.TierName
                    } : null,

                    // Notes
                    SponsorshipNotes = subscription.SponsorshipNotes,
                    CancellationReason = subscription.CancellationReason,
                    CancellationDate = subscription.CancellationDate
                };
            }
        }
    }

    /// <summary>
    /// Internal class for analysis statistics aggregation
    /// </summary>
    internal class UserAnalysisStats
    {
        public int UserId { get; set; }
        public int TotalCount { get; set; }
        public int Last7DaysCount { get; set; }
        public int Last30DaysCount { get; set; }
        public DateTime LastAnalysisDate { get; set; }
    }
}
