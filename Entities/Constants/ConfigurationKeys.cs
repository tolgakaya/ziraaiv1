namespace Entities.Constants
{
    public static class ConfigurationKeys
    {
        // Image Processing Configuration Keys
        public static class ImageProcessing
        {
            public const string MaxImageSizeMB = "IMAGE_MAX_SIZE_MB";
            public const string MinImageSizeBytes = "IMAGE_MIN_SIZE_BYTES";
            public const string MaxImageWidth = "IMAGE_MAX_WIDTH";
            public const string MaxImageHeight = "IMAGE_MAX_HEIGHT";
            public const string MinImageWidth = "IMAGE_MIN_WIDTH";
            public const string MinImageHeight = "IMAGE_MIN_HEIGHT";
            public const string EnableAutoResize = "IMAGE_ENABLE_AUTO_RESIZE";
            public const string ResizeQuality = "IMAGE_RESIZE_QUALITY";
            public const string SupportedFormats = "IMAGE_SUPPORTED_FORMATS";
            public const string StoragePath = "IMAGE_STORAGE_PATH";
        }
        
        // Application Configuration Keys
        public static class Application
        {
            public const string N8NWebhookUrl = "N8N_WEBHOOK_URL";
            public const string N8NAsyncWebhookUrl = "N8N_ASYNC_WEBHOOK_URL";
            public const string N8NTimeout = "N8N_TIMEOUT_SECONDS";
        }
        
        // Messaging Configuration Keys
        public static class Messaging
        {
            public const string DailyMessageLimitPerFarmer = "MESSAGING_DAILY_LIMIT_PER_FARMER";
            public const string EnableRateLimit = "MESSAGING_ENABLE_RATE_LIMIT";

            // Message Attachment Image Processing
            public const string EnableAttachmentImageResize = "MESSAGING_ATTACHMENT_IMAGE_ENABLE_RESIZE";
            public const string AttachmentImageMaxSizeMB = "MESSAGING_ATTACHMENT_IMAGE_MAX_SIZE_MB";
            public const string AttachmentImageMaxWidth = "MESSAGING_ATTACHMENT_IMAGE_MAX_WIDTH";
            public const string AttachmentImageMaxHeight = "MESSAGING_ATTACHMENT_IMAGE_MAX_HEIGHT";
        }
        
        // RabbitMQ Configuration Keys
        public static class RabbitMQ
        {
            public const string ConnectionString = "RABBITMQ_CONNECTION_STRING";
            public const string PlantAnalysisRequestQueue = "RABBITMQ_PLANT_ANALYSIS_REQUEST_QUEUE";
            public const string PlantAnalysisResultQueue = "RABBITMQ_PLANT_ANALYSIS_RESULT_QUEUE";
            public const string NotificationQueue = "RABBITMQ_NOTIFICATION_QUEUE";
            public const string Username = "RABBITMQ_USERNAME";
            public const string Password = "RABBITMQ_PASSWORD";
            public const string VirtualHost = "RABBITMQ_VIRTUAL_HOST";
            public const string Port = "RABBITMQ_PORT";
        }

        // Cache Configuration Keys
        public static class Cache
        {
            /// <summary>
            /// Dashboard cache TTL in minutes (dealer/sponsor dashboards)
            /// Default: 15 minutes
            /// </summary>
            public const string DashboardCacheDuration = "CACHE_DASHBOARD_DURATION_MINUTES";

            /// <summary>
            /// Statistics cache TTL in minutes (admin analytics)
            /// Default: 60 minutes
            /// </summary>
            public const string StatisticsCacheDuration = "CACHE_STATISTICS_DURATION_MINUTES";

            /// <summary>
            /// Reference data cache TTL in minutes (tiers, configs)
            /// Default: 1440 minutes (24 hours)
            /// </summary>
            public const string ReferenceDataCacheDuration = "CACHE_REFERENCE_DATA_DURATION_MINUTES";

            /// <summary>
            /// Analytics cache TTL in minutes (sponsor analytics)
            /// Default: 15 minutes
            /// </summary>
            public const string AnalyticsCacheDuration = "CACHE_ANALYTICS_DURATION_MINUTES";
        }
    }
}