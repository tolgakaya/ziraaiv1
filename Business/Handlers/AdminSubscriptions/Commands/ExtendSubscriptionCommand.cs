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

namespace Business.Handlers.AdminSubscriptions.Commands
{
    /// <summary>
    /// Admin command to extend an existing subscription
    /// </summary>
    public class ExtendSubscriptionCommand : IRequest<IResult>
    {
        public int SubscriptionId { get; set; }
        public int ExtensionMonths { get; set; }
        public string Notes { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class ExtendSubscriptionCommandHandler : IRequestHandler<ExtendSubscriptionCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IAdminAuditService _auditService;

            public ExtendSubscriptionCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IAdminAuditService auditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(ExtendSubscriptionCommand request, CancellationToken cancellationToken)
            {
                var subscription = await _subscriptionRepository.GetAsync(s => s.Id == request.SubscriptionId);
                if (subscription == null)
                {
                    return new ErrorResult("Subscription not found");
                }

                var beforeState = new
                {
                    subscription.EndDate,
                    subscription.Status,
                    subscription.IsActive
                };

                var originalEndDate = subscription.EndDate;
                subscription.EndDate = subscription.EndDate.AddMonths(request.ExtensionMonths);

                // If subscription was expired, reactivate it
                if (!subscription.IsActive || subscription.Status != "Active")
                {
                    subscription.IsActive = true;
                    subscription.Status = "Active";
                }

                _subscriptionRepository.Update(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "ExtendSubscription",
                    adminUserId: request.AdminUserId,
                    targetUserId: subscription.UserId,
                    entityType: "UserSubscription",
                    entityId: subscription.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Extended subscription by {request.ExtensionMonths} months{(string.IsNullOrEmpty(request.Notes) ? "" : $": {request.Notes}")}",
                    beforeState: beforeState,
                    afterState: new { subscription.EndDate, subscription.Status, subscription.IsActive }
                );

                return new SuccessResult($"Subscription extended from {originalEndDate:yyyy-MM-dd} to {subscription.EndDate:yyyy-MM-dd}");
            }
        }
    }
}
