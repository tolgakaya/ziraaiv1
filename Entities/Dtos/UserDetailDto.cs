using System;
using System.Collections.Generic;
using Core.Entities;

namespace Entities.Dtos
{
    /// <summary>
    /// Detailed user DTO including admin-specific fields
    /// Used for admin user management operations
    /// </summary>
    public class UserDetailDto : IDto
    {
        public int UserId { get; set; }
        public long CitizenId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhones { get; set; }
        public bool Status { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? Gender { get; set; }
        public DateTime RecordDate { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public DateTime UpdateContactDate { get; set; }
        public string RegistrationReferralCode { get; set; }
        public string AvatarUrl { get; set; }
        public string AvatarThumbnailUrl { get; set; }
        public DateTime? AvatarUpdatedDate { get; set; }
        
        /// <summary>
        /// Admin-specific fields
        /// </summary>
        public bool IsActive { get; set; }
        public DateTime? DeactivatedDate { get; set; }
        public int? DeactivatedBy { get; set; }
        public string DeactivatedByName { get; set; }
        public string DeactivationReason { get; set; }
        
        /// <summary>
        /// User roles
        /// </summary>
        public List<string> Roles { get; set; }
        
        /// <summary>
        /// User permissions/claims
        /// </summary>
        public List<string> Claims { get; set; }
        
        /// <summary>
        /// Subscription information
        /// </summary>
        public UserSubscriptionDto ActiveSubscription { get; set; }
        
        /// <summary>
        /// Usage statistics
        /// </summary>
        public int TotalPlantAnalyses { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }
}
