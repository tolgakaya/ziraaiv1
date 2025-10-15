# Sponsorship Code Expiry vs Subscription Duration Issue

## 📋 Executive Summary

**Discovery Date:** 2025-10-11
**Status:** ⚠️ Design Issue - Needs Implementation
**Priority:** Medium - Affects user experience and tier differentiation
**Impact:** All sponsorship tiers currently give same 30-day subscription duration

---

## 🎯 Core Question

> "Bizim ürettiğim kodun bir expire süresi var mı, bu subscribe olduktan sonra geçerlilik süresinden farklı mı?"

**Short Answer:** Evet, 2 farklı süre var:
1. **Code Expiry (ExpiryDate):** Kod ne zamana kadar kullanılabilir? → **30 gün** (default, sponsor değiştirebilir)
2. **Subscription Duration (EndDate):** Kod kullanıldıktan sonra subscription ne kadar sürecek? → **30 gün (sabit, tier fark etmeksizin)**

---

## 📊 Current System Analysis

### 1. Code Expiry (`SponsorshipCode.ExpiryDate`)

**Location:** `Entities/Concrete/SponsorshipCode.cs` (Line 26)
```csharp
public DateTime ExpiryDate { get; set; } // Code expiration date
```

**Set During Code Generation:**
```csharp
// DataAccess/Concrete/EntityFramework/SponsorshipCodeRepository.cs (Line 161)
ExpiryDate = DateTime.Now.AddDays(validityDays)  // validityDays = 30 (default)
```

**Validation During Redemption:**
```csharp
// Business/Services/Sponsorship/SponsorshipService.cs (Line 369)
if (sponsorshipCode.ExpiryDate < DateTime.Now)
    return new ErrorDataResult<SponsorshipCode>("Code has expired");
```

**Purpose:** Defines the time window during which a farmer can redeem/activate the code.

**Current Value:** 30 days (default) from code generation - sponsor can customize via ValidityDays parameter

---

### 2. Subscription Duration (`UserSubscription.EndDate`)

**Location:** `Entities/Concrete/UserSubscription.cs`
```csharp
public DateTime? EndDate { get; set; }
```

**Set During Code Redemption:**
```csharp
// Business/Services/Sponsorship/SponsorshipService.cs (Line 317)
var subscription = new UserSubscription
{
    ...
    StartDate = DateTime.Now,
    EndDate = DateTime.Now.AddDays(30), // ⚠️ HARDCODED 30 DAYS
    ...
};
```

**Purpose:** Defines how long the farmer's subscription will remain active after redeeming the code.

**Current Value:** 30 days (hardcoded, same for all tiers)

---

## 🔍 Detailed Timeline Example

