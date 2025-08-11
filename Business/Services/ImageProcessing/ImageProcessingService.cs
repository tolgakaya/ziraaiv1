using Business.Services.Configuration;
using Entities.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Business.Services.ImageProcessing
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly IConfigurationService _configurationService;

        public ImageProcessingService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public async Task<byte[]> ResizeImageAsync(byte[] imageBytes, int maxWidth, int maxHeight, int quality = 85)
        {
            try
            {
                using var image = Image.Load(imageBytes);
                
                // Calculate resize dimensions maintaining aspect ratio
                var (newWidth, newHeight) = CalculateResizeDimensions(image.Width, image.Height, maxWidth, maxHeight);
                
                // Only resize if needed
                if (image.Width > newWidth || image.Height > newHeight)
                {
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                using var output = new MemoryStream();
                
                // Determine output format and encoder
                IImageFormat format = image.Metadata.DecodedImageFormat ?? JpegFormat.Instance;
                IImageEncoder encoder;
                
                if (format == PngFormat.Instance)
                {
                    encoder = new PngEncoder();
                }
                else
                {
                    encoder = new JpegEncoder 
                    { 
                        Quality = quality 
                    };
                }

                await image.SaveAsync(output, encoder);
                return output.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to resize image: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> ResizeImageIfNeededAsync(byte[] imageBytes)
        {
            var enableAutoResize = await _configurationService.GetBoolValueAsync(
                ConfigurationKeys.ImageProcessing.EnableAutoResize, true);

            if (!enableAutoResize)
                return imageBytes;

            var maxSizeMB = await _configurationService.GetDecimalValueAsync(
                ConfigurationKeys.ImageProcessing.MaxImageSizeMB, 50.0m);
                
            var maxWidth = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MaxImageWidth, 1920);
                
            var maxHeight = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MaxImageHeight, 1080);

            // Use the new target size method for better file size control
            return await ResizeToTargetSizeAsync(imageBytes, (double)maxSizeMB, maxWidth, maxHeight);
        }

        public async Task<(int width, int height)> GetImageDimensionsAsync(byte[] imageBytes)
        {
            try
            {
                using var image = Image.Load(imageBytes);
                return (image.Width, image.Height);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get image dimensions: {ex.Message}", ex);
            }
        }

        public async Task<bool> IsImageWithinLimitsAsync(byte[] imageBytes)
        {
            var (width, height) = await GetImageDimensionsAsync(imageBytes);
            
            var minWidth = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MinImageWidth, 100);
                
            var minHeight = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MinImageHeight, 100);
                
            var maxWidth = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MaxImageWidth, 4000);
                
            var maxHeight = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MaxImageHeight, 4000);

            return width >= minWidth && height >= minHeight && 
                   width <= maxWidth && height <= maxHeight;
        }

        public async Task<string> GetImageFormatAsync(byte[] imageBytes)
        {
            try
            {
                using var stream = new MemoryStream(imageBytes);
                var format = await Image.DetectFormatAsync(stream);
                return format?.Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public async Task<byte[]> ResizeToTargetSizeAsync(byte[] imageBytes, double targetSizeMB, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                var targetSizeBytes = (long)(targetSizeMB * 1024 * 1024);
                
                // If already under target size, return as is
                if (imageBytes.Length <= targetSizeBytes)
                    return imageBytes;

                using var image = Image.Load(imageBytes);
                var originalFormat = image.Metadata.DecodedImageFormat;
                
                // Convert PNG to JPEG for better compression
                var useJpeg = originalFormat == PngFormat.Instance || originalFormat.Name == "PNG";
                
                // Start with reasonable quality and dimensions
                var currentQuality = 85;
                var currentWidth = Math.Min(image.Width, maxWidth);
                var currentHeight = Math.Min(image.Height, maxHeight);
                
                byte[] result = null;
                var attempts = 0;
                var maxAttempts = 10;
                
                while (attempts < maxAttempts)
                {
                    attempts++;
                    
                    // Calculate resize dimensions maintaining aspect ratio
                    var (newWidth, newHeight) = CalculateResizeDimensions(
                        image.Width, image.Height, currentWidth, currentHeight);
                    
                    // Create resized image
                    using var resizedImage = image.CloneAs<Rgba32>();
                    if (image.Width > newWidth || image.Height > newHeight)
                    {
                        resizedImage.Mutate(x => x.Resize(newWidth, newHeight));
                    }
                    
                    using var output = new MemoryStream();
                    
                    // Choose encoder based on format and quality
                    IImageEncoder encoder;
                    if (useJpeg)
                    {
                        encoder = new JpegEncoder { Quality = currentQuality };
                    }
                    else
                    {
                        encoder = new PngEncoder();
                    }
                    
                    await resizedImage.SaveAsync(output, encoder);
                    result = output.ToArray();
                    
                    // Check if we've reached target size
                    if (result.Length <= targetSizeBytes)
                    {
                        return result;
                    }
                    
                    // Adjust parameters for next attempt
                    if (currentQuality > 50)
                    {
                        currentQuality -= 10; // Reduce quality
                    }
                    else if (currentWidth > 800 || currentHeight > 600)
                    {
                        currentWidth = (int)(currentWidth * 0.8); // Reduce dimensions
                        currentHeight = (int)(currentHeight * 0.8);
                        currentQuality = 70; // Reset quality
                    }
                    else
                    {
                        // Force JPEG if not already
                        if (!useJpeg)
                        {
                            useJpeg = true;
                            currentQuality = 60;
                        }
                        else
                        {
                            currentQuality = Math.Max(30, currentQuality - 5);
                        }
                    }
                }
                
                // If we couldn't reach target size, return the best attempt
                return result ?? imageBytes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to resize image to target size: {ex.Message}", ex);
            }
        }

        private static (int width, int height) CalculateResizeDimensions(
            int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / originalWidth;
            var ratioY = (double)maxHeight / originalHeight;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(originalWidth * ratio);
            var newHeight = (int)(originalHeight * ratio);

            return (newWidth, newHeight);
        }
    }
}