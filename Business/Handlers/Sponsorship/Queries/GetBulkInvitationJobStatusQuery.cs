using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query to get status of a specific bulk invitation job
    /// </summary>
    public class GetBulkInvitationJobStatusQuery : IRequest<IDataResult<BulkInvitationJob>>
    {
        public int JobId { get; set; }
        public int SponsorId { get; set; } // For security - only job owner can view
    }
}