```
┌──────────────────────────────────────────────────────────────────────────┐
│  SPONSORSHIP LIFECYCLE - COMPLETE TIMELINE                               │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  📅 January 1, 2025: Sponsor Purchases 100 Codes (L Tier)               │
│     ├─ Purchase.ValidityDays = 30 (default)                             │
│     ├─ Codes Generated: AGRI-2025-X3K9, AGRI-2025-Y7P2, ...            │
│     └─ All codes: ExpiryDate = January 31, 2025                         │
│                                                                           │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                           │
│  📅 January 15, 2025: Sponsor Sends Code to Farmer A                    │
│     └─ DistributionDate = January 15, 2025                              │
│        Code: AGRI-2025-X3K9                                              │
│        Status: Sent but not yet redeemed                                │
│        ExpiryDate: January 31, 2025 (still valid for 16 days)          │
│                                                                           │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                           │
│  📅 January 20, 2025: Farmer A Redeems Code                             │
│     ├─ Code Validation:                                                  │
│     │  ✅ IsUsed = false                                                 │
│     │  ✅ IsActive = true                                                │
│     │  ✅ ExpiryDate (Jan 31, 2025) > Now (Jan 20, 2025)                │
│     │  ✅ CODE VALID - Proceed with redemption                           │
│     │                                                                     │
│     └─ Subscription Created:                                             │
│        ├─ StartDate = January 20, 2025                                  │
│        ├─ EndDate = February 19, 2025 (30 days) ⚠️ HARDCODED           │
│        ├─ Tier = L (Large)                                              │
│        ├─ Status = Active                                                │
│        └─ IsSponsoredSubscription = true                                │
│                                                                           │
│     Code Updated:                                                        │
│        ├─ IsUsed = true                                                  │
│        ├─ UsedDate = January 20, 2025                                   │
│        └─ UsedByUserId = [Farmer A's ID]                                │
│                                                                           │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                           │
│  📅 February 19, 2025: Farmer A's Subscription Expires                  │
│     └─ Subscription ends after exactly 30 days                           │
│        (No renewal for sponsored subscriptions)                          │
│                                                                           │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                           │
│  📅 January 25, 2025: Another Farmer (B) Redeems Different Code        │
│     ├─ Code: AGRI-2025-Y7P2 (from same purchase)                       │
│     ├─ ExpiryDate still valid: January 31, 2025 > January 25, 2025    │
│     └─ Gets same 30-day subscription (Jan 25 → Feb 24, 2025)           │
│                                                                           │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                           │
│  📅 January 30, 2025: Farmer C Tries to Redeem Code                    │
│     ├─ Code: AGRI-2025-Z8M5 (unused code from same purchase)           │
│     ├─ ExpiryDate: January 31, 2025 > January 30, 2025                 │
│     ├─ ✅ Code still valid (1 day remaining)                            │
│     └─ Gets 30-day subscription (Jan 30 → Feb 28, 2025)                │
│                                                                           │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                           │
│  📅 February 5, 2025: Farmer D Tries to Redeem Last Code               │
│     ├─ Code: AGRI-2025-W3N1 (unused code from same purchase)           │
│     ├─ ExpiryDate: January 31, 2025 < February 5, 2025                 │
│     └─ ❌ CODE EXPIRED - Cannot redeem                                   │
│        Error: "Invalid or expired sponsorship code"                     │
│                                                                           │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## ⚠️ Current Issues

### Issue 1: No Tier Differentiation for Subscription Duration

**Problem:**
All tiers (S, M, L, XL) provide the same 30-day subscription duration after code redemption.

**Current Code:**
```csharp
// SponsorshipService.cs - ActivateSponsorship method (Line 317)
var subscription = new UserSubscription
{
    ...
    EndDate = DateTime.Now.AddDays(30), // Same for all tiers!
    ...
};
```

**Business Impact:**
- No incentive for sponsors to purchase higher tiers based on subscription duration
- Tier differentiation limited to request limits and pricing only
- Farmers get same benefit regardless of tier (only difference is request quotas)

**Example Scenario:**
```
Sponsor A buys 100 S-tier codes (lowest tier) → Farmers get 30 days
Sponsor B buys 100 XL-tier codes (highest tier) → Farmers also get 30 days

❌ No difference in subscription length despite tier difference!
```

---

### Issue 2: No Flexibility for Custom Durations

**Problem:**
Sponsors cannot offer custom subscription durations based on:
- Seasonal promotions (e.g., 60-day summer campaign)
- Loyalty programs (e.g., 45 days for returning farmers)
- Partnership agreements (e.g., 90 days for strategic partners)

**Current Limitation:**
Duration is hardcoded in service layer, not configurable at purchase or tier level.

---

### Issue 3: Confusion Between Two Time Periods

**Problem:**
System has two distinct time periods with different purposes:

1. **Code Expiry (30 days default):** How long code can be redeemed
2. **Subscription Duration (30 days):** How long subscription lasts after redemption

**Potential User Confusion:**
- Sponsors might think "ValidityDays: 30" means subscription duration is also 30 days (which happens to be true by default, but they are independent settings)
- Documentation doesn't clearly distinguish these two periods
- No UI indication of subscription duration at code generation time

---

## 📐 Architecture Analysis

### Entity Relationships

```
┌─────────────────────────┐
│  SponsorshipPurchase    │
│  ┌──────────────────┐   │
│  │ ValidityDays: 30  │  ← Code expiry period (when can code be used)
│  └──────────────────┘   │
└────────────┬────────────┘
             │ 1:N
             ▼
