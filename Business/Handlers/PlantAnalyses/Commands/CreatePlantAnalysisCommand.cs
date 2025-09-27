using Business.Constants;
using Business.Services.PlantAnalysis;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Commands
{
    public class CreatePlantAnalysisCommand : PlantAnalysisRequestDto, IRequest<IDataResult<PlantAnalysisResponseDto>>
    {

        public class CreatePlantAnalysisCommandHandler : IRequestHandler<CreatePlantAnalysisCommand, IDataResult<PlantAnalysisResponseDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IPlantAnalysisService _plantAnalysisService;
            private readonly IMediator _mediator;

            public CreatePlantAnalysisCommandHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                IPlantAnalysisService plantAnalysisService,
                IMediator mediator)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _plantAnalysisService = plantAnalysisService;
                _mediator = mediator;
            }

            public async Task<IDataResult<PlantAnalysisResponseDto>> Handle(CreatePlantAnalysisCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    var plantAnalysis = new PlantAnalysis
                    {
                        // Required fields from database
                        AnalysisId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.UtcNow,

                        // Don't store base64 in database for performance reasons
                        // ImageBase64 = request.Image,
                        UserId = request.UserId,
                        FarmerId = request.FarmerId,
                        SponsorId = request.SponsorId,
                        SponsorUserId = request.SponsorUserId,
                        SponsorshipCodeId = request.SponsorshipCodeId,
                        FieldId = request.FieldId,
                        CropType = request.CropType,
                        Location = request.Location,

                        // GPS Coordinates as JSONB and helper fields
                        GpsCoordinates = request.GpsCoordinates != null ? JsonConvert.SerializeObject(request.GpsCoordinates) : null,
                        Latitude = request.GpsCoordinates?.Lat,
                        Longitude = request.GpsCoordinates?.Lng,

                        Altitude = request.Altitude,
                        PlantingDate = request.PlantingDate,
                        ExpectedHarvestDate = request.ExpectedHarvestDate,
                        LastFertilization = request.LastFertilization,
                        LastIrrigation = request.LastIrrigation,
                        PreviousTreatments = request.PreviousTreatments != null ? JsonConvert.SerializeObject(request.PreviousTreatments) : null,
                        SoilType = request.SoilType,
                        Temperature = request.Temperature,
                        Humidity = request.Humidity,
                        WeatherConditions = request.WeatherConditions,
                        UrgencyLevel = request.UrgencyLevel,
                        Notes = request.Notes,

                        // Contact info as text field and helper fields
                        ContactInfo = request.ContactInfo != null ? JsonConvert.SerializeObject(request.ContactInfo) : null,
                        ContactPhone = request.ContactInfo?.Phone,
                        ContactEmail = request.ContactInfo?.Email,

                        AdditionalInfo = request.AdditionalInfo != null ? JsonConvert.SerializeObject(request.AdditionalInfo) : null,

                        // Initialize JSONB fields with empty JSON
                        PlantIdentification = "{}",
                        HealthAssessment = "{}",
                        NutrientStatus = "{}",
                        PestDisease = "{}",
                        EnvironmentalStress = "{}",
                        CrossFactorInsights = null,
                        RiskAssessment = "{}",
                        Recommendations = "{}",
                        Summary = "{}",
                        ConfidenceNotes = null,
                        FarmerFriendlySummary = "",
                        ImageMetadata = "{}",
                        ImageUrl = "",
                        RequestMetadata = "{}",
                        TokenUsage = "{}",
                        ProcessingMetadata = "{}",
                        DetailedAnalysisData = "{}",

                        AnalysisDate = DateTime.Now,
                        AnalysisStatus = "pending",
                        Status = true,
                        CreatedDate = DateTime.Now,
                        ProcessingTimestamp = DateTime.Now
                    };

                    _plantAnalysisRepository.Add(plantAnalysis);
                    
                    try
                    {
                        await _plantAnalysisRepository.SaveChangesAsync();
                    }
                    catch (Exception saveEx)
                    {
                        return new ErrorDataResult<PlantAnalysisResponseDto>($"Database save error: {saveEx.Message} | Inner: {saveEx.InnerException?.Message}");
                    }

                    // Save image file to disk
                    try
                    {
                        var imagePath = await _plantAnalysisService.SaveImageFileAsync(request.Image, plantAnalysis.Id);
                        plantAnalysis.ImagePath = imagePath;
                        // Keep base64 for now, can be removed later if needed
                        // plantAnalysis.ImageBase64 = null; 
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the entire process
                        plantAnalysis.N8nWebhookResponse = $"Image save error: {ex.Message}";
                    }

                    PlantAnalysisResponseDto analysisResponse;
                    try
                    {
                        // Call N8N webhook for plant analysis
                        analysisResponse = await _plantAnalysisService.SendToN8nWebhookAsync(request);
                        
                        // Save analysis response data
                        plantAnalysis.AnalysisId = analysisResponse.AnalysisId;
                        plantAnalysis.DetailedAnalysisData = analysisResponse.DetailedAnalysis?.FullResponseJson ?? JsonConvert.SerializeObject(analysisResponse.DetailedAnalysis);
                        plantAnalysis.N8nWebhookResponse = analysisResponse.DetailedAnalysis?.FullResponseJson;
                        
                        // Update with response data if different from request
                        // SECURITY: FarmerId and SponsorId are NEVER updated from N8N response
                        // These values are securely determined by the controller based on authenticated user
                        if (!string.IsNullOrEmpty(analysisResponse.Location)) plantAnalysis.Location = analysisResponse.Location;
                        if (analysisResponse.GpsCoordinates != null)
                        {
                            plantAnalysis.GpsCoordinates = JsonConvert.SerializeObject(analysisResponse.GpsCoordinates);
                            plantAnalysis.Latitude = analysisResponse.GpsCoordinates.Lat;
                            plantAnalysis.Longitude = analysisResponse.GpsCoordinates.Lng;
                        }

                        // Additional metadata from N8N response
                        if (analysisResponse.LastFertilization.HasValue) plantAnalysis.LastFertilization = analysisResponse.LastFertilization;
                        if (analysisResponse.LastIrrigation.HasValue) plantAnalysis.LastIrrigation = analysisResponse.LastIrrigation;
                        if (analysisResponse.PreviousTreatments != null) plantAnalysis.PreviousTreatments = JsonConvert.SerializeObject(analysisResponse.PreviousTreatments);
                        if (!string.IsNullOrEmpty(analysisResponse.WeatherConditions)) plantAnalysis.WeatherConditions = analysisResponse.WeatherConditions;
                        if (analysisResponse.Temperature.HasValue) plantAnalysis.Temperature = analysisResponse.Temperature;
                        if (analysisResponse.Humidity.HasValue) plantAnalysis.Humidity = analysisResponse.Humidity;
                        if (!string.IsNullOrEmpty(analysisResponse.SoilType)) plantAnalysis.SoilType = analysisResponse.SoilType;
                        if (analysisResponse.ContactInfo != null)
                        {
                            plantAnalysis.ContactInfo = JsonConvert.SerializeObject(analysisResponse.ContactInfo);
                            plantAnalysis.ContactPhone = analysisResponse.ContactInfo.Phone;
                            plantAnalysis.ContactEmail = analysisResponse.ContactInfo.Email;
                        }
                        if (analysisResponse.AdditionalInfo != null) plantAnalysis.AdditionalInfo = JsonConvert.SerializeObject(analysisResponse.AdditionalInfo);
                        if (!string.IsNullOrEmpty(analysisResponse.ImageUrl)) plantAnalysis.ImageUrl = analysisResponse.ImageUrl;
                        
                        // Plant identification
                        if (analysisResponse.PlantIdentification != null)
                        {
                            plantAnalysis.PlantSpecies = analysisResponse.PlantIdentification.Species;
                            plantAnalysis.PlantVariety = analysisResponse.PlantIdentification.Variety;
                            plantAnalysis.GrowthStage = analysisResponse.PlantIdentification.GrowthStage;
                            plantAnalysis.IdentificationConfidence = analysisResponse.PlantIdentification.Confidence;
                            plantAnalysis.PlantIdentification = JsonConvert.SerializeObject(analysisResponse.PlantIdentification);
                        }
                        
                        // Health assessment
                        if (analysisResponse.HealthAssessment != null)
                        {
                            plantAnalysis.VigorScore = analysisResponse.HealthAssessment.VigorScore;
                            plantAnalysis.HealthSeverity = analysisResponse.HealthAssessment.Severity;
                            plantAnalysis.StressIndicators = JsonConvert.SerializeObject(analysisResponse.HealthAssessment.StressIndicators);
                            plantAnalysis.DiseaseSymptoms = JsonConvert.SerializeObject(analysisResponse.HealthAssessment.DiseaseSymptoms);
                            plantAnalysis.HealthAssessment = JsonConvert.SerializeObject(analysisResponse.HealthAssessment);
                        }
                        
                        // Nutrient status - Save all individual nutrients
                        if (analysisResponse.NutrientStatus != null)
                        {
                            plantAnalysis.PrimaryDeficiency = analysisResponse.NutrientStatus.PrimaryDeficiency;
                            plantAnalysis.NutrientSeverity = analysisResponse.NutrientStatus.Severity;

                            // Save individual nutrient status
                            plantAnalysis.Nitrogen = analysisResponse.NutrientStatus.Nitrogen;
                            plantAnalysis.Phosphorus = analysisResponse.NutrientStatus.Phosphorus;
                            plantAnalysis.Potassium = analysisResponse.NutrientStatus.Potassium;
                            plantAnalysis.Calcium = analysisResponse.NutrientStatus.Calcium;
                            plantAnalysis.Magnesium = analysisResponse.NutrientStatus.Magnesium;
                            plantAnalysis.Sulfur = analysisResponse.NutrientStatus.Sulfur;
                            plantAnalysis.Iron = analysisResponse.NutrientStatus.Iron;
                            plantAnalysis.Zinc = analysisResponse.NutrientStatus.Zinc;
                            plantAnalysis.Manganese = analysisResponse.NutrientStatus.Manganese;
                            plantAnalysis.Boron = analysisResponse.NutrientStatus.Boron;
                            plantAnalysis.Copper = analysisResponse.NutrientStatus.Copper;
                            plantAnalysis.Molybdenum = analysisResponse.NutrientStatus.Molybdenum;
                            plantAnalysis.Chlorine = analysisResponse.NutrientStatus.Chlorine;
                            plantAnalysis.Nickel = analysisResponse.NutrientStatus.Nickel;

                            plantAnalysis.NutrientStatus = JsonConvert.SerializeObject(analysisResponse.NutrientStatus);
                        }
                        
                        // Pest & Disease
                        if (analysisResponse.PestDisease != null)
                        {
                            plantAnalysis.AffectedAreaPercentage = (int?)analysisResponse.PestDisease.AffectedAreaPercentage;
                            plantAnalysis.SpreadRisk = analysisResponse.PestDisease.SpreadRisk;
                            plantAnalysis.PrimaryIssue = analysisResponse.PestDisease.PrimaryIssue;
                            plantAnalysis.PestDisease = JsonConvert.SerializeObject(analysisResponse.PestDisease);
                        }

                        // Environmental Stress
                        if (analysisResponse.EnvironmentalStress != null)
                        {
                            plantAnalysis.PrimaryStressor = analysisResponse.EnvironmentalStress.PrimaryStressor;
                            plantAnalysis.EnvironmentalStress = JsonConvert.SerializeObject(analysisResponse.EnvironmentalStress);
                        }

                        // Risk Assessment
                        if (analysisResponse.RiskAssessment != null)
                        {
                            plantAnalysis.RiskAssessment = JsonConvert.SerializeObject(analysisResponse.RiskAssessment);
                        }

                        // Summary
                        if (analysisResponse.Summary != null)
                        {
                            plantAnalysis.OverallHealthScore = analysisResponse.Summary.OverallHealthScore;
                            plantAnalysis.PrimaryConcern = analysisResponse.Summary.PrimaryConcern;
                            plantAnalysis.Prognosis = analysisResponse.Summary.Prognosis;
                            plantAnalysis.EstimatedYieldImpact = analysisResponse.Summary.EstimatedYieldImpact;
                            plantAnalysis.ConfidenceLevel = analysisResponse.Summary.ConfidenceLevel;
                            plantAnalysis.CriticalIssuesCount = analysisResponse.Summary.CriticalIssuesCount;
                            plantAnalysis.Summary = JsonConvert.SerializeObject(analysisResponse.Summary);
                        }

                        // Confidence Notes
                        if (analysisResponse.ConfidenceNotes != null)
                        {
                            plantAnalysis.ConfidenceNotes = JsonConvert.SerializeObject(analysisResponse.ConfidenceNotes);
                        }

                        // Farmer Friendly Summary
                        if (!string.IsNullOrEmpty(analysisResponse.FarmerFriendlySummary))
                        {
                            plantAnalysis.FarmerFriendlySummary = analysisResponse.FarmerFriendlySummary;
                        }

                        // Image Metadata
                        if (analysisResponse.ImageMetadata != null)
                        {
                            plantAnalysis.ImageUrl = analysisResponse.ImageMetadata.ImageUrl;
                            plantAnalysis.ImageMetadata = JsonConvert.SerializeObject(analysisResponse.ImageMetadata);
                        }
                        
                        // Processing Metadata (saved to DB, but not returned in response)
                        // Parse metadata from the full N8N response JSON
                        if (!string.IsNullOrEmpty(analysisResponse.DetailedAnalysis?.FullResponseJson))
                        {
                            try
                            {
                                var fullN8nResponse = JsonConvert.DeserializeObject<dynamic>(analysisResponse.DetailedAnalysis.FullResponseJson);

                                // Extract processing metadata
                                if (fullN8nResponse?.processing_metadata != null)
                                {
                                    plantAnalysis.AiModel = fullN8nResponse.processing_metadata.ai_model;
                                    plantAnalysis.WorkflowVersion = fullN8nResponse.processing_metadata.workflow_version;
                                    plantAnalysis.ProcessingMetadata = JsonConvert.SerializeObject(fullN8nResponse.processing_metadata);
                                }

                                // Extract token usage
                                if (fullN8nResponse?.token_usage?.summary != null)
                                {
                                    var tokenSummary = fullN8nResponse.token_usage.summary;
                                    plantAnalysis.TotalTokens = tokenSummary.total_tokens ?? 0;
                                    if (decimal.TryParse(tokenSummary.total_cost_usd?.ToString()?.Replace("$", ""), out decimal costUsd))
                                        plantAnalysis.TotalCostUsd = costUsd;
                                    if (decimal.TryParse(tokenSummary.total_cost_try?.ToString()?.Replace("₺", ""), out decimal costTry))
                                        plantAnalysis.TotalCostTry = costTry;
                                    plantAnalysis.ImageSizeKb = tokenSummary.image_size_kb;
                                    plantAnalysis.TokenUsage = JsonConvert.SerializeObject(fullN8nResponse.token_usage);
                                }

                                // Extract request metadata
                                if (fullN8nResponse?.request_metadata != null)
                                {
                                    plantAnalysis.RequestMetadata = JsonConvert.SerializeObject(fullN8nResponse.request_metadata);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't fail the entire process
                                plantAnalysis.N8nWebhookResponse = $"Metadata parsing error: {ex.Message}";
                            }
                        }

                        // Recommendations and Cross-Factor Insights
                        if (analysisResponse.Recommendations != null)
                        {
                            plantAnalysis.Recommendations = JsonConvert.SerializeObject(analysisResponse.Recommendations);
                        }

                        if (analysisResponse.CrossFactorInsights != null)
                        {
                            plantAnalysis.CrossFactorInsights = JsonConvert.SerializeObject(analysisResponse.CrossFactorInsights);
                        }
                        
                        // Legacy fields for backward compatibility
                        plantAnalysis.PlantType = analysisResponse.PlantType;
                        plantAnalysis.ElementDeficiencies = JsonConvert.SerializeObject(analysisResponse.ElementDeficiencies);
                        plantAnalysis.Diseases = JsonConvert.SerializeObject(analysisResponse.Diseases);
                        plantAnalysis.Pests = JsonConvert.SerializeObject(analysisResponse.Pests);
                        plantAnalysis.AnalysisResult = analysisResponse.OverallAnalysis;
                        
                        plantAnalysis.AnalysisStatus = "Completed";
                        plantAnalysis.UpdatedDate = DateTime.UtcNow;

                        analysisResponse.Id = plantAnalysis.Id;
                        analysisResponse.ImagePath = plantAnalysis.ImagePath;
                        
                        // SECURITY: Ensure response contains the secure sponsorship data from database
                        analysisResponse.FarmerId = plantAnalysis.FarmerId;
                        analysisResponse.SponsorId = plantAnalysis.SponsorId;
                        analysisResponse.SponsorUserId = plantAnalysis.SponsorUserId;
                        analysisResponse.SponsorshipCodeId = plantAnalysis.SponsorshipCodeId;
                    }
                    catch (Exception ex)
                    {
                        plantAnalysis.AnalysisStatus = "Failed";
                        plantAnalysis.N8nWebhookResponse = ex.Message;
                        plantAnalysis.UpdatedDate = DateTime.UtcNow;
                        
                        _plantAnalysisRepository.Update(plantAnalysis);
                        await _plantAnalysisRepository.SaveChangesAsync();
                        
                        return new ErrorDataResult<PlantAnalysisResponseDto>($"Analysis failed: {ex.Message}");
                    }

                    _plantAnalysisRepository.Update(plantAnalysis);
                    await _plantAnalysisRepository.SaveChangesAsync();

                    return new SuccessDataResult<PlantAnalysisResponseDto>(analysisResponse, "Plant analysis completed successfully");
                }
                catch (Exception ex)
                {
                    return new ErrorDataResult<PlantAnalysisResponseDto>($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}