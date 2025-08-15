using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Collections.Generic;
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
        
        // Company Features (tier-independent)
        public bool IsVerifiedCompany { get; set; } // Company verification status
        public string CompanyType { get; set; } // Agriculture, Technology, Retail, etc.
        public string BusinessModel { get; set; } // B2B, B2C, B2B2C
        
        // Verification and Status
        public bool IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }
        public string VerificationNotes { get; set; }
        public bool IsActive { get; set; }
        
        // Statistics (calculated from purchases)
        public int TotalPurchases { get; set; } // Total package purchases
        public int TotalCodesGenerated { get; set; } // Total codes generated across all purchases
        public int TotalCodesRedeemed { get; set; } // Total codes redeemed by farmers
        public decimal TotalInvestment { get; set; } // Total amount spent on all purchases
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual User Sponsor { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<SponsorshipPurchase> SponsorshipPurchases { get; set; }
        
        [JsonIgnore]
        public virtual User CreatedByUser { get; set; }
        
        [JsonIgnore]
        public virtual User UpdatedByUser { get; set; }
        
        public SponsorProfile()
        {
            SponsorshipPurchases = new HashSet<SponsorshipPurchase>();
        }
    }
}