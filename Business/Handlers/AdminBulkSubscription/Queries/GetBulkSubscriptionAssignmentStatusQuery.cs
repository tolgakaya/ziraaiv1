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

namespace Business.Handlers.AdminBulkSubscription.Queries
{
    /// <summary>
    /// Query to get bulk subscription assignment job status and progress
    /// </summary>
    public class GetBulkSubscriptionAssignmentStatusQuery : IRequest<IDataResult<BulkSubscriptionAssignmentProgressDto>>
    {
        public int JobId { get; set; }
        public int AdminId { get; set; }
    }

    public class GetBulkSubscriptionAssignmentStatusQueryHandler : IRequestHandler<GetBulkSubscriptionAssignmentStatusQuery, IDataResult<BulkSubscriptionAssignmentProgressDto>>
    {
        private readonly IBulkSubscriptionAssignmentJobRepository _bulkJobRepository;

        public GetBulkSubscriptionAssignmentStatusQueryHandler(IBulkSubscriptionAssignmentJobRepository bulkJobRepository)
        {
            _bulkJobRepository = bulkJobRepository;
        }

        [SecuredOperation(Priority = 1)]
        [PerformanceAspect(5)]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<BulkSubscriptionAssignmentProgressDto>> Handle(GetBulkSubscriptionAssignmentStatusQuery request, CancellationToken cancellationToken)
        {
            var job = await _bulkJobRepository.GetAsync(j => j.Id == request.JobId && j.AdminId == request.AdminId);

            if (job == null)
            {
                return new ErrorDataResult<BulkSubscriptionAssignmentProgressDto>("Job not found or you don't have permission to view it");
            }

            var progressDto = new BulkSubscriptionAssignmentProgressDto
            {
                JobId = job.Id,
                Status = job.Status,
                TotalFarmers = job.TotalFarmers,
                ProcessedFarmers = job.ProcessedFarmers,
                SuccessfulAssignments = job.SuccessfulAssignments,
                FailedAssignments = job.FailedAssignments,
                NewSubscriptionsCreated = job.NewSubscriptionsCreated,
                ExistingSubscriptionsUpdated = job.ExistingSubscriptionsUpdated,
                TotalNotificationsSent = job.TotalNotificationsSent,
                CreatedDate = job.CreatedDate,
                StartedDate = job.StartedDate,
                CompletedDate = job.CompletedDate,
                ResultFileUrl = job.ResultFileUrl
            };

            return new SuccessDataResult<BulkSubscriptionAssignmentProgressDto>(progressDto, "Job status retrieved successfully");
        }
    }
}
