using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Collections.Generic;
using Entities.Concrete;

namespace Business.Handlers.AdminAnalytics.Queries
{
    /// <summary>
    /// Admin query to get system activity logs
    /// </summary>
    public class GetActivityLogsQuery : IRequest<IDataResult<ActivityLogsDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? UserId { get; set; }
        public string ActionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, IDataResult<ActivityLogsDto>>
        {
            private readonly IAdminOperationLogRepository _logRepository;

            public GetActivityLogsQueryHandler(IAdminOperationLogRepository logRepository)
            {
                _logRepository = logRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<ActivityLogsDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
            {
                var query = _logRepository.Query();

                // Apply filters
                if (request.UserId.HasValue)
                {
                    query = query.Where(l => l.AdminUserId == request.UserId.Value || l.TargetUserId == request.UserId.Value);
                }

                if (!string.IsNullOrEmpty(request.ActionType))
                {
                    query = query.Where(l => l.Action == request.ActionType);
                }

                if (request.StartDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp <= request.EndDate.Value);
                }

                var totalCount = query.Count();
                var logs = query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var result = new ActivityLogsDto
                {
                    Logs = logs,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = totalCount
                };

                return new SuccessDataResult<ActivityLogsDto>(result, "Activity logs retrieved successfully");
            }
        }
    }

    public class ActivityLogsDto
    {
        public List<AdminOperationLog> Logs { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
