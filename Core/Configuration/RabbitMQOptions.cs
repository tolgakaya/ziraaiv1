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
        public string PlantAnalysisRequest { get; set; } = "plant-analysis-requests";
        public string PlantAnalysisResult { get; set; } = "plant-analysis-results";
        public string DealerInvitationRequest { get; set; } = "dealer-invitation-requests";
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