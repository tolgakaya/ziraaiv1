using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class SmartLinkEntityConfiguration : IEntityTypeConfiguration<SmartLink>
    {
        public void Configure(EntityTypeBuilder<SmartLink> builder)
        {
            builder.ToTable("SmartLinks");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.SponsorName)
                .HasMaxLength(200);
            
            builder.Property(x => x.LinkUrl)
                .IsRequired()
                .HasMaxLength(1000);
            
            builder.Property(x => x.LinkText)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(x => x.LinkDescription)
                .HasMaxLength(1000);
            
            builder.Property(x => x.LinkType)
                .HasMaxLength(50);
            
            builder.Property(x => x.Keywords)
                .HasMaxLength(2000);
            
            builder.Property(x => x.ProductCategory)
                .HasMaxLength(100);
            
            builder.Property(x => x.TargetCropTypes)
                .HasMaxLength(1000);
            
            builder.Property(x => x.TargetDiseases)
                .HasMaxLength(2000);
            
            builder.Property(x => x.TargetPests)
                .HasMaxLength(2000);
            
            builder.Property(x => x.TargetNutrientDeficiencies)
                .HasMaxLength(1000);
            
            builder.Property(x => x.TargetGrowthStages)
                .HasMaxLength(500);
            
            builder.Property(x => x.TargetRegions)
                .HasMaxLength(1000);
            
            builder.Property(x => x.DisplayPosition)
                .HasMaxLength(50);
            
            builder.Property(x => x.DisplayStyle)
                .HasMaxLength(50);
            
            builder.Property(x => x.IconUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.BackgroundColor)
                .HasMaxLength(10);
            
            builder.Property(x => x.TextColor)
                .HasMaxLength(10);
            
            builder.Property(x => x.ProductName)
                .HasMaxLength(200);
            
            builder.Property(x => x.ProductImageUrl)
                .HasMaxLength(500);
            
            builder.Property(x => x.ProductPrice)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.ProductCurrency)
                .HasMaxLength(10);
            
            builder.Property(x => x.ProductUnit)
                .HasMaxLength(50);
            
            builder.Property(x => x.DiscountPercentage)
                .HasPrecision(5, 2);
            
            builder.Property(x => x.ClickThroughRate)
                .HasPrecision(5, 2);
            
            builder.Property(x => x.ConversionRate)
                .HasPrecision(5, 2);
            
            builder.Property(x => x.ClickHistory)
                .HasMaxLength(4000);
            
            builder.Property(x => x.TestVariant)
                .HasMaxLength(10);
            
            builder.Property(x => x.TestPerformanceScore)
                .HasPrecision(5, 2);
            
            builder.Property(x => x.ActiveDays)
                .HasMaxLength(100);
            
            builder.Property(x => x.ActiveHours)
                .HasMaxLength(100);
            
            builder.Property(x => x.CostPerClick)
                .HasPrecision(18, 4);
            
            builder.Property(x => x.TotalBudget)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.SpentBudget)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.BillingType)
                .HasMaxLength(50);
            
            builder.Property(x => x.ApprovalNotes)
                .HasMaxLength(500);
            
            builder.Property(x => x.ComplianceNotes)
                .HasMaxLength(500);
            
            builder.Property(x => x.RelevanceScore)
                .HasPrecision(5, 2);
            
            builder.Property(x => x.AiRecommendations)
                .HasMaxLength(4000);
            
            // Relationships
            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            builder.HasIndex(x => x.SponsorId);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.ProductCategory);
            builder.HasIndex(x => x.IsApproved);
            builder.HasIndex(x => new { x.IsActive, x.Priority });
        }
    }
}