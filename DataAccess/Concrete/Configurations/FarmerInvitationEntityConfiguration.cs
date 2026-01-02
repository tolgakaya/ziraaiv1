using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    /// <summary>
    /// Entity Framework configuration for FarmerInvitation entity
    /// Defines table schema, constraints, indexes, and relationships
    /// </summary>
    public class FarmerInvitationEntityConfiguration : IEntityTypeConfiguration<FarmerInvitation>
    {
        public void Configure(EntityTypeBuilder<FarmerInvitation> builder)
        {
            // Table mapping
            builder.ToTable("FarmerInvitation");

            // Primary key
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .UseIdentityColumn();

            // Sponsor information
            builder.Property(x => x.SponsorId)
                .HasColumnName("SponsorId")
                .IsRequired();

            // Farmer information
            builder.Property(x => x.Phone)
                .HasColumnName("Phone")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.FarmerName)
                .HasColumnName("FarmerName")
                .HasMaxLength(200);

            builder.Property(x => x.Email)
                .HasColumnName("Email")
                .HasMaxLength(200);

            // Invitation details
            builder.Property(x => x.InvitationToken)
                .HasColumnName("InvitationToken")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("Status")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Pending");

            builder.Property(x => x.InvitationType)
                .HasColumnName("InvitationType")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Invite");

            // Code information
            builder.Property(x => x.CodeCount)
                .HasColumnName("CodeCount")
                .IsRequired();

            builder.Property(x => x.PackageTier)
                .HasColumnName("PackageTier")
                .HasMaxLength(10)
                .IsRequired(false); // Optional tier filter

            // Acceptance tracking
            builder.Property(x => x.AcceptedByUserId)
                .HasColumnName("AcceptedByUserId")
                .IsRequired(false);

            builder.Property(x => x.AcceptedDate)
                .HasColumnName("AcceptedDate")
                .IsRequired(false);

            // SMS/Messaging tracking
            builder.Property(x => x.LinkSentDate)
                .HasColumnName("LinkSentDate")
                .IsRequired(false);

            builder.Property(x => x.LinkSentVia)
                .HasColumnName("LinkSentVia")
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(x => x.LinkDelivered)
                .HasColumnName("LinkDelivered")
                .IsRequired()
                .HasDefaultValue(false);

            // Lifecycle
            builder.Property(x => x.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(x => x.ExpiryDate)
                .HasColumnName("ExpiryDate")
                .IsRequired();

            builder.Property(x => x.CancelledDate)
                .HasColumnName("CancelledDate")
                .IsRequired(false);

            builder.Property(x => x.Notes)
                .HasColumnName("Notes")
                .IsRequired(false);

            // Indexes
            builder.HasIndex(x => x.InvitationToken)
                .IsUnique()
                .HasDatabaseName("IX_FarmerInvitation_Token");

            builder.HasIndex(x => x.SponsorId)
                .HasDatabaseName("IX_FarmerInvitation_SponsorId");

            builder.HasIndex(x => x.Phone)
                .HasDatabaseName("IX_FarmerInvitation_Phone");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_FarmerInvitation_Status");

            // Composite index for common query pattern (sponsor filtering by status)
            builder.HasIndex(x => new { x.SponsorId, x.Status })
                .HasDatabaseName("IX_FarmerInvitation_SponsorId_Status");
        }
    }
}
