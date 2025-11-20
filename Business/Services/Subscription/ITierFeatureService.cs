using System.Threading.Tasks;

namespace Business.Services.Subscription
{
    /// <summary>
    /// Service for centralized tier-based feature access control
    /// Replaces hard-coded tier checks with database-driven permissions
    /// </summary>
    public interface ITierFeatureService
    {
        /// <summary>
        /// Check if a specific tier has access to a feature
        /// </summary>
        /// <param name="tierId">Subscription tier ID</param>
        /// <param name="featureKey">Feature key (e.g., "messaging", "smart_links")</param>
        /// <returns>True if tier has access to the feature</returns>
        Task<bool> HasFeatureAccessAsync(int tierId, string featureKey);

        /// <summary>
        /// Get feature configuration for a specific tier
        /// </summary>
        /// <param name="tierId">Subscription tier ID</param>
        /// <param name="featureKey">Feature key</param>
        /// <returns>Configuration JSON string or null if not configured</returns>
        Task<string> GetFeatureConfigAsync(int tierId, string featureKey);

        /// <summary>
        /// Get typed configuration value from JSON
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="tierId">Subscription tier ID</param>
        /// <param name="featureKey">Feature key</param>
        /// <returns>Deserialized configuration object or default(T) if not found</returns>
        Task<T> GetFeatureConfigAsync<T>(int tierId, string featureKey) where T : class;

        /// <summary>
        /// Check if feature is currently active (considering effective/expiry dates)
        /// </summary>
        /// <param name="tierId">Subscription tier ID</param>
        /// <param name="featureKey">Feature key</param>
        /// <returns>True if feature is active and within valid date range</returns>
        Task<bool> IsFeatureActiveAsync(int tierId, string featureKey);
    }
}
