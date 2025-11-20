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
    /// Get all tickets with filters - Admin only
    /// </summary>
    public class GetAllTicketsAsAdminQuery : IRequest<IDataResult<AdminTicketListResponseDto>>
    {
        public string Status { get; set; }  // Optional filter
        public string Category { get; set; }  // Optional filter
        public string Priority { get; set; }  // Optional filter

        public class GetAllTicketsAsAdminQueryHandler : IRequestHandler<GetAllTicketsAsAdminQuery, IDataResult<AdminTicketListResponseDto>>
        {
            private readonly ITicketRepository _ticketRepository;
            private readonly IUserRepository _userRepository;

            public GetAllTicketsAsAdminQueryHandler(
                ITicketRepository ticketRepository,
                IUserRepository userRepository)
            {
                _ticketRepository = ticketRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<AdminTicketListResponseDto>> Handle(GetAllTicketsAsAdminQuery request, CancellationToken cancellationToken)
            {
                var tickets = await _ticketRepository.GetAllTicketsForAdminAsync(
                    request.Status,
                    request.Category,
                    request.Priority);

                // Get user information for display
                var userIds = tickets.Select(t => t.UserId).Distinct().ToList();
                var assignedUserIds = tickets.Where(t => t.AssignedToUserId.HasValue)
                    .Select(t => t.AssignedToUserId.Value).Distinct().ToList();

                var allUserIds = userIds.Union(assignedUserIds).ToList();
                var users = await _userRepository.GetListAsync(u => allUserIds.Contains(u.UserId));
                var userDict = users.ToDictionary(u => u.UserId, u => u.FullName);

                var ticketDtos = tickets.Select(t => new AdminTicketListDto
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    UserName = userDict.ContainsKey(t.UserId) ? userDict[t.UserId] : "Bilinmiyor",
                    UserRole = t.UserRole,
                    Subject = t.Subject,
                    Category = t.Category,
                    Priority = t.Priority,
                    Status = t.Status,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedToUserId.HasValue && userDict.ContainsKey(t.AssignedToUserId.Value)
                        ? userDict[t.AssignedToUserId.Value]
                        : null,
                    CreatedDate = t.CreatedDate,
                    LastResponseDate = t.LastResponseDate,
                    MessageCount = t.Messages?.Count ?? 0
                }).ToList();

                var response = new AdminTicketListResponseDto
                {
                    Tickets = ticketDtos,
                    TotalCount = ticketDtos.Count
                };

                return new SuccessDataResult<AdminTicketListResponseDto>(response);
            }
        }
    }
}
