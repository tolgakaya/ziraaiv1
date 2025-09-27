using Business.Constants;
using Business.Services.Configuration;
using Business.Services.FileStorage;
using Business.Services.ImageProcessing;
using Core.CrossCuttingConcerns.Logging.Serilog;
using Core.Utilities.Results;
using Entities.Constants;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.PlantAnalysis
{
    public class PlantAnalysisService : IPlantAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IConfigurationService _configurationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<PlantAnalysisService> _logger;
        private readonly string _n8nWebhookUrl;
        private readonly bool _useImageUrl;

        public PlantAnalysisService(
            HttpClient httpClient, 
            IConfiguration configuration,
            IImageProcessingService imageProcessingService,
            IConfigurationService configurationService,
            IFileStorageService fileStorageService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PlantAnalysisService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _imageProcessingService = imageProcessingService;
            _configurationService = configurationService;
            _fileStorageService = fileStorageService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _n8nWebhookUrl = _configuration["N8N:WebhookUrl"];
            _useImageUrl = _configuration.GetValue<bool>("N8N:UseImageUrl", true);
            
            _logger.LogInformation("PlantAnalysisService initialized with N8N URL: {N8nUrl}, UseImageUrl: {UseImageUrl}, FileStorageService: {FileStorageType}", 
                _n8nWebhookUrl, _useImageUrl, _fileStorageService?.GetType().Name ?? "Unknown");
        }

        public async Task<PlantAnalysisResponseDto> SendToN8nWebhookAsync(PlantAnalysisRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            
            _logger.LogInformation("[PLANT_ANALYSIS_START] Starting synchronous plant analysis - CorrelationId: {CorrelationId}, FarmerId: {FarmerId}, CropType: {CropType}, SponsorId: {SponsorId}", 
                correlationId, request.FarmerId, request.CropType, request.SponsorId);

            try
            {
                // Validate required field
                if (string.IsNullOrEmpty(request.Image))
                {
                    _logger.LogWarning("[PLANT_ANALYSIS_VALIDATION_ERROR] Image field is required - CorrelationId: {CorrelationId}", correlationId);
                    throw new ArgumentException("Image field is required");
                }

                _logger.LogDebug("[PLANT_ANALYSIS_VALIDATION] Starting image validation - CorrelationId: {CorrelationId}", correlationId);
                
                // Validate that the uploaded file is actually an image
                ValidateImageFile(request.Image);
                
                _logger.LogInformation("[PLANT_ANALYSIS_IMAGE_PROCESSING] Starting image processing for AI optimization - CorrelationId: {CorrelationId}", correlationId);

                // Process image for AI (aggressive optimization)
                var imageProcessingStart = Stopwatch.StartNew();
                var processedImage = await ProcessImageForAIAsync(request.Image);
                imageProcessingStart.Stop();
                
                _logger.LogInformation("[PLANT_ANALYSIS_IMAGE_PROCESSED] Image processed in {ProcessingTime}ms - CorrelationId: {CorrelationId}, OriginalSize: {OriginalSize}, ProcessedSize: {ProcessedSize}", 
                    imageProcessingStart.ElapsedMilliseconds, correlationId, 
                    GetImageSize(request.Image), GetImageSize(processedImage));
                
                // Determine whether to use URL or base64
                object imagePayload;
                string imageUrl = null;
                
                if (_useImageUrl)
                {
                    _logger.LogInformation("[PLANT_ANALYSIS_URL_MODE] Using URL-based image processing for token optimization - CorrelationId: {CorrelationId}", correlationId);
                    
                    // Generate unique analysis ID
                    var analysisId = $"sync_analysis_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
                    
                    var uploadStart = Stopwatch.StartNew();
                    // Save image using file storage service and get URL
                    imageUrl = await _fileStorageService.UploadImageFromDataUriAsync(
                        processedImage, 
                        analysisId, 
                        "plant-images");
                    uploadStart.Stop();
                    
                    _logger.LogInformation("[PLANT_ANALYSIS_IMAGE_UPLOADED] Image uploaded to storage in {UploadTime}ms - CorrelationId: {CorrelationId}, ImageUrl: {ImageUrl}, StorageService: {StorageService}", 
                        uploadStart.ElapsedMilliseconds, correlationId, imageUrl, _fileStorageService?.GetType().Name);
                    
                    imagePayload = new { imageUrl = imageUrl };
                }
                else
                {
                    _logger.LogWarning("[PLANT_ANALYSIS_BASE64_MODE] Using base64 mode (not recommended for AI) - CorrelationId: {CorrelationId}", correlationId);
                    // Fallback to base64 (not recommended for AI)
                    imagePayload = new { image = processedImage };
                }

                // Prepare N8N webhook payload
                var n8nPayload = new
                {
                    // Use either imageUrl or image based on configuration
                    imageUrl = _useImageUrl ? imageUrl : null,
                    image = _useImageUrl ? null : processedImage,
                    farmer_id = request.FarmerId,
                    sponsor_id = request.SponsorId,
                    field_id = request.FieldId,
                    crop_type = request.CropType,
                    location = request.Location,
                    gps_coordinates = request.GpsCoordinates != null ? new
                    {
                        lat = request.GpsCoordinates.Lat,
                        lng = request.GpsCoordinates.Lng
                    } : null,
                    altitude = request.Altitude,
                    planting_date = request.PlantingDate?.ToString("yyyy-MM-dd"),
                    expected_harvest_date = request.ExpectedHarvestDate?.ToString("yyyy-MM-dd"),
                    last_fertilization = request.LastFertilization?.ToString("yyyy-MM-dd"),
                    last_irrigation = request.LastIrrigation?.ToString("yyyy-MM-dd"),
                    previous_treatments = request.PreviousTreatments,
                    soil_type = request.SoilType,
                    temperature = request.Temperature,
                    humidity = request.Humidity,
                    weather_conditions = request.WeatherConditions,
                    urgency_level = request.UrgencyLevel,
                    notes = request.Notes,
                    contact_info = request.ContactInfo != null ? new
                    {
                        phone = request.ContactInfo.Phone,
                        email = request.ContactInfo.Email
                    } : null,
                    additional_info = request.AdditionalInfo != null ? new
                    {
                        irrigation_method = request.AdditionalInfo.IrrigationMethod,
                        greenhouse = request.AdditionalInfo.Greenhouse,
                        organic_certified = request.AdditionalInfo.OrganicCertified
                    } : null
                };

                _logger.LogInformation("[PLANT_ANALYSIS_N8N_REQUEST] Sending payload to N8N webhook - CorrelationId: {CorrelationId}, PayloadSize: {PayloadSize} bytes, WebhookUrl: {WebhookUrl}", 
                    correlationId, Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(n8nPayload)), _n8nWebhookUrl);

                // Send JSON payload to N8N webhook
                var jsonContent = JsonConvert.SerializeObject(n8nPayload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var webhookCallStart = Stopwatch.StartNew();
                var response = await _httpClient.PostAsync(_n8nWebhookUrl, httpContent);
                webhookCallStart.Stop();

                _logger.LogInformation("[PLANT_ANALYSIS_N8N_RESPONSE] N8N webhook responded in {ResponseTime}ms - CorrelationId: {CorrelationId}, StatusCode: {StatusCode}, ContentLength: {ContentLength}", 
                    webhookCallStart.ElapsedMilliseconds, correlationId, response.StatusCode, response.Content.Headers.ContentLength ?? 0);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(responseContent))
                {
                    _logger.LogError("[PLANT_ANALYSIS_N8N_ERROR] N8N webhook returned empty response - CorrelationId: {CorrelationId}", correlationId);
                    throw new Exception("N8N webhook returned empty response");
                }

                _logger.LogDebug("[PLANT_ANALYSIS_N8N_RESPONSE_CONTENT] Raw N8N response received - CorrelationId: {CorrelationId}, ResponseLength: {ResponseLength}", 
                    correlationId, responseContent.Length);

                // Parse N8N response
                N8nAnalysisResponse analysisResult;
                
                try
                {
                    var deserializationStart = Stopwatch.StartNew();
                    analysisResult = JsonConvert.DeserializeObject<N8nAnalysisResponse>(responseContent);
                    deserializationStart.Stop();
                    
                    if (analysisResult == null)
                    {
                        _logger.LogError("[PLANT_ANALYSIS_DESERIALIZATION_ERROR] Failed to deserialize N8N response - CorrelationId: {CorrelationId}, ResponseContent: {ResponseContent}", 
                            correlationId, responseContent?.Substring(0, Math.Min(500, responseContent?.Length ?? 0)));
                        throw new Exception($"Failed to deserialize N8N response. Raw response: {responseContent}");
                    }

                    _logger.LogInformation("[PLANT_ANALYSIS_DESERIALIZATION_SUCCESS] N8N response deserialized successfully in {DeserializationTime}ms - CorrelationId: {CorrelationId}, AnalysisId: {AnalysisId}", 
                        deserializationStart.ElapsedMilliseconds, correlationId, analysisResult.AnalysisId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PLANT_ANALYSIS_DESERIALIZATION_EXCEPTION] Exception during N8N response parsing - CorrelationId: {CorrelationId}, ExceptionType: {ExceptionType}, ResponseContent: {ResponseContent}", 
                        correlationId, ex.GetType().Name, responseContent?.Substring(0, Math.Min(500, responseContent?.Length ?? 0)));
                    throw new Exception($"Failed to parse N8N response: {ex.Message}. Raw response: {responseContent}");
                }

                stopwatch.Stop();
                _logger.LogInformation("[PLANT_ANALYSIS_COMPLETED] Plant analysis completed successfully - CorrelationId: {CorrelationId}, TotalTime: {TotalTime}ms, AnalysisId: {AnalysisId}, VigorScore: {HealthScore}", 
                    correlationId, stopwatch.ElapsedMilliseconds, analysisResult.AnalysisId, analysisResult.HealthAssessment?.VigorScore ?? 0);

                return MapToPlantAnalysisResponseDto(analysisResult, responseContent);
            }
            catch (HttpRequestException httpEx)
            {
                stopwatch.Stop();
                _logger.LogError(httpEx, "[PLANT_ANALYSIS_HTTP_ERROR] HTTP error during N8N webhook call - CorrelationId: {CorrelationId}, ElapsedTime: {ElapsedTime}ms, Message: {ErrorMessage}", 
                    correlationId, stopwatch.ElapsedMilliseconds, httpEx.Message);
                throw new Exception($"N8N webhook HTTP error: {httpEx.Message}", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                stopwatch.Stop();
                _logger.LogError(timeoutEx, "[PLANT_ANALYSIS_TIMEOUT_ERROR] Timeout during N8N webhook call - CorrelationId: {CorrelationId}, ElapsedTime: {ElapsedTime}ms", 
                    correlationId, stopwatch.ElapsedMilliseconds);
                throw new Exception($"N8N webhook timeout: {timeoutEx.Message}", timeoutEx);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[PLANT_ANALYSIS_GENERAL_ERROR] Unexpected error during plant analysis - CorrelationId: {CorrelationId}, ElapsedTime: {ElapsedTime}ms, ExceptionType: {ExceptionType}, Message: {ErrorMessage}", 
                    correlationId, stopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);
                throw new Exception($"Plant analysis error: {ex.Message}", ex);
            }
        }

        private string GetImageSize(string dataUri)
        {
            try
            {
                if (string.IsNullOrEmpty(dataUri) || !dataUri.Contains(","))
                    return "Unknown";

                var base64Data = dataUri.Split(',')[1];
                var sizeBytes = (base64Data.Length * 3) / 4;
                
                if (sizeBytes < 1024)
                    return $"{sizeBytes} bytes";
                else if (sizeBytes < 1024 * 1024)
                    return $"{sizeBytes / 1024:F1} KB";
                else
                    return $"{sizeBytes / (1024 * 1024):F2} MB";
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        private PlantAnalysisResponseDto MapToPlantAnalysisResponseDto(N8nAnalysisResponse n8nResponse, string fullResponse)
        {
            if (n8nResponse == null)
            {
                throw new ArgumentNullException(nameof(n8nResponse), "N8N response cannot be null");
            }

            if (!n8nResponse.Success)
            {
                throw new Exception($"N8N analysis failed: {n8nResponse.ErrorMessage}");
            }

            var detailedAnalysis = new DetailedPlantAnalysisDto
            {
                PlantIdentification = MapPlantIdentification(n8nResponse.PlantIdentification),
                HealthAssessment = MapHealthAssessment(n8nResponse.HealthAssessment),
                NutrientStatus = MapNutrientStatus(n8nResponse.NutrientStatus),
                PestDisease = MapPestDisease(n8nResponse.PestDisease),
                EnvironmentalStress = MapEnvironmentalStress(n8nResponse.EnvironmentalStress),
                CrossFactorInsights = MapCrossFactorInsights(n8nResponse.CrossFactorInsights),
                Recommendations = MapRecommendations(n8nResponse.Recommendations),
                Summary = MapSummary(n8nResponse.Summary),
                FullResponseJson = fullResponse
            };

            return new PlantAnalysisResponseDto
            {
                AnalysisId = n8nResponse.AnalysisId,
                AnalysisDate = n8nResponse.Timestamp,
                Status = "Completed",
                FarmerId = n8nResponse.FarmerId,
                SponsorId = n8nResponse.SponsorId,
                Location = n8nResponse.Location,
                GpsCoordinates = n8nResponse.GpsCoordinates != null ? new GpsCoordinates
                {
                    Lat = n8nResponse.GpsCoordinates.Lat,
                    Lng = n8nResponse.GpsCoordinates.Lng
                } : null,
                Altitude = n8nResponse.Altitude,
                FieldId = n8nResponse.FieldId,
                CropType = n8nResponse.CropType,
                PlantingDate = ParseDate(n8nResponse.PlantingDate),
                ExpectedHarvestDate = ParseDate(n8nResponse.ExpectedHarvestDate),
                LastFertilization = ParseDate(n8nResponse.LastFertilization),
                LastIrrigation = ParseDate(n8nResponse.LastIrrigation),
                PreviousTreatments = n8nResponse.PreviousTreatments ?? new List<string>(),
                WeatherConditions = n8nResponse.WeatherConditions,
                Temperature = n8nResponse.Temperature,
                Humidity = n8nResponse.Humidity,
                SoilType = n8nResponse.SoilType,
                UrgencyLevel = n8nResponse.UrgencyLevel,
                Notes = n8nResponse.Notes,
                ContactInfo = MapContactInfoDto(n8nResponse.ContactInfo),
                AdditionalInfo = MapAdditionalInfoDto(n8nResponse.AdditionalInfo),
                ImageUrl = n8nResponse.ImageMetadata?.URL,
                
                // Analysis details
                PlantIdentification = MapPlantIdentificationDto(n8nResponse.PlantIdentification),
                HealthAssessment = MapHealthAssessmentDto(n8nResponse.HealthAssessment),
                NutrientStatus = MapNutrientStatusDto(n8nResponse.NutrientStatus),
                PestDisease = MapPestDiseaseDto(n8nResponse.PestDisease),
                EnvironmentalStress = MapEnvironmentalStressDto(n8nResponse.EnvironmentalStress),
                Recommendations = MapRecommendationsDto(n8nResponse.Recommendations),
                CrossFactorInsights = MapCrossFactorInsightsDto(n8nResponse.CrossFactorInsights),
                Summary = MapSummaryDto(n8nResponse.Summary),

                // Additional Analysis Data
                RiskAssessment = MapRiskAssessmentDto(n8nResponse.RiskAssessment),
                ConfidenceNotes = MapConfidenceNotesDto(n8nResponse.ConfidenceNotes),
                FarmerFriendlySummary = n8nResponse.FarmerFriendlySummary,

                // Image Metadata (ProcessingMetadata and TokenUsage are saved to DB but not returned in API response)
                ImageMetadata = MapImageMetadataDto(n8nResponse.ImageMetadata),

                // Full detailed analysis
                DetailedAnalysis = detailedAnalysis,
                
                // Legacy fields for backward compatibility
                PlantType = n8nResponse.PlantIdentification?.Species ?? "Unknown",
                GrowthStage = n8nResponse.PlantIdentification?.GrowthStage ?? "Unknown",
                OverallAnalysis = n8nResponse.Summary?.PrimaryConcern ?? "No analysis available",
                ElementDeficiencies = MapLegacyElementDeficiencies(n8nResponse.NutrientStatus),
                Diseases = MapLegacyDiseases(n8nResponse.PestDisease?.DiseasesDetected),
                Pests = MapLegacyPests(n8nResponse.PestDisease?.PestsDetected)
            };
        }
        
        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;
            
            if (DateTime.TryParse(dateString, out var date))
            {
                // Ensure the DateTime is in UTC for PostgreSQL timestamptz compatibility
                return date.Kind == DateTimeKind.Utc ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc);
            }
            
            return null;
        }

        private PlantIdentificationDto MapPlantIdentificationDto(N8nPlantIdentification source)
        {
            if (source == null) return new PlantIdentificationDto();
            
            return new PlantIdentificationDto
            {
                Species = source.Species,
                Variety = source.Variety,
                GrowthStage = source.GrowthStage,
                Confidence = source.Confidence,
                IdentifyingFeatures = source.IdentifyingFeatures ?? new List<string>(),
                VisibleParts = source.VisibleParts ?? new List<string>()
            };
        }
        
        private PlantIdentificationDto MapPlantIdentification(N8nPlantIdentification source)
        {
            if (source == null) return new PlantIdentificationDto();
            
            return new PlantIdentificationDto
            {
                Species = source.Species,
                Variety = source.Variety,
                GrowthStage = source.GrowthStage,
                Confidence = source.Confidence,
                IdentifyingFeatures = source.IdentifyingFeatures ?? new List<string>(),
                VisibleParts = source.VisibleParts ?? new List<string>()
            };
        }

        private HealthAssessmentDto MapHealthAssessmentDto(N8nHealthAssessment source)
        {
            if (source == null) return new HealthAssessmentDto();
            
            return new HealthAssessmentDto
            {
                VigorScore = source.VigorScore,
                LeafColor = source.LeafColor,
                LeafTexture = source.LeafTexture,
                GrowthPattern = source.GrowthPattern,
                StructuralIntegrity = source.StructuralIntegrity,
                StressIndicators = source.StressIndicators ?? new List<string>(),
                DiseaseSymptoms = source.DiseaseSymptoms ?? new List<string>(),
                Severity = source.Severity
            };
        }
        
        private HealthAssessmentDto MapHealthAssessment(N8nHealthAssessment source)
        {
            if (source == null) return new HealthAssessmentDto();
            
            return new HealthAssessmentDto
            {
                VigorScore = source.VigorScore,
                LeafColor = source.LeafColor,
                LeafTexture = source.LeafTexture,
                GrowthPattern = source.GrowthPattern,
                StructuralIntegrity = source.StructuralIntegrity,
                StressIndicators = source.StressIndicators ?? new List<string>(),
                DiseaseSymptoms = source.DiseaseSymptoms ?? new List<string>(),
                Severity = source.Severity
            };
        }

        private NutrientStatusDto MapNutrientStatusDto(N8nNutrientStatus source)
        {
            if (source == null) return new NutrientStatusDto();

            return new NutrientStatusDto
            {
                Nitrogen = source.Nitrogen,
                Phosphorus = source.Phosphorus,
                Potassium = source.Potassium,
                Calcium = source.Calcium,
                Magnesium = source.Magnesium,
                Iron = source.Iron,
                Sulfur = source.Sulfur,
                Zinc = source.Zinc,
                Manganese = source.Manganese,
                Boron = source.Boron,
                Copper = source.Copper,
                Molybdenum = source.Molybdenum,
                Chlorine = source.Chlorine,
                Nickel = source.Nickel,
                PrimaryDeficiency = source.PrimaryDeficiency,
                SecondaryDeficiencies = source.SecondaryDeficiencies ?? new List<string>(),
                Severity = source.Severity
            };
        }
        
        private NutrientStatusDto MapNutrientStatus(N8nNutrientStatus source)
        {
            if (source == null) return new NutrientStatusDto();

            return new NutrientStatusDto
            {
                Nitrogen = source.Nitrogen,
                Phosphorus = source.Phosphorus,
                Potassium = source.Potassium,
                Calcium = source.Calcium,
                Magnesium = source.Magnesium,
                Iron = source.Iron,
                Sulfur = source.Sulfur,
                Zinc = source.Zinc,
                Manganese = source.Manganese,
                Boron = source.Boron,
                Copper = source.Copper,
                Molybdenum = source.Molybdenum,
                Chlorine = source.Chlorine,
                Nickel = source.Nickel,
                PrimaryDeficiency = source.PrimaryDeficiency,
                SecondaryDeficiencies = source.SecondaryDeficiencies ?? new List<string>(),
                Severity = source.Severity
            };
        }

        private PestDiseaseDto MapPestDiseaseDto(N8nPestDisease source)
        {
            if (source == null) return new PestDiseaseDto();

            return new PestDiseaseDto
            {
                PestsDetected = source.PestsDetected?.Select(p => p.Type).ToList() ?? new List<string>(),
                DiseasesDetected = source.DiseasesDetected?.Select(d => d.Type).ToList() ?? new List<string>(),
                DamagePattern = source.DamagePattern,
                AffectedAreaPercentage = source.AffectedAreaPercentage,
                SpreadRisk = source.SpreadRisk,
                PrimaryIssue = source.PrimaryIssue
            };
        }
        
        private PestDiseaseDto MapPestDisease(N8nPestDisease source)
        {
            if (source == null) return new PestDiseaseDto();

            return new PestDiseaseDto
            {
                PestsDetected = source.PestsDetected?.Select(p => p.Type).ToList() ?? new List<string>(),
                DiseasesDetected = source.DiseasesDetected?.Select(d => d.Type).ToList() ?? new List<string>(),
                DamagePattern = source.DamagePattern,
                AffectedAreaPercentage = source.AffectedAreaPercentage,
                SpreadRisk = source.SpreadRisk,
                PrimaryIssue = source.PrimaryIssue
            };
        }

        private EnvironmentalStressDto MapEnvironmentalStressDto(N8nEnvironmentalStress source)
        {
            if (source == null) return new EnvironmentalStressDto();

            return new EnvironmentalStressDto
            {
                WaterStatus = source.WaterStatus,
                TemperatureStress = source.TemperatureStress,
                LightStress = source.LightStress,
                PhysicalDamage = source.PhysicalDamage,
                ChemicalDamage = source.ChemicalDamage,
                SoilIndicators = source.SoilIndicators,
                PhysiologicalDisorders = MapPhysiologicalDisordersDto(source.PhysiologicalDisorders),
                SoilHealthIndicators = MapSoilHealthIndicatorsDto(source.SoilHealthIndicators),
                PrimaryStressor = source.PrimaryStressor
            };
        }
        
        private EnvironmentalStressDto MapEnvironmentalStress(N8nEnvironmentalStress source)
        {
            if (source == null) return new EnvironmentalStressDto();

            return new EnvironmentalStressDto
            {
                WaterStatus = source.WaterStatus,
                TemperatureStress = source.TemperatureStress,
                LightStress = source.LightStress,
                PhysicalDamage = source.PhysicalDamage,
                ChemicalDamage = source.ChemicalDamage,
                SoilIndicators = source.SoilIndicators,
                PhysiologicalDisorders = MapPhysiologicalDisordersDto(source.PhysiologicalDisorders),
                SoilHealthIndicators = MapSoilHealthIndicatorsDto(source.SoilHealthIndicators),
                PrimaryStressor = source.PrimaryStressor
            };
        }

        private List<CrossFactorInsightDto> MapCrossFactorInsightsDto(List<N8nCrossFactorInsight> source)
        {
            if (source == null) return new List<CrossFactorInsightDto>();
            
            return source.Select(i => new CrossFactorInsightDto
            {
                Insight = i.Insight,
                Confidence = i.Confidence,
                AffectedAspects = i.AffectedAspects ?? new List<string>(),
                ImpactLevel = i.ImpactLevel
            }).ToList();
        }
        
        private List<CrossFactorInsightDto> MapCrossFactorInsights(List<N8nCrossFactorInsight> source)
        {
            if (source == null) return new List<CrossFactorInsightDto>();
            
            return source.Select(i => new CrossFactorInsightDto
            {
                Insight = i.Insight,
                Confidence = i.Confidence,
                AffectedAspects = i.AffectedAspects ?? new List<string>()
            }).ToList();
        }

        private RecommendationsDto MapRecommendationsDto(N8nRecommendations source)
        {
            if (source == null) return new RecommendationsDto();

            return new RecommendationsDto
            {
                Immediate = source.Immediate?.Select(r => new RecommendationDto
                {
                    Action = r.Action,
                    Details = r.Details,
                    Timeline = r.Timeline,
                    Priority = r.Priority
                }).ToList() ?? new List<RecommendationDto>(),

                ShortTerm = source.ShortTerm?.Select(r => new RecommendationDto
                {
                    Action = r.Action,
                    Details = r.Details,
                    Timeline = r.Timeline,
                    Priority = r.Priority
                }).ToList() ?? new List<RecommendationDto>(),

                Preventive = source.Preventive?.Select(r => new RecommendationDto
                {
                    Action = r.Action,
                    Details = r.Details,
                    Timeline = r.Timeline,
                    Priority = r.Priority
                }).ToList() ?? new List<RecommendationDto>(),

                Monitoring = source.Monitoring?.Select(m => new MonitoringItemDto
                {
                    Parameter = m.Parameter,
                    Frequency = m.Frequency,
                    Threshold = m.Threshold
                }).ToList() ?? new List<MonitoringItemDto>(),

                ResourceEstimation = MapResourceEstimationDto(source.ResourceEstimation),
                LocalizedRecommendations = MapLocalizedRecommendationsDto(source.LocalizedRecommendations)
            };
        }
        
        private RecommendationsDto MapRecommendations(N8nRecommendations source)
        {
            if (source == null) return new RecommendationsDto();

            return new RecommendationsDto
            {
                Immediate = source.Immediate?.Select(r => new RecommendationDto
                {
                    Action = r.Action,
                    Details = r.Details,
                    Timeline = r.Timeline,
                    Priority = r.Priority
                }).ToList() ?? new List<RecommendationDto>(),

                ShortTerm = source.ShortTerm?.Select(r => new RecommendationDto
                {
                    Action = r.Action,
                    Details = r.Details,
                    Timeline = r.Timeline,
                    Priority = r.Priority
                }).ToList() ?? new List<RecommendationDto>(),

                Preventive = source.Preventive?.Select(r => new RecommendationDto
                {
                    Action = r.Action,
                    Details = r.Details,
                    Timeline = r.Timeline,
                    Priority = r.Priority
                }).ToList() ?? new List<RecommendationDto>(),

                Monitoring = source.Monitoring?.Select(m => new MonitoringItemDto
                {
                    Parameter = m.Parameter,
                    Frequency = m.Frequency,
                    Threshold = m.Threshold
                }).ToList() ?? new List<MonitoringItemDto>(),

                ResourceEstimation = MapResourceEstimationDto(source.ResourceEstimation),
                LocalizedRecommendations = MapLocalizedRecommendationsDto(source.LocalizedRecommendations)
            };
        }

        private SummaryDto MapSummaryDto(N8nSummary source)
        {
            if (source == null) return new SummaryDto();
            
            return new SummaryDto
            {
                OverallHealthScore = source.OverallHealthScore,
                PrimaryConcern = source.PrimaryConcern,
                SecondaryConcerns = source.SecondaryConcerns ?? new List<string>(),
                CriticalIssuesCount = source.CriticalIssuesCount,
                ConfidenceLevel = source.ConfidenceLevel,
                Prognosis = source.Prognosis,
                EstimatedYieldImpact = source.EstimatedYieldImpact
            };
        }
        
        private ProcessingMetadataDto MapProcessingMetadataDto(N8nProcessingMetadata source)
        {
            if (source == null) return new ProcessingMetadataDto();
            
            return new ProcessingMetadataDto
            {
                ParseSuccess = source.ParseSuccess,
                ProcessingTimestamp = source.ProcessingTimestamp,
                AiModel = source.AiModel,
                WorkflowVersion = source.WorkflowVersion
            };
        }
        
        private TokenUsageDto MapTokenUsageDto(N8nTokenUsage source)
        {
            if (source == null) return new TokenUsageDto();
            
            return new TokenUsageDto
            {
                Summary = source.Summary != null ? new TokenSummaryDto
                {
                    Model = source.Summary.Model,
                    AnalysisId = source.Summary.AnalysisId,
                    Timestamp = source.Summary.Timestamp,
                    TotalTokens = source.Summary.TotalTokens,
                    TotalCostUsd = source.Summary.TotalCostUsd,
                    TotalCostTry = source.Summary.TotalCostTry,
                    ImageSizeKb = source.Summary.ImageSizeKb
                } : null,
                TokenBreakdown = source.TokenBreakdown != null ? new TokenBreakdownDto
                {
                    Input = source.TokenBreakdown.Input != null ? new TokenInputDto
                    {
                        SystemPrompt = source.TokenBreakdown.Input.SystemPrompt,
                        ContextData = source.TokenBreakdown.Input.ContextData,
                        Image = source.TokenBreakdown.Input.Image,
                        Total = source.TokenBreakdown.Input.Total
                    } : null,
                    Output = source.TokenBreakdown.Output != null ? new TokenOutputDto
                    {
                        Response = source.TokenBreakdown.Output.Response,
                        Total = source.TokenBreakdown.Output.Total
                    } : null,
                    GrandTotal = source.TokenBreakdown.GrandTotal
                } : null,
                CostBreakdown = source.CostBreakdown != null ? new CostBreakdownDto
                {
                    InputCostUsd = source.CostBreakdown.InputCostUsd,
                    OutputCostUsd = source.CostBreakdown.OutputCostUsd,
                    TotalCostUsd = source.CostBreakdown.TotalCostUsd,
                    TotalCostTry = source.CostBreakdown.TotalCostTry,
                    ExchangeRate = source.CostBreakdown.ExchangeRate
                } : null
            };
        }
        
        private SummaryDto MapSummary(N8nSummary source)
        {
            if (source == null) return new SummaryDto();
            
            return new SummaryDto
            {
                OverallHealthScore = source.OverallHealthScore,
                PrimaryConcern = source.PrimaryConcern,
                CriticalIssuesCount = source.CriticalIssuesCount,
                ConfidenceLevel = source.ConfidenceLevel
            };
        }

        // Legacy mapping methods for backward compatibility
        private string BuildLegacyRecommendations(N8nRecommendations recommendations)
        {
            if (recommendations == null) return "No recommendations available";
            
            var allRecommendations = new List<string>();
            
            if (recommendations.Immediate?.Any() == true)
                allRecommendations.AddRange(recommendations.Immediate.Select(r => $"Immediate: {r.Action}"));
                
            if (recommendations.ShortTerm?.Any() == true)
                allRecommendations.AddRange(recommendations.ShortTerm.Select(r => $"Short-term: {r.Action}"));
                
            if (recommendations.Preventive?.Any() == true)
                allRecommendations.AddRange(recommendations.Preventive.Select(r => $"Preventive: {r.Action}"));
            
            return string.Join("; ", allRecommendations);
        }

        private List<ElementDeficiencyDto> MapLegacyElementDeficiencies(N8nNutrientStatus nutrients)
        {
            if (nutrients?.PrimaryDeficiency == null || nutrients.PrimaryDeficiency == "none") 
                return new List<ElementDeficiencyDto>();
            
            return new List<ElementDeficiencyDto>
            {
                new ElementDeficiencyDto
                {
                    Element = nutrients.PrimaryDeficiency,
                    Severity = nutrients.Severity ?? "Unknown",
                    Description = $"Primary nutrient deficiency detected",
                    Treatment = "See detailed recommendations"
                }
            };
        }

        private List<DiseaseDto> MapLegacyDiseases(List<N8nDiseaseDetected> diseases)
        {
            if (diseases == null) return new List<DiseaseDto>();

            return diseases.Select(d => new DiseaseDto
            {
                Name = d.Type,
                Type = d.Category,
                Severity = d.Severity,
                Description = $"Disease: {d.Type} (Confidence: {d.Confidence:P0})",
                Treatment = "See detailed recommendations"
            }).ToList();
        }

        private List<PestDto> MapLegacyPests(List<N8nPestDetected> pests)
        {
            if (pests == null) return new List<PestDto>();

            return pests.Select(p => new PestDto
            {
                Name = p.Type,
                Type = p.Category,
                Severity = p.Severity,
                Description = $"Pest: {p.Type} (Confidence: {p.Confidence:P0})",
                Treatment = "See detailed recommendations"
            }).ToList();
        }

        private RiskAssessmentDto MapRiskAssessmentDto(N8nRiskAssessment source)
        {
            if (source == null) return null;

            return new RiskAssessmentDto
            {
                YieldLossProbability = source.YieldLossProbability,
                TimelineToWorsen = source.TimelineToWorsen,
                SpreadPotential = source.SpreadPotential
            };
        }

        private List<ConfidenceNoteDto> MapConfidenceNotesDto(List<N8nConfidenceNote> source)
        {
            if (source == null) return new List<ConfidenceNoteDto>();

            return source.Select(note => new ConfidenceNoteDto
            {
                Aspect = note.Aspect,
                Confidence = note.Confidence,
                Reason = note.Reason
            }).ToList();
        }

        private ImageMetadataDto MapImageMetadataDto(N8nImageMetadata source)
        {
            if (source == null) return null;

            return new ImageMetadataDto
            {
                Source = source.Source,
                ImageUrl = source.URL,
                HasImageExtension = source.HasImageExtension,
                UploadTimestamp = source.UploadTimestamp
            };
        }

        private List<PhysiologicalDisorderDto> MapPhysiologicalDisordersDto(List<N8nPhysiologicalDisorder> source)
        {
            if (source == null) return new List<PhysiologicalDisorderDto>();

            return source.Select(disorder => new PhysiologicalDisorderDto
            {
                Type = disorder.Type,
                Severity = disorder.Severity,
                Notes = disorder.Notes
            }).ToList();
        }

        private SoilHealthIndicatorsDto MapSoilHealthIndicatorsDto(N8nSoilHealthIndicators source)
        {
            if (source == null) return null;

            return new SoilHealthIndicatorsDto
            {
                Salinity = source.Salinity,
                PhIssue = source.PhIssue,
                OrganicMatter = source.OrganicMatter
            };
        }

        private ResourceEstimationDto MapResourceEstimationDto(N8nResourceEstimation source)
        {
            if (source == null) return null;

            return new ResourceEstimationDto
            {
                WaterRequiredLiters = source.WaterRequiredLiters,
                FertilizerCostEstimateUsd = source.FertilizerCostEstimateUsd,
                LaborHoursEstimate = source.LaborHoursEstimate
            };
        }

        private LocalizedRecommendationsDto MapLocalizedRecommendationsDto(N8nLocalizedRecommendations source)
        {
            if (source == null) return null;

            return new LocalizedRecommendationsDto
            {
                Region = source.Region,
                PreferredPractices = source.PreferredPractices ?? new List<string>(),
                RestrictedMethods = source.RestrictedMethods ?? new List<string>()
            };
        }

        private ContactInfoDto MapContactInfoDto(N8nContactInfo source)
        {
            if (source == null) return null;

            return new ContactInfoDto
            {
                Phone = source.Phone,
                Email = source.Email
            };
        }

        private AdditionalInfoDto MapAdditionalInfoDto(N8nAdditionalInfo source)
        {
            if (source == null) return null;

            return new AdditionalInfoDto
            {
                IrrigationMethod = source.IrrigationMethod,
                Greenhouse = source.Greenhouse,
                OrganicCertified = source.OrganicCertified
            };
        }

        public async Task<string> SaveImageFileAsync(string imageBase64, int analysisId)
        {
            try
            {
                // Validate that the file is actually an image
                ValidateImageFile(imageBase64);
                
                // Detect image format from data URI
                var imageInfo = GetImageInfoFromDataUri(imageBase64);
                
                // Convert base64 to byte array
                var base64Data = imageBase64;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Split(',')[1];
                }
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                // Double-check with file header validation
                var detectedFormat = ValidateAndDetectImageFormat(imageBytes);
                if (detectedFormat.Extension != imageInfo.Extension)
                {
                    // Trust the binary analysis over MIME type
                    imageInfo = detectedFormat;
                }

                // Use file storage service instead of direct file system access
                var uniqueAnalysisId = $"legacy_{analysisId}_{DateTime.Now:yyyyMMdd_HHmmss}";
                return await _fileStorageService.UploadImageFromDataUriAsync(imageBase64, uniqueAnalysisId, "plant-images");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving image file: {ex.Message}", ex);
            }
        }

        private ImageInfo GetImageInfoFromDataUri(string dataUri)
        {
            if (string.IsNullOrEmpty(dataUri))
                return new ImageInfo { Extension = ".jpg", MimeType = "image/jpeg" };

            var lowerDataUri = dataUri.ToLowerInvariant();

            // Check for different image formats
            if (lowerDataUri.Contains("data:image/jpeg") || lowerDataUri.Contains("data:image/jpg"))
                return new ImageInfo { Extension = ".jpg", MimeType = "image/jpeg" };
            
            if (lowerDataUri.Contains("data:image/png"))
                return new ImageInfo { Extension = ".png", MimeType = "image/png" };
            
            if (lowerDataUri.Contains("data:image/gif"))
                return new ImageInfo { Extension = ".gif", MimeType = "image/gif" };
            
            if (lowerDataUri.Contains("data:image/webp"))
                return new ImageInfo { Extension = ".webp", MimeType = "image/webp" };
            
            if (lowerDataUri.Contains("data:image/bmp"))
                return new ImageInfo { Extension = ".bmp", MimeType = "image/bmp" };
            
            if (lowerDataUri.Contains("data:image/svg"))
                return new ImageInfo { Extension = ".svg", MimeType = "image/svg+xml" };
            
            if (lowerDataUri.Contains("data:image/tiff") || lowerDataUri.Contains("data:image/tif"))
                return new ImageInfo { Extension = ".tiff", MimeType = "image/tiff" };

            // Default to JPEG if format not detected
            return new ImageInfo { Extension = ".jpg", MimeType = "image/jpeg" };
        }

        private void ValidateImageFile(string dataUri)
        {
            if (string.IsNullOrEmpty(dataUri))
                throw new ArgumentException("Image data is required");

            // Check if it's a valid data URI
            if (!dataUri.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid image format. Only image files are allowed.");

            // Check for supported MIME types
            var lowerDataUri = dataUri.ToLowerInvariant();
            var supportedMimeTypes = new[]
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

            bool isSupported = supportedMimeTypes.Any(mime => lowerDataUri.StartsWith(mime));
            if (!isSupported)
            {
                throw new ArgumentException("Unsupported image format. Supported formats: JPEG, PNG, GIF, WebP, BMP, SVG, TIFF");
            }

            // Validate base64 content
            try
            {
                var base64Data = dataUri.Contains(",") ? dataUri.Split(',')[1] : dataUri;
                var imageBytes = Convert.FromBase64String(base64Data);
                
                if (imageBytes.Length == 0)
                    throw new ArgumentException("Image data is empty");
                
                if (imageBytes.Length > 50 * 1024 * 1024) // 50MB limit
                    throw new ArgumentException("Image file too large. Maximum size is 50MB");
                
                if (imageBytes.Length < 100) // Minimum viable image size
                    throw new ArgumentException("Image file too small. Minimum size is 100 bytes");
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid base64 image data");
            }
        }

        private ImageInfo ValidateAndDetectImageFormat(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 4)
                throw new ArgumentException("Invalid image data");

            // Check file signatures (magic bytes)
            var fileSignature = imageBytes.Take(20).ToArray(); // Get first 20 bytes for analysis

            // JPEG: FF D8 FF
            if (fileSignature.Length >= 3 && 
                fileSignature[0] == 0xFF && fileSignature[1] == 0xD8 && fileSignature[2] == 0xFF)
            {
                return new ImageInfo { Extension = ".jpg", MimeType = "image/jpeg" };
            }

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (fileSignature.Length >= 8 && 
                fileSignature[0] == 0x89 && fileSignature[1] == 0x50 && 
                fileSignature[2] == 0x4E && fileSignature[3] == 0x47 &&
                fileSignature[4] == 0x0D && fileSignature[5] == 0x0A && 
                fileSignature[6] == 0x1A && fileSignature[7] == 0x0A)
            {
                return new ImageInfo { Extension = ".png", MimeType = "image/png" };
            }

            // GIF: 47 49 46 38 (GIF8)
            if (fileSignature.Length >= 4 && 
                fileSignature[0] == 0x47 && fileSignature[1] == 0x49 && 
                fileSignature[2] == 0x46 && fileSignature[3] == 0x38)
            {
                return new ImageInfo { Extension = ".gif", MimeType = "image/gif" };
            }

            // WebP: 52 49 46 46 ... 57 45 42 50 (RIFF...WEBP)
            if (fileSignature.Length >= 12 && 
                fileSignature[0] == 0x52 && fileSignature[1] == 0x49 && 
                fileSignature[2] == 0x46 && fileSignature[3] == 0x46 &&
                fileSignature[8] == 0x57 && fileSignature[9] == 0x45 && 
                fileSignature[10] == 0x42 && fileSignature[11] == 0x50)
            {
                return new ImageInfo { Extension = ".webp", MimeType = "image/webp" };
            }

            // BMP: 42 4D
            if (fileSignature.Length >= 2 && 
                fileSignature[0] == 0x42 && fileSignature[1] == 0x4D)
            {
                return new ImageInfo { Extension = ".bmp", MimeType = "image/bmp" };
            }

            // TIFF: 49 49 2A 00 (little endian) or 4D 4D 00 2A (big endian)
            if (fileSignature.Length >= 4 && 
                ((fileSignature[0] == 0x49 && fileSignature[1] == 0x49 && 
                  fileSignature[2] == 0x2A && fileSignature[3] == 0x00) ||
                 (fileSignature[0] == 0x4D && fileSignature[1] == 0x4D && 
                  fileSignature[2] == 0x00 && fileSignature[3] == 0x2A)))
            {
                return new ImageInfo { Extension = ".tiff", MimeType = "image/tiff" };
            }

            // SVG: Check for XML declaration and <svg tag
            var textContent = System.Text.Encoding.UTF8.GetString(imageBytes.Take(1000).ToArray()).ToLowerInvariant();
            if (textContent.Contains("<svg") || textContent.Contains("<?xml"))
            {
                return new ImageInfo { Extension = ".svg", MimeType = "image/svg+xml" };
            }

            throw new ArgumentException($"File is not a valid image. Detected file signature: {BitConverter.ToString(fileSignature.Take(8).ToArray())}");
        }

        private class ImageInfo
        {
            public string Extension { get; set; }
            public string MimeType { get; set; }
        }

        /// <summary>
        /// Intelligent Image Processing:
        /// 1. Decode base64 image
        /// 2. Check configuration limits
        /// 3. Auto-resize if needed and enabled
        /// 4. Validate final size
        /// 5. Return optimized base64 data URI
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
                // 1. Extract image bytes from data URI
                var base64Data = originalDataUri.Contains(",") ? originalDataUri.Split(',')[1] : originalDataUri;
                var imageBytes = Convert.FromBase64String(base64Data);
                
                // Get original format info
                var originalFormat = GetImageInfoFromDataUri(originalDataUri);
                
                // 2. Get configuration limits
                var maxSizeMB = await _configurationService.GetDecimalValueAsync(
                    ConfigurationKeys.ImageProcessing.MaxImageSizeMB, 50.0m);
                var maxSizeBytes = (long)(maxSizeMB * 1024 * 1024);
                
                var enableAutoResize = await _configurationService.GetBoolValueAsync(
                    ConfigurationKeys.ImageProcessing.EnableAutoResize, true);

                // 3. Check if processing is needed
                if (imageBytes.Length <= maxSizeBytes)
                {
                    // Image is already within limits, return as-is
                    return originalDataUri;
                }

                // 4. If auto-resize is disabled, throw error
                if (!enableAutoResize)
                {
                    var currentSizeMB = Math.Round((decimal)imageBytes.Length / (1024 * 1024), 2);
                    throw new InvalidOperationException(
                        $"Image too large ({currentSizeMB}MB). Maximum allowed: {maxSizeMB}MB. " +
                        $"Auto-resize is disabled.");
                }

                // 5. Try auto-resize
                var resizedBytes = await _imageProcessingService.ResizeImageIfNeededAsync(imageBytes);
                
                // 6. Check if resize was successful in reducing size
                if (resizedBytes.Length > maxSizeBytes)
                {
                    var originalSizeMB = Math.Round((decimal)imageBytes.Length / (1024 * 1024), 2);
                    var resizedSizeMB = Math.Round((decimal)resizedBytes.Length / (1024 * 1024), 2);
                    
                    throw new InvalidOperationException(
                        $"Image too large even after auto-resize. " +
                        $"Original: {originalSizeMB}MB, Resized: {resizedSizeMB}MB, " +
                        $"Maximum: {maxSizeMB}MB. Please use a smaller image or different format.");
                }

                // 7. Detect format of resized image (may have changed during processing)
                var processedFormat = ValidateAndDetectImageFormat(resizedBytes);
                
                // 8. Convert back to base64 data URI
                var processedBase64 = Convert.ToBase64String(resizedBytes);
                var processedDataUri = $"data:{processedFormat.MimeType};base64,{processedBase64}";

                // 9. Log processing info (optional)
                var originalSizeKB = Math.Round((decimal)imageBytes.Length / 1024, 1);
                var processedSizeKB = Math.Round((decimal)resizedBytes.Length / 1024, 1);
                var compressionRatio = Math.Round((1 - (decimal)resizedBytes.Length / imageBytes.Length) * 100, 1);
                
                // Could add logging here if needed
                // _logger.LogInformation($"Image processed: {originalSizeKB}KB -> {processedSizeKB}KB ({compressionRatio}% reduction)");

                return processedDataUri;
            }
            catch (Exception ex)
            {
                // Re-throw with more context
                throw new InvalidOperationException($"Image processing failed: {ex.Message}", ex);
            }
        }
    }

    public class N8nAnalysisResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("analysis_id")]
        public string AnalysisId { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonProperty("farmer_id")]
        public string FarmerId { get; set; }
        
        [JsonProperty("sponsor_id")]
        public string SponsorId { get; set; }
        
        [JsonProperty("location")]
        public string Location { get; set; }
        
        [JsonProperty("gps_coordinates")]
        public N8nGpsCoordinates GpsCoordinates { get; set; }
        
        [JsonProperty("altitude")]
        public int? Altitude { get; set; }
        
        [JsonProperty("field_id")]
        public string FieldId { get; set; }
        
        [JsonProperty("crop_type")]
        public string CropType { get; set; }
        
        [JsonProperty("planting_date")]
        public string PlantingDate { get; set; }
        
        [JsonProperty("expected_harvest_date")]
        public string ExpectedHarvestDate { get; set; }
        
        [JsonProperty("last_fertilization")]
        public string LastFertilization { get; set; }
        
        [JsonProperty("last_irrigation")]
        public string LastIrrigation { get; set; }
        
        [JsonProperty("previous_treatments")]
        public List<string> PreviousTreatments { get; set; }
        
        [JsonProperty("weather_conditions")]
        public string WeatherConditions { get; set; }
        
        [JsonProperty("temperature")]
        public decimal? Temperature { get; set; }
        
        [JsonProperty("humidity")]
        public decimal? Humidity { get; set; }
        
        [JsonProperty("soil_type")]
        public string SoilType { get; set; }
        
        [JsonProperty("urgency_level")]
        public string UrgencyLevel { get; set; }
        
        [JsonProperty("notes")]
        public string Notes { get; set; }
        
        [JsonProperty("contact_info")]
        public N8nContactInfo ContactInfo { get; set; }
        
        [JsonProperty("additional_info")]
        public N8nAdditionalInfo AdditionalInfo { get; set; }
        
        [JsonProperty("plant_identification")]
        public N8nPlantIdentification PlantIdentification { get; set; }
        
        [JsonProperty("health_assessment")]
        public N8nHealthAssessment HealthAssessment { get; set; }
        
        [JsonProperty("nutrient_status")]
        public N8nNutrientStatus NutrientStatus { get; set; }
        
        [JsonProperty("pest_disease")]
        public N8nPestDisease PestDisease { get; set; }
        
        [JsonProperty("environmental_stress")]
        public N8nEnvironmentalStress EnvironmentalStress { get; set; }
        
        [JsonProperty("cross_factor_insights")]
        public List<N8nCrossFactorInsight> CrossFactorInsights { get; set; }
        
        [JsonProperty("recommendations")]
        public N8nRecommendations Recommendations { get; set; }
        
        [JsonProperty("summary")]
        public N8nSummary Summary { get; set; }
        
        [JsonProperty("image_metadata")]
        public N8nImageMetadata ImageMetadata { get; set; }
        
        [JsonProperty("processing_metadata")]
        public N8nProcessingMetadata ProcessingMetadata { get; set; }
        
        [JsonProperty("token_usage")]
        public N8nTokenUsage TokenUsage { get; set; }
        
        [JsonProperty("error")]
        public bool Error { get; set; }
        
        [JsonProperty("error_message")]
        public string ErrorMessage { get; set; }
        
        [JsonProperty("error_type")]
        public string ErrorType { get; set; }

        [JsonProperty("risk_assessment")]
        public N8nRiskAssessment RiskAssessment { get; set; }

        [JsonProperty("confidence_notes")]
        public List<N8nConfidenceNote> ConfidenceNotes { get; set; }

        [JsonProperty("farmer_friendly_summary")]
        public string FarmerFriendlySummary { get; set; }
    }
    
    public class N8nGpsCoordinates
    {
        [JsonProperty("lat")]
        public decimal Lat { get; set; }
        
        [JsonProperty("lng")]
        public decimal Lng { get; set; }
    }
    
    public class N8nContactInfo
    {
        [JsonProperty("phone")]
        public string Phone { get; set; }
        
        [JsonProperty("email")]
        public string Email { get; set; }
    }
    
    public class N8nAdditionalInfo
    {
        [JsonProperty("irrigation_method")]
        public string IrrigationMethod { get; set; }
        
        [JsonProperty("greenhouse")]
        public bool? Greenhouse { get; set; }
        
        [JsonProperty("organic_certified")]
        public bool? OrganicCertified { get; set; }
    }

    public class N8nPlantIdentification
    {
        [JsonProperty("species")]
        public string Species { get; set; }
        
        [JsonProperty("variety")]
        public string Variety { get; set; }
        
        [JsonProperty("growth_stage")]
        public string GrowthStage { get; set; }
        
        [JsonProperty("confidence")]
        public int Confidence { get; set; }
        
        [JsonProperty("identifying_features")]
        public List<string> IdentifyingFeatures { get; set; }
        
        [JsonProperty("visible_parts")]
        public List<string> VisibleParts { get; set; }
    }

    public class N8nHealthAssessment
    {
        [JsonProperty("vigor_score")]
        public int VigorScore { get; set; }
        
        [JsonProperty("leaf_color")]
        public string LeafColor { get; set; }
        
        [JsonProperty("leaf_texture")]
        public string LeafTexture { get; set; }
        
        [JsonProperty("growth_pattern")]
        public string GrowthPattern { get; set; }
        
        [JsonProperty("structural_integrity")]
        public string StructuralIntegrity { get; set; }
        
        [JsonProperty("stress_indicators")]
        public List<string> StressIndicators { get; set; }
        
        [JsonProperty("disease_symptoms")]
        public List<string> DiseaseSymptoms { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
    }

    public class N8nNutrientStatus
    {
        [JsonProperty("nitrogen")]
        public string Nitrogen { get; set; }

        [JsonProperty("phosphorus")]
        public string Phosphorus { get; set; }

        [JsonProperty("potassium")]
        public string Potassium { get; set; }

        [JsonProperty("calcium")]
        public string Calcium { get; set; }

        [JsonProperty("magnesium")]
        public string Magnesium { get; set; }

        [JsonProperty("iron")]
        public string Iron { get; set; }

        [JsonProperty("sulfur")]
        public string Sulfur { get; set; }

        [JsonProperty("zinc")]
        public string Zinc { get; set; }

        [JsonProperty("manganese")]
        public string Manganese { get; set; }

        [JsonProperty("boron")]
        public string Boron { get; set; }

        [JsonProperty("copper")]
        public string Copper { get; set; }

        [JsonProperty("molybdenum")]
        public string Molybdenum { get; set; }

        [JsonProperty("chlorine")]
        public string Chlorine { get; set; }

        [JsonProperty("nickel")]
        public string Nickel { get; set; }

        [JsonProperty("primary_deficiency")]
        public string PrimaryDeficiency { get; set; }

        [JsonProperty("secondary_deficiencies")]
        public List<string> SecondaryDeficiencies { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }
    }

    public class N8nPestDisease
    {
        [JsonProperty("pests_detected")]
        public List<N8nPestDetected> PestsDetected { get; set; }

        [JsonProperty("diseases_detected")]
        public List<N8nDiseaseDetected> DiseasesDetected { get; set; }

        [JsonProperty("damage_pattern")]
        public string DamagePattern { get; set; }

        [JsonProperty("affected_area_percentage")]
        public decimal AffectedAreaPercentage { get; set; }

        [JsonProperty("spread_risk")]
        public string SpreadRisk { get; set; }

        [JsonProperty("primary_issue")]
        public string PrimaryIssue { get; set; }
    }

    public class N8nDiseaseDetected
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("affected_parts")]
        public List<string> AffectedParts { get; set; }

        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }
    }

    public class N8nPestDetected
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("affected_parts")]
        public List<string> AffectedParts { get; set; }

        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }
    }


    public class N8nEnvironmentalStress
    {
        [JsonProperty("water_status")]
        public string WaterStatus { get; set; }

        [JsonProperty("temperature_stress")]
        public string TemperatureStress { get; set; }

        [JsonProperty("light_stress")]
        public string LightStress { get; set; }

        [JsonProperty("physical_damage")]
        public string PhysicalDamage { get; set; }

        [JsonProperty("chemical_damage")]
        public string ChemicalDamage { get; set; }

        [JsonProperty("soil_indicators")]
        public string SoilIndicators { get; set; }

        [JsonProperty("physiological_disorders")]
        public List<N8nPhysiologicalDisorder> PhysiologicalDisorders { get; set; }

        [JsonProperty("soil_health_indicators")]
        public N8nSoilHealthIndicators SoilHealthIndicators { get; set; }

        [JsonProperty("primary_stressor")]
        public string PrimaryStressor { get; set; }
    }

    public class N8nCrossFactorInsight
    {
        [JsonProperty("insight")]
        public string Insight { get; set; }
        
        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }
        
        [JsonProperty("affected_aspects")]
        public List<string> AffectedAspects { get; set; }
        
        [JsonProperty("impact_level")]
        public string ImpactLevel { get; set; }
    }

    public class N8nRecommendations
    {
        [JsonProperty("immediate")]
        public List<N8nRecommendation> Immediate { get; set; }

        [JsonProperty("short_term")]
        public List<N8nRecommendation> ShortTerm { get; set; }

        [JsonProperty("preventive")]
        public List<N8nRecommendation> Preventive { get; set; }

        [JsonProperty("monitoring")]
        public List<N8nMonitoringItem> Monitoring { get; set; }

        [JsonProperty("resource_estimation")]
        public N8nResourceEstimation ResourceEstimation { get; set; }

        [JsonProperty("localized_recommendations")]
        public N8nLocalizedRecommendations LocalizedRecommendations { get; set; }
    }

    public class N8nRecommendation
    {
        [JsonProperty("action")]
        public string Action { get; set; }
        
        [JsonProperty("details")]
        public string Details { get; set; }
        
        [JsonProperty("timeline")]
        public string Timeline { get; set; }
        
        [JsonProperty("priority")]
        public string Priority { get; set; }
    }
    
    public class N8nMonitoringItem
    {
        [JsonProperty("parameter")]
        public string Parameter { get; set; }
        
        [JsonProperty("frequency")]
        public string Frequency { get; set; }
        
        [JsonProperty("threshold")]
        public string Threshold { get; set; }
    }

    public class N8nSummary
    {
        [JsonProperty("overall_health_score")]
        public int OverallHealthScore { get; set; }
        
        [JsonProperty("primary_concern")]
        public string PrimaryConcern { get; set; }
        
        [JsonProperty("secondary_concerns")]
        public List<string> SecondaryConcerns { get; set; }
        
        [JsonProperty("critical_issues_count")]
        public int CriticalIssuesCount { get; set; }
        
        [JsonProperty("confidence_level")]
        public decimal ConfidenceLevel { get; set; }
        
        [JsonProperty("prognosis")]
        public string Prognosis { get; set; }
        
        [JsonProperty("estimated_yield_impact")]
        public string EstimatedYieldImpact { get; set; }
    }
    
    public class N8nImageMetadata
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("image_url")]
        public string URL { get; set; }

        [JsonProperty("has_image_extension")]
        public bool HasImageExtension { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("size_bytes")]
        public decimal? SizeBytes { get; set; }

        [JsonProperty("size_kb")]
        public decimal? SizeKb { get; set; }

        [JsonProperty("size_mb")]
        public decimal? SizeMb { get; set; }

        [JsonProperty("base64_length")]
        public int? Base64Length { get; set; }

        [JsonProperty("upload_timestamp")]
        public DateTime? UploadTimestamp { get; set; }
    }
    
    public class N8nProcessingMetadata
    {
        [JsonProperty("parse_success")]
        public bool ParseSuccess { get; set; }
        
        [JsonProperty("processing_timestamp")]
        public DateTime ProcessingTimestamp { get; set; }
        
        [JsonProperty("ai_model")]
        public string AiModel { get; set; }
        
        [JsonProperty("workflow_version")]
        public string WorkflowVersion { get; set; }
    }
    
    public class N8nTokenUsage
    {
        [JsonProperty("summary")]
        public N8nTokenSummary Summary { get; set; }
        
        [JsonProperty("token_breakdown")]
        public N8nTokenBreakdown TokenBreakdown { get; set; }
        
        [JsonProperty("cost_breakdown")]
        public N8nCostBreakdown CostBreakdown { get; set; }
    }
    
    public class N8nTokenSummary
    {
        [JsonProperty("model")]
        public string Model { get; set; }
        
        [JsonProperty("analysis_id")]
        public string AnalysisId { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
        
        [JsonProperty("total_cost_usd")]
        public string TotalCostUsd { get; set; }
        
        [JsonProperty("total_cost_try")]
        public string TotalCostTry { get; set; }
        
        [JsonProperty("image_size_kb")]
        public decimal ImageSizeKb { get; set; }
    }
    
    public class N8nTokenBreakdown
    {
        [JsonProperty("input")]
        public N8nTokenInput Input { get; set; }
        
        [JsonProperty("output")]
        public N8nTokenOutput Output { get; set; }
        
        [JsonProperty("grand_total")]
        public int GrandTotal { get; set; }
    }
    
    public class N8nTokenInput
    {
        [JsonProperty("system_prompt")]
        public int SystemPrompt { get; set; }
        
        [JsonProperty("context_data")]
        public int ContextData { get; set; }
        
        [JsonProperty("image")]
        public int Image { get; set; }
        
        [JsonProperty("total")]
        public int Total { get; set; }
    }
    
    public class N8nTokenOutput
    {
        [JsonProperty("response")]
        public int Response { get; set; }
        
        [JsonProperty("total")]
        public int Total { get; set; }
    }
    
    public class N8nCostBreakdown
    {
        [JsonProperty("input_cost_usd")]
        public string InputCostUsd { get; set; }
        
        [JsonProperty("output_cost_usd")]
        public string OutputCostUsd { get; set; }
        
        [JsonProperty("total_cost_usd")]
        public string TotalCostUsd { get; set; }
        
        [JsonProperty("total_cost_try")]
        public string TotalCostTry { get; set; }
        
        [JsonProperty("exchange_rate")]
        public decimal ExchangeRate { get; set; }
    }

    public class N8nRiskAssessment
    {
        [JsonProperty("yield_loss_probability")]
        public string YieldLossProbability { get; set; }

        [JsonProperty("timeline_to_worsen")]
        public string TimelineToWorsen { get; set; }

        [JsonProperty("spread_potential")]
        public string SpreadPotential { get; set; }
    }

    public class N8nConfidenceNote
    {
        [JsonProperty("aspect")]
        public string Aspect { get; set; }

        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class N8nPhysiologicalDisorder
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }
    }

    public class N8nSoilHealthIndicators
    {
        [JsonProperty("salinity")]
        public string Salinity { get; set; }

        [JsonProperty("pH_issue")]
        public string PhIssue { get; set; }

        [JsonProperty("organic_matter")]
        public string OrganicMatter { get; set; }
    }

    public class N8nResourceEstimation
    {
        [JsonProperty("water_required_liters")]
        public string WaterRequiredLiters { get; set; }

        [JsonProperty("fertilizer_cost_estimate_usd")]
        public string FertilizerCostEstimateUsd { get; set; }

        [JsonProperty("labor_hours_estimate")]
        public string LaborHoursEstimate { get; set; }
    }

    public class N8nLocalizedRecommendations
    {
        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("preferred_practices")]
        public List<string> PreferredPractices { get; set; }

        [JsonProperty("restricted_methods")]
        public List<string> RestrictedMethods { get; set; }
    }
}