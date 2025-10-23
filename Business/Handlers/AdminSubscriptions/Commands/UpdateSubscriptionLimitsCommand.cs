using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.AdminAudit;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.AdminSubscriptions.Commands
{
    /// <summary>
    /// Command to update subscription usage limits (override tier defaults)
    /// Admin-only operation for custom limit adjustments
    /// </summary>
    public class UpdateSubscriptionLimitsCommand : IRequest<IResult>
    {
        public int SubscriptionId { get; set; }
        public int? DailyRequestLimit { get; set; }
        public int? MonthlyRequestLimit { get; set; }
        public string Reason { get; set; }
        public int AdminUserId { get; set; }
        public HttpContext HttpContext { get; set; }

        public class UpdateSubscriptionLimitsCommandHandler : IRequestHandler<UpdateSubscriptionLimitsCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IAdminAuditService _adminAuditService;

            public UpdateSubscriptionLimitsCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IAdminAuditService adminAuditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UpdateSubscriptionLimitsCommand request, CancellationToken cancellationToken)
            {
                // Validate reason
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
                {
                    return new ErrorResult("Reason is required (minimum 10 characters)");
                }

                // At least one limit must be provided
                if (!request.DailyRequestLimit.HasValue && !request.MonthlyRequestLimit.HasValue)
                {
                    return new ErrorResult("At least one limit (daily or monthly) must be specified");
                }

                // Get subscription
                var subscription = await _subscriptionRepository.GetAsync(s => s.Id == request.SubscriptionId);
                if (subscription == null)
                {
                    return new ErrorResult("Subscription not found");
                }

                // Save before state
                var beforeState = new
                {
                    subscription.DailyRequestLimit,
                    subscription.MonthlyRequestLimit
                };

                // Update limits
                if (request.DailyRequestLimit.HasValue)
                {
                    subscription.DailyRequestLimit = request.DailyRequestLimit.Value;
                }

                if (request.MonthlyRequestLimit.HasValue)
                {
                    subscription.MonthlyRequestLimit = request.MonthlyRequestLimit.Value;
                }

                subscription.UpdatedDate = DateTime.Now;

                _subscriptionRepository.Update(subscription);

                // Log admin operation
                await _adminAuditService.LogSubscriptionManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: "UPDATE_SUBSCRIPTION_LIMITS",
                    httpContext: request.HttpContext,
                    targetUserId: subscription.UserId,
                    subscriptionId: subscription.Id,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: new
                    {
                        subscription.DailyRequestLimit,
                        subscription.MonthlyRequestLimit
                    });

                return new SuccessResult("Subscription limits updated successfully");
            }
        }
    }
}
