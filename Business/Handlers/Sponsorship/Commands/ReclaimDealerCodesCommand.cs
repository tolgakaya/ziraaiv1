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
        public int UserId { get; set; } // Authenticated main sponsor ID
        public int DealerId { get; set; }
        public List<int> CodeIds { get; set; } // Optional - if empty, reclaim all unused
        public string ReclaimReason { get; set; }
    }
}
