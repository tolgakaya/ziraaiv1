using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Dtos;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminUsers.Queries
{
    /// <summary>
    /// Admin query to get user by ID with full details
    /// </summary>
    public class GetUserByIdQuery : IRequest<IDataResult<UserDto>>
    {
        public int UserId { get; set; }

        public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, IDataResult<UserDto>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IMapper _mapper;

            public GetUserByIdQueryHandler(
                IUserRepository userRepository,
                IUserGroupRepository userGroupRepository,
                IMapper mapper)
            {
                _userRepository = userRepository;
                _userGroupRepository = userGroupRepository;
                _mapper = mapper;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
            {
                // SECURITY: Check if requested user is an Admin
                // Admins should not be able to view other admin accounts
                var isAdminUser = _userGroupRepository.Query()
                    .Any(ug => ug.UserId == request.UserId && ug.GroupId == 1); // GroupId 1 = Admin role
                
                if (isAdminUser)
                {
                    return new ErrorDataResult<UserDto>("Access denied: Cannot view admin user details");
                }

                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);

                if (user == null)
                {
                    return new ErrorDataResult<UserDto>("User not found");
                }

                var userDto = _mapper.Map<UserDto>(user);
                return new SuccessDataResult<UserDto>(userDto, "User retrieved successfully");
            }
        }
    }
}
