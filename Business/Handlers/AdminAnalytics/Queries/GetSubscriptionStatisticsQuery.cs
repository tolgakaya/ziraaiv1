using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAnalytics;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
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
            private readonly IAdminStatisticsCacheService _cacheService;

            public GetSubscriptionStatisticsQueryHandler(IAdminStatisticsCacheService cacheService)
            {
                _cacheService = cacheService;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SubscriptionStatisticsDto>> Handle(GetSubscriptionStatisticsQuery request, CancellationToken cancellationToken)
            {
                // Use cache service with cache-first pattern
                // Cache hit: 15-40ms, Cache miss: 600-1500ms (then cached)
                var stats = await _cacheService.GetSubscriptionStatisticsAsync(request.StartDate, request.EndDate);

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
