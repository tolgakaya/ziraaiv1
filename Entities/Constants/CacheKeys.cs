namespace Entities.Constants
{
    /// <summary>
    /// Cache key patterns for Redis cache management
    /// Uses {0}, {1} placeholders for string formatting (e.g., string.Format(CacheKeys.DealerDashboard, dealerId))
    /// </summary>
    public static class CacheKeys
    {
        // ============================================
        // Dashboard Caches
        // ============================================

        /// <summary>
        /// Dealer dashboard cache key
        /// Pattern: dealer:dashboard:{dealerId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string DealerDashboard = "dealer:dashboard:{0}";

        /// <summary>
        /// Dealer summary cache key
        /// Pattern: dealer:summary:{dealerId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string DealerSummary = "dealer:summary:{0}";

        /// <summary>
        /// Dealer codes cache key
        /// Pattern: dealer:codes:{dealerId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string DealerCodes = "dealer:codes:{0}";

        /// <summary>
        /// Dealer invitations cache key
        /// Pattern: dealer:invitations:{dealerId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string DealerInvitations = "dealer:invitations:{0}";

        /// <summary>
        /// Sponsor dashboard cache key
        /// Pattern: sponsor:dashboard:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorDashboard = "sponsor:dashboard:{0}";

        /// <summary>
        /// Sponsor summary cache key
        /// Pattern: sponsor:summary:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorSummary = "sponsor:summary:{0}";

        /// <summary>
        /// Sponsor packages cache key
        /// Pattern: sponsor:packages:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorPackages = "sponsor:packages:{0}";

        // ============================================
        // Admin Statistics Caches
        // ============================================

        /// <summary>
        /// User statistics cache key with date range hash
        /// Pattern: admin:stats:users:{dateRangeHash}
        /// TTL: 60 minutes (configurable)
        /// </summary>
        public const string AdminUserStatistics = "admin:stats:users:{0}";

        /// <summary>
        /// Subscription statistics cache key with date range hash
        /// Pattern: admin:stats:subscriptions:{dateRangeHash}
        /// TTL: 60 minutes (configurable)
        /// </summary>
        public const string AdminSubscriptionStatistics = "admin:stats:subscriptions:{0}";

        /// <summary>
        /// Sponsorship statistics cache key
        /// Pattern: admin:stats:sponsorship:{sponsorId}
        /// TTL: 60 minutes (configurable)
        /// </summary>
        public const string AdminSponsorshipStatistics = "admin:stats:sponsorship:{0}";

        // ============================================
        // Analytics Caches
        // ============================================

        /// <summary>
        /// Sponsor analytics cache key
        /// Pattern: sponsor:analytics:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorAnalytics = "sponsor:analytics:{0}";

        /// <summary>
        /// Sponsor dealer analytics cache key
        /// Pattern: sponsor:dealer:analytics:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorDealerAnalytics = "sponsor:dealer:analytics:{0}";

        /// <summary>
        /// Sponsor ROI analytics cache key
        /// Pattern: sponsor:roi:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorROIAnalytics = "sponsor:roi:{0}";

        /// <summary>
        /// Sponsor temporal analytics cache key
        /// Pattern: sponsor:temporal:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorTemporalAnalytics = "sponsor:temporal:{0}";

        /// <summary>
        /// Sponsor messaging analytics cache key
        /// Pattern: sponsor:messaging:{sponsorId}
        /// TTL: 15 minutes (configurable)
        /// </summary>
        public const string SponsorMessagingAnalytics = "sponsor:messaging:{0}";

        // ============================================
        // Reference Data Caches (Long TTL)
        // ============================================

        /// <summary>
        /// Subscription tiers cache key (all tiers)
        /// Pattern: subscription:tiers
        /// TTL: 1440 minutes (24 hours, configurable)
        /// </summary>
        public const string SubscriptionTiers = "subscription:tiers";

        /// <summary>
        /// Individual subscription tier cache key
        /// Pattern: subscription:tier:{tierId}
        /// TTL: 1440 minutes (24 hours, configurable)
        /// </summary>
        public const string SubscriptionTier = "subscription:tier:{0}";

        /// <summary>
        /// Configuration cache key
        /// Pattern: config:{configKey}
        /// TTL: 1440 minutes (24 hours, configurable)
        /// </summary>
        public const string Configuration = "config:{0}";

        /// <summary>
        /// Configuration category cache key
        /// Pattern: config:category:{category}
        /// TTL: 1440 minutes (24 hours, configurable)
        /// </summary>
        public const string ConfigurationCategory = "config:category:{0}";

        // ============================================
        // Cache Invalidation Patterns
        // ============================================

        /// <summary>
        /// Pattern to invalidate all dealer caches
        /// Pattern: dealer:*
        /// </summary>
        public const string AllDealerCaches = "dealer:*";

        /// <summary>
        /// Pattern to invalidate all sponsor caches
        /// Pattern: sponsor:*
        /// </summary>
        public const string AllSponsorCaches = "sponsor:*";

        /// <summary>
        /// Pattern to invalidate all admin statistics
        /// Pattern: admin:stats:*
        /// </summary>
        public const string AllAdminStatistics = "admin:stats:*";

        /// <summary>
        /// Pattern to invalidate all user statistics
        /// Pattern: admin:stats:users:*
        /// </summary>
        public const string AllUserStatistics = "admin:stats:users:*";

        /// <summary>
        /// Pattern to invalidate all subscription statistics
        /// Pattern: admin:stats:subscriptions:*
        /// </summary>
        public const string AllSubscriptionStatistics = "admin:stats:subscriptions:*";

        /// <summary>
        /// Pattern to invalidate all sponsorship statistics
        /// Pattern: admin:stats:sponsorship:*
        /// </summary>
        public const string AllSponsorshipStatistics = "admin:stats:sponsorship:*";

        /// <summary>
        /// Pattern to invalidate all subscription tiers
        /// Pattern: subscription:tier:*
        /// </summary>
        public const string AllSubscriptionTiers = "subscription:tier:*";

        /// <summary>
        /// Pattern to invalidate all configurations
        /// Pattern: config:*
        /// </summary>
        public const string AllConfigurations = "config:*";
    }
}
