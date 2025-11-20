using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for GetBulkInvitationJobHistoryQuery
    /// Returns paginated list of bulk invitation jobs for sponsor
    /// </summary>
    public class GetBulkInvitationJobHistoryQueryHandler : IRequestHandler<GetBulkInvitationJobHistoryQuery, IDataResult<List<BulkInvitationJob>>>
    {
        private readonly IBulkInvitationJobRepository _bulkJobRepository;

        public GetBulkInvitationJobHistoryQueryHandler(IBulkInvitationJobRepository bulkJobRepository)
        {
            _bulkJobRepository = bulkJobRepository;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<List<BulkInvitationJob>>> Handle(GetBulkInvitationJobHistoryQuery request, CancellationToken cancellationToken)
        {
            // Build query with sponsor filter
            var query = _bulkJobRepository.Query()
                .Where(j => j.SponsorId == request.SponsorId);

            // Apply optional status filter
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(j => j.Status == request.Status);
            }

            // Order by created date descending (newest first)
            query = query.OrderByDescending(j => j.CreatedDate);

            // Apply pagination
            var jobs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new SuccessDataResult<List<BulkInvitationJob>>(
                jobs,
                $"Retrieved {jobs.Count} bulk invitation jobs"
            );
        }
    }
}
