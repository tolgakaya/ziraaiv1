using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query: Get summary of all dealers for a main sponsor
    /// Authorization: Sponsor role only
    /// Endpoint: GET /api/Sponsorship/dealer/summary
    /// </summary>
    public class GetDealerSummaryQuery : IRequest<IDataResult<DealerSummaryDto>>
    {
        public int SponsorId { get; set; } // Authenticated main sponsor ID
    }
}
