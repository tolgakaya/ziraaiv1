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
    public class TransferCodesToDealerCommand : IRequest<IDataResult<DealerCodeTransferResponseDto>>
    {
        public int UserId { get; set; } // Authenticated main sponsor ID
        public int DealerId { get; set; }
        public int PurchaseId { get; set; }
        public int CodeCount { get; set; }
        public string TransferNote { get; set; }
    }
}
