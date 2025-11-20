using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    /// <summary>
    /// Entity Framework configuration for BulkCodeDistributionJob entity
    /// Defines table schema, constraints, and relationships
    /// </summary>
    public class BulkCodeDistributionJobEntityConfiguration : IEntityTypeConfiguration<BulkCodeDistributionJob>
    {
        public void Configure(EntityTypeBuilder<BulkCodeDistributionJob> builder)
        {
            builder.ToTable("BulkCodeDistributionJobs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .UseIdentityColumn();

            builder.Property(x => x.SponsorId)
                .HasColumnName("SponsorId")
                .IsRequired();

            builder.Property(x => x.PurchaseId)
                .HasColumnName("PurchaseId")
                .IsRequired();

            builder.Property(x => x.SendSms)
                .HasColumnName("SendSms")
                .IsRequired();

            builder.Property(x => x.DeliveryMethod)
                .HasColumnName("DeliveryMethod")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Direct");

            builder.Property(x => x.TotalFarmers)
                .HasColumnName("TotalFarmers")
                .IsRequired();

            builder.Property(x => x.ProcessedFarmers)
                .HasColumnName("ProcessedFarmers")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.SuccessfulDistributions)
                .HasColumnName("SuccessfulDistributions")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.FailedDistributions)
                .HasColumnName("FailedDistributions")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.Status)
                .HasColumnName("Status")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Pending");

            builder.Property(x => x.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(x => x.StartedDate)
                .HasColumnName("StartedDate");

            builder.Property(x => x.CompletedDate)
                .HasColumnName("CompletedDate");

            builder.Property(x => x.OriginalFileName)
                .HasColumnName("OriginalFileName")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.FileSize)
                .HasColumnName("FileSize")
                .IsRequired();

            builder.Property(x => x.ResultFileUrl)
                .HasColumnName("ResultFileUrl")
                .HasMaxLength(1000);

            builder.Property(x => x.ErrorSummary)
                .HasColumnName("ErrorSummary")
                .HasColumnType("text");

            builder.Property(x => x.TotalCodesDistributed)
                .HasColumnName("TotalCodesDistributed")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.TotalSmsSent)
                .HasColumnName("TotalSmsSent")
                .IsRequired()
                .HasDefaultValue(0);

            // Indexes for performance
            builder.HasIndex(x => x.SponsorId)
                .HasDatabaseName("IX_BulkCodeDistributionJobs_SponsorId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_BulkCodeDistributionJobs_Status");

            builder.HasIndex(x => x.CreatedDate)
                .HasDatabaseName("IX_BulkCodeDistributionJobs_CreatedDate");

            builder.HasIndex(x => new { x.SponsorId, x.CreatedDate })
                .HasDatabaseName("IX_BulkCodeDistributionJobs_SponsorId_CreatedDate");
        }
    }
}
