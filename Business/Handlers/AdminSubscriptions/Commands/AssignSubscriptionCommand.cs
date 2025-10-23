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
    /// Admin command to assign a subscription to a user
    /// </summary>
    public class AssignSubscriptionCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public int SubscriptionTierId { get; set; }
        public int DurationMonths { get; set; }
        public bool IsSponsoredSubscription { get; set; }
        public int? SponsorId { get; set; }
        public string Notes { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class AssignSubscriptionCommandHandler : IRequestHandler<AssignSubscriptionCommand, IResult>
        {
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly IAdminAuditService _auditService;

            public AssignSubscriptionCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                ISubscriptionTierRepository tierRepository,
                IAdminAuditService auditService)
            {
                _subscriptionRepository = subscriptionRepository;
                _tierRepository = tierRepository;
                _auditService = auditService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(AssignSubscriptionCommand request, CancellationToken cancellationToken)
            {
                // Validate tier exists
                var tier = await _tierRepository.GetAsync(t => t.Id == request.SubscriptionTierId);
                if (tier == null)
                {
                    return new ErrorResult("Subscription tier not found");
                }

                var now = DateTime.Now;
                var subscription = new UserSubscription
                {
                    UserId = request.UserId,
                    SubscriptionTierId = request.SubscriptionTierId,
                    StartDate = now,
                    EndDate = now.AddMonths(request.DurationMonths),
                    IsActive = true,
                    Status = "Active",
                    IsSponsoredSubscription = request.IsSponsoredSubscription,
                    SponsorId = request.SponsorId,
                    SponsorshipNotes = request.Notes,
                    IsTrialSubscription = false,
                    CreatedDate = now,
                    CreatedUserId = request.AdminUserId,
                    QueueStatus = SubscriptionQueueStatus.Active,
                    ActivatedDate = now
                };

                _subscriptionRepository.Add(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "AssignSubscription",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.UserId,
                    entityType: "UserSubscription",
                    entityId: subscription.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Assigned {tier.TierName} subscription for {request.DurationMonths} months",
                    afterState: new { subscription.Id, subscription.SubscriptionTierId, subscription.StartDate, subscription.EndDate }
                );

                return new SuccessResult($"Subscription assigned successfully. Valid until {subscription.EndDate:yyyy-MM-dd}");
            }
        }
    }
}
