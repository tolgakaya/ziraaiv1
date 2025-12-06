using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Entities.Dtos
{
    public class PlantAnalysisDetailDto
    {
        // Basic Information
        public int Id { get; set; }
        
        [JsonProperty("analysis_id")]
        public string AnalysisId { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime AnalysisDate { get; set; }
        
        public string AnalysisStatus { get; set; }
        
        // User & Sponsor Information
        public int? UserId { get; set; }
        
        [JsonProperty("farmer_id")]
        public string FarmerId { get; set; }
        
        [JsonProperty("sponsor_id", NullValueHandling = NullValueHandling.Include)]
        [JsonPropertyName("sponsor_id")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Never)]
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

        // Risk Assessment
        public RiskAssessmentDetails RiskAssessment { get; set; }

        // Confidence Notes
        public List<ConfidenceNoteDetails> ConfidenceNotes { get; set; }

        // Farmer Friendly Summary
        public string FarmerFriendlySummary { get; set; }

        // Token Usage
        public TokenUsageDetails TokenUsage { get; set; }

        // Request Metadata
        public RequestMetadataDetails RequestMetadata { get; set; }

        // Sponsorship Metadata (optional - only present if analysis was done with sponsorship code)
        public AnalysisTierMetadata SponsorshipMetadata { get; set; }

        // Success Status
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool? Error { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    // Sub-DTOs for organized structure
    public class PlantIdentificationDetails
    {
        [JsonProperty("species")]
        public string Species { get; set; }

        [JsonProperty("variety")]
        public string Variety { get; set; }

        [JsonProperty("growth_stage")]
        public string GrowthStage { get; set; }

        [JsonProperty("confidence")]
        public decimal? Confidence { get; set; }

        [JsonProperty("identifying_features")]
        public List<string> IdentifyingFeatures { get; set; }

        [JsonProperty("visible_parts")]
        public List<string> VisibleParts { get; set; }
    }
    
    public class HealthAssessmentDetails
    {
        [JsonProperty("vigor_score")]
        public int? VigorScore { get; set; }
        
        [JsonProperty("leaf_color")]
        public string LeafColor { get; set; }
        
        [JsonProperty("leaf_texture")]
        public string LeafTexture { get; set; }
        
        [JsonProperty("growth_pattern")]
        public string GrowthPattern { get; set; }
        
        [JsonProperty("structural_integrity")]
        public string StructuralIntegrity { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
        
        [JsonProperty("stress_indicators")]
        public List<string> StressIndicators { get; set; }
        
        [JsonProperty("disease_symptoms")]
        public List<string> DiseaseSymptoms { get; set; }
    }
    
    public class NutrientStatusDetails
    {
        [JsonProperty("nitrogen")]
        public string Nitrogen { get; set; }
        
        [JsonProperty("phosphorus")]
        public string Phosphorus { get; set; }
        
        [JsonProperty("potassium")]
        public string Potassium { get; set; }
        
        [JsonProperty("calcium")]
        public string Calcium { get; set; }
        
        [JsonProperty("magnesium")]
        public string Magnesium { get; set; }
        
        [JsonProperty("sulfur")]
        public string Sulfur { get; set; }
        
        [JsonProperty("iron")]
        public string Iron { get; set; }
        
        [JsonProperty("zinc")]
        public string Zinc { get; set; }
        
        [JsonProperty("manganese")]
        public string Manganese { get; set; }
        
        [JsonProperty("boron")]
        public string Boron { get; set; }
        
        [JsonProperty("copper")]
        public string Copper { get; set; }
        
        [JsonProperty("molybdenum")]
        public string Molybdenum { get; set; }
        
        [JsonProperty("chlorine")]
        public string Chlorine { get; set; }
        
        [JsonProperty("nickel")]
        public string Nickel { get; set; }
        
        [JsonProperty("primary_deficiency")]
        public string PrimaryDeficiency { get; set; }
        
        [JsonProperty("secondary_deficiencies")]
        public List<string> SecondaryDeficiencies { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
    }
    
    public class PestDiseaseDetails
    {
        [JsonProperty("pests_detected")]
        public List<PestDetails> PestsDetected { get; set; }
        
        [JsonProperty("diseases_detected")]
        public List<DiseaseDetails> DiseasesDetected { get; set; }
        
        [JsonProperty("damage_pattern")]
        public string DamagePattern { get; set; }
        
        [JsonProperty("affected_area_percentage")]
        public decimal? AffectedAreaPercentage { get; set; }
        
        [JsonProperty("spread_risk")]
        public string SpreadRisk { get; set; }
        
        [JsonProperty("primary_issue")]
        public string PrimaryIssue { get; set; }
    }
    
    public class PestDetails
    {
        [JsonProperty("type")]
        public string Name { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
        
        [JsonProperty("affected_parts")]
        public List<string> AffectedParts { get; set; }
        
        [JsonProperty("confidence")]
        public decimal? Confidence { get; set; }
    }
    
    public class DiseaseDetails
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("category")]
        public string Category { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
        
        [JsonProperty("affected_parts")]
        public List<string> AffectedParts { get; set; }
        
        [JsonProperty("confidence")]
        public decimal? Confidence { get; set; }
    }
    
    public class EnvironmentalStressDetails
    {
        [JsonProperty("water_status")]
        public string WaterStatus { get; set; }
        
        [JsonProperty("temperature_stress")]
        public string TemperatureStress { get; set; }
        
        [JsonProperty("light_stress")]
        public string LightStress { get; set; }
        
        [JsonProperty("physical_damage")]
        public string PhysicalDamage { get; set; }
        
        [JsonProperty("chemical_damage")]
        public string ChemicalDamage { get; set; }
        
        [JsonProperty("physiological_disorders")]
        public List<PhysiologicalDisorderDetails> PhysiologicalDisorders { get; set; }
        
        [JsonProperty("soil_health_indicators")]
        public SoilHealthIndicatorDetails SoilHealthIndicators { get; set; }
        
        [JsonProperty("primary_stressor")]
        public string PrimaryStressor { get; set; }
        
        // Backward compatibility
        [JsonProperty("soil_indicators")]
        public string SoilIndicators { get; set; }
    }
    
    public class PhysiologicalDisorderDetails
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
        
        [JsonProperty("notes")]
        public string Notes { get; set; }
    }
    
    public class SoilHealthIndicatorDetails
    {
        [JsonProperty("salinity")]
        public string Salinity { get; set; }
        
        [JsonProperty("pH_issue")]
        public string PhIssue { get; set; }
        
        [JsonProperty("organic_matter")]
        public string OrganicMatter { get; set; }
    }
    
    public class AnalysisSummaryDetails
    {
        [JsonProperty("overall_health_score")]
        public int? OverallHealthScore { get; set; }
        
        [JsonProperty("primary_concern")]
        public string PrimaryConcern { get; set; }
        
        [JsonProperty("secondary_concerns")]
        public List<string> SecondaryConcerns { get; set; }
        
        [JsonProperty("critical_issues_count")]
        public int? CriticalIssuesCount { get; set; }
        
        [JsonProperty("confidence_level")]
        public decimal? ConfidenceLevel { get; set; }
        
        [JsonProperty("prognosis")]
        public string Prognosis { get; set; }
        
        [JsonProperty("estimated_yield_impact")]
        public string EstimatedYieldImpact { get; set; }
    }
    
    public class CrossFactorInsightDetails
    {
        [JsonProperty("insight")]
        public string Insight { get; set; }
        
        [JsonProperty("confidence")]
        public decimal? Confidence { get; set; }
        
        [JsonProperty("affected_aspects")]
        public List<string> AffectedAspects { get; set; }
        
        [JsonProperty("impact_level")]
        public string ImpactLevel { get; set; }
    }
    
    public class RecommendationsDetails
    {
        [JsonProperty("immediate")]
        public List<RecommendationItem> Immediate { get; set; }
        
        [JsonProperty("short_term")]
        public List<RecommendationItem> ShortTerm { get; set; }
        
        [JsonProperty("preventive")]
        public List<RecommendationItem> Preventive { get; set; }
        
        [JsonProperty("monitoring")]
        public List<MonitoringItem> Monitoring { get; set; }
        
        [JsonProperty("resource_estimation")]
        public ResourceEstimationDetails ResourceEstimation { get; set; }
        
        [JsonProperty("localized_recommendations")]
        public LocalizedRecommendationsDetails LocalizedRecommendations { get; set; }
    }
    
    public class RecommendationItem
    {
        [JsonProperty("action")]
        public string Action { get; set; }
        
        [JsonProperty("details")]
        public string Details { get; set; }
        
        [JsonProperty("timeline")]
        public string Timeline { get; set; }
        
        [JsonProperty("priority")]
        public string Priority { get; set; }
    }
    
    public class MonitoringItem
    {
        [JsonProperty("parameter")]
        public string Parameter { get; set; }
        
        [JsonProperty("frequency")]
        public string Frequency { get; set; }
        
        [JsonProperty("threshold")]
        public string Threshold { get; set; }
    }
    
    public class ResourceEstimationDetails
    {
        [JsonProperty("water_required_liters")]
        public string WaterRequiredLiters { get; set; }
        
        [JsonProperty("fertilizer_cost_estimate_usd")]
        public string FertilizerCostEstimateUsd { get; set; }
        
        [JsonProperty("labor_hours_estimate")]
        public string LaborHoursEstimate { get; set; }
    }
    
    public class LocalizedRecommendationsDetails
    {
        [JsonProperty("region")]
        public string Region { get; set; }
        
        [JsonProperty("preferred_practices")]
        public List<string> PreferredPractices { get; set; }
        
        [JsonProperty("restricted_methods")]
        public List<string> RestrictedMethods { get; set; }
    }
    
    public class ImageDetails
    {
        // Existing single-image fields (backward compatible)
        public string ImageUrl { get; set; }
        public string ImagePath { get; set; }
        public string Format { get; set; }
        public long? SizeBytes { get; set; }
        public decimal? SizeKb { get; set; }
        public decimal? SizeMb { get; set; }
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

    public class RiskAssessmentDetails
    {
        [JsonProperty("yield_loss_probability")]
        public string YieldLossProbability { get; set; }
        
        [JsonProperty("timeline_to_worsen")]
        public string TimelineToWorsen { get; set; }
        
        [JsonProperty("spread_potential")]
        public string SpreadPotential { get; set; }
    }

    public class ConfidenceNoteDetails
    {
        [JsonProperty("aspect")]
        public string Aspect { get; set; }
        
        [JsonProperty("confidence")]
        public decimal? Confidence { get; set; }
        
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class TokenUsageDetails
    {
        public int? TotalTokens { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public decimal? CostUsd { get; set; }
        public decimal? CostTry { get; set; }
    }

    public class RequestMetadataDetails
    {
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public DateTime? RequestTimestamp { get; set; }
        public string RequestId { get; set; }
        public string ApiVersion { get; set; }
    }
}