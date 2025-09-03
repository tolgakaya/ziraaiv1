using Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Queue
{
    public interface IBulkOperationService
    {
        Task<IDataResult<BulkOperationResponse>> ProcessBulkLinkSendAsync(BulkLinkSendRequest request);
        Task<IDataResult<BulkOperationResponse>> ProcessBulkCodeGenerationAsync(BulkCodeGenerationRequest request);
        Task<IDataResult<BulkOperationStatus>> GetBulkOperationStatusAsync(string operationId);
        Task<IDataResult<List<BulkOperationSummary>>> GetBulkOperationHistoryAsync(int sponsorId, int pageSize = 50);
        Task<IResult> CancelBulkOperationAsync(string operationId);
        Task<IResult> RetryFailedBulkItemsAsync(string operationId);
    }

    public class BulkLinkSendRequest
    {
        public string OperationName { get; set; } = "Bulk Link Send";
        public int SponsorId { get; set; }
        public List<BulkLinkRecipient> Recipients { get; set; } = new();
        public string MessageTemplate { get; set; }
        public string Channel { get; set; } = "SMS"; // SMS, WhatsApp, Email
        public BulkSchedulingOptions Scheduling { get; set; } = new();
        public BulkProcessingOptions Processing { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BulkCodeGenerationRequest
    {
        public string OperationName { get; set; } = "Bulk Code Generation";
        public int SponsorId { get; set; }
        public int SubscriptionTierId { get; set; }
        public int Quantity { get; set; }
        public string CodePrefix { get; set; } = "BULK";
        public int ValidityDays { get; set; } = 365;
        public BulkProcessingOptions Processing { get; set; } = new();
        public List<CodeGenerationBatch> Batches { get; set; } = new();
    }

    public class BulkLinkRecipient
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string SponsorshipCode { get; set; }
        public Dictionary<string, string> PersonalizationData { get; set; } = new();
        public int Priority { get; set; } = 5; // 1-10 scale
        public string CustomMessage { get; set; }
    }

    public class CodeGenerationBatch
    {
        public string BatchName { get; set; }
        public int Quantity { get; set; }
        public string CustomPrefix { get; set; }
        public Dictionary<string, object> BatchMetadata { get; set; } = new();
    }

    public class BulkSchedulingOptions
    {
        public bool IsScheduled { get; set; } = false;
        public DateTime? ScheduledDate { get; set; }
        public string TimeZone { get; set; } = "Europe/Istanbul";
        public bool SpreadOverTime { get; set; } = false;
        public int SpreadDurationMinutes { get; set; } = 60;
        public List<string> PreferredHours { get; set; } = new(); // "09:00", "14:00"
        public List<string> AvoidWeekdays { get; set; } = new(); // "Saturday", "Sunday"
    }

    public class BulkProcessingOptions
    {
        public int BatchSize { get; set; } = 50; // Process items in batches
        public int MaxConcurrency { get; set; } = 5; // Max parallel processing
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 30;
        public bool StopOnFirstError { get; set; } = false;
        public bool SendProgressNotifications { get; set; } = true;
        public string CallbackUrl { get; set; }
        public Dictionary<string, object> ProcessingSettings { get; set; } = new();
    }

    public class BulkOperationResponse
    {
        public string OperationId { get; set; }
        public string OperationType { get; set; }
        public BulkOperationStatus Status { get; set; } = BulkOperationStatus.Queued;
        public int TotalItems { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string StatusUrl { get; set; }
        public BulkProcessingMetrics Metrics { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
    }

    public enum BulkOperationStatus
    {
        Queued,
        Validating,
        Processing,
        Paused,
        Completed,
        Failed,
        Cancelled,
        PartiallyCompleted
    }

    public class BulkProcessingMetrics
    {
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public int SkippedItems { get; set; }
        public double ProgressPercentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
        public double SuccessRate => ProcessedItems > 0 ? (double)SuccessfulItems / ProcessedItems * 100 : 0;
        public TimeSpan? AverageProcessingTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public List<BulkError> Errors { get; set; } = new();
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }

    public class BulkError
    {
        public int ItemIndex { get; set; }
        public string ItemId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCategory { get; set; } // Validation, Processing, Network, etc.
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsRetryable { get; set; }
        public int RetryCount { get; set; }
        public Dictionary<string, object> ErrorContext { get; set; } = new();
    }

    public class BulkOperationSummary
    {
        public string OperationId { get; set; }
        public string OperationName { get; set; }
        public string OperationType { get; set; }
        public BulkOperationStatus Status { get; set; }
        public int TotalItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public string CreatedBy { get; set; }
        public Dictionary<string, object> Summary { get; set; } = new();
    }

    public class BulkItemResult
    {
        public int Index { get; set; }
        public string ItemId { get; set; }
        public BulkItemStatus Status { get; set; }
        public string ResultData { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
        public TimeSpan ProcessingDuration { get; set; }
        public Dictionary<string, object> ResultMetadata { get; set; } = new();
    }

    public enum BulkItemStatus
    {
        Pending,
        Processing,
        Success,
        Failed,
        Skipped,
        Retrying
    }

    public class BulkOperationProgress
    {
        public string OperationId { get; set; }
        public BulkOperationStatus Status { get; set; }
        public BulkProcessingMetrics Metrics { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string CurrentPhase { get; set; } // "Validation", "Processing", "Cleanup"
        public string StatusMessage { get; set; }
        public List<BulkItemResult> RecentResults { get; set; } = new();
    }
}