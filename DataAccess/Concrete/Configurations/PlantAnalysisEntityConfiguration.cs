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

            // Primary Key
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            // Basic Information
            builder.Property(x => x.AnalysisDate)
                .HasColumnName("AnalysisDate")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(x => x.AnalysisStatus)
                .HasColumnName("AnalysisStatus")
                .HasMaxLength(50)
                .HasDefaultValue("pending")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("Status")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.CreatedDate)
                .HasColumnName("CreatedDate")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(x => x.UpdatedDate)
                .HasColumnName("UpdatedDate")
                .HasColumnType("timestamp");

            // Analysis Identification
            builder.Property(x => x.AnalysisId)
                .HasColumnName("AnalysisId")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Timestamp)
                .HasColumnName("Timestamp")
                .HasColumnType("timestamp")
                .IsRequired();

            // User & Sponsor Information
            builder.Property(x => x.UserId)
                .HasColumnName("UserId");

            builder.Property(x => x.FarmerId)
                .HasColumnName("FarmerId")
                .HasMaxLength(50);

            builder.Property(x => x.SponsorId)
                .HasColumnName("SponsorId")
                .HasMaxLength(50);

            builder.Property(x => x.SponsorshipCodeId)
                .HasColumnName("SponsorshipCodeId");

            builder.Property(x => x.SponsorUserId)
                .HasColumnName("SponsorUserId");

            // Location Information
            builder.Property(x => x.Location)
                .HasColumnName("Location")
                .HasMaxLength(255);

            builder.Property(x => x.GpsCoordinates)
                .HasColumnName("GpsCoordinates")
                .HasColumnType("jsonb");

            // Helper fields for GPS coordinates (not in DB but used by code)
            builder.Ignore(x => x.Latitude);
            builder.Ignore(x => x.Longitude);

            builder.Property(x => x.Altitude)
                .HasColumnName("Altitude");

            // Field & Crop Information
            builder.Property(x => x.FieldId)
                .HasColumnName("FieldId")
                .HasMaxLength(100);

            builder.Property(x => x.CropType)
                .HasColumnName("CropType")
                .HasMaxLength(100);

            builder.Property(x => x.PlantingDate)
                .HasColumnName("PlantingDate")
                .HasColumnType("timestamp");

            builder.Property(x => x.ExpectedHarvestDate)
                .HasColumnName("ExpectedHarvestDate")
                .HasColumnType("timestamp");

            builder.Property(x => x.LastFertilization)
                .HasColumnName("LastFertilization")
                .HasColumnType("timestamp");

            builder.Property(x => x.LastIrrigation)
                .HasColumnName("LastIrrigation")
                .HasColumnType("timestamp");

            builder.Property(x => x.PreviousTreatments)
                .HasColumnName("PreviousTreatments")
                .HasColumnType("jsonb");

            // Environmental Conditions
            builder.Property(x => x.WeatherConditions)
                .HasColumnName("WeatherConditions")
                .HasMaxLength(100);

            builder.Property(x => x.Temperature)
                .HasColumnName("Temperature")
                .HasPrecision(5, 2);

            builder.Property(x => x.Humidity)
                .HasColumnName("Humidity")
                .HasPrecision(5, 2);

            builder.Property(x => x.SoilType)
                .HasColumnName("SoilType")
                .HasMaxLength(100);

            // Analysis Request Details
            builder.Property(x => x.UrgencyLevel)
                .HasColumnName("UrgencyLevel")
                .HasMaxLength(50);

            builder.Property(x => x.Notes)
                .HasColumnName("Notes")
                .HasColumnType("text");

            builder.Property(x => x.ContactInfo)
                .HasColumnName("ContactInfo")
                .HasColumnType("text");

            // Helper fields for contact info (not in DB but used by code)
            builder.Ignore(x => x.ContactPhone);
            builder.Ignore(x => x.ContactEmail);

            builder.Property(x => x.AdditionalInfo)
                .HasColumnName("AdditionalInfo")
                .HasColumnType("jsonb");

            // Plant Identification - JSONB + Helper Fields
            builder.Property(x => x.PlantIdentification)
                .HasColumnName("PlantIdentification")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.PlantSpecies)
                .HasColumnName("PlantSpecies")
                .HasColumnType("text");

            builder.Property(x => x.PlantVariety)
                .HasColumnName("PlantVariety")
                .HasMaxLength(100);

            builder.Property(x => x.GrowthStage)
                .HasColumnName("GrowthStage")
                .HasMaxLength(100);

            builder.Property(x => x.IdentificationConfidence)
                .HasColumnName("IdentificationConfidence");

            // Health Assessment - JSONB + Helper Fields
            builder.Property(x => x.HealthAssessment)
                .HasColumnName("HealthAssessment")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.VigorScore)
                .HasColumnName("VigorScore");

            builder.Property(x => x.HealthSeverity)
                .HasColumnName("HealthSeverity")
                .HasMaxLength(50);

            // Helper fields for health assessment (not in DB but used by code)
            builder.Ignore(x => x.StressIndicators);
            builder.Ignore(x => x.DiseaseSymptoms);

            // Nutrient Status - JSONB + Helper Fields
            builder.Property(x => x.NutrientStatus)
                .HasColumnName("NutrientStatus")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.Nitrogen)
                .HasColumnName("Nitrogen")
                .HasMaxLength(50);

            builder.Property(x => x.Phosphorus)
                .HasColumnName("Phosphorus")
                .HasMaxLength(50);

            builder.Property(x => x.Potassium)
                .HasColumnName("Potassium")
                .HasMaxLength(50);

            builder.Property(x => x.Calcium)
                .HasColumnName("Calcium")
                .HasMaxLength(50);

            builder.Property(x => x.Magnesium)
                .HasColumnName("Magnesium")
                .HasMaxLength(50);

            builder.Property(x => x.Sulfur)
                .HasColumnName("Sulfur")
                .HasMaxLength(50);

            builder.Property(x => x.Iron)
                .HasColumnName("Iron")
                .HasMaxLength(50);

            builder.Property(x => x.Zinc)
                .HasColumnName("Zinc")
                .HasMaxLength(50);

            builder.Property(x => x.Manganese)
                .HasColumnName("Manganese")
                .HasMaxLength(50);

            builder.Property(x => x.Boron)
                .HasColumnName("Boron")
                .HasMaxLength(50);

            builder.Property(x => x.Copper)
                .HasColumnName("Copper")
                .HasMaxLength(50);

            builder.Property(x => x.Molybdenum)
                .HasColumnName("Molybdenum")
                .HasMaxLength(50);

            builder.Property(x => x.Chlorine)
                .HasColumnName("Chlorine")
                .HasMaxLength(50);

            builder.Property(x => x.Nickel)
                .HasColumnName("Nickel")
                .HasMaxLength(50);

            builder.Property(x => x.PrimaryDeficiency)
                .HasColumnName("PrimaryDeficiency")
                .HasColumnType("text");

            builder.Property(x => x.NutrientSeverity)
                .HasColumnName("NutrientSeverity")
                .HasMaxLength(50);

            // Pest & Disease - JSONB + Helper Fields
            builder.Property(x => x.PestDisease)
                .HasColumnName("PestDisease")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.AffectedAreaPercentage)
                .HasColumnName("AffectedAreaPercentage");

            builder.Property(x => x.SpreadRisk)
                .HasColumnName("SpreadRisk")
                .HasMaxLength(50);

            builder.Property(x => x.PrimaryIssue)
                .HasColumnName("PrimaryIssue")
                .HasColumnType("text");

            // Environmental Stress - JSONB + Helper Field
            builder.Property(x => x.EnvironmentalStress)
                .HasColumnName("EnvironmentalStress")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.PrimaryStressor)
                .HasColumnName("PrimaryStressor")
                .HasColumnType("text");

            // Cross-Factor Insights
            builder.Property(x => x.CrossFactorInsights)
                .HasColumnName("CrossFactorInsights")
                .HasColumnType("jsonb");

            // Risk Assessment
            builder.Property(x => x.RiskAssessment)
                .HasColumnName("RiskAssessment")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            // Recommendations
            builder.Property(x => x.Recommendations)
                .HasColumnName("Recommendations")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            // Summary - JSONB + Helper Fields
            builder.Property(x => x.Summary)
                .HasColumnName("Summary")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.OverallHealthScore)
                .HasColumnName("OverallHealthScore")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.PrimaryConcern)
                .HasColumnName("PrimaryConcern")
                .HasColumnType("text");

            builder.Property(x => x.CriticalIssuesCount)
                .HasColumnName("CriticalIssuesCount");

            builder.Property(x => x.ConfidenceLevel)
                .HasColumnName("ConfidenceLevel");

            builder.Property(x => x.Prognosis)
                .HasColumnName("Prognosis")
                .HasMaxLength(50);

            builder.Property(x => x.EstimatedYieldImpact)
                .HasColumnName("EstimatedYieldImpact")
                .HasMaxLength(50);

            // Confidence Notes
            builder.Property(x => x.ConfidenceNotes)
                .HasColumnName("ConfidenceNotes")
                .HasColumnType("jsonb");

            // Farmer-Friendly Summary
            builder.Property(x => x.FarmerFriendlySummary)
                .HasColumnName("FarmerFriendlySummary")
                .HasColumnType("text")
                .HasDefaultValue("")
                .IsRequired();

            // Image Metadata
            builder.Property(x => x.ImageMetadata)
                .HasColumnName("ImageMetadata")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.ImageUrl)
                .HasColumnName("ImageUrl")
                .HasColumnType("text")
                .HasDefaultValue("")
                .IsRequired();

            // Request Metadata
            builder.Property(x => x.RequestMetadata)
                .HasColumnName("RequestMetadata")
                .HasColumnType("jsonb");

            // Token Usage
            builder.Property(x => x.TokenUsage)
                .HasColumnName("TokenUsage")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            // Processing Metadata - JSONB + Helper Fields
            builder.Property(x => x.ProcessingMetadata)
                .HasColumnName("ProcessingMetadata")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            builder.Property(x => x.AiModel)
                .HasColumnName("AiModel")
                .HasMaxLength(100)
                .HasDefaultValue("")
                .IsRequired();

            builder.Property(x => x.WorkflowVersion)
                .HasColumnName("WorkflowVersion")
                .HasMaxLength(50)
                .HasDefaultValue("")
                .IsRequired();

            builder.Property(x => x.TotalTokens)
                .HasColumnName("TotalTokens")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.TotalCostUsd)
                .HasColumnName("TotalCostUsd")
                .HasPrecision(10, 6)
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.TotalCostTry)
                .HasColumnName("TotalCostTry")
                .HasPrecision(10, 4)
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(x => x.ProcessingTimestamp)
                .HasColumnName("ProcessingTimestamp")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            // Full Response Storage
            builder.Property(x => x.DetailedAnalysisData)
                .HasColumnName("DetailedAnalysisData")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .IsRequired();

            // Legacy and Helper fields (not in DB but used by code)
            builder.Ignore(x => x.ImagePath);
            builder.Ignore(x => x.ImageSizeKb);
            builder.Ignore(x => x.PlantType);
            builder.Ignore(x => x.ElementDeficiencies);
            builder.Ignore(x => x.Diseases);
            builder.Ignore(x => x.Pests);
            builder.Ignore(x => x.AnalysisResult);
            builder.Ignore(x => x.N8nWebhookResponse);

            // Constraints
            builder.HasIndex(x => x.AnalysisId)
                .IsUnique()
                .HasDatabaseName("PlantAnalyses_AnalysisId_key");

            // Indexes matching the database
            builder.HasIndex(x => x.AnalysisDate).HasDatabaseName("IDX_PlantAnalyses_AnalysisDate");
            builder.HasIndex(x => x.AnalysisId).HasDatabaseName("IDX_PlantAnalyses_AnalysisId");
            builder.HasIndex(x => x.AnalysisStatus).HasDatabaseName("IDX_PlantAnalyses_AnalysisStatus");
            builder.HasIndex(x => x.CropType).HasDatabaseName("IDX_PlantAnalyses_CropType");
            builder.HasIndex(x => x.FarmerId).HasDatabaseName("IDX_PlantAnalyses_FarmerId");
            builder.HasIndex(x => x.Location).HasDatabaseName("IDX_PlantAnalyses_Location");
            builder.HasIndex(x => x.OverallHealthScore).HasDatabaseName("IDX_PlantAnalyses_OverallHealthScore");
            builder.HasIndex(x => x.ProcessingTimestamp).HasDatabaseName("IDX_PlantAnalyses_ProcessingTimestamp");
            builder.HasIndex(x => x.Timestamp).HasDatabaseName("IDX_PlantAnalyses_Timestamp");
            builder.HasIndex(x => x.UserId).HasDatabaseName("IDX_PlantAnalyses_UserId");

            // GIN indexes for JSONB fields
            builder.HasIndex(x => x.DetailedAnalysisData)
                .HasMethod("gin")
                .HasDatabaseName("IDX_PlantAnalyses_DetailedAnalysisData_GIN");

            builder.HasIndex(x => x.HealthAssessment)
                .HasMethod("gin")
                .HasDatabaseName("IDX_PlantAnalyses_HealthAssessment_GIN");

            builder.HasIndex(x => x.NutrientStatus)
                .HasMethod("gin")
                .HasDatabaseName("IDX_PlantAnalyses_NutrientStatus_GIN");

            builder.HasIndex(x => x.PestDisease)
                .HasMethod("gin")
                .HasDatabaseName("IDX_PlantAnalyses_PestDisease_GIN");

            builder.HasIndex(x => x.PlantIdentification)
                .HasMethod("gin")
                .HasDatabaseName("IDX_PlantAnalyses_PlantIdentification_GIN");

            builder.HasIndex(x => x.Recommendations)
                .HasMethod("gin")
                .HasDatabaseName("IDX_PlantAnalyses_Recommendations_GIN");

            // Foreign Key relationships
            builder.HasOne(x => x.SponsorshipCode)
                .WithMany()
                .HasForeignKey(x => x.SponsorshipCodeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PlantAnalyses_SponsorshipCodes");

            builder.HasOne(x => x.SponsorUser)
                .WithMany()
                .HasForeignKey(x => x.SponsorUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PlantAnalyses_SponsorUsers");
        }
    }
}