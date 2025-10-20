using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Package-level distribution statistics showing purchased vs distributed vs redeemed codes
    /// </summary>
    public class PackageDistributionStatisticsDto
    {
        /// <summary>
        /// Total codes purchased across all packages
        /// </summary>
        public int TotalCodesPurchased { get; set; }

        /// <summary>
        /// Total codes distributed via SMS/WhatsApp/Email
        /// </summary>
        public int TotalCodesDistributed { get; set; }

        /// <summary>
        /// Total codes redeemed by farmers
        /// </summary>
        public int TotalCodesRedeemed { get; set; }

        /// <summary>
        /// Codes purchased but not yet distributed
        /// </summary>
        public int CodesNotDistributed { get; set; }

        /// <summary>
        /// Codes distributed but not yet redeemed
        /// </summary>
        public int CodesDistributedNotRedeemed { get; set; }

        /// <summary>
        /// Distribution rate: (distributed / purchased) * 100
        /// </summary>
        public decimal DistributionRate { get; set; }

        /// <summary>
        /// Redemption rate: (redeemed / distributed) * 100
        /// </summary>
        public decimal RedemptionRate { get; set; }

        /// <summary>
        /// Overall success rate: (redeemed / purchased) * 100
        /// </summary>
        public decimal OverallSuccessRate { get; set; }

        /// <summary>
        /// Breakdown by individual package purchases
        /// </summary>
        public List<PackageBreakdown> PackageBreakdowns { get; set; }

        /// <summary>
        /// Breakdown by subscription tier
        /// </summary>
        public List<TierBreakdown> TierBreakdowns { get; set; }

        /// <summary>
        /// Breakdown by distribution channel
        /// </summary>
        public List<ChannelBreakdown> ChannelBreakdowns { get; set; }
    }

    public class PackageBreakdown
    {
        /// <summary>
        /// Purchase ID
        /// </summary>
        public int PurchaseId { get; set; }

        /// <summary>
        /// Purchase date
        /// </summary>
        public DateTime PurchaseDate { get; set; }

        /// <summary>
        /// Subscription tier name (S, M, L, XL)
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// Total codes in this package
        /// </summary>
        public int CodesPurchased { get; set; }

        /// <summary>
        /// Codes distributed from this package
        /// </summary>
        public int CodesDistributed { get; set; }

        /// <summary>
        /// Codes redeemed from this package
        /// </summary>
        public int CodesRedeemed { get; set; }

        /// <summary>
        /// Codes not yet distributed
        /// </summary>
        public int CodesNotDistributed { get; set; }

        /// <summary>
        /// Codes distributed but not redeemed
        /// </summary>
        public int CodesDistributedNotRedeemed { get; set; }

        /// <summary>
        /// Distribution rate for this package (%)
        /// </summary>
        public decimal DistributionRate { get; set; }

        /// <summary>
        /// Redemption rate for this package (%)
        /// </summary>
        public decimal RedemptionRate { get; set; }

        /// <summary>
        /// Package total amount
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Currency
        /// </summary>
        public string Currency { get; set; }
    }

    public class TierBreakdown
    {
        /// <summary>
        /// Tier name (S, M, L, XL)
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// Tier display name
        /// </summary>
        public string TierDisplayName { get; set; }

        /// <summary>
        /// Total codes purchased for this tier
        /// </summary>
        public int CodesPurchased { get; set; }

        /// <summary>
        /// Codes distributed for this tier
        /// </summary>
        public int CodesDistributed { get; set; }

        /// <summary>
        /// Codes redeemed for this tier
        /// </summary>
        public int CodesRedeemed { get; set; }

        /// <summary>
        /// Distribution rate (%)
        /// </summary>
        public decimal DistributionRate { get; set; }

        /// <summary>
        /// Redemption rate (%)
        /// </summary>
        public decimal RedemptionRate { get; set; }
    }

    public class ChannelBreakdown
    {
        /// <summary>
        /// Distribution channel (SMS, WhatsApp, Email, Manual)
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Total codes distributed via this channel
        /// </summary>
        public int CodesDistributed { get; set; }

        /// <summary>
        /// Codes successfully delivered
        /// </summary>
        public int CodesDelivered { get; set; }

        /// <summary>
        /// Codes redeemed from this channel
        /// </summary>
        public int CodesRedeemed { get; set; }

        /// <summary>
        /// Delivery rate (%)
        /// </summary>
        public decimal DeliveryRate { get; set; }

        /// <summary>
        /// Redemption rate (%)
        /// </summary>
        public decimal RedemptionRate { get; set; }
    }
}
