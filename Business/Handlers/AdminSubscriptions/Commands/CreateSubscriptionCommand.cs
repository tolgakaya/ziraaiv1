using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.AdminSubscriptions.Commands
{
    /// <summary>
    /// Command to create a new subscription for a user
    /// Admin-only operation with audit logging
    /// </summary>
    public class CreateSubscriptionCommand : IRequest<IDataResult<int>>
    {
        public int UserId { get; set; }
        public int SubscriptionTierId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsTrial { get; set; } = false;
        public bool AutoRenew { get; set; } = false;
        public string Reason { get; set; }
        public int AdminUserId { get; set; }
        public HttpContext HttpContext { get; set; }

        public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, IDataResult<int>>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IUserRepository _userRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly IAdminAuditService _adminAuditService;

            public CreateSubscriptionCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IUserRepository userRepository,
                ISubscriptionTierRepository tierRepository,
                IAdminAuditService adminAuditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _userRepository = userRepository;
                _tierRepository = tierRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<int>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
            {
                // Validate reason
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
                {
                    return new ErrorDataResult<int>("Reason is required (minimum 10 characters)");
                }

                // Verify user exists
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorDataResult<int>("User not found");
                }

                // Verify tier exists
                var tier = await _tierRepository.GetAsync(t => t.Id == request.SubscriptionTierId);
                if (tier == null)
                {
                    return new ErrorDataResult<int>("Subscription tier not found");
                }

                // Validate dates
                if (request.EndDate <= request.StartDate)
                {
                    return new ErrorDataResult<int>("End date must be after start date");
                }

                // Check for overlapping active subscriptions
                var hasActiveSubscription = await _subscriptionRepository.GetAsync(s =>
                    s.UserId == request.UserId &&
                    s.IsActive &&
                    s.Status);

                if (hasActiveSubscription != null)
                {
                    return new ErrorDataResult<int>(
                        $"User already has an active subscription (ID: {hasActiveSubscription.Id}). " +
                        "Please deactivate it first or use extend/upgrade commands.");
                }

                // Create new subscription
                var subscription = new UserSubscription
                {
                    UserId = request.UserId,
                    SubscriptionTierId = request.SubscriptionTierId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsActive = true,
                    Status = true,
                    IsTrial = request.IsTrial,
                    TrialEndDate = request.IsTrial ? request.EndDate : null,
                    AutoRenew = request.AutoRenew,
                    DailyRequestLimit = tier.DailyRequestLimit,
                    MonthlyRequestLimit = tier.MonthlyRequestLimit,
                    DailyRequestCount = 0,
                    MonthlyRequestCount = 0,
                    LastDailyReset = DateTime.Now,
                    LastMonthlyReset = DateTime.Now,
                    CreatedDate = DateTime.Now
                };

                await _subscriptionRepository.AddAsync(subscription);

                // Log admin operation
                await _adminAuditService.LogSubscriptionManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: "CREATE_SUBSCRIPTION",
                    httpContext: request.HttpContext,
                    targetUserId: request.UserId,
                    subscriptionId: subscription.Id,
                    reason: request.Reason,
                    beforeState: null,
                    afterState: new
                    {
                        subscription.Id,
                        subscription.UserId,
                        subscription.SubscriptionTierId,
                        TierName = tier.Name,
                        subscription.StartDate,
                        subscription.EndDate,
                        subscription.IsTrial,
                        subscription.AutoRenew
                    });

                return new SuccessDataResult<int>(
                    subscription.Id,
                    $"Subscription created successfully for user {user.FullName}");
            }
        }
    }
}