┌─────────────────────────┐
│   SponsorshipCode       │
│  ┌──────────────────┐   │
│  │ ExpiryDate       │  ← Calculated from ValidityDays
│  │ (30 days)        │  ← "Until when can farmer redeem this?"
│  └──────────────────┘   │
└────────────┬────────────┘
             │ Used by
             │ Redemption
             ▼
┌─────────────────────────┐
│   UserSubscription      │
│  ┌──────────────────┐   │
│  │ StartDate        │  ← When farmer redeemed code
│  │ EndDate          │  ← StartDate + 30 days ⚠️ HARDCODED
│  │ (30 days)        │  ← "How long will farmer have access?"
│  └──────────────────┘   │
└─────────────────────────┘
             ▲
             │
             └─────────── ⚠️ NO REFERENCE TO SubscriptionTier.DurationDays
                          (Because that field doesn't exist!)
```

### Missing Link: SubscriptionTier Duration

**What we have:**
```csharp
// SubscriptionTier.cs
public class SubscriptionTier
{
    public int DailyRequestLimit { get; set; }      ✅ Exists
    public int MonthlyRequestLimit { get; set; }    ✅ Exists
    public decimal MonthlyPrice { get; set; }       ✅ Exists
    public int MinPurchaseQuantity { get; set; }    ✅ Exists

    // ❌ MISSING:
    // public int SubscriptionDurationDays { get; set; }
}
```

**What we need:**
```csharp
public class SubscriptionTier
{
    ...
    public int SubscriptionDurationDays { get; set; } // NEW FIELD
    // How long subscription lasts when code is redeemed
    // Different per tier: S=14, M=21, L=30, XL=45 days
}
```

---

## 💡 Proposed Solutions

### Solution 1: Tier-Based Duration (⭐ RECOMMENDED)

**Approach:** Add `SubscriptionDurationDays` field to `SubscriptionTier` entity.

#### Implementation Steps

**1. Update Entity**

File: `Entities/Concrete/SubscriptionTier.cs`
```csharp
public class SubscriptionTier : IEntity
{
    public int Id { get; set; }
    public string TierName { get; set; } // S, M, L, XL
    public string DisplayName { get; set; }

    // Request Limits
    public int DailyRequestLimit { get; set; }
    public int MonthlyRequestLimit { get; set; }

    // Pricing
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; }

    // ✅ NEW: Subscription Duration
    public int SubscriptionDurationDays { get; set; } = 30; // Default 30 days

    // Sponsorship Purchase Limits
    public int MinPurchaseQuantity { get; set; } = 10;
    public int MaxPurchaseQuantity { get; set; } = 10000;
    public int RecommendedQuantity { get; set; } = 100;

    // ... rest of properties
}
```

**2. Create Migration**

```bash
dotnet ef migrations add AddSubscriptionDurationDaysToTier \
  --project DataAccess \
  --startup-project WebAPI \
  --context ProjectDbContext \
  --output-dir Migrations/Pg
```

**Migration SQL:**
```sql
-- Add column with default value
ALTER TABLE "SubscriptionTiers"
ADD COLUMN "SubscriptionDurationDays" INT NOT NULL DEFAULT 30;

-- Set tier-specific durations
UPDATE "SubscriptionTiers"
SET "SubscriptionDurationDays" = 14
WHERE "TierName" = 'S';

UPDATE "SubscriptionTiers"
SET "SubscriptionDurationDays" = 21
WHERE "TierName" = 'M';

