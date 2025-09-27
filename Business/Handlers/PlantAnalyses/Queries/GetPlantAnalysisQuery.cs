using Business.Constants;
using Business.Services.FileStorage;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    public class GetPlantAnalysisQuery : IRequest<IDataResult<PlantAnalysisResponseDto>>
    {
        public int Id { get; set; }

        public class GetPlantAnalysisQueryHandler : IRequestHandler<GetPlantAnalysisQuery, IDataResult<PlantAnalysisResponseDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IFileStorageService _fileStorageService;

            public GetPlantAnalysisQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                IFileStorageService fileStorageService)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _fileStorageService = fileStorageService;
            }

            private static List<string> TryDeserializeStringArray(string jsonString)
            {
                if (string.IsNullOrEmpty(jsonString))
                    return null;

                try
                {
                    return JsonConvert.DeserializeObject<string[]>(jsonString)?.ToList();
                }
                catch (JsonException)
                {
                    return null;
                }
            }

            private static T TryDeserializeObject<T>(string jsonString) where T : class
            {
                if (string.IsNullOrEmpty(jsonString))
                    return null;

                try
                {
                    return JsonConvert.DeserializeObject<T>(jsonString);
                }
                catch (JsonException)
                {
                    return null;
                }
            }

            public async Task<IDataResult<PlantAnalysisResponseDto>> Handle(GetPlantAnalysisQuery request, CancellationToken cancellationToken)
            {
                var plantAnalysis = await _plantAnalysisRepository.GetAsync(p => p.Id == request.Id && p.Status);
                
                if (plantAnalysis == null)
                    return new ErrorDataResult<PlantAnalysisResponseDto>("Analysis not found");

                // Convert image path to full URL if it's a relative path
                string imageUrl = plantAnalysis.ImagePath;
                if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
                {
                    // For relative paths, convert to full URL using base URL
                    var baseUrl = _fileStorageService.BaseUrl?.TrimEnd('/');
                    var relativePath = imageUrl.TrimStart('/');
                    imageUrl = $"{baseUrl}/{relativePath}";
                }

                var response = new PlantAnalysisResponseDto
                {
                    Id = plantAnalysis.Id,
                    ImagePath = imageUrl, // Return full URL instead of relative path
                    AnalysisDate = plantAnalysis.AnalysisDate,
                    Status = plantAnalysis.AnalysisStatus,
                    UserId = plantAnalysis.UserId,
                    
                    // Core Analysis Fields
                    AnalysisId = plantAnalysis.AnalysisId,
                    FarmerId = plantAnalysis.FarmerId,
                    SponsorId = plantAnalysis.SponsorId,
                    SponsorUserId = plantAnalysis.SponsorUserId,        // Actual sponsor user ID
                    SponsorshipCodeId = plantAnalysis.SponsorshipCodeId, // SponsorshipCode table ID
                    Location = plantAnalysis.Location,
                    GpsCoordinates = plantAnalysis.Latitude.HasValue && plantAnalysis.Longitude.HasValue
                        ? new GpsCoordinates { Lat = plantAnalysis.Latitude.Value, Lng = plantAnalysis.Longitude.Value }
                        : null,
                    Altitude = plantAnalysis.Altitude,
                    FieldId = plantAnalysis.FieldId,
                    CropType = plantAnalysis.CropType,
                    PlantingDate = plantAnalysis.PlantingDate,
                    ExpectedHarvestDate = plantAnalysis.ExpectedHarvestDate,
                    UrgencyLevel = plantAnalysis.UrgencyLevel,
                    Notes = plantAnalysis.Notes,
                    
                    // Plant Identification
                    PlantIdentification = !string.IsNullOrEmpty(plantAnalysis.PlantSpecies)
                        ? new PlantIdentificationDto
                        {
                            Species = plantAnalysis.PlantSpecies,
                            Variety = plantAnalysis.PlantVariety,
                            GrowthStage = plantAnalysis.GrowthStage,
                            Confidence = plantAnalysis.IdentificationConfidence ?? 0
                        }
                        : null,
                    
                    // Health Assessment
                    HealthAssessment = plantAnalysis.VigorScore.HasValue
                        ? new HealthAssessmentDto
                        {
                            VigorScore = plantAnalysis.VigorScore.Value,
                            Severity = plantAnalysis.HealthSeverity,
                            StressIndicators = TryDeserializeStringArray(plantAnalysis.StressIndicators),
                            DiseaseSymptoms = TryDeserializeStringArray(plantAnalysis.DiseaseSymptoms)
                        }
                        : null,
                    
                    // Nutrient Status
                    NutrientStatus = !string.IsNullOrEmpty(plantAnalysis.PrimaryDeficiency)
                        ? new NutrientStatusDto
                        {
                            PrimaryDeficiency = plantAnalysis.PrimaryDeficiency,
                            // Parse full nutrient status from JSON if available
                        }
                        : null,
                    
                    // Summary
                    Summary = plantAnalysis.OverallHealthScore > 0
                        ? new SummaryDto
                        {
                            OverallHealthScore = plantAnalysis.OverallHealthScore,
                            PrimaryConcern = plantAnalysis.PrimaryConcern,
                            Prognosis = plantAnalysis.Prognosis,
                            EstimatedYieldImpact = plantAnalysis.EstimatedYieldImpact,
                            ConfidenceLevel = plantAnalysis.ConfidenceLevel ?? 0
                        }
                        : null,
                    
                    // ProcessingMetadata and TokenUsage are no longer included in response
                    // They are stored in database but not returned to maintain clean API response
                    
                    // Recommendations
                    Recommendations = TryDeserializeObject<RecommendationsDto>(plantAnalysis.Recommendations),

                    // Cross Factor Insights
                    CrossFactorInsights = TryDeserializeObject<List<CrossFactorInsightDto>>(plantAnalysis.CrossFactorInsights),

                    // Detailed analysis data (full JSON for reference)
                    DetailedAnalysis = TryDeserializeObject<DetailedPlantAnalysisDto>(plantAnalysis.DetailedAnalysisData) ?? new DetailedPlantAnalysisDto(),

                    // Legacy fields for backward compatibility
                    PlantType = plantAnalysis.PlantType,
                    GrowthStage = plantAnalysis.GrowthStage,
                    ElementDeficiencies = TryDeserializeObject<List<ElementDeficiencyDto>>(plantAnalysis.ElementDeficiencies) ?? new List<ElementDeficiencyDto>(),
                    Diseases = TryDeserializeObject<List<DiseaseDto>>(plantAnalysis.Diseases) ?? new List<DiseaseDto>(),
                    Pests = TryDeserializeObject<List<PestDto>>(plantAnalysis.Pests) ?? new List<PestDto>()
                };

                if (!string.IsNullOrEmpty(plantAnalysis.AnalysisResult))
                {
                    try
                    {
                        var analysisResult = JsonConvert.DeserializeObject<dynamic>(plantAnalysis.AnalysisResult);
                        response.OverallAnalysis = analysisResult?.OverallAnalysis;
                        response.Recommendations = analysisResult?.Recommendations;
                    }
                    catch (JsonException)
                    {
                        // If JSON parsing fails, skip and continue with null values
                        response.OverallAnalysis = null;
                        response.Recommendations = null;
                    }
                }

                return new SuccessDataResult<PlantAnalysisResponseDto>(response);
            }
        }
    }
}