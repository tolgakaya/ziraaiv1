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
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactPerson { get; set; }
        public string CompanyType { get; set; }
        public string BusinessModel { get; set; }
    }

    public class UpdateSponsorProfileDto : IDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDescription { get; set; }
        public string SponsorLogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactPerson { get; set; }
        public string LinkedInUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
    }
}