using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class PlantAnalysisEntityConfiguration : IEntityTypeConfiguration<PlantAnalysis>
    {
        public void Configure(EntityTypeBuilder<PlantAnalysis> builder)
        {
            builder.ToTable("PlantAnalyses");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.ImagePath)
                .HasMaxLength(500);
            
            builder.Property(x => x.PlantType)
                .HasMaxLength(100);
            
            builder.Property(x => x.GrowthStage)
                .HasMaxLength(100);
            
            builder.Property(x => x.ElementDeficiencies)
                .HasColumnType("text");
            
            builder.Property(x => x.Diseases)
                .HasColumnType("text");
            
            builder.Property(x => x.Pests)
                .HasColumnType("text");
            
            builder.Property(x => x.AnalysisResult)
                .HasColumnType("text");
            
            builder.Property(x => x.N8nWebhookResponse)
                .HasColumnType("text");
            
            builder.Property(x => x.DetailedAnalysisData)
                .HasColumnType("text");
            
            builder.Property(x => x.AnalysisStatus)
                .HasMaxLength(50);
            
            // New fields for N8N response
            builder.Property(x => x.AnalysisId)
                .HasMaxLength(200);
            
            builder.Property(x => x.FarmerId)
                .HasMaxLength(100);
            
            builder.Property(x => x.SponsorId)
                .HasMaxLength(100);
            
            builder.Property(x => x.Location)
                .HasMaxLength(200);
            
            builder.Property(x => x.Latitude)
                .HasPrecision(18, 6);
            
            builder.Property(x => x.Longitude)
                .HasPrecision(18, 6);
            
            builder.Property(x => x.FieldId)
                .HasMaxLength(100);
            
            builder.Property(x => x.CropType)
                .HasMaxLength(100);
            
            builder.Property(x => x.WeatherConditions)
                .HasMaxLength(100);
            
            builder.Property(x => x.Temperature)
                .HasPrecision(5, 2);
            
            builder.Property(x => x.Humidity)
                .HasPrecision(5, 2);
            
            builder.Property(x => x.SoilType)
                .HasMaxLength(100);
            
            builder.Property(x => x.UrgencyLevel)
                .HasMaxLength(50);
            
            builder.Property(x => x.Notes)
                .HasMaxLength(1000);
            
            builder.Property(x => x.ContactPhone)
                .HasMaxLength(50);
            
            builder.Property(x => x.ContactEmail)
                .HasMaxLength(100);
            
            builder.Property(x => x.AdditionalInfo)
                .HasColumnType("text");
            
            builder.Property(x => x.PreviousTreatments)
                .HasColumnType("text");
            
            // Plant Identification
            builder.Property(x => x.PlantSpecies)
                .HasMaxLength(200);
            
            builder.Property(x => x.PlantVariety)
                .HasMaxLength(100);
            
            builder.Property(x => x.IdentificationConfidence)
                .HasPrecision(5, 2);
            
            // Health Assessment
            builder.Property(x => x.HealthSeverity)
                .HasMaxLength(50);
            
            builder.Property(x => x.StressIndicators)
                .HasColumnType("text");
            
            builder.Property(x => x.DiseaseSymptoms)
                .HasColumnType("text");
            
            // Nutrient Status
            builder.Property(x => x.PrimaryDeficiency)
                .HasMaxLength(100);
            
            builder.Property(x => x.NutrientStatus)
                .HasColumnType("text");
            
            // Summary
            builder.Property(x => x.PrimaryConcern)
                .HasMaxLength(500);
            
            builder.Property(x => x.Prognosis)
                .HasMaxLength(100);
            
            builder.Property(x => x.EstimatedYieldImpact)
                .HasMaxLength(100);
            
            builder.Property(x => x.ConfidenceLevel)
                .HasPrecision(5, 2);
            
            // Processing Metadata
            builder.Property(x => x.AiModel)
                .HasMaxLength(100);
            
            builder.Property(x => x.TotalTokens)
                .HasPrecision(10, 2);
            
            builder.Property(x => x.TotalCostUsd)
                .HasPrecision(10, 6);
            
            builder.Property(x => x.TotalCostTry)
                .HasPrecision(10, 4);
            
            builder.Property(x => x.ImageSizeKb)
                .HasPrecision(10, 2);
            
            // JSON fields
            builder.Property(x => x.Recommendations)
                .HasColumnType("text");
            
            builder.Property(x => x.CrossFactorInsights)
                .HasColumnType("text");
            
            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(x => x.CreatedDate)
                .IsRequired()
                .HasColumnType("timestamptz");
            
            builder.Property(x => x.UpdatedDate)
                .HasColumnType("timestamptz");
            
            builder.Property(x => x.AnalysisDate)
                .IsRequired()
                .HasColumnType("timestamptz");
            
            // DateTime fields from N8N response
            builder.Property(x => x.PlantingDate)
                .HasColumnType("timestamptz");
            
            builder.Property(x => x.ExpectedHarvestDate)
                .HasColumnType("timestamptz");
            
            builder.Property(x => x.LastFertilization)
                .HasColumnType("timestamptz");
            
            builder.Property(x => x.LastIrrigation)
                .HasColumnType("timestamptz");
            
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.AnalysisDate);
            builder.HasIndex(x => x.AnalysisId);
            builder.HasIndex(x => x.FarmerId);
            builder.HasIndex(x => x.SponsorId);
            builder.HasIndex(x => x.CropType);
        }
    }
}