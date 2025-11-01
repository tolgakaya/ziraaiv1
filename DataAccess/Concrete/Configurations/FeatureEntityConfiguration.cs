using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class FeatureEntityConfiguration : IEntityTypeConfiguration<Feature>
    {
        public void Configure(EntityTypeBuilder<Feature> builder)
        {
            builder.ToTable("Features");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.FeatureKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(f => f.FeatureKey)
                .IsUnique();

            builder.Property(f => f.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(f => f.Description)
                .HasMaxLength(1000);

            builder.Property(f => f.DefaultConfigJson)
                .HasMaxLength(2000);

            builder.Property(f => f.RequiresConfiguration)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(f => f.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(f => f.IsDeprecated)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(f => f.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(f => f.UpdatedDate);

            // Navigation properties
            builder.HasMany(f => f.TierFeatures)
                .WithOne(tf => tf.Feature)
                .HasForeignKey(tf => tf.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
