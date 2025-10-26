using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.Sponsorship.Commands
{
    /// <summary>
    /// Command: Create dealer invitation (Invite or AutoCreate type)
    /// Authorization: Sponsor role only
    /// Endpoint: POST /api/Sponsorship/dealer/invite
    /// </summary>
    public class CreateDealerInvitationCommand : IRequest<IDataResult<DealerInvitationResponseDto>>
    {
        public int SponsorId { get; set; } // Authenticated sponsor ID
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }
        public int PurchaseId { get; set; }
        public int CodeCount { get; set; }
        public string InvitationType { get; set; } // "Invite" or "AutoCreate"
    }
}
