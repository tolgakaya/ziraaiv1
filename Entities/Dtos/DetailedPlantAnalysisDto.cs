using Core.Entities;
using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class DetailedPlantAnalysisDto : IDto
    {
        public PlantIdentificationDto PlantIdentification { get; set; }
        public HealthAssessmentDto HealthAssessment { get; set; }
        public NutrientStatusDto NutrientStatus { get; set; }
        public PestDiseaseDto PestDisease { get; set; }
        public EnvironmentalStressDto EnvironmentalStress { get; set; }
        public List<CrossFactorInsightDto> CrossFactorInsights { get; set; }
        public RecommendationsDto Recommendations { get; set; }
        public SummaryDto Summary { get; set; }
        public string FullResponseJson { get; set; }
    }

    public class PlantIdentificationDto : IDto
    {
        public string Species { get; set; }
        public string Variety { get; set; }
        public string GrowthStage { get; set; }
        public decimal Confidence { get; set; }
        public List<string> IdentifyingFeatures { get; set; }
        public List<string> VisibleParts { get; set; }
    }

    public class HealthAssessmentDto : IDto
    {
        public int VigorScore { get; set; }
        public string LeafColor { get; set; }
        public string LeafTexture { get; set; }
        public string GrowthPattern { get; set; }
        public string StructuralIntegrity { get; set; }
        public List<string> StressIndicators { get; set; }
        public List<string> DiseaseSymptoms { get; set; }
        public string Severity { get; set; }
    }

    public class NutrientStatusDto : IDto
    {
        public string Nitrogen { get; set; }
        public string Phosphorus { get; set; }
        public string Potassium { get; set; }
        public string Calcium { get; set; }
        public string Magnesium { get; set; }
        public string Iron { get; set; }
        public string Sulfur { get; set; }
        public string Zinc { get; set; }
        public string Manganese { get; set; }
        public string Boron { get; set; }
        public string Copper { get; set; }
        public string Molybdenum { get; set; }
        public string Chlorine { get; set; }
        public string Nickel { get; set; }
        public string PrimaryDeficiency { get; set; }
        public List<string> SecondaryDeficiencies { get; set; }
        public string Severity { get; set; }
    }

    public class PestDiseaseDto : IDto
    {
        public List<string> PestsDetected { get; set; }
        public List<string> DiseasesDetected { get; set; }
        public string DamagePattern { get; set; }
        public decimal AffectedAreaPercentage { get; set; }
        public string SpreadRisk { get; set; }
        public string PrimaryIssue { get; set; }
    }

    public class EnvironmentalStressDto : IDto
    {
        public string WaterStatus { get; set; }
        public string TemperatureStress { get; set; }
        public string LightStress { get; set; }
        public string PhysicalDamage { get; set; }
        public string ChemicalDamage { get; set; }
        public string SoilIndicators { get; set; }
        public List<PhysiologicalDisorderDto> PhysiologicalDisorders { get; set; }
        public SoilHealthIndicatorsDto SoilHealthIndicators { get; set; }
        public string PrimaryStressor { get; set; }
    }

    public class CrossFactorInsightDto : IDto
    {
        public string Insight { get; set; }
        public decimal Confidence { get; set; }
        public List<string> AffectedAspects { get; set; }
        public string ImpactLevel { get; set; }
    }

    public class RecommendationsDto : IDto
    {
        public List<RecommendationDto> Immediate { get; set; }
        public List<RecommendationDto> ShortTerm { get; set; }
        public List<RecommendationDto> Preventive { get; set; }
        public List<MonitoringItemDto> Monitoring { get; set; }
        public ResourceEstimationDto ResourceEstimation { get; set; }
        public LocalizedRecommendationsDto LocalizedRecommendations { get; set; }
    }

    public class RecommendationDto : IDto
    {
        public string Action { get; set; }
        public string Details { get; set; }
        public string Timeline { get; set; }
        public string Priority { get; set; }
    }
    
    public class MonitoringItemDto : IDto
    {
        public string Parameter { get; set; }
        public string Frequency { get; set; }
        public string Threshold { get; set; }
    }

    public class SummaryDto : IDto
    {
        public int OverallHealthScore { get; set; }
        public string PrimaryConcern { get; set; }
        public List<string> SecondaryConcerns { get; set; }
        public int CriticalIssuesCount { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public string Prognosis { get; set; }
        public string EstimatedYieldImpact { get; set; }
    }

    public class RiskAssessmentDto : IDto
    {
        public string YieldLossProbability { get; set; }
        public string TimelineToWorsen { get; set; }
        public string SpreadPotential { get; set; }
    }

    public class ConfidenceNoteDto : IDto
    {
        public string Aspect { get; set; }
        public decimal Confidence { get; set; }
        public string Reason { get; set; }
    }

    public class ImageMetadataDto : IDto
    {
        // Existing single-image fields (backward compatible)
        public string Source { get; set; }
        public string ImageUrl { get; set; }
        public bool HasImageExtension { get; set; }
        public DateTime? UploadTimestamp { get; set; }
        
        // 🆕 Multi-image support (optional fields - null for single-image analyses)
        public int? TotalImages { get; set; }
        public List<string> ImagesProvided { get; set; }
        public bool? HasLeafTop { get; set; }
        public bool? HasLeafBottom { get; set; }
        public bool? HasPlantOverview { get; set; }
        public bool? HasRoot { get; set; }
        
        // 🆕 Additional image URLs (null for single-image analyses)
        public string LeafTopImageUrl { get; set; }
        public string LeafBottomImageUrl { get; set; }
        public string PlantOverviewImageUrl { get; set; }
        public string RootImageUrl { get; set; }
    }

    public class PhysiologicalDisorderDto : IDto
    {
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Notes { get; set; }
    }

    public class SoilHealthIndicatorsDto : IDto
    {
        public string Salinity { get; set; }
        public string PhIssue { get; set; }
        public string OrganicMatter { get; set; }
    }

    public class ResourceEstimationDto : IDto
    {
        public string WaterRequiredLiters { get; set; }
        public string FertilizerCostEstimateUsd { get; set; }
        public string LaborHoursEstimate { get; set; }
    }

    public class LocalizedRecommendationsDto : IDto
    {
        public string Region { get; set; }
        public List<string> PreferredPractices { get; set; }
        public List<string> RestrictedMethods { get; set; }
    }

    public class ContactInfoDto : IDto
    {
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    public class AdditionalInfoDto : IDto
    {
        public string IrrigationMethod { get; set; }
        public bool? Greenhouse { get; set; }
        public bool? OrganicCertified { get; set; }
    }
}