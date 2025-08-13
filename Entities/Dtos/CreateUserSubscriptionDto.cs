using System;

namespace Entities.Dtos
{
    public class CreateUserSubscriptionDto
    {
        public int SubscriptionTierId { get; set; }
        public int? DurationMonths { get; set; } = 1;
        public DateTime? StartDate { get; set; }
        public bool AutoRenew { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public decimal? PaidAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public bool IsTrialSubscription { get; set; }
        public int? TrialDays { get; set; } = 7;
    }
}