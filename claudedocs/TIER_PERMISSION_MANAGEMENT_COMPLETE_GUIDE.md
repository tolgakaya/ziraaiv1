# Tier-Based Permission Management - Complete Guide

**Date:** 2025-10-26  
**Purpose:** Subscription tier'larına göre permission ve feature access yönetimi
**Status:** ✅ Complete System Documentation

---

## Executive Summary

ZiraAI'da **2 farklı permission sistemi** bulunuyor:

1. **OperationClaim-Based Permissions** → Role-based authorization (Admin, Farmer, Sponsor vb.)
2. **Tier-Based Feature Access** → Subscription tier'larına göre feature kısıtlamaları

**ÖNEMLI:** Tier-based permissions **OperationClaim sistemine BAĞLI DEĞİL** - tamamen ayrı, service-level kontrollerle yönetiliyor!

---

## 1. OperationClaim-Based Permission System

### 1.1. Temel Yapı

**Entity:** `Core.Entities.Concrete.OperationClaim`

```csharp
public class OperationClaim : BaseEntity
{
    public string Name { get; set; }        // "PlantAnalysis.Create"
    public string Alias { get; set; }       // "Create Plant Analysis"
    public string Description { get; set; } // "Create new plant analysis"
}
```

**İlişkiler:**
- **UserClaim:** Kullanıcıya direkt permission atama
- **GroupClaim:** Gruba permission atama → Gruptaki tüm kullanıcılar inherit eder
- **Group:** Kullanıcı grupları (Administrators, Farmers, Sponsors, Support, API Users)

---

### 1.2. Permission Seeds

**Location:** `Business/Seeds/OperationClaimSeeds.cs`

**Kategoriler:**

| ID Range | Category | Examples |
|----------|----------|----------|
| 1-4 | System Admin | Admin, UserManagement, RoleManagement, ConfigurationManagement |
| 5-6 | User Roles | Farmer, Sponsor |
| 10-15 | Plant Analysis | PlantAnalysis.Create/Read/Update/Delete/List/Export |
| 20-25 | Subscription | Subscription.Create/Read/Update/Cancel/List, SubscriptionTier.Manage |
| 30-37 | Sponsorship | Sponsorship.*/SponsorshipCode.*/SponsorshipPurchase.Create |
| 40-45 | Sponsor Profile | SponsorProfile.*/SponsorContact.Manage/SponsorRequest.Manage |
| 50-54 | **Smart Link** | SmartLink.Create/Read/Update/Delete/Analytics **(XL tier only!)** |
| 60-62 | Analytics | Analytics.Dashboard/Reports/Export |
| 70-72 | Logs & Audit | Logs.View/Export, SecurityEvents.View |
| 80-82 | API Access | API.FullAccess/ReadOnly/PlantAnalysis |
| 90-91 | Mobile App | Mobile.Access, Mobile.PushNotifications |

**Total:** 91 OperationClaims

---

### 1.3. Group-Based Permissions

**Location:** `Business/Seeds/GroupSeeds.cs`

**Groups:**

| ID | Group Name | Description | Claims Count |
|----|------------|-------------|--------------|
| 1 | Administrators | Full system access | 91 (ALL) |
| 2 | Farmers | Basic plant analysis | 8 claims |
| 3 | Sponsors | Sponsorship management | 28 claims |
| 4 | Support | Read access + basic management | 12 claims |
| 5 | API Users | API access | 2 claims |

**Farmers Group Claims (8):**
```csharp
var farmerClaims = new[] { 
    5,   // Farmer role
    10,  // PlantAnalysis.Create
    11,  // PlantAnalysis.Read
    14,  // PlantAnalysis.List
    21,  // Subscription.Read
    41,  // SponsorProfile.Read
    90,  // Mobile.Access
    91   // Mobile.PushNotifications
};
```

