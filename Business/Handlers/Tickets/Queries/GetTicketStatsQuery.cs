using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.Tickets.Queries
{
    /// <summary>
    /// Get ticket statistics - Admin only
    /// </summary>
    public class GetTicketStatsQuery : IRequest<IDataResult<TicketStatsDto>>
    {
        public class GetTicketStatsQueryHandler : IRequestHandler<GetTicketStatsQuery, IDataResult<TicketStatsDto>>
        {
            private readonly ITicketRepository _ticketRepository;

            public GetTicketStatsQueryHandler(ITicketRepository ticketRepository)
            {
                _ticketRepository = ticketRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<TicketStatsDto>> Handle(GetTicketStatsQuery request, CancellationToken cancellationToken)
            {
                var openCount = await _ticketRepository.GetTicketCountByStatusAsync("Open");
                var inProgressCount = await _ticketRepository.GetTicketCountByStatusAsync("InProgress");
                var resolvedCount = await _ticketRepository.GetTicketCountByStatusAsync("Resolved");
                var closedCount = await _ticketRepository.GetTicketCountByStatusAsync("Closed");

                var stats = new TicketStatsDto
                {
                    OpenCount = openCount,
                    InProgressCount = inProgressCount,
                    ResolvedCount = resolvedCount,
                    ClosedCount = closedCount,
                    TotalCount = openCount + inProgressCount + resolvedCount + closedCount
                };

                return new SuccessDataResult<TicketStatsDto>(stats);
            }
        }
    }
}
