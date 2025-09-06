using Core.Entities.Concrete;
using Core.Utilities.Security.Hashing;
using Entities.Concrete;
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
                UserId = 1,
                CitizenId = 0, // System user
                FullName = "System Administrator",
                Email = "admin@ziraai.com",
                Status = true,
                BirthDate = null, // Optional field
                Gender = null, // Optional field
                AuthenticationProviderType = AuthenticationProviderType.Person.ToString(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RecordDate = DateTime.Now,
                UpdateContactDate = DateTime.Now,
                Address = "System",
                MobilePhones = "+905555555555",
                Notes = "Default system administrator account. Please change password after first login."
            });
            
            // Demo Farmer User
            HashingHelper.CreatePasswordHash("Farmer@123!", out passwordHash, out passwordSalt);
            users.Add(new User
            {
                UserId = 2,
                CitizenId = 0,
                FullName = "Demo Farmer",
                Email = "farmer@demo.ziraai.com",
                Status = true,
                BirthDate = null, // Optional field
                Gender = null, // Optional field
                AuthenticationProviderType = AuthenticationProviderType.Person.ToString(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RecordDate = DateTime.Now,
                UpdateContactDate = DateTime.Now,
                Address = "Demo Farm, Turkey",
                MobilePhones = "+905555555556",
                Notes = "Demo farmer account for testing"
            });
            
            // Demo Sponsor User
            HashingHelper.CreatePasswordHash("Sponsor@123!", out passwordHash, out passwordSalt);
            users.Add(new User
            {
                UserId = 3,
                CitizenId = 0,
                FullName = "Demo Sponsor",
                Email = "sponsor@demo.ziraai.com",
                Status = true,
                BirthDate = null, // Optional field
                Gender = null, // Optional field
                AuthenticationProviderType = AuthenticationProviderType.Person.ToString(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                RecordDate = DateTime.Now,
                UpdateContactDate = DateTime.Now,
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
                    AutoRenew = true,
                    PaymentMethod = "System",
                    PaidAmount = 0,
                    Currency = "TRY",
                    CurrentDailyUsage = 0,
                    CurrentMonthlyUsage = 0,
                    Status = "Active",
                    IsTrialSubscription = false,
                    IsSponsoredSubscription = false,
                    CreatedDate = DateTime.Now
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
                    AutoRenew = false,
                    PaymentMethod = "Trial",
                    PaidAmount = 0,
                    Currency = "TRY",
                    CurrentDailyUsage = 0,
                    CurrentMonthlyUsage = 0,
                    Status = "Active",
                    IsTrialSubscription = true,
                    TrialEndDate = DateTime.Now.AddDays(30),
                    IsSponsoredSubscription = false,
                    CreatedDate = DateTime.Now
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
                    AutoRenew = true,
                    PaymentMethod = "Demo",
                    PaidAmount = 599.99m,
                    Currency = "TRY",
                    CurrentDailyUsage = 0,
                    CurrentMonthlyUsage = 0,
                    Status = "Active",
                    IsTrialSubscription = false,
                    IsSponsoredSubscription = false,
                    CreatedDate = DateTime.Now
                }
            };
        }
        
        public static SponsorProfile GetDefaultSponsorProfile()
        {
            return new SponsorProfile
            {
                Id = 1,
                SponsorId = 3, // Demo Sponsor user
                CompanyName = "Demo Agricultural Supplies Co.",
                CompanyDescription = "Leading provider of agricultural supplies and farmer support services",
                SponsorLogoUrl = "/images/demo-sponsor-logo.png",
                WebsiteUrl = "https://demo.ziraai.com",
                ContactEmail = "sponsor@demo.ziraai.com",
                ContactPhone = "+905555555557",
                ContactPerson = "Demo Sponsor",
                Address = "Demo Plaza, Istanbul, Turkey",
                City = "Istanbul",
                Country = "Turkey",
                PostalCode = "34000",
                CompanyType = "Agriculture",
                BusinessModel = "B2B",
                IsVerified = true,
                IsVerifiedCompany = true,
                VerificationDate = DateTime.Now,
                VerificationNotes = "Demo company - pre-verified",
                IsActive = true,
                TotalPurchases = 0,
                TotalCodesGenerated = 0,
                TotalCodesRedeemed = 0,
                TotalInvestment = 0,
                CreatedDate = DateTime.Now,
                CreatedByUserId = 1
            };
        }
    }
}