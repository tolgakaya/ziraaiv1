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

namespace Business.Handlers.AdminSubscriptions.Commands
{
    /// <summary>
    /// Admin command to cancel an active subscription
    /// </summary>
    public class CancelSubscriptionCommand : IRequest<IResult>
    {
        public int SubscriptionId { get; set; }
        public string CancellationReason { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IAdminAuditService _auditService;

            public CancelSubscriptionCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IAdminAuditService auditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
            {
                var subscription = await _subscriptionRepository.GetAsync(s => s.Id == request.SubscriptionId);
                if (subscription == null)
                {
                    return new ErrorResult("Subscription not found");
                }

                if (!subscription.IsActive)
                {
                    return new ErrorResult("Subscription is already inactive");
                }

                var beforeState = new
                {
                    subscription.IsActive,
                    subscription.Status,
                    subscription.EndDate,
                    subscription.QueueStatus
                };

                subscription.IsActive = false;
                subscription.Status = "Cancelled";
                subscription.EndDate = DateTime.Now; // End immediately
                subscription.QueueStatus = SubscriptionQueueStatus.Cancelled;

                _subscriptionRepository.Update(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "CancelSubscription",
                    adminUserId: request.AdminUserId,
                    targetUserId: subscription.UserId,
                    entityType: "UserSubscription",
                    entityId: subscription.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: request.CancellationReason ?? "Admin cancelled subscription",
                    beforeState: beforeState,
                    afterState: new
                    {
                        subscription.IsActive,
                        subscription.Status,
                        subscription.EndDate,
                        subscription.QueueStatus
                    }
                );

                return new SuccessResult("Subscription cancelled successfully");
            }
        }
    }
}
