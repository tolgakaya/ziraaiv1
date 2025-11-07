using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Dtos;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminUsers.Queries
{
    /// <summary>
    /// Admin query to search users by email, name, or mobile phone
    /// </summary>
    public class SearchUsersQuery : IRequest<IDataResult<IEnumerable<UserDto>>>
    {
        public string SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, IDataResult<IEnumerable<UserDto>>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IMapper _mapper;

            public SearchUsersQueryHandler(
                IUserRepository userRepository,
                IUserGroupRepository userGroupRepository,
                IMapper mapper)
            {
                _userRepository = userRepository;
                _userGroupRepository = userGroupRepository;
                _mapper = mapper;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<UserDto>>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    return new ErrorDataResult<IEnumerable<UserDto>>("Search term cannot be empty");
                }

                var searchTerm = request.SearchTerm.ToLower();

                // SECURITY: Exclude Admin users from search results
                // Admins should not be able to search for or view other admin accounts
                var adminUserIds = _userGroupRepository.Query()
                    .Where(ug => ug.GroupId == 1) // GroupId 1 = Admin role
                    .Select(ug => ug.UserId)
                    .ToList();

                var users = _userRepository.Query()
                    .Where(u => !adminUserIds.Contains(u.UserId))
                    .Where(u =>
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.FullName.ToLower().Contains(searchTerm) ||
                        (u.MobilePhones != null && u.MobilePhones.Contains(searchTerm)))
                    .OrderByDescending(u => u.RecordDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var userDtoList = users.Select(user => _mapper.Map<UserDto>(user)).ToList();

                return new SuccessDataResult<IEnumerable<UserDto>>(userDtoList, $"Found {userDtoList.Count} users");
            }
        }
    }
}
