using Business.Services.Configuration;
using Business.Services.FileStorage;
using Business.Services.ImageProcessing;
using Business.Services.MessageQueue;
using Core.Configuration;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Constants;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    public class PlantAnalysisAsyncService : IPlantAnalysisAsyncService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly IPlantAnalysisService _plantAnalysisService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly RabbitMQOptions _rabbitMQOptions;

        public PlantAnalysisAsyncService(
            IMessageQueueService messageQueueService,
            IImageProcessingService imageProcessingService,
            IConfigurationService configurationService,
            IPlantAnalysisRepository plantAnalysisRepository,
            IPlantAnalysisService plantAnalysisService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _messageQueueService = messageQueueService;
            _imageProcessingService = imageProcessingService;
            _configurationService = configurationService;
            _plantAnalysisRepository = plantAnalysisRepository;
            _plantAnalysisService = plantAnalysisService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _rabbitMQOptions = rabbitMQOptions.Value;
        }

        public async Task<string> QueuePlantAnalysisAsync(PlantAnalysisRequestDto request)
        {
            try
            {
                // Generate unique IDs for tracking
                var correlationId = Guid.NewGuid().ToString("N");
                var analysisId = $"async_analysis_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{correlationId[..8]}";

                // Process image for AI (aggressive optimization for token reduction)
                var processedImageDataUri = await ProcessImageForAIAsync(request.Image);
                
                // Use PlantAnalysisService for image upload (same as sync endpoint)
                var imageUrl = await _plantAnalysisService.SaveImageFileAsync(processedImageDataUri, 999999);
                
                // Store full URL in database (consistent with sync endpoint)
                var imagePath = imageUrl;

                // Create initial PlantAnalysis entity with all request data
                var plantAnalysis = new Entities.Concrete.PlantAnalysis
                {
                    // Basic Info
                    AnalysisId = analysisId,
                    UserId = request.UserId,
                    FarmerId = request.FarmerId,
                    SponsorId = request.SponsorId,
                    SponsorUserId = request.SponsorUserId,        // Actual sponsor user ID
                    SponsorshipCodeId = request.SponsorshipCodeId, // SponsorshipCode table ID
                    FieldId = request.FieldId,
                    CropType = request.CropType,
                    Location = request.Location,
                    UrgencyLevel = request.UrgencyLevel,
                    Notes = request.Notes,
                    
                    // GPS and Environment
                    Latitude = request.GpsCoordinates?.Lat,
                    Longitude = request.GpsCoordinates?.Lng,
                    Altitude = request.Altitude,
                    Temperature = request.Temperature,
                    Humidity = request.Humidity,
                    WeatherConditions = request.WeatherConditions,
                    SoilType = request.SoilType,
                    
                    // Dates
                    PlantingDate = request.PlantingDate,
                    ExpectedHarvestDate = request.ExpectedHarvestDate,
                    LastFertilization = request.LastFertilization,
                    LastIrrigation = request.LastIrrigation,
                    
                    // Previous treatments
                    PreviousTreatments = request.PreviousTreatments != null ? 
                        JsonConvert.SerializeObject(request.PreviousTreatments) : null,
                    
                    // Contact Info
                    ContactPhone = request.ContactInfo?.Phone,
                    ContactEmail = request.ContactInfo?.Email,
                    
                    // Additional Info
                    AdditionalInfo = JsonConvert.SerializeObject(request.AdditionalInfo),
                    
                    // Image info
                    ImagePath = imagePath,
                    ImageSizeKb = (decimal?)(Convert.FromBase64String(processedImageDataUri.Split(',')[1]).Length / 1024.0),
                    
                    // Status
                    AnalysisStatus = "Processing",
                    Status = true,
                    CreatedDate = DateTime.Now
                };

                // Save to database first
                _plantAnalysisRepository.Add(plantAnalysis);
                await _plantAnalysisRepository.SaveChangesAsync();

                // Get queue name from appsettings
                var queueName = _rabbitMQOptions.Queues.PlantAnalysisRequest;

                // Create async request payload for RabbitMQ
                var asyncRequest = new PlantAnalysisAsyncRequestDto
                {
                    // Send URL instead of base64 to avoid token limits
                    ImageUrl = imageUrl,
                    Image = null, // Don't send base64 anymore
                    UserId = request.UserId,
                    FarmerId = request.FarmerId,
                    SponsorId = request.SponsorId,
                    SponsorUserId = request.SponsorUserId,        // Actual sponsor user ID
                    SponsorshipCodeId = request.SponsorshipCodeId, // SponsorshipCode table ID
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
                    // If publish fails, update the status in database
                    plantAnalysis.AnalysisStatus = "QueueFailed";
                    _plantAnalysisRepository.Update(plantAnalysis);
                    await _plantAnalysisRepository.SaveChangesAsync();
                    
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

        
        private async Task<string> ProcessImageForAIAsync(string originalDataUri)
        {
            try
            {
                if (string.IsNullOrEmpty(originalDataUri))
                    throw new ArgumentException("Image data URI is required");

                // Extract image bytes from data URI
                var base64Data = originalDataUri.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                // Get AI-optimized configuration (much smaller for token reduction)
                var maxSizeMB = await _configurationService.GetDecimalValueAsync(
                    "AI_IMAGE_MAX_SIZE_MB", 0.1m); // 100KB default for AI

                var enableAIOptimization = await _configurationService.GetBoolValueAsync(
                    "AI_IMAGE_OPTIMIZATION", true);

                if (!enableAIOptimization)
                {
                    return originalDataUri;
                }

                // Aggressive optimization for AI processing
                var optimizedBytes = await _imageProcessingService.ResizeToTargetSizeAsync(
                    imageBytes, 
                    (double)maxSizeMB,
                    maxWidth: 800,  // Lower resolution for AI
                    maxHeight: 600
                );

                // Always return as JPEG for consistency and smaller size
                var base64Optimized = Convert.ToBase64String(optimizedBytes);
                return $"data:image/jpeg;base64,{base64Optimized}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Image processing for AI failed: {ex.Message}", ex);
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