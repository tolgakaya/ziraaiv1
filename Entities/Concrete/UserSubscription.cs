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

        /// <summary>
        /// Foreign key to PaymentTransaction (iyzico payment integration)
        /// Links this subscription to the actual payment gateway transaction
        /// </summary>
        public int? PaymentTransactionId { get; set; }
        
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
        
        // Sponsorship Queue System (NO multiple active sponsorships allowed)
        public SubscriptionQueueStatus QueueStatus { get; set; } = SubscriptionQueueStatus.Active;
        public DateTime? QueuedDate { get; set; } // When code was redeemed (if queued/pending)
        public DateTime? ActivatedDate { get; set; } // When subscription actually became active
        public int? PreviousSponsorshipId { get; set; } // FK to sponsorship this is waiting for
        
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

        [JsonIgnore]
        public virtual UserSubscription PreviousSponsorship { get; set; } // Navigation for queue system

        /// <summary>
        /// Navigation property to PaymentTransaction
        /// </summary>
        [JsonIgnore]
        public virtual PaymentTransaction PaymentTransaction { get; set; }
    }
}