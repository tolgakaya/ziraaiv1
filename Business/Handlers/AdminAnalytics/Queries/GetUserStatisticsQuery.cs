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

            public GetUserStatisticsQueryHandler(IUserRepository userRepository)
            {
                _userRepository = userRepository;
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

                var stats = new UserStatisticsDto
                {
                    TotalUsers = allUsers.Count(),
                    ActiveUsers = allUsers.Count(u => u.IsActive && u.Status),
                    InactiveUsers = allUsers.Count(u => !u.IsActive || !u.Status),
                    // Role-based counts would require joining with UserOperationClaim table
                    // Simplified to just count active/inactive for now
                    FarmerUsers = 0, // TODO: Implement with proper join
                    SponsorUsers = 0, // TODO: Implement with proper join
                    AdminUsers = 0, // TODO: Implement with proper join
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
