using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for GetBulkInvitationJobStatusQuery
    /// Returns current status and progress of bulk invitation job
    /// </summary>
    public class GetBulkInvitationJobStatusQueryHandler : IRequestHandler<GetBulkInvitationJobStatusQuery, IDataResult<BulkInvitationJob>>
    {
        private readonly IBulkInvitationJobRepository _bulkJobRepository;

        public GetBulkInvitationJobStatusQueryHandler(IBulkInvitationJobRepository bulkJobRepository)
        {
            _bulkJobRepository = bulkJobRepository;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<BulkInvitationJob>> Handle(GetBulkInvitationJobStatusQuery request, CancellationToken cancellationToken)
        {
            // Security: Verify job belongs to requesting sponsor
            var job = await _bulkJobRepository.Query()
                .Where(j => j.Id == request.JobId && j.SponsorId == request.SponsorId)
                .FirstOrDefaultAsync(cancellationToken);

            if (job == null)
            {
                return new ErrorDataResult<BulkInvitationJob>("Bulk invitation job not found or access denied");
            }

            return new SuccessDataResult<BulkInvitationJob>(job, "Bulk invitation job status retrieved successfully");
        }
    }
}
