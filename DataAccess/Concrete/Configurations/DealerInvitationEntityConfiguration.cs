using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    /// <summary>
    /// Entity Framework configuration for DealerInvitation entity
    /// Defines table schema, constraints, and relationships
    /// </summary>
    public class DealerInvitationEntityConfiguration : IEntityTypeConfiguration<DealerInvitation>
    {
        public void Configure(EntityTypeBuilder<DealerInvitation> builder)
        {
            builder.ToTable("DealerInvitations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .UseIdentityColumn();

            builder.Property(x => x.SponsorId)
                .HasColumnName("SponsorId")
                .IsRequired();

            builder.Property(x => x.Email)
                .HasColumnName("Email")
                .HasMaxLength(255);

            builder.Property(x => x.Phone)
                .HasColumnName("Phone")
                .HasMaxLength(20);

            builder.Property(x => x.DealerName)
                .HasColumnName("DealerName")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("Status")
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue("Pending");

            builder.Property(x => x.InvitationType)
                .HasColumnName("InvitationType")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.InvitationToken)
                .HasColumnName("InvitationToken")
                .HasMaxLength(255);

            builder.Property(x => x.PurchaseId)
                .HasColumnName("PurchaseId")
                .IsRequired();

            builder.Property(x => x.CodeCount)
                .HasColumnName("CodeCount")
                .IsRequired();

            builder.Property(x => x.CreatedDealerId)
                .HasColumnName("CreatedDealerId");

            builder.Property(x => x.AcceptedDate)
                .HasColumnName("AcceptedDate");

            builder.Property(x => x.AutoCreatedPassword)
                .HasColumnName("AutoCreatedPassword")
                .HasMaxLength(255);

            builder.Property(x => x.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();

            builder.Property(x => x.ExpiryDate)
                .HasColumnName("ExpiryDate")
                .IsRequired();

            builder.Property(x => x.CancelledDate)
                .HasColumnName("CancelledDate");

            builder.Property(x => x.CancelledByUserId)
                .HasColumnName("CancelledByUserId");

            builder.Property(x => x.Notes)
                .HasColumnName("Notes")
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(x => x.InvitationToken)
                .IsUnique()
                .HasDatabaseName("IX_DealerInvitations_InvitationToken");

            builder.HasIndex(x => x.SponsorId)
                .HasDatabaseName("IX_DealerInvitations_SponsorId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_DealerInvitations_Status");

            builder.HasIndex(x => x.Email)
                .HasDatabaseName("IX_DealerInvitations_Email");
        }
    }
}
