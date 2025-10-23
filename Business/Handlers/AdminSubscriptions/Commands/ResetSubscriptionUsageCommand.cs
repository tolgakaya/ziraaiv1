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
    /// Command to reset subscription usage counters
    /// Admin-only operation for manual usage reset
    /// </summary>
    public class ResetSubscriptionUsageCommand : IRequest<IResult>
    {
        public int SubscriptionId { get; set; }
        public bool ResetDaily { get; set; } = true;
        public bool ResetMonthly { get; set; } = false;
        public string Reason { get; set; }
        public int AdminUserId { get; set; }
        public HttpContext HttpContext { get; set; }

        public class ResetSubscriptionUsageCommandHandler : IRequestHandler<ResetSubscriptionUsageCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IAdminAuditService _adminAuditService;

            public ResetSubscriptionUsageCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IAdminAuditService adminAuditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(ResetSubscriptionUsageCommand request, CancellationToken cancellationToken)
            {
                // Validate reason
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
                {
                    return new ErrorResult("Reason is required (minimum 10 characters)");
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
                    subscription.DailyRequestCount,
                    subscription.MonthlyRequestCount,
                    subscription.LastDailyReset,
                    subscription.LastMonthlyReset
                };

                // Reset usage
                if (request.ResetDaily)
                {
                    subscription.DailyRequestCount = 0;
                    subscription.LastDailyReset = DateTime.Now;
                }

                if (request.ResetMonthly)
                {
                    subscription.MonthlyRequestCount = 0;
                    subscription.LastMonthlyReset = DateTime.Now;
                }

                subscription.UpdatedDate = DateTime.Now;

                _subscriptionRepository.Update(subscription);

                // Log admin operation
                await _adminAuditService.LogSubscriptionManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: "RESET_SUBSCRIPTION_USAGE",
                    httpContext: request.HttpContext,
                    targetUserId: subscription.UserId,
                    subscriptionId: subscription.Id,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: new
                    {
                        subscription.DailyRequestCount,
                        subscription.MonthlyRequestCount,
                        subscription.LastDailyReset,
                        subscription.LastMonthlyReset
                    });

                var resetType = request.ResetDaily && request.ResetMonthly 
                    ? "Daily and monthly usage" 
                    : request.ResetDaily 
                        ? "Daily usage" 
                        : "Monthly usage";

                return new SuccessResult($"{resetType} reset successfully");
            }
        }
    }
}
