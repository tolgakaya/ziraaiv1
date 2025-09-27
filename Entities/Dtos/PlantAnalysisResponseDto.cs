using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Entities.Dtos
{
    public class PlantAnalysisResponseDto : IDto
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Status { get; set; }
        public int? UserId { get; set; }
        
        // Core Analysis Fields
        public string AnalysisId { get; set; }
        public string FarmerId { get; set; }
        public string SponsorId { get; set; }
        public int? SponsorUserId { get; set; }        // Actual sponsor user ID
        public int? SponsorshipCodeId { get; set; }    // SponsorshipCode table ID
        public string Location { get; set; }
        public GpsCoordinates GpsCoordinates { get; set; }
        public int? Altitude { get; set; }
        public string FieldId { get; set; }
        public string CropType { get; set; }
        public DateTime? PlantingDate { get; set; }
        public DateTime? ExpectedHarvestDate { get; set; }
        public DateTime? LastFertilization { get; set; }
        public DateTime? LastIrrigation { get; set; }
        public List<string> PreviousTreatments { get; set; }
        public string WeatherConditions { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public string SoilType { get; set; }
        public string UrgencyLevel { get; set; }
        public string Notes { get; set; }
        public ContactInfoDto ContactInfo { get; set; }
        public AdditionalInfoDto AdditionalInfo { get; set; }
        public string ImageUrl { get; set; }
        
        // Plant Identification
        public PlantIdentificationDto PlantIdentification { get; set; }

        // Health Assessment
        public HealthAssessmentDto HealthAssessment { get; set; }

        // Nutrient Status
        public NutrientStatusDto NutrientStatus { get; set; }

        // Pest & Disease
        public PestDiseaseDto PestDisease { get; set; }

        // Environmental Stress
        public EnvironmentalStressDto EnvironmentalStress { get; set; }

        // Recommendations
        public RecommendationsDto Recommendations { get; set; }

        // Cross Factor Insights
        public List<CrossFactorInsightDto> CrossFactorInsights { get; set; }

        // Summary
        public SummaryDto Summary { get; set; }

        // Risk Assessment
        public RiskAssessmentDto RiskAssessment { get; set; }

        // Confidence Notes
        public List<ConfidenceNoteDto> ConfidenceNotes { get; set; }

        // Farmer Friendly Summary
        public string FarmerFriendlySummary { get; set; }

        // Image Metadata
        public ImageMetadataDto ImageMetadata { get; set; }

        // Detailed Analysis Data (full JSON for reference)
        public DetailedPlantAnalysisDto DetailedAnalysis { get; set; }
        
        // Legacy fields for backward compatibility
        public string PlantType { get; set; }
        public string GrowthStage { get; set; }
        public List<ElementDeficiencyDto> ElementDeficiencies { get; set; }
        public List<DiseaseDto> Diseases { get; set; }
        public List<PestDto> Pests { get; set; }
        public string OverallAnalysis { get; set; }
    }





    public class ProcessingMetadataDto : IDto
    {
        public bool ParseSuccess { get; set; }
        public DateTime ProcessingTimestamp { get; set; }
        public string AiModel { get; set; }
        public string WorkflowVersion { get; set; }
    }

    public class TokenUsageDto : IDto
    {
        public TokenSummaryDto Summary { get; set; }
        public TokenBreakdownDto TokenBreakdown { get; set; }
        public CostBreakdownDto CostBreakdown { get; set; }
    }

    public class TokenSummaryDto : IDto
    {
        public string Model { get; set; }
        public string AnalysisId { get; set; }
        public DateTime Timestamp { get; set; }
        public int TotalTokens { get; set; }
        public string TotalCostUsd { get; set; }
        public string TotalCostTry { get; set; }
        public decimal ImageSizeKb { get; set; }
    }

    public class TokenBreakdownDto : IDto
    {
        public TokenInputDto Input { get; set; }
        public TokenOutputDto Output { get; set; }
        public int GrandTotal { get; set; }
    }

    public class TokenInputDto : IDto
    {
        public int SystemPrompt { get; set; }
        public int ContextData { get; set; }
        public int Image { get; set; }
        public int Total { get; set; }
    }

    public class TokenOutputDto : IDto
    {
        public int Response { get; set; }
        public int Total { get; set; }
    }

    public class CostBreakdownDto : IDto
    {
        public string InputCostUsd { get; set; }
        public string OutputCostUsd { get; set; }
        public string TotalCostUsd { get; set; }
        public string TotalCostTry { get; set; }
        public decimal ExchangeRate { get; set; }
    }

    // Legacy DTOs for backward compatibility
    public class ElementDeficiencyDto : IDto
    {
        public string Element { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public string Treatment { get; set; }
    }

    public class DiseaseDto : IDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public string Treatment { get; set; }
    }

    public class PestDto : IDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public string Treatment { get; set; }
    }
}