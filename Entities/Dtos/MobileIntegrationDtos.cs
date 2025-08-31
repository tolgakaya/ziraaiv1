using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class ClickTimeline
    {
        public DateTime Date { get; set; }
        public int Clicks { get; set; }
        public int AppOpens { get; set; }
    }

    public class AppConfiguration
    {
        public string Platform { get; set; } // "iOS", "Android"
        public string AppId { get; set; }
        public string AppStoreUrl { get; set; }
        public string CustomScheme { get; set; }
        public string UniversalLinkDomain { get; set; }
        public Dictionary<string, string> PathMappings { get; set; }
        public bool IsActive { get; set; }
    }

    public class LinkValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public bool IsActive { get; set; }
        public string ValidationMessage { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public Dictionary<string, object> LinkData { get; set; }
    }

    public class DeepLinkMetrics
    {
        public int TotalLinks { get; set; }
        public int ActiveLinks { get; set; }
        public int ExpiredLinks { get; set; }
        public int TotalClicks { get; set; }
        public int TotalAppOpens { get; set; }
        public decimal OverallConversionRate { get; set; }
        public Dictionary<string, int> LinksByType { get; set; }
        public Dictionary<string, int> LinksBySource { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}