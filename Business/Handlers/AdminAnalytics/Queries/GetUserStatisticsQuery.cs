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

namespace Business.Handlers.AdminAnalytics.Queries
{
    /// <summary>
    /// Admin query to get user statistics and metrics
    /// </summary>
    public class GetUserStatisticsQuery : IRequest<IDataResult<UserStatisticsDto>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetUserStatisticsQueryHandler : IRequestHandler<GetUserStatisticsQuery, IDataResult<UserStatisticsDto>>
        {
            private readonly IAdminStatisticsCacheService _cacheService;

            public GetUserStatisticsQueryHandler(IAdminStatisticsCacheService cacheService)
            {
                _cacheService = cacheService;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<UserStatisticsDto>> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
            {
                // Use cache service with cache-first pattern
                // Cache hit: 20-50ms, Cache miss: 800-2000ms (then cached)
                var stats = await _cacheService.GetUserStatisticsAsync(request.StartDate, request.EndDate);

                return new SuccessDataResult<UserStatisticsDto>(stats, "User statistics retrieved successfully");
            }
        }
    }

    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int FarmerUsers { get; set; }
        public int SponsorUsers { get; set; }
        public int AdminUsers { get; set; }
        public int UsersRegisteredToday { get; set; }
        public int UsersRegisteredThisWeek { get; set; }
        public int UsersRegisteredThisMonth { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
