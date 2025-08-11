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
            public const string N8NTimeout = "N8N_TIMEOUT_SECONDS";
        }
    }
}