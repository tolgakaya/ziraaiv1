using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class ReferralRewardEntityConfiguration : IEntityTypeConfiguration<ReferralReward>
    {
        public void Configure(EntityTypeBuilder<ReferralReward> builder)
        {
            builder.ToTable("ReferralRewards");

            builder.HasKey(x => x.Id);

            // ReferralTrackingId - link to tracking record
            builder.Property(x => x.ReferralTrackingId)
                .IsRequired();

            builder.HasIndex(x => x.ReferralTrackingId)
                .HasDatabaseName("IX_ReferralRewards_ReferralTrackingId");

            // ReferrerUserId - user receiving the reward
            builder.Property(x => x.ReferrerUserId)
                .IsRequired();

            builder.HasIndex(x => x.ReferrerUserId)
                .HasDatabaseName("IX_ReferralRewards_ReferrerUserId");

            // RefereeUserId - user who triggered the reward
            builder.Property(x => x.RefereeUserId)
                .IsRequired();

            builder.HasIndex(x => x.RefereeUserId)
                .HasDatabaseName("IX_ReferralRewards_RefereeUserId");

            // CreditAmount - configurable reward amount
            builder.Property(x => x.CreditAmount)
                .IsRequired();

            // AwardedAt - when reward was given
            builder.Property(x => x.AwardedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasIndex(x => x.AwardedAt)
                .HasDatabaseName("IX_ReferralRewards_AwardedAt");

            // SubscriptionId - which subscription received credits
            builder.Property(x => x.SubscriptionId)
                .IsRequired(false);

            // ExpiresAt - when credits expire (NULL = never per requirements)
            builder.Property(x => x.ExpiresAt)
                .IsRequired(false);

            // Foreign key to ReferralTracking
            builder.HasOne<ReferralTracking>()
                .WithMany()
                .HasForeignKey(x => x.ReferralTrackingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ReferralRewards_ReferralTracking");

            // Foreign key to Users (referrer)
            builder.HasOne<Core.Entities.Concrete.User>()
                .WithMany()
                .HasForeignKey(x => x.ReferrerUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ReferralRewards_ReferrerUsers");

            // Foreign key to Users (referee)
            builder.HasOne<Core.Entities.Concrete.User>()
                .WithMany()
                .HasForeignKey(x => x.RefereeUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ReferralRewards_RefereeUsers");

            // Foreign key to UserSubscriptions
            builder.HasOne<UserSubscription>()
                .WithMany()
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ReferralRewards_Subscriptions");
        }
    }
}
