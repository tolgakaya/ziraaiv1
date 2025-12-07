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

        /// <summary>
        /// Force activation: Cancel existing active sponsorship and activate new one immediately
        /// Default (false): Queue new sponsorship if active sponsorship exists
        /// </summary>
        public bool ForceActivation { get; set; } = false;

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
            private readonly Business.Services.AdminAnalytics.IAdminStatisticsCacheService _adminCacheService;

            public AssignSubscriptionCommandHandler(
                IUserSubscriptionRepository subscriptionRepository,
                ISubscriptionTierRepository tierRepository,
                IAdminAuditService auditService,
                Business.Services.AdminAnalytics.IAdminStatisticsCacheService adminCacheService)
            {
                _subscriptionRepository = subscriptionRepository;
                _tierRepository = tierRepository;
                _auditService = auditService;
                _adminCacheService = adminCacheService;
            }

            public async Task<IResult> Handle(AssignSubscriptionCommand request, CancellationToken cancellationToken)
            {
                // Validate tier exists
                var tier = await _tierRepository.GetAsync(t => t.Id == request.SubscriptionTierId);
                if (tier == null)
                {
                    return new ErrorResult("Subscription tier not found");
                }

                var now = DateTime.Now;

                // ✅ FIX: Check for ANY active subscription (sponsored OR regular OR trial)
                var existingActiveSubscription = await _subscriptionRepository.GetAsync(s =>
                    s.UserId == request.UserId &&
                    s.IsActive &&
                    s.Status == "Active" &&
                    s.EndDate > now);

                if (existingActiveSubscription != null)
                {
                    // If existing subscription is Trial, always replace it with new subscription
                    if (existingActiveSubscription.IsTrialSubscription)
                    {
                        existingActiveSubscription.IsActive = false;
                        existingActiveSubscription.Status = "Cancelled";
                        existingActiveSubscription.QueueStatus = SubscriptionQueueStatus.Cancelled;
                        existingActiveSubscription.EndDate = now;
                        existingActiveSubscription.UpdatedDate = now;

                        _subscriptionRepository.Update(existingActiveSubscription);

                        // Create new active subscription (trial replaced)
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
                            action: "AssignSubscription_ReplaceTrial",
                            adminUserId: request.AdminUserId,
                            targetUserId: request.UserId,
                            entityType: "UserSubscription",
                            entityId: subscription.Id,
                            isOnBehalfOf: false,
                            ipAddress: request.IpAddress,
                            userAgent: request.UserAgent,
                            requestPath: request.RequestPath,
                            reason: $"Replaced trial subscription with {tier.TierName} for {request.DurationMonths} months (cancelled subscription {existingActiveSubscription.Id})",
                            afterState: new {
                                NewSubscription = new { subscription.Id, subscription.SubscriptionTierId, subscription.StartDate, subscription.EndDate },
                                CancelledTrial = new { existingActiveSubscription.Id, existingActiveSubscription.EndDate }
                            }
                        );

                        // Invalidate admin statistics cache
                        await _adminCacheService.InvalidateAllStatisticsAsync();

                        return new SuccessResult($"Trial subscription replaced. New {tier.TierName} subscription activated. Valid until {subscription.EndDate:yyyy-MM-dd}");
                    }
                    // If existing subscription is sponsored and new is also sponsored
                    else if (existingActiveSubscription.IsSponsoredSubscription && request.IsSponsoredSubscription)
                    {
                        // OPTION 1: Force Activation (Cancel existing, activate new immediately)
                        if (request.ForceActivation)
                        {
                            return await HandleForceActivation(request, tier, existingActiveSubscription, now);
                        }
                        // OPTION 2: Queue the new sponsorship (default behavior)
                        else
                        {
                            return await HandleQueueSponsorship(request, tier, existingActiveSubscription, now);
                        }
                    }
                    // Existing is regular paid subscription (not trial, not sponsored)
                    else
                    {
                        return new ErrorResult($"User already has an active {existingActiveSubscription.SubscriptionTier?.TierName ?? "subscription"} (ID: {existingActiveSubscription.Id}, expires: {existingActiveSubscription.EndDate:yyyy-MM-dd}). Use ForceActivation=true to replace it.");
                    }
                }

                // No active subscription: Activate immediately
                return await HandleImmediateActivation(request, tier, now);
            }

            /// <summary>
            /// Cancel existing active sponsorship and activate new one immediately
            /// </summary>
            private async Task<IResult> HandleForceActivation(
                AssignSubscriptionCommand request,
                SubscriptionTier tier,
                UserSubscription activeSponsorship,
                DateTime now)
            {
                // Cancel existing sponsorship
                activeSponsorship.IsActive = false;
                activeSponsorship.Status = "Cancelled";
                activeSponsorship.QueueStatus = SubscriptionQueueStatus.Cancelled;
                activeSponsorship.EndDate = now; // Terminate immediately
                activeSponsorship.UpdatedDate = now;

                _subscriptionRepository.Update(activeSponsorship);

                // Create new active subscription
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
                    action: "AssignSubscription_ForceActivation",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.UserId,
                    entityType: "UserSubscription",
                    entityId: subscription.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Force activated {tier.TierName} subscription for {request.DurationMonths} months (cancelled subscription {activeSponsorship.Id})",
                    afterState: new {
                        NewSubscription = new { subscription.Id, subscription.SubscriptionTierId, subscription.StartDate, subscription.EndDate },
                        CancelledSubscription = new { activeSponsorship.Id, activeSponsorship.EndDate }
                    }
                );

                // Invalidate admin statistics cache (subscription changed)
                await _adminCacheService.InvalidateAllStatisticsAsync();

                return new SuccessResult($"Previous sponsorship cancelled. New {tier.TierName} subscription activated. Valid until {subscription.EndDate:yyyy-MM-dd}");
            }

            /// <summary>
            /// Queue new sponsorship to activate when current expires
            /// </summary>
            private async Task<IResult> HandleQueueSponsorship(
                AssignSubscriptionCommand request,
                SubscriptionTier tier,
                UserSubscription activeSponsorship,
                DateTime now)
            {
                // Create queued subscription
                var subscription = new UserSubscription
                {
                    UserId = request.UserId,
                    SubscriptionTierId = request.SubscriptionTierId,
                    StartDate = DateTime.MinValue, // Will be set on activation (using MinValue as placeholder)
                    EndDate = DateTime.MinValue, // Will be set on activation (using MinValue as placeholder)
                    IsActive = false, // Not active yet
                    Status = "Pending", // Queued status
                    IsSponsoredSubscription = request.IsSponsoredSubscription,
                    SponsorId = request.SponsorId,
                    SponsorshipNotes = request.Notes,
                    IsTrialSubscription = false,
                    CreatedDate = now,
                    CreatedUserId = request.AdminUserId,
                    QueueStatus = SubscriptionQueueStatus.Pending,
                    QueuedDate = now,
                    PreviousSponsorshipId = activeSponsorship.Id,
                    ActivatedDate = null
                };

                _subscriptionRepository.Add(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    action: "AssignSubscription_Queued",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.UserId,
                    entityType: "UserSubscription",
                    entityId: subscription.Id,
                    isOnBehalfOf: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Queued {tier.TierName} subscription for {request.DurationMonths} months (will activate after subscription {activeSponsorship.Id} expires)",
                    afterState: new {
                        subscription.Id,
                        subscription.SubscriptionTierId,
                        subscription.QueueStatus,
                        subscription.PreviousSponsorshipId,
                        EstimatedActivation = activeSponsorship.EndDate
                    }
                );

                // Invalidate admin statistics cache (subscription changed)
                await _adminCacheService.InvalidateAllStatisticsAsync();

                return new SuccessResult($"Subscription queued successfully. Will activate automatically on {activeSponsorship.EndDate:yyyy-MM-dd} when current sponsorship expires.");
            }

            /// <summary>
            /// Activate subscription immediately (no active sponsorship exists)
            /// </summary>
            private async Task<IResult> HandleImmediateActivation(
                AssignSubscriptionCommand request,
                SubscriptionTier tier,
                DateTime now)
            {
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

                // Invalidate admin statistics cache (subscription assigned)
                await _adminCacheService.InvalidateAllStatisticsAsync();

                return new SuccessResult($"Subscription assigned successfully. Valid until {subscription.EndDate:yyyy-MM-dd}");
            }
        }
    }
}
