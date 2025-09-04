using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SponsorRequestEntityConfiguration : IEntityTypeConfiguration<SponsorRequest>
    {
        public void Configure(EntityTypeBuilder<SponsorRequest> builder)
        {
            builder.ToTable("SponsorRequests");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.FarmerPhone)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(x => x.SponsorPhone)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(x => x.RequestMessage)
                .HasMaxLength(500);
                
            builder.Property(x => x.RequestToken)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.HasIndex(x => x.RequestToken)
                .IsUnique();
                
            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
                
            builder.Property(x => x.ApprovalNotes)
                .HasMaxLength(500);
                
            builder.Property(x => x.GeneratedSponsorshipCode)
                .HasMaxLength(50);
                
            builder.Property(x => x.RequestDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(x => x.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(x => x.UpdatedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            // Relationships
            builder.HasOne(x => x.Farmer)
                .WithMany()
                .HasForeignKey(x => x.FarmerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(x => x.ApprovedSubscriptionTier)
                .WithMany()
                .HasForeignKey(x => x.ApprovedSubscriptionTierId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}