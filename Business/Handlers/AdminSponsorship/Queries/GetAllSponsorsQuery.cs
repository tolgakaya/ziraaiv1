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

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to get all users with Sponsor role (GroupId = 3)
    /// </summary>
    public class GetAllSponsorsQuery : IRequest<IDataResult<IEnumerable<UserDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public bool? IsActive { get; set; }
        public string Status { get; set; }
        public string SearchTerm { get; set; }

        public class GetAllSponsorsQueryHandler : IRequestHandler<GetAllSponsorsQuery, IDataResult<IEnumerable<UserDto>>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IMapper _mapper;

            public GetAllSponsorsQueryHandler(
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
            public async Task<IDataResult<IEnumerable<UserDto>>> Handle(GetAllSponsorsQuery request, CancellationToken cancellationToken)
            {
                // Get only users with Sponsor role (GroupId = 3)
                var sponsorUserIds = _userGroupRepository.Query()
                    .Where(ug => ug.GroupId == 3) // GroupId 3 = Sponsor role
                    .Select(ug => ug.UserId)
                    .ToList();

                // Build filter expression
                var query = _userRepository.Query()
                    .Where(u => sponsorUserIds.Contains(u.UserId));

                // Filter by search term if specified (email, name, or mobile phone)
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(u =>
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.FullName.ToLower().Contains(searchTerm) ||
                        (u.MobilePhones != null && u.MobilePhones.Contains(searchTerm)));
                }

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

                // Apply pagination and project to DTO before ToList() to avoid reading DateTime infinity values
                var sponsorDtoList = query
                    .OrderByDescending(u => u.UserId)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(u => new UserDto
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Email = u.Email,
                        MobilePhones = u.MobilePhones,
                        Address = u.Address,
                        Notes = u.Notes,
                        Gender = u.Gender ?? 0,
                        Status = u.Status,
                        IsActive = u.IsActive
                    })
                    .ToList();

                return new SuccessDataResult<IEnumerable<UserDto>>(sponsorDtoList, $"Retrieved {sponsorDtoList.Count} sponsors successfully");
            }
        }
    }
}
