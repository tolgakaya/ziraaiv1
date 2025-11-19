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
    /// Get ticket detail with all messages (including internal) - Admin only
    /// </summary>
    public class GetTicketDetailAsAdminQuery : IRequest<IDataResult<AdminTicketDetailDto>>
    {
        public int TicketId { get; set; }

        public class GetTicketDetailAsAdminQueryHandler : IRequestHandler<GetTicketDetailAsAdminQuery, IDataResult<AdminTicketDetailDto>>
        {
            private readonly ITicketRepository _ticketRepository;
            private readonly IUserRepository _userRepository;

            public GetTicketDetailAsAdminQueryHandler(
                ITicketRepository ticketRepository,
                IUserRepository userRepository)
            {
                _ticketRepository = ticketRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<AdminTicketDetailDto>> Handle(GetTicketDetailAsAdminQuery request, CancellationToken cancellationToken)
            {
                var ticket = await _ticketRepository.GetTicketWithMessagesAsync(request.TicketId);
                if (ticket == null)
                {
                    return new ErrorDataResult<AdminTicketDetailDto>("Destek talebi bulunamadı.");
                }

                // Get user information
                var user = await _userRepository.GetAsync(u => u.UserId == ticket.UserId);
                var assignedUser = ticket.AssignedToUserId.HasValue
                    ? await _userRepository.GetAsync(u => u.UserId == ticket.AssignedToUserId.Value)
                    : null;

                // Get all message sender names
                var senderIds = ticket.Messages?.Select(m => m.FromUserId).Distinct().ToList() ?? new System.Collections.Generic.List<int>();
                var senders = await _userRepository.GetListAsync(u => senderIds.Contains(u.UserId));
                var senderDict = senders.ToDictionary(u => u.UserId, u => u.FullName);

                var ticketDetail = new AdminTicketDetailDto
                {
                    Id = ticket.Id,
                    UserId = ticket.UserId,
                    UserName = user?.FullName ?? "Bilinmiyor",
                    UserEmail = user?.Email ?? "Bilinmiyor",
                    UserRole = ticket.UserRole,
                    Subject = ticket.Subject,
                    Description = ticket.Description,
                    Category = ticket.Category,
                    Priority = ticket.Priority,
                    Status = ticket.Status,
                    AssignedToUserId = ticket.AssignedToUserId,
                    AssignedToUserName = assignedUser?.FullName,
                    CreatedDate = ticket.CreatedDate,
                    UpdatedDate = ticket.UpdatedDate,
                    ResolvedDate = ticket.ResolvedDate,
                    ClosedDate = ticket.ClosedDate,
                    ResolutionNotes = ticket.ResolutionNotes,
                    SatisfactionRating = ticket.SatisfactionRating,
                    SatisfactionFeedback = ticket.SatisfactionFeedback,
                    Messages = ticket.Messages?
                        .OrderBy(m => m.CreatedDate)
                        .Select(m => new AdminTicketMessageDto
                        {
                            Id = m.Id,
                            FromUserId = m.FromUserId,
                            FromUserName = senderDict.ContainsKey(m.FromUserId) ? senderDict[m.FromUserId] : "Bilinmiyor",
                            Message = m.Message,
                            IsAdminResponse = m.IsAdminResponse,
                            IsInternal = m.IsInternal,
                            IsRead = m.IsRead,
                            ReadDate = m.ReadDate,
                            CreatedDate = m.CreatedDate
                        }).ToList()
                };

                return new SuccessDataResult<AdminTicketDetailDto>(ticketDetail);
            }
        }
    }
}
