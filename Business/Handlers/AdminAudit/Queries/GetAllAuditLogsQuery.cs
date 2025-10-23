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
    /// Admin query to get all audit logs with pagination and filtering
    /// </summary>
    public class GetAllAuditLogsQuery : IRequest<IDataResult<IEnumerable<AdminOperationLog>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string Action { get; set; }
        public string EntityType { get; set; }
        public bool? IsOnBehalfOf { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetAllAuditLogsQueryHandler : IRequestHandler<GetAllAuditLogsQuery, IDataResult<IEnumerable<AdminOperationLog>>>
        {
            private readonly IAdminOperationLogRepository _logRepository;

            public GetAllAuditLogsQueryHandler(IAdminOperationLogRepository logRepository)
            {
                _logRepository = logRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<AdminOperationLog>>> Handle(GetAllAuditLogsQuery request, CancellationToken cancellationToken)
            {
                var query = _logRepository.Query()
                    .Include(l => l.AdminUser)
                    .Include(l => l.TargetUser)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Action))
                {
                    query = query.Where(l => l.Action == request.Action);
                }

                if (!string.IsNullOrEmpty(request.EntityType))
                {
                    query = query.Where(l => l.EntityType == request.EntityType);
                }

                if (request.IsOnBehalfOf.HasValue)
                {
                    query = query.Where(l => l.IsOnBehalfOf == request.IsOnBehalfOf.Value);
                }

                if (request.StartDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp <= request.EndDate.Value);
                }

                var logs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new SuccessDataResult<IEnumerable<AdminOperationLog>>(logs, "Audit logs retrieved successfully");
            }
        }
    }
}
