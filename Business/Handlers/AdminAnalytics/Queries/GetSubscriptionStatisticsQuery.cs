using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminAnalytics.Queries
{
    /// <summary>
    /// Admin query to get subscription statistics and metrics
    /// </summary>
    public class GetSubscriptionStatisticsQuery : IRequest<IDataResult<SubscriptionStatisticsDto>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetSubscriptionStatisticsQueryHandler : IRequestHandler<GetSubscriptionStatisticsQuery, IDataResult<SubscriptionStatisticsDto>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly ISubscriptionTierRepository _tierRepository;

            public GetSubscriptionStatisticsQueryHandler(
                IUserSubscriptionRepository subscriptionRepository,
                ISubscriptionTierRepository tierRepository)
            {
                _subscriptionRepository = subscriptionRepository;
                _tierRepository = tierRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SubscriptionStatisticsDto>> Handle(GetSubscriptionStatisticsQuery request, CancellationToken cancellationToken)
            {
                var allSubscriptions = _subscriptionRepository.Query()
                    .Include(s => s.SubscriptionTier);

                // Apply date filters if provided
                var query = allSubscriptions.AsQueryable();
                if (request.StartDate.HasValue)
                {
                    query = query.Where(s => s.CreatedDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(s => s.CreatedDate <= request.EndDate.Value);
                }

                var subscriptionsList = await query.ToListAsync(cancellationToken);

                var stats = new SubscriptionStatisticsDto
                {
                    TotalSubscriptions = subscriptionsList.Count,
                    ActiveSubscriptions = subscriptionsList.Count(s => s.IsActive && s.Status == "Active"),
                    ExpiredSubscriptions = subscriptionsList.Count(s => s.EndDate < DateTime.Now),
                    TrialSubscriptions = subscriptionsList.Count(s => s.IsTrialSubscription),
                    SponsoredSubscriptions = subscriptionsList.Count(s => s.IsSponsoredSubscription),
                    PaidSubscriptions = subscriptionsList.Count(s => !s.IsTrialSubscription && !s.IsSponsoredSubscription),
                    SubscriptionsByTier = subscriptionsList
                        .GroupBy(s => s.SubscriptionTier.TierName)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TotalRevenue = subscriptionsList
                        .Where(s => !s.IsTrialSubscription && !s.IsSponsoredSubscription)
                        .Sum(s => s.SubscriptionTier.MonthlyPrice * 
                            ((s.EndDate - s.StartDate).Days / 30.0m)),
                    AverageSubscriptionDuration = subscriptionsList.Any() 
                        ? subscriptionsList.Average(s => (s.EndDate - s.StartDate).Days) 
                        : 0,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GeneratedAt = DateTime.Now
                };

                return new SuccessDataResult<SubscriptionStatisticsDto>(stats, "Subscription statistics retrieved successfully");
            }
        }
    }

    public class SubscriptionStatisticsDto
    {
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public int SponsoredSubscriptions { get; set; }
        public int PaidSubscriptions { get; set; }
        public System.Collections.Generic.Dictionary<string, int> SubscriptionsByTier { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageSubscriptionDuration { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
