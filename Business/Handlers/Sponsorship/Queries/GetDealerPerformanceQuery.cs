using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query: Get performance analytics for a specific dealer
    /// Authorization: Sponsor role only (main sponsors can view their dealers)
    /// Endpoint: GET /api/Sponsorship/dealer/analytics/{dealerId}
    /// </summary>
    public class GetDealerPerformanceQuery : IRequest<IDataResult<DealerPerformanceDto>>
    {
        public int UserId { get; set; } // Authenticated main sponsor ID
        public int DealerId { get; set; }
    }
}
