using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class TicketEntityConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Subject)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(2000)
                .IsRequired();

            builder.Property(t => t.Category)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(t => t.Priority)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Status)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.UserRole)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.ResolutionNotes)
                .HasMaxLength(1000);

            builder.Property(t => t.SatisfactionFeedback)
                .HasMaxLength(500);

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.AssignedToUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.CreatedDate);
            builder.HasIndex(t => t.Priority);
            builder.HasIndex(t => t.Category);
        }
    }
}
