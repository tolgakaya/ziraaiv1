using Business.BusinessAspects;
using Business.Services.Sponsorship;
using Business.Services.Subscription;
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
            private readonly ISponsorDataAccessService _dataAccessService;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly IAnalysisMessageRepository _analysisMessageRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly ITierFeatureService _tierFeatureService;
            
            public GetPlantAnalysisDetailQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                ISponsorDataAccessService dataAccessService,
                ISponsorProfileRepository sponsorProfileRepository,
                IAnalysisMessageRepository analysisMessageRepository,
                IUserSubscriptionRepository userSubscriptionRepository,
                ISubscriptionTierRepository subscriptionTierRepository,
                ITierFeatureService tierFeatureService)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _dataAccessService = dataAccessService;
                _sponsorProfileRepository = sponsorProfileRepository;
                _analysisMessageRepository = analysisMessageRepository;
                _userSubscriptionRepository = userSubscriptionRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
                _tierFeatureService = tierFeatureService;
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
                    PlantIdentification = TryParseJsonForPlantIdentification(analysis),
                    HealthAssessment = TryParseJson<HealthAssessmentDetails>(analysis.HealthAssessment) ?? CreateBasicHealthAssessment(analysis),
                    NutrientStatus = TryParseJson<NutrientStatusDetails>(analysis.NutrientStatus) ?? CreateBasicNutrientStatus(analysis),
                    PestDisease = TryParseJson<PestDiseaseDetails>(analysis.PestDisease) ?? CreateBasicPestDisease(analysis),
                    EnvironmentalStress = TryParseJson<EnvironmentalStressDetails>(analysis.EnvironmentalStress) ?? CreateBasicEnvironmentalStress(analysis),
                    Summary = TryParseJson<AnalysisSummaryDetails>(analysis.Summary) ?? CreateBasicSummary(analysis),
                    CrossFactorInsights = TryParseJsonArray<CrossFactorInsightDetails>(analysis.CrossFactorInsights) ?? CreateBasicCrossFactorInsights(analysis),
                    Recommendations = CreateBasicRecommendations(analysis),

                    // Image Information from JSONB
                    ImageInfo = GetImageInfo(analysis),

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

                // 🎯 Populate sponsorship metadata if analysis was done with sponsorship code
                // For farmer view: Show sponsor info without tier restrictions
                // For sponsor view: GetFilteredAnalysisForSponsorQuery adds full tier metadata
                if (analysis.SponsorUserId.HasValue && analysis.SponsorshipCodeId.HasValue)
                {
                    try
                    {
                        var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(analysis.SponsorUserId.Value);

                        // Get subscription and tier information for dynamic feature checks
                        var subscription = await _userSubscriptionRepository.GetAsync(
                            us => us.Id == analysis.ActiveSponsorshipId.Value);

                        string tierName = "Unknown";
                        bool canMessage = false;
                        bool canViewLogo = false;

                        if (subscription != null)
                        {
                            // Get actual tier name
                            var tier = await _subscriptionTierRepository.GetAsync(
                                t => t.Id == subscription.SubscriptionTierId);
                            tierName = tier?.DisplayName ?? tier?.TierName ?? "Unknown";

                            // Check tier features dynamically
                            canMessage = await _tierFeatureService.HasFeatureAccessAsync(
                                subscription.SubscriptionTierId,
                                "messaging");

                            canViewLogo = await _tierFeatureService.HasFeatureAccessAsync(
                                subscription.SubscriptionTierId,
                                "sponsor_visibility");
                        }

                        // Check if sponsor has initiated conversation (sent first message)
                        // Farmer can only reply if sponsor has messaged them first
                        bool canReply = await _analysisMessageRepository.HasSponsorMessagedAnalysisAsync(
                            analysis.Id,
                            analysis.SponsorUserId.Value);

                        detailDto.SponsorshipMetadata = new AnalysisTierMetadata
                        {
                            TierName = tierName, // ✅ Dynamic: Actual tier name from subscription
                            AccessPercentage = 100, // ✅ Correct: Farmer sees all their own data
                            CanMessage = canMessage, // ✅ Dynamic: Tier-based messaging feature check
                            CanReply = canReply, // ✅ Dynamic: true only if sponsor has sent message first
                            CanViewLogo = canViewLogo, // ✅ Dynamic: Tier-based sponsor visibility check
                            SponsorInfo = sponsorProfile != null ? new SponsorDisplayInfoDto
                            {
                                SponsorId = sponsorProfile.SponsorId,
                                CompanyName = sponsorProfile.CompanyName,
                                LogoUrl = sponsorProfile.SponsorLogoUrl,
                                WebsiteUrl = sponsorProfile.WebsiteUrl
                            } : null,
                            AccessibleFields = new AccessibleFieldsInfo
                            {
                                // Farmer sees all their own analysis fields
                                CanViewBasicInfo = true,
                                CanViewHealthScore = true,
                                CanViewImages = true,
                                CanViewDetailedHealth = true,
                                CanViewDiseases = true,
                                CanViewNutrients = true,
                                CanViewRecommendations = true,
                                CanViewLocation = true,
                                CanViewFarmerContact = true,
                                CanViewFieldData = true,
                                CanViewProcessingData = true
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if sponsorship metadata fetch fails
                        Console.WriteLine($"[GetPlantAnalysisDetailQuery] Warning: Could not fetch sponsorship metadata: {ex.Message}");
                        detailDto.SponsorshipMetadata = null;
                    }
                }

                return new SuccessDataResult<PlantAnalysisDetailDto>(detailDto);
            }

            private static T TryParseJson<T>(string jsonString) where T : class
            {
                if (string.IsNullOrEmpty(jsonString))
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
            // Use helper fields first (now populated), then fallback to JSONB if needed
            string species = analysis.PlantSpecies;
            string variety = analysis.PlantVariety;
            string growthStage = analysis.GrowthStage;
            decimal? confidence = analysis.IdentificationConfidence;
            var identifyingFeatures = new List<string>();
            var visibleParts = new List<string>();

            // Debug logging
            Console.WriteLine($"[DEBUG] Helper fields - Species: {species}, Variety: {variety}, GrowthStage: {growthStage}, Confidence: {confidence}");
            Console.WriteLine($"[DEBUG] JSONB PlantIdentification: {analysis.PlantIdentification}");

            // Always parse from JSONB to get arrays and handle missing helper fields
            if (!string.IsNullOrEmpty(analysis.PlantIdentification))
            {
                try
                {
                    var plantId = JsonConvert.DeserializeObject<dynamic>(analysis.PlantIdentification);
                    
                    // Use JSONB as fallback for basic fields if helper fields are still null
                    if (string.IsNullOrEmpty(species) && plantId?.species != null)
                    {
                        species = plantId.species.ToString();
                    }
                    
                    if (string.IsNullOrEmpty(variety) && plantId?.variety != null)
                    {
                        variety = plantId.variety.ToString();
                    }
                    
                    if (string.IsNullOrEmpty(growthStage) && plantId?.growth_stage != null)
                    {
                        growthStage = plantId.growth_stage.ToString();
                    }
                    
                    if (!confidence.HasValue && plantId?.confidence != null)
                    {
                        if (decimal.TryParse(plantId.confidence.ToString(), out decimal parsedConfidence))
                        {
                            confidence = parsedConfidence;
                        }
                    }
                    
                    // Always parse arrays from JSONB (no helper fields exist for arrays)
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
                catch (Exception ex)
                {
                    // Log the error for debugging but continue with available data
                    Console.WriteLine($"[ERROR] Error parsing plant identification JSONB: {ex.Message}");
                    Console.WriteLine($"[ERROR] JSONB content: {analysis.PlantIdentification}");
                }
            }

            var result = new PlantIdentificationDetails
            {
                Species = species,
                Variety = variety,
                GrowthStage = growthStage,
                Confidence = confidence,
                IdentifyingFeatures = identifyingFeatures,
                VisibleParts = visibleParts
            };

            // Debug final result
            Console.WriteLine($"[DEBUG] Final result - Species: {result.Species}, Variety: {result.Variety}, GrowthStage: {result.GrowthStage}, Confidence: {result.Confidence}");
            Console.WriteLine($"[DEBUG] Final arrays - IdentifyingFeatures: [{string.Join(", ", result.IdentifyingFeatures)}], VisibleParts: [{string.Join(", ", result.VisibleParts)}]");

            return result;
        }

        private static PlantIdentificationDetails TryParseJsonForPlantIdentification(Entities.Concrete.PlantAnalysis analysis)
        {
            Console.WriteLine($"[DEBUG] TryParseJsonForPlantIdentification called for analysis ID: {analysis.Id}");

            var tryParseResult = TryParseJson<PlantIdentificationDetails>(analysis.PlantIdentification);

            if (tryParseResult != null)
            {
                Console.WriteLine($"[DEBUG] TryParseJson succeeded - Species: {tryParseResult.Species}, GrowthStage: {tryParseResult.GrowthStage}");
                Console.WriteLine($"[DEBUG] TryParseJson arrays - IdentifyingFeatures: [{string.Join(", ", tryParseResult.IdentifyingFeatures ?? new List<string>())}], VisibleParts: [{string.Join(", ", tryParseResult.VisibleParts ?? new List<string>())}]");
                return tryParseResult;
            }
            else
            {
                Console.WriteLine($"[DEBUG] TryParseJson failed, calling CreateBasicPlantIdentification");
                return CreateBasicPlantIdentification(analysis);
            }
        }

            private static HealthAssessmentDetails CreateBasicHealthAssessment(Entities.Concrete.PlantAnalysis analysis)
            {
                // First try to parse complete JSONB
                var healthAssessment = TryParseJson<HealthAssessmentDetails>(analysis.HealthAssessment);
                if (healthAssessment != null)
                {
                    // Override with helper fields if they exist (helper fields have priority)
                    if (analysis.VigorScore.HasValue)
                        healthAssessment.VigorScore = analysis.VigorScore;
                    if (!string.IsNullOrEmpty(analysis.HealthSeverity))
                        healthAssessment.Severity = analysis.HealthSeverity;
                    if (!string.IsNullOrEmpty(analysis.StressIndicators))
                        healthAssessment.StressIndicators = TryParseJsonArray(analysis.StressIndicators);
                    if (!string.IsNullOrEmpty(analysis.DiseaseSymptoms))
                        healthAssessment.DiseaseSymptoms = TryParseJsonArray(analysis.DiseaseSymptoms);
                    
                    return healthAssessment;
                }

                // Fallback: construct from helper fields and JSONB parts
                int? vigorScore = analysis.VigorScore;
                string severity = analysis.HealthSeverity;
                var stressIndicators = TryParseJsonArray(analysis.StressIndicators);
                var diseaseSymptoms = TryParseJsonArray(analysis.DiseaseSymptoms);
                string leafColor = null, leafTexture = null, growthPattern = null, structuralIntegrity = null;

                if (!string.IsNullOrEmpty(analysis.HealthAssessment))
                {
                    try
                    {
                        var healthData = JsonConvert.DeserializeObject<dynamic>(analysis.HealthAssessment);
                        
                        // Get basic fields
                        if (!vigorScore.HasValue && healthData?.vigor_score != null)
                        {
                            if (int.TryParse(healthData.vigor_score.ToString(), out int parsedVigor))
                                vigorScore = parsedVigor;
                        }
                        
                        leafColor = healthData?.leaf_color?.ToString();
                        leafTexture = healthData?.leaf_texture?.ToString();
                        growthPattern = healthData?.growth_pattern?.ToString();
                        structuralIntegrity = healthData?.structural_integrity?.ToString();
                        
                        if (string.IsNullOrEmpty(severity))
                            severity = healthData?.severity?.ToString();
                        
                        // Parse arrays if helper fields are empty
                        if (stressIndicators.Count == 0 && healthData?.stress_indicators != null)
                        {
                            foreach (var indicator in healthData.stress_indicators)
                                stressIndicators.Add(indicator.ToString());
                        }
                        
                        if (diseaseSymptoms.Count == 0 && healthData?.disease_symptoms != null)
                        {
                            foreach (var symptom in healthData.disease_symptoms)
                                diseaseSymptoms.Add(symptom.ToString());
                        }
                    }
                    catch { /* Silent fallback */ }
                }

                return new HealthAssessmentDetails
                {
                    VigorScore = vigorScore,
                    Severity = severity,
                    StressIndicators = stressIndicators,
                    DiseaseSymptoms = diseaseSymptoms,
                    LeafColor = leafColor,
                    LeafTexture = leafTexture,
                    GrowthPattern = growthPattern,
                    StructuralIntegrity = structuralIntegrity
                };
            }

            private static NutrientStatusDetails CreateBasicNutrientStatus(Entities.Concrete.PlantAnalysis analysis)
            {
                // First try to parse complete JSONB
                var nutrientStatus = TryParseJson<NutrientStatusDetails>(analysis.NutrientStatus);
                if (nutrientStatus != null)
                {
                    // Override with helper fields if they exist (helper fields have priority)
                    if (!string.IsNullOrEmpty(analysis.Nitrogen))
                        nutrientStatus.Nitrogen = analysis.Nitrogen;
                    if (!string.IsNullOrEmpty(analysis.Phosphorus))
                        nutrientStatus.Phosphorus = analysis.Phosphorus;
                    if (!string.IsNullOrEmpty(analysis.Potassium))
                        nutrientStatus.Potassium = analysis.Potassium;
                    if (!string.IsNullOrEmpty(analysis.Calcium))
                        nutrientStatus.Calcium = analysis.Calcium;
                    if (!string.IsNullOrEmpty(analysis.Magnesium))
                        nutrientStatus.Magnesium = analysis.Magnesium;
                    if (!string.IsNullOrEmpty(analysis.Sulfur))
                        nutrientStatus.Sulfur = analysis.Sulfur;
                    if (!string.IsNullOrEmpty(analysis.Iron))
                        nutrientStatus.Iron = analysis.Iron;
                    if (!string.IsNullOrEmpty(analysis.Zinc))
                        nutrientStatus.Zinc = analysis.Zinc;
                    if (!string.IsNullOrEmpty(analysis.Manganese))
                        nutrientStatus.Manganese = analysis.Manganese;
                    if (!string.IsNullOrEmpty(analysis.Boron))
                        nutrientStatus.Boron = analysis.Boron;
                    if (!string.IsNullOrEmpty(analysis.Copper))
                        nutrientStatus.Copper = analysis.Copper;
                    if (!string.IsNullOrEmpty(analysis.Molybdenum))
                        nutrientStatus.Molybdenum = analysis.Molybdenum;
                    if (!string.IsNullOrEmpty(analysis.Chlorine))
                        nutrientStatus.Chlorine = analysis.Chlorine;
                    if (!string.IsNullOrEmpty(analysis.Nickel))
                        nutrientStatus.Nickel = analysis.Nickel;
                    if (!string.IsNullOrEmpty(analysis.PrimaryDeficiency))
                        nutrientStatus.PrimaryDeficiency = analysis.PrimaryDeficiency;
                    if (!string.IsNullOrEmpty(analysis.NutrientSeverity))
                        nutrientStatus.Severity = analysis.NutrientSeverity;
                    
                    return nutrientStatus;
                }

                // Fallback: construct from helper fields and JSONB parts
                var secondaryDeficiencies = new List<string>();
                string primaryDeficiency = analysis.PrimaryDeficiency;

                if (!string.IsNullOrEmpty(analysis.NutrientStatus))
                {
                    try
                    {
                        var nutrientData = JsonConvert.DeserializeObject<dynamic>(analysis.NutrientStatus);
                        
                        // Get primary_deficiency if helper field is empty
                        if (string.IsNullOrEmpty(primaryDeficiency) && nutrientData?.primary_deficiency != null)
                            primaryDeficiency = nutrientData.primary_deficiency.ToString();
                        
                        // Get secondary_deficiencies
                        if (nutrientData?.secondary_deficiencies != null)
                        {
                            foreach (var deficiency in nutrientData.secondary_deficiencies)
                                secondaryDeficiencies.Add(deficiency.ToString());
                        }
                    }
                    catch { /* Silent fallback */ }
                }

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
                    PrimaryDeficiency = primaryDeficiency,
                    SecondaryDeficiencies = secondaryDeficiencies,
                    Severity = analysis.NutrientSeverity
                };
            }

            private static AnalysisSummaryDetails CreateBasicSummary(Entities.Concrete.PlantAnalysis analysis)
            {
                // First try to parse complete JSONB
                var summary = TryParseJson<AnalysisSummaryDetails>(analysis.Summary);
                if (summary != null)
                {
                    // Override with helper fields if they exist (helper fields have priority)
                    if (analysis.OverallHealthScore != 0)
                        summary.OverallHealthScore = analysis.OverallHealthScore;
                    if (!string.IsNullOrEmpty(analysis.PrimaryConcern))
                        summary.PrimaryConcern = analysis.PrimaryConcern;
                    if (analysis.CriticalIssuesCount != 0)
                        summary.CriticalIssuesCount = analysis.CriticalIssuesCount;
                    if (analysis.ConfidenceLevel.HasValue)
                        summary.ConfidenceLevel = analysis.ConfidenceLevel;
                    if (!string.IsNullOrEmpty(analysis.Prognosis))
                        summary.Prognosis = analysis.Prognosis;
                    if (!string.IsNullOrEmpty(analysis.EstimatedYieldImpact))
                        summary.EstimatedYieldImpact = analysis.EstimatedYieldImpact;
                    
                    return summary;
                }

                // Fallback: construct from helper fields and JSONB parts
                var secondaryConcerns = new List<string>();
                int? overallHealthScore = analysis.OverallHealthScore;
                string primaryConcern = analysis.PrimaryConcern;
                int? criticalIssuesCount = analysis.CriticalIssuesCount;
                decimal? confidenceLevel = analysis.ConfidenceLevel;
                string prognosis = analysis.Prognosis;
                string estimatedYieldImpact = analysis.EstimatedYieldImpact;

                if (!string.IsNullOrEmpty(analysis.Summary))
                {
                    try
                    {
                        var summaryData = JsonConvert.DeserializeObject<dynamic>(analysis.Summary);
                        
                        // Get values from JSONB if helper fields are null/empty
                        if (!overallHealthScore.HasValue && summaryData?.overall_health_score != null)
                        {
                            if (int.TryParse(summaryData.overall_health_score.ToString(), out int score))
                                overallHealthScore = score;
                        }
                        
                        if (string.IsNullOrEmpty(primaryConcern) && summaryData?.primary_concern != null)
                            primaryConcern = summaryData.primary_concern.ToString();
                        
                        if (!criticalIssuesCount.HasValue && summaryData?.critical_issues_count != null)
                        {
                            if (int.TryParse(summaryData.critical_issues_count.ToString(), out int count))
                                criticalIssuesCount = count;
                        }
                        
                        if (!confidenceLevel.HasValue && summaryData?.confidence_level != null)
                        {
                            if (decimal.TryParse(summaryData.confidence_level.ToString(), out decimal level))
                                confidenceLevel = level;
                        }
                        
                        if (string.IsNullOrEmpty(prognosis) && summaryData?.prognosis != null)
                            prognosis = summaryData.prognosis.ToString();
                        
                        if (string.IsNullOrEmpty(estimatedYieldImpact) && summaryData?.estimated_yield_impact != null)
                            estimatedYieldImpact = summaryData.estimated_yield_impact.ToString();
                        
                        // Always parse secondary_concerns array
                        if (summaryData?.secondary_concerns != null)
                        {
                            foreach (var concern in summaryData.secondary_concerns)
                                secondaryConcerns.Add(concern.ToString());
                        }
                    }
                    catch { /* Silent fallback */ }
                }

                return new AnalysisSummaryDetails
                {
                    OverallHealthScore = overallHealthScore,
                    PrimaryConcern = primaryConcern,
                    SecondaryConcerns = secondaryConcerns,
                    CriticalIssuesCount = criticalIssuesCount,
                    ConfidenceLevel = confidenceLevel,
                    Prognosis = prognosis,
                    EstimatedYieldImpact = estimatedYieldImpact
                };
            }

            private static PestDiseaseDetails CreateBasicPestDisease(Entities.Concrete.PlantAnalysis analysis)
            {
                // First try to parse complete JSONB
                var pestDisease = TryParseJson<PestDiseaseDetails>(analysis.PestDisease);
                if (pestDisease != null)
                {
                    // Override with helper fields if they exist (helper fields have priority)
                    if (analysis.AffectedAreaPercentage.HasValue)
                        pestDisease.AffectedAreaPercentage = analysis.AffectedAreaPercentage;
                    if (!string.IsNullOrEmpty(analysis.SpreadRisk))
                        pestDisease.SpreadRisk = analysis.SpreadRisk;
                    if (!string.IsNullOrEmpty(analysis.PrimaryIssue))
                        pestDisease.PrimaryIssue = analysis.PrimaryIssue;
                    
                    return pestDisease;
                }

                // Fallback: construct from helper fields and JSONB parts
                var pestsDetected = new List<PestDetails>();
                var diseasesDetected = new List<DiseaseDetails>();
                string damagePattern = null;
                decimal? affectedAreaPercentage = analysis.AffectedAreaPercentage;
                string spreadRisk = analysis.SpreadRisk;
                string primaryIssue = analysis.PrimaryIssue;

                if (!string.IsNullOrEmpty(analysis.PestDisease))
                {
                    try
                    {
                        var pestData = JsonConvert.DeserializeObject<dynamic>(analysis.PestDisease);
                        
                        damagePattern = pestData?.damage_pattern?.ToString();
                        
                        // Parse affected_area_percentage if helper field is null
                        if (!affectedAreaPercentage.HasValue && pestData?.affected_area_percentage != null)
                        {
                            if (decimal.TryParse(pestData.affected_area_percentage.ToString(), out decimal parsedPercentage))
                                affectedAreaPercentage = parsedPercentage;
                        }
                        
                        // Parse spread_risk if helper field is empty
                        if (string.IsNullOrEmpty(spreadRisk) && pestData?.spread_risk != null)
                            spreadRisk = pestData.spread_risk.ToString();
                        
                        // Parse primary_issue if helper field is empty
                        if (string.IsNullOrEmpty(primaryIssue) && pestData?.primary_issue != null)
                            primaryIssue = pestData.primary_issue.ToString();

                        // Parse pests_detected array
                        if (pestData?.pests_detected != null)
                        {
                            foreach (var pest in pestData.pests_detected)
                            {
                                var pestDetail = new PestDetails
                                {
                                    Name = pest?.type?.ToString(),
                                    Category = pest?.category?.ToString(),
                                    Severity = pest?.severity?.ToString(),
                                    AffectedParts = new List<string>()
                                };
                                
                                // Parse confidence if present
                                if (pest?.confidence != null)
                                {
                                    if (decimal.TryParse(pest.confidence.ToString(), out decimal confidence))
                                        pestDetail.Confidence = confidence;
                                }
                                
                                // Parse affected_parts array
                                if (pest?.affected_parts != null)
                                {
                                    foreach (var part in pest.affected_parts)
                                        pestDetail.AffectedParts.Add(part.ToString());
                                }
                                
                                pestsDetected.Add(pestDetail);
                            }
                        }

                        // Parse diseases_detected array
                        if (pestData?.diseases_detected != null)
                        {
                            foreach (var disease in pestData.diseases_detected)
                            {
                                var diseaseDetail = new DiseaseDetails
                                {
                                    Type = disease?.type?.ToString(),
                                    Category = disease?.category?.ToString(),
                                    Severity = disease?.severity?.ToString(),
                                    AffectedParts = new List<string>()
                                };
                                
                                // Parse confidence if present
                                if (disease?.confidence != null)
                                {
                                    if (decimal.TryParse(disease.confidence.ToString(), out decimal confidence))
                                        diseaseDetail.Confidence = confidence;
                                }
                                
                                // Parse affected_parts array
                                if (disease?.affected_parts != null)
                                {
                                    foreach (var part in disease.affected_parts)
                                        diseaseDetail.AffectedParts.Add(part.ToString());
                                }
                                
                                diseasesDetected.Add(diseaseDetail);
                            }
                        }
                    }
                    catch { /* Silent fallback */ }
                }

                return new PestDiseaseDetails
                {
                    PestsDetected = pestsDetected,
                    DiseasesDetected = diseasesDetected,
                    DamagePattern = damagePattern,
                    AffectedAreaPercentage = affectedAreaPercentage,
                    SpreadRisk = spreadRisk,
                    PrimaryIssue = primaryIssue
                };
            }

            private static EnvironmentalStressDetails CreateBasicEnvironmentalStress(Entities.Concrete.PlantAnalysis analysis)
            {
                // First try to parse complete JSONB
                var environmentalStress = TryParseJson<EnvironmentalStressDetails>(analysis.EnvironmentalStress);
                if (environmentalStress != null)
                {
                    // Override with helper field if it exists
                    if (!string.IsNullOrEmpty(analysis.PrimaryStressor))
                        environmentalStress.PrimaryStressor = analysis.PrimaryStressor;
                    
                    return environmentalStress;
                }

                // Fallback: construct from helper fields and JSONB parts
                string waterStatus = null, temperatureStress = null, lightStress = null;
                string physicalDamage = null, chemicalDamage = null, primaryStressor = analysis.PrimaryStressor;
                var physiologicalDisorders = new List<PhysiologicalDisorderDetails>();
                SoilHealthIndicatorDetails soilHealthIndicators = null;

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
                        
                        if (string.IsNullOrEmpty(primaryStressor))
                            primaryStressor = envData?.primary_stressor?.ToString();
                        
                        // Parse physiological_disorders array
                        if (envData?.physiological_disorders != null)
                        {
                            foreach (var disorder in envData.physiological_disorders)
                            {
                                physiologicalDisorders.Add(new PhysiologicalDisorderDetails
                                {
                                    Type = disorder?.type?.ToString(),
                                    Severity = disorder?.severity?.ToString(),
                                    Notes = disorder?.notes?.ToString()
                                });
                            }
                        }
                        
                        // Parse soil_health_indicators object
                        if (envData?.soil_health_indicators != null)
                        {
                            soilHealthIndicators = new SoilHealthIndicatorDetails
                            {
                                Salinity = envData.soil_health_indicators.salinity?.ToString(),
                                PhIssue = envData.soil_health_indicators.pH_issue?.ToString(),
                                OrganicMatter = envData.soil_health_indicators.organic_matter?.ToString()
                            };
                        }
                    }
                    catch { /* Silent fallback */ }
                }

                return new EnvironmentalStressDetails
                {
                    WaterStatus = waterStatus,
                    TemperatureStress = temperatureStress,
                    LightStress = lightStress,
                    PhysicalDamage = physicalDamage,
                    ChemicalDamage = chemicalDamage,
                    PhysiologicalDisorders = physiologicalDisorders,
                    SoilHealthIndicators = soilHealthIndicators,
                    PrimaryStressor = primaryStressor
                };
            }

            private static RiskAssessmentDetails CreateBasicRiskAssessment(Entities.Concrete.PlantAnalysis analysis)
            {
                // First try to parse complete JSONB
                var riskAssessment = TryParseJson<RiskAssessmentDetails>(analysis.RiskAssessment);
                if (riskAssessment != null)
                {
                    return riskAssessment;
                }

                // Fallback: construct from JSONB parts
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
                    catch { /* Silent fallback */ }
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
                // First try to parse complete JSONB
                var recommendations = TryParseJson<RecommendationsDetails>(analysis.Recommendations);
                if (recommendations != null)
                {
                    return recommendations;
                }

                // Fallback: construct from JSONB parts
                var immediate = new List<RecommendationItem>();
                var shortTerm = new List<RecommendationItem>();
                var preventive = new List<RecommendationItem>();
                var monitoring = new List<MonitoringItem>();
                ResourceEstimationDetails resourceEstimation = null;
                LocalizedRecommendationsDetails localizedRecommendations = null;

                if (!string.IsNullOrEmpty(analysis.Recommendations))
                {
                    try
                    {
                        var recData = JsonConvert.DeserializeObject<dynamic>(analysis.Recommendations);

                        // Parse immediate recommendations
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

                        // Parse short_term recommendations
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

                        // Parse preventive recommendations
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

                        // Parse monitoring recommendations
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

                        // Parse resource_estimation
                        if (recData?.resource_estimation != null)
                        {
                            resourceEstimation = new ResourceEstimationDetails
                            {
                                WaterRequiredLiters = recData.resource_estimation.water_required_liters?.ToString(),
                                FertilizerCostEstimateUsd = recData.resource_estimation.fertilizer_cost_estimate_usd?.ToString(),
                                LaborHoursEstimate = recData.resource_estimation.labor_hours_estimate?.ToString()
                            };
                        }

                        // Parse localized_recommendations
                        if (recData?.localized_recommendations != null)
                        {
                            var preferredPractices = new List<string>();
                            var restrictedMethods = new List<string>();

                            if (recData.localized_recommendations.preferred_practices != null)
                            {
                                foreach (var practice in recData.localized_recommendations.preferred_practices)
                                    preferredPractices.Add(practice.ToString());
                            }

                            if (recData.localized_recommendations.restricted_methods != null)
                            {
                                foreach (var method in recData.localized_recommendations.restricted_methods)
                                    restrictedMethods.Add(method.ToString());
                            }

                            localizedRecommendations = new LocalizedRecommendationsDetails
                            {
                                Region = recData.localized_recommendations.region?.ToString(),
                                PreferredPractices = preferredPractices,
                                RestrictedMethods = restrictedMethods
                            };
                        }
                    }
                    catch { /* Silent fallback */ }
                }

                return new RecommendationsDetails
                {
                    Immediate = immediate,
                    ShortTerm = shortTerm,
                    Preventive = preventive,
                    Monitoring = monitoring,
                    ResourceEstimation = resourceEstimation,
                    LocalizedRecommendations = localizedRecommendations
                };
            }

            private static List<CrossFactorInsightDetails> CreateBasicCrossFactorInsights(Entities.Concrete.PlantAnalysis analysis)
            {
                var insights = new List<CrossFactorInsightDetails>();

                if (!string.IsNullOrEmpty(analysis.CrossFactorInsights))
                {
                    try
                    {
                        var insightsData = JsonConvert.DeserializeObject<dynamic>(analysis.CrossFactorInsights);
                        
                        if (insightsData != null)
                        {
                            foreach (var insight in insightsData)
                            {
                                var insightDetail = new CrossFactorInsightDetails
                                {
                                    Insight = insight?.insight?.ToString(),
                                    AffectedAspects = new List<string>(),
                                    ImpactLevel = insight?.impact_level?.ToString()
                                };
                                
                                // Parse confidence
                                if (insight?.confidence != null)
                                {
                                    if (decimal.TryParse(insight.confidence.ToString(), out decimal confidence))
                                        insightDetail.Confidence = confidence;
                                }
                                
                                // Parse affected_aspects array
                                if (insight?.affected_aspects != null)
                                {
                                    foreach (var aspect in insight.affected_aspects)
                                        insightDetail.AffectedAspects.Add(aspect.ToString());
                                }
                                
                                insights.Add(insightDetail);
                            }
                        }
                    }
                    catch { /* Silent fallback */ }
                }

                return insights;
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

            private static ImageDetails GetImageInfo(Entities.Concrete.PlantAnalysis analysis)
            {
                // Try to parse ImageMetadata as ImageMetadataDto first
                var imageMetadata = TryParseJson<ImageMetadataDto>(analysis.ImageMetadata);
                
                if (imageMetadata != null && !string.IsNullOrEmpty(imageMetadata.ImageUrl))
                {
                    // ImageMetadata contains the URL, use it
                    return new ImageDetails 
                    { 
                        ImageUrl = imageMetadata.ImageUrl,
                        Format = "url",
                        UploadTimestamp = imageMetadata.UploadTimestamp
                    };
                }
                
                // Fallback to analysis.ImageUrl or try parsing as ImageDetails
                var imageInfo = TryParseJson<ImageDetails>(analysis.ImageMetadata);
                
                if (imageInfo == null)
                {
                    return new ImageDetails { ImageUrl = analysis.ImageUrl };
                }
                
                // If parsed ImageDetails doesn't have ImageUrl, use the one from analysis
                if (string.IsNullOrEmpty(imageInfo.ImageUrl))
                {
                    imageInfo.ImageUrl = analysis.ImageUrl;
                }
                
                return imageInfo;
            }
            
        }
    }
}