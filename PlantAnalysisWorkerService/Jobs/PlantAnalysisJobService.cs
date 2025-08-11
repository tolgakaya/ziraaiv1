using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Hangfire;
using Newtonsoft.Json;

namespace PlantAnalysisWorkerService.Jobs
{
    public class PlantAnalysisJobService : IPlantAnalysisJobService
    {
        private readonly ILogger<PlantAnalysisJobService> _logger;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;

        public PlantAnalysisJobService(
            ILogger<PlantAnalysisJobService> logger,
            IPlantAnalysisRepository plantAnalysisRepository)
        {
            _logger = logger;
            _plantAnalysisRepository = plantAnalysisRepository;
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task ProcessPlantAnalysisResultAsync(PlantAnalysisAsyncResponseDto result, string correlationId)
        {
            try
            {
                _logger.LogInformation($"Processing plant analysis result for ID: {result.AnalysisId}, Correlation: {correlationId}");

                // Create PlantAnalysis entity from the result
                var plantAnalysis = new PlantAnalysis
                {
                    // Basic Info
                    FarmerId = result.FarmerId,
                    SponsorId = result.SponsorId,
                    FieldId = result.FieldId,
                    CropType = result.CropType,
                    Location = result.Location,
                    UrgencyLevel = result.UrgencyLevel,
                    Notes = result.Notes,
                    
                    // GPS and Environment
                    Latitude = result.GpsCoordinates?.Lat,
                    Longitude = result.GpsCoordinates?.Lng,
                    Altitude = result.Altitude,
                    Temperature = result.Temperature,
                    Humidity = result.Humidity,
                    WeatherConditions = result.WeatherConditions,
                    SoilType = result.SoilType,
                    
                    // Dates
                    PlantingDate = result.PlantingDate,
                    ExpectedHarvestDate = result.ExpectedHarvestDate,
                    LastFertilization = result.LastFertilization,
                    LastIrrigation = result.LastIrrigation,
                    
                    // Contact Info
                    ContactPhone = result.ContactInfo?.Phone,
                    ContactEmail = result.ContactInfo?.Email,
                    
                    // Store additional info as JSON
                    AdditionalInfo = JsonConvert.SerializeObject(result.AdditionalInfo),
                    
                    // Analysis Results - stored as JSON
                    AnalysisResult = JsonConvert.SerializeObject(result),
                    
                    // Metadata
                    AnalysisId = result.AnalysisId,
                    AnalysisStatus = "Completed",
                    Status = true,
                    CreatedDate = result.Timestamp,
                    
                    // Image info (if we have image metadata)
                    ImageSizeKb = (decimal?)(result.ImageMetadata?.SizeKb),
                    
                    // Processing metadata
                    AiModel = result.ProcessingMetadata?.AiModel,
                    
                    // Analysis results from response
                    PlantSpecies = result.PlantIdentification?.Species,
                    PlantVariety = result.PlantIdentification?.Variety,
                    GrowthStage = result.PlantIdentification?.GrowthStage,
                    IdentificationConfidence = result.PlantIdentification?.Confidence,
                    
                    VigorScore = result.HealthAssessment?.VigorScore,
                    HealthSeverity = result.HealthAssessment?.Severity,
                    
                    PrimaryDeficiency = result.NutrientStatus?.PrimaryDeficiency,
                    
                    OverallHealthScore = result.Summary?.OverallHealthScore,
                    PrimaryConcern = result.Summary?.PrimaryConcern,
                    Prognosis = result.Summary?.Prognosis,
                    EstimatedYieldImpact = result.Summary?.EstimatedYieldImpact,
                    ConfidenceLevel = result.Summary?.ConfidenceLevel,
                    
                    // Store detailed data as JSON
                    DetailedAnalysisData = JsonConvert.SerializeObject(result),
                    Recommendations = JsonConvert.SerializeObject(result.Recommendations),
                    CrossFactorInsights = JsonConvert.SerializeObject(result.CrossFactorInsights)
                };

                // Save to database
                var savedEntity = _plantAnalysisRepository.Add(plantAnalysis);
                await _plantAnalysisRepository.SaveChangesAsync();

                _logger.LogInformation($"Successfully saved plant analysis result: {result.AnalysisId}");

                // Schedule notification job
                BackgroundJob.Enqueue(() => SendNotificationAsync(result));

                _logger.LogInformation($"Scheduled notification job for analysis: {result.AnalysisId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process analysis result: {result.AnalysisId}");
                throw; // Re-throw to trigger Hangfire retry
            }
        }

        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 10, 30 })]
        public async Task SendNotificationAsync(PlantAnalysisAsyncResponseDto result)
        {
            try
            {
                _logger.LogInformation($"Sending notification for plant analysis: {result.AnalysisId}");

                // TODO: Implement actual notification services
                // For now, just log the notification details
                var notificationMessage = $"Plant analysis completed for {result.FarmerId}. " +
                    $"Overall health score: {result.Summary?.OverallHealthScore}/10. " +
                    $"Primary concern: {result.Summary?.PrimaryConcern}";

                _logger.LogInformation($"Notification: {notificationMessage}");

                // Here you could implement:
                // 1. Email notification
                // 2. Push notification to mobile app
                // 3. SMS notification  
                // 4. WebSocket notification to web client
                // 5. Webhook to external system
                // 6. Slack/Teams notification

                // Simulate notification sending
                await Task.Delay(100);

                _logger.LogInformation($"Successfully sent notification for analysis: {result.AnalysisId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification for analysis: {result.AnalysisId}");
                throw; // Re-throw to trigger Hangfire retry
            }
        }
    }
}