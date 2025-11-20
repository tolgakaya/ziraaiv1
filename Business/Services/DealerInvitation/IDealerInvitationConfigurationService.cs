using System.Threading.Tasks;

namespace Business.Services.DealerInvitation
{
    /// <summary>
    /// Service for managing dealer invitation system configurations.
    /// Provides access to configurable settings like deep link URLs, token expiry, etc.
    /// </summary>
    public interface IDealerInvitationConfigurationService
    {
        /// <summary>
        /// Get deep link base URL for dealer invitations
        /// Priority: Environment Variable → appsettings → Database → Fallback
        /// Example: "https://ziraai-api-sit.up.railway.app/dealer-invitation/"
        /// </summary>
        Task<string> GetDeepLinkBaseUrlAsync();

        /// <summary>
        /// Get number of days before invitation token expires (default: 7)
        /// </summary>
        Task<int> GetTokenExpiryDaysAsync();

        /// <summary>
        /// Get SMS template for dealer invitations
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
