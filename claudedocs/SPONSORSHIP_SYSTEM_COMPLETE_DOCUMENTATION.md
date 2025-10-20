# ZiraAI Sponsorship System - Complete Documentation

**Version:** 1.0.0
**Last Updated:** 2025-10-07
**Status:** ✅ Core System 100% Implemented | 🚧 Extensions Planned

---

## 📑 Table of Contents

1. [Executive Summary](#executive-summary)
2. [System Overview](#system-overview)
3. [Architecture](#architecture)
4. [Database Schema](#database-schema)
5. [Tier-Based Features](#tier-based-features)
6. [Business Workflows](#business-workflows)
7. [API Reference](#api-reference)
8. [Configuration](#configuration)
9. [Implementation Guide](#implementation-guide)
10. [Smart Links System](#smart-links-system)
11. [Analytics & Reporting](#analytics--reporting)
12. [Planned Features](#planned-features)
13. [Related Documentation](#related-documentation)

---

## 📊 Executive Summary

The ZiraAI Sponsorship System enables agricultural companies (sponsors) to purchase subscription packages in bulk and distribute them to farmers through unique redemption codes. The system supports four tier levels (S, M, L, XL) with progressive feature unlocking, providing sponsors with varying levels of farmer data access, branding visibility, and communication capabilities.

### Key Capabilities
- **Bulk Purchase**: Sponsors buy subscription packages (S/M/L/XL) for distribution
- **Code Generation**: Automatic unique code generation (`AGRI-2025-X3K9` format)
- **Multi-Channel Distribution**: SMS and WhatsApp link delivery with tracking
- **Deferred Deep Linking**: Mobile app auto-fills codes from SMS/WhatsApp messages
- **Tier-Based Access**: Progressive data access (30%→60%→100%) and feature unlocking
- **Smart Links**: AI-powered contextual product recommendations (XL tier exclusive)
- **Logo Visibility**: Screen-specific branding based on tier level
- **Analytics**: Comprehensive tracking of code usage, ROI, and farmer engagement

### Implementation Status
| Component | Status | Completeness |
|-----------|--------|--------------|
| Core Entities & Database | ✅ Implemented | 100% |
| Purchase & Code Generation | ✅ Implemented | 100% |
| Distribution System (SMS/WhatsApp) | ✅ Implemented | 100% |
| Redemption Flow | ✅ Implemented | 100% |
| Tier-Based Features | ✅ Implemented | 100% |
| Data Access Filtering | ✅ Implemented | 100% |
| Logo Visibility Rules | ✅ Implemented | 100% |
| Smart Links (XL) | ✅ Implemented | 100% |
| Messaging System (M/L/XL) | ✅ Implemented | 100% |
| Analytics & Statistics | ✅ Implemented | 90% |
| WhatsApp Sponsor Request | 📋 Designed | 0% |
| Referral Integration | 📋 Designed | 0% |

---

## 🏗️ System Overview

### Business Model
The sponsorship system operates on a **purchase-based model**:

```
Sponsor Workflow:
1. Sponsor purchases bulk subscription packages (e.g., 100x M-tier codes)
2. System generates unique redemption codes
3. Sponsor distributes codes to farmers via SMS/WhatsApp
4. Farmers redeem codes for free subscriptions
5. Sponsor gains data access and branding based on tier level

Farmer Workflow:
1. Receives sponsorship code via SMS/WhatsApp
2. Mobile app auto-fills code (deferred deep linking)
3. Redeems code for free 30-day subscription
4. Uses ZiraAI plant analysis services
5. Sponsor logo appears on analysis results
```

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Sponsorship System                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │   Purchase   │───▶│     Code     │───▶│ Distribution │ │
│  │   Service    │    │  Generation  │    │   Service    │ │
│  └──────────────┘    └──────────────┘    └──────────────┘ │
│         │                    │                    │          │
│         ▼                    ▼                    ▼          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │ Sponsorship  │    │ Sponsorship  │    │ Notification │ │
│  │  Purchase    │    │     Code     │    │   Service    │ │
│  │   Entity     │    │    Entity    │    │ (SMS/WA API) │ │
│  └──────────────┘    └──────────────┘    └──────────────┘ │
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │  Redemption  │───▶│   Tier-Based │───▶│   Analytics  │ │
│  │   Service    │    │   Features   │    │   Service    │ │
│  └──────────────┘    └──────────────┘    └──────────────┘ │
│         │                    │                    │          │
│         ▼                    ▼                    ▼          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │     User     │    │   Sponsor    │    │   Sponsor    │ │
│  │ Subscription │    │  Visibility  │    │   Analysis   │ │
│  │    Entity    │    │   Service    │    │    Access    │ │
│  └──────────────┘    └──────────────┘    └──────────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Database Schema

### Core Entities

#### 1. SponsorshipPurchase
Records bulk subscription package purchases by sponsors.

```csharp
public class SponsorshipPurchase : IEntity
{
    public int Id { get; set; }

    // Sponsor Information
    public int SponsorId { get; set; }                    // FK: Users.UserId
    public string CompanyName { get; set; }                // Cached company name

    // Package Details
    public int SubscriptionTierId { get; set; }            // FK: SubscriptionTiers.Id (S/M/L/XL)
    public int Quantity { get; set; }                      // Number of codes purchased
    public decimal UnitPrice { get; set; }                 // Price per code
    public decimal TotalAmount { get; set; }               // Total purchase amount
    public string Currency { get; set; }                   // TRY, USD, EUR

    // Payment Information
    public DateTime PurchaseDate { get; set; }
    public string PaymentMethod { get; set; }              // CreditCard, BankTransfer, Invoice
    public string PaymentReference { get; set; }           // External payment ID
    public string PaymentStatus { get; set; }              // Pending, Completed, Failed, Refunded
    public DateTime? PaymentCompletedDate { get; set; }

    // Code Generation Settings
    public string CodePrefix { get; set; }                 // Default: "AGRI"
    public int ValidityDays { get; set; }                  // Default: 365
    public int CodesGenerated { get; set; }                // Actual codes created
    public int CodesUsed { get; set; }                     // Codes redeemed by farmers

    // Status
    public string Status { get; set; }                     // Active, Expired, Cancelled
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Navigation Properties
    public virtual User Sponsor { get; set; }
    public virtual SubscriptionTier SubscriptionTier { get; set; }
    public virtual ICollection<SponsorshipCode> Codes { get; set; }
}
```

**Indexes:**
```sql
CREATE INDEX IX_SponsorshipPurchase_SponsorId ON SponsorshipPurchases(SponsorId);
CREATE INDEX IX_SponsorshipPurchase_Status ON SponsorshipPurchases(Status);
CREATE INDEX IX_SponsorshipPurchase_PurchaseDate ON SponsorshipPurchases(PurchaseDate DESC);
```

---

#### 2. SponsorshipCode
Individual redemption codes distributed to farmers.

```csharp
public class SponsorshipCode : IEntity
{
    public int Id { get; set; }

    // Code Information
    public string Code { get; set; }                       // Unique: "AGRI-2025-X3K9"
    public int SponsorId { get; set; }                     // FK: Users.UserId
    public int SubscriptionTierId { get; set; }            // FK: SubscriptionTiers.Id
    public int SponsorshipPurchaseId { get; set; }         // FK: SponsorshipPurchases.Id

    // Validity
    public DateTime CreatedDate { get; set; }
    public DateTime ExpiryDate { get; set; }               // Default: +365 days
    public bool IsActive { get; set; }                     // Can be deactivated manually

    // Usage Tracking
    public bool IsUsed { get; set; }                       // false → available, true → redeemed
    public int? UsedByUserId { get; set; }                 // FK: Users.UserId (farmer)
    public DateTime? UsedDate { get; set; }
    public int? CreatedSubscriptionId { get; set; }        // FK: UserSubscriptions.Id

    // Distribution Tracking
    public string RedemptionLink { get; set; }             // https://ziraai.com/redeem/{Code}
    public string RecipientPhone { get; set; }             // +905551234567
    public string RecipientName { get; set; }              // Farmer name (optional)
    public DateTime? LinkSentDate { get; set; }
    public string LinkSentVia { get; set; }                // SMS, WhatsApp, Email
    public bool LinkDelivered { get; set; }                // Delivery confirmation
    public string DistributionChannel { get; set; }        // Final distribution method
    public DateTime? DistributionDate { get; set; }
    public string DistributedTo { get; set; }              // "{Name} ({Phone})"

    // Link Analytics
    public int LinkClickCount { get; set; }                // Number of redemption link clicks
    public DateTime? LinkClickDate { get; set; }           // First click timestamp
    public string LastClickIpAddress { get; set; }         // Last click IP for fraud detection

    // Notes
    public string Notes { get; set; }                      // Admin/sponsor notes

    // Navigation Properties
    public virtual User Sponsor { get; set; }
    public virtual User UsedByUser { get; set; }
    public virtual SubscriptionTier SubscriptionTier { get; set; }
    public virtual SponsorshipPurchase SponsorshipPurchase { get; set; }
    public virtual UserSubscription CreatedSubscription { get; set; }
}
```

**Indexes:**
```sql
CREATE UNIQUE INDEX IX_SponsorshipCode_Code ON SponsorshipCodes(Code);
CREATE INDEX IX_SponsorshipCode_SponsorId ON SponsorshipCodes(SponsorId);
CREATE INDEX IX_SponsorshipCode_IsUsed_IsActive ON SponsorshipCodes(IsUsed, IsActive);
CREATE INDEX IX_SponsorshipCode_RecipientPhone ON SponsorshipCodes(RecipientPhone);
CREATE INDEX IX_SponsorshipCode_ExpiryDate ON SponsorshipCodes(ExpiryDate) WHERE IsUsed = false;
```

---

#### 3. SponsorProfile
Sponsor company profile and statistics.

```csharp
public class SponsorProfile : IEntity
{
    public int Id { get; set; }

    // Sponsor Information
    public int SponsorId { get; set; }                     // FK: Users.UserId
    public string CompanyName { get; set; }
    public string CompanyDescription { get; set; }
    public string SponsorLogoUrl { get; set; }             // Logo for display on farmer screens
    public string WebsiteUrl { get; set; }

    // Contact Information
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }
    public string ContactPerson { get; set; }

    // Company Details
    public string CompanyType { get; set; }                // Manufacturer, Distributor, Retailer
    public string BusinessModel { get; set; }              // B2B, B2C, B2B2C

    // Statistics (Cached)
    public int TotalPurchases { get; set; }                // Number of bulk purchases
    public int TotalCodesGenerated { get; set; }           // Total codes created
    public int TotalCodesRedeemed { get; set; }            // Total codes used by farmers
    public decimal TotalInvestment { get; set; }           // Total amount spent (TRY)
    public DateTime? LastPurchaseDate { get; set; }

    // Verification & Status
    public bool IsVerified { get; set; }                   // Admin verified company
    public DateTime? VerificationDate { get; set; }
    public bool IsActive { get; set; }                     // Can create purchases

    // Audit
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Navigation Properties
    public virtual User Sponsor { get; set; }
    public virtual ICollection<SponsorshipPurchase> SponsorshipPurchases { get; set; }
}
```

**Indexes:**
```sql
CREATE UNIQUE INDEX IX_SponsorProfile_SponsorId ON SponsorProfiles(SponsorId);
CREATE INDEX IX_SponsorProfile_IsActive ON SponsorProfiles(IsActive);
```

---

#### 4. SponsorAnalysisAccess
Tracks sponsor access to farmer plant analysis data.

```csharp
public class SponsorAnalysisAccess : IEntity
{
    public int Id { get; set; }

    // Access Information
    public int SponsorId { get; set; }                     // FK: Users.UserId
    public int PlantAnalysisId { get; set; }               // FK: PlantAnalyses.Id
    public int FarmerId { get; set; }                      // FK: Users.UserId

    // Access Level (Based on Tier)
    public string AccessLevel { get; set; }                // Basic30, Extended60, Full100
    public int AccessPercentage { get; set; }              // 30, 60, or 100

    // Access Tracking
    public DateTime FirstViewedDate { get; set; }
    public DateTime LastViewedDate { get; set; }
    public int ViewCount { get; set; }

    // Field Access Control
    public string AccessedFields { get; set; }             // JSON array of accessible field names
    public string RestrictedFields { get; set; }           // JSON array of restricted field names

    // Granular Permissions (Boolean flags for quick checks)
    public bool CanViewHealthScore { get; set; }           // 30%+
    public bool CanViewDiseases { get; set; }              // 60%+
    public bool CanViewPests { get; set; }                 // 60%+
    public bool CanViewNutrients { get; set; }             // 60%+
    public bool CanViewRecommendations { get; set; }       // 60%+
    public bool CanViewFarmerContact { get; set; }         // 100% only
    public bool CanViewLocation { get; set; }              // 60%+
    public bool CanViewImages { get; set; }                // 30%+

    // Audit
    public DateTime CreatedDate { get; set; }

    // Navigation Properties
    public virtual User Sponsor { get; set; }
    public virtual PlantAnalysis PlantAnalysis { get; set; }
    public virtual User Farmer { get; set; }
}
```

**Indexes:**
```sql
CREATE INDEX IX_SponsorAnalysisAccess_SponsorId_PlantAnalysisId ON SponsorAnalysisAccess(SponsorId, PlantAnalysisId);
CREATE INDEX IX_SponsorAnalysisAccess_FarmerId ON SponsorAnalysisAccess(FarmerId);
```

---

#### 5. SmartLink (XL Tier Exclusive)
AI-powered contextual product links shown to farmers.

```csharp
public class SmartLink : IEntity
{
    public int Id { get; set; }

    // Owner
    public int SponsorId { get; set; }                     // FK: Users.UserId
    public string SponsorName { get; set; }                // Cached for display

    // Link Information
    public string LinkUrl { get; set; }                    // Target URL (product page, contact form)
    public string LinkText { get; set; }                   // Display text (e.g., "Buy Fungicide X")
    public string LinkDescription { get; set; }            // Detailed description
    public string LinkType { get; set; }                   // Product, Service, Information, Contact

    // Targeting & Matching (AI-Powered)
    public string Keywords { get; set; }                   // JSON: ["fungicide", "organic", "preventive"]
    public string ProductCategory { get; set; }            // Fertilizer, Pesticide, Equipment, Service
    public string TargetCropTypes { get; set; }            // JSON: ["Domates", "Biber", "Patlıcan"]
    public string TargetDiseases { get; set; }             // JSON: ["Mildew", "Blight"]
    public string TargetPests { get; set; }                // JSON: ["Aphid", "Whitefly"]
    public string TargetNutrientDeficiencies { get; set; } // JSON: ["Nitrogen", "Potassium"]
    public string TargetGrowthStages { get; set; }         // JSON: ["Flowering", "Fruiting"]
    public string TargetRegions { get; set; }              // JSON: ["Antalya", "İzmir"]

    // Display Settings
    public int Priority { get; set; }                      // 1-100 (higher = more visible)
    public string DisplayPosition { get; set; }            // Top, Bottom, Inline, Sidebar
    public string DisplayStyle { get; set; }               // Button, Text, Card, Banner
    public string IconUrl { get; set; }
    public string BackgroundColor { get; set; }            // Hex color
    public string TextColor { get; set; }
    public bool IsBold { get; set; }
    public bool IsHighlighted { get; set; }

    // Product Information
    public string ProductName { get; set; }
    public string ProductImageUrl { get; set; }
    public decimal? ProductPrice { get; set; }
    public string ProductCurrency { get; set; }            // TRY, USD, EUR
    public string ProductUnit { get; set; }                // kg, L, piece
    public bool IsPromotional { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public DateTime? PromotionStartDate { get; set; }
    public DateTime? PromotionEndDate { get; set; }

    // Analytics
    public int ClickCount { get; set; }
    public int UniqueClickCount { get; set; }
    public DateTime? LastClickDate { get; set; }
    public int DisplayCount { get; set; }
    public decimal ClickThroughRate { get; set; }          // CTR %
    public int ConversionCount { get; set; }               // Purchases/actions
    public decimal ConversionRate { get; set; }            // Conversion %
    public string ClickHistory { get; set; }               // JSON: click events

    // A/B Testing
    public string TestVariant { get; set; }                // A, B, C
    public int TestGroupSize { get; set; }
    public decimal TestPerformanceScore { get; set; }

    // Scheduling
    public bool IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string ActiveDays { get; set; }                 // JSON: ["Monday", "Tuesday"]
    public string ActiveHours { get; set; }                // JSON: ["9-17", "18-22"]
    public int? MaxDisplayCount { get; set; }              // Per user limit
    public int? MaxClickCount { get; set; }                // Total limit

    // Budget & Billing
    public decimal? CostPerClick { get; set; }             // CPC
    public decimal? TotalBudget { get; set; }
    public decimal? SpentBudget { get; set; }
    public string BillingType { get; set; }                // CPC, CPM, Fixed

    // Compliance
    public bool IsApproved { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string ApprovalNotes { get; set; }
    public bool IsCompliant { get; set; }
    public string ComplianceNotes { get; set; }

    // AI Integration
    public decimal? RelevanceScore { get; set; }           // AI-calculated
    public string AiRecommendations { get; set; }          // JSON: AI suggestions
    public DateTime? LastAiAnalysis { get; set; }
    public bool UseAiOptimization { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }

    // Navigation Properties
    public virtual User Sponsor { get; set; }
}
```

**Indexes:**
```sql
CREATE INDEX IX_SmartLink_SponsorId ON SmartLinks(SponsorId);
CREATE INDEX IX_SmartLink_IsActive_IsApproved ON SmartLinks(IsActive, IsApproved);
CREATE INDEX IX_SmartLink_Priority ON SmartLinks(Priority DESC);
CREATE INDEX IX_SmartLink_ProductCategory ON SmartLinks(ProductCategory);
```

---

#### 6. SubscriptionTier
Defines the four sponsorship tier levels.

```csharp
public class SubscriptionTier : IEntity
{
    public int Id { get; set; }

    // Tier Information
    public string TierName { get; set; }                   // S, M, L, XL
    public string DisplayName { get; set; }                // Small, Medium, Large, Extra Large
    public string Description { get; set; }

    // Request Limits (for farmers using sponsored subscriptions)
    public int DailyRequestLimit { get; set; }
    public int MonthlyRequestLimit { get; set; }

    // Pricing (for sponsors purchasing bulk codes)
    public decimal MonthlyPrice { get; set; }              // Price per code
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; }

    // Features
    public bool PrioritySupport { get; set; }
    public bool AdvancedAnalytics { get; set; }
    public bool ApiAccess { get; set; }
    public int ResponseTimeHours { get; set; }
    public string AdditionalFeatures { get; set; }         // JSON array

    // Status
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
```

---

## 🎯 Tier-Based Features

### Feature Matrix

| Feature | S Tier | M Tier | L Tier | XL Tier |
|---------|--------|--------|--------|---------|
| **Logo Visibility** |
| Result Screen | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Start Screen | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| Analysis Screen | ❌ No | ❌ No | ✅ Yes | ✅ Yes |
| Profile Screen | ❌ No | ❌ No | ✅ Yes | ✅ Yes |
| **Data Access** | 30% | 30% | 60% | 100% |
| Overall Health Score | ✅ | ✅ | ✅ | ✅ |
| Plant Species/Variety | ✅ | ✅ | ✅ | ✅ |
| Growth Stage | ✅ | ✅ | ✅ | ✅ |
| Plant Images | ✅ | ✅ | ✅ | ✅ |
| Disease Symptoms | ❌ | ❌ | ✅ | ✅ |
| Pest Information | ❌ | ❌ | ✅ | ✅ |
| Nutrient Status | ❌ | ❌ | ✅ | ✅ |
| Recommendations | ❌ | ❌ | ✅ | ✅ |
| Location Data | ❌ | ❌ | ✅ | ✅ |
| Weather Conditions | ❌ | ❌ | ✅ | ✅ |
| Farmer Contact Info | ❌ | ❌ | ❌ | ✅ |
| Field Details | ❌ | ❌ | ❌ | ✅ |
| Planting/Harvest Dates | ❌ | ❌ | ❌ | ✅ |
| **Communication** |
| Messaging Farmers | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| Conversation History | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| **Advanced Features** |
| Smart Links | ❌ No | ❌ No | ❌ No | ✅ Yes |
| AI Optimization | ❌ No | ❌ No | ❌ No | ✅ Yes |
| A/B Testing | ❌ No | ❌ No | ❌ No | ✅ Yes |
| Custom Analytics | ❌ No | ❌ No | ❌ No | ✅ Yes |

### Data Access Filtering Logic

**Implementation:** `SponsorDataAccessService.cs`

#### 30% Access (S & M Tiers)
```csharp
// Visible Fields
✅ OverallHealthScore
✅ PlantSpecies
✅ PlantVariety
✅ GrowthStage
✅ ImagePath
✅ CropType
✅ AnalysisDate

// Hidden Fields
❌ Disease/pest details
❌ Recommendations
❌ Location data
❌ Farmer contact
❌ All advanced insights
```

#### 60% Access (L Tier)
```csharp
// Additional Visible Fields
✅ VigorScore
✅ HealthSeverity
✅ StressIndicators
✅ DiseaseSymptoms
✅ PrimaryDeficiency
✅ NutrientStatus
✅ PrimaryConcern
✅ Prognosis
✅ Recommendations
✅ Location
✅ Latitude/Longitude
✅ WeatherConditions
✅ Temperature/Humidity
✅ SoilType
✅ Diseases (legacy)
✅ Pests (legacy)
✅ ElementDeficiencies (legacy)

// Still Hidden
❌ Farmer contact info
❌ Field ID
❌ Planting dates
❌ Detailed cross-factor insights
```

#### 100% Access (XL Tier)
```csharp
// All Fields Visible
✅ Everything from 30% and 60% access
✅ ContactPhone
✅ ContactEmail
✅ AdditionalInfo
✅ FieldId
✅ PlantingDate
✅ ExpectedHarvestDate
✅ LastFertilization
✅ LastIrrigation
✅ PreviousTreatments
✅ UrgencyLevel
✅ Notes
✅ DetailedAnalysisData
✅ CrossFactorInsights
✅ EstimatedYieldImpact
✅ ConfidenceLevel
✅ IdentificationConfidence
```

### Logo Visibility Rules

**Implementation:** `SponsorVisibilityService.cs`

```csharp
// Result Screen: All tiers can display logo
public async Task<bool> CanShowLogoOnResultScreenAsync(int sponsorId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
    return profile != null && profile.IsActive;
}

// Start Screen: M, L, XL only
public async Task<bool> CanShowLogoOnStartScreenAsync(int sponsorId)
{
    var purchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(sponsorId);

    foreach (var purchase in purchases)
    {
        var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
        if (tier != null && (tier.TierName == "M" || tier.TierName == "L" || tier.TierName == "XL"))
        {
            return true;
        }
    }

    return false;
}

// All Screens: L, XL only
public async Task<bool> CanShowLogoOnAllScreensAsync(int sponsorId)
{
    var purchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(sponsorId);

    foreach (var purchase in purchases)
    {
        var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
        if (tier != null && (tier.TierName == "L" || tier.TierName == "XL"))
        {
            return true;
        }
    }

    return false;
}

// Screen-Specific Check
public async Task<bool> CanShowLogoOnScreenAsync(int plantAnalysisId, string screenType)
{
    var tierName = await GetTierNameFromAnalysisAsync(plantAnalysisId);

    return screenType.ToLower() switch
    {
        "result" => true,                                      // All tiers
        "start" => tierName == "M" || tierName == "L" || tierName == "XL",
        "analysis" => tierName == "L" || tierName == "XL",
        "profile" => tierName == "L" || tierName == "XL",
        _ => false
    };
}
```

---

## 🔄 Business Workflows

### 1. Purchase Workflow

```
┌──────────────┐
│   Sponsor    │
│  Dashboard   │
└──────┬───────┘
       │
       │ POST /api/sponsorship/purchase-package
       │ {
       │   "subscriptionTierId": 2,      // M tier
       │   "quantity": 100,
       │   "totalAmount": 5000.00,
       │   "paymentReference": "PAY-12345"
       │ }
       ▼
┌──────────────────────────────────────────┐
│  PurchaseBulkSponsorshipCommand Handler  │
├──────────────────────────────────────────┤
│ 1. Validate sponsor                      │
│ 2. Get subscription tier details         │
│ 3. Create SponsorshipPurchase record     │
│    - Status: "Completed"                 │
│    - PaymentMethod: "CreditCard"         │
│    - CodePrefix: "AGRI"                  │
│    - ValidityDays: 365                   │
│ 4. Generate codes (quantity: 100)        │
└──────┬───────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  ISponsorshipCodeRepository     │
│  .GenerateCodesAsync()          │
├─────────────────────────────────┤
│ FOR i = 1 TO quantity:          │
│   1. Generate unique code       │
│      Format: {PREFIX}-{YEAR}-{RANDOM} │
│      Example: AGRI-2025-X3K9    │
│   2. Set expiry date (+365d)    │
│   3. Set sponsorId, tierId      │
│   4. Link to purchaseId         │
│   5. Save to database           │
│ ENDFOR                          │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  Update Purchase Record         │
│  - CodesGenerated: 100          │
│  - CodesUsed: 0                 │
└──────┬──────────────────────────┘
       │
       ▼
┌──────────────────────────────────┐
│  Response: SponsorshipPurchase   │
│  + List<SponsorshipCodeDto>      │
│    [                              │
│      { Code: "AGRI-2025-X3K9" },│
│      { Code: "AGRI-2025-P7M4" },│
│      ...                          │
│    ]                              │
└───────────────────────────────────┘
```

**Key Code:** `SponsorshipService.cs:PurchaseBulkSubscriptionsAsync()`

---

### 2. Code Distribution Workflow

```
┌──────────────┐
│   Sponsor    │
│  Dashboard   │
└──────┬───────┘
       │
       │ POST /api/sponsorship/send-link
       │ {
       │   "recipients": [
       │     { "code": "AGRI-2025-X3K9", "phone": "+905551234567", "name": "Ahmet Yılmaz" },
       │     { "code": "AGRI-2025-P7M4", "phone": "+905559876543", "name": "Mehmet Kaya" }
       │   ],
       │   "channel": "WhatsApp",
       │   "customMessage": "Optional custom text"
       │ }
       ▼
┌──────────────────────────────────────────┐
│  SendSponsorshipLinkCommand Handler      │
├──────────────────────────────────────────┤
│ 1. Validate codes                        │
│    - Check sponsor ownership             │
│    - Verify IsUsed = false               │
│    - Check ExpiryDate > now              │
│ 2. Generate redemption links             │
│    Format: https://ziraai.com/redeem/{CODE}
└──────┬───────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  INotificationService                   │
│  .SendBulkTemplateNotificationsAsync()  │
├─────────────────────────────────────────┤
│ Template: "sponsorship_invitation"      │
│                                          │
│ SMS Message:                             │
│ 🌱 ZiraAI'ya davet edildiniz!          │
│                                          │
│ Referans Kodunuz: AGRI-2025-X3K9       │
│                                          │
│ Uygulamayı indirin:                     │
│ https://play.google.com/store/apps...   │
│                                          │
│ Uygulama açıldığında kod otomatik       │
│ gelecek!                                 │
│                                          │
│ WhatsApp Message:                        │
│ 🌱 *ZiraAI'ya davet edildiniz!*        │
│                                          │
│ *Referans Kodunuz:* AGRI-2025-X3K9     │
│                                          │
│ Uygulamayı indirin:                     │
│ https://play.google.com/store/apps...   │
│                                          │
│ _Uygulama açıldığında kod otomatik      │
│ gelecek!_                                │
└──────┬──────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────┐
│  Update SponsorshipCode Records         │
│  FOR each successful send:              │
│    - RedemptionLink: generated URL      │
│    - RecipientPhone: farmer phone       │
│    - RecipientName: farmer name         │
│    - LinkSentDate: NOW                  │
│    - LinkSentVia: "WhatsApp"            │
│    - LinkDelivered: true                │
│    - DistributionChannel: "WhatsApp"    │
│    - DistributedTo: "{Name} ({Phone})"  │
│  ENDFOR                                  │
└──────┬──────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────┐
│  Response: BulkSendResult            │
│  {                                    │
│    totalSent: 2,                      │
│    successCount: 2,                   │
│    failureCount: 0,                   │
│    results: [                         │
│      { code: "AGRI-2025-X3K9",       │
│        phone: "+905551234567",       │
│        success: true,                 │
│        deliveryStatus: "Sent" },     │
│      { code: "AGRI-2025-P7M4",       │
│        phone: "+905559876543",       │
│        success: true,                 │
│        deliveryStatus: "Sent" }      │
│    ]                                  │
│  }                                    │
└───────────────────────────────────────┘
```

**Key Code:** `SendSponsorshipLinkCommand.cs`

---

### 3. Redemption Workflow (Farmer Side)

```
┌───────────────────┐
│     Farmer        │
│ Receives SMS/WA   │
│ with code         │
└─────────┬─────────┘
          │
          │ Click redemption link
          │ https://ziraai.com/redeem/AGRI-2025-X3K9
          │
          ▼
┌─────────────────────────────────────────┐
│  Mobile App Deep Link Handler           │
├─────────────────────────────────────────┤
│ 1. Extract code from URL                │
│ 2. Launch ZiraAI mobile app             │
│ 3. Auto-fill code in redemption screen  │
│    (Deferred Deep Linking)              │
└─────────┬───────────────────────────────┘
          │
          │ OR: Manual code entry
          │
          ▼
┌─────────────────────────────────────────┐
│  Track Link Click                       │
│  RedemptionService.TrackLinkClickAsync()│
├─────────────────────────────────────────┤
│  - LinkClickCount++                     │
│  - LinkClickDate = NOW                  │
│  - LastClickIpAddress = client IP       │
└─────────┬───────────────────────────────┘
          │
          │ POST /api/sponsorship/redeem
          │ { "code": "AGRI-2025-X3K9" }
          │
          ▼
┌─────────────────────────────────────────┐
│  RedeemSponsorshipCodeCommand Handler   │
├─────────────────────────────────────────┤
│ 1. Validate code                        │
│    - Code exists                        │
│    - IsUsed = false                     │
│    - IsActive = true                    │
│    - ExpiryDate > NOW                   │
│    - Not sponsor's own code             │
│                                          │
│ 2. Check existing subscription          │
│    - Allow upgrade from Trial           │
│    - Block if active paid subscription  │
│                                          │
│ 3. Deactivate trial if exists           │
│    - Set IsActive = false               │
│    - Set Status = "Upgraded"            │
└─────────┬───────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────┐
│  Create User Subscription               │
├─────────────────────────────────────────┤
│  UserSubscription:                      │
│    - UserId: farmer ID                  │
│    - SubscriptionTierId: from code      │
│    - StartDate: NOW                     │
│    - EndDate: NOW + 30 days             │
│    - IsActive: true                     │
│    - AutoRenew: false                   │
│    - PaymentMethod: "Sponsorship"       │
│    - PaymentReference: code             │
│    - PaidAmount: 0 (sponsored)          │
│    - IsSponsoredSubscription: true      │
│    - SponsorshipCodeId: code ID         │
│    - SponsorId: sponsor user ID         │
└─────────┬───────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────┐
│  Mark Code as Used                      │
│  ISponsorshipCodeRepository             │
│  .MarkAsUsedAsync()                     │
├─────────────────────────────────────────┤
│  - IsUsed = true                        │
│  - UsedByUserId = farmer ID             │
│  - UsedDate = NOW                       │
│  - CreatedSubscriptionId = subscription │
└─────────┬───────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────┐
│  Update Statistics                      │
│  - Purchase.CodesUsed++                 │
│  - SponsorProfile.TotalCodesRedeemed++  │
└─────────┬───────────────────────────────┘
          │
          ▼
┌──────────────────────────────────────────┐
│  Response: Success + Subscription Info   │
│  {                                        │
│    success: true,                         │
│    message: "M aboneliğiniz başarıyla   │
│              aktive edildi!",            │
│    data: {                                │
│      subscriptionId: 123,                │
│      tierName: "M",                      │
│      startDate: "2025-10-07",            │
│      endDate: "2025-11-06",              │
│      sponsorName: "Tarım A.Ş."          │
│    }                                      │
│  }                                        │
└───────────────────────────────────────────┘
```

**Key Code:** `RedeemSponsorshipCodeCommand.cs`, `SponsorshipService.cs:RedeemSponsorshipCodeAsync()`

---

## 📡 API Reference

### Sponsor Endpoints

#### 1. Create Sponsor Profile
```http
POST /api/v1/sponsorship/create-profile
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Request Body:**
```json
{
  "companyName": "Tarım Teknolojileri A.Ş.",
  "companyDescription": "Modern tarım çözümleri üreticisi",
  "sponsorLogoUrl": "https://cdn.ziraai.com/logos/tarim-as.png",
  "websiteUrl": "https://tarim-as.com",
  "contactEmail": "info@tarim-as.com",
  "contactPhone": "+902121234567",
  "contactPerson": "Ahmet Yılmaz",
  "companyType": "Manufacturer",
  "businessModel": "B2B2C"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Sponsor profili başarıyla oluşturuldu",
  "data": {
    "id": 1,
    "sponsorId": 42,
    "companyName": "Tarım Teknolojileri A.Ş.",
    "isActive": true,
    "isVerified": false,
    "createdDate": "2025-10-07T10:30:00Z"
  }
}
```

---

#### 2. Purchase Subscription Package
```http
POST /api/v1/sponsorship/purchase-package
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Request Body:**
```json
{
  "subscriptionTierId": 2,
  "quantity": 100,
  "totalAmount": 5000.00,
  "paymentReference": "STRIPE-PAY-12345",
  "codePrefix": "AGRI",
  "validityDays": 365
}
```

**Response:**
```json
{
  "success": true,
  "message": "100 sponsorship codes generated successfully",
  "data": {
    "id": 15,
    "sponsorId": 42,
    "subscriptionTierId": 2,
    "quantity": 100,
    "unitPrice": 50.00,
    "totalAmount": 5000.00,
    "currency": "TRY",
    "purchaseDate": "2025-10-07T10:35:00Z",
    "paymentStatus": "Completed",
    "codesGenerated": 100,
    "codesUsed": 0,
    "codePrefix": "AGRI",
    "validityDays": 365,
    "generatedCodes": [
      {
        "id": 1501,
        "code": "AGRI-2025-X3K9",
        "tierName": "M",
        "isUsed": false,
        "isActive": true,
        "expiryDate": "2026-10-07T10:35:00Z"
      },
      {
        "id": 1502,
        "code": "AGRI-2025-P7M4",
        "tierName": "M",
        "isUsed": false,
        "isActive": true,
        "expiryDate": "2026-10-07T10:35:00Z"
      }
      // ... 98 more codes
    ]
  }
}
```

---

#### 3. Send Sponsorship Links
```http
POST /api/v1/sponsorship/send-link
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Request Body:**
```json
{
  "recipients": [
    {
      "code": "AGRI-2025-X3K9",
      "phone": "+905551234567",
      "name": "Ahmet Yılmaz"
    },
    {
      "code": "AGRI-2025-P7M4",
      "phone": "+905559876543",
      "name": "Mehmet Kaya"
    }
  ],
  "channel": "WhatsApp",
  "customMessage": null
}
```

**Response:**
```json
{
  "success": true,
  "message": "📱 2 link başarıyla gönderildi via WhatsApp",
  "data": {
    "totalSent": 2,
    "successCount": 2,
    "failureCount": 0,
    "results": [
      {
        "code": "AGRI-2025-X3K9",
        "phone": "+905551234567",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      },
      {
        "code": "AGRI-2025-P7M4",
        "phone": "+905559876543",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

---

#### 4. Get Sponsorship Codes
```http
GET /api/v1/sponsorship/codes?onlyUnused=true
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1501,
      "code": "AGRI-2025-X3K9",
      "subscriptionTierId": 2,
      "tierName": "M",
      "isUsed": false,
      "isActive": true,
      "expiryDate": "2026-10-07T10:35:00Z",
      "recipientPhone": "+905551234567",
      "recipientName": "Ahmet Yılmaz",
      "linkSentDate": "2025-10-07T11:00:00Z",
      "linkSentVia": "WhatsApp",
      "linkClickCount": 3
    }
    // ... more codes
  ]
}
```

---

#### 5. Get Purchase History
```http
GET /api/v1/sponsorship/purchases
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 15,
      "sponsorId": 42,
      "subscriptionTierId": 2,
      "tierName": "M",
      "quantity": 100,
      "totalAmount": 5000.00,
      "currency": "TRY",
      "purchaseDate": "2025-10-07T10:35:00Z",
      "paymentStatus": "Completed",
      "codesGenerated": 100,
      "codesUsed": 12,
      "status": "Active"
    }
    // ... more purchases
  ]
}
```

---

#### 6. Get Sponsored Farmers
```http
GET /api/v1/sponsorship/farmers
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "farmerId": 237,
      "farmerName": "Ahmet Yılmaz",
      "farmerEmail": "ahmet@example.com",
      "code": "AGRI-2025-X3K9",
      "subscriptionTier": "Medium",
      "redeemedDate": "2025-10-08T14:22:00Z",
      "distributedTo": "Ahmet Yılmaz (+905551234567)"
    }
    // ... more farmers
  ]
}
```

---

#### 7. Get Sponsorship Statistics
```http
GET /api/v1/sponsorship/statistics
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalSpent": 15000.00,
    "totalCodesPurchased": 300,
    "totalCodesUsed": 87,
    "usageRate": 29.00,
    "unusedCodes": 213,
    "usageByTier": [
      {
        "tierName": "S",
        "codesPurchased": 50,
        "codesUsed": 15
      },
      {
        "tierName": "M",
        "codesPurchased": 200,
        "codesUsed": 62
      },
      {
        "tierName": "L",
        "codesPurchased": 50,
        "codesUsed": 10
      }
    ]
  }
}
```

---

#### 8. Get Link Statistics
```http
GET /api/v1/sponsorship/link-statistics?startDate=2025-10-01&endDate=2025-10-31
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Response:**
```json
{
  "success": true,
  "data": {
    "period": {
      "startDate": "2025-10-01",
      "endDate": "2025-10-31"
    },
    "totalLinksSent": 250,
    "totalLinksClicked": 187,
    "clickThroughRate": 74.80,
    "deliveryStatusBreakdown": {
      "sent": 245,
      "failed": 5,
      "pending": 0
    },
    "channelPerformance": {
      "SMS": {
        "sent": 100,
        "clicked": 68,
        "ctr": 68.00
      },
      "WhatsApp": {
        "sent": 145,
        "clicked": 119,
        "ctr": 82.07
      }
    }
  }
}
```

---

#### 9. Send Message to Farmer (M/L/XL Tiers)
```http
POST /api/v1/sponsorship/messages
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Request Body:**
```json
{
  "toUserId": 237,
  "plantAnalysisId": 456,
  "messageText": "Merhaba Ahmet Bey, analiz sonuçlarınızı inceledim. Size özel bir ürün tavsiyem var.",
  "messageType": "ProductRecommendation"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Mesaj başarıyla gönderildi",
  "data": {
    "id": 123,
    "fromUserId": 42,
    "toUserId": 237,
    "plantAnalysisId": 456,
    "messageText": "Merhaba Ahmet Bey...",
    "sentDate": "2025-10-08T15:30:00Z",
    "isRead": false
  }
}
```

---

#### 10. Create Smart Link (XL Tier Only)
```http
POST /api/v1/sponsorship/smart-links
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

**Request Body:**
```json
{
  "linkUrl": "https://shop.tarim-as.com/fungicide-x",
  "linkText": "Fungiside X - Külleme İlacı",
  "linkDescription": "Organik ve etkili külleme tedavisi",
  "linkType": "Product",
  "keywords": ["fungicide", "organic", "mildew"],
  "productCategory": "Pesticide",
  "targetCropTypes": ["Domates", "Biber", "Patlıcan"],
  "targetDiseases": ["Mildew", "Powdery Mildew"],
  "priority": 80,
  "displayPosition": "Inline",
  "displayStyle": "Card",
  "productName": "Fungiside X",
  "productPrice": 249.99,
  "productCurrency": "TRY",
  "isPromotional": true,
  "discountPercentage": 15
}
```

**Response:**
```json
{
  "success": true,
  "message": "Smart link oluşturuldu",
  "data": {
    "id": 78,
    "sponsorId": 42,
    "linkUrl": "https://shop.tarim-as.com/fungicide-x",
    "linkText": "Fungiside X - Külleme İlacı",
    "isActive": true,
    "isApproved": false,
    "clickCount": 0,
    "clickThroughRate": 0,
    "createdDate": "2025-10-08T16:00:00Z"
  }
}
```

---

### Farmer Endpoints

#### 11. Redeem Sponsorship Code
```http
POST /api/v1/sponsorship/redeem
Authorization: Bearer {token}
Roles: Farmer, Admin
```

**Request Body:**
```json
{
  "code": "AGRI-2025-X3K9"
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Medium aboneliğiniz başarıyla aktive edildi!",
  "data": {
    "id": 567,
    "userId": 237,
    "subscriptionTierId": 2,
    "tierName": "M",
    "startDate": "2025-10-08T14:22:00Z",
    "endDate": "2025-11-07T14:22:00Z",
    "isActive": true,
    "isSponsoredSubscription": true,
    "sponsorId": 42,
    "sponsorName": "Tarım Teknolojileri A.Ş."
  }
}
```

**Response (Error - Code Already Used):**
```json
{
  "success": false,
  "message": "Bu kod daha önce kullanılmış."
}
```

**Response (Error - Active Subscription Exists):**
```json
{
  "success": false,
  "message": "Zaten aktif bir aboneliğiniz var. Mevcut aboneliğiniz sona erdikten sonra yeni kod kullanabilirsiniz."
}
```

---

#### 12. Validate Sponsorship Code
```http
GET /api/v1/sponsorship/validate/AGRI-2025-X3K9
Authorization: Bearer {token}
Roles: Farmer, Sponsor, Admin
```

**Response (Valid Code):**
```json
{
  "success": true,
  "message": "Code is valid",
  "data": {
    "code": "AGRI-2025-X3K9",
    "subscriptionTier": "Premium",
    "expiryDate": "2026-10-07T10:35:00Z",
    "isValid": true
  }
}
```

**Response (Invalid Code):**
```json
{
  "success": false,
  "message": "Kod bulunamadı veya süresi dolmuş",
  "data": {
    "code": "AGRI-2025-X3K9",
    "isValid": false
  }
}
```

---

### Display Logic Endpoints

#### 13. Get Logo Permissions for Analysis
```http
GET /api/v1/sponsorship/logo-permissions/analysis/456?screen=result
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "plantAnalysisId": 456,
    "screen": "result",
    "canShowLogo": true,
    "tierName": "M",
    "sponsor": {
      "sponsorId": 42,
      "companyName": "Tarım Teknolojileri A.Ş.",
      "logoUrl": "https://cdn.ziraai.com/logos/tarim-as.png"
    }
  }
}
```

---

#### 14. Get Sponsor Display Info for Analysis
```http
GET /api/v1/sponsorship/display-info/analysis/456?screen=start
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "plantAnalysisId": 456,
    "screen": "start",
    "shouldDisplay": true,
    "sponsor": {
      "sponsorId": 42,
      "companyName": "Tarım Teknolojileri A.Ş.",
      "logoUrl": "https://cdn.ziraai.com/logos/tarim-as.png",
      "websiteUrl": "https://tarim-as.com"
    },
    "displaySettings": {
      "position": "bottom",
      "size": "medium",
      "opacity": 0.9
    }
  }
}
```

---

## ⚙️ Configuration

### Environment Variables

#### Required Configuration (All Environments)

```bash
# Database
ConnectionStrings__DArchPgContext=Host=xxx;Port=5432;Database=railway;Username=postgres;Password=xxx

# Mobile App Package Names (Environment-Specific)
MobileApp__PlayStorePackageName=com.ziraai.app              # Production
# MobileApp__PlayStorePackageName=com.ziraai.app.staging    # Staging
# MobileApp__PlayStorePackageName=com.ziraai.app.dev        # Development

# Redemption & Deep Links (Environment-Specific)
Referral__DeepLinkBaseUrl=https://ziraai.com/ref/                      # Production
# Referral__DeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/ref/ # Staging
# Referral__DeepLinkBaseUrl=https://localhost:5001/ref/                # Development

Referral__FallbackDeepLinkBaseUrl=https://ziraai.com/ref/              # Production fallback
SponsorRequest__DeepLinkBaseUrl=https://ziraai.com/sponsor-request/   # Production

# Sponsor Request Security
Security__RequestTokenSecret=${SPONSOR_REQUEST_TOKEN_SECRET}           # 32+ char random string
```

#### appsettings.json Configuration

```json
{
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 10,
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "Yapay destekli ZiraAI kullanarak bitkilerimi analiz yapmak istiyorum. Bunun için ZiraAI uygulamasında sponsor olmanızı istiyorum."
  },
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app"
  },
  "Referral": {
    "DeepLinkBaseUrl": "https://ziraai.com/ref/",
    "FallbackDeepLinkBaseUrl": "https://ziraai.com/ref/"
  },
  "Sponsorship": {
    "DefaultCodePrefix": "AGRI",
    "DefaultValidityDays": 365,
    "DefaultSubscriptionDays": 30,
    "MaxCodesPerPurchase": 1000,
    "MinCodesPerPurchase": 1
  },
  "SmartLinks": {
    "MaxLinksPerSponsor": {
      "S": 0,
      "M": 0,
      "L": 0,
      "XL": 50
    },
    "RequireApproval": true,
    "DefaultPriority": 50
  }
}
```

### Configuration Priority

1. **Environment Variables** (Highest)
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **Database Configuration**
5. **Fallback Values**

### Related Documentation
- [Environment Configuration Guide](./environment-configuration.md) - Complete environment variable reference
- [ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md](./ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md) - Railway-validated comprehensive guide

---

## 🛠️ Implementation Guide

### Step 1: Database Setup

```sql
-- Run migrations
dotnet ef migrations add AddSponsorshipSystem --project DataAccess --startup-project WebAPI

-- Apply to database
dotnet ef database update --project DataAccess --startup-project WebAPI

-- Verify tables created
SELECT table_name FROM information_schema.tables
WHERE table_name IN ('SponsorshipPurchases', 'SponsorshipCodes', 'SponsorProfiles',
                     'SponsorAnalysisAccess', 'SmartLinks');
```

### Step 2: Seed Subscription Tiers

```sql
INSERT INTO "SubscriptionTiers"
("TierName", "DisplayName", "Description", "MonthlyPrice", "DailyRequestLimit", "MonthlyRequestLimit", "IsActive")
VALUES
('S', 'Small', 'Temel sponsorluk paketi', 50.00, 10, 300, true),
('M', 'Medium', 'Orta seviye sponsorluk + mesajlaşma', 100.00, 20, 600, true),
('L', 'Large', 'Gelişmiş veri erişimi + tüm ekran logoları', 200.00, 50, 1500, true),
('XL', 'Extra Large', 'Tam veri erişimi + Smart Links + AI', 400.00, 100, 3000, true);
```

### Step 3: Create Sponsor User

```http
POST /api/v1/auth/register
{
  "fullName": "Tarım Teknolojileri A.Ş.",
  "email": "sponsor@tarim-as.com",
  "mobilePhones": "+902121234567",
  "password": "SecurePass123!",
  "role": "Sponsor"
}
```

### Step 4: Create Sponsor Profile

```http
POST /api/v1/sponsorship/create-profile
Authorization: Bearer {sponsor_token}
{
  "companyName": "Tarım Teknolojileri A.Ş.",
  "companyDescription": "Modern tarım çözümleri",
  "sponsorLogoUrl": "https://cdn.ziraai.com/logos/tarim-as.png",
  "contactEmail": "info@tarim-as.com"
}
```

### Step 5: Purchase Codes

```http
POST /api/v1/sponsorship/purchase-package
Authorization: Bearer {sponsor_token}
{
  "subscriptionTierId": 2,
  "quantity": 10,
  "totalAmount": 500.00,
  "paymentReference": "TEST-PAY-001"
}
```

### Step 6: Distribute Codes

```http
POST /api/v1/sponsorship/send-link
Authorization: Bearer {sponsor_token}
{
  "recipients": [
    {
      "code": "AGRI-2025-X3K9",
      "phone": "+905551234567",
      "name": "Test Farmer"
    }
  ],
  "channel": "SMS"
}
```

### Step 7: Test Redemption

```http
POST /api/v1/sponsorship/redeem
Authorization: Bearer {farmer_token}
{
  "code": "AGRI-2025-X3K9"
}
```

---

## 🔗 Smart Links System (XL Tier)

### Overview
Smart Links enable XL tier sponsors to display contextual product recommendations to farmers based on AI-powered analysis matching.

### Targeting Logic

```csharp
// Example: Automatic link matching
PlantAnalysis analysis = await GetAnalysis(456);
// Analysis shows: Tomato plant with Powdery Mildew

SmartLink[] matchingLinks = await GetMatchingSmartLinks(analysis);
// Returns links targeting:
// - TargetCropTypes: ["Domates"]
// - TargetDiseases: ["Powdery Mildew"]
// - Keywords: ["fungicide", "organic"]

// Sorted by:
// 1. RelevanceScore (AI-calculated)
// 2. Priority (1-100)
// 3. ClickThroughRate (historical performance)
```

### Display Integration

**Mobile App Integration:**
```dart
// Flutter example
Widget buildAnalysisResult(PlantAnalysis analysis) {
  return Column(
    children: [
      // Analysis results
      AnalysisResultCard(analysis),

      // Smart Links (if sponsor is XL tier)
      FutureBuilder<List<SmartLink>>(
        future: api.getSmartLinksForAnalysis(analysis.id),
        builder: (context, snapshot) {
          if (!snapshot.hasData) return Container();

          return SmartLinksCarousel(
            links: snapshot.data!,
            onLinkClick: (link) => api.trackSmartLinkClick(link.id)
          );
        }
      )
    ]
  );
}
```

### Analytics Tracking

```csharp
// Automatic tracking on link click
public async Task<IResult> TrackSmartLinkClickAsync(int linkId, int userId)
{
    var link = await _smartLinkRepository.GetAsync(l => l.Id == linkId);

    // Update analytics
    link.ClickCount++;
    link.UniqueClickCount++;  // De-duplicate by userId
    link.LastClickDate = DateTime.Now;
    link.ClickThroughRate = (decimal)link.ClickCount / link.DisplayCount * 100;

    // Log click event
    var clickEvent = new {
        UserId = userId,
        Timestamp = DateTime.Now,
        AnalysisContext = GetAnalysisContext(userId)
    };
    link.ClickHistory = AppendToJsonArray(link.ClickHistory, clickEvent);

    await _smartLinkRepository.SaveChangesAsync();
    return new SuccessResult();
}
```

### Budget Management

```csharp
// Check budget before displaying link
public async Task<bool> CanDisplaySmartLinkAsync(int linkId)
{
    var link = await _smartLinkRepository.GetAsync(l => l.Id == linkId);

    // Check budget constraints
    if (link.TotalBudget.HasValue && link.SpentBudget >= link.TotalBudget)
        return false;

    // Check click limits
    if (link.MaxClickCount.HasValue && link.ClickCount >= link.MaxClickCount)
        return false;

    // Check display limits per user
    if (link.MaxDisplayCount.HasValue)
    {
        var userDisplayCount = await GetUserDisplayCountAsync(linkId, currentUserId);
        if (userDisplayCount >= link.MaxDisplayCount)
            return false;
    }

    return link.IsActive && link.IsApproved;
}
```

---

## 📊 Analytics & Reporting

### Key Metrics

#### 1. Purchase Metrics
- Total investment (TRY/USD/EUR)
- Codes purchased by tier
- Average cost per code
- Purchase frequency

#### 2. Distribution Metrics
- Links sent (SMS vs WhatsApp)
- Delivery success rate
- Channel performance comparison
- Link click-through rate

#### 3. Redemption Metrics
- Total codes redeemed
- Redemption rate (%)
- Time-to-redemption (days)
- Redemption by tier

#### 4. ROI Metrics
- Cost per redeemed code
- Farmer lifetime value
- Sponsor branding impressions
- Data access value

#### 5. Smart Link Metrics (XL Tier)
- Total impressions
- Click-through rate
- Conversion rate
- Cost per conversion
- Revenue per link

### Sample Analytics Query

```sql
-- Sponsor Performance Dashboard
SELECT
    sp.CompanyName,
    st.TierName,
    COUNT(DISTINCT spu.Id) as TotalPurchases,
    SUM(spu.Quantity) as TotalCodesCreated,
    SUM(spu.CodesUsed) as TotalCodesRedeemed,
    ROUND(SUM(spu.CodesUsed)::decimal / NULLIF(SUM(spu.Quantity), 0) * 100, 2) as RedemptionRate,
    SUM(spu.TotalAmount) as TotalInvestment,
    ROUND(SUM(spu.TotalAmount) / NULLIF(SUM(spu.CodesUsed), 0), 2) as CostPerRedemption
FROM SponsorProfiles sp
JOIN SponsorshipPurchases spu ON sp.SponsorId = spu.SponsorId
JOIN SubscriptionTiers st ON spu.SubscriptionTierId = st.Id
WHERE sp.IsActive = true
GROUP BY sp.CompanyName, st.TierName
ORDER BY TotalInvestment DESC;
```

---

## 🚧 Planned Features

### 1. WhatsApp Sponsor Request System

**Status:** 📋 Designed (0% Implemented)
**Documentation:** Available in memory `whatsapp_sponsor_request_system_plan`

**Overview:**
Farmers can request sponsorship from companies via WhatsApp messages with deeplinks. Sponsors manage requests through mobile dashboard.

**Key Workflows:**
```
Farmer → Compose WhatsApp message → Send to sponsor
         ↓
       Deeplink: https://ziraai.com/sponsor-request/{token}
         ↓
Sponsor clicks → Opens ZiraAI app → Views request
         ↓
       Approve → Auto-generates sponsorship code → Sends to farmer
```

**New Entities:**
- `SponsorRequest`: Tracks farmer sponsorship requests
- `SponsorContact`: Sponsor's contact list management

**Implementation Phases:**
1. Backend Foundation (2-3 days)
2. Security & Integration (1-2 days)
3. Mobile UI - Farmer (2-3 days)
4. Mobile UI - Sponsor (3-4 days)
5. Enhancement & Polish (1-2 days)

**Related Documentation:**
- Memory: `whatsapp_sponsor_request_system_plan` - Complete implementation plan (5,000+ words)
- Memory: `whatsapp_sponsor_request_documentation_complete` - Full documentation suite (15,000+ words, 5 documents)
  - Technical Architecture
  - API Reference
  - Implementation Guide
  - Deployment Guide
  - Troubleshooting Guide

---

### 2. Referral Tier System Integration

**Status:** 📋 Designed (0% Implemented)
**Documentation:** Available in memory `referral_tier_system_ready_to_implement`

**Overview:**
Integrate sponsorship with referral system. Sponsors can incentivize farmers to refer others with bonus credits.

**Key Features:**
- 4 new database tables
- Configurable rewards (default: 10 credits per referral)
- Hybrid SMS+WhatsApp delivery
- 30-day link expiry
- Validation gate: 1 analysis required before referring

**Planned Tables:**
- `ReferralCodes`: User referral codes
- `ReferralUsage`: Referral redemption tracking
- `ReferralRewards`: Reward configurations
- `ReferralStatistics`: Analytics

**Integration Points:**
- Sponsorship codes can grant bonus referral credits
- XL tier sponsors can view referral chains
- Smart Links can promote referral programs

**Related Documentation:**
- Memory: `referral_tier_system_ready_to_implement` - 1,800+ line comprehensive design document

---

## 📚 Related Documentation

### Project Documentation
- [CLAUDE.md](../CLAUDE.md) - Project overview and development guidelines
- [Environment Configuration Guide](./environment-configuration.md) - Environment-specific configuration
- [ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md](./ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md) - Railway-validated comprehensive reference (v2.0.0)
- [Referral Testing Guide](./referral-testing-guide.md) - End-to-end testing guide

### Memory References
- `whatsapp_sponsor_request_system_plan` - Complete WhatsApp sponsor request implementation plan
- `whatsapp_sponsor_request_documentation_complete` - 5-document technical documentation suite
- `referral_tier_system_ready_to_implement` - Comprehensive referral system design (1,800+ lines)
- `referral_environment_configuration_complete` - Environment-based configuration implementation
- `sms_referral_auto_fill_implementation_complete` - Deferred deep linking implementation

### API Collections
- **Postman Collection:** `ZiraAI_Complete_API_Collection_v6.1.json`
  - 120+ endpoints including all sponsorship APIs
  - Pre-configured test scripts
  - Environment variables for dev/staging/production

---

## 📝 Changelog

### Version 1.0.0 (2025-10-07)
- ✅ Initial documentation release
- ✅ Core sponsorship system 100% implemented
- ✅ Tier-based features documented
- ✅ API reference complete
- ✅ Smart Links system documented
- ✅ Planned features outlined with references

---

## 🤝 Support & Contribution

### Getting Help
- Review this documentation thoroughly
- Check memory references for detailed implementations
- Consult related documentation files
- Review Postman collection for API examples

### Updating Documentation
When updating this document:
1. Update version number and changelog
2. Sync with related documentation files
3. Update implementation status percentages
4. Add new sections as features are implemented
5. Update memory references if new memories are created

---

**End of Documentation**

*Last Updated: 2025-10-07 by Claude Code*
*Document Version: 1.0.0*
*Sponsorship System Status: ✅ Core 100% | 🚧 Extensions Planned*
