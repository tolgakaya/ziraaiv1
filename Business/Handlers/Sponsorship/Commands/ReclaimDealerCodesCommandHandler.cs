using Business.BusinessAspects;
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
    /// Handler for reclaiming unused codes from dealer back to main sponsor
    /// Authorization: Sponsor role only
    /// </summary>
    public class ReclaimDealerCodesCommandHandler : IRequestHandler<ReclaimDealerCodesCommand, IDataResult<ReclaimCodesResponseDto>>
    {
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly IDealerInvitationRepository _invitationRepository;

        public ReclaimDealerCodesCommandHandler(
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            IDealerInvitationRepository invitationRepository)
        {
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _invitationRepository = invitationRepository;
        }

        [SecuredOperation(Priority = 1)]

        public async Task<IDataResult<ReclaimCodesResponseDto>> Handle(ReclaimDealerCodesCommand request, CancellationToken cancellationToken)
        {
            // 1. Get dealer's codes that belong to this sponsor
            var dealerCodes = await _sponsorshipCodeRepository.GetBySponsorIdAsync(request.UserId);

            // Filter codes that:
            // - Are currently assigned to the specified dealer
            // - Are not used
            // - Are active
            // - Not expired
            var reclaimableCodes = dealerCodes
                .Where(c => c.DealerId == request.DealerId
                         && !c.IsUsed
                         && c.IsActive
                         && c.ExpiryDate > DateTime.Now)
                .ToList();

            // Reclaim ALL unused codes from dealer (no selective reclaim)

            if (!reclaimableCodes.Any())
            {
                return new ErrorDataResult<ReclaimCodesResponseDto>("No codes available to reclaim from this dealer.");
            }

            // 2. Reclaim codes (set DealerId back to null)
            var reclaimTime = DateTime.Now;
            var reclaimedCodeIds = new System.Collections.Generic.List<int>();

            foreach (var code in reclaimableCodes)
            {
                code.DealerId = null; // Remove dealer assignment
                code.ReclaimedAt = reclaimTime;
                code.ReclaimedByUserId = request.UserId;

                _sponsorshipCodeRepository.Update(code);
                reclaimedCodeIds.Add(code.Id);
            }
            await _sponsorshipCodeRepository.SaveChangesAsync();

            // 3. Update dealer invitation status to "Reclaimed"
            // Find the accepted invitation for this dealer from this sponsor
            var invitation = await _invitationRepository.GetAsync(inv =>
                inv.SponsorId == request.UserId
                && inv.CreatedDealerId == request.DealerId
                && inv.Status == "Accepted");

            if (invitation != null)
            {
                invitation.Status = "Reclaimed";
                invitation.Notes = string.IsNullOrEmpty(invitation.Notes)
                    ? $"Codes reclaimed on {reclaimTime:yyyy-MM-dd HH:mm}: {request.ReclaimReason}"
                    : invitation.Notes + $"\nCodes reclaimed on {reclaimTime:yyyy-MM-dd HH:mm}: {request.ReclaimReason}";

                _invitationRepository.Update(invitation);
                await _invitationRepository.SaveChangesAsync();
            }

            // 4. Return response
            var response = new ReclaimCodesResponseDto
            {
                ReclaimedCount = reclaimedCodeIds.Count,
                ReclaimedCodeIds = reclaimedCodeIds,
                DealerId = request.DealerId,
                ReclaimedAt = reclaimTime
            };

            return new SuccessDataResult<ReclaimCodesResponseDto>(
                response,
                $"Successfully reclaimed {response.ReclaimedCount} codes from dealer.");
        }
    }
}
