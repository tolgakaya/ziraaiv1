using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    /// <summary>
    /// Entity Framework configuration for BulkSubscriptionAssignmentJob entity
    /// Defines table schema, constraints, and relationships
    /// </summary>
    public class BulkSubscriptionAssignmentJobEntityConfiguration : IEntityTypeConfiguration<BulkSubscriptionAssignmentJob>
    {
        public void Configure(EntityTypeBuilder<BulkSubscriptionAssignmentJob> builder)
        {
            builder.ToTable("BulkSubscriptionAssignmentJobs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .UseIdentityColumn();

            builder.Property(x => x.AdminId)
                .HasColumnName("AdminId")
                .IsRequired();

            builder.Property(x => x.DefaultTierId)
                .HasColumnName("DefaultTierId");

            builder.Property(x => x.DefaultDurationDays)
                .HasColumnName("DefaultDurationDays");

            builder.Property(x => x.SendNotification)
                .HasColumnName("SendNotification")
                .IsRequired();

            builder.Property(x => x.NotificationMethod)
                .HasColumnName("NotificationMethod")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Email");

            builder.Property(x => x.AutoActivate)
                .HasColumnName("AutoActivate")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.TotalFarmers)
                .HasColumnName("TotalFarmers")
                .IsRequired();

            builder.Property(x => x.ProcessedFarmers)
                .HasColumnName("ProcessedFarmers")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.SuccessfulAssignments)
                .HasColumnName("SuccessfulAssignments")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.FailedAssignments)
                .HasColumnName("FailedAssignments")
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

            builder.Property(x => x.NewSubscriptionsCreated)
                .HasColumnName("NewSubscriptionsCreated")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ExistingSubscriptionsUpdated)
                .HasColumnName("ExistingSubscriptionsUpdated")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.TotalNotificationsSent)
                .HasColumnName("TotalNotificationsSent")
                .IsRequired()
                .HasDefaultValue(0);

            // Indexes for performance
            builder.HasIndex(x => x.AdminId)
                .HasDatabaseName("IX_BulkSubscriptionAssignmentJobs_AdminId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_BulkSubscriptionAssignmentJobs_Status");

            builder.HasIndex(x => x.CreatedDate)
                .HasDatabaseName("IX_BulkSubscriptionAssignmentJobs_CreatedDate");

            builder.HasIndex(x => new { x.AdminId, x.CreatedDate })
                .HasDatabaseName("IX_BulkSubscriptionAssignmentJobs_AdminId_CreatedDate");
        }
    }
}
