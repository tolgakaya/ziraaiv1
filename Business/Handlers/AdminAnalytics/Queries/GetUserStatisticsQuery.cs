using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminAnalytics.Queries
{
    /// <summary>
    /// Admin query to get user statistics and metrics
    /// </summary>
    public class GetUserStatisticsQuery : IRequest<IDataResult<UserStatisticsDto>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetUserStatisticsQueryHandler : IRequestHandler<GetUserStatisticsQuery, IDataResult<UserStatisticsDto>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly IUserGroupRepository _userGroupRepository;

            public GetUserStatisticsQueryHandler(
                IUserRepository userRepository,
                IGroupRepository groupRepository,
                IUserGroupRepository userGroupRepository)
            {
                _userRepository = userRepository;
                _groupRepository = groupRepository;
                _userGroupRepository = userGroupRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<UserStatisticsDto>> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
            {
                var allUsers = _userRepository.Query();

                // Apply date filters if provided
                if (request.StartDate.HasValue)
                {
                    allUsers = allUsers.Where(u => u.RecordDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    allUsers = allUsers.Where(u => u.RecordDate <= request.EndDate.Value);
                }

                // Get role-based counts from Groups/UserGroups tables
                // Group IDs: 1 = Administrators, 2 = Farmer, 3 = Sponsor
                var adminGroup = await _groupRepository.GetAsync(g => g.GroupName == "Administrators");
                var farmerGroup = await _groupRepository.GetAsync(g => g.GroupName == "Farmer");
                var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");

                var adminUsers = adminGroup != null
                    ? _userGroupRepository.Query()
                        .Where(ug => ug.GroupId == adminGroup.Id)
                        .Select(ug => ug.UserId)
                        .Distinct()
                        .Count()
                    : 0;

                var farmerUsers = farmerGroup != null
                    ? _userGroupRepository.Query()
                        .Where(ug => ug.GroupId == farmerGroup.Id)
                        .Select(ug => ug.UserId)
                        .Distinct()
                        .Count()
                    : 0;

                var sponsorUsers = sponsorGroup != null
                    ? _userGroupRepository.Query()
                        .Where(ug => ug.GroupId == sponsorGroup.Id)
                        .Select(ug => ug.UserId)
                        .Distinct()
                        .Count()
                    : 0;

                var stats = new UserStatisticsDto
                {
                    TotalUsers = allUsers.Count(),
                    ActiveUsers = allUsers.Count(u => u.IsActive && u.Status),
                    InactiveUsers = allUsers.Count(u => !u.IsActive || !u.Status),
                    FarmerUsers = farmerUsers,
                    SponsorUsers = sponsorUsers,
                    AdminUsers = adminUsers,
                    UsersRegisteredToday = allUsers.Count(u => u.RecordDate.Date == DateTime.Now.Date),
                    UsersRegisteredThisWeek = allUsers.Count(u => u.RecordDate >= DateTime.Now.AddDays(-7)),
                    UsersRegisteredThisMonth = allUsers.Count(u => u.RecordDate >= DateTime.Now.AddDays(-30)),
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GeneratedAt = DateTime.Now
                };

                return new SuccessDataResult<UserStatisticsDto>(stats, "User statistics retrieved successfully");
            }
        }
    }

    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int FarmerUsers { get; set; }
        public int SponsorUsers { get; set; }
        public int AdminUsers { get; set; }
        public int UsersRegisteredToday { get; set; }
        public int UsersRegisteredThisWeek { get; set; }
        public int UsersRegisteredThisMonth { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
