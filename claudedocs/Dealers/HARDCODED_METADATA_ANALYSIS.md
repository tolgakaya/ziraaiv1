# Hardcoded sponsorshipMetadata Analysis

**Endpoint:** `GET /api/v1/PlantAnalyses/{id}/detail`
**File:** `Business/Handlers/PlantAnalyses/Queries/GetPlantAnalysisDetailQuery.cs`
**Date:** 2025-10-27

---

## Current Hardcoded Values

### ❌ Problem: All values are hardcoded for farmer view

```csharp
detailDto.SponsorshipMetadata = new AnalysisTierMetadata
{
    TierName = "Standard", // ❌ HARDCODED - Should reflect actual tier
    AccessPercentage = 100, // ❌ HARDCODED - Always 100%
    CanMessage = true, // ❌ HARDCODED - Should check tier features
    CanReply = canReply, // ✅ DYNAMIC (just fixed!)
    CanViewLogo = true, // ❌ HARDCODED - Should check tier features
    SponsorInfo = sponsorProfile != null ? new SponsorDisplayInfoDto
    {
        SponsorId = sponsorProfile.SponsorId,
        CompanyName = sponsorProfile.CompanyName,
        LogoUrl = sponsorProfile.SponsorLogoUrl,
        WebsiteUrl = sponsorProfile.WebsiteUrl
    } : null,
    AccessibleFields = new AccessibleFieldsInfo
    {
        // ❌ ALL HARDCODED TO TRUE - Should be based on actual permissions
        CanViewBasicInfo = true,
        CanViewHealthScore = true,
        CanViewImages = true,
        CanViewDetailedHealth = true,
        CanViewDiseases = true,
        CanViewNutrients = true,
        CanViewRecommendations = true,
        CanViewLocation = true,
        CanViewFarmerContact = true,
        CanViewFieldData = true,
        CanViewProcessingData = true
    }
};
```

---

## Issues Identified

### 1. TierName = "Standard" (Hardcoded)

**Current:** Always returns "Standard"
**Should:** Return actual tier name from subscription

```csharp
// WRONG
TierName = "Standard"

// CORRECT
TierName = subscriptionTier.TierName // e.g., "L", "XL", "Trial", etc.
```

**Impact:**
- Farmer doesn't know which tier their analysis used
- Mobile app can't show accurate tier information

---

### 2. AccessPercentage = 100 (Hardcoded)

**Current:** Always 100%
**Should:** This might be intentional for farmer view (farmer sees their own data 100%)

**Question:** Is this correct? Or should this reflect sponsor's access level to farmer's data?

**Context from architecture:**
- If this is "farmer's view of their own data" → 100% is correct
- If this is "what sponsor can access" → should be tier-based

**Recommendation:** Keep as 100% if it means "farmer sees all their own data"

---

### 3. CanMessage = true (Hardcoded)

**Current:** Always true
**Should:** Check if tier has "messaging" feature

**Issue:** Not all tiers have messaging!
- Trial: ❌ No messaging
- S: ❌ No messaging
- M: ❌ No messaging
- L: ✅ Has messaging
- XL: ✅ Has messaging

**Correct Logic:**
```csharp
// Get tier from analysis → subscription
var subscription = await _userSubscriptionRepository.GetAsync(
    us => us.Id == analysis.ActiveSponsorshipId.Value);

// Check if tier has messaging feature
bool canMessage = await _tierFeatureService.HasFeatureAccessAsync(
    subscription.SubscriptionTierId,
    "messaging");
```

**Impact:**
- Farmer sees "message sponsor" button even when tier doesn't support it
- Clicking button will fail with tier validation error

---

### 4. CanViewLogo = true (Hardcoded)

**Current:** Always true
**Should:** Check if tier has "sponsor_visibility" feature

**Issue:**
- Trial, S tiers: ❌ No sponsor visibility
- M, L, XL: ✅ Has sponsor visibility

**Correct Logic:**
```csharp
bool canViewLogo = await _tierFeatureService.HasFeatureAccessAsync(
    subscription.SubscriptionTierId,
    "sponsor_visibility");
```

---

### 5. AccessibleFields (All Hardcoded to True)

**Current:** All 11 fields hardcoded to `true`

**Analysis:** This section seems intentionally farmer-centric:
- "Farmer sees all their own analysis fields"
- Comment suggests this is by design

**Question:** What is the purpose of these fields?

**Scenario A: Farmer's View (Current)**
- Farmer viewing their own analysis → all true (correct)
- These fields don't restrict farmer's access to their own data

**Scenario B: Sponsor's View (Different endpoint)**
- Sponsor viewing farmer's analysis → should be tier-based
- But this endpoint is for farmer detail view, not sponsor view

