using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    /// <summary>
    /// Handler for transferring sponsorship codes from main sponsor to dealer
    /// Authorization: Sponsor role only
    /// </summary>
    public class TransferCodesToDealerCommandHandler : IRequestHandler<TransferCodesToDealerCommand, IDataResult<DealerCodeTransferResponseDto>>
    {
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IUserRepository _userRepository;

        public TransferCodesToDealerCommandHandler(
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IUserRepository userRepository)
        {
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _userRepository = userRepository;
        }

        public async Task<IDataResult<DealerCodeTransferResponseDto>> Handle(TransferCodesToDealerCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate dealer exists and is a sponsor
            var dealer = await _userRepository.GetAsync(u => u.UserId == request.DealerId);
            if (dealer == null)
            {
                return new ErrorDataResult<DealerCodeTransferResponseDto>("Dealer user not found.");
            }

            // Check if dealer has Sponsor role
            var dealerGroups = await _userRepository.GetUserGroupsAsync(request.DealerId);
            if (!dealerGroups.Any(g => g == "Sponsor"))
            {
                return new ErrorDataResult<DealerCodeTransferResponseDto>("Dealer must have Sponsor role to receive codes.");
            }

            // 2. Get unused codes from the purchase that belong to the main sponsor
            var allPurchaseCodes = await _sponsorshipCodeRepository.GetByPurchaseIdAsync(request.PurchaseId);
            
            // Filter: unused, active, not expired, not yet transferred to dealer, belongs to requesting sponsor
            var availableCodes = allPurchaseCodes
                .Where(c => c.SponsorId == request.UserId 
                         && !c.IsUsed 
                         && c.IsActive 
                         && c.ExpiryDate > DateTime.Now
                         && c.DealerId == null)
                .OrderBy(c => c.CreatedDate)
                .Take(request.CodeCount)
                .ToList();

            if (availableCodes.Count < request.CodeCount)
            {
                return new ErrorDataResult<DealerCodeTransferResponseDto>(
                    $"Not enough available codes. Requested: {request.CodeCount}, Available: {availableCodes.Count}");
            }

            // 3. Transfer codes to dealer
            var transferredCodeIds = new System.Collections.Generic.List<int>();
            var transferTime = DateTime.Now;

            foreach (var code in availableCodes)
            {
                code.DealerId = request.DealerId;
                code.TransferredAt = transferTime;
                code.TransferredByUserId = request.UserId;
                
                _sponsorshipCodeRepository.Update(code);
                transferredCodeIds.Add(code.Id);
            }

            // Save changes
            await _sponsorshipCodeRepository.SaveChangesAsync();

            // 4. Return response
            var response = new DealerCodeTransferResponseDto
            {
                TransferredCodeIds = transferredCodeIds,
                TransferredCount = transferredCodeIds.Count,
                DealerId = request.DealerId,
                DealerName = dealer.FullName ?? "",
                TransferredAt = transferTime
            };

            return new SuccessDataResult<DealerCodeTransferResponseDto>(
                response, 
                $"Successfully transferred {response.TransferredCount} codes to dealer.");
        }
    }
}
