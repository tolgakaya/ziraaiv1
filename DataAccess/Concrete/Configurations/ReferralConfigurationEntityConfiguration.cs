using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class ReferralConfigurationEntityConfiguration : IEntityTypeConfiguration<ReferralConfiguration>
    {
        public void Configure(EntityTypeBuilder<ReferralConfiguration> builder)
        {
            builder.ToTable("ReferralConfigurations");

            builder.HasKey(x => x.Id);

            // Key - configuration identifier (e.g., "Referral.CreditPerReferral")
            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Key)
                .IsUnique()
                .HasDatabaseName("IX_ReferralConfigurations_Key");

            // Value - stored as text, parsed by DataType
            builder.Property(x => x.Value)
                .IsRequired()
                .HasColumnType("text");

            // Description - human-readable explanation
            builder.Property(x => x.Description)
                .HasColumnType("text")
                .IsRequired(false);

            // DataType - "int", "bool", "string", "decimal"
            builder.Property(x => x.DataType)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("string");

            // UpdatedAt - when last modified
            builder.Property(x => x.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // UpdatedBy - admin user who modified (nullable)
            builder.Property(x => x.UpdatedBy)
                .IsRequired(false);

            // Foreign key to Users (admin who updated)
            builder.HasOne<Core.Entities.Concrete.User>()
                .WithMany()
                .HasForeignKey(x => x.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ReferralConfigurations_Users");
        }
    }
}
