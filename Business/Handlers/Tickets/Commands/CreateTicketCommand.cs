using System;
using System.Linq;
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
    /// Create a new support ticket - Farmer or Sponsor only
    /// </summary>
    public class CreateTicketCommand : IRequest<IDataResult<int>>
    {
        public int UserId { get; set; }  // From JWT
        public string UserRole { get; set; }  // From JWT (Farmer or Sponsor)
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }  // Technical, Billing, Account, General
        public string Priority { get; set; }  // Low, Normal, High

        public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, IDataResult<int>>
        {
            private readonly ITicketRepository _ticketRepository;

            public CreateTicketCommandHandler(ITicketRepository ticketRepository)
            {
                _ticketRepository = ticketRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<int>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
            {
                // Validate category
                var validCategories = new[] { "Technical", "Billing", "Account", "General" };
                if (!validCategories.Contains(request.Category))
                {
                    return new ErrorDataResult<int>("Geçersiz kategori. Geçerli değerler: Technical, Billing, Account, General");
                }

                // Validate priority
                var validPriorities = new[] { "Low", "Normal", "High" };
                var priority = string.IsNullOrEmpty(request.Priority) ? "Normal" : request.Priority;
                if (!validPriorities.Contains(priority))
                {
                    return new ErrorDataResult<int>("Geçersiz öncelik. Geçerli değerler: Low, Normal, High");
                }

                // Validate subject and description
                if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 200)
                {
                    return new ErrorDataResult<int>("Konu alanı zorunludur ve maksimum 200 karakter olabilir.");
                }

                if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 2000)
                {
                    return new ErrorDataResult<int>("Açıklama alanı zorunludur ve maksimum 2000 karakter olabilir.");
                }

                var ticket = new Ticket
                {
                    UserId = request.UserId,
                    UserRole = request.UserRole,
                    Subject = request.Subject.Trim(),
                    Description = request.Description.Trim(),
                    Category = request.Category,
                    Priority = priority,
                    Status = "Open",
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _ticketRepository.Add(ticket);
                await _ticketRepository.SaveChangesAsync();

                return new SuccessDataResult<int>(ticket.Id, "Destek talebi başarıyla oluşturuldu.");
            }
        }
    }
}
