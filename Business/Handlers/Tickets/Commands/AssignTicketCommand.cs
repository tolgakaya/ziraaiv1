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
    /// Assign ticket to admin user - Admin only
    /// </summary>
    public class AssignTicketCommand : IRequest<IResult>
    {
        public int TicketId { get; set; }
        public int? AssignedToUserId { get; set; }  // null to unassign

        public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, IResult>
        {
            private readonly ITicketRepository _ticketRepository;

            public AssignTicketCommandHandler(ITicketRepository ticketRepository)
            {
                _ticketRepository = ticketRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
            {
                var ticket = await _ticketRepository.GetAsync(t => t.Id == request.TicketId);
                if (ticket == null)
                {
                    return new ErrorResult("Destek talebi bulunamadı.");
                }

                if (ticket.Status == "Closed")
                {
                    return new ErrorResult("Kapatılmış destek talebi atanamaz.");
                }

                ticket.AssignedToUserId = request.AssignedToUserId;
                ticket.UpdatedDate = DateTime.Now;

                // Update status to InProgress if assigned
                if (request.AssignedToUserId.HasValue && ticket.Status == "Open")
                {
                    ticket.Status = "InProgress";
                }

                _ticketRepository.Update(ticket);
                await _ticketRepository.SaveChangesAsync();

                var message = request.AssignedToUserId.HasValue
                    ? "Destek talebi başarıyla atandı."
                    : "Destek talebi ataması kaldırıldı.";

                return new SuccessResult(message);
            }
        }
    }
}
