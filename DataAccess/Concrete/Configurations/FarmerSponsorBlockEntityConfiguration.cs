using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    /// <summary>
    /// Entity Framework configuration for FarmerSponsorBlock
    /// </summary>
    public class FarmerSponsorBlockEntityConfiguration : IEntityTypeConfiguration<FarmerSponsorBlock>
    {
        public void Configure(EntityTypeBuilder<FarmerSponsorBlock> builder)
        {
            builder.ToTable("FarmerSponsorBlocks");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("Id").ValueGeneratedOnAdd();

            builder.Property(x => x.FarmerId).HasColumnName("FarmerId").IsRequired();
            builder.Property(x => x.SponsorId).HasColumnName("SponsorId").IsRequired();
            builder.Property(x => x.IsBlocked).HasColumnName("IsBlocked").IsRequired().HasDefaultValue(false);
            builder.Property(x => x.IsMuted).HasColumnName("IsMuted").IsRequired().HasDefaultValue(false);
            builder.Property(x => x.CreatedDate).HasColumnName("CreatedDate").IsRequired();
            builder.Property(x => x.Reason).HasColumnName("Reason").HasMaxLength(500);

            // Relationships
            builder.HasOne(x => x.Farmer)
                .WithMany()
                .HasForeignKey(x => x.FarmerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sponsor)
                .WithMany()
                .HasForeignKey(x => x.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            builder.HasIndex(x => new { x.FarmerId, x.SponsorId }).IsUnique();
            builder.HasIndex(x => x.FarmerId);
            builder.HasIndex(x => x.SponsorId);
        }
    }
}
