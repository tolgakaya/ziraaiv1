using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Security
{
    public interface ISecurityService
    {
        Task<IResult> ValidateRateLimitAsync(string identifier, string action, int maxAttempts = 10, int windowMinutes = 60);
        Task<IDataResult<FraudAssessment>> AssessFraudRiskAsync(FraudAssessmentRequest request);
        Task<IResult> ReportSuspiciousActivityAsync(SuspiciousActivityReport report);
        Task<IDataResult<SecurityInsights>> GetSecurityInsightsAsync(int sponsorId);
        Task<IResult> BlockIpAddressAsync(string ipAddress, string reason, int durationHours = 24);
        Task<IResult> UnblockIpAddressAsync(string ipAddress);
        Task<IDataResult<List<BlockedEntity>>> GetBlockedEntitiesAsync();
    }

    public class FraudAssessmentRequest
    {
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Action { get; set; } // "redemption", "registration", "bulk_send"
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SessionId { get; set; }
        public string ReferrerUrl { get; set; }
        public List<string> RecentActions { get; set; } = new();
    }

    public class FraudAssessment
    {
        public string RequestId { get; set; }
        public FraudRiskLevel RiskLevel { get; set; }
        public double RiskScore { get; set; } // 0-100
        public List<FraudIndicator> Indicators { get; set; }
        public FraudDecision Decision { get; set; }
        public List<string> RecommendedActions { get; set; }
        public DateTime AssessedAt { get; set; }
        public string ReasonCode { get; set; }
        public Dictionary<string, object> RiskFactors { get; set; }
    }

    public enum FraudRiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum FraudDecision
    {
        Allow,
        Challenge, // Require additional verification
        Block,
        Review // Manual review required
    }

    public class FraudIndicator
    {
        public string Type { get; set; } // "ip_reputation", "velocity", "pattern_anomaly"
        public string Description { get; set; }
        public double Impact { get; set; } // 0-100
        public string Severity { get; set; } // "low", "medium", "high", "critical"
        public Dictionary<string, object> Details { get; set; }
    }

    public class SuspiciousActivityReport
    {
        public string ActivityType { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Description { get; set; }
        public string Evidence { get; set; } // JSON or text evidence
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public string ReportedBy { get; set; } // System or user ID
        public string Severity { get; set; } // "low", "medium", "high", "critical"
        public Dictionary<string, object> Context { get; set; }
    }

    public class SecurityInsights
    {
        public int TotalSecurityEvents { get; set; }
        public int BlockedAttempts { get; set; }
        public int SuspiciousActivities { get; set; }
        public double AverageFraudScore { get; set; }
        public List<TopThreat> TopThreats { get; set; }
        public List<SecurityTrend> TrendData { get; set; }
        public List<GeoSecurityData> GeographicRisks { get; set; }
        public RateLimitingStats RateLimitingStats { get; set; }
        public List<SecurityRecommendation> Recommendations { get; set; }
    }

    public class TopThreat
    {
        public string ThreatType { get; set; }
        public int Count { get; set; }
        public string Description { get; set; }
        public string LastSeen { get; set; }
        public string MitigationStatus { get; set; }
    }

    public class SecurityTrend
    {
        public DateTime Date { get; set; }
        public int SecurityEvents { get; set; }
        public int BlockedAttempts { get; set; }
        public double AverageRiskScore { get; set; }
    }

    public class GeoSecurityData
    {
        public string Country { get; set; }
        public string City { get; set; }
        public int ThreatCount { get; set; }
        public double RiskScore { get; set; }
        public bool IsBlocked { get; set; }
    }

    public class RateLimitingStats
    {
        public int TotalRequests { get; set; }
        public int LimitedRequests { get; set; }
        public double LimitingRate { get; set; }
        public Dictionary<string, int> ActionBreakdown { get; set; }
        public List<RateLimitEvent> RecentEvents { get; set; }
    }

    public class RateLimitEvent
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string IpAddress { get; set; }
        public int AttemptCount { get; set; }
        public bool WasBlocked { get; set; }
    }

    public class SecurityRecommendation
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; } // "low", "medium", "high", "critical"
        public List<string> Actions { get; set; }
        public string Impact { get; set; }
    }

    public class BlockedEntity
    {
        public string Type { get; set; } // "ip", "email", "phone"
        public string Value { get; set; }
        public string Reason { get; set; }
        public DateTime BlockedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string BlockedBy { get; set; }
        public bool IsActive { get; set; }
        public int ViolationCount { get; set; }
    }

    public class RateLimitConfig
    {
        public string Action { get; set; }
        public int MaxAttempts { get; set; }
        public int WindowMinutes { get; set; }
        public int BlockDurationMinutes { get; set; }
        public bool IsEnabled { get; set; }
        public string Description { get; set; }
    }
}