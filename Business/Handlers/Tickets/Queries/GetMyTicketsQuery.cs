using System.Collections.Generic;
using System.Linq;
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
    /// Get user's own tickets - Farmer or Sponsor only
    /// </summary>
    public class GetMyTicketsQuery : IRequest<IDataResult<TicketListResponseDto>>
    {
        public int UserId { get; set; }  // From JWT
        public string Status { get; set; }  // Optional filter
        public string Category { get; set; }  // Optional filter

        public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, IDataResult<TicketListResponseDto>>
        {
            private readonly ITicketRepository _ticketRepository;

            public GetMyTicketsQueryHandler(ITicketRepository ticketRepository)
            {
                _ticketRepository = ticketRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<TicketListResponseDto>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
            {
                var tickets = await _ticketRepository.GetUserTicketsAsync(
                    request.UserId,
                    request.Status,
                    request.Category);

                var ticketDtos = tickets.Select(t => new TicketListDto
                {
                    Id = t.Id,
                    Subject = t.Subject,
                    Category = t.Category,
                    Priority = t.Priority,
                    Status = t.Status,
                    CreatedDate = t.CreatedDate,
                    LastResponseDate = t.LastResponseDate,
                    HasUnreadMessages = t.Messages?.Any(m => m.IsAdminResponse && !m.IsRead) ?? false
                }).ToList();

                var response = new TicketListResponseDto
                {
                    Tickets = ticketDtos,
                    TotalCount = ticketDtos.Count
                };

                return new SuccessDataResult<TicketListResponseDto>(response);
            }
        }
    }
}
