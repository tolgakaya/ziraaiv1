using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IReferralRewardRepository : IEntityRepository<ReferralReward>
    {
        /// <summary>
        /// Get all rewards for a referrer
        /// </summary>
        Task<List<ReferralReward>> GetByReferrerUserIdAsync(int referrerUserId);

        /// <summary>
        /// Get reward by tracking ID
        /// </summary>
        Task<ReferralReward> GetByTrackingIdAsync(int trackingId);

        /// <summary>
        /// Get total credits earned by a referrer
        /// </summary>
        Task<int> GetTotalCreditsByReferrerAsync(int referrerUserId);

        /// <summary>
        /// Get total rewards count by referrer
        /// </summary>
        Task<int> GetRewardCountByReferrerAsync(int referrerUserId);

        /// <summary>
        /// Check if a tracking record already has a reward
        /// </summary>
        Task<bool> HasRewardAsync(int trackingId);
    }
}
