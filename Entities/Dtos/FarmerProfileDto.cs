using System;
using Core.Entities;
using Core.Entities.Concrete;

namespace Entities.Dtos
{
    /// <summary>
    /// Farmer profile response DTO - excludes sensitive data
    /// </summary>
    public class FarmerProfileDto : IDto
    {
        public int UserId { get; set; }
        public long CitizenId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhones { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? Gender { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public bool Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime RecordDate { get; set; }
        public DateTime UpdateContactDate { get; set; }

        // Avatar information
        public string AvatarUrl { get; set; }
        public string AvatarThumbnailUrl { get; set; }
        public DateTime? AvatarUpdatedDate { get; set; }

        // Referral information
        public string RegistrationReferralCode { get; set; }

        // Admin action tracking (read-only for farmer)
        public DateTime? DeactivatedDate { get; set; }
        public string DeactivationReason { get; set; }
    }
}
