using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System.Collections.Generic;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query to get all bulk invitation jobs for a sponsor
    /// Sorted by CreatedDate descending (newest first)
    /// </summary>
    public class GetBulkInvitationJobHistoryQuery : IRequest<IDataResult<List<BulkInvitationJob>>>
    {
        public int SponsorId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Status { get; set; } // Optional filter: Pending, Processing, Completed, PartialSuccess, Failed
    }
}
