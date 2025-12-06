using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAnalytics;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using MediatR;

namespace Business.Handlers.AdminAnalytics.Commands
{
    /// <summary>
    /// Admin command to rebuild all admin statistics caches
    /// Used for manual cache warming or after system maintenance
    /// </summary>
    public class RebuildAdminCacheCommand : IRequest<IResult>
    {
        public class RebuildAdminCacheCommandHandler : IRequestHandler<RebuildAdminCacheCommand, IResult>
        {
            private readonly IAdminStatisticsCacheService _cacheService;

            public RebuildAdminCacheCommandHandler(IAdminStatisticsCacheService cacheService)
            {
                _cacheService = cacheService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RebuildAdminCacheCommand request, CancellationToken cancellationToken)
            {
                await _cacheService.RebuildAllCachesAsync();

                return new SuccessResult("Admin statistics caches rebuilt successfully");
            }
        }
    }
}
