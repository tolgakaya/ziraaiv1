using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// ROI (Return on Investment) analytics for sponsors
    /// Includes cost analysis, value calculations, ROI metrics, and efficiency statistics
    /// </summary>
    public class SponsorROIAnalyticsDto
    {
        // Cost Breakdown
        public decimal TotalInvestment { get; set; }
        public decimal CostPerCode { get; set; }
        public decimal CostPerRedemption { get; set; }
        public decimal CostPerAnalysis { get; set; }
        public decimal CostPerFarmer { get; set; }

        // Value Analysis
        public decimal TotalAnalysesValue { get; set; }
        public decimal AverageLifetimeValuePerFarmer { get; set; }
        public decimal AverageValuePerCode { get; set; }

        // ROI Metrics
        public decimal OverallROI { get; set; }
        public string ROIStatus { get; set; } // Positive, Negative, Breakeven
        public List<TierROI> ROIByTier { get; set; }

        // Efficiency Metrics
        public decimal UtilizationRate { get; set; }
        public decimal WasteRate { get; set; }
        public int BreakevenAnalysisCount { get; set; }
        public int AnalysesUntilBreakeven { get; set; }
        public int? EstimatedPaybackDays { get; set; }

        // Supporting Data
        public int TotalCodesPurchased { get; set; }
        public int TotalCodesRedeemed { get; set; }
        public int TotalAnalysesGenerated { get; set; }
        public int UniqueFarmersReached { get; set; }
        public decimal AnalysisUnitValue { get; set; } // Configuration value used for calculations

        public SponsorROIAnalyticsDto()
        {
            ROIByTier = new List<TierROI>();
        }
    }

    /// <summary>
    /// ROI breakdown per subscription tier
    /// </summary>
    public class TierROI
    {
        public string TierName { get; set; }
        public decimal Investment { get; set; }
        public int CodesPurchased { get; set; }
        public int CodesRedeemed { get; set; }
        public int AnalysesGenerated { get; set; }
        public decimal TotalValue { get; set; }
        public decimal ROI { get; set; }
        public decimal UtilizationRate { get; set; }
    }
}
