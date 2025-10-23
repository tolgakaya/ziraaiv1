using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    /// <summary>
    /// Entity Framework configuration for AdminOperationLog entity
    /// Defines table structure, relationships, and indexes for audit trail
    /// </summary>
    public class AdminOperationLogEntityConfiguration : IEntityTypeConfiguration<AdminOperationLog>
    {
        public void Configure(EntityTypeBuilder<AdminOperationLog> builder)
        {
            // Table name
            builder.ToTable("AdminOperationLogs");

            // Primary Key
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .UseIdentityColumn();

            // Required fields
            builder.Property(x => x.AdminUserId)
                .HasColumnName("AdminUserId")
                .IsRequired();

            builder.Property(x => x.Action)
                .HasColumnName("Action")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Timestamp)
                .HasColumnName("Timestamp")
                .IsRequired();

            builder.Property(x => x.IsOnBehalfOf)
                .HasColumnName("IsOnBehalfOf")
                .HasDefaultValue(false)
                .IsRequired();

            // Optional fields
            builder.Property(x => x.TargetUserId)
                .HasColumnName("TargetUserId");

            builder.Property(x => x.EntityType)
                .HasColumnName("EntityType")
                .HasMaxLength(50);

            builder.Property(x => x.EntityId)
                .HasColumnName("EntityId");

            builder.Property(x => x.IpAddress)
                .HasColumnName("IpAddress")
                .HasMaxLength(45); // IPv6 compatible

            builder.Property(x => x.UserAgent)
                .HasColumnName("UserAgent")
                .HasColumnType("TEXT");

            builder.Property(x => x.RequestPath)
                .HasColumnName("RequestPath")
                .HasMaxLength(500);

            builder.Property(x => x.RequestPayload)
                .HasColumnName("RequestPayload")
                .HasColumnType("TEXT");

            builder.Property(x => x.ResponseStatus)
                .HasColumnName("ResponseStatus");

            builder.Property(x => x.Duration)
                .HasColumnName("Duration");

            builder.Property(x => x.Reason)
                .HasColumnName("Reason")
                .HasColumnType("TEXT");

            builder.Property(x => x.BeforeState)
                .HasColumnName("BeforeState")
                .HasColumnType("TEXT");

            builder.Property(x => x.AfterState)
                .HasColumnName("AfterState")
                .HasColumnType("TEXT");

            // Foreign Key Relationships
            builder.HasOne(x => x.AdminUser)
                .WithMany()
                .HasForeignKey(x => x.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict) // Don't cascade delete logs
                .HasConstraintName("FK_AdminOperationLogs_AdminUser");

            builder.HasOne(x => x.TargetUser)
                .WithMany()
                .HasForeignKey(x => x.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict) // Don't cascade delete logs
                .HasConstraintName("FK_AdminOperationLogs_TargetUser");

            // Indexes for performance (matching SQL migration)
            builder.HasIndex(x => x.AdminUserId)
                .HasDatabaseName("IX_AdminOperationLogs_AdminUserId");

            builder.HasIndex(x => x.TargetUserId)
                .HasDatabaseName("IX_AdminOperationLogs_TargetUserId");

            builder.HasIndex(x => x.Action)
                .HasDatabaseName("IX_AdminOperationLogs_Action");

            builder.HasIndex(x => x.Timestamp)
                .IsDescending()
                .HasDatabaseName("IX_AdminOperationLogs_Timestamp");

            builder.HasIndex(x => new { x.EntityType, x.EntityId })
                .HasDatabaseName("IX_AdminOperationLogs_Entity");

            builder.HasIndex(x => x.IsOnBehalfOf)
                .HasDatabaseName("IX_AdminOperationLogs_IsOnBehalfOf")
                .HasFilter("\"IsOnBehalfOf\" = TRUE"); // Partial index

            builder.HasIndex(x => x.IpAddress)
                .HasDatabaseName("IX_AdminOperationLogs_IpAddress");

            builder.HasIndex(x => new { x.AdminUserId, x.Timestamp })
                .IsDescending(false, true)
                .HasDatabaseName("IX_AdminOperationLogs_AdminUser_Timestamp");
        }
    }
}
