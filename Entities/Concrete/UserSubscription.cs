using Core.Entities;
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
        
        // Audit fields
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedUserId { get; set; }
        public int? UpdatedUserId { get; set; }
        
        // Navigation properties (for EF Core)
        [JsonIgnore]
        public virtual SubscriptionTier SubscriptionTier { get; set; }
    }
}