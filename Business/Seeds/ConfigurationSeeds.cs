using Entities.Concrete;
using System;
using System.Collections.Generic;

namespace Business.Seeds
{
    public static class ConfigurationSeeds
    {
        public static List<Configuration> GetDefaultConfigurations()
        {
            return new List<Configuration>
            {
                // Image Processing Settings
                new Configuration
                {
                    Id = 1,
                    Key = "IMAGE_MAX_SIZE_MB",
                    Value = "50.0",
                    Description = "Maximum image file size in MB (supports decimal values like 0.5)",
                    Category = "ImageProcessing",
                    ValueType = "decimal",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 2,
                    Key = "IMAGE_MIN_SIZE_BYTES",
                    Value = "100",
                    Description = "Minimum image file size in bytes",
                    Category = "ImageProcessing",
                    ValueType = "int",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 3,
                    Key = "IMAGE_MAX_WIDTH",
                    Value = "1920",
                    Description = "Maximum image width in pixels",
                    Category = "ImageProcessing",
                    ValueType = "int",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 4,
                    Key = "IMAGE_MAX_HEIGHT",
                    Value = "1080",
                    Description = "Maximum image height in pixels",
                    Category = "ImageProcessing",
                    ValueType = "int",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 5,
                    Key = "IMAGE_MIN_WIDTH",
                    Value = "100",
                    Description = "Minimum image width in pixels",
                    Category = "ImageProcessing",
                    ValueType = "int",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 6,
                    Key = "IMAGE_MIN_HEIGHT",
                    Value = "100",
                    Description = "Minimum image height in pixels",
                    Category = "ImageProcessing",
                    ValueType = "int",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 7,
                    Key = "IMAGE_ENABLE_AUTO_RESIZE",
                    Value = "true",
                    Description = "Enable automatic image resizing if dimensions exceed limits",
                    Category = "ImageProcessing",
                    ValueType = "bool",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 8,
                    Key = "IMAGE_RESIZE_QUALITY",
                    Value = "85",
                    Description = "JPEG quality for resized images (1-100)",
                    Category = "ImageProcessing",
                    ValueType = "int",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 9,
                    Key = "IMAGE_SUPPORTED_FORMATS",
                    Value = "JPEG,PNG,GIF,WebP,BMP,SVG,TIFF",
                    Description = "Comma-separated list of supported image formats",
                    Category = "ImageProcessing",
                    ValueType = "string",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 10,
                    Key = "IMAGE_STORAGE_PATH",
                    Value = "wwwroot/uploads/images",
                    Description = "Path where uploaded images are stored",
                    Category = "ImageProcessing",
                    ValueType = "string",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                
                // Application Settings
                new Configuration
                {
                    Id = 11,
                    Key = "N8N_WEBHOOK_URL",
                    Value = "https://your-n8n-instance.com/webhook/plant-analysis",
                    Description = "N8N webhook endpoint for plant analysis",
                    Category = "Application",
                    ValueType = "string",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Configuration
                {
                    Id = 12,
                    Key = "N8N_TIMEOUT_SECONDS",
                    Value = "300",
                    Description = "Timeout for N8N webhook requests in seconds",
                    Category = "Application",
                    ValueType = "int",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            };
        }
    }
}