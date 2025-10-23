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
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSubscriptions.Queries
{
    /// <summary>
    /// Query to get all subscriptions for a specific user
    /// Admin-only operation for user subscription history
    /// </summary>
    public class GetUserSubscriptionsQuery : IRequest<IDataResult<List<UserSubscriptionDto>>>
    {
        public int UserId { get; set; }
        public bool IncludeInactive { get; set; } = true;

        public class GetUserSubscriptionsQueryHandler : IRequestHandler<GetUserSubscriptionsQuery, IDataResult<List<UserSubscriptionDto>>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IUserRepository _userRepository;

            public GetUserSubscriptionsQueryHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IUserRepository userRepository)
            {
                _subscriptionRepository = subscriptionRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<UserSubscriptionDto>>> Handle(GetUserSubscriptionsQuery request, CancellationToken cancellationToken)
            {
                // Verify user exists
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorDataResult<List<UserSubscriptionDto>>("User not found");
                }

                var query = _subscriptionRepository.Query()
                    .Where(s => s.UserId == request.UserId)
                    .Include(s => s.SubscriptionTier);

                if (!request.IncludeInactive)
                {
                    query = query.Where(s => s.IsActive);
                }

                var subscriptions = await query
                    .OrderByDescending(s => s.StartDate)
                    .ToListAsync(cancellationToken);

                var subscriptionDtos = subscriptions.Select(s => new UserSubscriptionDto
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    SubscriptionTierId = s.SubscriptionTierId,
                    TierName = s.SubscriptionTier?.Name,
                    TierDisplayName = s.SubscriptionTier?.DisplayName,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    IsActive = s.IsActive,
                    AutoRenew = s.AutoRenew,
                    Status = s.Status ? "Active" : "Inactive",
                    CurrentDailyUsage = s.DailyRequestCount,
                    DailyRequestLimit = s.DailyRequestLimit,
                    CurrentMonthlyUsage = s.MonthlyRequestCount,
                    MonthlyRequestLimit = s.MonthlyRequestLimit,
                    LastUsageResetDate = s.LastDailyReset,
                    MonthlyUsageResetDate = s.LastMonthlyReset,
                    IsTrialSubscription = s.IsTrial,
                    TrialEndDate = s.TrialEndDate
                }).ToList();

                return new SuccessDataResult<List<UserSubscriptionDto>>(
                    subscriptionDtos,
                    $"Retrieved {subscriptionDtos.Count} subscriptions for user {user.FullName}");
            }
        }
    }
}
