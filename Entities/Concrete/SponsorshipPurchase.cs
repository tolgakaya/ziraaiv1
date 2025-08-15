using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class SponsorshipPurchase : IEntity
    {
        public int Id { get; set; }
        
        // Purchase Information
        public int SponsorId { get; set; } // Sponsor company user ID
        public int SubscriptionTierId { get; set; } // Which tier was purchased
        public int Quantity { get; set; } // Number of subscriptions purchased
        public decimal UnitPrice { get; set; } // Price per subscription
        public decimal TotalAmount { get; set; } // Total purchase amount
        public string Currency { get; set; } // TRY, USD, etc.
        
        // Payment Information
        public DateTime PurchaseDate { get; set; }
        public string PaymentMethod { get; set; } // CreditCard, BankTransfer, Invoice
        public string PaymentReference { get; set; } // Transaction ID or invoice number
        public string PaymentStatus { get; set; } // Pending, Completed, Failed, Refunded
        public DateTime? PaymentCompletedDate { get; set; }
        
        // Invoice Information
        public string InvoiceNumber { get; set; }
        public string InvoiceAddress { get; set; }
        public string TaxNumber { get; set; }
        public string CompanyName { get; set; }
        
        // Code Generation
        public int CodesGenerated { get; set; } // Number of codes actually generated
        public int CodesUsed { get; set; } // Number of codes redeemed by farmers
        public string CodePrefix { get; set; } // Custom prefix for codes (e.g., "AGRI", "FARM")
        public int ValidityDays { get; set; } // How many days codes are valid
        
        // Tier-Based Features
        public string SponsorTierFeatures { get; set; } // JSON object storing tier-specific features
        public string VisibilityLevel { get; set; } // ResultOnly, StartAndResult, AllScreens
        public string DataAccessLevel { get; set; } // Basic30, Medium60, Full100
        public bool HasMessaging { get; set; } // L and XL tiers
        public bool HasSmartLinking { get; set; } // XL tier only
        public int DataAccessPercentage { get; set; } // 30, 60, or 100
        public int MaxSmartLinks { get; set; } // Maximum smart links allowed
        public int MaxMessagesPerDay { get; set; } // Message quota
        
        // Status and Notes
        public string Status { get; set; } // Active, Completed, Cancelled
        public string Notes { get; set; } // Internal notes
        public string PurchaseReason { get; set; } // Why this purchase was made
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? ApprovedByUserId { get; set; } // Admin who approved the purchase
        public DateTime? ApprovalDate { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual User Sponsor { get; set; }
        
        [JsonIgnore]
        public virtual SubscriptionTier SubscriptionTier { get; set; }
        
        [JsonIgnore]
        public virtual User ApprovedByUser { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<SponsorshipCode> SponsorshipCodes { get; set; }
        
        public SponsorshipPurchase()
        {
            SponsorshipCodes = new HashSet<SponsorshipCode>();
        }
    }
}