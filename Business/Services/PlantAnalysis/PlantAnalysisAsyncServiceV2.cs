using Business.Services.Configuration;
using Business.Services.ImageProcessing;
using Business.Services.MessageQueue;
using Core.Configuration;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Constants;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    /// <summary>
    /// Improved async service that sends image URL instead of base64 to avoid token limits
    /// </summary>
    public class PlantAnalysisAsyncServiceV2 : IPlantAnalysisAsyncService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly RabbitMQOptions _rabbitMQOptions;

        public PlantAnalysisAsyncServiceV2(
            IMessageQueueService messageQueueService,
            IImageProcessingService imageProcessingService,
            IConfigurationService configurationService,
            IPlantAnalysisRepository plantAnalysisRepository,
            IHttpContextAccessor httpContextAccessor,
            IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _messageQueueService = messageQueueService;
            _imageProcessingService = imageProcessingService;
            _configurationService = configurationService;
            _plantAnalysisRepository = plantAnalysisRepository;
            _httpContextAccessor = httpContextAccessor;
            _rabbitMQOptions = rabbitMQOptions.Value;
        }

        public async Task<string> QueuePlantAnalysisAsync(PlantAnalysisRequestDto request)
        {
            try
            {
                // Generate unique IDs
                var correlationId = Guid.NewGuid().ToString("N");
                var analysisId = $"async_analysis_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{correlationId[..8]}";

                // Process and optimize image
                var processedImageDataUri = await ProcessImageForAIAsync(request.Image);
                
                // Save image and get accessible URL
                var (imagePath, imageUrl) = await SaveImageAndGetUrlAsync(processedImageDataUri, analysisId);

                // Save initial record to database
                var plantAnalysis = await SaveInitialRecordAsync(request, analysisId, imagePath, processedImageDataUri);

                // Create request with IMAGE URL instead of base64
                var asyncRequest = CreateAsyncRequest(request, analysisId, correlationId, imageUrl);

                // Publish to queue
                await PublishToQueueAsync(asyncRequest, correlationId, plantAnalysis);

                return analysisId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to queue plant analysis: {ex.Message}", ex);
            }
        }

        private async Task<string> ProcessImageForAIAsync(string originalDataUri)
        {
            try
            {
                if (string.IsNullOrEmpty(originalDataUri))
                    throw new ArgumentException("Image data URI is required");

                var base64Data = originalDataUri.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                // Get optimal size for AI processing (smaller = fewer tokens)
                var maxSizeMB = await _configurationService.GetDecimalValueAsync(
                    "AI_IMAGE_MAX_SIZE_MB", 0.1m); // 100KB for AI to minimize tokens

                var enableOptimization = await _configurationService.GetBoolValueAsync(
                    "AI_IMAGE_OPTIMIZATION", true);

                if (!enableOptimization)
                    return originalDataUri;

                // Aggressive optimization for AI
                var optimizedBytes = await OptimizeImageForAIAsync(imageBytes, (double)maxSizeMB);
                
                // Always return as JPEG for consistency
                var base64Optimized = Convert.ToBase64String(optimizedBytes);
                return $"data:image/jpeg;base64,{base64Optimized}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Image processing failed: {ex.Message}", ex);
            }
        }

        private async Task<byte[]> OptimizeImageForAIAsync(byte[] imageBytes, double targetSizeMB)
        {
            // Special optimization for AI processing
            // Lower resolution and quality to minimize tokens
            var aiOptimizedBytes = await _imageProcessingService.ResizeToTargetSizeAsync(
                imageBytes, 
                targetSizeMB,
                maxWidth: 800,  // Lower resolution for AI
                maxHeight: 600
            );

            return aiOptimizedBytes;
        }

        private async Task<(string path, string url)> SaveImageAndGetUrlAsync(string dataUri, string analysisId)
        {
            // Save image to file system
            var relativePath = await SaveImageToFileSystemAsync(dataUri, analysisId);
            
            // Generate accessible URL
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var imageUrl = $"{baseUrl}/{relativePath.Replace('\\', '/')}";
                return (relativePath, imageUrl);
            }
            
            // Fallback to local path
            return (relativePath, $"https://localhost:5001/{relativePath.Replace('\\', '/')}");
        }

        private async Task<string> SaveImageToFileSystemAsync(string dataUri, string analysisId)
        {
            try
            {
                var parts = dataUri.Split(',');
                var base64Data = parts[1];
                var imageBytes = Convert.FromBase64String(base64Data);
                
                // Always save as JPEG for consistency
                var fileName = $"plant_analysis_{analysisId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.jpg";
                var relativePath = Path.Combine("uploads", "plant-images", fileName);
                var fullPath = Path.Combine("wwwroot", relativePath);
                
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                await File.WriteAllBytesAsync(fullPath, imageBytes);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save image: {ex.Message}", ex);
            }
        }

        private async Task<Entities.Concrete.PlantAnalysis> SaveInitialRecordAsync(
            PlantAnalysisRequestDto request, string analysisId, string imagePath, string processedImageDataUri)
        {
            var plantAnalysis = new Entities.Concrete.PlantAnalysis
            {
                AnalysisId = analysisId,
                UserId = request.UserId,
                FarmerId = request.FarmerId,
                SponsorId = request.SponsorId,
                FieldId = request.FieldId,
                CropType = request.CropType,
                Location = request.Location,
                UrgencyLevel = request.UrgencyLevel,
                Notes = request.Notes,
                Latitude = request.GpsCoordinates?.Lat,
                Longitude = request.GpsCoordinates?.Lng,
                Altitude = request.Altitude,
                Temperature = request.Temperature,
                Humidity = request.Humidity,
                WeatherConditions = request.WeatherConditions,
                SoilType = request.SoilType,
                PlantingDate = request.PlantingDate,
                ExpectedHarvestDate = request.ExpectedHarvestDate,
                LastFertilization = request.LastFertilization,
                LastIrrigation = request.LastIrrigation,
                PreviousTreatments = request.PreviousTreatments != null ? 
                    JsonConvert.SerializeObject(request.PreviousTreatments) : null,
                ContactPhone = request.ContactInfo?.Phone,
                ContactEmail = request.ContactInfo?.Email,
                AdditionalInfo = JsonConvert.SerializeObject(request.AdditionalInfo),
                ImagePath = imagePath,
                ImageSizeKb = (decimal?)(Convert.FromBase64String(processedImageDataUri.Split(',')[1]).Length / 1024.0),
                AnalysisStatus = "Processing",
                Status = true,
                CreatedDate = DateTime.UtcNow
            };

            _plantAnalysisRepository.Add(plantAnalysis);
            await _plantAnalysisRepository.SaveChangesAsync();
            
            return plantAnalysis;
        }

        private PlantAnalysisAsyncRequestDto CreateAsyncRequest(
            PlantAnalysisRequestDto request, string analysisId, string correlationId, string imageUrl)
        {
            return new PlantAnalysisAsyncRequestDto
            {
                // Use ImageUrl instead of base64 Image
                ImageUrl = imageUrl,  // NEW: Send URL instead
                Image = null,         // Don't send base64 to avoid token limits
                
                UserId = request.UserId,
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
        }

        private async Task PublishToQueueAsync(
            PlantAnalysisAsyncRequestDto asyncRequest, string correlationId, Entities.Concrete.PlantAnalysis plantAnalysis)
        {
            var queueName = _rabbitMQOptions.Queues.PlantAnalysisRequest;
            var publishResult = await _messageQueueService.PublishAsync(queueName, asyncRequest, correlationId);

            if (!publishResult)
            {
                plantAnalysis.AnalysisStatus = "QueueFailed";
                _plantAnalysisRepository.Update(plantAnalysis);
                await _plantAnalysisRepository.SaveChangesAsync();
                
                throw new InvalidOperationException("Failed to publish message to queue");
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
    }
}