using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class BulkLinkSendRequest
    {
        public string RequestId { get; set; }
        public List<LinkRecipient> Recipients { get; set; }
        public string MessageTemplate { get; set; }
        public string DeepLinkType { get; set; }
        public Dictionary<string, object> LinkParameters { get; set; }
        public string SendMethod { get; set; } // "SMS", "WhatsApp", "Email"
        public DateTime? ScheduledTime { get; set; }
        public int Priority { get; set; } = 5;
        public bool GenerateQrCodes { get; set; }
        public string SponsorId { get; set; }
    }

    public class LinkRecipient
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public Dictionary<string, object> PersonalizationData { get; set; }
        public Dictionary<string, string> CustomLinkParameters { get; set; }
    }

    public class BulkCodeGenerationRequest
    {
        public string RequestId { get; set; }
        public int SponsorId { get; set; }
        public int SubscriptionTierId { get; set; }
        public int Quantity { get; set; }
        public int ValidityDays { get; set; }
        public string CodePrefix { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public bool AutoDistribute { get; set; }
        public string DistributionMethod { get; set; } // "SMS", "WhatsApp", "Email", "Manual"
        public List<string> Recipients { get; set; }
    }

    public class BulkOperationResponse
    {
        public string OperationId { get; set; }
        public string Status { get; set; } // "Queued", "Processing", "Completed", "Failed", "Partial"
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public List<string> Results { get; set; }
        public List<string> Errors { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class BulkOperationStatus
    {
        public string OperationId { get; set; }
        public string Status { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string CurrentPhase { get; set; }
        public int ItemsProcessed { get; set; }
        public int TotalItems { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public List<string> RecentMessages { get; set; }
        public Dictionary<string, object> Statistics { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class QueueMetrics
    {
        public string QueueName { get; set; }
        public int PendingJobs { get; set; }
        public int ProcessingJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
        public double AverageProcessingTime { get; set; }
        public DateTime LastJobProcessed { get; set; }
        public List<QueueWorker> ActiveWorkers { get; set; }
        public Dictionary<string, int> JobTypeBreakdown { get; set; }
    }

    public class QueueWorker
    {
        public string WorkerId { get; set; }
        public string Status { get; set; } // "Active", "Idle", "Offline"
        public DateTime LastActivity { get; set; }
        public int JobsProcessed { get; set; }
        public string CurrentJob { get; set; }
        public DateTime? CurrentJobStarted { get; set; }
    }

    public class BulkOperationRequest
    {
        public string RequestId { get; set; }
        public string OperationType { get; set; } // "LinkSend", "CodeGeneration", "DataExport"
        public Dictionary<string, object> Parameters { get; set; }
        public int Priority { get; set; } = 5;
        public DateTime? ScheduledTime { get; set; }
        public string CallbackUrl { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    }

    public class BulkOperationProgress
    {
        public string OperationId { get; set; }
        public string Phase { get; set; }
        public int ItemIndex { get; set; }
        public string ItemId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BatchProcessingOptions
    {
        public int BatchSize { get; set; } = 100;
        public TimeSpan BatchDelay { get; set; } = TimeSpan.FromSeconds(1);
        public int MaxConcurrency { get; set; } = 5;
        public bool FailFast { get; set; } = false;
        public bool ContinueOnError { get; set; } = true;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
    }

    public class QueueConfiguration
    {
        public string QueueName { get; set; }
        public int MaxWorkers { get; set; }
        public int MaxRetries { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public TimeSpan VisibilityTimeout { get; set; }
        public TimeSpan MessageRetention { get; set; }
        public bool DeadLetterQueue { get; set; }
        public Dictionary<string, object> CustomSettings { get; set; }
    }
}