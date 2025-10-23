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
    /// Query to get detailed subscription information by ID
    /// Admin-only operation with full subscription details and usage history
    /// </summary>
    public class GetSubscriptionByIdQuery : IRequest<IDataResult<SubscriptionDetailDto>>
    {
        public int SubscriptionId { get; set; }

        public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, IDataResult<SubscriptionDetailDto>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly ISubscriptionUsageLogRepository _usageLogRepository;
            private readonly IUserRepository _userRepository;

            public GetSubscriptionByIdQueryHandler(
                IUserSubscriptionRepository subscriptionRepository,
                ISubscriptionUsageLogRepository usageLogRepository,
                IUserRepository userRepository)
            {
                _subscriptionRepository = subscriptionRepository;
                _usageLogRepository = usageLogRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SubscriptionDetailDto>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
            {
                var subscription = await _subscriptionRepository.Query()
                    .Include(s => s.SubscriptionTier)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);

                if (subscription == null)
                {
                    return new ErrorDataResult<SubscriptionDetailDto>("Subscription not found");
                }

                // Get usage logs for last 30 days
                var thirtyDaysAgo = System.DateTime.Now.AddDays(-30);
                var usageLogs = await _usageLogRepository.Query()
                    .Where(l => l.SubscriptionId == subscription.Id && l.RequestDate >= thirtyDaysAgo)
                    .OrderByDescending(l => l.RequestDate)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                // Get total usage count
                var totalUsageCount = await _usageLogRepository.Query()
                    .CountAsync(l => l.SubscriptionId == subscription.Id, cancellationToken);

                var detailDto = new SubscriptionDetailDto
                {
                    Id = subscription.Id,
                    UserId = subscription.UserId,
                    UserName = subscription.User?.FullName,
                    UserEmail = subscription.User?.Email,
                    SubscriptionTierId = subscription.SubscriptionTierId,
                    TierName = subscription.SubscriptionTier?.Name,
                    TierDisplayName = subscription.SubscriptionTier?.DisplayName,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    AutoRenew = subscription.AutoRenew,
                    Status = subscription.Status ? "Active" : "Inactive",
                    CurrentDailyUsage = subscription.DailyRequestCount,
                    DailyRequestLimit = subscription.DailyRequestLimit,
                    CurrentMonthlyUsage = subscription.MonthlyRequestCount,
                    MonthlyRequestLimit = subscription.MonthlyRequestLimit,
                    LastUsageResetDate = subscription.LastDailyReset,
                    MonthlyUsageResetDate = subscription.LastMonthlyReset,
                    IsTrialSubscription = subscription.IsTrial,
                    TrialEndDate = subscription.TrialEndDate,
                    CreatedDate = subscription.CreatedDate,
                    UpdatedDate = subscription.UpdatedDate,
                    TotalUsageCount = totalUsageCount,
                    RecentUsageLogs = usageLogs.Select(l => new UsageLogDto
                    {
                        Id = l.Id,
                        RequestDate = l.RequestDate,
                        RequestType = l.RequestType,
                        IsSuccessful = l.IsSuccessful,
                        ResponseTime = l.ResponseTime
                    }).ToList()
                };

                return new SuccessDataResult<SubscriptionDetailDto>(detailDto, "Subscription details retrieved successfully");
            }
        }
    }

    /// <summary>
    /// Detailed subscription DTO with usage history
    /// </summary>
    public class SubscriptionDetailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public int SubscriptionTierId { get; set; }
        public string TierName { get; set; }
        public string TierDisplayName { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool AutoRenew { get; set; }
        public string Status { get; set; }
        public int CurrentDailyUsage { get; set; }
        public int DailyRequestLimit { get; set; }
        public int CurrentMonthlyUsage { get; set; }
        public int MonthlyRequestLimit { get; set; }
        public System.DateTime? LastUsageResetDate { get; set; }
        public System.DateTime? MonthlyUsageResetDate { get; set; }
        public bool IsTrialSubscription { get; set; }
        public System.DateTime? TrialEndDate { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public System.DateTime? UpdatedDate { get; set; }
        public int TotalUsageCount { get; set; }
        public System.Collections.Generic.List<UsageLogDto> RecentUsageLogs { get; set; }
    }

    /// <summary>
    /// Usage log DTO
    /// </summary>
    public class UsageLogDto
    {
        public int Id { get; set; }
        public System.DateTime RequestDate { get; set; }
        public string RequestType { get; set; }
        public bool IsSuccessful { get; set; }
        public int? ResponseTime { get; set; }
    }
}
