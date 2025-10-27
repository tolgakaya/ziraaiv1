using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Junction table mapping subscription tiers to features
    /// Defines which features are available for each tier with optional configuration
    /// Supports scheduled activation/deactivation for promotions and A/B testing
    /// </summary>
    public class TierFeature : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Subscription tier ID (Trial=1, S=2, M=3, L=4, XL=5)
        /// </summary>
        public int SubscriptionTierId { get; set; }
        
        /// <summary>
        /// Feature ID from Features table
        /// </summary>
        public int FeatureId { get; set; }
        
        /// <summary>
        /// Whether this feature is currently enabled for this tier
        /// Can be toggled via admin UI without code deployment
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// Tier-specific configuration JSON
        /// Overrides Feature.DefaultConfigJson when provided
        /// Example: {"maxLinksPerSponsor": 50, "requiresApproval": false}
        /// </summary>
        public string ConfigurationJson { get; set; }
        
        /// <summary>
        /// When this feature should become active (optional)
        /// Used for scheduled promotions (e.g., Black Friday campaigns)
        /// Null = active immediately
        /// </summary>
        public DateTime? EffectiveDate { get; set; }
        
        /// <summary>
        /// When this feature should be deactivated (optional)
        /// Used for limited-time promotions or A/B testing
        /// Null = never expires
        /// </summary>
        public DateTime? ExpiryDate { get; set; }
        
        /// <summary>
        /// When this tier-feature mapping was created
        /// </summary>
        public DateTime CreatedDate { get; set; }
        
        /// <summary>
        /// User ID who created this mapping
        /// For audit trail
        /// </summary>
        public int CreatedByUserId { get; set; }
        
        /// <summary>
        /// When this mapping was last updated
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// User ID who last modified this mapping
        /// For audit trail
        /// </summary>
        public int? ModifiedByUserId { get; set; }
        
        // Navigation Properties
        
        /// <summary>
        /// Subscription tier this feature is enabled for
        /// </summary>
        public virtual SubscriptionTier SubscriptionTier { get; set; }
        
        /// <summary>
        /// Feature definition
        /// </summary>
        public virtual Feature Feature { get; set; }
    }
}
