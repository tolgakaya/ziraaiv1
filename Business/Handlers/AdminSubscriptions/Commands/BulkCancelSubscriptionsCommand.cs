using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Admin command to bulk cancel multiple subscriptions
    /// </summary>
    public class BulkCancelSubscriptionsCommand : IRequest<IResult>
    {
        public List<int> SubscriptionIds { get; set; }
        public string CancellationReason { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class BulkCancelSubscriptionsCommandHandler : IRequestHandler<BulkCancelSubscriptionsCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IAdminAuditService _auditService;

            public BulkCancelSubscriptionsCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IAdminAuditService auditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(BulkCancelSubscriptionsCommand request, CancellationToken cancellationToken)
            {
                if (request.SubscriptionIds == null || !request.SubscriptionIds.Any())
                {
                    return new ErrorResult("No subscription IDs provided");
                }

                var subscriptions = _subscriptionRepository.GetList(s => request.SubscriptionIds.Contains(s.Id));

                if (!subscriptions.Any())
                {
                    return new ErrorResult("No subscriptions found with the provided IDs");
                }

                var cancelledCount = 0;
                var alreadyCancelled = 0;

                foreach (var subscription in subscriptions)
                {
                    if (!subscription.IsActive)
                    {
                        alreadyCancelled++;
                        continue;
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
                    subscription.EndDate = DateTime.Now;
                    subscription.QueueStatus = SubscriptionQueueStatus.Cancelled;

                    _subscriptionRepository.Update(subscription);
                    cancelledCount++;

                    // Audit log for each subscription
                    await _auditService.LogAsync(
                        action: "BulkCancelSubscription",
                        adminUserId: request.AdminUserId,
                        targetUserId: subscription.UserId,
                        entityType: "UserSubscription",
                        entityId: subscription.Id,
                        isOnBehalfOf: false,
                        ipAddress: request.IpAddress,
                        userAgent: request.UserAgent,
                        requestPath: request.RequestPath,
                        reason: request.CancellationReason,
                        beforeState: beforeState,
                        afterState: new
                        {
                            subscription.IsActive,
                            subscription.Status,
                            subscription.EndDate,
                            subscription.QueueStatus
                        }
                    );
                }

                await _subscriptionRepository.SaveChangesAsync();

                return new SuccessResult($"Bulk cancellation completed. Cancelled: {cancelledCount}, Already inactive: {alreadyCancelled}");
            }
        }
    }
}
