using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class ReferralCodeEntityConfiguration : IEntityTypeConfiguration<ReferralCode>
    {
        public void Configure(EntityTypeBuilder<ReferralCode> builder)
        {
            builder.ToTable("ReferralCodes");

            builder.HasKey(x => x.Id);

            // Code - unique referral code (e.g., ZIRA-ABC123)
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("IX_ReferralCodes_Code");

            // UserId - referrer who generated this code
            builder.Property(x => x.UserId)
                .IsRequired();

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_ReferralCodes_UserId");

            // IsActive - whether code can be used
            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // CreatedAt - when code was generated
            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // ExpiresAt - when code expires (CreatedAt + configured days)
            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("IX_ReferralCodes_ExpiresAt");

            // Status - 0=Active, 1=Expired, 2=Disabled
            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_ReferralCodes_Status");

            // Foreign key to Users (referrer)
            builder.HasOne<Core.Entities.Concrete.User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ReferralCodes_Users");
        }
    }
}
