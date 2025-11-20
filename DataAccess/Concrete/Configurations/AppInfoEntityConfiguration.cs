using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class AppInfoEntityConfiguration : IEntityTypeConfiguration<AppInfo>
    {
        public void Configure(EntityTypeBuilder<AppInfo> builder)
        {
            builder.HasKey(a => a.Id);

            // Company Info
            builder.Property(a => a.CompanyName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.CompanyDescription)
                .HasMaxLength(1000);

            builder.Property(a => a.AppVersion)
                .HasMaxLength(20);

            // Address
            builder.Property(a => a.Address)
                .HasMaxLength(500);

            // Contact Info
            builder.Property(a => a.Email)
                .HasMaxLength(100);

            builder.Property(a => a.Phone)
                .HasMaxLength(50);

            builder.Property(a => a.WebsiteUrl)
                .HasMaxLength(200);

            // Social Media
            builder.Property(a => a.FacebookUrl)
                .HasMaxLength(200);

            builder.Property(a => a.InstagramUrl)
                .HasMaxLength(200);

            builder.Property(a => a.YouTubeUrl)
                .HasMaxLength(200);

            builder.Property(a => a.TwitterUrl)
                .HasMaxLength(200);

            builder.Property(a => a.LinkedInUrl)
                .HasMaxLength(200);

            // Legal Pages
            builder.Property(a => a.TermsOfServiceUrl)
                .HasMaxLength(200);

            builder.Property(a => a.PrivacyPolicyUrl)
                .HasMaxLength(200);

            builder.Property(a => a.CookiePolicyUrl)
                .HasMaxLength(200);

            // Indexes
            builder.HasIndex(a => a.IsActive);
        }
    }
}
