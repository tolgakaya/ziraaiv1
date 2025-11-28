using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Entities.Dtos
{
    public class PlantAnalysisAsyncResponseDto
    {
        [JsonProperty("plant_identification")]
        public PlantIdentification PlantIdentification { get; set; }
        
        [JsonProperty("health_assessment")]
        public HealthAssessment HealthAssessment { get; set; }
        
        [JsonProperty("nutrient_status")]
        public NutrientStatus NutrientStatus { get; set; }
        
        [JsonProperty("pest_disease")]
        public PestDisease PestDisease { get; set; }
        
        [JsonProperty("environmental_stress")]
        public EnvironmentalStress EnvironmentalStress { get; set; }
        
        [JsonProperty("cross_factor_insights")]
        public List<CrossFactorInsight> CrossFactorInsights { get; set; }
        
        [JsonProperty("recommendations")]
        public Recommendations Recommendations { get; set; }
        
        [JsonProperty("summary")]
        public AnalysisSummary Summary { get; set; }
        
        // Metadata
        [JsonProperty("analysis_id")]
        public string AnalysisId { get; set; }
        
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonProperty("user_id")]
        public int? UserId { get; set; } // Worker'da entity'ye atamak için
        
        [JsonProperty("farmer_id")]
        public string FarmerId { get; set; }
        
        [JsonProperty("sponsor_id")]
        public string SponsorId { get; set; }
        public int? SponsorUserId { get; set; }        // Actual sponsor user ID
        public int? SponsorshipCodeId { get; set; }    // SponsorshipCode table ID
        
        [JsonProperty("location")]
        public string Location { get; set; }
        
        [JsonProperty("gps_coordinates")]
        public GpsCoordinates GpsCoordinates { get; set; }
        
        [JsonProperty("altitude")]
        public int? Altitude { get; set; }
        
        [JsonProperty("field_id")]
        public string FieldId { get; set; }
        
        [JsonProperty("crop_type")]
        public string CropType { get; set; }
        
        [JsonProperty("planting_date")]
        public DateTime? PlantingDate { get; set; }
        
        [JsonProperty("expected_harvest_date")]
        public DateTime? ExpectedHarvestDate { get; set; }
        
        [JsonProperty("last_fertilization")]
        public DateTime? LastFertilization { get; set; }
        
        [JsonProperty("last_irrigation")]
        public DateTime? LastIrrigation { get; set; }
        
        [JsonProperty("previous_treatments")]
        public string[] PreviousTreatments { get; set; }
        
        [JsonProperty("weather_conditions")]
        public string WeatherConditions { get; set; }
        
        [JsonProperty("temperature")]
        public decimal? Temperature { get; set; }
        
        [JsonProperty("humidity")]
        public decimal? Humidity { get; set; }
        
        [JsonProperty("soil_type")]
        public string SoilType { get; set; }
        
        [JsonProperty("urgency_level")]
        public string UrgencyLevel { get; set; }
        
        [JsonProperty("notes")]
        public string Notes { get; set; }
        [JsonProperty("contact_info")]
        public ContactInfo ContactInfo { get; set; }
        
        [JsonProperty("additional_info")]
        public AdditionalInfoData AdditionalInfo { get; set; }

        // Risk Assessment
        [JsonProperty("risk_assessment")]
        public RiskAssessment RiskAssessment { get; set; }

        // Confidence Notes
        [JsonProperty("confidence_notes")]
        public List<ConfidenceNote> ConfidenceNotes { get; set; }

        // Farmer Friendly Summary
        [JsonProperty("farmer_friendly_summary")]
        public string FarmerFriendlySummary { get; set; }

        // Image URL and Path
        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        // Multi-Image URLs (for comprehensive analysis with up to 5 images)
        [JsonProperty("leaf_top_url")]
        public string LeafTopUrl { get; set; }

        [JsonProperty("leaf_bottom_url")]
        public string LeafBottomUrl { get; set; }

        [JsonProperty("plant_overview_url")]
        public string PlantOverviewUrl { get; set; }

        [JsonProperty("root_url")]
        public string RootUrl { get; set; }
        
        // Image and Processing Metadata
        [JsonProperty("image_metadata")]
        public ImageMetadata ImageMetadata { get; set; }
        
        [JsonProperty("rabbitmq_metadata")]
        public RabbitMQMetadata RabbitMQMetadata { get; set; }
        
        [JsonProperty("processing_metadata")]
        public ProcessingMetadata ProcessingMetadata { get; set; }

        [JsonProperty("token_usage")]
        public TokenUsage TokenUsage { get; set; }

        [JsonProperty("request_metadata")]
        public RequestMetadata RequestMetadata { get; set; }

        // Response Status
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("error")]
        public bool Error { get; set; }
        
        [JsonProperty("error_message")]
        public string ErrorMessage { get; set; }
        
        [JsonProperty("error_type")]
        public string ErrorType { get; set; }
    }

    public class PlantIdentification
    {
        [JsonProperty("species")]
        public string Species { get; set; }
        
        [JsonProperty("variety")]
        public string Variety { get; set; }
        
        [JsonProperty("growth_stage")]
        public string GrowthStage { get; set; }
        
        [JsonProperty("confidence")]
        public int Confidence { get; set; }
        
        [JsonProperty("identifying_features")]
        public string[] IdentifyingFeatures { get; set; }
        
        [JsonProperty("visible_parts")]
        public string[] VisibleParts { get; set; }
    }

    public class HealthAssessment
    {
        [JsonProperty("vigor_score")]
        public int VigorScore { get; set; }
        
        [JsonProperty("leaf_color")]
        public string LeafColor { get; set; }
        
        [JsonProperty("leaf_texture")]
        public string LeafTexture { get; set; }
        
        [JsonProperty("growth_pattern")]
        public string GrowthPattern { get; set; }
        
        [JsonProperty("structural_integrity")]
        public string StructuralIntegrity { get; set; }
        
        [JsonProperty("stress_indicators")]
        public string[] StressIndicators { get; set; }
        
        [JsonProperty("disease_symptoms")]
        public string[] DiseaseSymptoms { get; set; }
        
        [JsonProperty("severity")]
        public string Severity { get; set; }
    }

    public class NutrientStatus
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
        public string[] SecondaryDeficiencies { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }
    }

    public class PestDisease
    {
        [JsonProperty("pests_detected")]
        public PestDetectedDto[] PestsDetected { get; set; }

        [JsonProperty("diseases_detected")]
        public DiseaseDetectedDto[] DiseasesDetected { get; set; }

        [JsonProperty("damage_pattern")]
        public string DamagePattern { get; set; }

        [JsonProperty("affected_area_percentage")]
        public int AffectedAreaPercentage { get; set; }

        [JsonProperty("spread_risk")]
        public string SpreadRisk { get; set; }

        [JsonProperty("primary_issue")]
        public string PrimaryIssue { get; set; }
    }

    public class EnvironmentalStress
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

        [JsonProperty("soil_indicators")]
        public string SoilIndicators { get; set; }

        [JsonProperty("primary_stressor")]
        public string PrimaryStressor { get; set; }
    }

    public class CrossFactorInsight
    {
        [JsonProperty("insight")]
        public string Insight { get; set; }

        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }

        [JsonProperty("affected_aspects")]
        public string[] AffectedAspects { get; set; }

        [JsonProperty("impact_level")]
        public string ImpactLevel { get; set; }
    }

    public class Recommendations
    {
        [JsonProperty("immediate")]
        public Recommendation[] Immediate { get; set; }

        [JsonProperty("short_term")]
        public Recommendation[] ShortTerm { get; set; }

        [JsonProperty("preventive")]
        public Recommendation[] Preventive { get; set; }

        [JsonProperty("monitoring")]
        public MonitoringParameter[] Monitoring { get; set; }

        [JsonProperty("resource_estimation")]
        public ResourceEstimation ResourceEstimation { get; set; }

        [JsonProperty("localized_recommendations")]
        public LocalizedRecommendations LocalizedRecommendations { get; set; }
    }

    public class Recommendation
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

    public class MonitoringParameter
    {
        [JsonProperty("parameter")]
        public string Parameter { get; set; }

        [JsonProperty("frequency")]
        public string Frequency { get; set; }

        [JsonProperty("threshold")]
        public string Threshold { get; set; }
    }

    public class AnalysisSummary
    {
        [JsonProperty("overall_health_score")]
        public int OverallHealthScore { get; set; }
        
        [JsonProperty("primary_concern")]
        public string PrimaryConcern { get; set; }
        
        [JsonProperty("secondary_concerns")]
        public string[] SecondaryConcerns { get; set; }
        
        [JsonProperty("critical_issues_count")]
        public int CriticalIssuesCount { get; set; }
        
        [JsonProperty("confidence_level")]
        public int ConfidenceLevel { get; set; }
        
        [JsonProperty("prognosis")]
        public string Prognosis { get; set; }
        
        [JsonProperty("estimated_yield_impact")]
        public string EstimatedYieldImpact { get; set; }
    }

    public class ImageMetadata
    {
        public string Format { get; set; }
        public string URL { get; set; } // Image URL from N8N response
        public decimal? SizeBytes { get; set; }
        public decimal? SizeKb { get; set; }
        public decimal? SizeMb { get; set; }
        public int? Base64Length { get; set; }
        public DateTime? UploadTimestamp { get; set; }
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

    public class DiseaseDetectedDto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("affected_parts")]
        public string[] AffectedParts { get; set; }

        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }
    }

    public class PestDetectedDto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("affected_parts")]
        public string[] AffectedParts { get; set; }

        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }
    }

    public class TokenUsage
    {
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("cost_usd")]
        public decimal CostUsd { get; set; }

        [JsonProperty("cost_try")]
        public decimal CostTry { get; set; }
    }

    public class RequestMetadata
    {
        [JsonProperty("user_agent")]
        public string UserAgent { get; set; }

        [JsonProperty("ip_address")]
        public string IpAddress { get; set; }

        [JsonProperty("request_timestamp")]
        public DateTime RequestTimestamp { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("api_version")]
        public string ApiVersion { get; set; }
    }

    public class RiskAssessment
    {
        [JsonProperty("yield_loss_probability")]
        public string YieldLossProbability { get; set; }

        [JsonProperty("timeline_to_worsen")]
        public string TimelineToWorsen { get; set; }

        [JsonProperty("spread_potential")]
        public string SpreadPotential { get; set; }
    }

    public class ConfidenceNote
    {
        [JsonProperty("aspect")]
        public string Aspect { get; set; }

        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class ResourceEstimation
    {
        [JsonProperty("water_required_liters")]
        public string WaterRequiredLiters { get; set; }

        [JsonProperty("fertilizer_cost_estimate_usd")]
        public string FertilizerCostEstimateUsd { get; set; }

        [JsonProperty("labor_hours_estimate")]
        public string LaborHoursEstimate { get; set; }
    }

    public class LocalizedRecommendations
    {
        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("preferred_practices")]
        public string[] PreferredPractices { get; set; }

        [JsonProperty("restricted_methods")]
        public string[] RestrictedMethods { get; set; }
    }

}