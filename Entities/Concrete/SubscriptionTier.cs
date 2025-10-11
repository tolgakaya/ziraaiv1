using Core.Entities;
using System;

namespace Entities.Concrete
{
    public class SubscriptionTier : IEntity
    {
        public int Id { get; set; }
        public string TierName { get; set; } // S, M, L, XL
        public string DisplayName { get; set; } // Small, Medium, Large, Extra Large
        public string Description { get; set; }
        
        // Request Limits
        public int DailyRequestLimit { get; set; }
        public int MonthlyRequestLimit { get; set; }
        
        // Pricing
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public string Currency { get; set; } // TRY, USD, EUR
        
        // Sponsorship Purchase Limits
        public int MinPurchaseQuantity { get; set; } = 10; // Minimum codes per purchase
        public int MaxPurchaseQuantity { get; set; } = 10000; // Maximum codes per purchase
        public int RecommendedQuantity { get; set; } = 100; // Recommended/default quantity
        
        // Features
        public bool PrioritySupport { get; set; }
        public bool AdvancedAnalytics { get; set; }
        public bool ApiAccess { get; set; }
        public int ResponseTimeHours { get; set; } // Expected response time in hours
        public string AdditionalFeatures { get; set; } // JSON array of additional features
        
        // Status
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        
        // Audit fields
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedUserId { get; set; }
        public int? UpdatedUserId { get; set; }
    }
}