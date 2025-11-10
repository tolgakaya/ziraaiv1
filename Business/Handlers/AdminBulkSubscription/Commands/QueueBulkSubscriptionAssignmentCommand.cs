using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.Admin;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.AdminBulkSubscription.Commands
{
    /// <summary>
    /// Command to queue bulk subscription assignment job
    /// </summary>
    public class QueueBulkSubscriptionAssignmentCommand : IRequest<IDataResult<BulkSubscriptionAssignmentJobDto>>
    {
        public IFormFile ExcelFile { get; set; }
        public int? DefaultTierId { get; set; }
        public int? DefaultDurationDays { get; set; }
        public bool SendNotification { get; set; } = true;
        public string NotificationMethod { get; set; } = "Email";
        public bool AutoActivate { get; set; } = true;
        public int AdminId { get; set; }
    }

    public class QueueBulkSubscriptionAssignmentCommandHandler : IRequestHandler<QueueBulkSubscriptionAssignmentCommand, IDataResult<BulkSubscriptionAssignmentJobDto>>
    {
        private readonly IBulkSubscriptionAssignmentService _bulkSubscriptionAssignmentService;

        public QueueBulkSubscriptionAssignmentCommandHandler(IBulkSubscriptionAssignmentService bulkSubscriptionAssignmentService)
        {
            _bulkSubscriptionAssignmentService = bulkSubscriptionAssignmentService;
        }

        [SecuredOperation(Priority = 1)]
        [PerformanceAspect(5)]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<BulkSubscriptionAssignmentJobDto>> Handle(QueueBulkSubscriptionAssignmentCommand request, CancellationToken cancellationToken)
        {
            var result = await _bulkSubscriptionAssignmentService.QueueBulkSubscriptionAssignmentAsync(
                request.ExcelFile,
                request.AdminId,
                request.DefaultTierId,
                request.DefaultDurationDays,
                request.SendNotification,
                request.NotificationMethod,
                request.AutoActivate
            );

            return new SuccessDataResult<BulkSubscriptionAssignmentJobDto>(result.Data, result.Message);
        }
    }
}
