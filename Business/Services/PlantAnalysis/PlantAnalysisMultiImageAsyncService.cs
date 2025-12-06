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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    /// <summary>
    /// Service for multi-image plant analysis async operations.
    /// Processes up to 5 images, uploads to storage, and queues for AI analysis.
    /// </summary>
    public class PlantAnalysisMultiImageAsyncService : IPlantAnalysisMultiImageAsyncService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly RabbitMQOptions _rabbitMQOptions;

        // Feature flag: Switch between OLD system (direct to worker) and NEW system (via Dispatcher)
        private readonly bool _useRawAnalysisQueue;

        public PlantAnalysisMultiImageAsyncService(
            IMessageQueueService messageQueueService,
            IImageProcessingService imageProcessingService,
            IConfigurationService configurationService,
            IPlantAnalysisRepository plantAnalysisRepository,
            IFileStorageService fileStorageService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _messageQueueService = messageQueueService;
            _imageProcessingService = imageProcessingService;
            _configurationService = configurationService;
            _plantAnalysisRepository = plantAnalysisRepository;
            _fileStorageService = fileStorageService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _rabbitMQOptions = rabbitMQOptions.Value;

            // Read feature flag from configuration (defaults to false for backward compatibility)
            _useRawAnalysisQueue = configuration.GetValue<bool>("PlantAnalysis:UseRawAnalysisQueue", false);

            // CRITICAL DEBUG LOG: Verify feature flag is read correctly
            Console.WriteLine($"[PlantAnalysisMultiImageAsyncService] UseRawAnalysisQueue = {_useRawAnalysisQueue}");
            Console.WriteLine($"[PlantAnalysisMultiImageAsyncService] RawAnalysisRequest Queue = {_rabbitMQOptions.Queues.RawAnalysisRequest}");
            Console.WriteLine($"[PlantAnalysisMultiImageAsyncService] PlantAnalysisMultiImageRequest Queue = {_rabbitMQOptions.Queues.PlantAnalysisMultiImageRequest}");
        }

        public async Task<(string analysisId, int plantAnalysisId)> QueuePlantAnalysisAsync(PlantAnalysisMultiImageRequestDto request)
        {
            // CRITICAL DEBUG: Method entry
            Console.WriteLine($"[QueueMultiImageAnalysisAsync] === METHOD ENTRY ===");
            Console.WriteLine($"[QueueMultiImageAnalysisAsync] _useRawAnalysisQueue = {_useRawAnalysisQueue}");

            try
            {
                // Generate unique IDs for tracking
                var correlationId = Guid.NewGuid().ToString("N");
                var analysisId = $"async_multi_analysis_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{correlationId[..8]}";

                Console.WriteLine($"[QueueMultiImageAnalysisAsync] Generated AnalysisId = {analysisId}");

                // Process and upload all images
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] Starting image processing...");
                var imageUrls = await ProcessAndUploadAllImagesAsync(request, analysisId);
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] Image processing complete");

                // Create initial PlantAnalysis entity with all request data
                var plantAnalysis = new Entities.Concrete.PlantAnalysis
                {
                    // Basic Info
                    AnalysisId = analysisId,
                    UserId = request.UserId,
                    FarmerId = request.FarmerId,
                    SponsorId = request.SponsorId,
                    SponsorUserId = request.SponsorUserId,
                    SponsorshipCodeId = request.SponsorshipCodeId,
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

                    // Multi-Image URLs
                    ImageUrl = imageUrls.MainImageUrl,
                    ImagePath = imageUrls.MainImageUrl, // Legacy field
                    LeafTopUrl = imageUrls.LeafTopUrl,
                    LeafBottomUrl = imageUrls.LeafBottomUrl,
                    PlantOverviewUrl = imageUrls.PlantOverviewUrl,
                    RootUrl = imageUrls.RootUrl,

                    // Image size (main image only for backward compatibility)
                    ImageSizeKb = imageUrls.MainImageSizeKb,

                    // Status
                    AnalysisStatus = "Processing",
                    Status = true,
                    CreatedDate = DateTime.Now
                };

                // Save to database first
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] Saving to database...");
                _plantAnalysisRepository.Add(plantAnalysis);
                await _plantAnalysisRepository.SaveChangesAsync();
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] Database save complete - PlantAnalysisId: {plantAnalysis.Id}");

                // Get queue name based on feature flag
                // NEW system: raw-analysis-queue → Dispatcher → Provider queues (unified queue for all requests)
                // OLD system: plant-analysis-multi-image-requests → Worker Pool (direct)
                var queueName = _useRawAnalysisQueue
                    ? _rabbitMQOptions.Queues.RawAnalysisRequest  // NEW system (unified)
                    : _rabbitMQOptions.Queues.PlantAnalysisMultiImageRequest; // OLD system (legacy)

                // CRITICAL DEBUG LOG: Verify which queue is being used
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] _useRawAnalysisQueue = {_useRawAnalysisQueue}");
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] Selected Queue = {queueName}");
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] AnalysisId = {analysisId}");

                // Create async request payload for RabbitMQ
                var asyncRequest = new PlantAnalysisMultiImageAsyncRequestDto
                {
                    // Image URLs (no base64 to avoid token limits)
                    ImageUrl = imageUrls.MainImageUrl,
                    LeafTopUrl = imageUrls.LeafTopUrl,
                    LeafBottomUrl = imageUrls.LeafBottomUrl,
                    PlantOverviewUrl = imageUrls.PlantOverviewUrl,
                    RootUrl = imageUrls.RootUrl,

                    // User context
                    UserId = request.UserId,
                    FarmerId = request.FarmerId,
                    SponsorId = request.SponsorId,
                    SponsorUserId = request.SponsorUserId,
                    SponsorshipCodeId = request.SponsorshipCodeId,

                    // Metadata
                    Location = request.Location,
                    GpsCoordinates = request.GpsCoordinates,
                    CropType = request.CropType,
                    FieldId = request.FieldId,
                    UrgencyLevel = request.UrgencyLevel,
                    Notes = request.Notes,

                    // Additional fields
                    Altitude = request.Altitude,
                    PlantingDate = request.PlantingDate,
                    ExpectedHarvestDate = request.ExpectedHarvestDate,
                    LastFertilization = request.LastFertilization,
                    LastIrrigation = request.LastIrrigation,
                    PreviousTreatments = request.PreviousTreatments,
                    WeatherConditions = request.WeatherConditions,
                    Temperature = request.Temperature,
                    Humidity = request.Humidity,
                    SoilType = request.SoilType,
                    ContactInfo = request.ContactInfo,
                    AdditionalInfo = request.AdditionalInfo,

                    // Queue management (multi-image specific)
                    ResponseQueue = _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult,
                    CorrelationId = correlationId,
                    AnalysisId = analysisId
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

                return (analysisId, plantAnalysis.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] EXCEPTION: {ex.Message}");
                Console.WriteLine($"[QueueMultiImageAnalysisAsync] Stack Trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to queue multi-image plant analysis: {ex.Message}", ex);
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

        /// <summary>
        /// Process and upload all provided images (main + optional images).
        /// Each image is optimized to 100KB for AI processing and uploaded to storage.
        /// </summary>
        private async Task<MultiImageUrls> ProcessAndUploadAllImagesAsync(
            PlantAnalysisMultiImageRequestDto request,
            string analysisId)
        {
            var result = new MultiImageUrls();

            // Process main image (required)
            var processedMainImage = await ProcessImageForAIAsync(request.Image);
            result.MainImageUrl = await _fileStorageService.UploadImageFromDataUriAsync(
                processedMainImage,
                $"{analysisId}_main",
                "plant-images");
            result.MainImageSizeKb = (decimal)(Convert.FromBase64String(processedMainImage.Split(',')[1]).Length / 1024.0);

            // Process optional images
            if (!string.IsNullOrEmpty(request.LeafTopImage))
            {
                var processed = await ProcessImageForAIAsync(request.LeafTopImage);
                result.LeafTopUrl = await _fileStorageService.UploadImageFromDataUriAsync(
                    processed,
                    $"{analysisId}_leaf_top",
                    "plant-images");
            }

            if (!string.IsNullOrEmpty(request.LeafBottomImage))
            {
                var processed = await ProcessImageForAIAsync(request.LeafBottomImage);
                result.LeafBottomUrl = await _fileStorageService.UploadImageFromDataUriAsync(
                    processed,
                    $"{analysisId}_leaf_bottom",
                    "plant-images");
            }

            if (!string.IsNullOrEmpty(request.PlantOverviewImage))
            {
                var processed = await ProcessImageForAIAsync(request.PlantOverviewImage);
                result.PlantOverviewUrl = await _fileStorageService.UploadImageFromDataUriAsync(
                    processed,
                    $"{analysisId}_plant_overview",
                    "plant-images");
            }

            if (!string.IsNullOrEmpty(request.RootImage))
            {
                var processed = await ProcessImageForAIAsync(request.RootImage);
                result.RootUrl = await _fileStorageService.UploadImageFromDataUriAsync(
                    processed,
                    $"{analysisId}_root",
                    "plant-images");
            }

            return result;
        }

        /// <summary>
        /// Aggressive image optimization for AI processing (target: 100KB).
        /// Reduces token usage by 99.6% compared to base64 encoding.
        /// </summary>
        private async Task<string> ProcessImageForAIAsync(string originalDataUri)
        {
            try
            {
                if (string.IsNullOrEmpty(originalDataUri))
                    throw new ArgumentException("Image data URI is required");

                // Extract image bytes from data URI
                var base64Data = originalDataUri.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                // Get AI-optimized configuration (100KB default for AI)
                var maxSizeMB = await _configurationService.GetDecimalValueAsync(
                    "AI_IMAGE_MAX_SIZE_MB", 0.1m);

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
                    maxWidth: 800,
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

        /// <summary>
        /// Helper class to hold all image URLs after processing.
        /// </summary>
        private class MultiImageUrls
        {
            public string MainImageUrl { get; set; }
            public decimal MainImageSizeKb { get; set; }
            public string LeafTopUrl { get; set; }
            public string LeafBottomUrl { get; set; }
            public string PlantOverviewUrl { get; set; }
            public string RootUrl { get; set; }
        }
    }
}
