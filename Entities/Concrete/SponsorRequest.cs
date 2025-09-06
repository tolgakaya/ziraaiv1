using Core.Entities;
using Core.Entities.Concrete;
using System;

namespace Entities.Concrete
{
    public class SponsorRequest : IEntity
    {
        public int Id { get; set; }
        public int FarmerId { get; set; }
        public int SponsorId { get; set; }
        public string FarmerPhone { get; set; }    // +905551234567
        public string SponsorPhone { get; set; }   // +905557654321
        public string RequestMessage { get; set; }
        public string RequestToken { get; set; }   // Hashed verification token
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }         // Pending, Approved, Rejected, Expired
        public DateTime? ApprovalDate { get; set; }
        public int? ApprovedSubscriptionTierId { get; set; }
#nullable enable
        public string? ApprovalNotes { get; set; }
        public string? GeneratedSponsorshipCode { get; set; }
#nullable restore
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // Navigation properties
        public virtual User Farmer { get; set; }
        public virtual User Sponsor { get; set; }
        public virtual SubscriptionTier ApprovedSubscriptionTier { get; set; }
    }
}