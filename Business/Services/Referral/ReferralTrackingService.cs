using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Core.DataAccess;

namespace Business.Services.Referral
{
    public class ReferralTrackingService : IReferralTrackingService
    {
        private readonly IReferralTrackingRepository _trackingRepository;
        private readonly IReferralCodeRepository _codeRepository;
        private readonly ILogger<ReferralTrackingService> _logger;

        public ReferralTrackingService(
            IReferralTrackingRepository trackingRepository,
            IReferralCodeRepository codeRepository,
            ILogger<ReferralTrackingService> logger)
        {
            _trackingRepository = trackingRepository;
            _codeRepository = codeRepository;
            _logger = logger;
        }

        public async Task<IResult> TrackClickAsync(string code, string ipAddress, string deviceId)
        {
            try
            {
                // Validate code exists and is active
                var referralCode = await _codeRepository.GetActiveCodeAsync(code);
                if (referralCode == null)
                {
                    _logger.LogWarning("Click tracked for invalid/expired code: {Code}", code);
                    return new ErrorResult("Invalid or expired referral code");
                }

                // Check for duplicate clicks (anti-abuse)
                if (!string.IsNullOrEmpty(deviceId))
                {
                    var existingTracking = await _trackingRepository.GetByDeviceAndCodeAsync(deviceId, referralCode.Id);
                    if (existingTracking != null)
                    {
                        _logger.LogInformation("Duplicate click prevented: Device {DeviceId}, Code {Code}",
                            deviceId, code);
                        return new SuccessResult("Click already tracked");
                    }
                }

                // Create tracking record
                var tracking = new ReferralTracking
                {
                    ReferralCodeId = referralCode.Id,
                    ClickedAt = DateTime.Now,
                    Status = (int)ReferralTrackingStatus.Clicked,
                    IpAddress = ipAddress,
                    DeviceId = deviceId
                };

                _trackingRepository.Add(tracking);
                await _trackingRepository.SaveChangesAsync();

                _logger.LogInformation("Referral click tracked: Code {Code}, Device {DeviceId}, IP {IpAddress}",
                    code, deviceId, ipAddress);

                return new SuccessResult("Click tracked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking click for code: {Code}", code);
                return new ErrorResult("Failed to track click");
            }
        }

        public async Task<IResult> LinkRegistrationAsync(int userId, string code)
        {
            try
            {
                // Validate code
                var referralCode = await _codeRepository.GetActiveCodeAsync(code);
                if (referralCode == null)
                {
                    _logger.LogWarning("Registration linked to invalid code: {Code}", code);
                    return new ErrorResult("Invalid or expired referral code");
                }

                // Self-referral prevention
                if (referralCode.UserId == userId)
                {
                    _logger.LogWarning("Self-referral attempt detected: User {UserId}, Code {Code}",
                        userId, code);
                    return new ErrorResult("Cannot use your own referral code");
                }

                // Check if user already has a referral
                var existingTracking = await _trackingRepository.GetByRefereeUserIdAsync(userId);
                if (existingTracking != null)
                {
                    _logger.LogWarning("User {UserId} already has a referral tracking record", userId);
                    return new ErrorResult("User already registered with a referral code");
                }

                // Find existing click tracking or create new
                var tracking = (await _trackingRepository.GetListAsync(
                    t => t.ReferralCodeId == referralCode.Id && t.RefereeUserId == null))
                    .OrderByDescending(t => t.ClickedAt)
                    .FirstOrDefault();

                if (tracking != null)
                {
                    // Update existing tracking record
                    tracking.RefereeUserId = userId;
                    tracking.RegisteredAt = DateTime.Now;
                    tracking.Status = (int)ReferralTrackingStatus.Registered;
                    _trackingRepository.Update(tracking);
                }
                else
                {
                    // Create new tracking record (direct registration without click)
                    tracking = new ReferralTracking
                    {
                        ReferralCodeId = referralCode.Id,
                        RefereeUserId = userId,
                        ClickedAt = DateTime.Now,
                        RegisteredAt = DateTime.Now,
                        Status = (int)ReferralTrackingStatus.Registered
                    };
                    _trackingRepository.Add(tracking);
                }

                await _trackingRepository.SaveChangesAsync();

                _logger.LogInformation("Registration linked to referral: User {UserId}, Code {Code}",
                    userId, code);

                return new SuccessResult("Registration linked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking registration for user {UserId}, code {Code}",
                    userId, code);
                return new ErrorResult("Failed to link registration");
            }
        }

