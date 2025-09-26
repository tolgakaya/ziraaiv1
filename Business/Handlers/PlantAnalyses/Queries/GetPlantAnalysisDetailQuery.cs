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

                // Simple mapping - use JSONB fields directly or parse them as needed
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
                    PreviousTreatments = TryParseJsonArray(analysis.PreviousTreatments),

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
                    AdditionalInfo = TryParseJson<AdditionalInfoData>(analysis.AdditionalInfo),

                    // Parse from JSONB fields
                    PlantIdentification = TryParseJson<PlantIdentificationDetails>(analysis.PlantIdentification) ?? CreateBasicPlantIdentification(analysis),
                    HealthAssessment = TryParseJson<HealthAssessmentDetails>(analysis.HealthAssessment) ?? CreateBasicHealthAssessment(analysis),
                    NutrientStatus = TryParseJson<NutrientStatusDetails>(analysis.NutrientStatus) ?? CreateBasicNutrientStatus(analysis),
                    PestDisease = TryParseJson<PestDiseaseDetails>(analysis.PestDisease) ?? new PestDiseaseDetails(),
                    EnvironmentalStress = TryParseJson<EnvironmentalStressDetails>(analysis.EnvironmentalStress) ?? new EnvironmentalStressDetails(),
                    Summary = TryParseJson<AnalysisSummaryDetails>(analysis.Summary) ?? CreateBasicSummary(analysis),
                    CrossFactorInsights = TryParseJsonArray<CrossFactorInsightDetails>(analysis.CrossFactorInsights),
                    Recommendations = TryParseJson<RecommendationsDetails>(analysis.Recommendations) ?? new RecommendationsDetails(),

                    // Image Information from JSONB
                    ImageInfo = TryParseJson<ImageDetails>(analysis.ImageMetadata) ?? new ImageDetails { ImageUrl = analysis.ImageUrl },

                    // Processing Information from JSONB
                    ProcessingInfo = TryParseJson<ProcessingDetails>(analysis.ProcessingMetadata) ?? CreateBasicProcessingInfo(analysis),

                    // Success Status
                    Success = true,
                    Message = "Success",
                    Error = false,
                    ErrorMessage = null
                };

                return new SuccessDataResult<PlantAnalysisDetailDto>(detailDto);
            }

            private static T TryParseJson<T>(string jsonString) where T : class
            {
                if (string.IsNullOrEmpty(jsonString) || jsonString == "{}")
                    return null;

                try
                {
                    return JsonConvert.DeserializeObject<T>(jsonString);
                }
                catch
                {
                    return null;
                }
            }

            private static List<string> TryParseJsonArray(string jsonString)
            {
                if (string.IsNullOrEmpty(jsonString))
                    return new List<string>();

                try
                {
                    return JsonConvert.DeserializeObject<List<string>>(jsonString) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }

            private static List<T> TryParseJsonArray<T>(string jsonString) where T : class
            {
                if (string.IsNullOrEmpty(jsonString))
                    return new List<T>();

                try
                {
                    return JsonConvert.DeserializeObject<List<T>>(jsonString) ?? new List<T>();
                }
                catch
                {
                    return new List<T>();
                }
            }

            private static PlantIdentificationDetails CreateBasicPlantIdentification(Entities.Concrete.PlantAnalysis analysis)
            {
                return new PlantIdentificationDetails
                {
                    Species = analysis.PlantSpecies,
                    Variety = analysis.PlantVariety,
                    GrowthStage = analysis.GrowthStage,
                    Confidence = analysis.IdentificationConfidence,
                    IdentifyingFeatures = new List<string>(),
                    VisibleParts = new List<string>()
                };
            }

            private static HealthAssessmentDetails CreateBasicHealthAssessment(Entities.Concrete.PlantAnalysis analysis)
            {
                return new HealthAssessmentDetails
                {
                    VigorScore = analysis.VigorScore,
                    Severity = analysis.HealthSeverity,
                    StressIndicators = TryParseJsonArray(analysis.StressIndicators),
                    DiseaseSymptoms = TryParseJsonArray(analysis.DiseaseSymptoms),
                    LeafColor = null,
                    LeafTexture = null,
                    GrowthPattern = null,
                    StructuralIntegrity = null
                };
            }

            private static NutrientStatusDetails CreateBasicNutrientStatus(Entities.Concrete.PlantAnalysis analysis)
            {
                return new NutrientStatusDetails
                {
                    Nitrogen = analysis.Nitrogen,
                    Phosphorus = analysis.Phosphorus,
                    Potassium = analysis.Potassium,
                    Calcium = analysis.Calcium,
                    Magnesium = analysis.Magnesium,
                    Sulfur = analysis.Sulfur,
                    Iron = analysis.Iron,
                    Zinc = analysis.Zinc,
                    Manganese = analysis.Manganese,
                    Boron = analysis.Boron,
                    Copper = analysis.Copper,
                    Molybdenum = analysis.Molybdenum,
                    Chlorine = analysis.Chlorine,
                    Nickel = analysis.Nickel,
                    PrimaryDeficiency = analysis.PrimaryDeficiency,
                    SecondaryDeficiencies = new List<string>(),
                    Severity = analysis.NutrientSeverity
                };
            }

            private static AnalysisSummaryDetails CreateBasicSummary(Entities.Concrete.PlantAnalysis analysis)
            {
                return new AnalysisSummaryDetails
                {
                    OverallHealthScore = analysis.OverallHealthScore,
                    PrimaryConcern = analysis.PrimaryConcern,
                    SecondaryConcerns = new List<string>(),
                    CriticalIssuesCount = analysis.CriticalIssuesCount,
                    Prognosis = analysis.Prognosis,
                    EstimatedYieldImpact = analysis.EstimatedYieldImpact,
                    ConfidenceLevel = analysis.ConfidenceLevel
                };
            }

            private static ProcessingDetails CreateBasicProcessingInfo(Entities.Concrete.PlantAnalysis analysis)
            {
                return new ProcessingDetails
                {
                    AiModel = analysis.AiModel,
                    WorkflowVersion = analysis.WorkflowVersion,
                    ProcessingTimestamp = analysis.ProcessingTimestamp,
                    ProcessingTimeMs = null,
                    ParseSuccess = true,
                    CorrelationId = null,
                    RetryCount = null
                };
            }
            
        }
    }
}