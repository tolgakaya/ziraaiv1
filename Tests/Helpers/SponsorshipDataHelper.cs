using System;
using System.Collections.Generic;
using Entities.Concrete;
using Entities.Dtos;
using Business.Handlers.Sponsorship.Commands;
using Business.Handlers.AnalysisMessages.Commands;
using Business.Handlers.SmartLinks.Commands;

namespace Tests.Helpers
{
    public static class SponsorshipDataHelper
    {
        public static SponsorProfile GetSponsorProfile(int sponsorId = 1)
        {
            return new SponsorProfile
            {
                Id = 1,
                SponsorId = sponsorId,
                CompanyName = "Green Agriculture Co.",
                CompanyDescription = "Leading agricultural solutions provider",
                SponsorLogoUrl = "https://example.com/logo.png",
                WebsiteUrl = "https://example.com",
                ContactEmail = "info@greenagri.com",
                ContactPhone = "+90 555 123 4567",
                ContactPerson = "John Smith",
                CompanyType = "Private",
                BusinessModel = "B2B",
                IsActive = true,
                IsVerifiedCompany = true,
                TotalPurchases = 5,
                TotalCodesGenerated = 150,
                TotalCodesRedeemed = 120,
                CreatedDate = DateTime.Now.AddMonths(-6),
                UpdatedDate = DateTime.Now.AddDays(-1)
            };
        }

        public static List<SponsorProfile> GetSponsorProfileList()
        {
            return new List<SponsorProfile>
            {
                GetSponsorProfile(1),
                GetSponsorProfile(2) with 
                { 
                    Id = 2, 
                    CompanyName = "AgriTech Solutions",
                    CompanyType = "Cooperative",
                    BusinessModel = "B2C"
                },
                GetSponsorProfile(3) with 
                { 
                    Id = 3, 
                    CompanyName = "Farm Innovations Ltd",
                    IsActive = false
                }
            };
        }

        public static SponsorshipPurchase GetSponsorshipPurchase(int sponsorId = 1, int subscriptionTierId = 3)
        {
            return new SponsorshipPurchase
            {
                Id = 1,
                SponsorId = sponsorId,
                SubscriptionTierId = subscriptionTierId,
                Quantity = 50,
                Amount = 2999.50m,
                Currency = "TRY",
                PaymentReference = "PAY_001_20241201",
                CodePrefix = "SPT001",
                ValidityDays = 365,
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-30),
                UpdatedDate = DateTime.Now.AddDays(-30)
            };
        }

        public static SponsorshipCode GetSponsorshipCode(string code = "SPT001-ABC123")
        {
            return new SponsorshipCode
            {
                Id = 1,
                Code = code,
                SponsorId = 1,
                SubscriptionTierId = 3,
                PurchaseId = 1,
                IsUsed = false,
                ExpiryDate = DateTime.Now.AddDays(365),
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-30)
            };
        }

        public static List<SponsorshipCode> GetSponsorshipCodeList()
        {
            return new List<SponsorshipCode>
            {
                GetSponsorshipCode("SPT001-ABC123"),
                GetSponsorshipCode("SPT001-DEF456") with 
                { 
                    Id = 2, 
                    IsUsed = true, 
                    UserId = 5,
                    UsedDate = DateTime.Now.AddDays(-15)
                },
                GetSponsorshipCode("SPT001-GHI789") with 
                { 
                    Id = 3,
                    ExpiryDate = DateTime.Now.AddDays(-1),
                    IsActive = false
                }
            };
        }

        public static SmartLink GetSmartLink(int sponsorId = 1)
        {
            return new SmartLink
            {
                Id = 1,
                SponsorId = sponsorId,
                LinkUrl = "https://example.com/product/fertilizer",
                LinkText = "Premium Organic Fertilizer",
                LinkDescription = "High-quality organic fertilizer for better crop yields",
                LinkType = "Product",
                Keywords = "[\"fertilizer\", \"organic\", \"premium\"]",
                ProductCategory = "Fertilizers",
                TargetCropTypes = "[\"tomato\", \"pepper\", \"cucumber\"]",
                ProductName = "OrganicMax Premium",
                ProductPrice = 299.99m,
                Currency = "TRY",
                Priority = 75,
                IsActive = true,
                ClickCount = 45,
                CreatedDate = DateTime.Now.AddDays(-20),
                UpdatedDate = DateTime.Now.AddDays(-5)
            };
        }

        public static AnalysisMessage GetAnalysisMessage()
        {
            return new AnalysisMessage
            {
                Id = 1,
                SponsorUserId = 2,
                FarmerId = 5,
                Subject = "Plant Analysis Follow-up",
                Message = "Your tomato plants show excellent growth patterns. Consider our premium fertilizer for even better results.",
                SentDate = DateTime.Now.AddHours(-2),
                IsRead = false,
                ReadDate = null
            };
        }

        // DTOs for API requests
        public static CreateSponsorProfileDto GetValidCreateSponsorProfileDto()
        {
            return new CreateSponsorProfileDto
            {
                CompanyName = "Test Agriculture Company",
                CompanyDescription = "Test agricultural solutions provider",
                SponsorLogoUrl = "https://test.com/logo.png",
                WebsiteUrl = "https://test.com",
                ContactEmail = "test@testcompany.com",
                ContactPhone = "+90 555 999 8888",
                ContactPerson = "Test Manager",
                CompanyType = "Private",
                BusinessModel = "B2B"
            };
        }

        public static PurchaseBulkSponsorshipCommand GetValidPurchaseCommand()
        {
            return new PurchaseBulkSponsorshipCommand
            {
                SponsorId = 1,
                SubscriptionTierId = 3, // L tier
                Quantity = 25,
                Amount = 1499.75m,
                Currency = "TRY",
                PaymentReference = "TEST_PAY_001",
                ValidityDays = 365
            };
        }

        public static RedeemSponsorshipCodeCommand GetValidRedemptionCommand()
        {
            return new RedeemSponsorshipCodeCommand
            {
                UserId = 5,
                Code = "SPT001-ABC123",
                UserEmail = "farmer@test.com",
                UserFullName = "Test Farmer"
            };
        }

        public static SendMessageCommand GetValidSendMessageCommand()
        {
            return new SendMessageCommand
            {
                FromUserId = 2, // Sponsor
                FarmerId = 5,
                Subject = "Test Message",
                Message = "This is a test message to the farmer."
            };
        }

        public static CreateSmartLinkCommand GetValidCreateSmartLinkCommand()
        {
            return new CreateSmartLinkCommand
            {
                SponsorId = 1,
                LinkUrl = "https://test.com/product",
                LinkText = "Test Product",
                LinkDescription = "Test product description",
                LinkType = "Product",
                Keywords = new List<string> { "test", "product" },
                ProductCategory = "Test Category",
                TargetCropTypes = new List<string> { "tomato" },
                ProductName = "Test Product Name",
                ProductPrice = 199.99m,
                Currency = "TRY",
                Priority = 50
            };
        }

        public static SendSponsorshipLinkCommand GetValidSendLinkCommand()
        {
            return new SendSponsorshipLinkCommand
            {
                SponsorId = 1,
                Recipients = new List<SponsorshipLinkRecipient>
                {
                    new SponsorshipLinkRecipient
                    {
                        Phone = "+90 555 123 4567",
                        Name = "Test Farmer",
                        Message = "Check out this great fertilizer!"
                    }
                },
                Channel = "SMS",
                SponsorshipCode = "SPT001-ABC123"
            };
        }
    }
}