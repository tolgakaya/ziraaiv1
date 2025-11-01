using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.DealerInvitation
{
    public class DealerInvitationConfigurationService : IDealerInvitationConfigurationService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<DealerInvitationConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public DealerInvitationConfigurationService(
            IMemoryCache cache,
            ILogger<DealerInvitationConfigurationService> logger,
            IConfiguration configuration)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GetDeepLinkBaseUrlAsync()
        {
            // Priority: 1. Environment Variable (via appsettings override), 2. Fallback from config
            var configValue = _configuration["DealerInvitation:DeepLinkBaseUrl"];

            if (!string.IsNullOrWhiteSpace(configValue))
            {
                _logger.LogDebug("Using dealer invitation deep link base URL from appsettings/env: {Url}", configValue);
                return await Task.FromResult(configValue);
            }

            // If not in appsettings/env, use fallback from configuration
            var fallbackUrl = _configuration["DealerInvitation:FallbackDeepLinkBaseUrl"]
                ?? _configuration["WebAPI:BaseUrl"]?.TrimEnd('/') + "/dealer-invitation/"
                ?? throw new InvalidOperationException("DealerInvitation:DeepLinkBaseUrl or DealerInvitation:FallbackDeepLinkBaseUrl must be configured");

            _logger.LogDebug("Using dealer invitation deep link fallback URL: {Url}", fallbackUrl);
            return await Task.FromResult(fallbackUrl);
        }

        public async Task<int> GetTokenExpiryDaysAsync()
        {
            // Check appsettings/env first
            var configValue = _configuration["DealerInvitation:TokenExpiryDays"];
            if (!string.IsNullOrWhiteSpace(configValue) && int.TryParse(configValue, out var days))
            {
                _logger.LogDebug("Using token expiry days from appsettings/env: {Days}", days);
                return await Task.FromResult(days);
            }

            // Default: 7 days
            _logger.LogDebug("Using default token expiry days: 7");
            return await Task.FromResult(7);
        }

        public async Task<string> GetSmsTemplateAsync()
        {
            // Check appsettings/env first
            var configValue = _configuration["DealerInvitation:SmsTemplate"];
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                _logger.LogDebug("Using SMS template from appsettings/env");
                return await Task.FromResult(configValue);
            }

            // Default template
            const string defaultTemplate = "🎁 {sponsorName} Bayilik Daveti!\n\n" +
                                          "Davet Kodunuz: DEALER-{token}\n\n" +
                                          "Hemen katılmak için tıklayın:\n{deepLink}\n\n" +
                                          "Veya uygulamayı indirin:\n{playStoreLink}";

            _logger.LogDebug("Using default SMS template");
            return await Task.FromResult(defaultTemplate);
        }

        public async Task<bool> IsWhatsAppEnabledAsync()
        {
            // Check appsettings/env first
            var configValue = _configuration["DealerInvitation:EnableWhatsApp"];
            if (!string.IsNullOrWhiteSpace(configValue) && bool.TryParse(configValue, out var enabled))
            {
                return await Task.FromResult(enabled);
            }

            // Default: enabled
            return await Task.FromResult(true);
        }

        public async Task<bool> IsSmsEnabledAsync()
        {
            // Check appsettings/env first
            var configValue = _configuration["DealerInvitation:EnableSMS"];
            if (!string.IsNullOrWhiteSpace(configValue) && bool.TryParse(configValue, out var enabled))
            {
                return await Task.FromResult(enabled);
            }

            // Default: enabled
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateConfigurationAsync(string key, string value, int updatedBy)
        {
            // Note: This implementation only supports appsettings/environment variables.
            // Database-driven configuration can be added later if needed.
            _logger.LogWarning("UpdateConfigurationAsync not implemented - use appsettings or environment variables");
            return await Task.FromResult(false);
        }
    }
}
