using System;

namespace Entities.Dtos
{
    public class SubscriptionUsageStatusDto
    {
        public bool HasActiveSubscription { get; set; }
        public string SubscriptionStatus { get; set; }
        public string TierName { get; set; }
        public string TierDisplayName { get; set; }
        
        // Daily Usage
        public int DailyUsed { get; set; }
        public int DailyLimit { get; set; }
        public int DailyRemaining { get; set; }
        public double DailyUsagePercentage { get; set; }
        public DateTime? NextDailyReset { get; set; }
        
        // Monthly Usage
        public int MonthlyUsed { get; set; }
        public int MonthlyLimit { get; set; }
        public int MonthlyRemaining { get; set; }
        public double MonthlyUsagePercentage { get; set; }
        public DateTime? NextMonthlyReset { get; set; }
        
        // Subscription Info
        public DateTime? SubscriptionEndDate { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public bool CanMakeRequest { get; set; }
        public string LimitExceededMessage { get; set; }

        // Referral Credits
        public int ReferralCredits { get; set; }
    }
}