using Core.Entities;
using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for dealer performance analytics
    /// Shows statistics for codes distributed by a specific dealer
    /// Used in GET /api/Sponsorship/dealer/analytics
    /// </summary>
    public class DealerPerformanceDto : IDto
    {
        /// <summary>
        /// Dealer ID
        /// </summary>
        public int DealerId { get; set; }

        /// <summary>
        /// Dealer name
        /// </summary>
        public string DealerName { get; set; }

        /// <summary>
        /// Dealer email
        /// </summary>
        public string DealerEmail { get; set; }

        /// <summary>
        /// Total codes transferred to this dealer
        /// </summary>
        public int TotalCodesReceived { get; set; }

        /// <summary>
        /// Codes sent to farmers by dealer
        /// </summary>
        public int CodesSent { get; set; }

        /// <summary>
        /// Codes used by farmers (redeemed)
        /// </summary>
        public int CodesUsed { get; set; }

        /// <summary>
        /// Unused codes still with dealer
        /// </summary>
        public int CodesAvailable { get; set; }

        /// <summary>
        /// Codes reclaimed by main sponsor
        /// </summary>
        public int CodesReclaimed { get; set; }

        /// <summary>
        /// Usage percentage (CodesUsed / CodesSent * 100)
        /// </summary>
        public decimal UsageRate { get; set; }

        /// <summary>
        /// Number of unique farmers who used codes from this dealer
        /// </summary>
        public int UniqueFarmersReached { get; set; }

        /// <summary>
        /// Total plant analyses from this dealer's codes
        /// </summary>
        public int TotalAnalyses { get; set; }

        /// <summary>
        /// Date when first code was transferred to this dealer
        /// </summary>
        public DateTime? FirstTransferDate { get; set; }

        /// <summary>
        /// Date of most recent code transfer
        /// </summary>
        public DateTime? LastTransferDate { get; set; }
    }

    /// <summary>
    /// Summary DTO for all dealers under a sponsor
    /// Used in GET /api/Sponsorship/dealer/summary
    /// </summary>
    public class DealerSummaryDto : IDto
    {
        /// <summary>
        /// Total number of dealers
        /// </summary>
        public int TotalDealers { get; set; }

        /// <summary>
        /// Total codes distributed to all dealers
        /// </summary>
        public int TotalCodesDistributed { get; set; }

        /// <summary>
        /// Total codes used by farmers via dealers
        /// </summary>
        public int TotalCodesUsed { get; set; }

        /// <summary>
        /// Total codes still available with dealers
        /// </summary>
        public int TotalCodesAvailable { get; set; }

        /// <summary>
        /// Total codes reclaimed from dealers
        /// </summary>
        public int TotalCodesReclaimed { get; set; }

        /// <summary>
        /// Overall usage rate across all dealers
        /// </summary>
        public decimal OverallUsageRate { get; set; }

        /// <summary>
        /// List of individual dealer performances
        /// </summary>
        public List<DealerPerformanceDto> Dealers { get; set; }
    }

    /// <summary>
    /// DTO for reclaiming codes from dealer
    /// Used in POST /api/Sponsorship/dealer/reclaim-codes
    /// </summary>
    public class ReclaimCodesDto : IDto
    {
        /// <summary>
        /// Dealer ID to reclaim codes from
        /// </summary>
        public int DealerId { get; set; }

        /// <summary>
        /// Specific code IDs to reclaim (optional - if empty, reclaim all unused)
        /// </summary>
        public List<int> CodeIds { get; set; }

        /// <summary>
        /// Reason for reclaiming codes
        /// </summary>
        public string ReclaimReason { get; set; }
    }

    /// <summary>
    /// Response DTO for code reclaim operation
    /// </summary>
    public class ReclaimCodesResponseDto : IDto
    {
        /// <summary>
        /// Number of codes successfully reclaimed
        /// </summary>
        public int ReclaimedCount { get; set; }

        /// <summary>
        /// List of reclaimed code IDs
        /// </summary>
        public List<int> ReclaimedCodeIds { get; set; }

        /// <summary>
        /// Dealer ID codes were reclaimed from
        /// </summary>
        public int DealerId { get; set; }

        /// <summary>
        /// Timestamp of reclaim operation
        /// </summary>
        public DateTime ReclaimedAt { get; set; }
    }

    /// <summary>
    /// Dashboard summary DTO for dealer view
    /// Quick statistics about transferred codes
    /// Used in GET /api/Sponsorship/dealer/my-dashboard
    /// </summary>
    public class DealerDashboardSummaryDto : IDto
    {
        /// <summary>
        /// Total codes transferred to this dealer
        /// </summary>
        public int TotalCodesReceived { get; set; }

        /// <summary>
        /// Codes sent to farmers by dealer
        /// </summary>
        public int CodesSent { get; set; }

        /// <summary>
        /// Codes used by farmers (redeemed)
        /// </summary>
        public int CodesUsed { get; set; }

        /// <summary>
        /// Available codes (not sent to farmers yet, active, not expired)
        /// </summary>
        public int CodesAvailable { get; set; }

        /// <summary>
        /// Usage rate percentage (CodesUsed / CodesSent * 100)
        /// </summary>
        public decimal UsageRate { get; set; }

        /// <summary>
        /// Number of pending invitations
        /// </summary>
        public int PendingInvitationsCount { get; set; }
    }
}