**Recommendation:**
- If this endpoint is ONLY for farmers viewing their own analyses → Keep as all true
- If sponsors also use this endpoint → Need tier-based logic

**Note:** There's a separate query for sponsor view: `GetFilteredAnalysisForSponsorQuery`
- Sponsor view has proper tier-based filtering
- This endpoint seems farmer-only

---

## Architecture Context

### Current Comment in Code:
```csharp
// 🎯 Populate sponsorship metadata if analysis was done with sponsorship code
// For farmer view: Show sponsor info without tier restrictions
// For sponsor view: GetFilteredAnalysisForSponsorQuery adds full tier metadata
```

**This suggests:**
- This endpoint is for FARMER viewing their own analysis
- Farmer should see all their own data (makes sense)
- Sponsor's tier-based restrictions are handled in a different endpoint

---

## Decision Matrix

| Field | Current | Is It Wrong? | Fix Priority | Reason |
|-------|---------|-------------|--------------|---------|
| TierName | "Standard" | ✅ YES | 🔴 HIGH | Should show actual tier |
| AccessPercentage | 100 | ❓ MAYBE | 🟡 LOW | Correct if "farmer's view" |
| CanMessage | true | ✅ YES | 🔴 HIGH | Should check tier feature |
| CanReply | dynamic | ✅ FIXED | ✅ DONE | Already fixed! |
| CanViewLogo | true | ✅ YES | 🟡 MEDIUM | Should check tier feature |
| AccessibleFields | all true | ❓ MAYBE | 🟢 N/A | Correct if farmer-only endpoint |

---

## Recommended Fixes

### Priority 1: TierName (High Impact)

```csharp
// Get subscription to find tier
var subscription = await _userSubscriptionRepository.GetAsync(
    us => us.Id == analysis.ActiveSponsorshipId.Value);

if (subscription != null)
{
    var tier = await _subscriptionTierRepository.GetAsync(
        t => t.Id == subscription.SubscriptionTierId);

    detailDto.SponsorshipMetadata = new AnalysisTierMetadata
    {
        TierName = tier?.TierName ?? "Unknown",
        // ...
    };
}
```

### Priority 2: CanMessage (High Impact)

```csharp
// Check if tier has messaging feature
bool canMessage = false;
if (subscription != null)
{
    canMessage = await _tierFeatureService.HasFeatureAccessAsync(
        subscription.SubscriptionTierId,
        "messaging");
}

detailDto.SponsorshipMetadata = new AnalysisTierMetadata
{
    CanMessage = canMessage,
    // ...
};
```

### Priority 3: CanViewLogo (Medium Impact)

```csharp
// Check if tier has sponsor_visibility feature
bool canViewLogo = false;
if (subscription != null)
{
    canViewLogo = await _tierFeatureService.HasFeatureAccessAsync(
        subscription.SubscriptionTierId,
        "sponsor_visibility");
}

detailDto.SponsorshipMetadata = new AnalysisTierMetadata
{
    CanViewLogo = canViewLogo,
    // ...
};
```

---

## Questions to Answer

1. **AccessPercentage = 100:**
   - Is this "farmer sees 100% of their own data"? → Keep as 100
   - Or "sponsor can access 100% of farmer data"? → Make tier-based

2. **AccessibleFields all true:**
   - Is this endpoint ONLY for farmers? → Keep all true
   - Can sponsors also call this endpoint? → Make tier-based

3. **Who uses this endpoint?**
   - Only farmers viewing their own analyses?
   - Or both farmers and sponsors?

---

## Complete Fixed Version (Recommended)

