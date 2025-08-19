using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class FraudAssessment
    {
        public string RequestId { get; set; }
        public double RiskScore { get; set; }
        public string RiskLevel { get; set; } // "Low", "Medium", "High", "Critical"
        public List<string> RiskFactors { get; set; }
        public List<string> Recommendations { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime AssessedAt { get; set; }
    }

    public class FraudAssessmentRequest
    {
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public DateTime RequestTime { get; set; }
        public string SessionId { get; set; }
        public string DeviceFingerprint { get; set; }
    }

    public class SuspiciousActivityReport
    {
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public Dictionary<string, object> Evidence { get; set; }
        public string Severity { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    public class RateLimitStatus
    {
        public string Identifier { get; set; }
        public string Action { get; set; }
        public int CurrentAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public bool IsLimited { get; set; }
        public TimeSpan ResetIn { get; set; }
    }

    public class BlockedEntity
    {
        public int Id { get; set; }
        public string EntityType { get; set; } // "IP", "User", "Device", "Phone"
        public string EntityValue { get; set; }
        public string Reason { get; set; }
        public string Severity { get; set; }
        public DateTime BlockedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string BlockedBy { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class SecurityEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public Dictionary<string, object> EventData { get; set; }
        public DateTime OccurredAt { get; set; }
        public bool IsProcessed { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class ThreatIntelligence
    {
        public string ThreatType { get; set; }
        public string Source { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, object> Indicators { get; set; }
        public List<string> MitigationStrategies { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class SecurityAlertRule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public string Severity { get; set; }
        public bool IsEnabled { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public List<string> Actions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggered { get; set; }
    }

    public class SecurityDashboard
    {
        public int TotalEvents { get; set; }
        public int CriticalEvents { get; set; }
        public int BlockedAttempts { get; set; }
        public int ActiveBlocks { get; set; }
        public Dictionary<string, int> EventsByType { get; set; }
        public Dictionary<string, int> EventsBySeverity { get; set; }
        public List<SecurityEvent> RecentEvents { get; set; }
        public List<BlockedEntity> RecentBlocks { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SecurityReport
    {
        public string ReportType { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public SecurityDashboard Summary { get; set; }
        public List<ThreatIntelligence> ThreatsDetected { get; set; }
        public List<string> Recommendations { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}