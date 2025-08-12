using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class PlantAnalysisAsyncResponseDto
    {
        public PlantIdentification PlantIdentification { get; set; }
        public HealthAssessment HealthAssessment { get; set; }
        public NutrientStatus NutrientStatus { get; set; }
        public PestDisease PestDisease { get; set; }
        public EnvironmentalStress EnvironmentalStress { get; set; }
        public List<CrossFactorInsight> CrossFactorInsights { get; set; }
        public Recommendations Recommendations { get; set; }
        public AnalysisSummary Summary { get; set; }
        
        // Metadata
        public string AnalysisId { get; set; }
        public DateTime Timestamp { get; set; }
        public int? UserId { get; set; } // Worker'da entity'ye atamak için
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public string Location { get; set; }
        public GpsCoordinates GpsCoordinates { get; set; }
        public int? Altitude { get; set; }
        public string FieldId { get; set; }
        public string CropType { get; set; }
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        public string[] PreviousTreatments { get; set; }
        public string WeatherConditions { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string SoilType { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public AdditionalInfoData AdditionalInfo { get; set; }
        
        // Image and Processing Metadata
        public ImageMetadata ImageMetadata { get; set; }
        public RabbitMQMetadata RabbitMQMetadata { get; set; }
        public ProcessingMetadata ProcessingMetadata { get; set; }
        
        // Response Status
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }
    }

    public class PlantIdentification
    {
        public string Species { get; set; }
        public string Variety { get; set; }
        public string GrowthStage { get; set; }
        public int Confidence { get; set; }
        public string[] IdentifyingFeatures { get; set; }
        public string[] VisibleParts { get; set; }
    }

    public class HealthAssessment
    {
        public int VigorScore { get; set; }
        public string LeafColor { get; set; }
        public string LeafTexture { get; set; }
        public string GrowthPattern { get; set; }
        public string StructuralIntegrity { get; set; }
        public string[] StressIndicators { get; set; }
        public string[] DiseaseSymptoms { get; set; }
        public string Severity { get; set; }
    }

    public class NutrientStatus
    {
        public string Nitrogen { get; set; }
        public string Phosphorus { get; set; }
        public string Potassium { get; set; }
        public string Calcium { get; set; }
        public string Magnesium { get; set; }
        public string Iron { get; set; }
        public string PrimaryDeficiency { get; set; }
        public string[] SecondaryDeficiencies { get; set; }
        public string Severity { get; set; }
    }

    public class PestDisease
    {
        public string[] PestsDetected { get; set; }
        public string[] DiseasesDetected { get; set; }
        public string DamagePattern { get; set; }
        public int AffectedAreaPercentage { get; set; }
        public string SpreadRisk { get; set; }
        public string PrimaryIssue { get; set; }
    }

    public class EnvironmentalStress
    {
        public string WaterStatus { get; set; }
        public string TemperatureStress { get; set; }
        public string LightStress { get; set; }
        public string PhysicalDamage { get; set; }
        public string ChemicalDamage { get; set; }
        public string SoilIndicators { get; set; }
        public string PrimaryStressor { get; set; }
    }

    public class CrossFactorInsight
    {
        public string Insight { get; set; }
        public decimal Confidence { get; set; }
        public string[] AffectedAspects { get; set; }
        public string ImpactLevel { get; set; }
    }

    public class Recommendations
    {
        public Recommendation[] Immediate { get; set; }
        public Recommendation[] ShortTerm { get; set; }
        public Recommendation[] Preventive { get; set; }
        public MonitoringParameter[] Monitoring { get; set; }
    }

    public class Recommendation
    {
        public string Action { get; set; }
        public string Details { get; set; }
        public string Timeline { get; set; }
        public string Priority { get; set; }
    }

    public class MonitoringParameter
    {
        public string Parameter { get; set; }
        public string Frequency { get; set; }
        public string Threshold { get; set; }
    }

    public class AnalysisSummary
    {
        public int OverallHealthScore { get; set; }
        public string PrimaryConcern { get; set; }
        public string[] SecondaryConcerns { get; set; }
        public int CriticalIssuesCount { get; set; }
        public int ConfidenceLevel { get; set; }
        public string Prognosis { get; set; }
        public string EstimatedYieldImpact { get; set; }
    }

    public class ImageMetadata
    {
        public string Format { get; set; }
        public decimal SizeBytes { get; set; }
        public decimal SizeKb { get; set; }
        public decimal SizeMb { get; set; }
        public int Base64Length { get; set; }
        public DateTime UploadTimestamp { get; set; }
    }

    public class RabbitMQMetadata
    {
        public string CorrelationId { get; set; }
        public string ResponseQueue { get; set; }
        public string CallbackUrl { get; set; }
        public string Priority { get; set; }
        public int RetryCount { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string MessageId { get; set; }
        public string RoutingKey { get; set; }
    }

    public class ProcessingMetadata
    {
        public bool ParseSuccess { get; set; }
        public DateTime ProcessingTimestamp { get; set; }
        public string AiModel { get; set; }
        public string WorkflowVersion { get; set; }
        public DateTime ReceivedAt { get; set; }
        public int ProcessingTimeMs { get; set; }
        public int RetryCount { get; set; }
        public string Priority { get; set; }
    }
}