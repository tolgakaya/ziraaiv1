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
            private readonly IMapper _mapper;

            public GetUserByIdQueryHandler(IUserRepository userRepository, IMapper mapper)
            {
                _userRepository = userRepository;
                _mapper = mapper;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
            {
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
