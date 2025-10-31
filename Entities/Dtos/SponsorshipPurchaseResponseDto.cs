using Entities.Concrete;
using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class SponsorshipPurchaseResponseDto
    {
        public int Id { get; set; }
        public int SponsorId { get; set; }
        public int SubscriptionTierId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? PaymentCompletedDate { get; set; }
        public string CompanyName { get; set; }
        public int CodesGenerated { get; set; }
        public int CodesUsed { get; set; }
        public string CodePrefix { get; set; }
        public int ValidityDays { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Include generated sponsorship codes
        public List<SponsorshipCodeDto> GeneratedCodes { get; set; } = new List<SponsorshipCodeDto>();
    }

    public class SponsorshipCodeDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string TierName { get; set; }
        public bool IsUsed { get; set; }
        public bool IsActive { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime? UsedDate { get; set; }
        public int? UsedByUserId { get; set; }
        public string UsedByUserName { get; set; }
        public string Notes { get; set; }

        // Dealer-specific fields
        public DateTime? CreatedDate { get; set; }
        public DateTime? TransferredAt { get; set; }
        public DateTime? DistributionDate { get; set; }
        public string RecipientPhone { get; set; }
        public string RecipientName { get; set; }
        public string DistributedTo { get; set; }
        public string SubscriptionTier { get; set; }
    }
}