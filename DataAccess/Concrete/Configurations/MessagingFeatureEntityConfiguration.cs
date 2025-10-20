using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class MessagingFeatureEntityConfiguration : IEntityTypeConfiguration<MessagingFeature>
    {
        public void Configure(EntityTypeBuilder<MessagingFeature> builder)
        {
            builder.ToTable("MessagingFeatures");

            builder.HasKey(mf => mf.Id);

            builder.Property(mf => mf.FeatureName)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(mf => mf.FeatureName)
                .IsUnique();

            builder.Property(mf => mf.DisplayName)
                .HasMaxLength(200);

            builder.Property(mf => mf.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(mf => mf.RequiredTier)
                .HasMaxLength(20)
                .HasDefaultValue("None");

            builder.Property(mf => mf.MaxFileSize)
                .IsRequired(false);

            builder.Property(mf => mf.MaxDuration)
                .IsRequired(false);

            builder.Property(mf => mf.AllowedMimeTypes)
                .HasMaxLength(1000);

            builder.Property(mf => mf.TimeLimit)
                .IsRequired(false);

            builder.Property(mf => mf.Description)
                .HasMaxLength(500);

            builder.Property(mf => mf.ConfigurationJson)
                .HasColumnType("text");

            builder.Property(mf => mf.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(mf => mf.UpdatedDate)
                .IsRequired(false);

            builder.Property(mf => mf.CreatedByUserId)
                .IsRequired(false);

            builder.Property(mf => mf.UpdatedByUserId)
                .IsRequired(false);

            // Relationships
            builder.HasOne(mf => mf.CreatedByUser)
                .WithMany()
                .HasForeignKey(mf => mf.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(mf => mf.UpdatedByUser)
                .WithMany()
                .HasForeignKey(mf => mf.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
