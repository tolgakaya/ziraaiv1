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
    /// Command to cancel/deactivate a subscription
    /// Admin-only operation with mandatory reason
    /// </summary>
    public class CancelSubscriptionCommand : IRequest<IResult>
    {
        public int SubscriptionId { get; set; }
        public string Reason { get; set; }
        public bool ImmediateCancel { get; set; } = false;  // If true, cancel immediately; else, prevent renewal
        public int AdminUserId { get; set; }
        public HttpContext HttpContext { get; set; }

        public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IAdminAuditService _adminAuditService;

            public CancelSubscriptionCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IAdminAuditService adminAuditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
            {
                // Validate reason
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
                {
                    return new ErrorResult("Cancellation reason is required (minimum 10 characters)");
                }

                // Get subscription
                var subscription = await _subscriptionRepository.GetAsync(s => s.Id == request.SubscriptionId);
                if (subscription == null)
                {
                    return new ErrorResult("Subscription not found");
                }

                if (!subscription.IsActive)
                {
                    return new ErrorResult("Subscription is already cancelled");
                }

                // Save before state
                var beforeState = new
                {
                    subscription.IsActive,
                    subscription.Status,
                    subscription.AutoRenew,
                    subscription.EndDate
                };

                // Cancel subscription
                if (request.ImmediateCancel)
                {
                    // Immediate cancellation: deactivate now
                    subscription.IsActive = false;
                    subscription.Status = false;
                    subscription.EndDate = DateTime.Now;
                }

                // Always disable auto-renew on cancellation
                subscription.AutoRenew = false;
                subscription.UpdatedDate = DateTime.Now;

                _subscriptionRepository.Update(subscription);

                // Log admin operation
                await _adminAuditService.LogSubscriptionManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: request.ImmediateCancel ? "CANCEL_SUBSCRIPTION_IMMEDIATE" : "CANCEL_SUBSCRIPTION_AT_END",
                    httpContext: request.HttpContext,
                    targetUserId: subscription.UserId,
                    subscriptionId: subscription.Id,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: new
                    {
                        subscription.IsActive,
                        subscription.Status,
                        subscription.AutoRenew,
                        subscription.EndDate
                    });

                var message = request.ImmediateCancel
                    ? "Subscription cancelled immediately"
                    : "Subscription will not renew (cancelled at end of current period)";

                return new SuccessResult(message);
            }
        }
    }
}
