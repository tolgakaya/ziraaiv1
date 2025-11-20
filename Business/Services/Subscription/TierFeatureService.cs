using DataAccess.Abstract;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.Subscription
{
    /// <summary>
    /// Centralized service for tier-based feature access control
    /// Implements caching for performance optimization
    /// </summary>
    public class TierFeatureService : ITierFeatureService
    {
        private readonly ITierFeatureRepository _tierFeatureRepository;
        private readonly IFeatureRepository _featureRepository;
        private readonly IMemoryCache _cache;
        private const int CacheExpirationMinutes = 15;

        public TierFeatureService(
            ITierFeatureRepository tierFeatureRepository,
            IFeatureRepository featureRepository,
            IMemoryCache cache)
        {
            _tierFeatureRepository = tierFeatureRepository;
            _featureRepository = featureRepository;
            _cache = cache;
        }

        public async Task<bool> HasFeatureAccessAsync(int tierId, string featureKey)
        {
            var cacheKey = $"tier_feature_access_{tierId}_{featureKey}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);

                // Get feature by key
                var feature = await _featureRepository.GetAsync(f => f.FeatureKey == featureKey && f.IsActive && !f.IsDeprecated);
                if (feature == null)
                    return false;

                // Get tier-feature mapping
                var tierFeature = await _tierFeatureRepository.GetAsync(tf =>
                    tf.SubscriptionTierId == tierId &&
                    tf.FeatureId == feature.Id &&
                    tf.IsEnabled);

                if (tierFeature == null)
                    return false;

                // Check effective/expiry dates
                var now = DateTime.Now;
                if (tierFeature.EffectiveDate.HasValue && now < tierFeature.EffectiveDate.Value)
                    return false;

                if (tierFeature.ExpiryDate.HasValue && now > tierFeature.ExpiryDate.Value)
                    return false;

                return true;
            });
        }

        public async Task<string> GetFeatureConfigAsync(int tierId, string featureKey)
        {
            var cacheKey = $"tier_feature_config_{tierId}_{featureKey}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);

                // Get feature by key
                var feature = await _featureRepository.GetAsync(f => f.FeatureKey == featureKey && f.IsActive && !f.IsDeprecated);
                if (feature == null)
                    return null;

                // Get tier-feature mapping
                var tierFeature = await _tierFeatureRepository.GetAsync(tf =>
                    tf.SubscriptionTierId == tierId &&
                    tf.FeatureId == feature.Id &&
                    tf.IsEnabled);

                // Return tier-specific config if exists, otherwise default config
                return tierFeature?.ConfigurationJson ?? feature.DefaultConfigJson;
            });
        }

        public async Task<T> GetFeatureConfigAsync<T>(int tierId, string featureKey) where T : class
        {
            var configJson = await GetFeatureConfigAsync(tierId, featureKey);
            
            if (string.IsNullOrWhiteSpace(configJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(configJson);
            }
            catch (JsonException)
            {
                // Log error in production
                return null;
            }
        }

        public async Task<bool> IsFeatureActiveAsync(int tierId, string featureKey)
        {
            // This is the same as HasFeatureAccessAsync but with clearer naming
            return await HasFeatureAccessAsync(tierId, featureKey);
        }
    }
}