        public async Task<IResult> ValidateReferralAsync(int userId)
        {
            try
            {
                // Get tracking record for this user
                var tracking = await _trackingRepository.GetByRefereeUserIdAsync(userId);

                if (tracking == null)
                {
                    // No referral tracking for this user
                    return new SuccessResult("No referral to validate");
                }

                if (tracking.Status >= (int)ReferralTrackingStatus.Validated)
                {
                    // Already validated
                    return new SuccessResult("Referral already validated");
                }

                // Update to validated status
                tracking.FirstAnalysisAt = DateTime.Now;
                tracking.Status = (int)ReferralTrackingStatus.Validated;

                _trackingRepository.Update(tracking);
                await _trackingRepository.SaveChangesAsync();

                _logger.LogInformation("Referral validated: User {UserId}, Tracking {TrackingId}",
                    userId, tracking.Id);

                // Note: Reward processing will be handled by ReferralRewardService
                return new SuccessResult("Referral validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating referral for user {UserId}", userId);
                return new ErrorResult("Failed to validate referral");
            }
        }

        public async Task<IResult> MarkAsRewardedAsync(int trackingId)
        {
            try
            {
                var tracking = await _trackingRepository.GetAsync(t => t.Id == trackingId);

                if (tracking == null)
                    return new ErrorResult("Tracking record not found");

                if (tracking.Status >= (int)ReferralTrackingStatus.Rewarded)
                    return new SuccessResult("Already marked as rewarded");

                tracking.RewardProcessedAt = DateTime.Now;
                tracking.Status = (int)ReferralTrackingStatus.Rewarded;

                _trackingRepository.Update(tracking);
                await _trackingRepository.SaveChangesAsync();

                _logger.LogInformation("Referral marked as rewarded: Tracking {TrackingId}", trackingId);

                return new SuccessResult("Marked as rewarded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking tracking {TrackingId} as rewarded", trackingId);
                return new ErrorResult("Failed to mark as rewarded");
            }
        }

        public async Task<IDataResult<ReferralTracking>> GetByRefereeUserIdAsync(int refereeUserId)
        {
            try
            {
                var tracking = await _trackingRepository.GetByRefereeUserIdAsync(refereeUserId);
                return new SuccessDataResult<ReferralTracking>(tracking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracking for referee {RefereeUserId}", refereeUserId);
                return new ErrorDataResult<ReferralTracking>("Error retrieving tracking record");
            }
        }

        public async Task<IDataResult<List<ReferralTracking>>> GetByReferrerUserIdAsync(int referrerUserId)
        {
            try
            {
                var trackings = await _trackingRepository.GetByReferrerUserIdAsync(referrerUserId);
                return new SuccessDataResult<List<ReferralTracking>>(trackings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trackings for referrer {ReferrerUserId}", referrerUserId);
                return new ErrorDataResult<List<ReferralTracking>>("Error retrieving tracking records");
            }
        }

        public async Task<IDataResult<Dictionary<string, int>>> GetStatsByReferrerAsync(int referrerUserId)
        {
            try
            {
                var stats = await _trackingRepository.GetStatsByReferrerUserIdAsync(referrerUserId);
                return new SuccessDataResult<Dictionary<string, int>>(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats for referrer {ReferrerUserId}", referrerUserId);
                return new ErrorDataResult<Dictionary<string, int>>("Error retrieving statistics");
            }
        }

        public async Task<IDataResult<List<ReferralTracking>>> GetPendingValidationsAsync()
        {
            try
            {
                var pending = await _trackingRepository.GetPendingValidationsAsync();
                return new SuccessDataResult<List<ReferralTracking>>(pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending validations");
                return new ErrorDataResult<List<ReferralTracking>>("Error retrieving pending validations");
            }
        }
    }
}
