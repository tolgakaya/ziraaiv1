using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminBulkSubscription.Queries
{
    /// <summary>
    /// Query to get bulk subscription assignment job result file URL
    /// </summary>
    public class GetBulkSubscriptionAssignmentResultQuery : IRequest<IDataResult<string>>
    {
        public int JobId { get; set; }
        public int AdminId { get; set; }
    }

    public class GetBulkSubscriptionAssignmentResultQueryHandler : IRequestHandler<GetBulkSubscriptionAssignmentResultQuery, IDataResult<string>>
    {
        private readonly IBulkSubscriptionAssignmentJobRepository _bulkJobRepository;

        public GetBulkSubscriptionAssignmentResultQueryHandler(IBulkSubscriptionAssignmentJobRepository bulkJobRepository)
        {
            _bulkJobRepository = bulkJobRepository;
        }

        [SecuredOperation(Priority = 1)]
        [PerformanceAspect(5)]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<string>> Handle(GetBulkSubscriptionAssignmentResultQuery request, CancellationToken cancellationToken)
        {
            var job = await _bulkJobRepository.GetAsync(j => j.Id == request.JobId && j.AdminId == request.AdminId);

            if (job == null)
            {
                return new ErrorDataResult<string>("Job not found or you don't have permission to view it");
            }

            if (string.IsNullOrWhiteSpace(job.ResultFileUrl))
            {
                return new ErrorDataResult<string>("Result file is not available yet. Job may still be processing.");
            }

            return new SuccessDataResult<string>(job.ResultFileUrl, "Result file URL retrieved successfully");
        }
    }
}
