using Core.Entities;
using System;

namespace Entities.Concrete
{
    public class SubscriptionUsageLog : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int UserSubscriptionId { get; set; }
        public int? PlantAnalysisId { get; set; } // Reference to the analysis performed
        
        // Usage Information
        public string UsageType { get; set; } // PlantAnalysis, APICall, etc.
        public DateTime UsageDate { get; set; }
        public string RequestEndpoint { get; set; } // Which endpoint was called
        public string RequestMethod { get; set; } // GET, POST, etc.
        
        // Response Information
        public bool IsSuccessful { get; set; }
        public string ResponseStatus { get; set; } // Success, RateLimited, Error
        public string ErrorMessage { get; set; }
        
        // Quota Information at time of request
        public int DailyQuotaUsed { get; set; }
        public int DailyQuotaLimit { get; set; }
        public int MonthlyQuotaUsed { get; set; }
        public int MonthlyQuotaLimit { get; set; }
        
        // Additional Metadata
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestData { get; set; } // JSON of request parameters
        public int? ResponseTimeMs { get; set; } // Response time in milliseconds
        
        // Audit fields
        public DateTime CreatedDate { get; set; }
    }
}