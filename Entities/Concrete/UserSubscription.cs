using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class UserSubscription : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionTierId { get; set; }
        
        // Subscription Period
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool AutoRenew { get; set; }
        
        // Payment Information
        public string PaymentMethod { get; set; } // CreditCard, BankTransfer, etc.
        public string PaymentReference { get; set; } // Transaction ID or reference
        public decimal PaidAmount { get; set; }
        public string Currency { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextPaymentDate { get; set; }
        
        // Usage Tracking
        public int CurrentDailyUsage { get; set; }
        public int CurrentMonthlyUsage { get; set; }
        public DateTime? LastUsageResetDate { get; set; } // For daily reset tracking
        public DateTime? MonthlyUsageResetDate { get; set; } // For monthly reset tracking
        
        // Status
        public string Status { get; set; } // Active, Expired, Cancelled, Suspended
        public string CancellationReason { get; set; }
        public DateTime? CancellationDate { get; set; }
        
        // Trial Information
        public bool IsTrialSubscription { get; set; }
        public DateTime? TrialEndDate { get; set; }
        
        // Sponsorship Information
        public int? SponsorshipCodeId { get; set; } // Which sponsorship code was used
        public int? SponsorId { get; set; } // Sponsor company user ID
        public bool IsSponsoredSubscription { get; set; } // Is this a sponsored subscription
        public string SponsorshipNotes { get; set; } // Any notes from sponsor
        
        // Audit fields
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedUserId { get; set; }
        public int? UpdatedUserId { get; set; }
        
        // Navigation properties (for EF Core)

        /// <summary>
        /// Analysis credits earned through referrals (separate from subscription quota)
        /// Unlimited accumulation, never expires (per user requirements)
        /// Used before subscription quota when creating plant analysis
        /// </summary>
        public int ReferralCredits { get; set; }
        [JsonIgnore]
        public virtual SubscriptionTier SubscriptionTier { get; set; }
        
        [JsonIgnore]
        public virtual SponsorshipCode SponsorshipCode { get; set; }
        
        [JsonIgnore]
        public virtual User Sponsor { get; set; }
    }
}