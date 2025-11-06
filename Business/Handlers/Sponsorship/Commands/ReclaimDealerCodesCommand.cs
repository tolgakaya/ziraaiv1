using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Business.Handlers.Sponsorship.Commands
{
    /// <summary>
    /// Command: Reclaim unused codes from dealer back to main sponsor
    /// Authorization: Sponsor role only (main sponsors)
    /// Endpoint: POST /api/Sponsorship/dealer/reclaim-codes
    /// </summary>
    public class ReclaimDealerCodesCommand : IRequest<IDataResult<ReclaimCodesResponseDto>>
    {
        /// <summary>
        /// Main sponsor user ID (set from authentication token)
        /// </summary>
        public int UserId { get; set; }
        
        /// <summary>
        /// Dealer ID to reclaim codes from
        /// </summary>
        public int DealerId { get; set; }
        
        /// <summary>
        /// Reason for reclaiming codes (audit trail)
        /// </summary>
        public string ReclaimReason { get; set; }
    }
}
