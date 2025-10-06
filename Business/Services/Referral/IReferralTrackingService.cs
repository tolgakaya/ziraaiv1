using Core.Utilities.Results;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Referral
{
    /// <summary>
    /// Service for tracking referral journey: Click → Register → Validate → Reward
    /// </summary>
    public interface IReferralTrackingService
    {
        /// <summary>
        /// Track a referral link click (Step 1: Clicked)
        /// </summary>
        Task<IResult> TrackClickAsync(string code, string ipAddress, string deviceId);

        /// <summary>
        /// Link a user registration to a referral code (Step 2: Registered)
        /// </summary>
        Task<IResult> LinkRegistrationAsync(int userId, string code);

        /// <summary>
        /// Validate referral after first analysis (Step 3: Validated → triggers reward)
        /// </summary>
        Task<IResult> ValidateReferralAsync(int userId);

        /// <summary>
        /// Mark referral as rewarded (Step 4: Rewarded)
        /// </summary>
        Task<IResult> MarkAsRewardedAsync(int trackingId);

        /// <summary>
        /// Get tracking record by referee user ID
        /// </summary>
        Task<IDataResult<ReferralTracking>> GetByRefereeUserIdAsync(int refereeUserId);

        /// <summary>
        /// Get all tracking records for a referrer
        /// </summary>
        Task<IDataResult<List<ReferralTracking>>> GetByReferrerUserIdAsync(int referrerUserId);

        /// <summary>
        /// Get tracking statistics for a referrer
        /// </summary>
        Task<IDataResult<Dictionary<string, int>>> GetStatsByReferrerAsync(int referrerUserId);

        /// <summary>
        /// Get pending validations (registered but not validated yet)
        /// </summary>
        Task<IDataResult<List<ReferralTracking>>> GetPendingValidationsAsync();
    }
}
