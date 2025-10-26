using Core.Entities;
using System;
using System.Collections.Generic;

namespace Entities.Concrete
{
    /// <summary>
    /// Feature registry for subscription tier-based features
    /// Centralized feature definitions with configuration support
    /// </summary>
    public class Feature : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Unique feature identifier (e.g., "messaging", "smart_links")
        /// Used in code for feature checks
        /// </summary>
        public string FeatureKey { get; set; }
        
        /// <summary>
        /// Display name for admin UI (e.g., "Messaging", "Smart Links")
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Feature description for documentation
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Feature category (Communication, Analytics, Marketing, etc.)
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// Default configuration JSON for this feature
        /// Used when tier-specific configuration is not provided
        /// </summary>
        public string DefaultConfigJson { get; set; }
        
        /// <summary>
        /// Whether this feature requires configuration
        /// If true, ConfigurationJson must be provided when enabling
        /// </summary>
        public bool RequiresConfiguration { get; set; }
        
        /// <summary>
        /// JSON schema for validating feature configuration
        /// </summary>
        public string ConfigurationSchema { get; set; }
        
        /// <summary>
        /// Whether this feature is currently active
        /// Inactive features are hidden from admin UI
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Whether this feature is deprecated
        /// Deprecated features show warning in admin UI
        /// </summary>
        public bool IsDeprecated { get; set; }
        
        /// <summary>
        /// When this feature was created
        /// </summary>
        public DateTime CreatedDate { get; set; }
        
        /// <summary>
        /// When this feature was last modified
        /// </summary>
        public DateTime? ModifiedDate { get; set; }
        
        // Navigation Properties
        
        /// <summary>
        /// Tier-feature mappings (which tiers have access to this feature)
        /// </summary>
        public virtual ICollection<TierFeature> TierFeatures { get; set; }
    }
}
