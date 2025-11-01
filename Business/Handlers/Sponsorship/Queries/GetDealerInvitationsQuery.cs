using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query: Get list of dealer invitations for a sponsor
    /// Authorization: Sponsor role only
    /// Endpoint: GET /api/Sponsorship/dealer/invitations
    /// </summary>
    public class GetDealerInvitationsQuery : IRequest<IDataResult<List<DealerInvitationListDto>>>
    {
        public int SponsorId { get; set; } // Authenticated sponsor ID
        public string Status { get; set; } // Optional filter: Pending, Accepted, Expired, Cancelled
    }
}