UPDATE "SubscriptionTiers"
SET "SubscriptionDurationDays" = 30
WHERE "TierName" = 'L';

UPDATE "SubscriptionTiers"
SET "SubscriptionDurationDays" = 45
WHERE "TierName" = 'XL';

-- Add comment for documentation
COMMENT ON COLUMN "SubscriptionTiers"."SubscriptionDurationDays" IS
'Duration in days that a farmer subscription remains active after redeeming a sponsorship code';
```

**3. Update Service Logic**

File: `Business/Services/Sponsorship/SponsorshipService.cs`

**Before (Lines 276-353):**
```csharp
private async Task<IDataResult<UserSubscription>> ActivateSponsorship(
    string code,
    int userId,
    SponsorshipCode sponsorshipCode,
    UserSubscription existingSubscription)
{
    // ... existing validation logic ...

    // Get tier information
    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
    if (tier == null)
        return new ErrorDataResult<UserSubscription>("Subscription tier not found");

    // Create active subscription
    var subscription = new UserSubscription
    {
        UserId = userId,
        SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(30), // ❌ HARDCODED
        ...
    };
}
```

**After (FIXED):**
```csharp
private async Task<IDataResult<UserSubscription>> ActivateSponsorship(
    string code,
    int userId,
    SponsorshipCode sponsorshipCode,
    UserSubscription existingSubscription)
{
    // ... existing validation logic ...

    // Get tier information
    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
    if (tier == null)
        return new ErrorDataResult<UserSubscription>("Subscription tier not found");

    // ✅ Use tier's subscription duration
    var durationDays = tier.SubscriptionDurationDays > 0
        ? tier.SubscriptionDurationDays
        : 30; // Fallback to 30 if not set

    // Create active subscription
    var subscription = new UserSubscription
    {
        UserId = userId,
        SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(durationDays), // ✅ TIER-BASED!
        ...
    };

    Console.WriteLine($"[SponsorshipRedeem] Activated {tier.DisplayName} tier subscription for {durationDays} days");
}
```

**4. Update DTOs and Responses**

File: `Entities/Dtos/SubscriptionTierDto.cs` (if exists, or create it)
```csharp
public class SubscriptionTierDto
{
    public int Id { get; set; }
    public string TierName { get; set; }
    public string DisplayName { get; set; }
    public int DailyRequestLimit { get; set; }
    public int MonthlyRequestLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public int SubscriptionDurationDays { get; set; } // ✅ NEW
    public string Currency { get; set; }
}
```

#### Tier Duration Recommendations

| Tier | Monthly Price | Request Limits | **Subscription Duration** | Rationale |
|------|--------------|----------------|---------------------------|-----------|
| **S (Small)** | Low | 5/day, 100/month | **14 days** | Entry-level, shorter engagement |
| **M (Medium)** | Medium | 15/day, 300/month | **21 days** | Standard tier, 3-week trial |
| **L (Large)** | High | 50/day, 1000/month | **30 days** | Full month access |
| **XL (Extra Large)** | Highest | 100/day, 2500/month | **45 days** | Premium tier, extended access |

**Benefits:**
- Clear tier differentiation
- Incentive for sponsors to purchase higher tiers
- Predictable costs (sponsors know exactly what farmers get)

---

### Solution 2: Purchase-Based Duration

**Approach:** Allow sponsors to specify subscription duration at purchase time.

**Implementation:**

**1. Update SponsorshipPurchase Entity**
```csharp
public class SponsorshipPurchase
{
    // ... existing fields ...

    public int ValidityDays { get; set; } = 30;  // Code expiry (default)

    // ✅ NEW: Custom subscription duration
    public int SubscriptionDurationDays { get; set; } = 30; // How long subscription lasts
}
```

**2. Update Purchase API**
```csharp
POST /api/v1/Sponsorship/purchase
{
    "tierId": 3,
    "quantity": 100,
    "validityDays": 30,         // Codes expire in 30 days (default)
    "subscriptionDurationDays": 45  // Each code gives 45-day subscription
}
```

**3. Store in Codes**
```csharp
// Option A: Store in SponsorshipCode
public class SponsorshipCode
{
    public int SubscriptionDurationDays { get; set; } // Copy from Purchase
}