**Sponsors Group Claims (28):**
```csharp
var sponsorClaims = new[] { 
    6,   // Sponsor role
    11,  // PlantAnalysis.Read
    14,  // PlantAnalysis.List
    21,  // Subscription.Read
    24,  // Subscription.List
    30-37, // All Sponsorship.*
    40-45, // All SponsorProfile.* + SponsorContact/Request
    50-54, // All SmartLink.* (XL tier feature!)
    60-62, // All Analytics.*
    90-91  // Mobile access
};
```

**⚠️ IMPORTANT NOTE:**
- **SmartLink claims (50-54)** Sponsors grubunda VAR
- **ANCAK** SmartLink.Create permission'ı tier kontrolü ile sınırlandırılıyor
- Yani: Sponsor rolü SmartLink.Create claim'ine sahip OLSA BİLE, **XL tier'ı yoksa** smart link oluşturamaz!

---

## 2. Tier-Based Feature Access System

### 2.1. Kontrol Mekanizması

**Tier-based feature kontrolü SERVICE LAYER'da** yapılıyor, **OperationClaim'den BAĞIMSIZ!**

**Yöntem:**
- Service metodları `SubscriptionTier.Id` veya `TierName` kontrolü yapıyor
- Controller'larda `[SecuredOperation]` attribute'u OperationClaim kontrolü yapıyor
- İki sistem **AYRI AYRI** kontrol ediliyor

---

### 2.2. Messaging Feature (L, XL Tier)

**Location:** `Business/Services/Sponsorship/AnalysisMessagingService.cs:67-68`

**Kod:**
```csharp
// Sadece L, XL tier'larında mesajlaşma var (L=3, XL=4)
// M tier'da mesajlaşma yok çünkü çiftçi profili anonim
if (purchase.SubscriptionTierId >= 3) // L=3, XL=4
{
    return true;
}
```

**⚠️ HATALI YORUM:** Kod yorumunda "L=3, XL=4" yazıyor ama **gerçekte:**
- M = ID 3
- L = ID 4
- XL = ID 5

**Doğrusu:**
```csharp
// Sadece L, XL tier'larında mesajlaşma var (L=4, XL=5)
// M tier'da mesajlaşma yok çünkü çiftçi profili anonim
if (purchase.SubscriptionTierId >= 4) // L=4, XL=5
{
    return true;
}
```

**Kontrol Edilen Metodlar:**
1. `CanSendMessageAsync(int sponsorId)` - Genel mesajlaşma yetkisi
2. `CanSendMessageForAnalysisAsync(...)` - Analiz bazlı mesajlaşma
3. `CanFarmerReplyAsync(...)` - Farmer'ın cevap verme yetkisi

---

### 2.3. Smart Link Feature (XL Tier Only)

**Location:** `Business/Services/Sponsorship/SmartLinkService.cs:45-52`

**Kod:**
```csharp
foreach (var purchase in profile.SponsorshipPurchases)
{
    // XL tier (ID=4) smart link oluşturabilir
    if (purchase.SubscriptionTierId == 4) // XL tier
    {
        Console.WriteLine($"[SmartLinkService] Sponsor {sponsorId} has XL tier, can create smart links");
        return true;
    }
}
```

**⚠️ HATALI ID:** Kod XL tier için ID=4 kullanıyor ama **gerçekte XL = ID 5**

**Doğrusu:**
```csharp
if (purchase.SubscriptionTierId == 5) // XL tier (CORRECT ID)
```

**Özellikler:**
- XL tier sponsors: 50 smart link quota
- Smart link oluşturmadan önce `CanCreateSmartLinksAsync()` kontrolü
- Quota kontrolü: `GetActiveSmartLinksCountAsync() < GetMaxSmartLinksAsync()`

---

### 2.4. Sponsor Visibility (M, L, XL Tiers)

**Location:** `Business/Services/Sponsorship/SponsorVisibilityService.cs`

**Kontroller:**

```csharp
// Logo visibility - M, L, XL
if (tier != null && (tier.TierName == "M" || tier.TierName == "L" || tier.TierName == "XL"))
{
    // Show logo
}

// Profile visibility - L, XL
if (tier != null && (tier.TierName == "L" || tier.TierName == "XL"))
{
    // Show full profile
}
```

