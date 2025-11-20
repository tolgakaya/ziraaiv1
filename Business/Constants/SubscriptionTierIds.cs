namespace Business.Constants
{
    /// <summary>
    /// Subscription tier ID constants to eliminate magic numbers
    /// Database IDs from SubscriptionTiers table
    /// </summary>
    public static class SubscriptionTierIds
    {
        /// <summary>
        /// Trial tier - 1 daily analysis, 30 monthly
        /// No premium features
        /// </summary>
        public const int Trial = 1;
        
        /// <summary>
        /// S (Small) tier - 5 daily, 50 monthly
        /// 30% data access, no messaging
        /// </summary>
        public const int Small = 2;
        
        /// <summary>
        /// M (Medium) tier - 20 daily, 200 monthly
        /// 60% data access, analytics, logo visibility
        /// No messaging or smart links
        /// </summary>
        public const int Medium = 3;
        
        /// <summary>
        /// L (Large) tier - 50 daily, 500 monthly
        /// MESSAGING enabled, 100% data access, API, profile visibility
        /// No smart links or voice messages
        /// </summary>
        public const int Large = 4;
        
        /// <summary>
        /// XL (Extra Large) tier - 200 daily, 2000 monthly
        /// ALL FEATURES: Smart links, voice messages, full access
        /// </summary>
        public const int ExtraLarge = 5;
    }
}
