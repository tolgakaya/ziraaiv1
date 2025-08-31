using System;
using System.Collections.Generic;
using Entities.Concrete;
using Entities.Dtos;

namespace Tests.Helpers
{
    public static class SubscriptionDataHelper
    {
        public static SubscriptionTier GetSubscriptionTier(int id = 1, string tierName = "S")
        {
            return new SubscriptionTier
            {
                Id = id,
                TierName = tierName,
                DisplayName = GetDisplayName(tierName),
                Description = GetDescription(tierName),
                DailyRequestLimit = GetDailyLimit(tierName),
                MonthlyRequestLimit = GetMonthlyLimit(tierName),
                MonthlyPrice = GetMonthlyPrice(tierName),
                YearlyPrice = GetYearlyPrice(tierName),
                Currency = "TRY",
                PrioritySupport = tierName == "L" || tierName == "XL",
                AdvancedAnalytics = tierName == "XL",
                ApiAccess = true,
                ResponseTimeHours = GetResponseTime(tierName),
                AdditionalFeatures = GetFeatures(tierName),
                IsActive = true,
                CreatedDate = DateTime.Now.AddMonths(-12),
                UpdatedDate = DateTime.Now.AddMonths(-1)
            };
        }

        public static List<SubscriptionTier> GetSubscriptionTierList()
        {
            return new List<SubscriptionTier>
            {
                GetSubscriptionTier(1, "Trial") with { MonthlyPrice = 0m, YearlyPrice = 0m },
                GetSubscriptionTier(2, "S"),
                GetSubscriptionTier(3, "M"),
                GetSubscriptionTier(4, "L"),
                GetSubscriptionTier(5, "XL")
            };
        }

        public static UserSubscription GetUserSubscription(int userId = 1, int subscriptionTierId = 2)
        {
            var tier = GetSubscriptionTier(subscriptionTierId, GetTierNameById(subscriptionTierId));
            return new UserSubscription
            {
                Id = 1,
                UserId = userId,
                SubscriptionTierId = subscriptionTierId,
                SubscriptionTier = tier,
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now.AddDays(335),
                IsActive = true,
                AutoRenew = true,
                PaymentMethod = "CreditCard",
                PaymentReference = "txn_test_123456",
                PaidAmount = tier.MonthlyPrice,
                Currency = "TRY",
                LastPaymentDate = DateTime.Now.AddDays(-30),
                NextPaymentDate = DateTime.Now.AddDays(335),
                CurrentDailyUsage = 3,
                CurrentMonthlyUsage = 25,
                LastUsageResetDate = DateTime.Now.Date,
                MonthlyUsageResetDate = DateTime.Now.Date.AddDays(-DateTime.Now.Day + 1),
                Status = "Active",
                IsTrialSubscription = false,
                TrialEndDate = null,
                CreatedDate = DateTime.Now.AddDays(-30),
                CreatedUserId = userId,
                UpdatedDate = DateTime.Now.AddHours(-1),
                UpdatedUserId = userId
            };
        }

        public static List<UserSubscription> GetUserSubscriptionList()
        {
            return new List<UserSubscription>
            {
                GetUserSubscription(1, 2), // Active S subscription
                GetUserSubscription(2, 3) with // Active M subscription
                { 
                    Id = 2,
                    CurrentDailyUsage = 8,
                    CurrentMonthlyUsage = 65
                },
                GetUserSubscription(3, 1) with // Trial subscription
                { 
                    Id = 3,
                    IsTrialSubscription = true,
                    TrialEndDate = DateTime.Now.AddDays(5),
                    PaidAmount = 0m
                },
                GetUserSubscription(4, 4) with // Expired L subscription
                { 
                    Id = 4,
                    EndDate = DateTime.Now.AddDays(-5),
                    IsActive = false,
                    Status = "Expired"
                }
            };
        }

        public static SubscriptionUsageLog GetUsageLog(int userId = 1, int subscriptionId = 1)
        {
            return new SubscriptionUsageLog
            {
                Id = 1,
                UserId = userId,
                UserSubscriptionId = subscriptionId,
                PlantAnalysisId = 123,
                UsageType = "PlantAnalysis",
                UsageDate = DateTime.Now.AddHours(-1),
                RequestEndpoint = "/api/plantanalyses/analyze",
                RequestMethod = "POST",
                IsSuccessful = true,
                ResponseStatus = "200",
                ErrorMessage = null,
                QuotaUsedDaily = 4,
                QuotaLimitDaily = 5,
                QuotaUsedMonthly = 26,
                QuotaLimitMonthly = 50,
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                ResponseTimeMs = 1250,
                CreatedDate = DateTime.Now.AddHours(-1)
            };
        }