**Visibility Levels:**
```csharp
"start" => tierName == "M" || tierName == "L" || tierName == "XL",     // Analysis start screen
"analysis" => tierName == "L" || tierName == "XL",                     // Analysis result screen
"profile" => tierName == "L" || tierName == "XL",                      // Profile details
```

---

### 2.5. Data Access Percentage (S, M, L, XL)

**Location:** `Business/Services/Sponsorship/SponsorDataAccessService.cs`

**Kod:**
```csharp
var accessPercentage = purchase.SubscriptionTierId switch
{
    1 => 0,   // Trial: No access
    2 => 30,  // S tier: 30%
    3 => 60,  // M tier: 60%
    4 => 100, // L tier: 100%
    5 => 100, // XL tier: 100%
    _ => 0
};
```

**Kullanım:**
- Sponsorların erişebileceği farmer datası yüzdesi
- S tier: %30 (sadece temel bilgiler)
- M tier: %60 (daha detaylı bilgiler)
- L/XL tier: %100 (tüm bilgiler)

---

## 3. Permission Control Flow

### 3.1. Standard API Endpoint Authorization

```
HTTP Request: POST /api/v1/sponsorship/messages
    ↓
1. Controller Attribute: [SecuredOperation("Sponsor")]
    ↓
2. OperationClaim Check: User has "Sponsor" claim?
    ↓ YES
3. Command Handler: SendMessageCommand.Handle()
    ↓
4. Service Method: _messagingService.CanSendMessageForAnalysisAsync()
    ↓
5. Tier Check: purchase.SubscriptionTierId >= 4? (L or XL)
    ↓ YES
6. Execute: Send message
    ↓
Response: 200 OK
```

**Fail Scenarios:**

**Scenario A - No Sponsor Claim:**
```
[SecuredOperation("Sponsor")] → 403 Forbidden
"You are not authorized to perform this operation"
```

**Scenario B - Has Sponsor Claim, but S or M Tier:**
```
CanSendMessageForAnalysisAsync() → false
"Messaging is only available for L and XL tier sponsors"
```

---

### 3.2. Smart Link Creation Flow

```
HTTP Request: POST /api/v1/sponsorship/smartlinks
    ↓
1. [SecuredOperation("SmartLink.Create")] - OperationClaim check
    ↓ User has claim
2. CreateSmartLinkCommand.Handle()
    ↓
3. _smartLinkService.CanCreateSmartLinksAsync(sponsorId)
    ↓
4. Tier Check: purchase.SubscriptionTierId == 5? (XL tier)
    ↓ YES
5. Quota Check: activeCount < maxLinks (50)?
    ↓ YES
6. Create Smart Link
    ↓
Response: 201 Created
```

**Fail Scenarios:**

**Scenario A - No SmartLink.Create Claim:**
```
[SecuredOperation] → 403 Forbidden
```

**Scenario B - Has Claim, but Not XL Tier:**
```
CanCreateSmartLinksAsync() → false
"Smart links are only available for XL tier sponsors"
```

**Scenario C - Quota Exceeded:**
```
GetActiveSmartLinksCountAsync() >= 50
"You have reached your smart link quota (50 links)"
```

---

## 4. Implementation Locations

### 4.1. Permission Seeds

| File | Purpose |
|------|---------|
| `Business/Seeds/OperationClaimSeeds.cs` | 91 OperationClaim tanımları |
| `Business/Seeds/GroupSeeds.cs` | 5 Group + GroupClaim ilişkileri |

**Seed Execution:** `Business/Services/DatabaseInitializer/DatabaseInitializerService.cs`

---

### 4.2. Tier-Based Feature Controls

