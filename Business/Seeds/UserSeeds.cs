using Core.Entities.Concrete;
using Core.Utilities.Security.Hashing;
using System;
using System.Collections.Generic;

namespace Business.Seeds
{
    public static class UserSeeds
    {
        public static List<User> GetDefaultUsers()
        {
            var users = new List<User>();
            
            // Create password hash for default admin
            byte[] passwordHash, passwordSalt;
            HashingHelper.CreatePasswordHash("Admin@123!", out passwordHash, out passwordSalt);
            
            users.Add(new User
            {
                Id = 1,
                CitizenId = 0, // System user
                FullName = "System Administrator",
                Email = "admin@ziraai.com",
                Status = true,
                AuthenticationProviderType = AuthenticationProviderType.Person.ToString(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RecordDate = DateTime.Now,
                Address = "System",
                MobilePhones = "+905555555555",
                Notes = "Default system administrator account. Please change password after first login."
            });
            
            // Demo Farmer User
            HashingHelper.CreatePasswordHash("Farmer@123!", out passwordHash, out passwordSalt);
            users.Add(new User
            {
                Id = 2,
                CitizenId = 0,
                FullName = "Demo Farmer",
                Email = "farmer@demo.ziraai.com",
                Status = true,
                AuthenticationProviderType = AuthenticationProviderType.Person.ToString(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RecordDate = DateTime.Now,
                Address = "Demo Farm, Turkey",
                MobilePhones = "+905555555556",
                Notes = "Demo farmer account for testing"
            });
            
            // Demo Sponsor User
            HashingHelper.CreatePasswordHash("Sponsor@123!", out passwordHash, out passwordSalt);
            users.Add(new User
            {
                Id = 3,
                CitizenId = 0,
                FullName = "Demo Sponsor",
                Email = "sponsor@demo.ziraai.com",
                Status = true,
                AuthenticationProviderType = AuthenticationProviderType.Person.ToString(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RecordDate = DateTime.Now,
                Address = "Demo Company, Turkey",
                MobilePhones = "+905555555557",
                Notes = "Demo sponsor account for testing"
            });
            
            return users;
        }
        
        public static List<UserGroup> GetDefaultUserGroups()
        {
            return new List<UserGroup>
            {
                new UserGroup { GroupId = 1, UserId = 1 }, // Admin user -> Administrators group
                new UserGroup { GroupId = 2, UserId = 2 }, // Demo Farmer -> Farmers group
                new UserGroup { GroupId = 3, UserId = 3 }  // Demo Sponsor -> Sponsors group
            };
        }
        
        public static List<UserSubscription> GetDefaultUserSubscriptions()
        {
            return new List<UserSubscription>
            {
                // Admin gets XL subscription
                new UserSubscription
                {
                    Id = 1,
                    UserId = 1,
                    SubscriptionTierId = 5, // XL
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(10), // 10 years for admin
                    IsActive = true,
                    IsTrial = false,
                    AutoRenew = true,
                    DailyRequestCount = 0,
                    MonthlyRequestCount = 0,
                    LastResetDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    PaymentStatus = "Complimentary",
                    PaymentMethod = "System",
                    Notes = "System administrator complimentary subscription"
                },
                
                // Demo Farmer gets Trial subscription
                new UserSubscription
                {
                    Id = 2,
                    UserId = 2,
                    SubscriptionTierId = 1, // Trial
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30),
                    IsActive = true,
                    IsTrial = true,
                    AutoRenew = false,
                    DailyRequestCount = 0,
                    MonthlyRequestCount = 0,
                    LastResetDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    PaymentStatus = "Trial",
                    PaymentMethod = "Trial",
                    Notes = "Demo farmer trial subscription"
                },
                
                // Demo Sponsor gets L subscription
                new UserSubscription
                {
                    Id = 3,
                    UserId = 3,
                    SubscriptionTierId = 4, // L
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(1),
                    IsActive = true,
                    IsTrial = false,
                    AutoRenew = true,
                    DailyRequestCount = 0,
                    MonthlyRequestCount = 0,
                    LastResetDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    PaymentStatus = "Demo",
                    PaymentMethod = "Demo",
                    Notes = "Demo sponsor subscription"
                }
            };
        }
        
        public static SponsorProfile GetDefaultSponsorProfile()
        {
            return new SponsorProfile
            {
                Id = 1,
                UserId = 3, // Demo Sponsor user
                CompanyName = "Demo Agricultural Supplies Co.",
                CompanyDescription = "Leading provider of agricultural supplies and farmer support services",
                ContactPhone = "+905555555557",
                ContactEmail = "sponsor@demo.ziraai.com",
                Website = "https://demo.ziraai.com",
                Address = "Demo Plaza, Istanbul, Turkey",
                LogoUrl = "/images/demo-sponsor-logo.png",
                IsVerified = true,
                VerificationDate = DateTime.Now,
                TotalSponsored = 0,
                ActiveSponsorships = 0,
                IsActive = true,
                CreatedDate = DateTime.Now,
                SocialMediaLinks = "{\"twitter\":\"@demosponsor\",\"linkedin\":\"demo-agricultural-supplies\"}",
                PreferredCategories = "[\"Vegetables\",\"Fruits\",\"Grains\"]",
                SponsorshipBudget = 100000,
                Currency = "TRY",
                Rating = 5.0m,
                Notes = "Demo sponsor profile for testing purposes"
            };
        }
    }
}