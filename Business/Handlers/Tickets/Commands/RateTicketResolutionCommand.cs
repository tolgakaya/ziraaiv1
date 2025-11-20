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
    /// Rate ticket resolution - Farmer or Sponsor only
    /// </summary>
    public class RateTicketResolutionCommand : IRequest<IResult>
    {
        public int UserId { get; set; }  // From JWT
        public int TicketId { get; set; }
        public int Rating { get; set; }  // 1-5
        public string Feedback { get; set; }

        public class RateTicketResolutionCommandHandler : IRequestHandler<RateTicketResolutionCommand, IResult>
        {
            private readonly ITicketRepository _ticketRepository;

            public RateTicketResolutionCommandHandler(ITicketRepository ticketRepository)
            {
                _ticketRepository = ticketRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RateTicketResolutionCommand request, CancellationToken cancellationToken)
            {
                // Validate rating
                if (request.Rating < 1 || request.Rating > 5)
                {
                    return new ErrorResult("Puanlama 1-5 arasında olmalıdır.");
                }

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

                // Only resolved tickets can be rated
                if (ticket.Status != "Resolved" && ticket.Status != "Closed")
                {
                    return new ErrorResult("Sadece çözülmüş veya kapatılmış destek talepleri puanlanabilir.");
                }

                // Already rated
                if (ticket.SatisfactionRating.HasValue)
                {
                    return new ErrorResult("Bu destek talebi zaten puanlanmış.");
                }

                ticket.SatisfactionRating = request.Rating;
                ticket.SatisfactionFeedback = request.Feedback?.Trim();
                ticket.UpdatedDate = DateTime.Now;

                // Auto-close after rating if resolved
                if (ticket.Status == "Resolved")
                {
                    ticket.Status = "Closed";
                    ticket.ClosedDate = DateTime.Now;
                }

                _ticketRepository.Update(ticket);
                await _ticketRepository.SaveChangesAsync();

                return new SuccessResult("Puanlamanız için teşekkür ederiz.");
            }
        }
    }
}
