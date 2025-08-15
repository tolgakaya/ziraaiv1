using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class SponsorProfile : IEntity
    {
        public int Id { get; set; }
        
        // Sponsor Information
        public int SponsorId { get; set; } // User ID of the sponsor
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        
        // Contact Information
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactPerson { get; set; }
        
        // Social Media Links
        public string LinkedInUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        
        // Business Information
        public string TaxNumber { get; set; }
        public string TradeRegistryNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        
        // Tier and Features
        public int CurrentSubscriptionTierId { get; set; }
        public string VisibilityLevel { get; set; } // ResultOnly, StartAndResult, AllScreens
        public string DataAccessLevel { get; set; } // Basic30, Medium60, Full100
        public bool HasMessaging { get; set; }
        public bool HasSmartLinking { get; set; }
        
        // Verification and Status
        public bool IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string VerificationNotes { get; set; }
        public bool IsActive { get; set; }
        
        // Statistics
        public int TotalSponsored { get; set; } // Total farmers sponsored
        public int ActiveSponsored { get; set; } // Currently active sponsorships
        public decimal TotalInvestment { get; set; } // Total amount spent on sponsorships
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual User Sponsor { get; set; }
        
        [JsonIgnore]
        public virtual SubscriptionTier CurrentSubscriptionTier { get; set; }
        
        [JsonIgnore]
        public virtual User CreatedByUser { get; set; }
        
        [JsonIgnore]
        public virtual User UpdatedByUser { get; set; }
    }
}