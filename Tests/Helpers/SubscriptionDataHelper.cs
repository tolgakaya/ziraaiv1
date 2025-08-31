using System;
using System.Collections.Generic;
using Entities.Concrete;
using Entities.Dtos;

namespace Tests.Helpers
{
    public static class SubscriptionDataHelper
    {
        public static SubscriptionTier GetSubscriptionTier(string name = "S")
        {
            return new SubscriptionTier
            {
                Id = 1,
                TierName = name,
                DisplayName = $"{name} Plan",
                Description = $"Professional {name} tier subscription",
                DailyRequestLimit = GetDailyLimitForTier(name),
                MonthlyRequestLimit = GetMonthlyLimitForTier(name),
                MonthlyPrice = GetPriceForTier(name),
                YearlyPrice = GetPriceForTier(name) * 10m,
                Currency = "TRY",
                PrioritySupport = name != "S",
                AdvancedAnalytics = name == "L" || name == "XL",
                ApiAccess = name == "XL",
                ResponseTimeHours = name == "XL" ? 2 : 24,
                AdditionalFeatures = $"[\"Priority support\", \"{name} tier features\"]",
                IsActive = true,
                DisplayOrder = GetSortOrderForTier(name),
                CreatedDate = DateTime.Now.AddDays(-60),
                UpdatedDate = DateTime.Now
            };
        }

        public static SubscriptionTier GetTrialTier()
        {
            return new SubscriptionTier
            {
                Id = 0,
                TierName = "Trial",
                DisplayName = "Trial Plan",
                Description = "Free trial subscription",
                DailyRequestLimit = 5,
                MonthlyRequestLimit = 20,
                MonthlyPrice = 0,
                YearlyPrice = 0,
                Currency = "TRY",
                PrioritySupport = false,
                AdvancedAnalytics = false,
                ApiAccess = false,
                ResponseTimeHours = 48,
                AdditionalFeatures = "[\"Limited analysis\", \"Basic support\"]",
                IsActive = true,
                DisplayOrder = 0,
                CreatedDate = DateTime.Now.AddDays(-60)
            };
        }

        public static UserSubscription GetActiveUserSubscription(int userId = 1)
        {
            return new UserSubscription
            {
                Id = 1,
                UserId = userId,
                SubscriptionTierId = 1,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(20),
                IsActive = true,
                AutoRenew = true,
                PaymentMethod = "Credit Card",
                PaymentReference = "txn_subscription_123",
                Status = "Active",
                PaidAmount = 299m,
                Currency = "TRY",
                CreatedDate = DateTime.Now.AddDays(-10),
                UpdatedDate = DateTime.Now
            };
        }

        public static UserSubscription GetExpiredUserSubscription(int userId = 2)
        {
            var subscription = GetActiveUserSubscription(userId);
            subscription.Id = 2;
            subscription.EndDate = DateTime.Now.AddDays(-5);
            subscription.IsActive = false;
            return subscription;
        }

        public static UserSubscription GetTrialUserSubscription(int userId = 3)
        {
            var subscription = GetActiveUserSubscription(userId);
            subscription.Id = 3;
            subscription.SubscriptionTierId = 0; // Trial tier
            subscription.StartDate = DateTime.Now.AddDays(-3);
            subscription.EndDate = DateTime.Now.AddDays(4);
            subscription.AutoRenew = false;
            subscription.PaymentReference = null;
            subscription.PaymentMethod = "Trial";
            subscription.PaidAmount = 0;
            return subscription;
        }

        public static SubscriptionUsageLog GetSubscriptionUsageLog(int userId = 1)
        {
            return new SubscriptionUsageLog
            {
                Id = 1,
                UserId = userId,
                UserSubscriptionId = 1,
                UsageDate = DateTime.Now.AddHours(-2),
                UsageType = "PlantAnalysis",
                RequestEndpoint = "/api/plantanalyses",
                RequestMethod = "POST",
                IsSuccessful = true,
                ResponseStatus = "Success",
                DailyQuotaUsed = 3,
                DailyQuotaLimit = 50,
                MonthlyQuotaUsed = 45,
                MonthlyQuotaLimit = 500,
                CreatedDate = DateTime.Now.AddHours(-2)
            };
        }

        public static SubscriptionUsageLog GetHighUsageLog(int userId = 1)
        {
            var usage = GetSubscriptionUsageLog(userId);
            usage.Id = 2;
            usage.DailyQuotaUsed = 45; // Near daily limit
            return usage;
        }

        public static SubscriptionUsageLog GetExceededUsageLog(int userId = 1)
        {
            var usage = GetSubscriptionUsageLog(userId);
            usage.Id = 3;
            usage.DailyQuotaUsed = 55; // Over daily limit of 50
            usage.ResponseStatus = "RateLimited";
            return usage;
        }

        public static List<SubscriptionTier> GetSubscriptionTierList()
        {
            return new List<SubscriptionTier>
            {
                GetTrialTier(),
                GetSubscriptionTier("S"),
                GetSubscriptionTier("M"),
                GetSubscriptionTier("L"),
                GetSubscriptionTier("XL")
            };
        }

        public static SubscriptionUsageStatusDto GetSubscriptionUsageStatusDto(int userId = 1)
        {
            return new SubscriptionUsageStatusDto
            {
                DailyUsed = 3,
                DailyLimit = 50,
                MonthlyUsed = 45,
                MonthlyLimit = 500,
                CanMakeRequest = true,
                TierName = "S"
            };
        }

        private static int GetDailyLimitForTier(string tier)
        {
            return tier switch
            {
                "S" => 50,
                "M" => 100,
                "L" => 200,
                "XL" => 500,
                _ => 10
            };
        }

        private static int GetMonthlyLimitForTier(string tier)
        {
            return tier switch
            {
                "S" => 500,
                "M" => 1000,
                "L" => 2500,
                "XL" => 10000,
                _ => 100
            };
        }

        private static decimal GetPriceForTier(string tier)
        {
            return tier switch
            {
                "S" => 299m,
                "M" => 599m,
                "L" => 999m,
                "XL" => 1999m,
                _ => 0m
            };
        }

        private static int GetSortOrderForTier(string tier)
        {
            return tier switch
            {
                "S" => 1,
                "M" => 2,
                "L" => 3,
                "XL" => 4,
                _ => 0
            };
        }
    }
}