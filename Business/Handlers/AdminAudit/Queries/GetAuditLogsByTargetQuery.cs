using System;
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
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.AdminAudit.Queries
{
    /// <summary>
    /// Admin query to get audit logs by target user
    /// </summary>
    public class GetAuditLogsByTargetQuery : IRequest<IDataResult<IEnumerable<AdminOperationLog>>>
    {
        public int TargetUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetAuditLogsByTargetQueryHandler : IRequestHandler<GetAuditLogsByTargetQuery, IDataResult<IEnumerable<AdminOperationLog>>>
        {
            private readonly IAdminOperationLogRepository _logRepository;

            public GetAuditLogsByTargetQueryHandler(IAdminOperationLogRepository logRepository)
            {
                _logRepository = logRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<AdminOperationLog>>> Handle(GetAuditLogsByTargetQuery request, CancellationToken cancellationToken)
            {
                var logs = await _logRepository.GetByTargetUserIdAsync(
                    request.TargetUserId,
                    request.Page,
                    request.PageSize);

                // Apply date filters if provided
                if (request.StartDate.HasValue || request.EndDate.HasValue)
                {
                    var filteredLogs = logs.AsQueryable();

                    if (request.StartDate.HasValue)
                    {
                        filteredLogs = filteredLogs.Where(l => l.Timestamp >= request.StartDate.Value);
                    }

                    if (request.EndDate.HasValue)
                    {
                        filteredLogs = filteredLogs.Where(l => l.Timestamp <= request.EndDate.Value);
                    }

                    logs = filteredLogs.ToList();
                }

                return new SuccessDataResult<IEnumerable<AdminOperationLog>>(logs, $"Found {logs.Count} logs for target user {request.TargetUserId}");
            }
        }
    }
}
