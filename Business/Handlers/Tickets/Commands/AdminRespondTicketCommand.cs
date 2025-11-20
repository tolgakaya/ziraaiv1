using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.Tickets.Commands
{
    /// <summary>
    /// Admin responds to a ticket - Admin only
    /// </summary>
    public class AdminRespondTicketCommand : IRequest<IResult>
    {
        public int AdminUserId { get; set; }  // From JWT
        public int TicketId { get; set; }
        public string Message { get; set; }
        public bool IsInternal { get; set; }  // Internal note (not visible to user)

        public class AdminRespondTicketCommandHandler : IRequestHandler<AdminRespondTicketCommand, IResult>
        {
            private readonly ITicketRepository _ticketRepository;
            private readonly ITicketMessageRepository _ticketMessageRepository;

            public AdminRespondTicketCommandHandler(
                ITicketRepository ticketRepository,
                ITicketMessageRepository ticketMessageRepository)
            {
                _ticketRepository = ticketRepository;
                _ticketMessageRepository = ticketMessageRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(AdminRespondTicketCommand request, CancellationToken cancellationToken)
            {
                // Validate message
                if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 2000)
                {
                    return new ErrorResult("Mesaj alanı zorunludur ve maksimum 2000 karakter olabilir.");
                }

                var ticket = await _ticketRepository.GetAsync(t => t.Id == request.TicketId);
                if (ticket == null)
                {
                    return new ErrorResult("Destek talebi bulunamadı.");
                }

                if (ticket.Status == "Closed")
                {
                    return new ErrorResult("Kapatılmış destek talebine yanıt verilemez.");
                }

                var ticketMessage = new TicketMessage
                {
                    TicketId = request.TicketId,
                    FromUserId = request.AdminUserId,
                    Message = request.Message.Trim(),
                    IsAdminResponse = true,
                    IsInternal = request.IsInternal,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };

                _ticketMessageRepository.Add(ticketMessage);

                // Update ticket
                ticket.LastResponseDate = DateTime.Now;
                ticket.UpdatedDate = DateTime.Now;

                // Auto-assign if not assigned
                if (!ticket.AssignedToUserId.HasValue)
                {
                    ticket.AssignedToUserId = request.AdminUserId;
                }

                // Update status to InProgress if Open
                if (ticket.Status == "Open")
                {
                    ticket.Status = "InProgress";
                }

                _ticketRepository.Update(ticket);
                await _ticketMessageRepository.SaveChangesAsync();

                var message = request.IsInternal
                    ? "Dahili not başarıyla eklendi."
                    : "Yanıt başarıyla gönderildi.";

                return new SuccessResult(message);
            }
        }
    }
}
