using System.Threading.Tasks;

namespace Business.Services.Cache
{
    /// <summary>
    /// Cache invalidation service for managing cache lifecycles across distributed systems
    /// Supports Redis Pub/Sub for broadcast invalidation in multi-instance deployments
    /// </summary>
    public interface ICacheInvalidationService
    {
        /// <summary>
        /// Invalidate dealer dashboard cache
        /// Triggered by: Code transfers, invitations, code distribution, code reclaim
        /// </summary>
        /// <param name="dealerId">Dealer user ID</param>
        Task InvalidateDealerDashboardAsync(int dealerId);

        /// <summary>
        /// Invalidate sponsor dashboard cache
        /// Triggered by: Package purchases, code redemptions, dealer transfers
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        Task InvalidateSponsorDashboardAsync(int sponsorId);

        /// <summary>
        /// Invalidate all admin statistics caches
        /// Triggered by: User registration, subscription assignment/cancellation, purchases
        /// Uses pattern-based invalidation to clear all admin stats
        /// </summary>
        Task InvalidateAdminStatisticsAsync();

        /// <summary>
        /// Invalidate subscription tier cache
        /// Triggered by: Tier creation, updates, deletions
        /// Supports broadcast invalidation for distributed systems
        /// </summary>
        /// <param name="tierId">Optional tier ID (null = invalidate all tiers)</param>
        Task InvalidateSubscriptionTierAsync(int? tierId = null);

        /// <summary>
        /// Invalidate configuration cache
        /// Triggered by: Configuration updates
        /// Supports broadcast invalidation for distributed systems
        /// </summary>
        /// <param name="key">Configuration key to invalidate</param>
        Task InvalidateConfigurationAsync(string key);

        /// <summary>
        /// Invalidate sponsor analytics cache
        /// Triggered by: Code transfers, redemptions, farmer activities
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        Task InvalidateSponsorAnalyticsAsync(int sponsorId);

        /// <summary>
        /// Invalidate dealer codes cache
        /// Triggered by: Code transfers, distributions, reclaims
        /// </summary>
        /// <param name="dealerId">Dealer user ID</param>
        Task InvalidateDealerCodesAsync(int dealerId);

        /// <summary>
        /// Invalidate dealer invitations cache
        /// Triggered by: Invitation creation, acceptance, rejection
        /// </summary>
        /// <param name="dealerId">Dealer user ID</param>
        Task InvalidateDealerInvitationsAsync(int dealerId);

        /// <summary>
        /// Invalidate user statistics cache (admin)
        /// Triggered by: User registration, role changes, deactivation
        /// </summary>
        Task InvalidateUserStatisticsAsync();

        /// <summary>
        /// Invalidate subscription statistics cache (admin)
        /// Triggered by: Subscription assignment, cancellation, extension
        /// </summary>
        Task InvalidateSubscriptionStatisticsAsync();

        /// <summary>
        /// Invalidate sponsorship statistics cache (admin)
        /// Triggered by: Package purchases, code redemptions
        /// </summary>
        Task InvalidateSponsorshipStatisticsAsync();

        /// <summary>
        /// Broadcast cache invalidation to all instances (Redis Pub/Sub)
        /// Used for reference data that needs immediate consistency across all servers
        /// </summary>
        /// <param name="cacheKey">Cache key pattern to invalidate</param>
        Task BroadcastInvalidationAsync(string cacheKey);

        /// <summary>
        /// Remove multiple cache keys by pattern
        /// Useful for batch invalidation (e.g., all dealer caches, all admin stats)
        /// </summary>
        /// <param name="pattern">Redis key pattern (e.g., "dealer:*", "admin:stats:*")</param>
        Task RemoveByPatternAsync(string pattern);
    }
}
