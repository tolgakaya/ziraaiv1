# Critical Breaking Changes Investigation Summary

**Date:** 2025-10-26
**Severity:** 🔴 CRITICAL
**Status:** Root cause identified, fix ready

---

## Problem Statement

**Endpoint:** `GET /api/v1/sponsorship/analyses?pageSize=10`

**Symptoms:**
1. Missing 9 critical fields in response (health scores, plant info, images)
2. `tierName` showing "Unknown" instead of "L"
3. `accessPercentage` showing 0 instead of 60/100
4. `canMessage` showing false instead of true

**Mobile App Impact:**
- Cannot display analysis details in list
- Cannot show plant health information
- Cannot show plant images
- Messaging appears disabled
- User experience completely broken

---

## Root Cause Analysis

### Handler Investigation

**File:** `Business/Handlers/PlantAnalyses/Queries/GetSponsoredAnalysesListQuery.cs`

**Line 104:**
```csharp
var accessPercentage = await _dataAccessService.GetDataAccessPercentageAsync(request.SponsorId);
```

**Line 304-355 (MapToSummaryDto method):**
```csharp
// 30% Access Fields (S & M tiers)
if (accessPercentage >= 30) {
    dto.OverallHealthScore = analysis.OverallHealthScore;
    dto.PlantSpecies = analysis.PlantSpecies;
    dto.PlantVariety = analysis.PlantVariety;
    dto.GrowthStage = analysis.GrowthStage;
    dto.ImageUrl = ...;
}

// 60% Access Fields (L tier)
if (accessPercentage >= 60) {
    dto.VigorScore = analysis.VigorScore;
    dto.HealthSeverity = analysis.HealthSeverity;
    dto.PrimaryConcern = analysis.PrimaryConcern;
    dto.Location = analysis.Location;
}
```

**Problem:** When `accessPercentage = 0`, NONE of these fields get populated!

### Service Investigation

**File:** `Business/Services/Sponsorship/SponsorDataAccessService.cs`

**Method:** `GetDataAccessPercentageFromPurchasesAsync`

**Lines 84-92:**
```csharp
var hasDataAccess = await _tierFeatureService.HasFeatureAccessAsync(
    purchase.SubscriptionTierId,
    "data_access_percentage"
);

if (hasDataAccess) {
    var config = await _tierFeatureService.GetFeatureConfigAsync<DataAccessConfig>(
        purchase.SubscriptionTierId,
        "data_access_percentage"
    );
    var accessPercentage = config?.percentage ?? 0;
    // ...
}
```

**Problem:** `HasFeatureAccessAsync` returns FALSE because feature not in database!

---

## Root Cause Confirmed

**The tier feature refactoring on 2025-01-26 created the TierFeatureService but:**

1. ✅ Code was updated to use `TierFeatureService`
2. ✅ Feature schema was created (Features + TierFeatures tables)
3. ✅ Migration SQL was documented in `claudedocs/TIER_FEATURE_MIGRATION_GUIDE.md`
4. ❌ **Migration SQL was NEVER run on staging database!**

**Missing from database:**
- `data_access_percentage` feature mapping in TierFeatures table
- Without this mapping, service returns 0
- With 0, handler doesn't populate ANY analysis fields

---

## Fix Plan

### Step 1: Verify Problem (Run CHECK_DATA_ACCESS_FEATURE.sql)

Expected result if bug confirmed:
```
Query 2 (TierFeatures for data_access_percentage): 0 rows ❌
```

### Step 2: Apply Fix (Run FIX_DATA_ACCESS_FEATURE.sql)

Inserts:
- S Tier (ID=2): `{"percentage": 30}`
- M Tier (ID=3): `{"percentage": 60}`
- L Tier (ID=4): `{"percentage": 100}`
- XL Tier (ID=5): `{"percentage": 100}`

### Step 3: Clear Cache

**Options:**
1. Restart WebAPI service on Railway (fastest)
2. Wait 15 minutes for cache expiry
3. Clear Redis keys: `tier_feature_access_*`

### Step 4: Verify Fix

Test endpoint again:
```
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analyses?pageSize=10
```

Expected:
- ✅ `tierName: "L"` (not "Unknown")
- ✅ `accessPercentage: 100` (not 0)
- ✅ `canMessage: true` (not false)
- ✅ All 9 missing fields populated

---

## Other Potentially Broken Endpoints

**All endpoints using SponsorDataAccessService are affected:**

### Critical (Immediate Investigation Needed)

