using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.Tickets.Commands
{
    /// <summary>
    /// Close user's own ticket - Farmer or Sponsor only
    /// </summary>
    public class CloseTicketCommand : IRequest<IResult>
    {
        public int UserId { get; set; }  // From JWT
        public int TicketId { get; set; }

        public class CloseTicketCommandHandler : IRequestHandler<CloseTicketCommand, IResult>
        {
            private readonly ITicketRepository _ticketRepository;

            public CloseTicketCommandHandler(ITicketRepository ticketRepository)
            {
                _ticketRepository = ticketRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(CloseTicketCommand request, CancellationToken cancellationToken)
            {
                // Get ticket and verify ownership
                var ticket = await _ticketRepository.GetAsync(t => t.Id == request.TicketId);
                if (ticket == null)
                {
                    return new ErrorResult("Destek talebi bulunamadı.");
                }

                if (ticket.UserId != request.UserId)
                {
                    return new ErrorResult("Bu destek talebine erişim yetkiniz yok.");
                }

                if (ticket.Status == "Closed")
                {
                    return new ErrorResult("Destek talebi zaten kapatılmış.");
                }

                ticket.Status = "Closed";
                ticket.ClosedDate = DateTime.Now;
                ticket.UpdatedDate = DateTime.Now;

                _ticketRepository.Update(ticket);
                await _ticketRepository.SaveChangesAsync();

                return new SuccessResult("Destek talebi başarıyla kapatıldı.");
            }
        }
    }
}
