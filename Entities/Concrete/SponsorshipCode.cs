using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class SponsorshipCode : IEntity
    {
        public int Id { get; set; }
        
        // Code Information
        public string Code { get; set; } // Unique sponsorship code (e.g., AGRI-2025-X3K9)
        public int SponsorId { get; set; } // Sponsor company user ID
        public int SubscriptionTierId { get; set; } // Which subscription tier this code provides
        public int SponsorshipPurchaseId { get; set; } // Link to bulk purchase record
        
        // Usage Information
        public bool IsUsed { get; set; }
        public int? UsedByUserId { get; set; } // Farmer who redeemed the code
        public DateTime? UsedDate { get; set; }
        public int? CreatedSubscriptionId { get; set; } // UserSubscription created from this code
        
        // Validity
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; } // Code expiration date
        public bool IsActive { get; set; } // Can be deactivated by sponsor
        
        // Additional Information
        public string Notes { get; set; } // Optional notes from sponsor
        public string DistributedTo { get; set; } // Optional: Name/info of intended recipient
        public string DistributionChannel { get; set; } // Email, SMS, Physical, etc.
        public DateTime? DistributionDate { get; set; }
        
        // Link Distribution Fields
        public string RedemptionLink { get; set; } // Generated redemption link
        public DateTime? LinkClickDate { get; set; } // First click timestamp
        public int LinkClickCount { get; set; } // Total click count
        public string RecipientPhone { get; set; } // Phone number for SMS/WhatsApp
        public string RecipientName { get; set; } // Recipient's name for personalized messages
        public DateTime? LinkSentDate { get; set; } // When the link was sent
        public string LinkSentVia { get; set; } // SMS, WhatsApp, Email
        public bool LinkDelivered { get; set; } // Delivery confirmation
        public string LastClickIpAddress { get; set; } // For tracking and security
        
        // Navigation properties (removed to prevent EF save conflicts)
        // Use foreign key IDs directly instead of navigation properties
    }
}