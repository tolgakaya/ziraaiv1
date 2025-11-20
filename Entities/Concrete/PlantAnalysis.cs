using Core.Entities;
using Core.Entities.Concrete;
using System;

namespace Entities.Concrete
{
    public class PlantAnalysis : IEntity
    {
        // Primary Key
        public int Id { get; set; }

        // Basic Information
        public DateTime AnalysisDate { get; set; }
        public string AnalysisStatus { get; set; } = "pending";
        public bool Status { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Analysis Identification
        public string AnalysisId { get; set; }
        public DateTime Timestamp { get; set; }

        // User & Sponsor Information
        public int? UserId { get; set; }
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public int? SponsorshipCodeId { get; set; }
        public int? SponsorUserId { get; set; }
        
        // Dealer Distribution (NEW)
        public int? DealerId { get; set; } // Dealer who distributed the code (NULL = direct sponsor distribution)
        
        // Sponsor Attribution (tracks which sponsor was active during analysis)
        public int? ActiveSponsorshipId { get; set; } // FK to UserSubscription that was active
        public int? SponsorCompanyId { get; set; } // Denormalized sponsor company ID for performance

        // Location Information
        public string Location { get; set; }
        public string GpsCoordinates { get; set; } // JSONB
        public decimal? Latitude { get; set; } // Helper field for GPS coordinates
        public decimal? Longitude { get; set; } // Helper field for GPS coordinates
        public int? Altitude { get; set; }

        // Field & Crop Information
        public string FieldId { get; set; }
        public string CropType { get; set; }
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        public string PreviousTreatments { get; set; } // JSONB

        // Environmental Conditions
        public string WeatherConditions { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string SoilType { get; set; }

        // Analysis Request Details
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public string ContactInfo { get; set; } // Text field instead of separate fields
        public string ContactPhone { get; set; } // Helper field for contact info
        public string ContactEmail { get; set; } // Helper field for contact info
        public string AdditionalInfo { get; set; } // JSONB

        // Plant Identification - JSONB + Helper Fields
        public string PlantIdentification { get; set; } // JSONB
        public string PlantSpecies { get; set; } // Helper field
        public string PlantVariety { get; set; }
        public string GrowthStage { get; set; }
        public decimal? IdentificationConfidence { get; set; }

        // Health Assessment - JSONB + Helper Fields
        public string HealthAssessment { get; set; } // JSONB
        public int? VigorScore { get; set; } // Helper field
        public string HealthSeverity { get; set; }
        public string StressIndicators { get; set; } // Helper field - JSON string
        public string DiseaseSymptoms { get; set; } // Helper field - JSON string

        // Nutrient Status - JSONB + Helper Fields
        public string NutrientStatus { get; set; } // JSONB
        public string Nitrogen { get; set; } // Individual nutrient fields
        public string Phosphorus { get; set; }
        public string Potassium { get; set; }
        public string Calcium { get; set; }
        public string Magnesium { get; set; }
        public string Sulfur { get; set; }
        public string Iron { get; set; }
        public string Zinc { get; set; }
        public string Manganese { get; set; }
        public string Boron { get; set; }
        public string Copper { get; set; }
        public string Molybdenum { get; set; }
        public string Chlorine { get; set; }
        public string Nickel { get; set; }
        public string PrimaryDeficiency { get; set; }
        public string NutrientSeverity { get; set; }

        // Pest & Disease - JSONB + Helper Fields
        public string PestDisease { get; set; } // JSONB
        public int? AffectedAreaPercentage { get; set; } // Helper field
        public string SpreadRisk { get; set; }
        public string PrimaryIssue { get; set; }

        // Environmental Stress - JSONB + Helper Field
        public string EnvironmentalStress { get; set; } // JSONB
        public string PrimaryStressor { get; set; } // Helper field

        // Cross-Factor Insights
        public string CrossFactorInsights { get; set; } // JSONB

        // Risk Assessment
        public string RiskAssessment { get; set; } // JSONB

        // Recommendations
        public string Recommendations { get; set; } // JSONB

        // Summary - JSONB + Helper Fields
        public string Summary { get; set; } // JSONB
        public int OverallHealthScore { get; set; } = 0; // Helper field
        public string PrimaryConcern { get; set; }
        public int? CriticalIssuesCount { get; set; }
        public decimal? ConfidenceLevel { get; set; }
        public string Prognosis { get; set; }
        public string EstimatedYieldImpact { get; set; }

        // Confidence Notes
        public string ConfidenceNotes { get; set; } // JSONB

        // Farmer-Friendly Summary
        public string FarmerFriendlySummary { get; set; } = "";

        // Image Metadata
        public string ImageMetadata { get; set; } // JSONB
        public string ImageUrl { get; set; } = "";

        // Request Metadata
        public string RequestMetadata { get; set; } // JSONB

        // Token Usage
        public string TokenUsage { get; set; } // JSONB

        // Processing Metadata - JSONB + Helper Fields
        public string ProcessingMetadata { get; set; } // JSONB
        public string AiModel { get; set; } = "";
        public string WorkflowVersion { get; set; } = "";
        public int TotalTokens { get; set; } = 0; // Helper field
        public decimal TotalCostUsd { get; set; } = 0;
        public decimal TotalCostTry { get; set; } = 0;
        public DateTime ProcessingTimestamp { get; set; } = DateTime.Now;

        // Full Response Storage
        public string DetailedAnalysisData { get; set; } // JSONB

        // Legacy and Helper fields for backward compatibility
        public string ImagePath { get; set; } // Helper field
        public decimal? ImageSizeKb { get; set; } // Helper field
        public string PlantType { get; set; } // Legacy field
        public string ElementDeficiencies { get; set; } // Legacy field - JSON string
        public string Diseases { get; set; } // Legacy field - JSON string
        public string Pests { get; set; } // Legacy field - JSON string
        public string AnalysisResult { get; set; } // Legacy field - JSON string
        public string N8nWebhookResponse { get; set; } // Legacy field - JSON string

        // Admin On-Behalf-Of (OBO) Tracking
        /// <summary>
        /// Admin user ID who created this analysis on behalf of a farmer
        /// </summary>
        public int? CreatedByAdminId { get; set; }

        /// <summary>
        /// Indicates if this analysis was created by an admin on behalf of a user
        /// </summary>
        public bool IsOnBehalfOf { get; set; } = false;

        // Navigation properties
        public virtual SponsorshipCode SponsorshipCode { get; set; }
        public virtual User SponsorUser { get; set; }
        public virtual UserSubscription ActiveSponsorship { get; set; }
    }
}