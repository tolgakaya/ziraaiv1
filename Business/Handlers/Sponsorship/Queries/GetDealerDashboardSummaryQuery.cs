using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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

        public class GetDealerDashboardSummaryQueryHandler : IRequestHandler<GetDealerDashboardSummaryQuery, IDataResult<DealerDashboardSummaryDto>>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IDealerInvitationRepository _invitationRepository;
            private readonly ILogger<GetDealerDashboardSummaryQueryHandler> _logger;

            public GetDealerDashboardSummaryQueryHandler(
                ISponsorshipCodeRepository codeRepository,
                IDealerInvitationRepository invitationRepository,
                ILogger<GetDealerDashboardSummaryQueryHandler> logger)
            {
                _codeRepository = codeRepository;
                _invitationRepository = invitationRepository;
                _logger = logger;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<DealerDashboardSummaryDto>> Handle(
                GetDealerDashboardSummaryQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🔍 Fetching dashboard summary for dealer {DealerId}", request.DealerId);

                    // Single query to get all dealer codes (not reclaimed)
                    // Performance: Uses index on DealerId + ReclaimedAt
                    var dealerCodes = await _codeRepository.Query()
                        .Where(c => c.DealerId == request.DealerId && c.ReclaimedAt == null)
                        .Select(c => new
                        {
                            c.IsUsed,
                            c.DistributionDate,
                            c.ExpiryDate,
                            c.IsActive
                        })
                        .ToListAsync(cancellationToken);

                    // Calculate statistics in-memory (already loaded)
                    var now = DateTime.Now;
                    var totalReceived = dealerCodes.Count;
                    var codesSent = dealerCodes.Count(c => c.DistributionDate.HasValue);
                    var codesUsed = dealerCodes.Count(c => c.IsUsed);
                    var codesAvailable = dealerCodes.Count(c => !c.IsUsed &&
                                                                 c.DistributionDate == null &&
                                                                 c.ExpiryDate > now &&
                                                                 c.IsActive);

                    var usageRate = codesSent > 0
                        ? Math.Round((decimal)codesUsed / codesSent * 100, 2)
                        : 0;

                    // Get pending invitations count (separate optimized query)
                    var pendingCount = await _invitationRepository.Query()
                        .Where(i => (i.Email == request.DealerId.ToString() || i.Phone == request.DealerId.ToString()) &&
                                    i.Status == "Pending" &&
                                    i.ExpiryDate > now)
                        .CountAsync(cancellationToken);

                    var summary = new DealerDashboardSummaryDto
                    {
                        TotalCodesReceived = totalReceived,
                        CodesSent = codesSent,
                        CodesUsed = codesUsed,
                        CodesAvailable = codesAvailable,
                        UsageRate = usageRate,
                        PendingInvitationsCount = pendingCount
                    };

                    _logger.LogInformation("✅ Dashboard summary: Total={Total}, Available={Available}, Sent={Sent}, Used={Used}, Pending={Pending}",
                        totalReceived, codesAvailable, codesSent, codesUsed, pendingCount);

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