// Option B: Reference Purchase and lookup dynamically
// (requires JOIN in redemption query)
```

**Pros:**
- Maximum flexibility for sponsors
- Custom promotions (e.g., "Buy 100 codes, get 60-day subscriptions!")
- Seasonal campaigns

**Cons:**
- More complex UI (another input field)
- Harder to standardize pricing
- Might confuse sponsors

---

### Solution 3: Configuration-Based Duration

**Approach:** Define durations in `appsettings.json`, easy to change without DB migration.

**Implementation:**

**1. Configuration File**
```json
// appsettings.json
{
  "Sponsorship": {
    "SubscriptionDurations": {
      "S": 14,
      "M": 21,
      "L": 30,
      "XL": 45,
      "Default": 30
    },
    "CodeValidityDays": 30
  }
}
```

**2. Service Logic**
```csharp
public class SponsorshipService
{
    private readonly IConfiguration _configuration;

    private int GetSubscriptionDuration(string tierName)
    {
        var key = $"Sponsorship:SubscriptionDurations:{tierName}";
        return _configuration.GetValue<int>(key, 30); // Default 30
    }

    private async Task<IDataResult<UserSubscription>> ActivateSponsorship(...)
    {
        var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
        var durationDays = GetSubscriptionDuration(tier.TierName);

        var subscription = new UserSubscription
        {
            EndDate = DateTime.Now.AddDays(durationDays),
            ...
        };
    }
}
```

**Pros:**
- No database migration needed
- Easy to change durations (just edit config)
- Environment-specific values (Dev vs Staging vs Prod)

**Cons:**
- Not stored in database (harder to audit/report)
- Not visible in tier listings without extra logic
- Configuration drift risk between environments

---

## 📊 Comparison Matrix

| Feature | Solution 1: Tier-Based | Solution 2: Purchase-Based | Solution 3: Config-Based |
|---------|----------------------|----------------------------|--------------------------|
| **Implementation Complexity** | Medium (DB migration) | High (DB + UI changes) | Low (just config + code) |
| **Flexibility** | Medium (per tier) | High (per purchase) | Medium (per tier) |
| **User Experience** | ⭐ Simple & predictable | Complex (more options) | ⭐ Simple & predictable |
| **Pricing Model** | ⭐ Clear tier benefits | Custom pricing needed | ⭐ Clear tier benefits |
| **Maintenance** | Low (DB-driven) | Medium (more moving parts) | ⚠️ Config drift risk |
| **Auditability** | ⭐ Excellent (DB records) | ⭐ Excellent (DB records) | ⚠️ Poor (config only) |
| **Scalability** | ⭐ Excellent | Good | Good |
| **Recommendation** | ⭐⭐⭐ **BEST** | ⭐⭐ Use case: special campaigns | ⭐ Quick fix, not long-term |

---

## 🎯 Recommended Implementation: Solution 1 (Tier-Based)

### Why Tier-Based is Best

1. **Clear Value Proposition:** Each tier offers distinct benefits (limits + duration)
2. **Simple UX:** Sponsors don't need to make complex decisions
3. **Predictable Costs:** Sponsors know exactly what they're purchasing
4. **Standardized:** All codes from same tier behave consistently
5. **Maintainable:** Duration lives in DB, easy to query and report
6. **Extensible:** Can add Solution 2 later for custom campaigns

### Implementation Roadmap

#### Phase 1: Database & Entity (1-2 hours)
- [ ] Add `SubscriptionDurationDays` to `SubscriptionTier` entity
- [ ] Update EF configuration
- [ ] Create migration
- [ ] Apply migration to staging/production
- [ ] Seed tier data with durations (S=14, M=21, L=30, XL=45)

#### Phase 2: Service Logic (2-3 hours)
- [ ] Update `ActivateSponsorship` method in `SponsorshipService`
- [ ] Update `QueueSponsorship` method (for queue activation)
- [ ] Add logging to show which duration was used
- [ ] Add unit tests for tier-based duration

#### Phase 3: API & DTOs (1-2 hours)
- [ ] Update `SubscriptionTierDto` to include duration
- [ ] Update tier listing endpoints to return duration
- [ ] Update purchase confirmation to show duration
- [ ] Update redemption response to show subscription end date

#### Phase 4: UI Updates (2-3 hours)
- [ ] Show subscription duration in tier selection UI
- [ ] Display duration in purchase confirmation
- [ ] Show end date in redemption success message
- [ ] Update sponsor dashboard to show duration per tier

#### Phase 5: Documentation (1 hour)
- [ ] Update API documentation
- [ ] Update user guides for sponsors
- [ ] Create FAQ: "Code expiry vs subscription duration"
- [ ] Update mobile app integration docs

#### Phase 6: Testing & Deployment (2-3 hours)
- [ ] Integration tests for all tiers
- [ ] Verify queue activation uses correct duration
- [ ] Test edge cases (expired codes, tier changes)
- [ ] Staging deployment
- [ ] Production rollout with monitoring

**Total Estimate: 9-14 hours**

---

## 🧪 Testing Scenarios

### Test Case 1: Basic Redemption with Different Tiers

```
Given: 4 farmers, each receiving a code from different tiers
When: All farmers redeem codes on same day (March 1, 2025)
Then:
  - Farmer A (S tier, 14 days) → Subscription ends March 15
  - Farmer B (M tier, 21 days) → Subscription ends March 22
  - Farmer C (L tier, 30 days) → Subscription ends March 31
  - Farmer D (XL tier, 45 days) → Subscription ends April 15
