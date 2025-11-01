# Tier Calculation Fix - Dealer Analysis Display

**Date:** 2025-10-27  
**Issue:** Dealer sees "Unknown" tier and canMessage=false for analyses from codes they distributed  
**Root Cause:** Both list and detail endpoints were using user's tier instead of analysis tier

---

## Problem Description

When dealer (ID 158) views analyses from codes they distributed:
- ❌ Tier shows as "Unknown" (should be "L")
- ❌ canMessage shows false (should be true for L tier)
- ✅ Sponsor (ID 159) sees correct tier ("L") for same analyses

**Critical Architectural Insight (from user):**
> "Hiçkimsenin Tier'ı yok. Dağıtılan kodun Tier'ı var. Doğal olarak analizin Tier'ı var."

Translation: **NO USER has a tier. The distributed CODE has a tier. Naturally, the ANALYSIS has a tier.**

---

## Root Cause Analysis

### Incorrect Approach (OLD)
Both endpoints used `GetSponsorHighestTierAsync()` which:
1. Looked at user's `SponsorProfile.SponsorSubscriptions`
2. Found user's purchased packages
3. Returned the highest tier from purchases

**Problem:** Dealer has no purchases → tier = "Unknown"

### Correct Architecture
Each analysis gets its tier from:
```
Analysis.ActiveSponsorshipId → UserSubscription → SubscriptionTier
```

- Analysis was created using a specific sponsorship code
- That code has a tier (e.g., "L", "XL", etc.)
- Tier belongs to the analysis, not to the viewing user
- Both sponsor AND dealer must retrieve tier from analysis

---

## Solution Implemented

### Step 1: Created `GetAnalysisUsedTierAsync` Method

Added to both query handlers:

```csharp
/// <summary>
/// Get the tier that was used for a specific analysis
/// This is the CORRECT approach - tier comes from the analysis, not from user
/// Analysis → ActiveSponsorshipId → UserSubscription → SubscriptionTier
/// </summary>
private async Task<Entities.Concrete.SubscriptionTier> GetAnalysisUsedTierAsync(
    Entities.Concrete.PlantAnalysis analysis)
{
    if (!analysis.ActiveSponsorshipId.HasValue)
        return null;

    var subscription = await _userSubscriptionRepository.GetAsync(
        s => s.Id == analysis.ActiveSponsorshipId.Value);

    if (subscription == null)
        return null;

    return await _subscriptionTierRepository.GetAsync(
        t => t.Id == subscription.SubscriptionTierId);
}
```

### Step 2: Updated List Endpoint

**File:** `Business/Handlers/PlantAnalyses/Queries/GetSponsoredAnalysesListQuery.cs`

**Changes:**
1. Added `IUserSubscriptionRepository` dependency
2. Removed `GetSponsorHighestTierAsync()` call
3. Converted `Select()` to `foreach` loop for async tier lookup:

```csharp
// OLD: Single tier for all analyses (wrong!)
var sponsorTier = await GetSponsorHighestTierAsync(sponsorProfile);
var items = pagedAnalyses.Select(analysis => MapToSummaryDto(analysis, sponsorProfile, sponsorTier));

// NEW: Individual tier per analysis (correct!)
var items = new List<SponsoredAnalysisSummaryDto>();
foreach (var analysis in pagedAnalyses)
{
    var analysisTier = await GetAnalysisUsedTierAsync(analysis);
    var dto = MapToSummaryDto(analysis, sponsorProfile, analysisTier);
    items.Add(dto);
}
```

### Step 3: Updated Detail Endpoint

**File:** `Business/Handlers/PlantAnalyses/Queries/GetFilteredAnalysisForSponsorQuery.cs`

**Changes:**
1. Added `IPlantAnalysisRepository` dependency (to fetch analysis entity)
2. Added `IUserSubscriptionRepository` dependency
3. Replaced tier lookup logic:

```csharp
// OLD: User's tier (wrong!)
var sponsorTier = await GetSponsorHighestTierAsync(sponsorProfile);
var tierName = sponsorTier?.TierName ?? "Unknown";

// NEW: Analysis's tier (correct!)
var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == request.PlantAnalysisId);
var analysisTier = await GetAnalysisUsedTierAsync(analysis);
var tierName = analysisTier?.TierName ?? "Unknown";
```

---

## Files Modified

### 1. GetSponsoredAnalysesListQuery.cs (List Endpoint)
- **Line ~30:** Added `IUserSubscriptionRepository _userSubscriptionRepository`
- **Line ~107:** Removed `GetSponsorHighestTierAsync(sponsorProfile)` call
- **Line ~210-260:** Converted `Select()` to `foreach` with `GetAnalysisUsedTierAsync(analysis)`
- **Line ~393-410:** Added new `GetAnalysisUsedTierAsync()` method

### 2. GetFilteredAnalysisForSponsorQuery.cs (Detail Endpoint)
- **Line ~25:** Added `IPlantAnalysisRepository _plantAnalysisRepository`
- **Line ~26:** Added `IUserSubscriptionRepository _userSubscriptionRepository`
- **Line ~88-90:** Changed to fetch analysis entity and call `GetAnalysisUsedTierAsync()`
- **Line ~152-169:** Added new `GetAnalysisUsedTierAsync()` method

---

## Expected Behavior After Fix

### Dealer View (ID 158)
```json
{
  "tierName": "L",           // ✅ Correct (from analysis tier)
  "canMessage": true,        // ✅ Correct (L tier allows messaging)
  "canViewLogo": true,       // ✅ Correct
  "sponsorInfo": {
    "sponsorId": 158,        // Dealer's ID
    "companyName": "uc tarim"
  }
}
```

### Sponsor View (ID 159)
```json
{
  "tierName": "L",           // ✅ Already correct
  "canMessage": true,        // ✅ Already correct
  "canViewLogo": true,       // ✅ Already correct
  "sponsorInfo": {
    "sponsorId": 159,        // Sponsor's ID
    "companyName": "dort tarim"
  }
}
```

---

## Testing Checklist

- [ ] Deploy to staging environment
- [ ] Test dealer (ID 158) list endpoint → verify tierName = "L"
- [ ] Test dealer (ID 158) detail endpoint → verify canMessage = true
- [ ] Test sponsor (ID 159) endpoints → verify still working correctly
- [ ] Test analyses with different tiers (S, M, L, XL)
- [ ] Test analyses without ActiveSponsorshipId → verify tierName = "Unknown"
- [ ] Verify all tier-based fields are dynamic (not hardcoded)

---

## Key Takeaways

1. **Tier belongs to analysis, not user** - Critical architectural principle
2. **Use ActiveSponsorshipId** - This FK links analysis to its tier
3. **No user has a tier** - Users distribute codes with tiers
4. **Same logic for sponsor AND dealer** - Both must get tier from analysis
5. **All tier fields must be dynamic** - No hardcoded values

---

## Related Documents

- [HASUNREAD_FILTER_BUG.md](./HASUNREAD_FILTER_BUG.md) - Previous fix for farmer filter
- [sponsor_analysis.json](./sponsor_analysis.json) - Correct response example
- [dealer_analysis.json](./dealer_analysis.json) - Response before fix

---

**Status:** ✅ FIXED  
**Build:** ✅ Succeeded  
**Ready for Testing:** ✅ Yes
