using Core.Utilities.Results;
using MediatR;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// DTO for dealer search result
    /// </summary>
    public class DealerSearchResultDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public bool IsSponsor { get; set; }
    }

    /// <summary>
    /// Query: Search for existing sponsor/dealer by email (Method A: Manual search)
    /// Authorization: Sponsor role only
    /// Endpoint: GET /api/Sponsorship/dealer/search?email={email}
    /// </summary>
    public class SearchDealerByEmailQuery : IRequest<IDataResult<DealerSearchResultDto>>
    {
        public string Email { get; set; }
    }
}
