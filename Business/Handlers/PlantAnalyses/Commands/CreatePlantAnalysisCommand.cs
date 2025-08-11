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
                        // Don't store base64 in database for performance reasons
                        // ImageBase64 = request.Image,
                        UserId = request.UserId,
                        FarmerId = request.FarmerId,
                        SponsorId = request.SponsorId,
                        FieldId = request.FieldId,
                        CropType = request.CropType,
                        Location = request.Location,
                        Latitude = request.GpsCoordinates?.Lat,
                        Longitude = request.GpsCoordinates?.Lng,
                        Altitude = request.Altitude,
                        PlantingDate = request.PlantingDate?.ToUniversalTime(),
                        ExpectedHarvestDate = request.ExpectedHarvestDate?.ToUniversalTime(),
                        LastFertilization = request.LastFertilization?.ToUniversalTime(),
                        LastIrrigation = request.LastIrrigation?.ToUniversalTime(),
                        PreviousTreatments = request.PreviousTreatments != null ? JsonConvert.SerializeObject(request.PreviousTreatments) : null,
                        SoilType = request.SoilType,
                        Temperature = request.Temperature,
                        Humidity = request.Humidity,
                        WeatherConditions = request.WeatherConditions,
                        UrgencyLevel = request.UrgencyLevel,
                        Notes = request.Notes,
                        ContactPhone = request.ContactInfo?.Phone,
                        ContactEmail = request.ContactInfo?.Email,
                        AdditionalInfo = request.AdditionalInfo != null ? JsonConvert.SerializeObject(request.AdditionalInfo) : null,
                        AnalysisDate = DateTime.UtcNow,
                        AnalysisStatus = "Processing",
                        Status = true,
                        CreatedDate = DateTime.UtcNow
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
                        if (!string.IsNullOrEmpty(analysisResponse.FarmerId)) plantAnalysis.FarmerId = analysisResponse.FarmerId;
                        if (!string.IsNullOrEmpty(analysisResponse.SponsorId)) plantAnalysis.SponsorId = analysisResponse.SponsorId;
                        if (!string.IsNullOrEmpty(analysisResponse.Location)) plantAnalysis.Location = analysisResponse.Location;
                        if (analysisResponse.GpsCoordinates != null)
                        {
                            plantAnalysis.Latitude = analysisResponse.GpsCoordinates.Lat;
                            plantAnalysis.Longitude = analysisResponse.GpsCoordinates.Lng;
                        }
                        
                        // Plant identification
                        if (analysisResponse.PlantIdentification != null)
                        {
                            plantAnalysis.PlantSpecies = analysisResponse.PlantIdentification.Species;
                            plantAnalysis.PlantVariety = analysisResponse.PlantIdentification.Variety;
                            plantAnalysis.GrowthStage = analysisResponse.PlantIdentification.GrowthStage;
                            plantAnalysis.IdentificationConfidence = analysisResponse.PlantIdentification.Confidence;
                        }
                        
                        // Health assessment
                        if (analysisResponse.HealthAssessment != null)
                        {
                            plantAnalysis.VigorScore = analysisResponse.HealthAssessment.VigorScore;
                            plantAnalysis.HealthSeverity = analysisResponse.HealthAssessment.Severity;
                            plantAnalysis.StressIndicators = JsonConvert.SerializeObject(analysisResponse.HealthAssessment.StressIndicators);
                            plantAnalysis.DiseaseSymptoms = JsonConvert.SerializeObject(analysisResponse.HealthAssessment.DiseaseSymptoms);
                        }
                        
                        // Nutrient status
                        if (analysisResponse.NutrientStatus != null)
                        {
                            plantAnalysis.PrimaryDeficiency = analysisResponse.NutrientStatus.PrimaryDeficiency;
                            plantAnalysis.NutrientStatus = JsonConvert.SerializeObject(analysisResponse.NutrientStatus);
                        }
                        
                        // Summary
                        if (analysisResponse.Summary != null)
                        {
                            plantAnalysis.OverallHealthScore = analysisResponse.Summary.OverallHealthScore;
                            plantAnalysis.PrimaryConcern = analysisResponse.Summary.PrimaryConcern;
                            plantAnalysis.Prognosis = analysisResponse.Summary.Prognosis;
                            plantAnalysis.EstimatedYieldImpact = analysisResponse.Summary.EstimatedYieldImpact;
                            plantAnalysis.ConfidenceLevel = analysisResponse.Summary.ConfidenceLevel;
                        }
                        
                        // Metadata
                        if (analysisResponse.ProcessingMetadata != null)
                        {
                            plantAnalysis.AiModel = analysisResponse.ProcessingMetadata.AiModel;
                        }
                        
                        if (analysisResponse.TokenUsage?.Summary != null)
                        {
                            plantAnalysis.TotalTokens = analysisResponse.TokenUsage.Summary.TotalTokens;
                            // Parse cost strings to decimal
                            if (decimal.TryParse(analysisResponse.TokenUsage.Summary.TotalCostUsd?.Replace("$", ""), out var costUsd))
                                plantAnalysis.TotalCostUsd = costUsd;
                            if (decimal.TryParse(analysisResponse.TokenUsage.Summary.TotalCostTry?.Replace("₺", ""), out var costTry))
                                plantAnalysis.TotalCostTry = costTry;
                            plantAnalysis.ImageSizeKb = analysisResponse.TokenUsage.Summary.ImageSizeKb;
                        }
                        
                        // Recommendations and insights
                        plantAnalysis.Recommendations = JsonConvert.SerializeObject(analysisResponse.Recommendations);
                        plantAnalysis.CrossFactorInsights = JsonConvert.SerializeObject(analysisResponse.CrossFactorInsights);
                        
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