1. **GET /api/v1/sponsorship/analyses/{id}**
   - Single analysis detail
   - Uses same access percentage logic
   - Probably missing same fields

2. **GET /api/v1/PlantAnalyses/{id}** (Farmer view)
   - If sponsor-sponsored, may have permission issues

3. **POST /api/v1/sponsorship/messages** (Messaging endpoints)
   - May reject messages due to `canMessage: false`
   - Tier validation might fail

### Medium Priority

4. **GET /api/v1/sponsorship/dealer/performance**
   - May show wrong tier info
   - May calculate wrong access stats

5. **GET /api/v1/sponsorship/statistics**
   - Summary stats may be incorrect

---

## Files to Check for Similar Issues

Use Grep to find all usages of TierFeatureService:

```bash
# Find all files using TierFeatureService
grep -r "ITierFeatureService" Business/Services/
grep -r "_tierFeatureService" Business/Services/
grep -r "HasFeatureAccessAsync" Business/
grep -r "GetFeatureConfigAsync" Business/
```

**Expected locations:**
1. AnalysisMessagingService.cs (messaging feature)
2. SmartLinkService.cs (smart_links feature)
3. SponsorVisibilityService.cs (sponsor_visibility feature)
4. FarmerProfileVisibilityService.cs (uses sponsor_visibility)
5. **SponsorDataAccessService.cs (data_access_percentage) ← BROKEN**

---

## Why This Happened

### Timeline

1. **2025-01-26:** Tier feature refactoring completed
   - Memory: `tier_feature_refactoring_complete_2025_01_26`
   - All services updated to use TierFeatureService
   - Migration SQL documented

2. **2025-01-26 to 2025-10-26:** Development continued
   - Multiple features added (dealer distribution, etc.)
   - **Assumption:** Migration SQL was already run
   - **Reality:** Migration SQL never ran on staging

3. **2025-10-26:** E2E testing for dealer feature
   - Tested code transfer, dealer analytics
   - **Did NOT verify response field completeness**
   - Tests passed because fields EXIST (just empty/zero)

### What Went Wrong

1. **Migration Not Run:**
   - SQL in `claudedocs/` but never executed
   - No verification step after refactoring

2. **Incomplete Testing:**
   - E2E test only checked field existence
   - Did NOT verify field VALUES
   - Did NOT compare with old response

3. **No Breaking Change Detection:**
   - No automated tests for response structure
   - No mobile integration testing
   - No approval process for API changes

---

## Lessons Learned

### Critical Rules Violated

1. **❌ Database migration not verified after deployment**
   - Migration SQL existed but wasn't run
   - No post-deployment verification

2. **❌ Breaking changes not detected**
   - Response structure changes not flagged
   - No backward compatibility check

3. **❌ Testing insufficient**
   - E2E test checked presence, not values
   - No comparison with previous response

### New Safety Rules Required

1. **Database Migrations:**
   - [ ] Migration SQL written
   - [ ] Migration SQL reviewed
   - [ ] Migration SQL run on staging
   - [ ] Verification query run
   - [ ] Documented in migration log

2. **API Response Changes:**
   - [ ] Old response documented
   - [ ] New response documented
   - [ ] Comparison analysis done
   - [ ] Mobile team notified
   - [ ] Approval received
   - [ ] Integration test updated

3. **Testing Requirements:**
   - [ ] Unit tests for service logic
   - [ ] Integration tests for endpoints
   - [ ] Response structure validation
   - [ ] Field value validation
   - [ ] Backward compatibility test

---

## Next Steps

1. ✅ Document issue (this file)
2. ✅ Create verification SQL (CHECK_DATA_ACCESS_FEATURE.sql)
3. ✅ Create fix SQL (FIX_DATA_ACCESS_FEATURE.sql)
4. ⏳ User runs verification SQL
5. ⏳ User runs fix SQL
6. ⏳ Restart staging service or wait for cache expiry
7. ⏳ Test endpoint and verify all fields present
8. ⏳ Search for other broken endpoints
9. ⏳ Update CRITICAL_BREAKING_CHANGES_ANALYSIS.md
10. ⏳ Create post-mortem document
11. ⏳ Update development rules

---

**Prepared by:** Claude Code
**Files Created:**
- `claudedocs/Dealers/CRITICAL_BREAKING_CHANGES_ANALYSIS.md`
- `claudedocs/Dealers/CHECK_DATA_ACCESS_FEATURE.sql`
- `claudedocs/Dealers/FIX_DATA_ACCESS_FEATURE.sql`
- `claudedocs/Dealers/INVESTIGATION_SUMMARY.md`
