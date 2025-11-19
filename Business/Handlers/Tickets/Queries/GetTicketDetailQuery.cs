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
    /// Get ticket detail with messages - Farmer or Sponsor only (own tickets)
    /// </summary>
    public class GetTicketDetailQuery : IRequest<IDataResult<TicketDetailDto>>
    {
        public int UserId { get; set; }  // From JWT
        public int TicketId { get; set; }

        public class GetTicketDetailQueryHandler : IRequestHandler<GetTicketDetailQuery, IDataResult<TicketDetailDto>>
        {
            private readonly ITicketRepository _ticketRepository;
            private readonly ITicketMessageRepository _ticketMessageRepository;

            public GetTicketDetailQueryHandler(
                ITicketRepository ticketRepository,
                ITicketMessageRepository ticketMessageRepository)
            {
                _ticketRepository = ticketRepository;
                _ticketMessageRepository = ticketMessageRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<TicketDetailDto>> Handle(GetTicketDetailQuery request, CancellationToken cancellationToken)
            {
                var ticket = await _ticketRepository.GetTicketWithMessagesAsync(request.TicketId);
                if (ticket == null)
                {
                    return new ErrorDataResult<TicketDetailDto>("Destek talebi bulunamadı.");
                }

                // Verify ownership
                if (ticket.UserId != request.UserId)
                {
                    return new ErrorDataResult<TicketDetailDto>("Bu destek talebine erişim yetkiniz yok.");
                }

                // Mark admin messages as read
                await _ticketMessageRepository.MarkMessagesAsReadAsync(request.TicketId, request.UserId);

                // Map to DTO (exclude internal admin notes)
                var ticketDetail = new TicketDetailDto
                {
                    Id = ticket.Id,
                    Subject = ticket.Subject,
                    Description = ticket.Description,
                    Category = ticket.Category,
                    Priority = ticket.Priority,
                    Status = ticket.Status,
                    CreatedDate = ticket.CreatedDate,
                    UpdatedDate = ticket.UpdatedDate,
                    ResolvedDate = ticket.ResolvedDate,
                    ClosedDate = ticket.ClosedDate,
                    ResolutionNotes = ticket.ResolutionNotes,
                    SatisfactionRating = ticket.SatisfactionRating,
                    SatisfactionFeedback = ticket.SatisfactionFeedback,
                    Messages = ticket.Messages?
                        .Where(m => !m.IsInternal)  // Hide internal notes from users
                        .OrderBy(m => m.CreatedDate)
                        .Select(m => new TicketMessageDto
                        {
                            Id = m.Id,
                            Message = m.Message,
                            IsAdminResponse = m.IsAdminResponse,
                            CreatedDate = m.CreatedDate
                        }).ToList()
                };

                return new SuccessDataResult<TicketDetailDto>(ticketDetail);
            }
        }
    }
}
