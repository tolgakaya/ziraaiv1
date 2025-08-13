using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SponsorshipCodeEntityConfiguration : IEntityTypeConfiguration<SponsorshipCode>
    {
        public void Configure(EntityTypeBuilder<SponsorshipCode> builder)
        {
            builder.ToTable("SponsorshipCodes");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("IX_SponsorshipCodes_Code");
            
            builder.Property(x => x.Notes)
                .HasMaxLength(500);
            
            builder.Property(x => x.DistributedTo)
                .HasMaxLength(200);
            
            builder.Property(x => x.DistributionChannel)
                .HasMaxLength(50);
            
            builder.Property(x => x.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Relationships
            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.UsedByUser)
                .WithMany()
                .HasForeignKey(x => x.UsedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.SubscriptionTier)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionTierId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.SponsorshipPurchase)
                .WithMany(p => p.SponsorshipCodes)
                .HasForeignKey(x => x.SponsorshipPurchaseId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(x => x.CreatedSubscription)
                .WithOne(s => s.SponsorshipCode)
                .HasForeignKey<SponsorshipCode>(x => x.CreatedSubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for performance
            builder.HasIndex(x => x.SponsorId)
                .HasDatabaseName("IX_SponsorshipCodes_SponsorId");
            
            builder.HasIndex(x => x.IsUsed)
                .HasDatabaseName("IX_SponsorshipCodes_IsUsed");
            
            builder.HasIndex(x => new { x.SponsorId, x.IsUsed })
                .HasDatabaseName("IX_SponsorshipCodes_SponsorId_IsUsed");
        }
    }
}