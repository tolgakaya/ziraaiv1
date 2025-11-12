using System;

namespace Entities.Dtos
{
    /// <summary>
    /// High-level summary of farmer's journey
    /// Provides key metrics and lifecycle stage at a glance
    /// </summary>
    public class JourneySummaryDto
    {
        /// <summary>
        /// Date when farmer first redeemed a sponsorship code
        /// </summary>
        public DateTime? FirstCodeRedemption { get; set; }

        /// <summary>
        /// Total number of days since first code redemption
        /// </summary>
        public int TotalDaysAsCustomer { get; set; }

        /// <summary>
        /// Total number of plant analyses performed
        /// </summary>
        public int TotalAnalyses { get; set; }

        /// <summary>
        /// Total amount farmer has spent (for paid subscriptions)
        /// Currently 0 for sponsored farmers
        /// </summary>
        public decimal TotalSpent { get; set; }

        /// <summary>
        /// Estimated value generated for sponsor (based on engagement and analyses)
        /// Calculated as: TotalAnalyses * AvgAnalysisValue
        /// </summary>
        public decimal TotalValueGenerated { get; set; }

        /// <summary>
        /// Current subscription tier (S, M, L, XL)
        /// </summary>
        public string CurrentTier { get; set; }

        /// <summary>
        /// Current lifecycle stage: Active, At-Risk, Dormant, Churned
        /// </summary>
        public string LifecycleStage { get; set; }

        /// <summary>
        /// Next subscription renewal date (if applicable)
        /// </summary>
        public DateTime? NextRenewalDate { get; set; }

        /// <summary>
        /// Days until renewal (if applicable)
        /// Null if no active subscription or already expired
        /// </summary>
        public int? DaysUntilRenewal { get; set; }
    }
}
