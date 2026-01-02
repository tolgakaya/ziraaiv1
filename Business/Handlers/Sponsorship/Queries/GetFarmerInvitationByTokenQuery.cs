using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query: Get farmer invitation details by token
    /// Authorization: AllowAnonymous (for unregistered users)
    /// Endpoint: GET /api/Sponsorship/farmer/invitations/{token}
    /// </summary>
    public class GetFarmerInvitationByTokenQuery : IRequest<IDataResult<FarmerInvitationDetailDto>>
    {
        public string InvitationToken { get; set; }
    }
}