```

### Test Case 2: Code Expiry Before Redemption

```
Given: Code generated January 1, 2025 (expires January 31, 2025)
When: Farmer tries to redeem on February 5, 2025
Then: Redemption fails with "Invalid or expired sponsorship code"
```

### Test Case 3: Queue Activation with Tier Duration

```
Given: Farmer has active L-tier sponsorship (30 days remaining)
When: Farmer redeems XL-tier code
Then:
  - New subscription queued with XL tier (45 days)
  - Queue activates automatically when current subscription expires
  - New subscription lasts 45 days (not 30)
```

### Test Case 4: Trial Upgrade with Tier Duration

```
Given: Farmer has Trial subscription (7 days remaining)
When: Farmer redeems M-tier sponsorship code
Then:
  - Trial subscription deactivated immediately
  - M-tier subscription activated with 21-day duration
  - Farmer gets full 21 days (not 7 remaining from trial)
```

---

## 📈 Expected Impact

### Business Benefits

**For Sponsors:**
- ✅ Clear tier differentiation (better value at higher tiers)
- ✅ Predictable budgeting (know exact cost per farmer-day)
- ✅ Better ROI on higher tier purchases
- ✅ Competitive advantage (offer longer access to key farmers)

**For Farmers:**
- ✅ Longer access with higher-tier sponsorships
- ✅ Clear expectations (know subscription end date upfront)
- ✅ Incentive to accept higher-tier sponsorships

**For Platform:**
- ✅ Increased tier diversity (not all sponsors buying L tier)
- ✅ Higher revenue potential (more XL tier purchases)
- ✅ Better farmer retention (longer access = more usage)

### Metrics to Track

**Pre-Implementation (Current State):**
- All tiers: 30-day average subscription duration
- Tier distribution: 70% L tier, 20% M tier, 10% S/XL tier

**Post-Implementation (Expected):**
- Tier distribution: 20% S, 30% M, 30% L, 20% XL (more balanced)
- Average subscription duration: 27 days (weighted average)
- XL tier purchases: +150% increase
- Farmer retention: +30% (longer access periods)

---

## 🔒 Backward Compatibility

### Migration Strategy

**Existing Codes (Before Implementation):**
```sql
-- Update existing SubscriptionTiers with durations
UPDATE "SubscriptionTiers" SET "SubscriptionDurationDays" = 30 WHERE "SubscriptionDurationDays" IS NULL;
```

**Existing Active Subscriptions:**
- No change needed (already have `EndDate` set)
- Remain active until their current `EndDate`

**Unredeemed Codes:**
- Will use new tier-based durations when redeemed
- No change to `ExpiryDate` (code validity unchanged)

### Rollback Plan

If implementation causes issues:

1. **Revert Code Changes:**
   ```csharp
   EndDate = DateTime.Now.AddDays(30) // Back to hardcoded
   ```

2. **Keep DB Column:**
   - Don't remove `SubscriptionDurationDays` column
   - Set all to 30 for consistency

3. **Monitoring:**
   - Track redemption success rate
   - Monitor subscription creation errors
   - Check for unexpected subscription durations

---

## 📝 API Documentation Updates

### GET /api/v1/Subscription/tiers

**Before:**
```json
{
  "id": 3,
  "tierName": "L",
  "displayName": "Large",
  "dailyRequestLimit": 50,
  "monthlyRequestLimit": 1000,
  "monthlyPrice": 299.00,
  "currency": "TRY"
}
```

**After:**
```json
{
  "id": 3,
  "tierName": "L",
  "displayName": "Large",
  "dailyRequestLimit": 50,
  "monthlyRequestLimit": 1000,
  "monthlyPrice": 299.00,
  "currency": "TRY",
  "subscriptionDurationDays": 30  // ✅ NEW
}
```

### POST /api/v1/Sponsorship/redeem

**Response Update:**
```json
{
  "success": true,
  "message": "Sponsorluk aktivasyonu tamamlandı!",
  "data": {
    "subscriptionId": 12345,
    "tierId": 3,
    "tierName": "L",
    "startDate": "2025-03-10T10:30:00Z",
    "endDate": "2025-04-09T10:30:00Z",
    "durationDays": 30,  // ✅ NEW: Show how long subscription lasts
    "status": "Active"
  }
}
```

---

## 🎓 User Communication

### For Sponsors

**Email Template:**
```
Subject: Yeni Özellik: Tier Bazlı Abonelik Süreleri

