using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SubscriptionUsageLogEntityConfiguration : IEntityTypeConfiguration<SubscriptionUsageLog>
    {
        public void Configure(EntityTypeBuilder<SubscriptionUsageLog> builder)
        {
            builder.ToTable("SubscriptionUsageLogs");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.UsageType)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.Property(x => x.RequestEndpoint)
                .HasMaxLength(200);
            
            builder.Property(x => x.RequestMethod)
                .HasMaxLength(10);
            
            builder.Property(x => x.ResponseStatus)
                .HasMaxLength(50);
            
            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(1000);
            
            builder.Property(x => x.IpAddress)
                .HasMaxLength(45); // Support IPv6
            
            builder.Property(x => x.UserAgent)
                .HasMaxLength(500);
            
            builder.Property(x => x.RequestData)
                .HasMaxLength(4000);
            
            // Indexes for performance
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.UserSubscriptionId);
            builder.HasIndex(x => x.PlantAnalysisId);
            builder.HasIndex(x => x.UsageDate);
            builder.HasIndex(x => new { x.UserId, x.UsageDate });
            builder.HasIndex(x => new { x.UserSubscriptionId, x.UsageDate });
            builder.HasIndex(x => x.IsSuccessful);
        }
    }
}