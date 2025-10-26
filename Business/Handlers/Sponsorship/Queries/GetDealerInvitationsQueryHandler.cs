using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for getting list of dealer invitations for a sponsor
    /// Authorization: Sponsor role only
    /// </summary>
    public class GetDealerInvitationsQueryHandler : IRequestHandler<GetDealerInvitationsQuery, IDataResult<List<DealerInvitationListDto>>>
    {
        private readonly IDealerInvitationRepository _dealerInvitationRepository;

        public GetDealerInvitationsQueryHandler(IDealerInvitationRepository dealerInvitationRepository)
        {
            _dealerInvitationRepository = dealerInvitationRepository;
        }

        public async Task<IDataResult<List<DealerInvitationListDto>>> Handle(GetDealerInvitationsQuery request, CancellationToken cancellationToken)
        {
            // Get all invitations for this sponsor
            var invitations = await _dealerInvitationRepository.GetListAsync(i => i.SponsorId == request.SponsorId);

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                invitations = invitations.Where(i => i.Status == request.Status).ToList();
            }

            // Map to DTO
            var invitationDtos = invitations.Select(i => new DealerInvitationListDto
            {
                Id = i.Id,
                Email = i.Email,
                Phone = i.Phone,
                DealerName = i.DealerName,
                Status = i.Status,
                InvitationType = i.InvitationType,
                CodeCount = i.CodeCount,
                CreatedDealerId = i.CreatedDealerId,
                AcceptedDate = i.AcceptedDate,
                CreatedAt = i.CreatedDate,
                ExpiresAt = i.ExpiryDate
            })
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

            return new SuccessDataResult<List<DealerInvitationListDto>>(invitationDtos);
        }
    }
}
