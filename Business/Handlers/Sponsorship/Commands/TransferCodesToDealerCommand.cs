using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.Sponsorship.Commands
{
    /// <summary>
    /// Command: Transfer sponsorship codes from main sponsor to dealer
    /// Authorization: Sponsor role only (main sponsors)
    /// Endpoint: POST /api/Sponsorship/dealer/transfer-codes
    /// </summary>
    /// <summary>
    /// Command: Transfer sponsorship codes from main sponsor to dealer
    /// Authorization: Sponsor role only (main sponsors)
    /// Endpoint: POST /api/Sponsorship/dealer/transfer-codes
    /// </summary>
    public class TransferCodesToDealerCommand : IRequest<IDataResult<DealerCodeTransferResponseDto>>
    {
        public int UserId { get; set; } // Authenticated main sponsor ID
        public int DealerId { get; set; }
        
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
        public string TransferNote { get; set; }
    }
}
