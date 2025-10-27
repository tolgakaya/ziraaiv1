using Business.Constants;
using Entities.Concrete;
using System.Collections.Generic;

namespace Business.Seeds
{
    /// <summary>
    /// Feature registry seed data for database-driven tier permission system
    /// Defines available features and their tier assignments
    /// </summary>
    public static class FeatureSeeds
    {
        public static List<Feature> GetDefaultFeatures()
        {
            return new List<Feature>
            {
                new Feature
                {
                    Id = 1,
                    FeatureKey = "messaging",
                    DisplayName = "Messaging",
                    Description = "Send messages to farmers about their analyses",
                    RequiresConfiguration = false,
                    IsActive = true,
                    IsDeprecated = false
                },
                new Feature
                {
                    Id = 2,
                    FeatureKey = "voice_messages",
                    DisplayName = "Voice Messages",
                    Description = "Send voice messages to farmers",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"maxDurationSeconds\": 300, \"maxFileSizeMB\": 10}",
                    IsActive = true,
                    IsDeprecated = false
                },
                new Feature
                {
                    Id = 3,
                    FeatureKey = "smart_links",
                    DisplayName = "Smart Links",
                    Description = "Create and manage smart links for product promotion",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"maxLinksPerSponsor\": 50, \"requiresApproval\": false}",
                    IsActive = true,
                    IsDeprecated = false
                },
                new Feature
                {
                    Id = 4,
                    FeatureKey = "advanced_analytics",
                    DisplayName = "Advanced Analytics",
                    Description = "Access to advanced analytics dashboards",
                    RequiresConfiguration = false,
                    IsActive = true,
                    IsDeprecated = false
                },
                new Feature
                {
                    Id = 5,
                    FeatureKey = "api_access",
                    DisplayName = "API Access",
                    Description = "Programmatic access to ZiraAI API",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"rateLimit\": 1000, \"rateLimitWindow\": \"hour\"}",
                    IsActive = true,
                    IsDeprecated = false
                },
                new Feature
                {
                    Id = 6,
                    FeatureKey = "sponsor_visibility",
                    DisplayName = "Sponsor Visibility",
                    Description = "Show sponsor logo and profile to farmers",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"logoVisibility\": true, \"profileVisibility\": true}",
                    IsActive = true,
                    IsDeprecated = false
                },
                new Feature
                {
                    Id = 7,
                    FeatureKey = "data_access_percentage",
                    DisplayName = "Farmer Data Access",
                    Description = "Percentage of farmer data accessible to sponsor",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"accessPercentage\": 100}",
                    IsActive = true,
                    IsDeprecated = false
                },
                new Feature
                {
                    Id = 8,
                    FeatureKey = "priority_support",
                    DisplayName = "Priority Support",
                    Description = "Faster response times for support requests",
                    RequiresConfiguration = true,
                    DefaultConfigJson = "{\"responseTimeHours\": 12}",
                    IsActive = true,
                    IsDeprecated = false
                }
            };
        }
        
        public static List<TierFeature> GetDefaultTierFeatures()
        {
            return new List<TierFeature>
            {
                // Trial Tier (ID=1) - No premium features
                
                // S Tier (ID=2) - Data access only
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Small,
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 30}",
                    CreatedByUserId = 1 // System
                },
                
                // M Tier (ID=3) - Analytics + Logo + Data
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Medium,
                    FeatureId = 4, // advanced_analytics
                    IsEnabled = true,
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Medium,
                    FeatureId = 6, // sponsor_visibility
                    IsEnabled = true,
                    ConfigurationJson = "{\"logoVisibility\": true, \"profileVisibility\": false}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Medium,
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 60}",
                    CreatedByUserId = 1
                },
                
                // L Tier (ID=4) - Messaging + API + Full visibility
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Large,
                    FeatureId = 1, // messaging
                    IsEnabled = true,
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Large,
                    FeatureId = 4, // advanced_analytics
                    IsEnabled = true,
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Large,
                    FeatureId = 5, // api_access
                    IsEnabled = true,
                    ConfigurationJson = "{\"rateLimit\": 1000, \"rateLimitWindow\": \"hour\"}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Large,
                    FeatureId = 6, // sponsor_visibility
                    IsEnabled = true,
                    ConfigurationJson = "{\"logoVisibility\": true, \"profileVisibility\": true}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Large,
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 100}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.Large,
                    FeatureId = 8, // priority_support
                    IsEnabled = true,
                    ConfigurationJson = "{\"responseTimeHours\": 12}",
                    CreatedByUserId = 1
                },
                
                // XL Tier (ID=5) - ALL features including voice messages and smart links
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 1, // messaging
                    IsEnabled = true,
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 2, // voice_messages
                    IsEnabled = true,
                    ConfigurationJson = "{\"maxDurationSeconds\": 300, \"maxFileSizeMB\": 10}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 3, // smart_links
                    IsEnabled = true,
                    ConfigurationJson = "{\"maxLinksPerSponsor\": 50, \"requiresApproval\": false}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 4, // advanced_analytics
                    IsEnabled = true,
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 5, // api_access
                    IsEnabled = true,
                    ConfigurationJson = "{\"rateLimit\": 5000, \"rateLimitWindow\": \"hour\"}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 6, // sponsor_visibility
                    IsEnabled = true,
                    ConfigurationJson = "{\"logoVisibility\": true, \"profileVisibility\": true}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 7, // data_access_percentage
                    IsEnabled = true,
                    ConfigurationJson = "{\"accessPercentage\": 100}",
                    CreatedByUserId = 1
                },
                new TierFeature 
                { 
                    SubscriptionTierId = SubscriptionTierIds.ExtraLarge,
                    FeatureId = 8, // priority_support
                    IsEnabled = true,
                    ConfigurationJson = "{\"responseTimeHours\": 6}",
                    CreatedByUserId = 1
                }
            };
        }
    }
}
