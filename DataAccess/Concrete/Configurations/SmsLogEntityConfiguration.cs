using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SmsLogEntityConfiguration : IEntityTypeConfiguration<SmsLog>
    {
        public void Configure(EntityTypeBuilder<SmsLog> builder)
        {
            builder.ToTable("SmsLogs");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();
            
            builder.Property(x => x.Action)
                .HasColumnName("Action")
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.SenderUserId)
                .HasColumnName("SenderUserId")
                .IsRequired(false); // Nullable

            builder.Property(x => x.Content)
                .HasColumnName("Content")
                .IsRequired()
                .HasColumnType("text"); // JSON content can be large
            
            builder.Property(x => x.CreatedDate)
                .HasColumnName("CreatedDate")
                .IsRequired();
            
            // Index for filtering by action type
            builder.HasIndex(x => x.Action)
                .HasDatabaseName("IX_SmsLogs_Action");

            // Index for filtering by sender
            builder.HasIndex(x => x.SenderUserId)
                .HasDatabaseName("IX_SmsLogs_SenderUserId");

            // Index for date-based queries
            builder.HasIndex(x => x.CreatedDate)
                .HasDatabaseName("IX_SmsLogs_CreatedDate");
        }
    }
}
