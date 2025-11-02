using Core.Entities;
using System;

namespace Entities.Dtos
{
    public class SponsorProfileDto : IDto
    {
        public int Id { get; set; }
        public int SponsorId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
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
        
        public string CompanyType { get; set; }
        public string BusinessModel { get; set; }
        public bool IsVerifiedCompany { get; set; }
        public bool IsActive { get; set; }
        public int TotalPurchases { get; set; }
        public int TotalCodesGenerated { get; set; }
        public int TotalCodesRedeemed { get; set; }
        public decimal TotalInvestment { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateSponsorProfileDto : IDto
    {
        // Required fields
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        
        // Optional fields
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string ContactPerson { get; set; }
        public string CompanyType { get; set; }
        public string BusinessModel { get; set; }
        public string Password { get; set; } // Optional: For phone-registered users to enable email+password login
        
        // Social Media Links (Optional)
        public string LinkedInUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        
        // Business Information (Optional)
        public string TaxNumber { get; set; }
        public string TradeRegistryNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
    }

    public class UpdateSponsorProfileDto : IDto
    {
        // All fields optional for update (partial update support)
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactPerson { get; set; }
        public string CompanyType { get; set; }
        public string BusinessModel { get; set; }
        
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
        
        // Special: Password update (optional, validates old password required)
        public string Password { get; set; }
    }
}