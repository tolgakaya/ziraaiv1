using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IReferralCodeRepository : IEntityRepository<ReferralCode>
    {
        /// <summary>
        /// Get referral code by code string
        /// </summary>
        Task<ReferralCode> GetByCodeAsync(string code);

        /// <summary>
        /// Get active and valid (not expired) code by code string
        /// </summary>
        Task<ReferralCode> GetActiveCodeAsync(string code);

        /// <summary>
        /// Get all codes created by a user
        /// </summary>
        Task<List<ReferralCode>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Get all active codes for a user
        /// </summary>
        Task<List<ReferralCode>> GetActiveCodesByUserIdAsync(int userId);

        /// <summary>
        /// Check if a code is valid (active and not expired)
        /// </summary>
        Task<bool> IsCodeValidAsync(string code);

        /// <summary>
        /// Mark expired codes as expired status
        /// </summary>
        Task<int> MarkExpiredCodesAsync();

        /// <summary>
        /// Disable a specific code
        /// </summary>
        Task<bool> DisableCodeAsync(string code);
    }
}
