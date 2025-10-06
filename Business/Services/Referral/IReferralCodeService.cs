using Core.Utilities.Results;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Referral
{
    /// <summary>
    /// Service for managing referral codes.
    /// Handles code generation, validation, and lifecycle management.
    /// </summary>
    public interface IReferralCodeService
    {
        /// <summary>
        /// Generate a new unique referral code for a user.
        /// Format: {Prefix}-{6 alphanumeric} (e.g., ZIRA-ABC123)
        /// </summary>
        Task<IDataResult<ReferralCode>> GenerateCodeAsync(int userId);

        /// <summary>
        /// Get referral code by code string
        /// </summary>
        Task<IDataResult<ReferralCode>> GetByCodeAsync(string code);

        /// <summary>
        /// Get active and valid (not expired) code by code string
        /// </summary>
        Task<IDataResult<ReferralCode>> GetActiveCodeAsync(string code);

        /// <summary>
        /// Get all codes created by a user
        /// </summary>
        Task<IDataResult<List<ReferralCode>>> GetUserCodesAsync(int userId);

        /// <summary>
        /// Get all active codes for a user
        /// </summary>
        Task<IDataResult<List<ReferralCode>>> GetUserActiveCodesAsync(int userId);

        /// <summary>
        /// Validate if a code is valid (active and not expired)
        /// </summary>
        Task<IResult> ValidateCodeAsync(string code);

        /// <summary>
        /// Disable a specific code
        /// </summary>
        Task<IResult> DisableCodeAsync(string code, int userId);

        /// <summary>
        /// Mark expired codes as expired (scheduled job)
        /// </summary>
        Task<IDataResult<int>> MarkExpiredCodesAsync();

        /// <summary>
        /// Generate unique code string
        /// </summary>
        Task<string> GenerateUniqueCodeStringAsync();
    }
}
