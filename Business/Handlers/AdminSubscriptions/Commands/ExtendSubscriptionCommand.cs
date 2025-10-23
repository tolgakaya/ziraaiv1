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
    /// Command to extend an existing subscription's end date
    /// Admin-only operation with audit logging
    /// </summary>
    public class ExtendSubscriptionCommand : IRequest<IResult>
    {
        public int SubscriptionId { get; set; }
        public DateTime NewEndDate { get; set; }
        public string Reason { get; set; }
        public int AdminUserId { get; set; }
        public HttpContext HttpContext { get; set; }

        public class ExtendSubscriptionCommandHandler : IRequestHandler<ExtendSubscriptionCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IAdminAuditService _adminAuditService;

            public ExtendSubscriptionCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                IAdminAuditService adminAuditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _adminAuditService = adminAuditService;
            }

            [SecuredOperation(Priority = 1, Roles = "Admin")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(ExtendSubscriptionCommand request, CancellationToken cancellationToken)
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

                // Validate new end date
                if (request.NewEndDate <= subscription.EndDate)
                {
                    return new ErrorResult("New end date must be after current end date");
                }

                // Save before state
                var beforeState = new
                {
                    subscription.EndDate,
                    subscription.UpdatedDate
                };

                // Extend subscription
                subscription.EndDate = request.NewEndDate;
                subscription.UpdatedDate = DateTime.Now;

                // If trial, also update trial end date
                if (subscription.IsTrial && subscription.TrialEndDate.HasValue)
                {
                    subscription.TrialEndDate = request.NewEndDate;
                }

                _subscriptionRepository.Update(subscription);

                // Log admin operation
                await _adminAuditService.LogSubscriptionManagementAsync(
                    adminUserId: request.AdminUserId,
                    action: "EXTEND_SUBSCRIPTION",
                    httpContext: request.HttpContext,
                    targetUserId: subscription.UserId,
                    subscriptionId: subscription.Id,
                    reason: request.Reason,
                    beforeState: beforeState,
                    afterState: new
                    {
                        subscription.EndDate,
                        subscription.UpdatedDate,
                        subscription.TrialEndDate
                    });

                return new SuccessResult($"Subscription extended to {request.NewEndDate:yyyy-MM-dd} successfully");
            }
        }
    }
}
