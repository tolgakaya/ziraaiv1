using System;
using System.Collections.Generic;
using Entities.Concrete;

namespace Entities.Dtos
{
    public class UserSubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionTierId { get; set; }
        public string TierName { get; set; }
        public string TierDisplayName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool AutoRenew { get; set; }
        public string Status { get; set; }
        public int CurrentDailyUsage { get; set; }
        public int DailyRequestLimit { get; set; }
        public int CurrentMonthlyUsage { get; set; }
        public int MonthlyRequestLimit { get; set; }
        public DateTime? LastUsageResetDate { get; set; }
        public DateTime? MonthlyUsageResetDate { get; set; }
        public bool IsTrialSubscription { get; set; }
        public DateTime? TrialEndDate { get; set; }

        // Queue support
        public SubscriptionQueueStatus? QueueStatus { get; set; }
        public DateTime? QueuedDate { get; set; }
        public int? PreviousSponsorshipId { get; set; }
        public List<QueuedSubscriptionDto> QueuedSubscriptions { get; set; }
    }

    public class QueuedSubscriptionDto
    {
        public int Id { get; set; }
        public int SubscriptionTierId { get; set; }
        public string TierName { get; set; }
        public string TierDisplayName { get; set; }
        public SubscriptionQueueStatus QueueStatus { get; set; }
        public DateTime QueuedDate { get; set; }
        public int? PreviousSponsorshipId { get; set; }
        public string Status { get; set; }
        public bool IsSponsoredSubscription { get; set; }
    }
}