```csharp
if (analysis.SponsorUserId.HasValue && analysis.SponsorshipCodeId.HasValue)
{
    try
    {
        var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(
            analysis.SponsorUserId.Value);

        // Get subscription and tier information
        var subscription = await _userSubscriptionRepository.GetAsync(
            us => us.Id == analysis.ActiveSponsorshipId.Value);

        if (subscription != null)
        {
            var tier = await _subscriptionTierRepository.GetAsync(
                t => t.Id == subscription.SubscriptionTierId);

            // Check tier features dynamically
            bool canMessage = await _tierFeatureService.HasFeatureAccessAsync(
                subscription.SubscriptionTierId,
                "messaging");

            bool canViewLogo = await _tierFeatureService.HasFeatureAccessAsync(
                subscription.SubscriptionTierId,
                "sponsor_visibility");

            bool canReply = await _analysisMessageRepository.HasSponsorMessagedAnalysisAsync(
                analysis.Id,
                analysis.SponsorUserId.Value);

            detailDto.SponsorshipMetadata = new AnalysisTierMetadata
            {
                TierName = tier?.DisplayName ?? tier?.TierName ?? "Unknown", // ✅ Dynamic
                AccessPercentage = 100, // ✅ Keep - farmer sees all their own data
                CanMessage = canMessage, // ✅ Dynamic - tier-based
                CanReply = canReply, // ✅ Already dynamic
                CanViewLogo = canViewLogo, // ✅ Dynamic - tier-based
                SponsorInfo = sponsorProfile != null ? new SponsorDisplayInfoDto
                {
                    SponsorId = sponsorProfile.SponsorId,
                    CompanyName = sponsorProfile.CompanyName,
                    LogoUrl = sponsorProfile.SponsorLogoUrl,
                    WebsiteUrl = sponsorProfile.WebsiteUrl
                } : null,
                AccessibleFields = new AccessibleFieldsInfo
                {
                    // ✅ Keep all true - farmer sees all their own analysis fields
                    CanViewBasicInfo = true,
                    CanViewHealthScore = true,
                    CanViewImages = true,
                    CanViewDetailedHealth = true,
                    CanViewDiseases = true,
                    CanViewNutrients = true,
                    CanViewRecommendations = true,
                    CanViewLocation = true,
                    CanViewFarmerContact = true,
                    CanViewFieldData = true,
                    CanViewProcessingData = true
                }
            };
        }
    }
    catch (Exception ex)
    {
        // Log but don't fail if sponsorship metadata fetch fails
        Console.WriteLine($"[GetPlantAnalysisDetailQuery] Warning: Could not fetch sponsorship metadata: {ex.Message}");
        detailDto.SponsorshipMetadata = null;
    }
}
```

---

## Dependencies Needed

Add these to the handler constructor if not already present:

```csharp
private readonly ISubscriptionTierRepository _subscriptionTierRepository;
private readonly ITierFeatureService _tierFeatureService;
private readonly IUserSubscriptionRepository _userSubscriptionRepository; // Already added for canReply

public GetPlantAnalysisDetailQueryHandler(
    IPlantAnalysisRepository plantAnalysisRepository,
    ISponsorDataAccessService dataAccessService,
    ISponsorProfileRepository sponsorProfileRepository,
    IAnalysisMessageRepository analysisMessageRepository,
    IUserSubscriptionRepository userSubscriptionRepository, // ✅ Already added
    ISubscriptionTierRepository subscriptionTierRepository, // ➕ Need to add
    ITierFeatureService tierFeatureService) // ➕ Need to add
{
    // ...
}
```

---

## Testing Scenarios

### Test 1: Trial Tier Analysis
```json
{
  "tierName": "Trial",
  "canMessage": false,  // ❌ Trial has no messaging
  "canViewLogo": false, // ❌ Trial has no sponsor visibility
  "canReply": false     // ❌ Sponsor hasn't messaged
}
```

### Test 2: M Tier Analysis
```json
{
  "tierName": "M",
  "canMessage": false,  // ❌ M has no messaging
  "canViewLogo": true,  // ✅ M has sponsor visibility
  "canReply": false     // ❌ Sponsor hasn't messaged
}
```

### Test 3: L Tier Analysis (Sponsor Messaged)
```json
{
  "tierName": "L",
  "canMessage": true,   // ✅ L has messaging
  "canViewLogo": true,  // ✅ L has sponsor visibility
  "canReply": true      // ✅ Sponsor has messaged
}
```

### Test 4: XL Tier Analysis
```json
{
  "tierName": "XL",
  "canMessage": true,   // ✅ XL has messaging
  "canViewLogo": true,  // ✅ XL has sponsor visibility
  "canReply": false     // ❌ Sponsor hasn't messaged yet
}
```

---

## Summary

**Hardcoded Values Found:**
1. ✅ **TierName** - Should be dynamic (actual tier name)
2. ❓ **AccessPercentage** - Probably correct at 100%
3. ✅ **CanMessage** - Should check tier features
4. ✅ **CanViewLogo** - Should check tier features
5. ❓ **AccessibleFields** - Probably correct (all true for farmer)

**Action Required:**
- Fix TierName, CanMessage, CanViewLogo to be tier-based
- Confirm purpose of AccessPercentage and AccessibleFields
- Add required dependencies (repositories/services)

**Estimated Time:** 2-3 hours

---

**Related Files:**
- `Business/Handlers/PlantAnalyses/Queries/GetPlantAnalysisDetailQuery.cs`
- `Business/Services/Sponsorship/TierFeatureService.cs`
- `DataAccess/Abstract/ISubscriptionTierRepository.cs`
- `DataAccess/Abstract/IUserSubscriptionRepository.cs`
