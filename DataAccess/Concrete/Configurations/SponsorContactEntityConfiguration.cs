using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SponsorContactEntityConfiguration : IEntityTypeConfiguration<SponsorContact>
    {
        public void Configure(EntityTypeBuilder<SponsorContact> builder)
        {
            builder.ToTable("SponsorContacts");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.ContactName)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(x => x.Source)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Manual");
                
            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(x => x.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(x => x.UpdatedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            // Composite index for sponsor and phone
            builder.HasIndex(x => new { x.SponsorId, x.PhoneNumber })
                .IsUnique();
                
            // Relationships
            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}