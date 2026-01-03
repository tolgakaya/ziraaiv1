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
            
            // Foreign key relationships (without navigation properties to prevent EF save conflicts)
            builder.Property(x => x.SponsorId)
                .IsRequired();
            
            builder.Property(x => x.SubscriptionTierId)
                .IsRequired();
            
            builder.Property(x => x.SponsorshipPurchaseId)
                .IsRequired();

            // Farmer Invitation System Fields (nullable for backward compatibility)
            builder.Property(x => x.FarmerInvitationId)
                .IsRequired(false);

            builder.Property(x => x.ReservedForFarmerInvitationId)
                .IsRequired(false);

            builder.Property(x => x.ReservedForFarmerAt)
                .IsRequired(false);

            // Indexes for performance
            builder.HasIndex(x => x.SponsorId)
                .HasDatabaseName("IX_SponsorshipCodes_SponsorId");
            
            builder.HasIndex(x => x.IsUsed)
                .HasDatabaseName("IX_SponsorshipCodes_IsUsed");
            
            builder.HasIndex(x => new { x.SponsorId, x.IsUsed })
                .HasDatabaseName("IX_SponsorshipCodes_SponsorId_IsUsed");
            
            // Composite index for sent+expired codes query (CRITICAL for performance with millions of rows)
            // Covers: WHERE SponsorId = X AND DistributionDate IS NOT NULL AND ExpiryDate < NOW AND IsUsed = false
            // ORDER BY ExpiryDate DESC, DistributionDate DESC
            builder.HasIndex(x => new { x.SponsorId, x.DistributionDate, x.ExpiryDate, x.IsUsed })
                .HasDatabaseName("IX_SponsorshipCodes_SentExpired")
                .HasFilter("\"DistributionDate\" IS NOT NULL AND \"IsUsed\" = false");
            
            // Index for unsent codes query optimization
            builder.HasIndex(x => new { x.SponsorId, x.DistributionDate, x.IsUsed })
                .HasDatabaseName("IX_SponsorshipCodes_Unsent")
                .HasFilter("\"DistributionDate\" IS NULL");
            
            // Index for sent but unused codes query
            builder.HasIndex(x => new { x.SponsorId, x.DistributionDate, x.IsUsed })
                .HasDatabaseName("IX_SponsorshipCodes_SentUnused")
                .HasFilter("\"DistributionDate\" IS NOT NULL AND \"IsUsed\" = false");

            // Farmer Invitation System Indexes
            builder.HasIndex(x => x.FarmerInvitationId)
                .HasDatabaseName("IX_SponsorshipCode_FarmerInvitationId");

            builder.HasIndex(x => x.ReservedForFarmerInvitationId)
                .HasDatabaseName("IX_SponsorshipCode_ReservedForFarmerInvitationId");
        }
    }
}