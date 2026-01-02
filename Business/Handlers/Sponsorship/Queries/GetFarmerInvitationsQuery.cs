using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query: Get list of farmer invitations for a sponsor
    /// Authorization: Sponsor role only
    /// Endpoint: GET /api/Sponsorship/farmer/invitations
    /// </summary>
    public class GetFarmerInvitationsQuery : IRequest<IDataResult<List<FarmerInvitationListDto>>>
    {
        public int SponsorId { get; set; } // Authenticated sponsor ID
        public string Status { get; set; } // Optional filter: Pending, Accepted, Expired, Cancelled
    }
}
