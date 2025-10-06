using Core.Utilities.Results;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Referral
{
    /// <summary>
    /// Service for managing referral rewards (analysis credits).
    /// Handles credit allocation, tracking, and usage.
    /// </summary>
    public interface IReferralRewardService
    {
        /// <summary>
        /// Process reward for a validated referral (awards credits to referrer)
        /// </summary>
        Task<IResult> ProcessRewardAsync(int referralTrackingId);

        /// <summary>
        /// Get total referral credits balance for a user
        /// </summary>
        Task<IDataResult<int>> GetReferralCreditsBalanceAsync(int userId);

        /// <summary>
        /// Get total credits earned by a referrer (all time)
        /// </summary>
        Task<IDataResult<int>> GetTotalCreditsEarnedAsync(int userId);

        /// <summary>
        /// Get all rewards for a referrer
        /// </summary>
        Task<IDataResult<List<ReferralReward>>> GetRewardsByReferrerAsync(int referrerUserId);

        /// <summary>
        /// Deduct one referral credit when creating plant analysis
        /// </summary>
        Task<IResult> DeductReferralCreditAsync(int userId);

        /// <summary>
        /// Get reward breakdown for a user (earned, used, current balance)
        /// </summary>
        Task<IDataResult<ReferralCreditBreakdown>> GetCreditBreakdownAsync(int userId);
    }

    /// <summary>
    /// Breakdown of referral credits
    /// </summary>
    public class ReferralCreditBreakdown
    {
        public int TotalEarned { get; set; }
        public int TotalUsed { get; set; }
        public int CurrentBalance { get; set; }
    }
}
