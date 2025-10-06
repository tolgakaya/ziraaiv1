using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DataAccess;

namespace Business.Services.Referral
{
    public class ReferralRewardService : IReferralRewardService
    {
        private readonly IReferralRewardRepository _rewardRepository;
        private readonly IReferralTrackingRepository _trackingRepository;
        private readonly IReferralCodeRepository _codeRepository;
        private readonly IUserSubscriptionRepository _subscriptionRepository;
        private readonly IReferralConfigurationService _configService;
        private readonly ILogger<ReferralRewardService> _logger;

        public ReferralRewardService(
            IReferralRewardRepository rewardRepository,
            IReferralTrackingRepository trackingRepository,
            IReferralCodeRepository codeRepository,
            IUserSubscriptionRepository subscriptionRepository,
            IReferralConfigurationService configService,
            ILogger<ReferralRewardService> logger)
        {
            _rewardRepository = rewardRepository;
            _trackingRepository = trackingRepository;
            _codeRepository = codeRepository;
            _subscriptionRepository = subscriptionRepository;
            _configService = configService;
            _logger = logger;
        }

        public async Task<IResult> ProcessRewardAsync(int referralTrackingId)
        {
            try
            {
                // Get tracking record
                var tracking = await _trackingRepository.GetAsync(t => t.Id == referralTrackingId);
                if (tracking == null)
                    return new ErrorResult("Tracking record not found");

                if (tracking.Status != (int)ReferralTrackingStatus.Validated)
                    return new ErrorResult("Referral not yet validated");

                // Check if already rewarded
                var existingReward = await _rewardRepository.GetByTrackingIdAsync(referralTrackingId);
                if (existingReward != null)
                    return new SuccessResult("Reward already processed");

                // Get referral code to find referrer
                var referralCode = await _codeRepository.GetAsync(rc => rc.Id == tracking.ReferralCodeId);
                if (referralCode == null)
                    return new ErrorResult("Referral code not found");

                var referrerUserId = referralCode.UserId;
                var refereeUserId = tracking.RefereeUserId.Value;

                // Get configurable credit amount
                var creditAmount = await _configService.GetCreditsPerReferralAsync();

                // Get or create active subscription for referrer
                var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(referrerUserId);
                if (subscription == null)
                {
                    _logger.LogWarning("No active subscription for referrer {ReferrerUserId}", referrerUserId);
                    return new ErrorResult("Referrer has no active subscription");
                }

                // Add credits to subscription
                subscription.ReferralCredits += creditAmount;
                _subscriptionRepository.Update(subscription);

                // Create reward record
                var reward = new ReferralReward
                {
                    ReferralTrackingId = referralTrackingId,
                    ReferrerUserId = referrerUserId,
                    RefereeUserId = refereeUserId,
                    CreditAmount = creditAmount,
                    AwardedAt = DateTime.Now,
                    SubscriptionId = subscription.Id,
                    ExpiresAt = null // Never expires per requirements
                };

                _rewardRepository.Add(reward);

                // Mark tracking as rewarded
                tracking.RewardProcessedAt = DateTime.Now;
                tracking.Status = (int)ReferralTrackingStatus.Rewarded;
                _trackingRepository.Update(tracking);

                await _rewardRepository.SaveChangesAsync();
                await _trackingRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "Referral reward processed: {CreditAmount} credits to referrer {ReferrerUserId} for referee {RefereeUserId}",
                    creditAmount, referrerUserId, refereeUserId);

                return new SuccessResult($"{creditAmount} credits awarded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reward for tracking {TrackingId}", referralTrackingId);
                return new ErrorResult("Failed to process reward");
            }
        }

        public async Task<IDataResult<int>> GetReferralCreditsBalanceAsync(int userId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

                if (subscription == null)
                    return new SuccessDataResult<int>(0);

                return new SuccessDataResult<int>(subscription.ReferralCredits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting referral credits balance for user {UserId}", userId);
                return new ErrorDataResult<int>(0, "Error retrieving credit balance");
            }
        }

        public async Task<IDataResult<int>> GetTotalCreditsEarnedAsync(int userId)
        {
            try
            {
                var totalCredits = await _rewardRepository.GetTotalCreditsByReferrerAsync(userId);
                return new SuccessDataResult<int>(totalCredits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total credits earned for user {UserId}", userId);
                return new ErrorDataResult<int>(0, "Error retrieving total credits");
            }
        }

        public async Task<IDataResult<List<ReferralReward>>> GetRewardsByReferrerAsync(int referrerUserId)
        {
            try
            {
                var rewards = await _rewardRepository.GetByReferrerUserIdAsync(referrerUserId);
                return new SuccessDataResult<List<ReferralReward>>(rewards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rewards for referrer {ReferrerUserId}", referrerUserId);
                return new ErrorDataResult<List<ReferralReward>>("Error retrieving rewards");
            }
        }

        public async Task<IResult> DeductReferralCreditAsync(int userId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

                if (subscription == null)
                    return new ErrorResult("No active subscription found");

                if (subscription.ReferralCredits <= 0)
                    return new ErrorResult("No referral credits available");

                subscription.ReferralCredits--;
                _subscriptionRepository.Update(subscription);
                await _subscriptionRepository.SaveChangesAsync();

                _logger.LogInformation("Deducted 1 referral credit from user {UserId}, balance: {Balance}",
                    userId, subscription.ReferralCredits);

                return new SuccessResult("Referral credit deducted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deducting referral credit for user {UserId}", userId);
                return new ErrorResult("Failed to deduct referral credit");
            }
        }

        public async Task<IDataResult<ReferralCreditBreakdown>> GetCreditBreakdownAsync(int userId)
        {
            try
            {
                var totalEarned = await _rewardRepository.GetTotalCreditsByReferrerAsync(userId);

                var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
                var currentBalance = subscription?.ReferralCredits ?? 0;

                var totalUsed = totalEarned - currentBalance;

                var breakdown = new ReferralCreditBreakdown
                {
                    TotalEarned = totalEarned,
                    TotalUsed = totalUsed,
                    CurrentBalance = currentBalance
                };

                return new SuccessDataResult<ReferralCreditBreakdown>(breakdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit breakdown for user {UserId}", userId);
                return new ErrorDataResult<ReferralCreditBreakdown>("Error retrieving credit breakdown");
            }
        }
    }
}
