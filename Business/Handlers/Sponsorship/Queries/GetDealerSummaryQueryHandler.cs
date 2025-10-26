using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for getting summary of all dealers for a sponsor
    /// Authorization: Sponsor role only
    /// </summary>
    public class GetDealerSummaryQueryHandler : IRequestHandler<GetDealerSummaryQuery, IDataResult<DealerSummaryDto>>
    {
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly IUserRepository _userRepository;

        public GetDealerSummaryQueryHandler(
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IPlantAnalysisRepository plantAnalysisRepository,
            IUserRepository userRepository)
        {
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _plantAnalysisRepository = plantAnalysisRepository;
            _userRepository = userRepository;
        }

        public async Task<IDataResult<DealerSummaryDto>> Handle(GetDealerSummaryQuery request, CancellationToken cancellationToken)
        {
            // 1. Get all codes for this sponsor
            var sponsorCodes = await _sponsorshipCodeRepository.GetBySponsorIdAsync(request.SponsorId);

            // 2. Get unique dealer IDs
            var dealerIds = sponsorCodes
                .Where(c => c.DealerId.HasValue || (c.TransferredAt.HasValue && c.ReclaimedAt.HasValue))
                .Select(c => c.DealerId)
                .Where(d => d.HasValue)
                .Distinct()
                .Select(d => d.Value)
                .ToList();

            // 3. Get all analyses
            var allAnalyses = await _plantAnalysisRepository.GetListAsync();

            // 4. Build dealer performance list
            var dealerPerformances = new List<DealerPerformanceDto>();

            foreach (var dealerId in dealerIds)
            {
                var dealer = await _userRepository.GetAsync(u => u.UserId == dealerId);
                if (dealer == null) continue;

                // Get codes for this dealer
                var dealerCodes = sponsorCodes.Where(c => c.DealerId == dealerId || 
                                                         (c.DealerId == null && c.TransferredAt.HasValue && c.ReclaimedAt.HasValue))
                                             .ToList();

                var currentDealerCodes = dealerCodes.Where(c => c.DealerId == dealerId).ToList();

                // Calculate stats
                var totalReceived = dealerCodes.Count(c => c.TransferredAt.HasValue);
                var sent = currentDealerCodes.Count(c => c.DistributionDate.HasValue);
                var used = currentDealerCodes.Count(c => c.IsUsed);
                var available = currentDealerCodes.Count(c => !c.IsUsed && c.IsActive && c.ExpiryDate > DateTime.Now && !c.DistributionDate.HasValue);
                var reclaimed = dealerCodes.Count(c => c.ReclaimedAt.HasValue);

                var dealerAnalyses = allAnalyses.Where(a => a.DealerId == dealerId).ToList();
                var uniqueFarmers = dealerAnalyses.Select(a => a.UserId).Distinct().Count();
                var totalAnalyses = dealerAnalyses.Count;

                var usageRate = sent > 0 ? (decimal)used / sent * 100 : 0;

                var firstTransfer = dealerCodes.Where(c => c.TransferredAt.HasValue)
                                              .OrderBy(c => c.TransferredAt)
                                              .FirstOrDefault()?.TransferredAt;
                
                var lastTransfer = dealerCodes.Where(c => c.TransferredAt.HasValue)
                                             .OrderByDescending(c => c.TransferredAt)
                                             .FirstOrDefault()?.TransferredAt;

                dealerPerformances.Add(new DealerPerformanceDto
                {
                    DealerId = dealerId,
                    DealerName = dealer.FullName ?? "",
                    DealerEmail = dealer.Email,
                    TotalCodesReceived = totalReceived,
                    CodesSent = sent,
                    CodesUsed = used,
                    CodesAvailable = available,
                    CodesReclaimed = reclaimed,
                    UsageRate = Math.Round(usageRate, 2),
                    UniqueFarmersReached = uniqueFarmers,
                    TotalAnalyses = totalAnalyses,
                    FirstTransferDate = firstTransfer,
                    LastTransferDate = lastTransfer
                });
            }

            // 5. Calculate overall summary
            var summary = new DealerSummaryDto
            {
                TotalDealers = dealerPerformances.Count,
                TotalCodesDistributed = dealerPerformances.Sum(d => d.TotalCodesReceived),
                TotalCodesUsed = dealerPerformances.Sum(d => d.CodesUsed),
                TotalCodesAvailable = dealerPerformances.Sum(d => d.CodesAvailable),
                TotalCodesReclaimed = dealerPerformances.Sum(d => d.CodesReclaimed),
                OverallUsageRate = dealerPerformances.Sum(d => d.CodesSent) > 0
                    ? Math.Round((decimal)dealerPerformances.Sum(d => d.CodesUsed) / dealerPerformances.Sum(d => d.CodesSent) * 100, 2)
                    : 0,
                Dealers = dealerPerformances.OrderByDescending(d => d.TotalCodesReceived).ToList()
            };

            return new SuccessDataResult<DealerSummaryDto>(summary);
        }
    }
}
