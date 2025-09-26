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
                    PestDisease = TryParseJson<PestDiseaseDetails>(analysis.PestDisease) ?? CreateBasicPestDisease(analysis),
                    EnvironmentalStress = TryParseJson<EnvironmentalStressDetails>(analysis.EnvironmentalStress) ?? CreateBasicEnvironmentalStress(analysis),
                    Summary = TryParseJson<AnalysisSummaryDetails>(analysis.Summary) ?? CreateBasicSummary(analysis),
                    CrossFactorInsights = TryParseJsonArray<CrossFactorInsightDetails>(analysis.CrossFactorInsights),
                    Recommendations = TryParseJson<RecommendationsDetails>(analysis.Recommendations) ?? CreateBasicRecommendations(analysis),

                    // Image Information from JSONB
                    ImageInfo = TryParseJson<ImageDetails>(analysis.ImageMetadata) ?? new ImageDetails { ImageUrl = analysis.ImageUrl },

                    // Processing Information from JSONB
                    ProcessingInfo = TryParseJson<ProcessingDetails>(analysis.ProcessingMetadata) ?? CreateBasicProcessingInfo(analysis),

                    // Risk Assessment
                    RiskAssessment = TryParseJson<RiskAssessmentDetails>(analysis.RiskAssessment) ?? CreateBasicRiskAssessment(analysis),

                    // Confidence Notes
                    ConfidenceNotes = TryParseJsonArray<ConfidenceNoteDetails>(analysis.ConfidenceNotes),

                    // Farmer Friendly Summary
                    FarmerFriendlySummary = analysis.FarmerFriendlySummary,

                    // Token Usage
                    TokenUsage = TryParseJson<TokenUsageDetails>(analysis.TokenUsage) ?? new TokenUsageDetails(),

                    // Request Metadata
                    RequestMetadata = TryParseJson<RequestMetadataDetails>(analysis.RequestMetadata) ?? new RequestMetadataDetails(),

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
                // Try to get identifying features and visible parts from JSONB first
                var identifyingFeatures = new List<string>();
                var visibleParts = new List<string>();

                if (!string.IsNullOrEmpty(analysis.PlantIdentification))
                {
                    try
                    {
                        var plantId = JsonConvert.DeserializeObject<dynamic>(analysis.PlantIdentification);
                        if (plantId?.identifying_features != null)
                        {
                            foreach (var feature in plantId.identifying_features)
                            {
                                identifyingFeatures.Add(feature.ToString());
                            }
                        }
                        if (plantId?.visible_parts != null)
                        {
                            foreach (var part in plantId.visible_parts)
                            {
                                visibleParts.Add(part.ToString());
                            }
                        }
                    }
                    catch { /* Silent fallback to empty lists */ }
                }

                return new PlantIdentificationDetails
                {
                    Species = analysis.PlantSpecies,
                    Variety = analysis.PlantVariety,
                    GrowthStage = analysis.GrowthStage, // Helper field has priority
                    Confidence = analysis.IdentificationConfidence,
                    IdentifyingFeatures = identifyingFeatures,
                    VisibleParts = visibleParts
                };
            }

            private static HealthAssessmentDetails CreateBasicHealthAssessment(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get additional details from JSONB first
                string leafColor = null, leafTexture = null, growthPattern = null, structuralIntegrity = null;

                if (!string.IsNullOrEmpty(analysis.HealthAssessment))
                {
                    try
                    {
                        var healthData = JsonConvert.DeserializeObject<dynamic>(analysis.HealthAssessment);
                        leafColor = healthData?.leaf_color?.ToString();
                        leafTexture = healthData?.leaf_texture?.ToString();
                        growthPattern = healthData?.growth_pattern?.ToString();
                        structuralIntegrity = healthData?.structural_integrity?.ToString();
                    }
                    catch { /* Silent fallback to null values */ }
                }

                return new HealthAssessmentDetails
                {
                    VigorScore = analysis.VigorScore, // Helper field has priority
                    Severity = analysis.HealthSeverity, // Helper field has priority
                    StressIndicators = TryParseJsonArray(analysis.StressIndicators), // Helper field
                    DiseaseSymptoms = TryParseJsonArray(analysis.DiseaseSymptoms), // Helper field
                    LeafColor = leafColor,
                    LeafTexture = leafTexture,
                    GrowthPattern = growthPattern,
                    StructuralIntegrity = structuralIntegrity
                };
            }

            private static NutrientStatusDetails CreateBasicNutrientStatus(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get secondary deficiencies from JSONB
                var secondaryDeficiencies = new List<string>();

                if (!string.IsNullOrEmpty(analysis.NutrientStatus))
                {
                    try
                    {
                        var nutrientData = JsonConvert.DeserializeObject<dynamic>(analysis.NutrientStatus);
                        if (nutrientData?.secondary_deficiencies != null)
                        {
                            foreach (var deficiency in nutrientData.secondary_deficiencies)
                            {
                                secondaryDeficiencies.Add(deficiency.ToString());
                            }
                        }
                    }
                    catch { /* Silent fallback to empty list */ }
                }

                return new NutrientStatusDetails
                {
                    Nitrogen = analysis.Nitrogen, // Helper fields have priority
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
                    PrimaryDeficiency = analysis.PrimaryDeficiency, // Helper field
                    SecondaryDeficiencies = secondaryDeficiencies,
                    Severity = analysis.NutrientSeverity // Helper field
                };
            }

            private static AnalysisSummaryDetails CreateBasicSummary(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get secondary concerns from JSONB
                var secondaryConcerns = new List<string>();

                if (!string.IsNullOrEmpty(analysis.Summary))
                {
                    try
                    {
                        var summaryData = JsonConvert.DeserializeObject<dynamic>(analysis.Summary);
                        if (summaryData?.secondary_concerns != null)
                        {
                            foreach (var concern in summaryData.secondary_concerns)
                            {
                                secondaryConcerns.Add(concern.ToString());
                            }
                        }
                    }
                    catch { /* Silent fallback to empty list */ }
                }

                return new AnalysisSummaryDetails
                {
                    OverallHealthScore = analysis.OverallHealthScore, // Helper field has priority
                    PrimaryConcern = analysis.PrimaryConcern, // Helper field has priority
                    SecondaryConcerns = secondaryConcerns,
                    CriticalIssuesCount = analysis.CriticalIssuesCount, // Helper field has priority
                    Prognosis = analysis.Prognosis, // Helper field has priority
                    EstimatedYieldImpact = analysis.EstimatedYieldImpact, // Helper field has priority
                    ConfidenceLevel = analysis.ConfidenceLevel // Helper field has priority
                };
            }

            private static PestDiseaseDetails CreateBasicPestDisease(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get pest/disease details from JSONB
                var pestsDetected = new List<PestDetails>();
                var diseasesDetected = new List<DiseaseDetails>();
                string damagePattern = null;

                if (!string.IsNullOrEmpty(analysis.PestDisease))
                {
                    try
                    {
                        var pestData = JsonConvert.DeserializeObject<dynamic>(analysis.PestDisease);
                        damagePattern = pestData?.damage_pattern?.ToString();

                        if (pestData?.pests_detected != null)
                        {
                            foreach (var pest in pestData.pests_detected)
                            {
                                pestsDetected.Add(new PestDetails
                                {
                                    Name = pest?.type?.ToString(),
                                    Category = pest?.category?.ToString(),
                                    Severity = pest?.severity?.ToString(),
                                    AffectedParts = pest?.affected_parts != null ?
                                        TryParseJsonArray(pest.affected_parts.ToString()) :
                                        new List<string>()
                                });
                            }
                        }

                        if (pestData?.diseases_detected != null)
                        {
                            foreach (var disease in pestData.diseases_detected)
                            {
                                diseasesDetected.Add(new DiseaseDetails
                                {
                                    Type = disease?.type?.ToString(),
                                    Category = disease?.category?.ToString(),
                                    Severity = disease?.severity?.ToString(),
                                    AffectedParts = disease?.affected_parts != null ?
                                        TryParseJsonArray(disease.affected_parts.ToString()) :
                                        new List<string>()
                                });
                            }
                        }
                    }
                    catch { /* Silent fallback to empty lists */ }
                }

                return new PestDiseaseDetails
                {
                    PestsDetected = pestsDetected,
                    DiseasesDetected = diseasesDetected,
                    DamagePattern = damagePattern,
                    AffectedAreaPercentage = analysis.AffectedAreaPercentage,
                    SpreadRisk = analysis.SpreadRisk,
                    PrimaryIssue = analysis.PrimaryIssue
                };
            }

            private static EnvironmentalStressDetails CreateBasicEnvironmentalStress(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get environmental stress details from JSONB
                string waterStatus = null, temperatureStress = null, lightStress = null, physicalDamage = null, chemicalDamage = null, soilIndicators = null;

                if (!string.IsNullOrEmpty(analysis.EnvironmentalStress))
                {
                    try
                    {
                        var envData = JsonConvert.DeserializeObject<dynamic>(analysis.EnvironmentalStress);
                        waterStatus = envData?.water_status?.ToString();
                        temperatureStress = envData?.temperature_stress?.ToString();
                        lightStress = envData?.light_stress?.ToString();
                        physicalDamage = envData?.physical_damage?.ToString();
                        chemicalDamage = envData?.chemical_damage?.ToString();
                        soilIndicators = envData?.soil_indicators?.ToString();
                    }
                    catch { /* Silent fallback to null values */ }
                }

                return new EnvironmentalStressDetails
                {
                    WaterStatus = waterStatus,
                    TemperatureStress = temperatureStress,
                    LightStress = lightStress,
                    PhysicalDamage = physicalDamage,
                    ChemicalDamage = chemicalDamage,
                    SoilIndicators = soilIndicators,
                    PrimaryStressor = analysis.PrimaryStressor
                };
            }

            private static RiskAssessmentDetails CreateBasicRiskAssessment(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get risk assessment details from JSONB
                string yieldLossProbability = null, timelineToWorsen = null, spreadPotential = null;

                if (!string.IsNullOrEmpty(analysis.RiskAssessment))
                {
                    try
                    {
                        var riskData = JsonConvert.DeserializeObject<dynamic>(analysis.RiskAssessment);
                        yieldLossProbability = riskData?.yield_loss_probability?.ToString();
                        timelineToWorsen = riskData?.timeline_to_worsen?.ToString();
                        spreadPotential = riskData?.spread_potential?.ToString();
                    }
                    catch { /* Silent fallback to null values */ }
                }

                return new RiskAssessmentDetails
                {
                    YieldLossProbability = yieldLossProbability,
                    TimelineToWorsen = timelineToWorsen,
                    SpreadPotential = spreadPotential
                };
            }

            private static RecommendationsDetails CreateBasicRecommendations(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get recommendations from JSONB
                var immediate = new List<RecommendationItem>();
                var shortTerm = new List<RecommendationItem>();
                var preventive = new List<RecommendationItem>();
                var monitoring = new List<MonitoringItem>();

                if (!string.IsNullOrEmpty(analysis.Recommendations))
                {
                    try
                    {
                        var recData = JsonConvert.DeserializeObject<dynamic>(analysis.Recommendations);

                        if (recData?.immediate != null)
                        {
                            foreach (var item in recData.immediate)
                            {
                                immediate.Add(new RecommendationItem
                                {
                                    Action = item?.action?.ToString(),
                                    Details = item?.details?.ToString(),
                                    Timeline = item?.timeline?.ToString(),
                                    Priority = item?.priority?.ToString()
                                });
                            }
                        }

                        if (recData?.short_term != null)
                        {
                            foreach (var item in recData.short_term)
                            {
                                shortTerm.Add(new RecommendationItem
                                {
                                    Action = item?.action?.ToString(),
                                    Details = item?.details?.ToString(),
                                    Timeline = item?.timeline?.ToString(),
                                    Priority = item?.priority?.ToString()
                                });
                            }
                        }

                        if (recData?.preventive != null)
                        {
                            foreach (var item in recData.preventive)
                            {
                                preventive.Add(new RecommendationItem
                                {
                                    Action = item?.action?.ToString(),
                                    Details = item?.details?.ToString(),
                                    Timeline = item?.timeline?.ToString(),
                                    Priority = item?.priority?.ToString()
                                });
                            }
                        }

                        if (recData?.monitoring != null)
                        {
                            foreach (var item in recData.monitoring)
                            {
                                monitoring.Add(new MonitoringItem
                                {
                                    Parameter = item?.parameter?.ToString(),
                                    Frequency = item?.frequency?.ToString(),
                                    Threshold = item?.threshold?.ToString()
                                });
                            }
                        }
                    }
                    catch { /* Silent fallback to empty lists */ }
                }

                return new RecommendationsDetails
                {
                    Immediate = immediate,
                    ShortTerm = shortTerm,
                    Preventive = preventive,
                    Monitoring = monitoring
                };
            }

            private static ProcessingDetails CreateBasicProcessingInfo(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to get additional processing details from JSONB
                long? processingTimeMs = null;
                bool? parseSuccess = true;
                string correlationId = null;
                int? retryCount = null;

                if (!string.IsNullOrEmpty(analysis.ProcessingMetadata))
                {
                    try
                    {
                        var processingData = JsonConvert.DeserializeObject<dynamic>(analysis.ProcessingMetadata);
                        processingTimeMs = processingData?.processing_time_ms;
                        parseSuccess = processingData?.parse_success ?? true;
                        correlationId = processingData?.correlation_id?.ToString();
                        retryCount = processingData?.retry_count;
                    }
                    catch { /* Silent fallback to default values */ }
                }

                return new ProcessingDetails
                {
                    AiModel = analysis.AiModel,
                    WorkflowVersion = analysis.WorkflowVersion,
                    ProcessingTimestamp = analysis.ProcessingTimestamp != DateTime.MinValue ? analysis.ProcessingTimestamp : analysis.AnalysisDate,
                    ProcessingTimeMs = processingTimeMs,
                    ParseSuccess = parseSuccess,
                    CorrelationId = correlationId,
                    RetryCount = retryCount
                };
            }
            
        }
    }
}