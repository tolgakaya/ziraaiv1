using Business.Services.Subscription;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.Utilities.Results;

namespace WebAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class SubscriptionsController : BaseApiController
    {
        private readonly ISubscriptionValidationService _subscriptionValidationService;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISubscriptionUsageLogRepository _usageLogRepository;

        public SubscriptionsController(
            ISubscriptionValidationService subscriptionValidationService,
            ISubscriptionTierRepository tierRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionUsageLogRepository usageLogRepository)
        {
            _subscriptionValidationService = subscriptionValidationService;
            _tierRepository = tierRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _usageLogRepository = usageLogRepository;
        }

        /// <summary>
        /// Get all available subscription tiers
        /// </summary>
        [HttpGet("tiers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTiers()
        {
            var tiers = await _tierRepository.GetActiveTiersAsync();
            
            var tierDtos = tiers.Select(t => new SubscriptionTierDto
            {
                Id = t.Id,
                TierName = t.TierName,
                DisplayName = t.DisplayName,
                Description = t.Description,
                DailyRequestLimit = t.DailyRequestLimit,
                MonthlyRequestLimit = t.MonthlyRequestLimit,
                MonthlyPrice = t.MonthlyPrice,
                YearlyPrice = t.YearlyPrice,
                Currency = t.Currency,
                PrioritySupport = t.PrioritySupport,
                AdvancedAnalytics = t.AdvancedAnalytics,
                ApiAccess = t.ApiAccess,
                ResponseTimeHours = t.ResponseTimeHours,
                Features = GetFeatures(t.AdditionalFeatures),
                IsActive = t.IsActive
            }).ToList();

            return Ok(new SuccessDataResult<List<SubscriptionTierDto>>(tierDtos));
        }

        /// <summary>
        /// Get current user's subscription status
        /// </summary>
        [HttpGet("my-subscription")]
        [Authorize(Roles = "Farmer,Admin")]  // Fixed: Added Farmer role
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMySubscription()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var subscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId.Value);

            if (subscription == null)
                return NotFound(new ErrorResult("No active subscription found"));

            // Get queued subscriptions (pending sponsorships waiting to activate)
            var queuedSubscriptions = await _userSubscriptionRepository.GetListAsync(
                s => s.UserId == userId.Value &&
                     s.QueueStatus == SubscriptionQueueStatus.Pending &&
                     !s.IsActive);

            var dto = new UserSubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                SubscriptionTierId = subscription.SubscriptionTierId,
                TierName = subscription.SubscriptionTier?.TierName,
                TierDisplayName = subscription.SubscriptionTier?.DisplayName,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                IsActive = subscription.IsActive,
                AutoRenew = subscription.AutoRenew,
                Status = subscription.Status,
                CurrentDailyUsage = subscription.CurrentDailyUsage,
                DailyRequestLimit = subscription.SubscriptionTier?.DailyRequestLimit ?? 0,
                CurrentMonthlyUsage = subscription.CurrentMonthlyUsage,
                MonthlyRequestLimit = subscription.SubscriptionTier?.MonthlyRequestLimit ?? 0,
                LastUsageResetDate = subscription.LastUsageResetDate,
                MonthlyUsageResetDate = subscription.MonthlyUsageResetDate,
                IsTrialSubscription = subscription.IsTrialSubscription,
                TrialEndDate = subscription.TrialEndDate,
                QueueStatus = subscription.QueueStatus,
                QueuedDate = subscription.QueuedDate,
                PreviousSponsorshipId = subscription.PreviousSponsorshipId,
                QueuedSubscriptions = queuedSubscriptions.Select(q => new QueuedSubscriptionDto
                {
                    Id = q.Id,
                    SubscriptionTierId = q.SubscriptionTierId,
                    TierName = q.SubscriptionTier?.TierName,
                    TierDisplayName = q.SubscriptionTier?.DisplayName,
                    QueueStatus = q.QueueStatus,
                    QueuedDate = q.QueuedDate ?? DateTime.Now,
                    PreviousSponsorshipId = q.PreviousSponsorshipId,
                    Status = q.Status,
                    IsSponsoredSubscription = q.IsSponsoredSubscription
                }).ToList()
            };

            return Ok(new SuccessDataResult<UserSubscriptionDto>(dto));
        }

        /// <summary>
        /// Get current user's usage status
        /// </summary>
        [HttpGet("usage-status")]
        [Authorize(Roles = "Farmer,Admin")]  // Fixed: Added Farmer role
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsageStatus()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var status = await _subscriptionValidationService.CheckSubscriptionStatusAsync(userId.Value);
            
            return Ok(status);
        }

        /// <summary>
        /// Subscribe to a plan
        /// </summary>
        [HttpPost("subscribe")]
        [Authorize(Roles = "Farmer,Admin")]  // Fixed: Added Farmer role
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Subscribe([FromBody] CreateUserSubscriptionDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            // Check if user already has an active non-trial subscription
            var existingSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId.Value);
            if (existingSubscription != null)
            {
                // Allow upgrade from trial to paid subscription
                if (existingSubscription.IsTrialSubscription && !request.IsTrialSubscription)
                {
                    // Cancel the trial subscription
                    existingSubscription.IsActive = false;
                    existingSubscription.Status = "Upgraded";
                    existingSubscription.CancellationDate = DateTime.UtcNow;
                    existingSubscription.CancellationReason = "Upgraded to paid subscription";
                    existingSubscription.UpdatedDate = DateTime.UtcNow;
                    existingSubscription.UpdatedUserId = userId.Value;
                    
                    _userSubscriptionRepository.Update(existingSubscription);
                }
                else
                {
                    return BadRequest(new ErrorResult("You already have an active subscription. Please cancel it first."));
                }
            }

            // Get the tier
            var tier = await _tierRepository.GetAsync(t => t.Id == request.SubscriptionTierId && t.IsActive);
            if (tier == null)
                return BadRequest(new ErrorResult("Invalid subscription tier"));

            // Create subscription
            var subscription = new Entities.Concrete.UserSubscription
            {
                UserId = userId.Value,
                SubscriptionTierId = request.SubscriptionTierId,
                StartDate = request.StartDate ?? DateTime.UtcNow,
                EndDate = (request.StartDate ?? DateTime.UtcNow).AddMonths(request.DurationMonths ?? 1),
                IsActive = true,
                AutoRenew = request.AutoRenew,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                PaidAmount = request.PaidAmount ?? tier.MonthlyPrice * (request.DurationMonths ?? 1),
                Currency = request.Currency,
                LastPaymentDate = DateTime.UtcNow,
                NextPaymentDate = (request.StartDate ?? DateTime.UtcNow).AddMonths(request.DurationMonths ?? 1),
                CurrentDailyUsage = 0,
                CurrentMonthlyUsage = 0,
                LastUsageResetDate = DateTime.UtcNow,
                MonthlyUsageResetDate = DateTime.UtcNow,
                Status = "Active",
                IsTrialSubscription = request.IsTrialSubscription,
                TrialEndDate = request.IsTrialSubscription ? DateTime.UtcNow.AddDays(request.TrialDays ?? 7) : null,
                CreatedDate = DateTime.UtcNow,
                CreatedUserId = userId.Value
            };

            _userSubscriptionRepository.Add(subscription);
            await _userSubscriptionRepository.SaveChangesAsync();

            return Ok(new SuccessResult($"Successfully subscribed to {tier.DisplayName} plan"));
        }

        /// <summary>
        /// Cancel subscription
        /// </summary>
        [HttpPost("cancel")]
        [Authorize(Roles = "Farmer,Admin")]  // Fixed: Added Farmer role
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var subscription = await _userSubscriptionRepository.GetAsync(
                s => s.Id == request.UserSubscriptionId && s.UserId == userId.Value && s.IsActive);
            
            if (subscription == null)
                return NotFound(new ErrorResult("Subscription not found"));

            if (request.ImmediateCancellation)
            {
                subscription.IsActive = false;
                subscription.Status = "Cancelled";
                subscription.CancellationDate = DateTime.UtcNow;
            }
            else
            {
                subscription.AutoRenew = false;
                subscription.Status = "Pending Cancellation";
            }

            subscription.CancellationReason = request.CancellationReason;
            subscription.UpdatedDate = DateTime.UtcNow;
            subscription.UpdatedUserId = userId.Value;

            _userSubscriptionRepository.Update(subscription);
            await _userSubscriptionRepository.SaveChangesAsync();

            return Ok(new SuccessResult(request.ImmediateCancellation 
                ? "Subscription cancelled immediately" 
                : "Subscription will be cancelled at the end of the current period"));
        }

        /// <summary>
        /// Get subscription history
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Farmer,Admin")]  // Fixed: Added Farmer role
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionHistory()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var subscriptions = await _userSubscriptionRepository.GetUserSubscriptionHistoryAsync(userId.Value);
            
            var dtos = subscriptions.Select(s => new UserSubscriptionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                SubscriptionTierId = s.SubscriptionTierId,
                TierName = s.SubscriptionTier?.TierName,
                TierDisplayName = s.SubscriptionTier?.DisplayName,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsActive = s.IsActive,
                AutoRenew = s.AutoRenew,
                Status = s.Status,
                CurrentDailyUsage = s.CurrentDailyUsage,
                DailyRequestLimit = s.SubscriptionTier?.DailyRequestLimit ?? 0,
                CurrentMonthlyUsage = s.CurrentMonthlyUsage,
                MonthlyRequestLimit = s.SubscriptionTier?.MonthlyRequestLimit ?? 0,
                IsTrialSubscription = s.IsTrialSubscription,
                TrialEndDate = s.TrialEndDate
            }).ToList();

            return Ok(new SuccessDataResult<List<UserSubscriptionDto>>(dtos));
        }

        /// <summary>
        /// Get usage logs (Admin only)
        /// </summary>
        [HttpGet("usage-logs")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsageLogs(int? userId, DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            
            if (userId.HasValue)
            {
                var logs = await _usageLogRepository.GetUserUsageLogsAsync(userId.Value, start, end);
                return Ok(new SuccessDataResult<List<Entities.Concrete.SubscriptionUsageLog>>(logs));
            }

            var allLogs = await _usageLogRepository.GetListAsync(
                l => l.UsageDate >= start && l.UsageDate <= end);
            
            return Ok(new SuccessDataResult<List<Entities.Concrete.SubscriptionUsageLog>>(allLogs.ToList()));
        }

        /// <summary>
        /// Update subscription tier (Admin only)
        /// </summary>
        [HttpPut("tiers/{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateTier(int id, [FromBody] UpdateSubscriptionTierDto request)
        {
            var tier = await _tierRepository.GetAsync(t => t.Id == id);
            if (tier == null)
                return NotFound(new ErrorResult("Tier not found"));

            tier.DisplayName = request.DisplayName;
            tier.Description = request.Description;
            tier.DailyRequestLimit = request.DailyRequestLimit;
            tier.MonthlyRequestLimit = request.MonthlyRequestLimit;
            tier.MonthlyPrice = request.MonthlyPrice;
            tier.YearlyPrice = request.YearlyPrice ?? tier.YearlyPrice;
            tier.Currency = request.Currency;
            tier.PrioritySupport = request.PrioritySupport;
            tier.AdvancedAnalytics = request.AdvancedAnalytics;
            tier.ApiAccess = request.ApiAccess;
            tier.ResponseTimeHours = request.ResponseTimeHours;
            tier.AdditionalFeatures = request.Features != null ? JsonConvert.SerializeObject(request.Features) : "[]";
            tier.IsActive = request.IsActive;
            tier.UpdatedDate = DateTime.UtcNow;
            tier.UpdatedUserId = GetUserId();

            _tierRepository.Update(tier);
            await _tierRepository.SaveChangesAsync();

            return Ok(new SuccessResult($"Tier {tier.TierName} updated successfully"));
        }

        private int? GetUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            
            return null;
        }

        private static List<string> GetFeatures(string additionalFeatures)
        {
            try
            {
                return !string.IsNullOrEmpty(additionalFeatures) 
                    ? JsonConvert.DeserializeObject<List<string>>(additionalFeatures) ?? new List<string>()
                    : new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}