using Core.Entities.Concrete;
using System;
using System.Collections.Generic;

namespace Business.Seeds
{
    public static class OperationClaimSeeds
    {
        public static List<OperationClaim> GetDefaultOperationClaims()
        {
            return new List<OperationClaim>
            {
                // System Administration Claims
                new OperationClaim { Id = 1, Name = "Admin", Alias = "System Administrator", Description = "Full system access" },
                new OperationClaim { Id = 2, Name = "UserManagement", Alias = "User Management", Description = "Manage users" },
                new OperationClaim { Id = 3, Name = "RoleManagement", Alias = "Role Management", Description = "Manage roles and permissions" },
                new OperationClaim { Id = 4, Name = "ConfigurationManagement", Alias = "Configuration Management", Description = "Manage system configurations" },
                
                // User Roles
                new OperationClaim { Id = 5, Name = "Farmer", Alias = "Farmer", Description = "Farmer role access" },
                new OperationClaim { Id = 6, Name = "Sponsor", Alias = "Sponsor", Description = "Sponsor role access" },
                
                // Plant Analysis Claims
                new OperationClaim { Id = 10, Name = "PlantAnalysis.Create", Alias = "Create Plant Analysis", Description = "Create new plant analysis" },
                new OperationClaim { Id = 11, Name = "PlantAnalysis.Read", Alias = "View Plant Analysis", Description = "View plant analysis" },
                new OperationClaim { Id = 12, Name = "PlantAnalysis.Update", Alias = "Update Plant Analysis", Description = "Update plant analysis" },
                new OperationClaim { Id = 13, Name = "PlantAnalysis.Delete", Alias = "Delete Plant Analysis", Description = "Delete plant analysis" },
                new OperationClaim { Id = 14, Name = "PlantAnalysis.List", Alias = "List Plant Analysis", Description = "List all plant analyses" },
                new OperationClaim { Id = 15, Name = "PlantAnalysis.Export", Alias = "Export Plant Analysis", Description = "Export plant analysis data" },
                
                // Subscription Management Claims
                new OperationClaim { Id = 20, Name = "Subscription.Create", Alias = "Create Subscription", Description = "Create new subscription" },
                new OperationClaim { Id = 21, Name = "Subscription.Read", Alias = "View Subscription", Description = "View subscription details" },
                new OperationClaim { Id = 22, Name = "Subscription.Update", Alias = "Update Subscription", Description = "Update subscription" },
                new OperationClaim { Id = 23, Name = "Subscription.Cancel", Alias = "Cancel Subscription", Description = "Cancel subscription" },
                new OperationClaim { Id = 24, Name = "Subscription.List", Alias = "List Subscriptions", Description = "List all subscriptions" },
                new OperationClaim { Id = 25, Name = "SubscriptionTier.Manage", Alias = "Manage Subscription Tiers", Description = "Manage subscription tiers" },
                
                // Sponsorship Claims
                new OperationClaim { Id = 30, Name = "Sponsorship.Create", Alias = "Create Sponsorship", Description = "Create sponsorship" },
                new OperationClaim { Id = 31, Name = "Sponsorship.Read", Alias = "View Sponsorship", Description = "View sponsorship details" },
                new OperationClaim { Id = 32, Name = "Sponsorship.Update", Alias = "Update Sponsorship", Description = "Update sponsorship" },
                new OperationClaim { Id = 33, Name = "Sponsorship.Delete", Alias = "Delete Sponsorship", Description = "Delete sponsorship" },
                new OperationClaim { Id = 34, Name = "Sponsorship.List", Alias = "List Sponsorships", Description = "List all sponsorships" },
                new OperationClaim { Id = 35, Name = "SponsorshipCode.Generate", Alias = "Generate Sponsorship Codes", Description = "Generate sponsorship codes" },
                new OperationClaim { Id = 36, Name = "SponsorshipCode.Distribute", Alias = "Distribute Sponsorship Codes", Description = "Distribute sponsorship codes" },
                new OperationClaim { Id = 37, Name = "SponsorshipPurchase.Create", Alias = "Create Sponsorship Purchase", Description = "Create sponsorship purchase" },
                
                // Sponsor Profile Claims
                new OperationClaim { Id = 40, Name = "SponsorProfile.Create", Alias = "Create Sponsor Profile", Description = "Create sponsor profile" },
                new OperationClaim { Id = 41, Name = "SponsorProfile.Read", Alias = "View Sponsor Profile", Description = "View sponsor profile" },
                new OperationClaim { Id = 42, Name = "SponsorProfile.Update", Alias = "Update Sponsor Profile", Description = "Update sponsor profile" },
                new OperationClaim { Id = 43, Name = "SponsorProfile.Delete", Alias = "Delete Sponsor Profile", Description = "Delete sponsor profile" },
                new OperationClaim { Id = 44, Name = "SponsorContact.Manage", Alias = "Manage Sponsor Contacts", Description = "Manage sponsor contacts" },
                new OperationClaim { Id = 45, Name = "SponsorRequest.Manage", Alias = "Manage Sponsor Requests", Description = "Manage sponsor requests via WhatsApp" },
                
                // Smart Link Claims (XL Tier)
                new OperationClaim { Id = 50, Name = "SmartLink.Create", Alias = "Create Smart Link", Description = "Create smart link (XL tier only)" },
                new OperationClaim { Id = 51, Name = "SmartLink.Read", Alias = "View Smart Link", Description = "View smart link details" },
                new OperationClaim { Id = 52, Name = "SmartLink.Update", Alias = "Update Smart Link", Description = "Update smart link" },
                new OperationClaim { Id = 53, Name = "SmartLink.Delete", Alias = "Delete Smart Link", Description = "Delete smart link" },
                new OperationClaim { Id = 54, Name = "SmartLink.Analytics", Alias = "View Smart Link Analytics", Description = "View smart link analytics" },
                
                // Analytics Claims
                new OperationClaim { Id = 60, Name = "Analytics.Dashboard", Alias = "View Analytics Dashboard", Description = "View analytics dashboard" },
                new OperationClaim { Id = 61, Name = "Analytics.Reports", Alias = "Generate Reports", Description = "Generate analytics reports" },
                new OperationClaim { Id = 62, Name = "Analytics.Export", Alias = "Export Analytics", Description = "Export analytics data" },
                
                // Log and Audit Claims
                new OperationClaim { Id = 70, Name = "Logs.View", Alias = "View Logs", Description = "View system logs" },
                new OperationClaim { Id = 71, Name = "Logs.Export", Alias = "Export Logs", Description = "Export system logs" },
                new OperationClaim { Id = 72, Name = "SecurityEvents.View", Alias = "View Security Events", Description = "View security events" },
                
                // API Access Claims
                new OperationClaim { Id = 80, Name = "API.FullAccess", Alias = "Full API Access", Description = "Full API access" },
                new OperationClaim { Id = 81, Name = "API.ReadOnly", Alias = "Read-Only API Access", Description = "Read-only API access" },
                new OperationClaim { Id = 82, Name = "API.PlantAnalysis", Alias = "Plant Analysis API", Description = "Plant analysis API access" },
                
                // Mobile App Claims
                new OperationClaim { Id = 90, Name = "Mobile.Access", Alias = "Mobile App Access", Description = "Mobile application access" },
                new OperationClaim { Id = 91, Name = "Mobile.PushNotifications", Alias = "Push Notifications", Description = "Receive push notifications" },

                // Admin Query Claims (Handler-specific)
                new OperationClaim { Id = 100, Name = "GetAllSubscriptionsQuery", Alias = "Get All Subscriptions", Description = "Query all subscriptions" },
                new OperationClaim { Id = 101, Name = "GetSubscriptionDetailsQuery", Alias = "Get Subscription Details", Description = "Query detailed subscription information" },
                new OperationClaim { Id = 102, Name = "GetSubscriptionByIdQuery", Alias = "Get Subscription By ID", Description = "Query subscription by ID" },
                new OperationClaim { Id = 103, Name = "AssignSubscriptionCommand", Alias = "Assign Subscription", Description = "Assign subscription to user" },
                new OperationClaim { Id = 104, Name = "ExtendSubscriptionCommand", Alias = "Extend Subscription", Description = "Extend user subscription" },
                new OperationClaim { Id = 105, Name = "CancelSubscriptionCommand", Alias = "Cancel Subscription", Description = "Cancel user subscription" },
                new OperationClaim { Id = 106, Name = "BulkCancelSubscriptionsCommand", Alias = "Bulk Cancel Subscriptions", Description = "Cancel multiple subscriptions" },
                new OperationClaim { Id = 132, Name = "GetAllSponsorsQuery", Alias = "Get All Sponsors", Description = "Query all users with Sponsor role (GroupId = 3)" },

                // Admin Sponsor View Claims (Phase 1)
                new OperationClaim { Id = 133, Name = "GetSponsorAnalysesAsAdminQuery", Alias = "Get Sponsor Analyses (Admin)", Description = "Admin view of sponsor's analyses with filters and messaging" },
                new OperationClaim { Id = 134, Name = "GetSponsorAnalysisDetailAsAdminQuery", Alias = "Get Sponsor Analysis Detail (Admin)", Description = "Admin view of detailed sponsor analysis with messages" },
                new OperationClaim { Id = 135, Name = "GetSponsorMessagesAsAdminQuery", Alias = "Get Sponsor Messages (Admin)", Description = "Admin view of all sponsor messages with filters" },
                new OperationClaim { Id = 139, Name = "SendMessageAsSponsorCommand", Alias = "Send Message As Sponsor (Admin)", Description = "Admin send message on behalf of sponsor" },

                // Non-Sponsored Farmer Analytics Claims (Phase 2)
                new OperationClaim { Id = 136, Name = "GetNonSponsoredAnalysesQuery", Alias = "Get Non-Sponsored Analyses", Description = "Query analyses without sponsorship for opportunity identification" },
                new OperationClaim { Id = 137, Name = "GetNonSponsoredFarmerDetailQuery", Alias = "Get Non-Sponsored Farmer Detail", Description = "Detailed farmer profile for sponsorship targeting" },

                // Sponsorship Comparison Analytics (Phase 3)
                new OperationClaim { Id = 138, Name = "GetSponsorshipComparisonAnalyticsQuery", Alias = "Get Sponsorship Comparison Analytics", Description = "Compare sponsored vs non-sponsored analysis metrics" }
            };
        }
    }
}