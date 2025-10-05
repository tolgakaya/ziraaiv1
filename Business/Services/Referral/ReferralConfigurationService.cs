using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.Referral
{
    public class ReferralConfigurationService : IReferralConfigurationService
    {
        private readonly IReferralConfigurationRepository _configRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ReferralConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public ReferralConfigurationService(
            IReferralConfigurationRepository configRepository,
            IMemoryCache cache,
            ILogger<ReferralConfigurationService> logger,
            IConfiguration configuration)
        {
            _configRepository = configRepository;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<int> GetCreditsPerReferralAsync()
        {
            return await GetCachedIntValueAsync(
                ReferralConfigurationKeys.CreditPerReferral,
                10); // Default: 10 credits per referral
        }

        public async Task<int> GetLinkExpiryDaysAsync()
        {
            return await GetCachedIntValueAsync(
                ReferralConfigurationKeys.LinkExpiryDays,
                30); // Default: 30 days
        }

        public async Task<int> GetMinAnalysisForValidationAsync()
        {
            return await GetCachedIntValueAsync(
                ReferralConfigurationKeys.MinAnalysisForValidation,
                1); // Default: 1 analysis required
        }

        public async Task<int> GetMaxReferralsPerUserAsync()
        {
            return await GetCachedIntValueAsync(
                ReferralConfigurationKeys.MaxReferralsPerUser,
                0); // Default: 0 = unlimited
        }

        public async Task<bool> IsWhatsAppEnabledAsync()
        {
            return await GetCachedBoolValueAsync(
                ReferralConfigurationKeys.EnableWhatsApp,
                true); // Default: enabled
        }

        public async Task<bool> IsSmsEnabledAsync()
        {
            return await GetCachedBoolValueAsync(
                ReferralConfigurationKeys.EnableSMS,
                true); // Default: enabled
        }

        public async Task<string> GetCodePrefixAsync()
        {
            return await GetCachedStringValueAsync(
                ReferralConfigurationKeys.CodePrefix,
                "ZIRA"); // Default: ZIRA prefix
        }

        public async Task<string> GetDeepLinkBaseUrlAsync()
        {
            // Priority: 1. appsettings.json (environment-specific), 2. Database, 3. Fallback from config
            var configValue = _configuration["Referral:DeepLinkBaseUrl"];

            if (!string.IsNullOrWhiteSpace(configValue))
            {
                _logger.LogDebug("Using deep link base URL from appsettings: {Url}", configValue);
                return await Task.FromResult(configValue);
            }

            // If not in appsettings, try database with fallback from configuration
            var fallbackUrl = _configuration["Referral:FallbackDeepLinkBaseUrl"]
                ?? throw new InvalidOperationException("Referral:DeepLinkBaseUrl or Referral:FallbackDeepLinkBaseUrl must be configured");

            return await GetCachedStringValueAsync(
                ReferralConfigurationKeys.DeepLinkBaseUrl,
                fallbackUrl);
        }

        public async Task<bool> UpdateConfigurationAsync(string key, string value, int updatedBy)
        {
            try
            {
                var success = await _configRepository.UpdateValueAsync(key, value, updatedBy);

                if (success)
                {
                    // Clear cache for this key
                    _cache.Remove(GetCacheKey(key));
                    _logger.LogInformation("Referral configuration updated: {Key} = {Value} by user {UserId}",
                        key, value, updatedBy);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating referral configuration: {Key}", key);
                return false;
            }
        }

        #region Private Helper Methods

        private async Task<int> GetCachedIntValueAsync(string key, int defaultValue)
        {
            var cacheKey = GetCacheKey(key);

            if (_cache.TryGetValue(cacheKey, out int cachedValue))
                return cachedValue;

            try
            {
                var value = await _configRepository.GetIntValueAsync(key, defaultValue);
                _cache.Set(cacheKey, value, _cacheExpiration);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting int config value: {Key}, using default: {DefaultValue}",
                    key, defaultValue);
                return defaultValue;
            }
        }

        private async Task<bool> GetCachedBoolValueAsync(string key, bool defaultValue)
        {
            var cacheKey = GetCacheKey(key);

            if (_cache.TryGetValue(cacheKey, out bool cachedValue))
                return cachedValue;

            try
            {
                var value = await _configRepository.GetBoolValueAsync(key, defaultValue);
                _cache.Set(cacheKey, value, _cacheExpiration);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bool config value: {Key}, using default: {DefaultValue}",
                    key, defaultValue);
                return defaultValue;
            }
        }

        private async Task<string> GetCachedStringValueAsync(string key, string defaultValue)
        {
            var cacheKey = GetCacheKey(key);

            if (_cache.TryGetValue(cacheKey, out string cachedValue))
                return cachedValue;

            try
            {
                var value = await _configRepository.GetValueAsync(key) ?? defaultValue;
                _cache.Set(cacheKey, value, _cacheExpiration);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting string config value: {Key}, using default: {DefaultValue}",
                    key, defaultValue);
                return defaultValue;
            }
        }

        private static string GetCacheKey(string key) => $"referral_config_{key}";

        #endregion
    }
}