| Feature | Location | Tier Requirement |
|---------|----------|------------------|
| Messaging | `Business/Services/Sponsorship/AnalysisMessagingService.cs:67` | L (4), XL (5) |
| Smart Link | `Business/Services/Sponsorship/SmartLinkService.cs:45` | XL (5) |
| Logo Visibility | `Business/Services/Sponsorship/SponsorVisibilityService.cs` | M (3), L (4), XL (5) |
| Profile Visibility | `Business/Services/Sponsorship/SponsorVisibilityService.cs` | L (4), XL (5) |
| Data Access % | `Business/Services/Sponsorship/SponsorDataAccessService.cs` | S=30%, M=60%, L/XL=100% |
| Voice Messages | `Business/Services/Messaging/MessagingFeatureService.cs` | XL (5) |

---

### 4.3. Entity Configurations

| Entity | Location | Purpose |
|--------|----------|---------|
| OperationClaim | `DataAccess/Concrete/Configurations/OperationClaimEntityConfiguration.cs` | Claim seed data |
| Group | `DataAccess/Concrete/Configurations/GroupEntityConfiguration.cs` | Group seed data |
| GroupClaim | `DataAccess/Concrete/Configurations/GroupClaimEntityConfiguration.cs` | Group-claim relationships |
| SubscriptionTier | `DataAccess/Concrete/Configurations/SubscriptionTierEntityConfiguration.cs` | Tier definitions |

---

## 5. Tier ID Reference

**⚠️ CRITICAL: Tier ID Mapping**

| Tier Name | Database ID | Feature Access |
|-----------|-------------|----------------|
| Trial | 1 | Basic analysis only |
| S (Small) | 2 | Analysis + 30% data access |
| M (Medium) | 3 | Analysis + 60% data + Logo visibility + Analytics |
| L (Large) | 4 | **Messaging** + 100% data + API + Profile visibility |
| XL (Extra Large) | 5 | **Smart Links** + **Voice Messages** + All features |

**Common Code Errors:**

```csharp
// ❌ WRONG (from current codebase)
if (purchase.SubscriptionTierId >= 3) // Comment says "L=3, XL=4"

// ✅ CORRECT
if (purchase.SubscriptionTierId >= 4) // L=4, XL=5

// ❌ WRONG (from current codebase)
if (purchase.SubscriptionTierId == 4) // For XL tier

// ✅ CORRECT
if (purchase.SubscriptionTierId == 5) // XL tier
```

---

## 6. Feature Matrix

| Feature | Trial | S | M | L | XL | Control Location |
|---------|-------|---|---|---|----|------------------|
| Plant Analysis | ✅ | ✅ | ✅ | ✅ | ✅ | Subscription quota |
| Daily Limit | 1 | 5 | 20 | 50 | 200 | SubscriptionTier.DailyRequestLimit |
| Monthly Limit | 30 | 50 | 200 | 500 | 2000 | SubscriptionTier.MonthlyRequestLimit |
| Advanced Analytics | ❌ | ❌ | ✅ | ✅ | ✅ | SubscriptionTier.AdvancedAnalytics |
| API Access | ❌ | ❌ | ❌ | ✅ | ✅ | SubscriptionTier.ApiAccess |
| Logo Visibility | ❌ | ❌ | ✅ | ✅ | ✅ | SponsorVisibilityService |
| Profile Visibility | ❌ | ❌ | ❌ | ✅ | ✅ | SponsorVisibilityService |
| Data Access % | 0% | 30% | 60% | 100% | 100% | SponsorDataAccessService |
| **Messaging** | ❌ | ❌ | ❌ | ✅ | ✅ | **AnalysisMessagingService** |
| **Voice Messages** | ❌ | ❌ | ❌ | ❌ | ✅ | **MessagingFeatureService** |
| **Smart Links** | ❌ | ❌ | ❌ | ❌ | ✅ | **SmartLinkService** |
| Priority Support | ❌ | ❌ | ❌ | ✅ | ✅ | SubscriptionTier.PrioritySupport |

---

## 7. Yeni Permission Ekleme (Best Practices)

### 7.1. OperationClaim Ekleme

