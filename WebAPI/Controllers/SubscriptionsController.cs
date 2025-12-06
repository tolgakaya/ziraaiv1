using Business.Services.Subscription;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SubscriptionsController> _logger;

        public SubscriptionsController(
            ISubscriptionValidationService subscriptionValidationService,
            ISubscriptionTierRepository tierRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            ISubscriptionUsageLogRepository usageLogRepository,
            ILogger<SubscriptionsController> logger)
        {
            _subscriptionValidationService = subscriptionValidationService;
            _tierRepository = tierRepository;
            _userSubscriptionRepository = userSubscriptionRepository;
            _usageLogRepository = usageLogRepository;
            _logger = logger;
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
                MinPurchaseQuantity = t.MinPurchaseQuantity,
                MaxPurchaseQuantity = t.MaxPurchaseQuantity,
                RecommendedQuantity = t.RecommendedQuantity,
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
        /// Subscribe to a plan - Validation only
        /// This endpoint validates subscription eligibility and returns payment initialization instructions.
        /// Actual subscription is created after successful payment via payment callback.
        /// Flow: Subscribe (validate) → Initialize Payment → iyzico Payment → Payment Callback → Subscription Created
        /// </summary>
        [HttpPost("subscribe")]
        [Authorize(Roles = "Farmer,Admin")]
        [ProducesResponseType(typeof(SuccessDataResult<SubscribeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            _logger.LogInformation("[Subscription] User {UserId} initiated subscription purchase. TierId: {TierId}, Duration: {Duration} months",
                userId.Value, request.SubscriptionTierId, request.DurationMonths);

            // ⚠️ IMPORTANT: This endpoint now just validates and redirects to payment flow
            // Actual subscription creation happens in PaymentController after successful payment

            // 1. Validate subscription tier
            var tier = await _tierRepository.GetAsync(t => t.Id == request.SubscriptionTierId && t.IsActive);
            if (tier == null)
            {
                _logger.LogWarning("[Subscription] Invalid subscription tier. TierId: {TierId}, UserId: {UserId}",
                    request.SubscriptionTierId, userId.Value);
                return BadRequest(new ErrorResult("Invalid subscription tier"));
            }

            // 2. Check if user already has active non-trial subscription
            var existingSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId.Value);
            if (existingSubscription != null)
            {
                // Allow upgrade from trial to paid
                if (!existingSubscription.IsTrialSubscription)
                {
                    _logger.LogWarning("[Subscription] User {UserId} already has active subscription {SubId}",
                        userId.Value, existingSubscription.Id);
                    return BadRequest(new ErrorResult("You already have an active subscription. Please cancel it first."));
                }

                _logger.LogInformation("[Subscription] User {UserId} upgrading from trial subscription {TrialSubId}",
                    userId.Value, existingSubscription.Id);
            }

            // 3. Calculate amount based on duration
            decimal amount;
            if (request.DurationMonths == 12)
            {
                // Yearly subscription
                amount = tier.YearlyPrice;
            }
            else
            {
                // Monthly or custom duration
                amount = tier.MonthlyPrice * (request.DurationMonths ?? 1);
            }

            // 4. Return payment initialization instructions
            var response = new SubscribeResponseDto
            {
                SubscriptionTierId = request.SubscriptionTierId,
                TierName = tier.TierName,
                TierDisplayName = tier.DisplayName,
                Amount = amount,
                Currency = "TRY",
                DurationMonths = request.DurationMonths ?? 1,
                NextStep = "Initialize payment via POST /api/v1/payments/initialize",
                PaymentInitializeUrl = "/api/v1/payments/initialize",
                PaymentFlowType = "FarmerSubscription"
            };

            _logger.LogInformation("[Subscription] Subscription purchase validated. UserId: {UserId}, Amount: {Amount} {Currency}, Duration: {Duration} months",
                userId.Value, amount, "TRY", request.DurationMonths ?? 1);

            return Ok(new SuccessDataResult<SubscribeResponseDto>(response,
                "Subscription validated. Please proceed to payment initialization."));
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