using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SponsorAnalysisAccessEntityConfiguration : IEntityTypeConfiguration<SponsorAnalysisAccess>
    {
        public void Configure(EntityTypeBuilder<SponsorAnalysisAccess> builder)
        {
            builder.ToTable("SponsorAnalysisAccess");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.AccessLevel)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.Property(x => x.AccessedFields)
                .HasMaxLength(4000);
            
            builder.Property(x => x.RestrictedFields)
                .HasMaxLength(4000);
            
            builder.Property(x => x.ContactMethod)
                .HasMaxLength(50);
            
            builder.Property(x => x.Notes)
                .HasMaxLength(1000);
            
            builder.Property(x => x.IpAddress)
                .HasMaxLength(50);
            
            builder.Property(x => x.UserAgent)
                .HasMaxLength(500);
            
            // Relationships
            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.PlantAnalysis)
                .WithMany()
                .HasForeignKey(x => x.PlantAnalysisId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(x => x.Farmer)
                .WithMany()
                .HasForeignKey(x => x.FarmerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.SponsorshipCode)
                .WithMany()
                .HasForeignKey(x => x.SponsorshipCodeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.SponsorshipPurchase)
                .WithMany()
                .HasForeignKey(x => x.SponsorshipPurchaseId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            builder.HasIndex(x => x.SponsorId);
            builder.HasIndex(x => x.PlantAnalysisId);
            builder.HasIndex(x => x.FarmerId);
            builder.HasIndex(x => new { x.SponsorId, x.PlantAnalysisId }).IsUnique();
            builder.HasIndex(x => x.FirstViewedDate);
            builder.HasIndex(x => x.AccessLevel);
        }
    }
}