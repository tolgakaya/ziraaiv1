# Affected Endpoints by Missing Tier Features

**Investigation Date:** 2025-10-26
**Root Cause:** TierFeatures table missing feature mappings

---

## Critical Issue: data_access_percentage

### Affected Endpoints (HIGH PRIORITY)

#### 1. GET /api/v1/sponsorship/analyses
**File:** `Business/Handlers/PlantAnalyses/Queries/GetSponsoredAnalysesListQuery.cs:104`
**Status:** 🔴 BROKEN - Confirmed
**Impact:**
- Response missing 9 critical fields
- tierName showing "Unknown"
- accessPercentage showing 0
- canMessage showing false
- Mobile app list view broken

**Fix:** Run FIX_DATA_ACCESS_FEATURE.sql

---

#### 2. GET /api/v1/sponsorship/analyses/{id}
**File:** `Business/Handlers/PlantAnalyses/Queries/GetPlantAnalysisDetailQuery.cs`
**Status:** 🔴 LIKELY BROKEN
**Impact:**
- Analysis detail view may be incomplete
- May hide premium fields from L/XL tier sponsors
- May show "Unknown" tier
- Needs verification

**Test After Fix:**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analyses/76
```

**Expected Fields:**
- overallHealthScore
- plantSpecies
- plantVariety
- growthStage
- imageUrl
- vigorScore
- healthSeverity
- primaryConcern
- recommendations (if XL tier)
- farmerInfo (if XL tier)

---

#### 3. GET /api/v1/sponsorship/filtered-analysis
**File:** `Business/Handlers/PlantAnalyses/Queries/GetFilteredAnalysisForSponsorQuery.cs`
**Status:** 🔴 LIKELY BROKEN
**Impact:**
- Filtered analysis results incomplete
- Same field mapping issues as list endpoint
- Needs verification

---

## Other Tier Features (Need Verification)

### Features Using TierFeatureService

| Feature Key | Services Using It | Database Status | Impact if Missing |
|-------------|-------------------|-----------------|-------------------|
| `messaging` | AnalysisMessagingService | ❓ Unknown | Messaging blocked for L/XL |
| `smart_links` | SmartLinkService | ❓ Unknown | Smart links blocked for XL |
| `sponsor_visibility` | SponsorVisibilityService, FarmerProfileVisibilityService | ❓ Unknown | Logo/profile hidden |
| `data_access_percentage` | SponsorDataAccessService | ❌ **MISSING** | Analysis fields hidden |

---

## Verification SQL for ALL Features

```sql
-- Check which features are mapped in TierFeatures
SELECT
    f."FeatureKey",
    COUNT(tf."Id") as "MappingCount"
FROM public."Features" f
LEFT JOIN public."TierFeatures" tf ON f."Id" = tf."FeatureId"
GROUP BY f."FeatureKey"
ORDER BY f."FeatureKey";
```

**Expected Result:**
```
FeatureKey              | MappingCount
------------------------+--------------
messaging               | 2            (L, XL)
smart_links             | 1            (XL)
sponsor_visibility      | 3            (M, L, XL)
data_access_percentage  | 4            (S, M, L, XL)
voice_messages          | 1            (XL)
advanced_analytics      | 3            (M, L, XL)
api_access              | 2            (L, XL)
priority_support        | 2            (L, XL)
```

**If MappingCount = 0:** Feature exists but NOT mapped → Service returns false/null

---

## Complete Verification Checklist

### Step 1: Database Verification
- [ ] Run CHECK_DATA_ACCESS_FEATURE.sql
- [ ] Run verification SQL above for ALL features
- [ ] Document which features are missing

### Step 2: Apply Fixes
- [ ] Run FIX_DATA_ACCESS_FEATURE.sql (if data_access_percentage missing)
- [ ] Run migration SQL from TIER_FEATURE_MIGRATION_GUIDE.md (if others missing)
- [ ] Verify inserts successful

### Step 3: Service Restart
- [ ] Restart WebAPI on Railway (or wait 15 min for cache expiry)
- [ ] Verify service restarted successfully

### Step 4: Endpoint Testing

**Primary Endpoint (List):**
```bash
# Test analyses list
curl -H "Authorization: Bearer $TOKEN" \
  "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analyses?pageSize=10" \
  | jq '.data.items[0] | {tierName, accessPercentage, canMessage, overallHealthScore, imageUrl}'
```

**Expected:**
```json
{
  "tierName": "L",
  "accessPercentage": 100,
  "canMessage": true,
  "overallHealthScore": 6,
  "imageUrl": "https://iili.io/K4azIa9.jpg"
}
```

**Detail Endpoint:**
```bash
# Test single analysis detail
curl -H "Authorization: Bearer $TOKEN" \
  "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analyses/76" \
  | jq '.data | {tierName, accessPercentage, recommendations}'
```

**Messaging Endpoint:**
```bash
# Test messaging permission
curl -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"analysisId": 76, "message": "Test"}' \
  "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/messages"
```

### Step 5: Feature Verification

Test each tier feature service:

- [ ] Messaging (L, XL tiers)
- [ ] Smart Links (XL tier only)
- [ ] Sponsor Visibility (M, L, XL tiers)
- [ ] Data Access (S=30%, M=60%, L=100%, XL=100%)

---

## Files Modified by Tier Refactoring

**Original Refactoring Commit:** `97112c3` (2025-01-26)

**Services Modified:**
1. `Business/Services/Sponsorship/AnalysisMessagingService.cs`
2. `Business/Services/Sponsorship/FarmerProfileVisibilityService.cs`
3. `Business/Services/Sponsorship/SmartLinkService.cs`
4. `Business/Services/Sponsorship/SponsorDataAccessService.cs`
5. `Business/Services/Sponsorship/SponsorVisibilityService.cs`

**All of these depend on TierFeatures table having correct mappings!**

---

## Migration Files Location

**Primary Migration:**
`DataAccess/Migrations/Pg/Manual/AddTierFeatureManagementSystem.sql`

**Documentation:**
`claudedocs/TIER_FEATURE_MIGRATION_GUIDE.md`

**Operations Guide:**
`claudedocs/TIER_FEATURE_MANAGEMENT_OPERATIONS.md`

---

## Summary

**Total Affected Endpoints:** At least 3 confirmed
**Root Cause:** Database migration not run
**Fix:** SQL scripts ready, waiting for execution
**Testing:** Comprehensive test plan prepared

**Next Action:** User must run CHECK_DATA_ACCESS_FEATURE.sql and report results
