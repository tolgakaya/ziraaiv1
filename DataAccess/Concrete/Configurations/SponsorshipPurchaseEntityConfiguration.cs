using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SponsorshipPurchaseEntityConfiguration : IEntityTypeConfiguration<SponsorshipPurchase>
    {
        public void Configure(EntityTypeBuilder<SponsorshipPurchase> builder)
        {
            builder.ToTable("SponsorshipPurchases");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();
            
            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("TRY");
            
            builder.Property(x => x.PaymentMethod)
                .HasMaxLength(50);
            
            builder.Property(x => x.PaymentReference)
                .HasMaxLength(200);
            
            builder.Property(x => x.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            
            builder.Property(x => x.InvoiceNumber)
                .HasMaxLength(100);
            
            builder.Property(x => x.InvoiceAddress)
                .HasMaxLength(500);
            
            builder.Property(x => x.TaxNumber)
                .HasMaxLength(50);
            
            builder.Property(x => x.CompanyName)
                .HasMaxLength(200);
            
            builder.Property(x => x.CodePrefix)
                .HasMaxLength(10)
                .HasDefaultValue("AGRI");
            
            builder.Property(x => x.ValidityDays)
                .HasDefaultValue(30);
            
            builder.Property(x => x.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");
            
            builder.Property(x => x.Notes)
                .HasMaxLength(1000);
            
            builder.Property(x => x.PurchaseReason)
                .HasMaxLength(500);
            
            // Tier-Based Features removed - now calculated dynamically from SponsorshipCode -> Tier relationship
            
            builder.Property(x => x.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Relationships
            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.SubscriptionTier)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionTierId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Sponsor Profile relationship
            builder.HasOne(x => x.SponsorProfile)
                .WithMany(x => x.SponsorshipPurchases)
                .HasForeignKey(x => x.SponsorId)
                .HasPrincipalKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            builder.HasIndex(x => x.SponsorId)
                .HasDatabaseName("IX_SponsorshipPurchases_SponsorId");
            
            builder.HasIndex(x => x.PaymentStatus)
                .HasDatabaseName("IX_SponsorshipPurchases_PaymentStatus");
            
            builder.HasIndex(x => x.InvoiceNumber)
                .HasDatabaseName("IX_SponsorshipPurchases_InvoiceNumber");
        }
    }
}