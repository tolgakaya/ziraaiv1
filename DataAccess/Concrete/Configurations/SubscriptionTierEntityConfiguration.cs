using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DataAccess.Concrete.Configurations
{
    public class SubscriptionTierEntityConfiguration : IEntityTypeConfiguration<SubscriptionTier>
    {
        public void Configure(EntityTypeBuilder<SubscriptionTier> builder)
        {
            builder.ToTable("SubscriptionTiers");
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.TierName)
                .IsRequired()
                .HasMaxLength(10);
            
            builder.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.Property(x => x.Description)
                .HasMaxLength(500);
            
            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("TRY");
            
            builder.Property(x => x.MonthlyPrice)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.YearlyPrice)
                .HasPrecision(18, 2);
            
            builder.Property(x => x.AdditionalFeatures)
                .HasMaxLength(2000);
            
            builder.HasIndex(x => x.TierName)
                .IsUnique();
            
            builder.HasIndex(x => x.IsActive);
            
            // Seed data for Trial, S, M, L, XL tiers
            builder.HasData(
                new SubscriptionTier
                {
                    Id = 1,
                    TierName = "Trial",
                    DisplayName = "Trial",
                    Description = "30-day trial with limited access",
                    DailyRequestLimit = 1,
                    MonthlyRequestLimit = 30,
                    MonthlyPrice = 0m,
                    YearlyPrice = 0m,
                    Currency = "TRY",
                    PrioritySupport = false,
                    AdvancedAnalytics = false,
                    ApiAccess = false,
                    ResponseTimeHours = 72,
                    AdditionalFeatures = JsonConvert.SerializeObject(new List<string> 
                    { 
                        "Basic plant analysis",
                        "Email notifications",
                        "Trial access"
                    }),
                    IsActive = true,
                    DisplayOrder = 0,
                    CreatedDate = new DateTime(2025, 8, 13, 16, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionTier
                {
                    Id = 2,
                    TierName = "S",
                    DisplayName = "Small",
                    Description = "Perfect for small farms and hobbyists",
                    DailyRequestLimit = 5,
                    MonthlyRequestLimit = 50,
                    MonthlyPrice = 99.99m,
                    YearlyPrice = 999.99m,
                    Currency = "TRY",
                    PrioritySupport = false,
                    AdvancedAnalytics = false,
                    ApiAccess = false,
                    ResponseTimeHours = 48,
                    AdditionalFeatures = JsonConvert.SerializeObject(new List<string> 
                    { 
                        "Basic plant analysis",
                        "Email notifications",
                        "Basic reports"
                    }),
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedDate = new DateTime(2025, 8, 13, 16, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionTier
                {
                    Id = 3,
                    TierName = "M",
                    DisplayName = "Medium",
                    Description = "Ideal for medium-sized farms",
                    DailyRequestLimit = 20,
                    MonthlyRequestLimit = 200,
                    MonthlyPrice = 299.99m,
                    YearlyPrice = 2999.99m,
                    Currency = "TRY",
                    PrioritySupport = false,
                    AdvancedAnalytics = true,
                    ApiAccess = false,
                    ResponseTimeHours = 24,
                    AdditionalFeatures = JsonConvert.SerializeObject(new List<string> 
                    { 
                        "Advanced plant analysis",
                        "Email & SMS notifications",
                        "Detailed reports",
                        "Historical data access",
                        "Basic API access"
                    }),
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedDate = new DateTime(2025, 8, 13, 16, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionTier
                {
                    Id = 4,
                    TierName = "L",
                    DisplayName = "Large",
                    Description = "Best for large commercial farms",
                    DailyRequestLimit = 50,
                    MonthlyRequestLimit = 500,
                    MonthlyPrice = 599.99m,
                    YearlyPrice = 5999.99m,
                    Currency = "TRY",
                    PrioritySupport = true,
                    AdvancedAnalytics = true,
                    ApiAccess = true,
                    ResponseTimeHours = 12,
                    AdditionalFeatures = JsonConvert.SerializeObject(new List<string> 
                    { 
                        "Premium plant analysis with AI insights",
                        "All notification channels",
                        "Custom reports",
                        "Full historical data",
                        "Full API access",
                        "Priority support",
                        "Export capabilities"
                    }),
                    IsActive = true,
                    DisplayOrder = 4,
                    CreatedDate = new DateTime(2025, 8, 13, 16, 0, 0, DateTimeKind.Utc)
                },
                new SubscriptionTier
                {
                    Id = 5,
                    TierName = "XL",
                    DisplayName = "Extra Large",
                    Description = "Enterprise solution for agricultural corporations",
                    DailyRequestLimit = 200,
                    MonthlyRequestLimit = 2000,
                    MonthlyPrice = 1499.99m,
                    YearlyPrice = 14999.99m,
                    Currency = "TRY",
                    PrioritySupport = true,
                    AdvancedAnalytics = true,
                    ApiAccess = true,
                    ResponseTimeHours = 6,
                    AdditionalFeatures = JsonConvert.SerializeObject(new List<string> 
                    { 
                        "Enterprise AI analysis with custom models",
                        "All features included",
                        "Dedicated support team",
                        "Custom integrations",
                        "White-label options",
                        "SLA guarantee",
                        "Training sessions",
                        "Unlimited data retention"
                    }),
                    IsActive = true,
                    DisplayOrder = 5,
                    CreatedDate = new DateTime(2025, 8, 13, 16, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}