using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    public class GetPlantAnalysisDetailQuery : IRequest<IDataResult<PlantAnalysisDetailDto>>
    {
        public int Id { get; set; }
        
        public class GetPlantAnalysisDetailQueryHandler : IRequestHandler<GetPlantAnalysisDetailQuery, IDataResult<PlantAnalysisDetailDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            
            public GetPlantAnalysisDetailQueryHandler(IPlantAnalysisRepository plantAnalysisRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
            }
            
            public async Task<IDataResult<PlantAnalysisDetailDto>> Handle(GetPlantAnalysisDetailQuery request, CancellationToken cancellationToken)
            {
                var analysis = await _plantAnalysisRepository.GetAsync(p => p.Id == request.Id);
                
                if (analysis == null)
                {
                    return new ErrorDataResult<PlantAnalysisDetailDto>("Plant analysis not found");
                }
                
                var detailDto = new PlantAnalysisDetailDto
                {
                    // Basic Information
                    Id = analysis.Id,
                    AnalysisId = analysis.AnalysisId,
                    AnalysisDate = analysis.AnalysisDate,
                    AnalysisStatus = analysis.AnalysisStatus,
                    
                    // User & Sponsor Information
                    UserId = analysis.UserId,
                    FarmerId = analysis.FarmerId,
                    SponsorId = analysis.SponsorId,
                    SponsorUserId = analysis.SponsorUserId,
                    SponsorshipCodeId = analysis.SponsorshipCodeId,
                    
                    // Location Information
                    Location = analysis.Location,
                    Latitude = analysis.Latitude,
                    Longitude = analysis.Longitude,
                    Altitude = analysis.Altitude,
                    
                    // Field & Crop Information
                    FieldId = analysis.FieldId,
                    CropType = analysis.CropType,
                    PlantingDate = analysis.PlantingDate,
                    ExpectedHarvestDate = analysis.ExpectedHarvestDate,
                    LastFertilization = analysis.LastFertilization,
                    LastIrrigation = analysis.LastIrrigation,
                    PreviousTreatments = string.IsNullOrEmpty(analysis.PreviousTreatments) 
                        ? new List<string>() 
                        : JsonConvert.DeserializeObject<List<string>>(analysis.PreviousTreatments),
                    
                    // Environmental Conditions
                    WeatherConditions = analysis.WeatherConditions,
                    Temperature = analysis.Temperature,
                    Humidity = analysis.Humidity,
                    SoilType = analysis.SoilType,
                    
                    // Contact Information
                    ContactPhone = analysis.ContactPhone,
                    ContactEmail = analysis.ContactEmail,
                    
                    // Analysis Request Details
                    UrgencyLevel = analysis.UrgencyLevel,
                    Notes = analysis.Notes,
                    AdditionalInfo = string.IsNullOrEmpty(analysis.AdditionalInfo) 
                        ? null 
                        : JsonConvert.DeserializeObject<AdditionalInfoData>(analysis.AdditionalInfo),
                    
                    // Plant Identification Details
                    PlantIdentification = new PlantIdentificationDetails
                    {
                        Species = analysis.PlantSpecies,
                        Variety = analysis.PlantVariety,
                        GrowthStage = analysis.GrowthStage,
                        Confidence = analysis.IdentificationConfidence,
                        IdentifyingFeatures = string.IsNullOrEmpty(analysis.IdentifyingFeatures) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(analysis.IdentifyingFeatures),
                        VisibleParts = string.IsNullOrEmpty(analysis.VisibleParts) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(analysis.VisibleParts)
                    },
                    
                    // Health Assessment Details
                    HealthAssessment = new HealthAssessmentDetails
                    {
                        VigorScore = analysis.VigorScore,
                        LeafColor = analysis.LeafColor,
                        LeafTexture = analysis.LeafTexture,
                        GrowthPattern = analysis.GrowthPattern,
                        StructuralIntegrity = analysis.StructuralIntegrity,
                        Severity = analysis.HealthSeverity,
                        StressIndicators = string.IsNullOrEmpty(analysis.StressIndicators) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(analysis.StressIndicators),
                        DiseaseSymptoms = string.IsNullOrEmpty(analysis.DiseaseSymptoms) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(analysis.DiseaseSymptoms)
                    },
                    
                    // Nutrient Status Details
                    NutrientStatus = new NutrientStatusDetails
                    {
                        Nitrogen = analysis.NitrogenStatus,
                        Phosphorus = analysis.PhosphorusStatus,
                        Potassium = analysis.PotassiumStatus,
                        Calcium = analysis.CalciumStatus,
                        Magnesium = analysis.MagnesiumStatus,
                        Iron = analysis.IronStatus,
                        PrimaryDeficiency = analysis.PrimaryDeficiency,
                        SecondaryDeficiencies = string.IsNullOrEmpty(analysis.SecondaryDeficiencies) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(analysis.SecondaryDeficiencies),
                        Severity = analysis.NutrientSeverity
                    },
                    
                    // Environmental Stress Details
                    EnvironmentalStress = new EnvironmentalStressDetails
                    {
                        WaterStatus = analysis.WaterStatus,
                        TemperatureStress = analysis.TemperatureStress,
                        LightStress = analysis.LightStress,
                        PhysicalDamage = analysis.PhysicalDamage,
                        ChemicalDamage = analysis.ChemicalDamage,
                        SoilIndicators = analysis.SoilIndicators,
                        PrimaryStressor = analysis.PrimaryStressor
                    },
                    
                    // Summary & Insights
                    Summary = new AnalysisSummaryDetails
                    {
                        OverallHealthScore = analysis.OverallHealthScore,
                        PrimaryConcern = analysis.PrimaryConcern,
                        SecondaryConcerns = string.IsNullOrEmpty(analysis.SecondaryConcerns) 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(analysis.SecondaryConcerns),
                        CriticalIssuesCount = analysis.CriticalIssuesCount,
                        Prognosis = analysis.Prognosis,
                        EstimatedYieldImpact = analysis.EstimatedYieldImpact,
                        ConfidenceLevel = analysis.ConfidenceLevel
                    },
                    
                    // Image Information
                    ImageInfo = new ImageDetails
                    {
                        ImageUrl = analysis.ImageUrl ?? analysis.ImagePath,
                        ImagePath = analysis.ImagePath,
                        Format = analysis.ImageFormat,
                        SizeBytes = analysis.ImageSizeBytes,
                        SizeKb = analysis.ImageSizeKb,
                        SizeMb = analysis.ImageSizeMb,
                        UploadTimestamp = analysis.ImageUploadTimestamp
                    },
                    
                    // Processing Information
                    ProcessingInfo = new ProcessingDetails
                    {
                        AiModel = analysis.AiModel,
                        WorkflowVersion = analysis.WorkflowVersion,
                        ProcessingTimestamp = analysis.ProcessingTimestamp,
                        ProcessingTimeMs = analysis.ProcessingTimeMs,
                        ParseSuccess = analysis.ParseSuccess,
                        CorrelationId = analysis.CorrelationId,
                        RetryCount = analysis.RetryCount
                    },
                    
                    // Success Status
                    Success = analysis.Success ?? true,
                    Message = analysis.Message,
                    Error = analysis.Error,
                    ErrorMessage = analysis.ErrorMessage
                };
                
                // Parse Pest & Disease Details
                try
                {
                    var pestDiseaseDetails = new PestDiseaseDetails
                    {
                        PestsDetected = string.IsNullOrEmpty(analysis.PestsDetected) 
                            ? new List<PestDetails>() 
                            : ParsePestDetails(analysis.PestsDetected),
                        DiseasesDetected = string.IsNullOrEmpty(analysis.DiseasesDetected) 
                            ? new List<DiseaseDetails>() 
                            : ParseDiseaseDetails(analysis.DiseasesDetected),
                        DamagePattern = analysis.DamagePattern,
                        AffectedAreaPercentage = analysis.AffectedAreaPercentage,
                        SpreadRisk = analysis.SpreadRisk,
                        PrimaryIssue = analysis.PrimaryIssue
                    };
                    detailDto.PestDisease = pestDiseaseDetails;
                }
                catch
                {
                    // If parsing fails, create empty structure
                    detailDto.PestDisease = new PestDiseaseDetails
                    {
                        PestsDetected = new List<PestDetails>(),
                        DiseasesDetected = new List<DiseaseDetails>(),
                        DamagePattern = analysis.DamagePattern,
                        AffectedAreaPercentage = analysis.AffectedAreaPercentage,
                        SpreadRisk = analysis.SpreadRisk,
                        PrimaryIssue = analysis.PrimaryIssue
                    };
                }
                
                // Parse Cross-Factor Insights
                try
                {
                    detailDto.CrossFactorInsights = string.IsNullOrEmpty(analysis.CrossFactorInsights)
                        ? new List<CrossFactorInsightDetails>()
                        : JsonConvert.DeserializeObject<List<CrossFactorInsightDetails>>(analysis.CrossFactorInsights);
                }
                catch
                {
                    detailDto.CrossFactorInsights = new List<CrossFactorInsightDetails>();
                }
                
                // Parse Recommendations
                try
                {
                    var recommendations = new RecommendationsDetails
                    {
                        Immediate = string.IsNullOrEmpty(analysis.ImmediateRecommendations)
                            ? new List<RecommendationItem>()
                            : JsonConvert.DeserializeObject<List<RecommendationItem>>(analysis.ImmediateRecommendations),
                        ShortTerm = string.IsNullOrEmpty(analysis.ShortTermRecommendations)
                            ? new List<RecommendationItem>()
                            : JsonConvert.DeserializeObject<List<RecommendationItem>>(analysis.ShortTermRecommendations),
                        Preventive = string.IsNullOrEmpty(analysis.PreventiveRecommendations)
                            ? new List<RecommendationItem>()
                            : JsonConvert.DeserializeObject<List<RecommendationItem>>(analysis.PreventiveRecommendations),
                        Monitoring = string.IsNullOrEmpty(analysis.MonitoringRecommendations)
                            ? new List<MonitoringItem>()
                            : JsonConvert.DeserializeObject<List<MonitoringItem>>(analysis.MonitoringRecommendations)
                    };
                    detailDto.Recommendations = recommendations;
                }
                catch
                {
                    // If specific recommendation parsing fails, try to parse from main Recommendations field
                    try
                    {
                        if (!string.IsNullOrEmpty(analysis.Recommendations))
                        {
                            detailDto.Recommendations = JsonConvert.DeserializeObject<RecommendationsDetails>(analysis.Recommendations);
                        }
                        else
                        {
                            detailDto.Recommendations = new RecommendationsDetails
                            {
                                Immediate = new List<RecommendationItem>(),
                                ShortTerm = new List<RecommendationItem>(),
                                Preventive = new List<RecommendationItem>(),
                                Monitoring = new List<MonitoringItem>()
                            };
                        }
                    }
                    catch
                    {
                        detailDto.Recommendations = new RecommendationsDetails
                        {
                            Immediate = new List<RecommendationItem>(),
                            ShortTerm = new List<RecommendationItem>(),
                            Preventive = new List<RecommendationItem>(),
                            Monitoring = new List<MonitoringItem>()
                        };
                    }
                }
                
                return new SuccessDataResult<PlantAnalysisDetailDto>(detailDto);
            }
            
            private List<PestDetails> ParsePestDetails(string pestsJson)
            {
                try
                {
                    // First try to deserialize as list of PestDetails
                    return JsonConvert.DeserializeObject<List<PestDetails>>(pestsJson);
                }
                catch
                {
                    // If that fails, try to deserialize as generic object array and convert
                    try
                    {
                        var genericPests = JsonConvert.DeserializeObject<List<dynamic>>(pestsJson);
                        var pestDetails = new List<PestDetails>();
                        
                        foreach (var pest in genericPests)
                        {
                            pestDetails.Add(new PestDetails
                            {
                                Name = pest.name?.ToString() ?? pest.type?.ToString() ?? "Unknown",
                                Category = pest.category?.ToString() ?? "unknown",
                                Severity = pest.severity?.ToString() ?? "unknown",
                                AffectedParts = pest.affected_parts != null 
                                    ? JsonConvert.DeserializeObject<List<string>>(pest.affected_parts.ToString())
                                    : new List<string>()
                            });
                        }
                        
                        return pestDetails;
                    }
                    catch
                    {
                        return new List<PestDetails>();
                    }
                }
            }
            
            private List<DiseaseDetails> ParseDiseaseDetails(string diseasesJson)
            {
                try
                {
                    // First try to deserialize as list of DiseaseDetails
                    return JsonConvert.DeserializeObject<List<DiseaseDetails>>(diseasesJson);
                }
                catch
                {
                    // If that fails, try to deserialize as generic object array and convert
                    try
                    {
                        var genericDiseases = JsonConvert.DeserializeObject<List<dynamic>>(diseasesJson);
                        var diseaseDetails = new List<DiseaseDetails>();
                        
                        foreach (var disease in genericDiseases)
                        {
                            diseaseDetails.Add(new DiseaseDetails
                            {
                                Type = disease.type?.ToString() ?? disease.name?.ToString() ?? "Unknown",
                                Category = disease.category?.ToString() ?? "unknown",
                                Severity = disease.severity?.ToString() ?? "unknown",
                                AffectedParts = disease.affected_parts != null 
                                    ? JsonConvert.DeserializeObject<List<string>>(disease.affected_parts.ToString())
                                    : new List<string>()
                            });
                        }
                        
                        return diseaseDetails;
                    }
                    catch
                    {
                        return new List<DiseaseDetails>();
                    }
                }
            }
        }
    }
}