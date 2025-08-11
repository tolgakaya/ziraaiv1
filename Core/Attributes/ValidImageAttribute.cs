using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Core.Attributes
{
    public class ValidImageAttribute : ValidationAttribute
    {
        private readonly double _maxSizeInMB;
        private readonly string[] _supportedMimeTypes = 
        {
            "data:image/jpeg",
            "data:image/jpg", 
            "data:image/png",
            "data:image/gif",
            "data:image/webp",
            "data:image/bmp",
            "data:image/svg+xml",
            "data:image/svg",
            "data:image/tiff",
            "data:image/tif"
        };

        public ValidImageAttribute(double maxSizeInMB = 50.0)
        {
            _maxSizeInMB = maxSizeInMB;
            ErrorMessage = "Invalid image file. Supported formats: JPEG, PNG, GIF, WebP, BMP, SVG, TIFF";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("Image is required");

            var dataUri = value.ToString();
            
            try
            {
                // Check if it's a valid data URI
                if (!dataUri.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult("Invalid image format. Only image files are allowed.");
                }

                // Check for supported MIME types
                var lowerDataUri = dataUri.ToLowerInvariant();
                bool isSupported = _supportedMimeTypes.Any(mime => lowerDataUri.StartsWith(mime));
                if (!isSupported)
                {
                    return new ValidationResult("Unsupported image format. Supported formats: JPEG, PNG, GIF, WebP, BMP, SVG, TIFF");
                }

                // Validate base64 content
                var base64Data = dataUri.Contains(",") ? dataUri.Split(',')[1] : dataUri;
                var imageBytes = Convert.FromBase64String(base64Data);
                
                if (imageBytes.Length == 0)
                {
                    return new ValidationResult("Image data is empty");
                }

                // Use attribute parameter or defaults (configuration will be checked at service level)
                long maxSizeInBytes = (long)(_maxSizeInMB * 1024 * 1024);
                long minSizeInBytes = 100;
                
                if (imageBytes.Length > maxSizeInBytes)
                {
                    return new ValidationResult($"Image file too large. Maximum size is {_maxSizeInMB}MB");
                }
                
                if (imageBytes.Length < minSizeInBytes)
                {
                    return new ValidationResult($"Image file too small. Minimum size is {minSizeInBytes} bytes");
                }

                return ValidationResult.Success;
            }
            catch (FormatException)
            {
                return new ValidationResult("Invalid base64 image data");
            }
            catch
            {
                return new ValidationResult("Invalid image file");
            }
        }
    }
}