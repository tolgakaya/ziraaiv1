using System.Threading.Tasks;

namespace Business.Services.Referral
{
    /// <summary>
    /// Service for managing referral system configurations.
    /// Provides access to configurable settings like credit amounts, expiry days, etc.
    /// </summary>
    public interface IReferralConfigurationService
    {
        /// <summary>
        /// Get number of credits awarded per successful referral (default: 10)
        /// </summary>
        Task<int> GetCreditsPerReferralAsync();

        /// <summary>
        /// Get number of days before referral link expires (default: 30)
        /// </summary>
        Task<int> GetLinkExpiryDaysAsync();

        /// <summary>
        /// Get minimum number of analyses required for validation (default: 1)
        /// </summary>
        Task<int> GetMinAnalysisForValidationAsync();

        /// <summary>
        /// Get maximum referrals per user (0 = unlimited, default: 0)
        /// </summary>
        Task<int> GetMaxReferralsPerUserAsync();

        /// <summary>
        /// Check if WhatsApp link sharing is enabled (default: true)
        /// </summary>
        Task<bool> IsWhatsAppEnabledAsync();

        /// <summary>
        /// Check if SMS link sharing is enabled (default: true)
        /// </summary>
        Task<bool> IsSmsEnabledAsync();

        /// <summary>
        /// Get referral code prefix (default: "ZIRA")
        /// </summary>
        Task<string> GetCodePrefixAsync();

        /// <summary>
        /// Get deep link base URL (default: "https://ziraai.com/ref/")
        /// </summary>
        Task<string> GetDeepLinkBaseUrlAsync();

        /// <summary>
        /// Update configuration value (admin only)
        /// </summary>
        Task<bool> UpdateConfigurationAsync(string key, string value, int updatedBy);
    }
}
