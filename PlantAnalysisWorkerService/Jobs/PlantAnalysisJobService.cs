using Business.Services.FileStorage;
using Business.Services.Notification;
using Business.Services.Referral;
using Core.CrossCuttingConcerns.Caching;
using DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;
using Entities.Concrete;
using Entities.Dtos;
using Hangfire;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace PlantAnalysisWorkerService.Jobs
{
    public class PlantAnalysisJobService : IPlantAnalysisJobService
    {
        private readonly ILogger<PlantAnalysisJobService> _logger;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IReferralTrackingService _referralTrackingService;
        private readonly IReferralRewardService _referralRewardService;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
        private readonly ICacheManager _cacheManager;

        public PlantAnalysisJobService(
            ILogger<PlantAnalysisJobService> logger,
            IPlantAnalysisRepository plantAnalysisRepository,
            IFileStorageService fileStorageService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IReferralTrackingService referralTrackingService,
            IReferralRewardService referralRewardService,
            IUserSubscriptionRepository userSubscriptionRepository,
            ISponsorshipCodeRepository sponsorshipCodeRepository,
            ICacheManager cacheManager)
        {
            _logger = logger;
            _plantAnalysisRepository = plantAnalysisRepository;
            _fileStorageService = fileStorageService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _referralTrackingService = referralTrackingService;
            _referralRewardService = referralRewardService;
            _userSubscriptionRepository = userSubscriptionRepository;
            _sponsorshipCodeRepository = sponsorshipCodeRepository;
            _cacheManager = cacheManager;
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
                        CrossFactorInsights = JsonConvert.SerializeObject(result.CrossFactorInsights),
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
                    
                    // Update health assessment results (all fields)
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

                    // Debug logging
                    _logger.LogInformation($"PestDisease JSON: {existingAnalysis.PestDisease}");

                    // Update environmental stress data
                    existingAnalysis.PrimaryStressor = result.EnvironmentalStress?.PrimaryStressor;
                    existingAnalysis.EnvironmentalStress = JsonConvert.SerializeObject(result.EnvironmentalStress);

                    // Debug logging
                    _logger.LogInformation($"EnvironmentalStress JSON: {existingAnalysis.EnvironmentalStress}");
                    _logger.LogInformation($"RiskAssessment JSON: {JsonConvert.SerializeObject(result.RiskAssessment)}");

                    // Update additional JSONB fields
                    existingAnalysis.PlantIdentification = JsonConvert.SerializeObject(result.PlantIdentification);
                    existingAnalysis.HealthAssessment = JsonConvert.SerializeObject(result.HealthAssessment);
                    existingAnalysis.Summary = JsonConvert.SerializeObject(result.Summary);
                    existingAnalysis.CrossFactorInsights = JsonConvert.SerializeObject(result.CrossFactorInsights);
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
                    
                    // Update legacy fields for backward compatibility
                    existingAnalysis.PlantType = result.PlantIdentification?.Species;
                    existingAnalysis.ElementDeficiencies = JsonConvert.SerializeObject(result.NutrientStatus);
                    existingAnalysis.Diseases = JsonConvert.SerializeObject(result.PestDisease?.DiseasesDetected ?? new object[0]);
                    existingAnalysis.Pests = JsonConvert.SerializeObject(result.PestDisease?.PestsDetected ?? new object[0]);
                    
                    // 🔥 CRITICAL: Capture active sponsor attribution
                    await CaptureActiveSponsorAsync(existingAnalysis, existingAnalysis.UserId);
                    
                    _plantAnalysisRepository.Update(existingAnalysis);
                }

                // Save changes to database
                await _plantAnalysisRepository.SaveChangesAsync();

                _logger.LogInformation($"Successfully saved/updated plant analysis result: {result.AnalysisId}");

                // ✅ REFERRAL VALIDATION & REWARD PROCESSING
                // Check if this is the user's first analysis and trigger referral validation
                try
                {
                    // Extract UserId from FarmerId format (F046 -> 46)
                    int? userId = null;
                    if (existingAnalysis != null)
                    {
                        userId = existingAnalysis.UserId;
                    }
                    else if (!string.IsNullOrEmpty(result.FarmerId) && result.FarmerId.StartsWith("F"))
                    {
                        if (int.TryParse(result.FarmerId.Substring(1), out var parsedUserId))
                        {
                            userId = parsedUserId;
                        }
                    }

                    if (userId.HasValue)
                    {
                        // Check if this is the user's first completed analysis AND reward hasn't been processed yet
                    var analysisCount = await _plantAnalysisRepository.Query()
                        .Where(a => a.UserId == userId.Value && a.AnalysisStatus == "Completed")
                        .CountAsync();

                    _logger.LogInformation($"📊 User {userId.Value} has {analysisCount} completed analysis(es)");

                    // Get existing tracking record to check if reward was already processed
                    var trackingResult = await _referralTrackingService.GetByRefereeUserIdAsync(userId.Value);
                    var hasUnprocessedReferral = trackingResult.Success && 
                                                trackingResult.Data != null && 
                                                trackingResult.Data.Status < (int)Entities.Concrete.ReferralTrackingStatus.Rewarded;

                    if (analysisCount >= 1 && hasUnprocessedReferral) // First completed analysis with unprocessed referral
                        {
                            _logger.LogInformation($"🎯 First analysis detected for user {userId.Value}, validating referral...");

                            // Validate referral (updates tracking to Validated status)
                            var validationResult = await _referralTrackingService.ValidateReferralAsync(userId.Value);
                            
                            if (validationResult.Success)
                            {
                                _logger.LogInformation($"✅ Referral validated for user {userId.Value}");

                            // Use the tracking record we already fetched above
                            if (trackingResult.Success && trackingResult.Data != null)
                                {
                                    var tracking = trackingResult.Data;
                                    
                                    // Process the reward (awards credits to referrer)
                                    var rewardResult = await _referralRewardService.ProcessRewardAsync(tracking.Id);
                                    
                                    if (rewardResult.Success)
                                    {
                                        _logger.LogInformation($"💰 Referral reward processed successfully for tracking ID {tracking.Id}");
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"⚠️ Failed to process referral reward: {rewardResult.Message}");
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"ℹ️ No referral to validate for user {userId.Value}: {validationResult.Message}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Cannot process referral: Unable to extract userId from FarmerId: {result.FarmerId}");
                    }
                }
                catch (Exception refEx)
                {
                    // Log error but don't fail the entire job
                    _logger.LogError(refEx, $"❌ Error processing referral validation/reward for analysis {result.AnalysisId}");
                }

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
                _logger.LogInformation($"🔔 Sending real-time notification for plant analysis: {result.AnalysisId}");

                // Extract UserId from FarmerId format (F046 -> 46)
                int? userId = null;
                if (!string.IsNullOrEmpty(result.FarmerId) && result.FarmerId.StartsWith("F"))
                {
                    if (int.TryParse(result.FarmerId.Substring(1), out var parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                if (!userId.HasValue)
                {
                    _logger.LogWarning($"⚠️ Cannot send notification: Unable to extract userId from FarmerId: {result.FarmerId}");
                    return;
                }

                // Fetch the complete analysis from database to get the ID
                var analysis = await _plantAnalysisRepository.GetAsync(x => x.AnalysisId == result.AnalysisId);
                if (analysis == null)
                {
                    _logger.LogWarning($"⚠️ Cannot send notification: Analysis not found in database: {result.AnalysisId}");
                    return;
                }

                // Create notification DTO
                var notification = new PlantAnalysisNotificationDto
                {
                    AnalysisId = analysis.Id, // Use database ID for deep link
                    UserId = userId.Value,
                    Status = "Completed",
                    CompletedAt = DateTime.UtcNow,
                    CropType = result.CropType,
                    PrimaryConcern = result.Summary?.PrimaryConcern,
                    OverallHealthScore = result.Summary?.OverallHealthScore,
                    ImageUrl = result.ImageMetadata?.URL ?? analysis.ImagePath,
                    DeepLink = $"app://analysis/{analysis.Id}", // Use database ID
                    SponsorId = result.SponsorId,
                    Message = $"Your {result.CropType} analysis is ready! Health Score: {result.Summary?.OverallHealthScore}/100"
                };

                // 🆕 Send notification via HTTP to WebAPI (cross-process communication)
                await SendNotificationViaHttp(userId.Value, notification);

                _logger.LogInformation(
                    $"✅ Successfully sent notification request - UserId: {userId.Value}, AnalysisId: {result.AnalysisId}, " +
                    $"HealthScore: {result.Summary?.OverallHealthScore}/100");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send notification for analysis: {result.AnalysisId}");
                // Don't re-throw - Hangfire will retry automatically
            }
        }

        /// <summary>
        /// Send notification to WebAPI via HTTP (cross-process communication)
        /// WebAPI will broadcast via SignalR Hub
        /// </summary>
        private async Task SendNotificationViaHttp(int userId, PlantAnalysisNotificationDto notification)
        {
            try
            {
                // Use .NET Configuration API (automatically reads Railway env vars with __ pattern)
                var webApiBaseUrl = _configuration.GetValue<string>("WebAPI:BaseUrl")
                                   ?? "https://localhost:5001"; // Fallback for local development

                var internalSecret = _configuration.GetValue<string>("WebAPI:InternalSecret")
                                    ?? "ZiraAI_Internal_Secret_2025"; // Fallback for local development

                // Log configuration source
                if (webApiBaseUrl == "https://localhost:5001")
                {
                    _logger.LogWarning("⚠️ Using default WebAPI URL - NOT SAFE FOR PRODUCTION!");
                }
                else
                {
                    _logger.LogInformation("✅ WebAPI URL loaded: {Url}", webApiBaseUrl);
                }

                if (internalSecret == "ZiraAI_Internal_Secret_2025")
                {
                    _logger.LogWarning("⚠️ Using default internal secret - NOT SAFE FOR PRODUCTION!");
                }
                else
                {
                    var secretPreview = internalSecret.Length > 10 
                        ? $"{internalSecret.Substring(0, 5)}...{internalSecret.Substring(internalSecret.Length - 5)}" 
                        : "***";
                    _logger.LogInformation("✅ Internal secret loaded - Length: {Length}, Preview: {Preview}", 
                        internalSecret.Length, secretPreview);
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(webApiBaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var requestBody = new
                {
                    internalSecret,
                    userId,
                    notification
                };

                var response = await httpClient.PostAsJsonAsync("/api/internal/signalr/analysis-completed", requestBody);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ HTTP notification sent successfully to WebAPI");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"⚠️ HTTP notification failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send HTTP notification to WebAPI");
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


        /// <summary>
        /// Capture active sponsor attribution for this analysis
        /// Critical for: logo display, sponsor access control, messaging permissions, dashboard analytics
        /// </summary>
        /// <summary>
        /// Invalidate sponsor dashboard cache when analysis is created/completed
        /// </summary>
        private void InvalidateSponsorDashboardCache(int sponsorId)
        {
            var cacheKey = $"SponsorDashboard:{sponsorId}";
            _cacheManager.Remove(cacheKey);
        }

        private async Task CaptureActiveSponsorAsync(PlantAnalysis analysis, int? userId)
        {
            if (!userId.HasValue)
            {
                _logger.LogInformation($"[SponsorAttribution] ⚠️ No userId provided for analysis {analysis.Id} - skipping sponsor capture");
                return;
            }

            try
            {
                _logger.LogInformation($"[SponsorAttribution] 🔍 Looking for active sponsorship for user {userId.Value}");

                // Get active sponsored subscription
                var activeSponsorship = await _userSubscriptionRepository.GetAsync(s =>
                    s.UserId == userId.Value &&
                    s.IsSponsoredSubscription &&
                    s.QueueStatus == SubscriptionQueueStatus.Active &&
                    s.IsActive &&
                    s.EndDate > DateTime.Now);

                if (activeSponsorship == null)
                {
                    _logger.LogWarning($"[SponsorAttribution] ❌ No active sponsored subscription found for user {userId.Value}");
                    
                    // Debug: Check if user has ANY subscription
                    var anySubscription = await _userSubscriptionRepository.GetAsync(s => s.UserId == userId.Value);
                    if (anySubscription != null)
                    {
                        _logger.LogInformation($"[SponsorAttribution] ℹ️ User has subscription but not active/sponsored:");
                        _logger.LogInformation($"   - IsSponsoredSubscription: {anySubscription.IsSponsoredSubscription}");
                        _logger.LogInformation($"   - QueueStatus: {anySubscription.QueueStatus}");
                        _logger.LogInformation($"   - IsActive: {anySubscription.IsActive}");
                        _logger.LogInformation($"   - EndDate: {anySubscription.EndDate} (Now: {DateTime.Now})");
                    }
                    else
                    {
                        _logger.LogInformation($"[SponsorAttribution] ℹ️ User has no subscription at all");
                    }
                    return;
                }

                _logger.LogInformation($"[SponsorAttribution] ✅ Found active sponsorship: ID={activeSponsorship.Id}, CodeId={activeSponsorship.SponsorshipCodeId}");

                // Get sponsor company ID from the code
                var code = await _sponsorshipCodeRepository.GetAsync(c => 
                    c.Id == activeSponsorship.SponsorshipCodeId);

                if (code == null)
                {
                    _logger.LogWarning($"[SponsorAttribution] ❌ Sponsorship code {activeSponsorship.SponsorshipCodeId} not found!");
                    return;
                }

                _logger.LogInformation($"[SponsorAttribution] ✅ Found sponsorship code: {code.Code}, SponsorId={code.SponsorId}");

                // Set both attribution fields
                analysis.ActiveSponsorshipId = activeSponsorship.Id;
                analysis.SponsorCompanyId = code.SponsorId;
                
                _logger.LogInformation($"[SponsorAttribution] ✅ Analysis {analysis.Id} attributed to sponsor {code.SponsorId} (subscription {activeSponsorship.Id})");
                
                // Invalidate sponsor dashboard cache
                InvalidateSponsorDashboardCache(code.SponsorId);
                _logger.LogInformation($"[SponsorAttribution] 🗑️ Dashboard cache invalidated for sponsor {code.SponsorId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SponsorAttribution] ❌ ERROR capturing sponsor for analysis: {ex.Message}");
                _logger.LogError($"[SponsorAttribution] Stack trace: {ex.StackTrace}");
                // Don't fail analysis completion if sponsor capture fails
            }
        }
    }
}