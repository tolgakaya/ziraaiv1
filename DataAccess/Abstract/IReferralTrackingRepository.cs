using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IReferralTrackingRepository : IEntityRepository<ReferralTracking>
    {
        /// <summary>
        /// Get tracking record by referral code ID
        /// </summary>
        Task<List<ReferralTracking>> GetByReferralCodeIdAsync(int referralCodeId);

        /// <summary>
        /// Get tracking record by referee user ID
        /// </summary>
        Task<ReferralTracking> GetByRefereeUserIdAsync(int refereeUserId);

        /// <summary>
        /// Get tracking record by device ID and referral code (duplicate prevention)
        /// </summary>
        Task<ReferralTracking> GetByDeviceAndCodeAsync(string deviceId, int referralCodeId);

        /// <summary>
        /// Get all tracking records for a referrer (via code owner)
        /// </summary>
        Task<List<ReferralTracking>> GetByReferrerUserIdAsync(int referrerUserId);

        /// <summary>
        /// Get tracking statistics by referral code
        /// </summary>
        Task<Dictionary<string, int>> GetStatsByReferralCodeIdAsync(int referralCodeId);

        /// <summary>
        /// Get tracking statistics by referrer user
        /// </summary>
        Task<Dictionary<string, int>> GetStatsByReferrerUserIdAsync(int referrerUserId);

        /// <summary>
        /// Update tracking status
        /// </summary>
        Task<bool> UpdateStatusAsync(int trackingId, ReferralTrackingStatus status);

        /// <summary>
        /// Get pending validations (registered but not validated)
        /// </summary>
        Task<List<ReferralTracking>> GetPendingValidationsAsync();
    }
}
