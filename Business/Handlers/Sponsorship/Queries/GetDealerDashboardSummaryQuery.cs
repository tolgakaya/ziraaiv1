using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get quick dashboard summary for dealer
    /// Optimized for fast loading with minimal queries
    /// </summary>
    public class GetDealerDashboardSummaryQuery : IRequest<IDataResult<DealerDashboardSummaryDto>>
    {
        public int DealerId { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }

        public class GetDealerDashboardSummaryQueryHandler : IRequestHandler<GetDealerDashboardSummaryQuery, IDataResult<DealerDashboardSummaryDto>>
        {
            private readonly IDealerDashboardCacheService _cacheService;
            private readonly ILogger<GetDealerDashboardSummaryQueryHandler> _logger;

            public GetDealerDashboardSummaryQueryHandler(
                IDealerDashboardCacheService cacheService,
                ILogger<GetDealerDashboardSummaryQueryHandler> logger)
            {
                _cacheService = cacheService;
                _logger = logger;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<DealerDashboardSummaryDto>> Handle(
                GetDealerDashboardSummaryQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🔍 Fetching dashboard summary for dealer {DealerId} (cache-enabled)", request.DealerId);

                    // Use cache service with cache-first pattern
                    // Cache hit: 10-30ms, Cache miss: 500-1200ms (then cached for 15min default)
                    var summary = await _cacheService.GetDashboardSummaryAsync(request.DealerId);

                    _logger.LogInformation("✅ Dashboard summary: Total={Total}, Available={Available}, Sent={Sent}, Used={Used}, Pending={Pending}",
                        summary.TotalCodesReceived, summary.CodesAvailable, summary.CodesSent, summary.CodesUsed, summary.PendingInvitationsCount);

                    return new SuccessDataResult<DealerDashboardSummaryDto>(
                        summary,
                        "Dashboard summary retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error retrieving dashboard summary for dealer {DealerId}", request.DealerId);
                    return new ErrorDataResult<DealerDashboardSummaryDto>(
                        "Dashboard özeti alınırken hata oluştu");
                }
            }
        }
    }
}
