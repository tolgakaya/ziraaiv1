using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace DataAccess.Concrete.Configurations
{
    public class DeepLinkEntityConfiguration : IEntityTypeConfiguration<DeepLink>
    {
        public void Configure(EntityTypeBuilder<DeepLink> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).UseIdentityColumn();
            
            builder.Property(x => x.LinkId).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.LinkId).IsUnique();
            
            builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
            builder.Property(x => x.PrimaryParameter).HasMaxLength(200);
            builder.Property(x => x.AdditionalParameters).HasMaxLength(500);
            
            builder.Property(x => x.DeepLinkUrl).IsRequired().HasMaxLength(500);
            builder.Property(x => x.UniversalLinkUrl).HasMaxLength(500);
            builder.Property(x => x.WebFallbackUrl).HasMaxLength(500);
            builder.Property(x => x.ShortUrl).HasMaxLength(200);
            builder.Property(x => x.QrCodeUrl).HasMaxLength(5000);
            
            builder.Property(x => x.CampaignSource).HasMaxLength(50);
            builder.Property(x => x.SponsorId).HasMaxLength(50);
            
            builder.Property(x => x.CreatedDate).IsRequired();
            builder.Property(x => x.ExpiryDate).IsRequired();
            builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            
            // Analytics counters
            builder.Property(x => x.TotalClicks).HasDefaultValue(0);
            builder.Property(x => x.MobileAppOpens).HasDefaultValue(0);
            builder.Property(x => x.WebFallbackOpens).HasDefaultValue(0);
            builder.Property(x => x.UniqueDevices).HasDefaultValue(0);
            builder.Property(x => x.LastClickDate);
            
            // Note: SponsorId is stored as string, relationship handled manually in service layer
                
            // Table name
            builder.ToTable("DeepLinks");
        }
    }

    public class DeepLinkClickRecordEntityConfiguration : IEntityTypeConfiguration<DeepLinkClickRecord>
    {
        public void Configure(EntityTypeBuilder<DeepLinkClickRecord> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).UseIdentityColumn();
            
            builder.Property(x => x.LinkId).IsRequired().HasMaxLength(50);
            builder.Property(x => x.UserAgent).HasMaxLength(500);
            builder.Property(x => x.IpAddress).HasMaxLength(45); // IPv4/IPv6
            builder.Property(x => x.Platform).HasMaxLength(20);
            builder.Property(x => x.DeviceId).HasMaxLength(100);
            builder.Property(x => x.Referrer).HasMaxLength(500);
            
            builder.Property(x => x.ClickDate).IsRequired();
            builder.Property(x => x.Country).HasMaxLength(100);
            builder.Property(x => x.City).HasMaxLength(100);
            builder.Property(x => x.Source).HasMaxLength(50);
            
            // Action tracking
            builder.Property(x => x.DidOpenApp).HasDefaultValue(false);
            builder.Property(x => x.DidCompleteAction).HasDefaultValue(false);
            builder.Property(x => x.ActionCompletedDate);
            builder.Property(x => x.ActionResult).HasMaxLength(50);
            
            // Relationships
            builder.HasOne(x => x.DeepLink)
                .WithMany()
                .HasForeignKey(x => x.LinkId)
                .HasPrincipalKey(d => d.LinkId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Indexes for performance
            builder.HasIndex(x => x.LinkId);
            builder.HasIndex(x => x.ClickDate);
            builder.HasIndex(x => x.Platform);
            
            // Table name
            builder.ToTable("DeepLinkClickRecords");
        }
    }
}