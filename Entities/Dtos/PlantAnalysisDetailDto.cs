using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class PlantAnalysisDetailDto
    {
        // Basic Information
        public int Id { get; set; }
        public string AnalysisId { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string AnalysisStatus { get; set; }
        
        // User & Sponsor Information
        public int? UserId { get; set; }
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public int? SponsorUserId { get; set; }
        public int? SponsorshipCodeId { get; set; }
        
        // Location Information
        public string Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? Altitude { get; set; }
        
        // Field & Crop Information
        public string FieldId { get; set; }
        public string CropType { get; set; }
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        public List<string> PreviousTreatments { get; set; }
        
        // Environmental Conditions
        public string WeatherConditions { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string SoilType { get; set; }
        
        // Contact Information
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        
        // Analysis Request Details
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public AdditionalInfoData AdditionalInfo { get; set; }
        
        // Plant Identification Details
        public PlantIdentificationDetails PlantIdentification { get; set; }
        
        // Health Assessment Details
        public HealthAssessmentDetails HealthAssessment { get; set; }
        
        // Nutrient Status Details
        public NutrientStatusDetails NutrientStatus { get; set; }
        
        // Pest & Disease Details
        public PestDiseaseDetails PestDisease { get; set; }
        
        // Environmental Stress Details
        public EnvironmentalStressDetails EnvironmentalStress { get; set; }
        
        // Summary & Insights
        public AnalysisSummaryDetails Summary { get; set; }
        
        // Cross-Factor Insights
        public List<CrossFactorInsightDetails> CrossFactorInsights { get; set; }
        
        // Recommendations
        public RecommendationsDetails Recommendations { get; set; }
        
        // Image Information
        public ImageDetails ImageInfo { get; set; }
        
        // Processing Information
        public ProcessingDetails ProcessingInfo { get; set; }
        
        // Success Status
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool? Error { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    // Sub-DTOs for organized structure
    public class PlantIdentificationDetails
    {
        public string Species { get; set; }
        public string Variety { get; set; }
        public string GrowthStage { get; set; }
        public decimal? Confidence { get; set; }
        public List<string> IdentifyingFeatures { get; set; }
        public List<string> VisibleParts { get; set; }
    }
    
    public class HealthAssessmentDetails
    {
        public int? VigorScore { get; set; }
        public string LeafColor { get; set; }
        public string LeafTexture { get; set; }
        public string GrowthPattern { get; set; }
        public string StructuralIntegrity { get; set; }
        public string Severity { get; set; }
        public List<string> StressIndicators { get; set; }
        public List<string> DiseaseSymptoms { get; set; }
    }
    
    public class NutrientStatusDetails
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
    
    public class PestDiseaseDetails
    {
        public List<PestDetails> PestsDetected { get; set; }
        public List<DiseaseDetails> DiseasesDetected { get; set; }
        public string DamagePattern { get; set; }
        public decimal? AffectedAreaPercentage { get; set; }
        public string SpreadRisk { get; set; }
        public string PrimaryIssue { get; set; }
    }
    
    public class PestDetails
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public List<string> AffectedParts { get; set; }
    }
    
    public class DiseaseDetails
    {
        public string Type { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public List<string> AffectedParts { get; set; }
    }
    
    public class EnvironmentalStressDetails
    {
        public string WaterStatus { get; set; }
        public string TemperatureStress { get; set; }
        public string LightStress { get; set; }
        public string PhysicalDamage { get; set; }
        public string ChemicalDamage { get; set; }
        public string SoilIndicators { get; set; }
        public string PrimaryStressor { get; set; }
    }
    
    public class AnalysisSummaryDetails
    {
        public int? OverallHealthScore { get; set; }
        public string PrimaryConcern { get; set; }
        public List<string> SecondaryConcerns { get; set; }
        public int? CriticalIssuesCount { get; set; }
        public string Prognosis { get; set; }
        public string EstimatedYieldImpact { get; set; }
        public decimal? ConfidenceLevel { get; set; }
    }
    
    public class CrossFactorInsightDetails
    {
        public string Insight { get; set; }
        public decimal? Confidence { get; set; }
        public List<string> AffectedAspects { get; set; }
        public string ImpactLevel { get; set; }
    }
    
    public class RecommendationsDetails
    {
        public List<RecommendationItem> Immediate { get; set; }
        public List<RecommendationItem> ShortTerm { get; set; }
        public List<RecommendationItem> Preventive { get; set; }
        public List<MonitoringItem> Monitoring { get; set; }
    }
    
    public class RecommendationItem
    {
        public string Action { get; set; }
        public string Details { get; set; }
        public string Timeline { get; set; }
        public string Priority { get; set; }
    }
    
    public class MonitoringItem
    {
        public string Parameter { get; set; }
        public string Frequency { get; set; }
        public string Threshold { get; set; }
    }
    
    public class ImageDetails
    {
        public string ImageUrl { get; set; }
        public string ImagePath { get; set; }
        public string Format { get; set; }
        public long? SizeBytes { get; set; }
        public decimal? SizeKb { get; set; }
        public decimal? SizeMb { get; set; }
        public DateTime? UploadTimestamp { get; set; }
    }
    
    public class ProcessingDetails
    {
        public string AiModel { get; set; }
        public string WorkflowVersion { get; set; }
        public DateTime? ProcessingTimestamp { get; set; }
        public long? ProcessingTimeMs { get; set; }
        public bool? ParseSuccess { get; set; }
        public string CorrelationId { get; set; }
        public int? RetryCount { get; set; }
    }
}