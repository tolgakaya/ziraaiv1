using System;
using Entities.Concrete;

namespace Entities.Dtos
{
    /// <summary>
    /// Detailed subscription information including user, usage, and analysis statistics
    /// </summary>
    public class SubscriptionDetailDto
    {
        // Subscription Basic Information
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionTierId { get; set; }
        public string TierName { get; set; }
        public string TierDisplayName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public bool IsSponsoredSubscription { get; set; }
        public SubscriptionQueueStatus QueueStatus { get; set; }
        public bool AutoRenew { get; set; }
        public DateTime CreatedDate { get; set; }

        // Subscription Limits (from SubscriptionTier)
        public int DailyRequestLimit { get; set; }
        public int MonthlyRequestLimit { get; set; }

        // Current Usage (from UserSubscription)
        public int CurrentDailyUsage { get; set; }
        public int CurrentMonthlyUsage { get; set; }

        // Calculated Usage Metrics
        public int RemainingDailyRequests { get; set; }
        public int RemainingMonthlyRequests { get; set; }
        public double DailyUsagePercentage { get; set; }
        public double MonthlyUsagePercentage { get; set; }

        // Time Metrics
        public int RemainingDays { get; set; }
        public int TotalDurationDays { get; set; }
        public double TimeUsagePercentage { get; set; }

        // Referral Credits
        public int ReferralCredits { get; set; }

        // User Information
        public UserSummaryDto User { get; set; }

        // Sponsor Information (if sponsored)
        public SponsorSummaryDto Sponsor { get; set; }

        // Analysis Statistics
        public AnalysisStatsDto AnalysisStats { get; set; }

        // Queue Information (if queued)
        public QueueInfoDto QueueInfo { get; set; }

        // Notes
        public string SponsorshipNotes { get; set; }
        public string CancellationReason { get; set; }
        public DateTime? CancellationDate { get; set; }
    }

    /// <summary>
    /// User summary information
    /// </summary>
    public class UserSummaryDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhones { get; set; }
        public string AvatarThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime RecordDate { get; set; }
    }

    /// <summary>
    /// Sponsor summary information
    /// </summary>
    public class SponsorSummaryDto
    {
        public int SponsorId { get; set; }
        public string SponsorName { get; set; }
        public string SponsorEmail { get; set; }
        public string SponsorPhone { get; set; }
    }

    /// <summary>
    /// Analysis statistics for a subscription
    /// </summary>
    public class AnalysisStatsDto
    {
        /// <summary>
        /// Total analysis count for this user (all time)
        /// </summary>
        public int TotalAnalysisCount { get; set; }

        /// <summary>
        /// Analysis count during this specific subscription period
        /// </summary>
        public int CurrentSubscriptionAnalysisCount { get; set; }

        /// <summary>
        /// Date of the last analysis
        /// </summary>
        public DateTime? LastAnalysisDate { get; set; }

        /// <summary>
        /// Analysis count in last 7 days
        /// </summary>
        public int Last7DaysAnalysisCount { get; set; }

        /// <summary>
        /// Analysis count in last 30 days
        /// </summary>
        public int Last30DaysAnalysisCount { get; set; }

        /// <summary>
        /// Average analyses per day during subscription period
        /// </summary>
        public double AverageAnalysesPerDay { get; set; }
    }

    /// <summary>
    /// Queue information for pending subscriptions
    /// </summary>
    public class QueueInfoDto
    {
        public bool IsQueued { get; set; }
        public DateTime? QueuedDate { get; set; }
        public DateTime? EstimatedActivationDate { get; set; }
        public int? PreviousSponsorshipId { get; set; }
        public string PreviousSponsorshipTierName { get; set; }
    }
}
