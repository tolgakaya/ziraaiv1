using Core.Entities;
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
        public string SponsorId { get; set; }
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
        
        // Plant Identification
        public string PlantSpecies { get; set; }
        public string PlantVariety { get; set; }
        public string GrowthStage { get; set; }
        public decimal? IdentificationConfidence { get; set; }
        
        // Health Assessment
        public int? VigorScore { get; set; }
        public string HealthSeverity { get; set; }
        public string StressIndicators { get; set; } // JSON array
        public string DiseaseSymptoms { get; set; } // JSON array
        
        // Nutrient Status
        public string PrimaryDeficiency { get; set; }
        public string NutrientStatus { get; set; } // JSON object
        
        // Summary
        public int? OverallHealthScore { get; set; }
        public string PrimaryConcern { get; set; }
        public string Prognosis { get; set; }
        public string EstimatedYieldImpact { get; set; }
        public decimal? ConfidenceLevel { get; set; }
        
        // Processing Metadata
        public string AiModel { get; set; }
        public decimal? TotalTokens { get; set; }
        public decimal? TotalCostUsd { get; set; }
        public decimal? TotalCostTry { get; set; }
        public decimal? ImageSizeKb { get; set; }
        
        // Full Response Storage
        public string DetailedAnalysisData { get; set; } // Full analysis JSON
        public string Recommendations { get; set; } // JSON object
        public string CrossFactorInsights { get; set; } // JSON array
        
        // Legacy fields for backward compatibility
        public string PlantType { get; set; }
        public string ElementDeficiencies { get; set; }
        public string Diseases { get; set; }
        public string Pests { get; set; }
        public string AnalysisResult { get; set; }
        public string N8nWebhookResponse { get; set; }
    }
}