**Step 1:** `Business/Seeds/OperationClaimSeeds.cs` güncellemesi
```csharp
new OperationClaim 
{ 
    Id = 100, 
    Name = "CustomFeature.Create", 
    Alias = "Create Custom Feature", 
    Description = "Create custom feature" 
}
```

**Step 2:** İlgili gruba claim ekleme
```csharp
// GroupSeeds.cs
var sponsorClaims = new[] { ..., 100 }; // Add new claim ID
```

**Step 3:** Controller'da `[SecuredOperation]` attribute
```csharp
[SecuredOperation("CustomFeature.Create")]
[HttpPost("custom-feature")]
public async Task<IActionResult> CreateCustomFeature(...)
```

**Step 4:** Migration ve veritabanı güncelleme
```bash
dotnet ef migrations add AddCustomFeatureClaim --project DataAccess --startup-project WebAPI
dotnet ef database update --project DataAccess --startup-project WebAPI
```

---

### 7.2. Tier-Based Feature Ekleme

**Step 1:** Service'de tier kontrolü ekle
```csharp
public async Task<bool> CanUseCustomFeatureAsync(int userId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
    
    if (profile?.SponsorshipPurchases != null)
    {
        foreach (var purchase in profile.SponsorshipPurchases)
        {
            // Custom feature: L ve XL tier'larda aktif
            if (purchase.SubscriptionTierId >= 4) // L=4, XL=5
            {
                return true;
            }
        }
    }
    
    return false;
}
```

**Step 2:** Command handler'da tier kontrolü
```csharp
public async Task<IResult> Handle(CreateCustomFeatureCommand request, ...)
{
    var canUse = await _customFeatureService.CanUseCustomFeatureAsync(request.UserId);
    
    if (!canUse)
    {
        return new ErrorResult("Custom feature is only available for L and XL tier users");
    }
    
    // Implementation...
}
```

**Step 3:** (Optional) SubscriptionTier entity'ye field ekle
```csharp
public class SubscriptionTier : IEntity
{
    // Existing fields...
    
    public bool CustomFeatureAccess { get; set; } // NEW
}
```

**Step 4:** Migration ve seed data güncelleme
```csharp
// SubscriptionTierEntityConfiguration.cs
new SubscriptionTier
{
    Id = 4, // L tier
    TierName = "L",
    // ...
    CustomFeatureAccess = true, // NEW
}
```

---

## 8. Testing Checklist

### 8.1. OperationClaim Testing

- [ ] User has claim → 200 OK
- [ ] User does NOT have claim → 403 Forbidden
- [ ] Group claim inheritance works correctly
- [ ] Admin group has all claims (1-91)

### 8.2. Tier-Based Feature Testing

- [ ] User with correct tier → Feature accessible
- [ ] User with lower tier → Feature blocked with clear error message
- [ ] Trial user → Only basic features accessible
- [ ] S tier → 30% data access
- [ ] M tier → Logo visible, no messaging
- [ ] L tier → Messaging works, no smart links
- [ ] XL tier → All features including smart links

### 8.3. Combined Testing (Claim + Tier)

- [ ] User has Sponsor claim + L tier → Can message
- [ ] User has Sponsor claim + S tier → Cannot message (tier restriction)
- [ ] User has SmartLink.Create claim + L tier → Cannot create smart link (tier restriction)
- [ ] User has SmartLink.Create claim + XL tier → Can create smart link ✅

---

## 9. Kritik Hatalar ve Düzeltmeleri

### 9.1. Tier ID Uyumsuzlukları

**Hata Konumları:**

1. `AnalysisMessagingService.cs:67` - Comment: "L=3, XL=4" → **Gerçek: L=4, XL=5**
2. `AnalysisMessagingService.cs:68` - Code: `>= 3` → **Doğrusu: >= 4**
3. `SmartLinkService.cs:45` - Code: `== 4` for XL → **Doğrusu: == 5**

**Düzeltme Önerisi:**

