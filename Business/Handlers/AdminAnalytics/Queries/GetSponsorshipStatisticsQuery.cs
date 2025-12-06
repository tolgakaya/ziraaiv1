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
    /// Admin query to get sponsorship statistics and metrics
    /// </summary>
    public class GetSponsorshipStatisticsQuery : IRequest<IDataResult<SponsorshipStatisticsDto>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetSponsorshipStatisticsQueryHandler : IRequestHandler<GetSponsorshipStatisticsQuery, IDataResult<SponsorshipStatisticsDto>>
        {
            private readonly IAdminStatisticsCacheService _cacheService;

            public GetSponsorshipStatisticsQueryHandler(IAdminStatisticsCacheService cacheService)
            {
                _cacheService = cacheService;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorshipStatisticsDto>> Handle(GetSponsorshipStatisticsQuery request, CancellationToken cancellationToken)
            {
                // Use cache service with cache-first pattern
                // Cache hit: 10-30ms, Cache miss: 500-1200ms (then cached)
                var stats = await _cacheService.GetSponsorshipStatisticsAsync(request.StartDate, request.EndDate);

                return new SuccessDataResult<SponsorshipStatisticsDto>(stats, "Sponsorship statistics retrieved successfully");
            }
        }
    }

    public class SponsorshipStatisticsDto
    {
        public int TotalPurchases { get; set; }
        public int CompletedPurchases { get; set; }
        public int PendingPurchases { get; set; }
        public int RefundedPurchases { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCodesGenerated { get; set; }
        public int TotalCodesUsed { get; set; }
        public int TotalCodesActive { get; set; }
        public int TotalCodesExpired { get; set; }
        public double CodeRedemptionRate { get; set; }
        public decimal AveragePurchaseAmount { get; set; }
        public int TotalQuantityPurchased { get; set; }
        public int UniqueSponsorCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
