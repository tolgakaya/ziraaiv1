using Business.Services.Configuration;
using Entities.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
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

            var maxWidth = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MaxImageWidth, 1920);
                
            var maxHeight = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.MaxImageHeight, 1080);
                
            var quality = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.ImageProcessing.ResizeQuality, 85);

            var (currentWidth, currentHeight) = await GetImageDimensionsAsync(imageBytes);
            
            // Check if resize is needed
            if (currentWidth <= maxWidth && currentHeight <= maxHeight)
                return imageBytes;

            return await ResizeImageAsync(imageBytes, maxWidth, maxHeight, quality);
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