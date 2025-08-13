using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class UserSubscriptionEntityConfiguration : IEntityTypeConfiguration<UserSubscription>
    {
        public void Configure(EntityTypeBuilder<UserSubscription> builder)
        {
            builder.ToTable("UserSubscriptions");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Active");
            
            builder.Property(x => x.PaymentMethod)
                .HasMaxLength(50);
            
            builder.Property(x => x.PaymentReference)
                .HasMaxLength(200);
            
            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("TRY");
            
            builder.Property(x => x.PaidAmount)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.CancellationReason)
                .HasMaxLength(500);
            
            // Indexes
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.SubscriptionTierId);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.UserId, x.IsActive });
            builder.HasIndex(x => x.EndDate);
            
            // Relationships
            builder.HasOne(x => x.SubscriptionTier)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionTierId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}