```csharp
// ❌ BEFORE (AnalysisMessagingService.cs:67-68)
// Sadece L, XL tier'larında mesajlaşma var (L=3, XL=4)
if (purchase.SubscriptionTierId >= 3) // L=3, XL=4

// ✅ AFTER
// Sadece L, XL tier'larında mesajlaşma var (L=4, XL=5)
if (purchase.SubscriptionTierId >= 4) // L=4, XL=5
```

```csharp
// ❌ BEFORE (SmartLinkService.cs:45)
if (purchase.SubscriptionTierId == 4) // XL tier

// ✅ AFTER
if (purchase.SubscriptionTierId == 5) // XL tier (CORRECT ID)
```

---

### 9.2. Magic Numbers Kullanımı

**Problem:** Tier ID'ler hard-coded
```csharp
if (purchase.SubscriptionTierId >= 4) // What is 4?
```

**Önerilen Çözüm:** Constants kullanımı
```csharp
public static class SubscriptionTierIds
{
    public const int Trial = 1;
    public const int Small = 2;
    public const int Medium = 3;
    public const int Large = 4;
    public const int ExtraLarge = 5;
}

// Usage:
if (purchase.SubscriptionTierId >= SubscriptionTierIds.Large)
{
    // Messaging allowed for L and XL tiers
}

if (purchase.SubscriptionTierId == SubscriptionTierIds.ExtraLarge)
{
    // Smart links only for XL tier
}
```

---

## 10. Architecture Decision Records

### ADR-001: Why Separate OperationClaim and Tier Systems?

**Decision:** Keep tier-based feature controls separate from OperationClaim system

**Rationale:**
- OperationClaims → **Static permissions** (create, read, update, delete operations)
- Tier checks → **Dynamic feature access** (changes with subscription, upgradeable)
- Separation allows tier upgrades without permission migrations
- Sponsors can have claim but be blocked by tier (clear monetization)

**Consequences:**
- Two-layer authorization checks (claim + tier)
- More complex testing scenarios
- Clear separation of concerns
- Better user upgrade path

---

### ADR-002: Why Service-Level Tier Checks?

**Decision:** Implement tier checks in service layer, not middleware or attributes

**Rationale:**
- Business logic belongs in services
- Flexible tier requirements per feature
- Easier to unit test
- Can provide detailed error messages
- Can check multiple purchases (hybrid sponsors)

**Consequences:**
- Developers must remember to add tier checks
- No centralized tier validation
- Can lead to inconsistent implementations

---

## 11. Quick Reference

### Permission Check Template

```csharp
// 1. OperationClaim check (Controller)
[SecuredOperation("Feature.Create")]
public async Task<IActionResult> CreateFeature(...)

// 2. Tier check (Service)
public async Task<bool> CanUseFeatureAsync(int userId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
    
    if (profile?.SponsorshipPurchases != null)
    {
        foreach (var purchase in profile.SponsorshipPurchases)
        {
            if (purchase.SubscriptionTierId >= RequiredTierId)
                return true;
        }
    }
    
    return false;
}
```

### Common Queries

**Get user's claims:**
```csharp
var userClaims = await _userClaimRepository.GetUserClaims(userId);
```

**Get user's tier:**
```csharp
var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
var tierId = profile?.SponsorshipPurchases?.FirstOrDefault()?.SubscriptionTierId;
```

**Check both claim and tier:**
```csharp
// Claim check via [SecuredOperation]
// Tier check via service method
var hasClaim = HttpContext.User.HasClaim(...);
var hasTier = await _service.CanUseFeatureAsync(userId);

if (hasClaim && hasTier)
{
    // Feature accessible
}
```

---

**Last Updated:** 2025-10-26  
**Author:** Claude Code  
**Related Documentation:**
- `claudedocs/TIER_BASED_FEATURES_GUIDE.md` - Feature restrictions
- `claudedocs/Messaging/MESSAGING_AUTHORIZATION_COMPLETE.md` - Messaging implementation