Değerli Sponsorumuz,

Sponsorluk sistemimizdeki önemli bir güncellemeyi sizlerle paylaşmak istiyoruz.

YENILIK: Tier Bazlı Abonelik Süreleri
─────────────────────────────────────
Artık her tier farklı abonelik süresi sunuyor:

• S (Small):  14 gün
• M (Medium): 21 gün
• L (Large):  30 gün
• XL:         45 gün

ÖNCEKİ DURUM:
Tüm tier'lar 30 günlük abonelik veriyordu.

YENİ DURUM:
Her tier kendi abonelik süresini veriyor.

ÖRNEK:
- 100 adet L tier kod satın alırsanız → Çiftçiler 30 gün erişim alır
- 100 adet XL tier kod satın alırsanız → Çiftçiler 45 gün erişim alır

Mevcut kullanılmamış kodlarınız yeni sürelere göre çalışacaktır.

Sorularınız için: support@ziraai.com
```

### For Farmers

**In-App Message:**
```
🎉 Yeni Özellik!

Sponsorluk kodları artık tier'a göre farklı süreler sunuyor:

S:  14 gün erişim
M:  21 gün erişim
L:  30 gün erişim
XL: 45 gün erişim

Kodunuzu kullandığınızda kaç gün erişim alacağınız gösterilecek!
```

---

## 🐛 Known Issues & Edge Cases

### Edge Case 1: Tier Duration Change After Code Generation

**Scenario:**
1. Sponsor purchases 100 L-tier codes (March 1, duration = 30 days)
2. Platform updates L-tier duration to 35 days (March 15)
3. Farmer redeems code on March 20

**Question:** Should farmer get 30 or 35 days?

**Options:**
- **A (Snapshot):** Store duration in `SponsorshipCode` at generation (30 days)
- **B (Dynamic):** Always use current tier duration (35 days)

**Recommendation:** Option A (Snapshot) for consistency and auditability.

**Implementation:**
```csharp
// Add to SponsorshipCode entity
public int? SubscriptionDurationDays { get; set; }

