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

namespace Business.Handlers.AdminUsers.Queries
{
    /// <summary>
    /// Query to get detailed user information by ID
    /// Admin-only operation with full user details including roles and claims
    /// </summary>
    public class GetUserByIdQuery : IRequest<IDataResult<UserDetailDto>>
    {
        public int UserId { get; set; }

        public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, IDataResult<UserDetailDto>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;

            public GetUserByIdQueryHandler(
                IUserRepository userRepository,
                IUserSubscriptionRepository userSubscriptionRepository,
                IPlantAnalysisRepository plantAnalysisRepository)
            {
                _userRepository = userRepository;
                _userSubscriptionRepository = userSubscriptionRepository;
                _plantAnalysisRepository = plantAnalysisRepository;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorDataResult<UserDetailDto>("User not found");
                }

                // Get user roles and claims
                var roles = await _userRepository.GetUserGroupsAsync(user.UserId);
                var claims = await _userRepository.GetClaimsAsync(user.UserId);

                // Get active subscription
                var activeSubscription = await _userSubscriptionRepository.GetAsync(s =>
                    s.UserId == user.UserId &&
                    s.IsActive &&
                    s.Status);

                // Get usage statistics
                var totalAnalyses = await _plantAnalysisRepository.GetCountAsync(p => p.UserId == user.UserId);

                // Get deactivated by admin name if applicable
                string deactivatedByName = null;
                if (user.DeactivatedBy.HasValue)
                {
                    var deactivatingAdmin = await _userRepository.GetAsync(u => u.UserId == user.DeactivatedBy.Value);
                    deactivatedByName = deactivatingAdmin?.FullName;
                }

                var userDto = new UserDetailDto
                {
                    UserId = user.UserId,
                    CitizenId = user.CitizenId,
                    FullName = user.FullName,
                    Email = user.Email,
                    MobilePhones = user.MobilePhones,
                    Status = user.Status,
                    BirthDate = user.BirthDate,
                    Gender = user.Gender,
                    RecordDate = user.RecordDate,
                    Address = user.Address,
                    Notes = user.Notes,
                    UpdateContactDate = user.UpdateContactDate,
                    RegistrationReferralCode = user.RegistrationReferralCode,
                    AvatarUrl = user.AvatarUrl,
                    AvatarThumbnailUrl = user.AvatarThumbnailUrl,
                    AvatarUpdatedDate = user.AvatarUpdatedDate,
                    IsActive = user.IsActive,
                    DeactivatedDate = user.DeactivatedDate,
                    DeactivatedBy = user.DeactivatedBy,
                    DeactivatedByName = deactivatedByName,
                    DeactivationReason = user.DeactivationReason,
                    Roles = roles,
                    Claims = claims.Select(c => c.Name).ToList(),
                    ActiveSubscription = activeSubscription != null ? new UserSubscriptionDto
                    {
                        Id = activeSubscription.Id,
                        UserId = activeSubscription.UserId,
                        SubscriptionTierId = activeSubscription.SubscriptionTierId,
                        SubscriptionTierName = activeSubscription.SubscriptionTier?.Name,
                        StartDate = activeSubscription.StartDate,
                        EndDate = activeSubscription.EndDate,
                        IsActive = activeSubscription.IsActive,
                        IsTrial = activeSubscription.IsTrial,
                        DailyRequestLimit = activeSubscription.DailyRequestLimit,
                        MonthlyRequestLimit = activeSubscription.MonthlyRequestLimit,
                        DailyRequestCount = activeSubscription.DailyRequestCount,
                        MonthlyRequestCount = activeSubscription.MonthlyRequestCount
                    } : null,
                    TotalPlantAnalyses = totalAnalyses
                };

                return new SuccessDataResult<UserDetailDto>(userDto, "User details retrieved successfully");
            }
        }
    }
}
