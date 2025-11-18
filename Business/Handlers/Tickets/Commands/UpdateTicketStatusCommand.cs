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
    /// Update ticket status - Admin only
    /// </summary>
    public class UpdateTicketStatusCommand : IRequest<IResult>
    {
        public int TicketId { get; set; }
        public string Status { get; set; }  // Open, InProgress, Resolved, Closed
        public string ResolutionNotes { get; set; }  // Required for Resolved status

        public class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, IResult>
        {
            private readonly ITicketRepository _ticketRepository;

            public UpdateTicketStatusCommandHandler(ITicketRepository ticketRepository)
            {
                _ticketRepository = ticketRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
            {
                // Validate status
                var validStatuses = new[] { "Open", "InProgress", "Resolved", "Closed" };
                if (string.IsNullOrWhiteSpace(request.Status) || !Array.Exists(validStatuses, s => s == request.Status))
                {
                    return new ErrorResult("Geçersiz durum değeri. Geçerli değerler: Open, InProgress, Resolved, Closed");
                }

                var ticket = await _ticketRepository.GetAsync(t => t.Id == request.TicketId);
                if (ticket == null)
                {
                    return new ErrorResult("Destek talebi bulunamadı.");
                }

                // Validation for Resolved status
                if (request.Status == "Resolved" && string.IsNullOrWhiteSpace(request.ResolutionNotes))
                {
                    return new ErrorResult("Çözüm notları zorunludur.");
                }

                // Prevent reopening closed tickets with satisfaction rating
                if (ticket.Status == "Closed" && ticket.SatisfactionRating.HasValue && request.Status != "Closed")
                {
                    return new ErrorResult("Puanlanmış ve kapatılmış destek talebi yeniden açılamaz.");
                }

                ticket.Status = request.Status;
                ticket.UpdatedDate = DateTime.Now;

                if (request.Status == "Resolved")
                {
                    ticket.ResolvedDate = DateTime.Now;
                    ticket.ResolutionNotes = request.ResolutionNotes?.Trim();
                }
                else if (request.Status == "Closed")
                {
                    ticket.ClosedDate = DateTime.Now;
                }

                _ticketRepository.Update(ticket);
                await _ticketRepository.SaveChangesAsync();

                return new SuccessResult("Destek talebi durumu başarıyla güncellendi.");
            }
        }
    }
}
