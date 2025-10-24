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
            private readonly IUserClaimRepository _userClaimRepository;
            private readonly IOperationClaimRepository _operationClaimRepository;

            public GetUserStatisticsQueryHandler(
                IUserRepository userRepository,
                IUserClaimRepository userClaimRepository,
                IOperationClaimRepository operationClaimRepository)
            {
                _userRepository = userRepository;
                _userClaimRepository = userClaimRepository;
                _operationClaimRepository = operationClaimRepository;
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

                // Get role-based counts
                var adminClaimId = _operationClaimRepository.Query()
                    .Where(c => c.Name == "Admin")
                    .Select(c => c.Id)
                    .FirstOrDefault();

                var farmerClaimId = _operationClaimRepository.Query()
                    .Where(c => c.Name == "Farmer")
                    .Select(c => c.Id)
                    .FirstOrDefault();

                var sponsorClaimId = _operationClaimRepository.Query()
                    .Where(c => c.Name == "Sponsor")
                    .Select(c => c.Id)
                    .FirstOrDefault();

                var adminUsers = _userClaimRepository.Query()
                    .Where(uc => uc.ClaimId == adminClaimId)
                    .Select(uc => uc.UserId)
                    .Distinct()
                    .Count();

                var farmerUsers = _userClaimRepository.Query()
                    .Where(uc => uc.ClaimId == farmerClaimId)
                    .Select(uc => uc.UserId)
                    .Distinct()
                    .Count();

                var sponsorUsers = _userClaimRepository.Query()
                    .Where(uc => uc.ClaimId == sponsorClaimId)
                    .Select(uc => uc.UserId)
                    .Distinct()
                    .Count();

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
