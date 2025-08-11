using Core.Entities;
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
}