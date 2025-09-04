using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class AnalysisMessageEntityConfiguration : IEntityTypeConfiguration<AnalysisMessage>
    {
        public void Configure(EntityTypeBuilder<AnalysisMessage> builder)
        {
            builder.ToTable("AnalysisMessages");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(4000);
            
            builder.Property(x => x.MessageType)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.Property(x => x.Subject)
                .HasMaxLength(200);
            
            builder.Property(x => x.SenderRole)
                .HasMaxLength(50);
            
            builder.Property(x => x.SenderName)
                .HasMaxLength(100);
            
            builder.Property(x => x.SenderCompany)
                .HasMaxLength(200);
            
            builder.Property(x => x.AttachmentUrls)
                .HasMaxLength(2000);
            
            builder.Property(x => x.LinkedProducts)
                .HasMaxLength(2000);
            
            builder.Property(x => x.RecommendedActions)
                .HasMaxLength(2000);
            
            builder.Property(x => x.Priority)
                .HasMaxLength(20);
            
            builder.Property(x => x.Category)
                .HasMaxLength(50);
            
            builder.Property(x => x.FlagReason)
                .HasMaxLength(500);
            
            builder.Property(x => x.RatingFeedback)
                .HasMaxLength(500);
            
            builder.Property(x => x.ModerationNotes)
                .HasMaxLength(500);
            
            builder.Property(x => x.IpAddress)
                .HasMaxLength(50);
            
            builder.Property(x => x.UserAgent)
                .HasMaxLength(500);
            
            // Relationships
            builder.HasOne(x => x.PlantAnalysis)
                .WithMany()
                .HasForeignKey(x => x.PlantAnalysisId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(x => x.FromUser)
                .WithMany()
                .HasForeignKey(x => x.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.ToUser)
                .WithMany()
                .HasForeignKey(x => x.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.ParentMessage)
                .WithMany()
                .HasForeignKey(x => x.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            builder.HasIndex(x => x.PlantAnalysisId);
            builder.HasIndex(x => x.FromUserId);
            builder.HasIndex(x => x.ToUserId);
            builder.HasIndex(x => x.SentDate);
            builder.HasIndex(x => x.IsRead);
            builder.HasIndex(x => x.IsDeleted);
            builder.HasIndex(x => x.MessageType);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => new { x.ToUserId, x.IsRead });
        }
    }
}