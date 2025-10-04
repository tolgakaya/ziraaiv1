using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class ReferralTrackingEntityConfiguration : IEntityTypeConfiguration<ReferralTracking>
    {
        public void Configure(EntityTypeBuilder<ReferralTracking> builder)
        {
            builder.ToTable("ReferralTracking");

            builder.HasKey(x => x.Id);

            // ReferralCodeId - which code was used
            builder.Property(x => x.ReferralCodeId)
                .IsRequired();

            builder.HasIndex(x => x.ReferralCodeId)
                .HasDatabaseName("IX_ReferralTracking_ReferralCodeId");

            // RefereeUserId - new user who used the code (nullable until registration)
            builder.Property(x => x.RefereeUserId)
                .IsRequired(false);

            builder.HasIndex(x => x.RefereeUserId)
                .HasDatabaseName("IX_ReferralTracking_RefereeUserId");

            // Timestamp fields - track the journey
            builder.Property(x => x.ClickedAt)
                .IsRequired(false);

            builder.Property(x => x.RegisteredAt)
                .IsRequired(false);

            builder.Property(x => x.FirstAnalysisAt)
                .IsRequired(false);

            builder.Property(x => x.RewardProcessedAt)
                .IsRequired(false);

            // Status - 0=Clicked, 1=Registered, 2=Validated, 3=Rewarded
            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_ReferralTracking_Status");

            // RefereeMobilePhone - phone number before registration
            builder.Property(x => x.RefereeMobilePhone)
                .HasMaxLength(15)
                .IsRequired(false);

            // IpAddress - for tracking and anti-abuse
            builder.Property(x => x.IpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            // DeviceId - for duplicate prevention
            builder.Property(x => x.DeviceId)
                .HasMaxLength(255)
                .IsRequired(false);

            builder.HasIndex(x => x.DeviceId)
                .HasDatabaseName("IX_ReferralTracking_DeviceId");

            // FailureReason - if referral failed
            builder.Property(x => x.FailureReason)
                .HasColumnType("text")
                .IsRequired(false);

            // Foreign key to ReferralCodes
            builder.HasOne<ReferralCode>()
                .WithMany()
                .HasForeignKey(x => x.ReferralCodeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ReferralTracking_ReferralCodes");

            // Foreign key to Users (referee)
            builder.HasOne<Core.Entities.Concrete.User>()
                .WithMany()
                .HasForeignKey(x => x.RefereeUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ReferralTracking_Users");
        }
    }
}