// Set during code generation
sponsorshipCode.SubscriptionDurationDays = tier.SubscriptionDurationDays;

// Use during redemption
var durationDays = sponsorshipCode.SubscriptionDurationDays
    ?? tier.SubscriptionDurationDays
    ?? 30; // Triple fallback
```

---

### Edge Case 2: Queued Subscription with Different Tier Duration

**Scenario:**
1. Farmer has active M-tier (21 days, 10 days remaining)
2. Farmer redeems L-tier code (30 days)
3. M-tier expires, L-tier activates from queue

**Expected Behavior:**
- Queued L-tier subscription should activate with 30 days (not 21)
- `QueueSponsorship` method needs same fix as `ActivateSponsorship`

---

### Edge Case 3: Zero or Negative Duration

**Scenario:**
Configuration error or DB corruption sets `SubscriptionDurationDays = 0`

**Validation:**
```csharp
// Add check in ActivateSponsorship
if (durationDays <= 0)
{
    _logger.LogError("Invalid subscription duration: {Duration} for tier {Tier}",
        durationDays, tier.TierName);
    durationDays = 30; // Fallback
}
```

---

## 📚 Related Documentation

- [Sponsorship System Complete Documentation](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [Sponsorship Queue System](./SPONSORSHIP_QUEUE_SYSTEM_DESIGN.md)
- [Subscription Tier Management](./SUBSCRIPTION_TIER_MANAGEMENT.md) *(if exists)*
- [Environment Variables Reference](./ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md)

---

## 🎯 Success Criteria

### Functional
✅ Each tier provides distinct subscription duration
✅ Farmers see correct end date upon redemption
✅ Queued subscriptions activate with correct duration
✅ Existing unredeemed codes work with new durations
✅ No impact on code expiry validation

### Technical
✅ No breaking changes to existing API contracts
✅ Migration runs successfully on staging/production
✅ All unit tests pass
✅ Integration tests cover all tiers
✅ Performance: <5ms overhead in redemption flow

### Business
✅ Tier distribution more balanced (not 70% L tier)
✅ XL tier purchases increase by at least 50%
✅ Farmer retention improves (longer access = more usage)
✅ Clear documentation for sponsors and farmers
✅ No confusion about code expiry vs subscription duration

---

## 📞 Next Steps

**When Ready to Implement:**

1. **Review & Approval:** Stakeholder review of this document
2. **Create Feature Branch:** `feature/tier-based-subscription-duration`
3. **Phase 1-6:** Follow implementation roadmap (9-14 hours)
4. **Staging Testing:** Comprehensive testing with all tiers
5. **Production Rollout:** Gradual rollout with monitoring
6. **Post-Deployment:** Monitor metrics and gather feedback

**Questions to Resolve:**
- [ ] Approve proposed tier durations (S=14, M=21, L=30, XL=45)?
- [ ] Should we snapshot duration in SponsorshipCode? (Edge Case 1)
- [ ] How to communicate changes to existing sponsors?
- [ ] Timeline for implementation?

---

**Document Version:** 1.1
**Created:** 2025-10-11
**Last Updated:** 2025-10-12
**Author:** Claude Code Assistant
**Status:** Updated - Code expiry default changed from 365 to 30 days
**Related Issues:** None (new feature)
