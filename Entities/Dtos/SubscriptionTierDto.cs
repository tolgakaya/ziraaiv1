using System.Collections.Generic;

namespace Entities.Dtos
{
    public class SubscriptionTierDto
    {
        public int Id { get; set; }
        public string TierName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int DailyRequestLimit { get; set; }
        public int MonthlyRequestLimit { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal? YearlyPrice { get; set; }
        public string Currency { get; set; }
        
        // Sponsorship Purchase Limits
        public int MinPurchaseQuantity { get; set; }
        public int MaxPurchaseQuantity { get; set; }
        public int RecommendedQuantity { get; set; }
        
        public bool PrioritySupport { get; set; }
        public bool AdvancedAnalytics { get; set; }
        public bool ApiAccess { get; set; }
        public int ResponseTimeHours { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public bool IsActive { get; set; }
    }
}