        public static List<SubscriptionUsageLog> GetUsageLogList()
        {
            return new List<SubscriptionUsageLog>
            {
                GetUsageLog(1, 1),
                GetUsageLog(1, 1) with 
                { 
                    Id = 2, 
                    UsageDate = DateTime.Now.AddHours(-3),
                    QuotaUsedDaily = 3,
                    QuotaUsedMonthly = 25
                },
                GetUsageLog(2, 2) with 
                { 
                    Id = 3,
                    QuotaUsedDaily = 9,
                    QuotaLimitDaily = 20,
                    QuotaUsedMonthly = 66,
                    QuotaLimitMonthly = 200
                }
            };
        }

        // DTOs for API requests
        public static CreateUserSubscriptionDto GetValidCreateSubscriptionDto()
        {
            return new CreateUserSubscriptionDto
            {
                SubscriptionTierId = 2, // S tier
                DurationMonths = 1,
                AutoRenew = true,
                PaymentMethod = "CreditCard",
                PaymentReference = "test_txn_789",
                PaidAmount = 99.99m,
                Currency = "TRY",
                IsTrialSubscription = false,
                TrialDays = null
            };
        }

        public static CreateUserSubscriptionDto GetValidTrialSubscriptionDto()
        {
            return new CreateUserSubscriptionDto
            {
                SubscriptionTierId = 1, // Trial tier
                DurationMonths = 1,
                AutoRenew = false,
                PaymentMethod = null,
                PaymentReference = null,
                PaidAmount = 0m,
                Currency = "TRY",
                IsTrialSubscription = true,
                TrialDays = 30
            };
        }

        public static CancelSubscriptionDto GetValidCancelSubscriptionDto()
        {
            return new CancelSubscriptionDto
            {
                UserSubscriptionId = 1,
                CancellationReason = "Too expensive for my needs",
                ImmediateCancellation = false
            };
        }

        public static UpdateSubscriptionTierDto GetValidUpdateTierDto()
        {
            return new UpdateSubscriptionTierDto
            {
                DisplayName = "Updated Small Plan",
                Description = "Updated small plan with new features",
                DailyRequestLimit = 7,
                MonthlyRequestLimit = 70,
                MonthlyPrice = 119.99m,
                YearlyPrice = 1199.99m,
                Currency = "TRY",
                PrioritySupport = false,
                AdvancedAnalytics = false,
                ApiAccess = true,
                ResponseTimeHours = 48,
                Features = new List<string> { "Basic Analysis", "Email Support" },
                IsActive = true
            };
        }

        // Helper methods
        private static string GetDisplayName(string tierName)
        {
            return tierName switch
            {
                "Trial" => "Trial",
                "S" => "Small",
                "M" => "Medium",
                "L" => "Large",
                "XL" => "Extra Large",
                _ => tierName
            };
        }

        private static string GetDescription(string tierName)
        {
            return tierName switch
            {
                "Trial" => "30-day trial with limited access",
                "S" => "Basic plan for individual farmers",
                "M" => "Enhanced plan with more features",
                "L" => "Professional plan with priority support",
                "XL" => "Premium plan with all features",
                _ => "Custom subscription tier"
            };
        }

        private static int GetDailyLimit(string tierName)
        {
            return tierName switch
            {
                "Trial" => 1,
                "S" => 5,
                "M" => 20,
                "L" => 50,
                "XL" => 200,
                _ => 1
            };
        }

        private static int GetMonthlyLimit(string tierName)
        {
            return tierName switch
            {
                "Trial" => 30,
                "S" => 50,
                "M" => 200,
                "L" => 500,
                "XL" => 2000,
                _ => 30
            };
        }

        private static decimal GetMonthlyPrice(string tierName)
        {
            return tierName switch
            {
                "Trial" => 0m,
                "S" => 99.99m,
                "M" => 299.99m,
                "L" => 599.99m,
                "XL" => 1499.99m,
                _ => 0m
            };
        }

        private static decimal GetYearlyPrice(string tierName)
        {
            return GetMonthlyPrice(tierName) * 10; // 2 months free
        }

        private static int GetResponseTime(string tierName)
        {
            return tierName switch
            {
                "Trial" => 72,
                "S" => 48,
                "M" => 24,
                "L" => 12,
                "XL" => 4,
                _ => 72
            };
        }

        private static string GetFeatures(string tierName)
        {
            var features = tierName switch
            {
                "Trial" => new[] { "Basic Analysis" },
                "S" => new[] { "Basic Analysis", "Email Support" },
                "M" => new[] { "Enhanced Analysis", "Email Support", "Dashboard" },
                "L" => new[] { "Professional Analysis", "Priority Support", "Advanced Dashboard", "API Access" },
                "XL" => new[] { "Premium Analysis", "24/7 Support", "Advanced Dashboard", "Full API Access", "Custom Reports" },
                _ => new[] { "Basic Features" }
            };
            
            return Newtonsoft.Json.JsonConvert.SerializeObject(features);
        }

        private static string GetTierNameById(int id)
        {
            return id switch
            {
                1 => "Trial",
                2 => "S",
                3 => "M",
                4 => "L",
                5 => "XL",
                _ => "S"
            };
        }
    }
}