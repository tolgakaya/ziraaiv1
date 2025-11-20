using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class TicketMessageEntityConfiguration : IEntityTypeConfiguration<TicketMessage>
    {
        public void Configure(EntityTypeBuilder<TicketMessage> builder)
        {
            builder.HasKey(tm => tm.Id);

            builder.Property(tm => tm.Message)
                .HasMaxLength(2000)
                .IsRequired();

            builder.HasOne(tm => tm.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(tm => tm.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(tm => tm.FromUser)
                .WithMany()
                .HasForeignKey(tm => tm.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(tm => tm.TicketId);
            builder.HasIndex(tm => tm.CreatedDate);
            builder.HasIndex(tm => tm.FromUserId);
        }
    }
}
