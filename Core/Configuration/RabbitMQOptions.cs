using System.ComponentModel.DataAnnotations;

namespace Core.Configuration
{
    public class RabbitMQOptions
    {
        public const string SectionName = "RabbitMQ";
        
        [Required]
        public string ConnectionString { get; set; } = "amqp://dev:devpass@localhost:5672/";
        
        [Required]
        public QueueOptions Queues { get; set; } = new();
        
        public RetrySettings RetrySettings { get; set; } = new();
        
        public ConnectionSettings ConnectionSettings { get; set; } = new();
    }
    
    public class QueueOptions
    {
        // Legacy queues (OLD system - direct to worker)
        public string PlantAnalysisRequest { get; set; } = "plant-analysis-requests";
        public string PlantAnalysisResult { get; set; } = "plant-analysis-results";
        public string PlantAnalysisMultiImageRequest { get; set; } = "plant-analysis-multi-image-requests";
        public string PlantAnalysisMultiImageResult { get; set; } = "plant-analysis-multi-image-results";

        // NEW: Raw analysis queue (routed by Dispatcher)
        public string RawAnalysisRequest { get; set; } = "raw-analysis-queue";

        // Other queues
        public string DealerInvitationRequest { get; set; } = "dealer-invitation-requests";
        public string FarmerInvitationRequest { get; set; } = "farmer-invitation-requests";
        public string FarmerCodeDistributionRequest { get; set; } = "farmer-code-distribution-requests";
        public string FarmerSubscriptionAssignmentRequest { get; set; } = "farmer-subscription-assignment-requests";
        public string Notification { get; set; } = "notifications";
    }
    
    public class RetrySettings
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;
    }
    
    public class ConnectionSettings
    {
        public int RequestedHeartbeat { get; set; } = 60;
        public int NetworkRecoveryInterval { get; set; } = 10;
    }
}