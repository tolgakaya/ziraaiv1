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
    /// Add a message to user's own ticket - Farmer or Sponsor only
    /// </summary>
    public class AddTicketMessageCommand : IRequest<IResult>
    {
        public int UserId { get; set; }  // From JWT
        public int TicketId { get; set; }
        public string Message { get; set; }

        public class AddTicketMessageCommandHandler : IRequestHandler<AddTicketMessageCommand, IResult>
        {
            private readonly ITicketRepository _ticketRepository;
            private readonly ITicketMessageRepository _ticketMessageRepository;

            public AddTicketMessageCommandHandler(
                ITicketRepository ticketRepository,
                ITicketMessageRepository ticketMessageRepository)
            {
                _ticketRepository = ticketRepository;
                _ticketMessageRepository = ticketMessageRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(AddTicketMessageCommand request, CancellationToken cancellationToken)
            {
                // Validate message
                if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 2000)
                {
                    return new ErrorResult("Mesaj alanı zorunludur ve maksimum 2000 karakter olabilir.");
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

                // Check if ticket is closed
                if (ticket.Status == "Closed")
                {
                    return new ErrorResult("Kapatılmış destek talebine mesaj eklenemez.");
                }

                var ticketMessage = new TicketMessage
                {
                    TicketId = request.TicketId,
                    FromUserId = request.UserId,
                    Message = request.Message.Trim(),
                    IsAdminResponse = false,
                    IsInternal = false,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };

                _ticketMessageRepository.Add(ticketMessage);

                // Update ticket's last response date
                ticket.LastResponseDate = DateTime.Now;
                ticket.UpdatedDate = DateTime.Now;
                _ticketRepository.Update(ticket);

                await _ticketMessageRepository.SaveChangesAsync();

                return new SuccessResult("Mesaj başarıyla eklendi.");
            }
        }
    }
}
