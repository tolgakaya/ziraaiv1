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
        
        /// <summary>
        /// Optional purchase ID filter. If not specified, codes from any purchase will be selected.
        /// DEPRECATED: Use PackageTier for more flexible filtering.
        /// </summary>
        public int? PurchaseId { get; set; }
        
        /// <summary>
        /// Optional tier filter for code selection: S, M, L, XL.
        /// If not specified, codes from any tier will be selected automatically.
        /// System will intelligently select codes based on expiry date (FIFO).
        /// </summary>
        public string PackageTier { get; set; }
        
        public int CodeCount { get; set; }
        public string InvitationType { get; set; } // "Invite" or "AutoCreate"
    }
}
