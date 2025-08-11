using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class ConfigurationEntityConfiguration : IEntityTypeConfiguration<Configuration>
    {
        public void Configure(EntityTypeBuilder<Configuration> builder)
        {
            builder.ToTable("Configurations");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();
                
            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(500);
                
            builder.Property(x => x.Description)
                .HasMaxLength(1000);
                
            builder.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(x => x.ValueType)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(x => x.CreatedDate)
                .IsRequired()
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("NOW()");
                
            builder.Property(x => x.UpdatedDate)
                .HasColumnType("timestamptz");
                
            builder.HasIndex(x => x.Key)
                .IsUnique()
                .HasDatabaseName("IX_Configurations_Key");
                
            builder.HasIndex(x => x.Category)
                .HasDatabaseName("IX_Configurations_Category");
        }
    }
}