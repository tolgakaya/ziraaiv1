using System.Threading.Tasks;

namespace Business.Services.FarmerInvitation
{
    /// <summary>
    /// Service for managing farmer invitation system configurations.
    /// Provides access to configurable settings like deep link URLs, token expiry, etc.
    /// </summary>
    public interface IFarmerInvitationConfigurationService
    {
        /// <summary>
        /// Get deep link base URL for farmer invitations
        /// Priority: Environment Variable → appsettings → Database → Fallback
        /// Example: "https://ziraai-api-sit.up.railway.app/farmer-invite/"
        /// </summary>
        Task<string> GetDeepLinkBaseUrlAsync();

        /// <summary>
        /// Get number of days before invitation token expires (default: 7)
        /// </summary>
        Task<int> GetTokenExpiryDaysAsync();

        /// <summary>
        /// Get SMS template for farmer invitations
        /// Placeholders: {sponsorName}, {token}, {deepLink}, {playStoreLink}
        /// </summary>
        Task<string> GetSmsTemplateAsync();

        /// <summary>
        /// Check if WhatsApp invitations are enabled (default: true)
        /// </summary>
        Task<bool> IsWhatsAppEnabledAsync();

        /// <summary>
        /// Check if SMS invitations are enabled (default: true)
        /// </summary>
        Task<bool> IsSmsEnabledAsync();

        /// <summary>
        /// Update configuration value (admin only)
        /// </summary>
        Task<bool> UpdateConfigurationAsync(string key, string value, int updatedBy);
    }
}
