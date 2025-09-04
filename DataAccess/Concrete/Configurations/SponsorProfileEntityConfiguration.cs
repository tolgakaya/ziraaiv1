using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SponsorProfileEntityConfiguration : IEntityTypeConfiguration<SponsorProfile>
    {
        public void Configure(EntityTypeBuilder<SponsorProfile> builder)
        {
            builder.ToTable("SponsorProfiles");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.CompanyName)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(x => x.CompanyDescription)
                .HasMaxLength(2000);
            
            builder.Property(x => x.SponsorLogoUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.WebsiteUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.ContactEmail)
                .HasMaxLength(100);
            
            builder.Property(x => x.ContactPhone)
                .HasMaxLength(20);
            
            builder.Property(x => x.ContactPerson)
                .HasMaxLength(100);
            
            builder.Property(x => x.LinkedInUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.TwitterUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.FacebookUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.InstagramUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.TaxNumber)
                .HasMaxLength(50);
            
            builder.Property(x => x.TradeRegistryNumber)
                .HasMaxLength(50);
            
            builder.Property(x => x.Address)
                .HasMaxLength(500);
            
            builder.Property(x => x.City)
                .HasMaxLength(100);
            
            builder.Property(x => x.Country)
                .HasMaxLength(100);
            
            builder.Property(x => x.PostalCode)
                .HasMaxLength(20);
            
            builder.Property(x => x.CompanyType)
                .HasMaxLength(100);
            
            builder.Property(x => x.BusinessModel)
                .HasMaxLength(50);
            
            builder.Property(x => x.VerificationNotes)
                .HasMaxLength(500);
            
            builder.Property(x => x.TotalInvestment)
                .HasPrecision(18, 2);
            
            // Relationships
            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            builder.HasIndex(x => x.SponsorId).IsUnique();
            builder.HasIndex(x => x.CompanyName);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.IsVerified);
        }
    }
}