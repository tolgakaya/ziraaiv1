using Core.Entities;
using Core.Entities.Concrete;
using System;

namespace Entities.Concrete
{
    public class PlantAnalysis : IEntity
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }
        // Removed ImageBase64 for performance - images are now stored as files
        public DateTime AnalysisDate { get; set; }
        public int? UserId { get; set; }
        public string AnalysisStatus { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        
        // N8N Analysis Response Fields
        public string AnalysisId { get; set; }
        public string FarmerId { get; set; }
        
        // Sponsorship Tracking
        public string SponsorId { get; set; } // Legacy field for backward compatibility
        public int? SponsorshipCodeId { get; set; } // Which sponsorship code was used for this analysis
        public int? SponsorUserId { get; set; } // Sponsor company user ID
        public string Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? Altitude { get; set; }
        public string FieldId { get; set; }
        public string CropType { get; set; }
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        public string PreviousTreatments { get; set; } // JSON array
        public string WeatherConditions { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string SoilType { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string AdditionalInfo { get; set; } // JSON object
        
        // Plant Identification - Enhanced
        public string PlantSpecies { get; set; }
        public string PlantVariety { get; set; }
        public string GrowthStage { get; set; }
        public decimal? IdentificationConfidence { get; set; }
        public string IdentifyingFeatures { get; set; } // JSON array - NEW
        public string VisibleParts { get; set; } // JSON array - NEW
        
        // Health Assessment - Enhanced
        public int? VigorScore { get; set; }
        public string LeafColor { get; set; } // NEW
        public string LeafTexture { get; set; } // NEW
        public string GrowthPattern { get; set; } // NEW
        public string StructuralIntegrity { get; set; } // NEW
        public string HealthSeverity { get; set; }
        public string StressIndicators { get; set; } // JSON array
        public string DiseaseSymptoms { get; set; } // JSON array
        
        // Nutrient Status - Enhanced
        public string NitrogenStatus { get; set; } // NEW
        public string PhosphorusStatus { get; set; } // NEW
        public string PotassiumStatus { get; set; } // NEW
        public string CalciumStatus { get; set; } // NEW
        public string MagnesiumStatus { get; set; } // NEW
        public string IronStatus { get; set; } // NEW
        public string PrimaryDeficiency { get; set; }
        public string SecondaryDeficiencies { get; set; } // JSON array - NEW
        public string NutrientSeverity { get; set; } // NEW
        public string NutrientStatus { get; set; } // JSON object - Full details
        
        // Pest & Disease - Enhanced
        public string PestsDetected { get; set; } // JSON array - NEW
        public string DiseasesDetected { get; set; } // JSON array - NEW
        public string DamagePattern { get; set; } // NEW
        public decimal? AffectedAreaPercentage { get; set; } // NEW
        public string SpreadRisk { get; set; } // NEW
        public string PrimaryIssue { get; set; } // NEW
        
        // Environmental Stress - NEW Section
        public string WaterStatus { get; set; } // NEW
        public string TemperatureStress { get; set; } // NEW
        public string LightStress { get; set; } // NEW
        public string PhysicalDamage { get; set; } // NEW
        public string ChemicalDamage { get; set; } // NEW
        public string SoilIndicators { get; set; } // NEW
        public string PrimaryStressor { get; set; } // NEW
        
        // Summary
        public int? OverallHealthScore { get; set; }
        public string PrimaryConcern { get; set; }
        public string SecondaryConcerns { get; set; } // JSON array - NEW
        public int? CriticalIssuesCount { get; set; } // NEW
        public string Prognosis { get; set; }
        public string EstimatedYieldImpact { get; set; }
        public decimal? ConfidenceLevel { get; set; }
        
        // Cross-Factor Insights - Enhanced
        public string CrossFactorInsights { get; set; } // JSON array with detailed structure
        
        // Recommendations - Enhanced Structure
        public string ImmediateRecommendations { get; set; } // JSON array - NEW
        public string ShortTermRecommendations { get; set; } // JSON array - NEW
        public string PreventiveRecommendations { get; set; } // JSON array - NEW
        public string MonitoringRecommendations { get; set; } // JSON array - NEW
        public string Recommendations { get; set; } // JSON object - Full structure
        
        // Image Metadata - NEW
        public string ImageFormat { get; set; } // NEW
        public string ImageUrl { get; set; } // NEW
        public long? ImageSizeBytes { get; set; } // NEW
        public decimal? ImageSizeKb { get; set; }
        public decimal? ImageSizeMb { get; set; } // NEW
        public DateTime? ImageUploadTimestamp { get; set; } // NEW
        
        // RabbitMQ Metadata - NEW
        public string CorrelationId { get; set; } // NEW
        public string ResponseQueue { get; set; } // NEW
        public string MessagePriority { get; set; } // NEW
        public int? RetryCount { get; set; } // NEW
        public DateTime? ReceivedAt { get; set; } // NEW
        public string MessageId { get; set; } // NEW
        public string RoutingKey { get; set; } // NEW
        
        // Processing Metadata - Enhanced
        public string AiModel { get; set; }
        public string WorkflowVersion { get; set; } // NEW
        public DateTime? ProcessingTimestamp { get; set; } // NEW
        public long? ProcessingTimeMs { get; set; } // NEW
        public bool? ParseSuccess { get; set; } // NEW
        public decimal? TotalTokens { get; set; }
        public decimal? TotalCostUsd { get; set; }
        public decimal? TotalCostTry { get; set; }
        
        // Response Status - NEW
        public bool? Success { get; set; } // NEW
        public string Message { get; set; } // NEW
        public bool? Error { get; set; } // NEW
        public string ErrorMessage { get; set; } // NEW
        public string ErrorType { get; set; } // NEW
        
        // Full Response Storage
        public string DetailedAnalysisData { get; set; } // Full analysis JSON
        
        // Legacy fields for backward compatibility
        public string PlantType { get; set; }
        public string ElementDeficiencies { get; set; }
        public string Diseases { get; set; }
        public string Pests { get; set; }
        public string AnalysisResult { get; set; }
        public string N8nWebhookResponse { get; set; }
        
        // Navigation properties
        public virtual SponsorshipCode SponsorshipCode { get; set; }
        public virtual User SponsorUser { get; set; }
    }
}