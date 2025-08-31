using System;
using System.Collections.Generic;
using Entities.Concrete;
using Entities.Dtos;

namespace Tests.Helpers
{
    public static class SponsorshipDataHelper
    {
        public static SponsorProfile GetSponsorProfile()
        {
            return new SponsorProfile
            {
                Id = 1,
                SponsorId = 10,
                CompanyName = "AgroTech Solutions",
                ContactPerson = "John Smith",
                ContactEmail = "john.smith@agrotech.com",
                ContactPhone = "+905551234567",
                WebsiteUrl = "https://agrotech.com",
                CompanyType = "Agricultural Technology",
                BusinessModel = "B2B",
                IsActive = true,
                IsVerified = true,
                TotalPurchases = 5,
                TotalCodesGenerated = 500,
                TotalCodesRedeemed = 320,
                TotalInvestment = 25000m,
                CreatedDate = DateTime.Now.AddDays(-30),
                UpdatedDate = DateTime.Now
            };
        }

        public static SponsorProfile GetInactiveSponsorProfile()
        {
            var profile = GetSponsorProfile();
            profile.Id = 2;
            profile.SponsorId = 11;
            profile.IsActive = false;
            return profile;
        }

        public static SponsorshipPurchase GetSponsorshipPurchase()
        {
            return new SponsorshipPurchase
            {
                Id = 1,
                SponsorUserId = 10,
                Tier = "Gold",
                Quantity = 100,
                TotalCost = 5000m,
                PurchaseDate = DateTime.Now.AddDays(-5),
                ExpiryDate = DateTime.Now.AddDays(25),
                PaymentStatus = "Completed",
                PaymentMethod = "Credit Card",
                TransactionId = "txn_12345",
                Status = true,
                CreatedDate = DateTime.Now.AddDays(-5),
                UpdatedDate = DateTime.Now
            };
        }

        public static SponsorshipCode GetSponsorshipCode()
        {
            return new SponsorshipCode
            {
                Id = 1,
                Code = "GOLD-ABC123",
                Tier = "Gold",
                SponsorUserId = 10,
                ExpiryDate = DateTime.Now.AddDays(25),
                Status = true,
                CreatedDate = DateTime.Now.AddDays(-5),
                UpdatedDate = DateTime.Now
            };
        }

        public static List<SponsorshipCode> GetSponsorshipCodeList(int count = 3)
        {
            var list = new List<SponsorshipCode>();
            for (int i = 1; i <= count; i++)
            {
                var code = GetSponsorshipCode();
                code.Id = i;
                code.Code = $"GOLD-{i:D6}";
                list.Add(code);
            }
            return list;
        }
    }
}