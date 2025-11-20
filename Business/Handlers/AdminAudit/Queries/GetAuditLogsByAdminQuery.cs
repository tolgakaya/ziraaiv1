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
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminAudit.Queries
{
    /// <summary>
    /// Admin query to get audit logs by admin user
    /// </summary>
    public class GetAuditLogsByAdminQuery : IRequest<IDataResult<IEnumerable<AdminOperationLog>>>
    {
        public int AdminUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetAuditLogsByAdminQueryHandler : IRequestHandler<GetAuditLogsByAdminQuery, IDataResult<IEnumerable<AdminOperationLog>>>
        {
            private readonly IAdminOperationLogRepository _logRepository;

            public GetAuditLogsByAdminQueryHandler(IAdminOperationLogRepository logRepository)
            {
                _logRepository = logRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<AdminOperationLog>>> Handle(GetAuditLogsByAdminQuery request, CancellationToken cancellationToken)
            {
                var logs = await _logRepository.GetByAdminUserIdAsync(
                    request.AdminUserId,
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

                return new SuccessDataResult<IEnumerable<AdminOperationLog>>(logs, $"Found {logs.Count} logs for admin user {request.AdminUserId}");
            }
        }
    }
}
