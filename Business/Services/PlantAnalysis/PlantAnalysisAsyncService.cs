using Business.Services.Configuration;
using Business.Services.ImageProcessing;
using Business.Services.MessageQueue;
using Core.Configuration;
using Entities.Constants;
using Entities.Dtos;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    public class PlantAnalysisAsyncService : IPlantAnalysisAsyncService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IConfigurationService _configurationService;
        private readonly RabbitMQOptions _rabbitMQOptions;

        public PlantAnalysisAsyncService(
            IMessageQueueService messageQueueService,
            IImageProcessingService imageProcessingService,
            IConfigurationService configurationService,
            IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _messageQueueService = messageQueueService;
            _imageProcessingService = imageProcessingService;
            _configurationService = configurationService;
            _rabbitMQOptions = rabbitMQOptions.Value;
        }

        public async Task<string> QueuePlantAnalysisAsync(PlantAnalysisRequestDto request)
        {
            try
            {
                // Generate unique IDs for tracking
                var correlationId = Guid.NewGuid().ToString("N");
                var analysisId = $"async_analysis_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{correlationId[..8]}";

                // Process image intelligently (resize, optimize, etc.)
                var processedImageDataUri = await ProcessImageIntelligentlyAsync(request.Image);

                // Get queue name from appsettings
                var queueName = _rabbitMQOptions.Queues.PlantAnalysisRequest;

                // Create async request payload
                var asyncRequest = new PlantAnalysisAsyncRequestDto
                {
                    Image = processedImageDataUri,
                    FarmerId = request.FarmerId,
                    SponsorId = request.SponsorId,
                    Location = request.Location,
                    GpsCoordinates = request.GpsCoordinates,
                    CropType = request.CropType,
                    FieldId = request.FieldId,
                    UrgencyLevel = request.UrgencyLevel,
                    Notes = request.Notes,
                    ResponseQueue = "plant-analysis-results",
                    CorrelationId = correlationId,
                    AnalysisId = analysisId,
                    
                    // Additional fields
                    Altitude = request.Altitude,
                    PlantingDate = request.PlantingDate,
                    ExpectedHarvestDate = request.ExpectedHarvestDate,
                    LastFertilization = request.LastFertilization,
                    LastIrrigation = request.LastIrrigation,
                    PreviousTreatments = request.PreviousTreatments?.ToArray(),
                    WeatherConditions = request.WeatherConditions,
                    Temperature = request.Temperature,
                    Humidity = request.Humidity,
                    SoilType = request.SoilType,
                    ContactInfo = request.ContactInfo,
                    AdditionalInfo = request.AdditionalInfo
                };

                // Publish to RabbitMQ queue
                var publishResult = await _messageQueueService.PublishAsync(queueName, asyncRequest, correlationId);

                if (!publishResult)
                {
                    throw new InvalidOperationException("Failed to publish message to queue");
                }

                return analysisId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to queue plant analysis: {ex.Message}", ex);
            }
        }

        public async Task<bool> IsQueueHealthyAsync()
        {
            try
            {
                return await _messageQueueService.IsConnectedAsync();
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ProcessImageIntelligentlyAsync(string originalDataUri)
        {
            try
            {
                if (string.IsNullOrEmpty(originalDataUri))
                    throw new ArgumentException("Image data URI is required");

                // Extract image bytes from data URI
                var base64Data = originalDataUri.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                // Get configuration for size management
                var maxSizeMB = await _configurationService.GetDecimalValueAsync(
                    ConfigurationKeys.ImageProcessing.MaxImageSizeMB, 0.25m);

                var enableAutoResize = await _configurationService.GetBoolValueAsync(
                    ConfigurationKeys.ImageProcessing.EnableAutoResize, true);

                var maxSizeBytes = (long)(maxSizeMB * 1024 * 1024);

                // Check if image exceeds size limit
                if (imageBytes.Length > maxSizeBytes && enableAutoResize)
                {
                    // Use intelligent resizing to target size
                    var resizedBytes = await _imageProcessingService.ResizeToTargetSizeAsync(
                        imageBytes, (double)maxSizeMB);

                    // Verify the resize was successful
                    if (resizedBytes.Length <= maxSizeBytes)
                    {
                        // Return optimized image as data URI (always JPEG after processing)
                        var base64Resized = Convert.ToBase64String(resizedBytes);
                        return $"data:image/jpeg;base64,{base64Resized}";
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Image too large even after auto-resize. " +
                            $"Original: {imageBytes.Length / 1024.0 / 1024.0:F2}MB, " +
                            $"Resized: {resizedBytes.Length / 1024.0 / 1024.0:F2}MB, " +
                            $"Maximum: {maxSizeMB}MB. Please use a smaller image or different format.");
                    }
                }
                else if (imageBytes.Length > maxSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"Image size ({imageBytes.Length / 1024.0 / 1024.0:F2}MB) exceeds maximum allowed size ({maxSizeMB}MB). " +
                        "Auto-resize is disabled.");
                }

                // Image is within limits, return as is
                return originalDataUri;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Image processing failed: {ex.Message}", ex);
            }
        }
    }
}