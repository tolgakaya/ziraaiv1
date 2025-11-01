using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class TierFeatureEntityConfiguration : IEntityTypeConfiguration<TierFeature>
    {
        public void Configure(EntityTypeBuilder<TierFeature> builder)
        {
            builder.ToTable("TierFeatures");

            builder.HasKey(tf => tf.Id);

            builder.Property(tf => tf.SubscriptionTierId)
                .IsRequired();

            builder.Property(tf => tf.FeatureId)
                .IsRequired();

            // Unique constraint: Each tier can have each feature only once
            builder.HasIndex(tf => new { tf.SubscriptionTierId, tf.FeatureId })
                .IsUnique();

            builder.Property(tf => tf.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(tf => tf.ConfigurationJson)
                .HasMaxLength(2000);

            builder.Property(tf => tf.EffectiveDate);

            builder.Property(tf => tf.ExpiryDate);

            builder.Property(tf => tf.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(tf => tf.UpdatedDate);

            builder.Property(tf => tf.CreatedByUserId)
                .IsRequired();

            builder.Property(tf => tf.ModifiedByUserId);

            // Foreign key to SubscriptionTier
            builder.HasOne(tf => tf.SubscriptionTier)
                .WithMany()
                .HasForeignKey(tf => tf.SubscriptionTierId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Feature
            builder.HasOne(tf => tf.Feature)
                .WithMany(f => f.TierFeatures)
                .HasForeignKey(tf => tf.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
