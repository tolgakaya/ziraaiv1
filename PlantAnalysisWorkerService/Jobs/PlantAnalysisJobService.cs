using Business.Services.FileStorage;
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
        private readonly IFileStorageService _fileStorageService;

        public PlantAnalysisJobService(
            ILogger<PlantAnalysisJobService> logger,
            IPlantAnalysisRepository plantAnalysisRepository,
            IFileStorageService fileStorageService)
        {
            _logger = logger;
            _plantAnalysisRepository = plantAnalysisRepository;
            _fileStorageService = fileStorageService;
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task ProcessPlantAnalysisResultAsync(PlantAnalysisAsyncResponseDto result, string correlationId)
        {
            try
            {
                _logger.LogInformation($"Processing plant analysis result for ID: {result.AnalysisId}, Correlation: {correlationId}");

                // Find existing PlantAnalysis record by AnalysisId
                var existingAnalysis = await _plantAnalysisRepository.GetAsync(x => x.AnalysisId == result.AnalysisId);
                
                if (existingAnalysis == null)
                {
                    _logger.LogWarning($"No existing analysis found for ID: {result.AnalysisId}. Creating new record.");
                    
                    // Fallback: Create new record if not found (shouldn't happen in normal flow)
                    // Extract UserId from FarmerId format (F046 -> 46)
                    int? userId = null;
                    if (!string.IsNullOrEmpty(result.FarmerId) && result.FarmerId.StartsWith("F"))
                    {
                        if (int.TryParse(result.FarmerId.Substring(1), out var parsedUserId))
                        {
                            userId = parsedUserId;
                        }
                    }
                    
                    var newAnalysis = new PlantAnalysis
                    {
                        // Basic Info from response
                        AnalysisId = result.AnalysisId,
                        UserId = userId, // Extracted from FarmerId
                        FarmerId = result.FarmerId,
                        SponsorId = result.SponsorId,
                        SponsorUserId = result.SponsorUserId,        // Actual sponsor user ID
                        SponsorshipCodeId = result.SponsorshipCodeId, // SponsorshipCode table ID
                        FieldId = result.FieldId,
                        CropType = result.CropType,
                        Location = result.Location,
                        UrgencyLevel = result.UrgencyLevel,
                        Notes = result.Notes,
                        
                        // Image URL from image_metadata (critical fix!)
                        ImagePath = result.ImageMetadata?.URL ?? result.ImageUrl ?? result.ImagePath,
                        
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
                        
                        // Previous treatments
                        PreviousTreatments = result.PreviousTreatments != null ? 
                            JsonConvert.SerializeObject(result.PreviousTreatments) : null,
                        
                        // Contact Info
                        ContactPhone = result.ContactInfo?.Phone,
                        ContactEmail = result.ContactInfo?.Email,
                        
                        // Additional Info
                        AdditionalInfo = JsonConvert.SerializeObject(result.AdditionalInfo),
                        
                        // Analysis Results
                        AnalysisResult = JsonConvert.SerializeObject(result),
                        AnalysisStatus = "Completed",
                        Status = true,
                        CreatedDate = DateTime.Now,
                        AnalysisDate = result.Timestamp.ToLocalTime(),
                        Timestamp = result.Timestamp.ToLocalTime(),
                        N8nWebhookResponse = JsonConvert.SerializeObject(result),
                        
                        // Processing metadata (complete mapping)
                        AiModel = result.ProcessingMetadata?.AiModel ?? "",
                        WorkflowVersion = result.ProcessingMetadata?.WorkflowVersion ?? "",
                        ProcessingTimestamp = (result.ProcessingMetadata?.ProcessingTimestamp != null && result.ProcessingMetadata.ProcessingTimestamp > DateTime.MinValue)
                            ? result.ProcessingMetadata.ProcessingTimestamp
                            : result.Timestamp.ToLocalTime(),

                        // Store full metadata as JSONB
                        ProcessingMetadata = JsonConvert.SerializeObject(result.ProcessingMetadata),
                        TokenUsage = JsonConvert.SerializeObject(result.TokenUsage),
                        RequestMetadata = JsonConvert.SerializeObject(result.RequestMetadata),
                        
                        // Analysis results from response
                        PlantSpecies = result.PlantIdentification?.Species,
                        PlantVariety = result.PlantIdentification?.Variety,
                        GrowthStage = result.PlantIdentification?.GrowthStage,
                        IdentificationConfidence = result.PlantIdentification?.Confidence,
                        
                        VigorScore = result.HealthAssessment?.VigorScore,
                        HealthSeverity = result.HealthAssessment?.Severity,
                        StressIndicators = JsonConvert.SerializeObject(result.HealthAssessment?.StressIndicators ?? new string[0]),
                        DiseaseSymptoms = JsonConvert.SerializeObject(result.HealthAssessment?.DiseaseSymptoms ?? new string[0]),
                        
                        // Individual nutrients (all 14 elements)
                        Nitrogen = result.NutrientStatus?.Nitrogen,
                        Phosphorus = result.NutrientStatus?.Phosphorus,
                        Potassium = result.NutrientStatus?.Potassium,
                        Calcium = result.NutrientStatus?.Calcium,
                        Magnesium = result.NutrientStatus?.Magnesium,
                        Sulfur = result.NutrientStatus?.Sulfur,
                        Iron = result.NutrientStatus?.Iron,
                        Zinc = result.NutrientStatus?.Zinc,
                        Manganese = result.NutrientStatus?.Manganese,
                        Boron = result.NutrientStatus?.Boron,
                        Copper = result.NutrientStatus?.Copper,
                        Molybdenum = result.NutrientStatus?.Molybdenum,
                        Chlorine = result.NutrientStatus?.Chlorine,
                        Nickel = result.NutrientStatus?.Nickel,
                        PrimaryDeficiency = result.NutrientStatus?.PrimaryDeficiency,
                        NutrientSeverity = result.NutrientStatus?.Severity,
                        NutrientStatus = JsonConvert.SerializeObject(result.NutrientStatus),

                        // Pest and disease data
                        AffectedAreaPercentage = (int?)(result.PestDisease?.AffectedAreaPercentage ?? 0),
                        SpreadRisk = result.PestDisease?.SpreadRisk,
                        PrimaryIssue = result.PestDisease?.PrimaryIssue,
                        PestDisease = JsonConvert.SerializeObject(result.PestDisease),

                        // Environmental stress data
                        PrimaryStressor = result.EnvironmentalStress?.PrimaryStressor,
                        EnvironmentalStress = JsonConvert.SerializeObject(result.EnvironmentalStress),

                        // Additional JSONB fields
                        PlantIdentification = JsonConvert.SerializeObject(result.PlantIdentification),
                        HealthAssessment = JsonConvert.SerializeObject(result.HealthAssessment),
                        Summary = JsonConvert.SerializeObject(result.Summary),
                        ImageMetadata = JsonConvert.SerializeObject(result.ImageMetadata),
                        RiskAssessment = JsonConvert.SerializeObject(result.RiskAssessment),
                        ConfidenceNotes = JsonConvert.SerializeObject(result.ConfidenceNotes),

                        OverallHealthScore = result.Summary?.OverallHealthScore ?? 0,
                        PrimaryConcern = result.Summary?.PrimaryConcern,
                        CriticalIssuesCount = result.Summary?.CriticalIssuesCount,
                        Prognosis = result.Summary?.Prognosis,
                        EstimatedYieldImpact = result.Summary?.EstimatedYieldImpact,
                        ConfidenceLevel = result.Summary?.ConfidenceLevel,
                        FarmerFriendlySummary = result.FarmerFriendlySummary ?? "",
                        
                        // Store detailed data as JSON
                        DetailedAnalysisData = JsonConvert.SerializeObject(result),
                        Recommendations = JsonConvert.SerializeObject(result.Recommendations),
                        CrossFactorInsights = JsonConvert.SerializeObject(result.CrossFactorInsights),
                        
                        // Legacy fields for backward compatibility
                        PlantType = result.PlantIdentification?.Species,
                        ElementDeficiencies = JsonConvert.SerializeObject(result.NutrientStatus),
                        Diseases = JsonConvert.SerializeObject(result.PestDisease?.DiseasesDetected ?? new object[0]),
                        Pests = JsonConvert.SerializeObject(result.PestDisease?.PestsDetected ?? new object[0])
                    };
                    
                    _plantAnalysisRepository.Add(newAnalysis);
                }
                else
                {
                    _logger.LogInformation($"Updating existing analysis record for ID: {result.AnalysisId}");
                    
                    // Update existing record with analysis results
                    existingAnalysis.AnalysisResult = JsonConvert.SerializeObject(result);
                    existingAnalysis.AnalysisStatus = "Completed";
                    existingAnalysis.UpdatedDate = DateTime.Now;
                    existingAnalysis.AnalysisDate = result.Timestamp.ToLocalTime();
                    existingAnalysis.Timestamp = result.Timestamp.ToLocalTime();
                    existingAnalysis.N8nWebhookResponse = JsonConvert.SerializeObject(result);
                    
                    // Update ImagePath from image_metadata (critical fix!)
                    existingAnalysis.ImagePath = result.ImageMetadata?.URL ?? ConvertToFullUrlIfNeeded(existingAnalysis.ImagePath);
                    
                    // Update AI processing results (complete metadata mapping)
                    existingAnalysis.AiModel = result.ProcessingMetadata?.AiModel ?? "";
                    existingAnalysis.WorkflowVersion = result.ProcessingMetadata?.WorkflowVersion ?? "";
                    existingAnalysis.ProcessingTimestamp = (result.ProcessingMetadata?.ProcessingTimestamp != null && result.ProcessingMetadata.ProcessingTimestamp > DateTime.MinValue)
                        ? result.ProcessingMetadata.ProcessingTimestamp
                        : result.Timestamp.ToLocalTime();

                    // Update full metadata as JSONB
                    existingAnalysis.ProcessingMetadata = JsonConvert.SerializeObject(result.ProcessingMetadata);
                    existingAnalysis.TokenUsage = JsonConvert.SerializeObject(result.TokenUsage);
                    existingAnalysis.RequestMetadata = JsonConvert.SerializeObject(result.RequestMetadata);
                    
                    // Update plant identification results
                    existingAnalysis.PlantSpecies = result.PlantIdentification?.Species;
                    existingAnalysis.PlantVariety = result.PlantIdentification?.Variety;
                    existingAnalysis.GrowthStage = result.PlantIdentification?.GrowthStage;
                    existingAnalysis.IdentificationConfidence = result.PlantIdentification?.Confidence;
                    
                    // Update health assessment results
                    existingAnalysis.VigorScore = result.HealthAssessment?.VigorScore;
                    existingAnalysis.HealthSeverity = result.HealthAssessment?.Severity;
                    existingAnalysis.StressIndicators = JsonConvert.SerializeObject(result.HealthAssessment?.StressIndicators ?? new string[0]);
                    existingAnalysis.DiseaseSymptoms = JsonConvert.SerializeObject(result.HealthAssessment?.DiseaseSymptoms ?? new string[0]);
                    
                    // Update nutrient status - Individual nutrients (all 14 elements)
                    existingAnalysis.Nitrogen = result.NutrientStatus?.Nitrogen;
                    existingAnalysis.Phosphorus = result.NutrientStatus?.Phosphorus;
                    existingAnalysis.Potassium = result.NutrientStatus?.Potassium;
                    existingAnalysis.Calcium = result.NutrientStatus?.Calcium;
                    existingAnalysis.Magnesium = result.NutrientStatus?.Magnesium;
                    existingAnalysis.Sulfur = result.NutrientStatus?.Sulfur;
                    existingAnalysis.Iron = result.NutrientStatus?.Iron;
                    existingAnalysis.Zinc = result.NutrientStatus?.Zinc;
                    existingAnalysis.Manganese = result.NutrientStatus?.Manganese;
                    existingAnalysis.Boron = result.NutrientStatus?.Boron;
                    existingAnalysis.Copper = result.NutrientStatus?.Copper;
                    existingAnalysis.Molybdenum = result.NutrientStatus?.Molybdenum;
                    existingAnalysis.Chlorine = result.NutrientStatus?.Chlorine;
                    existingAnalysis.Nickel = result.NutrientStatus?.Nickel;
                    existingAnalysis.PrimaryDeficiency = result.NutrientStatus?.PrimaryDeficiency;
                    existingAnalysis.NutrientSeverity = result.NutrientStatus?.Severity;
                    existingAnalysis.NutrientStatus = JsonConvert.SerializeObject(result.NutrientStatus);

                    // Update pest and disease data
                    existingAnalysis.AffectedAreaPercentage = (int?)(result.PestDisease?.AffectedAreaPercentage ?? 0);
                    existingAnalysis.SpreadRisk = result.PestDisease?.SpreadRisk;
                    existingAnalysis.PrimaryIssue = result.PestDisease?.PrimaryIssue;
                    existingAnalysis.PestDisease = JsonConvert.SerializeObject(result.PestDisease);

                    // Update environmental stress data
                    existingAnalysis.PrimaryStressor = result.EnvironmentalStress?.PrimaryStressor;
                    existingAnalysis.EnvironmentalStress = JsonConvert.SerializeObject(result.EnvironmentalStress);

                    // Update additional JSONB fields
                    existingAnalysis.PlantIdentification = JsonConvert.SerializeObject(result.PlantIdentification);
                    existingAnalysis.HealthAssessment = JsonConvert.SerializeObject(result.HealthAssessment);
                    existingAnalysis.Summary = JsonConvert.SerializeObject(result.Summary);
                    existingAnalysis.ImageMetadata = JsonConvert.SerializeObject(result.ImageMetadata);
                    existingAnalysis.RiskAssessment = JsonConvert.SerializeObject(result.RiskAssessment);
                    existingAnalysis.ConfidenceNotes = JsonConvert.SerializeObject(result.ConfidenceNotes);

                    // Update summary results
                    existingAnalysis.OverallHealthScore = result.Summary?.OverallHealthScore ?? 0;
                    existingAnalysis.PrimaryConcern = result.Summary?.PrimaryConcern;
                    existingAnalysis.CriticalIssuesCount = result.Summary?.CriticalIssuesCount;
                    existingAnalysis.Prognosis = result.Summary?.Prognosis;
                    existingAnalysis.EstimatedYieldImpact = result.Summary?.EstimatedYieldImpact;
                    existingAnalysis.ConfidenceLevel = result.Summary?.ConfidenceLevel;
                    existingAnalysis.FarmerFriendlySummary = result.FarmerFriendlySummary ?? "";
                    
                    // Store detailed analysis data
                    existingAnalysis.DetailedAnalysisData = JsonConvert.SerializeObject(result);
                    existingAnalysis.Recommendations = JsonConvert.SerializeObject(result.Recommendations);
                    existingAnalysis.CrossFactorInsights = JsonConvert.SerializeObject(result.CrossFactorInsights);
                    
                    // Update legacy fields for backward compatibility
                    existingAnalysis.PlantType = result.PlantIdentification?.Species;
                    existingAnalysis.ElementDeficiencies = JsonConvert.SerializeObject(result.NutrientStatus);
                    existingAnalysis.Diseases = JsonConvert.SerializeObject(result.PestDisease?.DiseasesDetected ?? new object[0]);
                    existingAnalysis.Pests = JsonConvert.SerializeObject(result.PestDisease?.PestsDetected ?? new object[0]);
                    
                    _plantAnalysisRepository.Update(existingAnalysis);
                }

                // Save changes to database
                await _plantAnalysisRepository.SaveChangesAsync();

                _logger.LogInformation($"Successfully saved/updated plant analysis result: {result.AnalysisId}");

                // Schedule notification job
                BackgroundJob.Enqueue(() => SendNotificationAsync(result));

                _logger.LogInformation($"Scheduled notification job for analysis: {result.AnalysisId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process analysis result: {result.AnalysisId}");
                
                // Try to update status as failed if we have the analysis ID
                try
                {
                    var failedAnalysis = await _plantAnalysisRepository.GetAsync(x => x.AnalysisId == result.AnalysisId);
                    if (failedAnalysis != null)
                    {
                        failedAnalysis.AnalysisStatus = "Failed";
                        failedAnalysis.UpdatedDate = DateTime.Now;
                        _plantAnalysisRepository.Update(failedAnalysis);
                        await _plantAnalysisRepository.SaveChangesAsync();
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, $"Failed to update analysis status as failed for ID: {result.AnalysisId}");
                }
                
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
        
        private string ConvertToFullUrlIfNeeded(string imagePath)
        {
            try
            {
                // If already a full URL, return as is
                if (string.IsNullOrEmpty(imagePath) || imagePath.StartsWith("http"))
                {
                    return imagePath;
                }
                
                // Convert relative path to full URL
                var baseUrl = _fileStorageService.BaseUrl?.TrimEnd('/');
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    var relativePath = imagePath.TrimStart('/');
                    return $"{baseUrl}/{relativePath}";
                }
                
                // Fallback: return the original path
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to convert image path to URL: {ex.Message}");
                return imagePath;
            }
        }
    }
}