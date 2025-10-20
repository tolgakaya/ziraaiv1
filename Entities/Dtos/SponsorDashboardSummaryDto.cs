using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Sponsor dashboard summary with key metrics for mobile app home screen
    /// </summary>
    public class SponsorDashboardSummaryDto
    {
        /// <summary>
        /// Total number of codes purchased across all packages
        /// </summary>
        public int TotalCodesCount { get; set; }

        /// <summary>
        /// Number of codes that have been distributed (sent) to farmers
        /// </summary>
        public int SentCodesCount { get; set; }

        /// <summary>
        /// Percentage of codes that have been sent: (sentCodes / totalCodes) * 100
        /// </summary>
        public decimal SentCodesPercentage { get; set; }

        /// <summary>
        /// Total number of plant analyses performed using sponsored subscriptions
        /// </summary>
        public int TotalAnalysesCount { get; set; }

        /// <summary>
        /// Number of bulk subscription purchases made by this sponsor
        /// </summary>
        public int PurchasesCount { get; set; }

        /// <summary>
        /// Total amount spent on sponsorship packages
        /// </summary>
        public decimal TotalSpent { get; set; }

        /// <summary>
        /// Currency of total spent amount
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// List of active subscription packages grouped by tier
        /// </summary>
        public List<ActivePackageSummary> ActivePackages { get; set; }

        /// <summary>
        /// Overall statistics summary
        /// </summary>
        public OverallStatistics OverallStats { get; set; }
    }

    /// <summary>
    /// Summary of codes for a specific subscription tier
    /// </summary>
    public class ActivePackageSummary
    {
        /// <summary>
        /// Subscription tier name (S, M, L, XL)
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// Tier display name (Small, Medium, Large, Extra Large)
        /// </summary>
        public string TierDisplayName { get; set; }

        /// <summary>
        /// Total codes purchased for this tier
        /// </summary>
        public int TotalCodes { get; set; }

        /// <summary>
        /// Codes that have been distributed/sent (DistributionDate != null)
        /// </summary>
        public int SentCodes { get; set; }

        /// <summary>
        /// Codes that haven't been sent yet (DistributionDate == null)
        /// </summary>
        public int UnsentCodes { get; set; }

        /// <summary>
        /// Sent codes that have been redeemed by farmers (IsUsed = true)
        /// </summary>
        public int UsedCodes { get; set; }

        /// <summary>
        /// Sent codes that haven't been redeemed yet (sent but IsUsed = false)
        /// </summary>
        public int UnusedSentCodes { get; set; }

        /// <summary>
        /// Codes remaining (not yet distributed)
        /// Same as UnsentCodes, provided for clarity: totalCodes - sentCodes
        /// </summary>
        public int RemainingCodes { get; set; }

        /// <summary>
        /// Usage percentage of sent codes: (usedCodes / sentCodes) * 100
        /// Shows redemption rate of distributed codes
        /// </summary>
        public decimal UsagePercentage { get; set; }

        /// <summary>
        /// Distribution percentage: (sentCodes / totalCodes) * 100
        /// Shows how many codes have been distributed
        /// </summary>
        public decimal DistributionPercentage { get; set; }

        /// <summary>
        /// Number of unique farmers who redeemed codes from this tier
        /// </summary>
        public int UniqueFarmers { get; set; }

        /// <summary>
        /// Number of plant analyses performed using this tier's codes
        /// </summary>
        public int AnalysesCount { get; set; }
    }

    /// <summary>
    /// Overall sponsorship statistics
    /// </summary>
    public class OverallStatistics
    {
        /// <summary>
        /// Total codes distributed via SMS
        /// </summary>
        public int SmsDistributions { get; set; }

        /// <summary>
        /// Total codes distributed via WhatsApp
        /// </summary>
        public int WhatsAppDistributions { get; set; }

        /// <summary>
        /// Overall redemption rate: (totalUsed / totalSent) * 100
        /// </summary>
        public decimal OverallRedemptionRate { get; set; }

        /// <summary>
        /// Average time (in days) from distribution to redemption
        /// </summary>
        public decimal AverageRedemptionTime { get; set; }

        /// <summary>
        /// Number of unique farmers sponsored across all tiers
        /// </summary>
        public int TotalUniqueFarmers { get; set; }

        /// <summary>
        /// Date of the most recent purchase
        /// </summary>
        public DateTime? LastPurchaseDate { get; set; }

        /// <summary>
        /// Date of the most recent code distribution
        /// </summary>
        public DateTime? LastDistributionDate { get; set; }
    }
}
