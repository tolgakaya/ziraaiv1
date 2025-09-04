using Core.Entities;
using System;

namespace Entities.Dtos
{
    public class SmartLinkDto : IDto
    {
        public int Id { get; set; }
        public int SponsorId { get; set; }
        public string SponsorName { get; set; }
        public string LinkUrl { get; set; }
        public string LinkText { get; set; }
        public string LinkDescription { get; set; }
        public string LinkType { get; set; }
        public string[] Keywords { get; set; }
        public string ProductCategory { get; set; }
        public string[] TargetCropTypes { get; set; }
        public string[] TargetDiseases { get; set; }
        public int Priority { get; set; }
        public string DisplayPosition { get; set; }
        public string DisplayStyle { get; set; }
        public string ProductName { get; set; }
        public decimal? ProductPrice { get; set; }
        public string ProductCurrency { get; set; }
        public bool IsPromotional { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int ClickCount { get; set; }
        public decimal ClickThroughRate { get; set; }
        public decimal? RelevanceScore { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class CreateSmartLinkDto : IDto
    {
        public string LinkUrl { get; set; }
        public string LinkText { get; set; }
        public string LinkDescription { get; set; }
        public string LinkType { get; set; } = "Product";
        public string[] Keywords { get; set; }
        public string ProductCategory { get; set; }
        public string[] TargetCropTypes { get; set; }
        public string[] TargetDiseases { get; set; }
        public string[] TargetPests { get; set; }
        public int Priority { get; set; } = 50;
        public string DisplayPosition { get; set; } = "Inline";
        public string DisplayStyle { get; set; } = "Button";
        public string ProductName { get; set; }
        public decimal? ProductPrice { get; set; }
        public string ProductCurrency { get; set; } = "TRY";
        public bool IsPromotional { get; set; }
        public decimal? DiscountPercentage { get; set; }
    }

    public class SmartLinkPerformanceDto : IDto
    {
        public int Id { get; set; }
        public string LinkText { get; set; }
        public string ProductName { get; set; }
        public int ClickCount { get; set; }
        public int DisplayCount { get; set; }
        public decimal ClickThroughRate { get; set; }
        public int ConversionCount { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal? TotalRevenue { get; set; }
        public DateTime LastClickDate { get; set; }
    }
}