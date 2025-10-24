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
    /// Admin query to get all users with pagination
    /// </summary>
    public class GetAllUsersQuery : IRequest<IDataResult<IEnumerable<UserDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public bool? IsActive { get; set; }
        public string Status { get; set; }

        public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IDataResult<IEnumerable<UserDto>>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IMapper _mapper;

            public GetAllUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
            {
                _userRepository = userRepository;
                _mapper = mapper;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
            {
                // Build filter expression
                var query = _userRepository.Query();

                // Filter by IsActive if specified
                if (request.IsActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == request.IsActive.Value);
                }

                // Filter by Status if specified
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(u => u.Status.ToString() == request.Status);
                }

                // Apply pagination
                var users = query
                    .OrderByDescending(u => u.RecordDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var userDtoList = users.Select(user => _mapper.Map<UserDto>(user)).ToList();

                return new SuccessDataResult<IEnumerable<UserDto>>(userDtoList, "Users retrieved successfully");
            }
        }
    }
}
