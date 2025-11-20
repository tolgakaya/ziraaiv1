using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for getting performance analytics for a specific dealer
    /// Authorization: Sponsor role only (main sponsor can view their dealers)
    /// </summary>
    public class GetDealerPerformanceQueryHandler : IRequestHandler<GetDealerPerformanceQuery, IDataResult<DealerPerformanceDto>>
    {
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly IUserRepository _userRepository;

        public GetDealerPerformanceQueryHandler(
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IPlantAnalysisRepository plantAnalysisRepository,
            IUserRepository userRepository)
        {
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _plantAnalysisRepository = plantAnalysisRepository;
            _userRepository = userRepository;
        }

        [SecuredOperation(Priority = 1)]

        public async Task<IDataResult<DealerPerformanceDto>> Handle(GetDealerPerformanceQuery request, CancellationToken cancellationToken)
        {
            // 1. Get dealer information
            var dealer = await _userRepository.GetAsync(u => u.UserId == request.DealerId);
            if (dealer == null)
            {
                return new ErrorDataResult<DealerPerformanceDto>("Dealer not found.");
            }

            // 2. Get all codes for this sponsor
            var sponsorCodes = await _sponsorshipCodeRepository.GetBySponsorIdAsync(request.UserId);
            
            // 3. Filter codes that were transferred to this dealer
            var dealerCodes = sponsorCodes.Where(c => c.DealerId == request.DealerId).ToList();

            // 4. Calculate statistics
            var totalCodesReceived = dealerCodes.Count(c => c.TransferredAt.HasValue);
            var codesSent = dealerCodes.Count(c => c.DistributionDate.HasValue);
            var codesUsed = dealerCodes.Count(c => c.IsUsed);
            var codesAvailable = dealerCodes.Count(c => !c.IsUsed && c.IsActive && c.ExpiryDate > DateTime.Now && !c.DistributionDate.HasValue);

            // 5. Get plant analyses from dealer's codes
            var dealerCodeIds = dealerCodes.Select(c => c.Id).ToList();
            var allAnalyses = await _plantAnalysisRepository.GetListAsync();
            var dealerAnalyses = allAnalyses.Where(a => a.DealerId == request.DealerId).ToList();

            var uniqueFarmers = dealerAnalyses.Select(a => a.UserId).Distinct().Count();
            var totalAnalyses = dealerAnalyses.Count;

            // 6. Calculate usage rate
            var usageRate = codesSent > 0 ? (decimal)codesUsed / codesSent * 100 : 0;

            // 7. Get transfer dates
            var firstTransfer = dealerCodes.Where(c => c.TransferredAt.HasValue)
                                          .OrderBy(c => c.TransferredAt)
                                          .FirstOrDefault()?.TransferredAt;
            
            var lastTransfer = dealerCodes.Where(c => c.TransferredAt.HasValue)
                                         .OrderByDescending(c => c.TransferredAt)
                                         .FirstOrDefault()?.TransferredAt;

            // 8. Build response
            var performance = new DealerPerformanceDto
            {
                DealerId = request.DealerId,
                DealerName = dealer.FullName ?? "",
                DealerEmail = dealer.Email,
                TotalCodesReceived = totalCodesReceived,
                CodesSent = codesSent,
                CodesUsed = codesUsed,
                CodesAvailable = codesAvailable,
                UsageRate = Math.Round(usageRate, 2),
                UniqueFarmersReached = uniqueFarmers,
                TotalAnalyses = totalAnalyses,
                FirstTransferDate = firstTransfer,
                LastTransferDate = lastTransfer
            };

            return new SuccessDataResult<DealerPerformanceDto>(performance);
        }
    }
}
