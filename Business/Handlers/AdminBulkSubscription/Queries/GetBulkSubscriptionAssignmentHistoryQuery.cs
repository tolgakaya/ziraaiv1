using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminBulkSubscription.Queries
{
    /// <summary>
    /// Query to get bulk subscription assignment job history for an admin
    /// </summary>
    public class GetBulkSubscriptionAssignmentHistoryQuery : IRequest<IDataResult<List<BulkSubscriptionAssignmentJobHistoryDto>>>
    {
        public int AdminId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetBulkSubscriptionAssignmentHistoryQueryHandler : IRequestHandler<GetBulkSubscriptionAssignmentHistoryQuery, IDataResult<List<BulkSubscriptionAssignmentJobHistoryDto>>>
    {
        private readonly IBulkSubscriptionAssignmentJobRepository _bulkJobRepository;

        public GetBulkSubscriptionAssignmentHistoryQueryHandler(IBulkSubscriptionAssignmentJobRepository bulkJobRepository)
        {
            _bulkJobRepository = bulkJobRepository;
        }

        [SecuredOperation(Priority = 1)]
        [PerformanceAspect(5)]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<List<BulkSubscriptionAssignmentJobHistoryDto>>> Handle(GetBulkSubscriptionAssignmentHistoryQuery request, CancellationToken cancellationToken)
        {
            var skip = (request.PageNumber - 1) * request.PageSize;

            var jobs = await _bulkJobRepository
                .Query()
                .Where(j => j.AdminId == request.AdminId)
                .OrderByDescending(j => j.CreatedDate)
                .Skip(skip)
                .Take(request.PageSize)
                .Select(j => new BulkSubscriptionAssignmentJobHistoryDto
                {
                    JobId = j.Id,
                    OriginalFileName = j.OriginalFileName,
                    FileSize = j.FileSize,
                    TotalFarmers = j.TotalFarmers,
                    ProcessedFarmers = j.ProcessedFarmers,
                    SuccessfulAssignments = j.SuccessfulAssignments,
                    FailedAssignments = j.FailedAssignments,
                    Status = j.Status,
                    CreatedDate = j.CreatedDate,
                    CompletedDate = j.CompletedDate
                })
                .ToListAsync(cancellationToken);

            return new SuccessDataResult<List<BulkSubscriptionAssignmentJobHistoryDto>>(jobs, "Job history retrieved successfully");
        }
    }
}
