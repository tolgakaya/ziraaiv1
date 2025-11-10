# Non-Sponsored Analysis Detail Endpoint - Deployment Checklist

## Overview

This checklist guides the deployment of the non-sponsored analysis detail endpoint for admin view.

**Branch**: `enhancement-for-admin-operations`
**Commit**: `23adc17 - feat: Add non-sponsored analysis detail endpoint for admin view`
**Date**: 2025-11-10

---

## Changes Summary

### 1. New Query Handler: GetNonSponsoredAnalysisDetailQuery

**File**: `Business/Handlers/AdminSponsorship/Queries/GetNonSponsoredAnalysisDetailQuery.cs`

**Purpose**: Enable admin to view complete analysis details for non-sponsored farmers (same view as farmer sees)

**Key Features**:
- Verifies analysis is non-sponsored before returning details
- Delegates to existing `GetPlantAnalysisDetailQuery` for consistency
- Returns complete `PlantAnalysisDetailDto` with all analysis sections
- Includes SecuredOperation, PerformanceAspect, and LogAspect

**Logic**:
```csharp
// Verify analysis exists and is non-sponsored
var analysis = await _plantAnalysisRepository.GetAsync(p =>
    p.Id == request.PlantAnalysisId &&
    p.Status &&
    string.IsNullOrEmpty(p.SponsorId) &&
    p.SponsorshipCodeId == null &&
    p.SponsorUserId == null);

if (analysis == null)
{
    return new ErrorDataResult<PlantAnalysisDetailDto>("Non-sponsored analysis not found");
}

// Use existing farmer query for consistent view
var farmerQuery = new GetPlantAnalysisDetailQuery { Id = request.PlantAnalysisId };
var result = await _mediator.Send(farmerQuery, cancellationToken);
```

### 2. New Controller Endpoint

**File**: `WebAPI/Controllers/AdminSponsorshipController.cs` (Line 479-494)

**Endpoint**: `GET /api/admin/sponsorship/non-sponsored/analyses/{plantAnalysisId}`

**Code**:
```csharp
/// <summary>
/// Get detailed analysis information for a non-sponsored analysis
/// Admin sees the same view as farmer (full analysis details)
/// </summary>
/// <param name="plantAnalysisId">Plant analysis ID</param>
[HttpGet("non-sponsored/analyses/{plantAnalysisId}")]
public async Task<IActionResult> GetNonSponsoredAnalysisDetail(int plantAnalysisId)
{
    var query = new GetNonSponsoredAnalysisDetailQuery
    {
        PlantAnalysisId = plantAnalysisId
    };

    var result = await Mediator.Send(query);
    return GetResponse(result);
}
```

### 3. Database Operation Claim

**File**: `claudedocs/AdminOperations/ADD_ADMIN_NON_SPONSORED_ANALYSIS_DETAIL_CLAIM.sql`

**New Claim**:
- **ID**: 140
- **Name**: `GetNonSponsoredAnalysisDetailQuery`
- **Alias**: "Admin Non-Sponsored Analysis Detail View"
- **Description**: "Admin olarak sponsorsuz analiz detayı görüntüleme (farmer görünümü ile aynı)"
- **Assigned To**: Administrators group (GroupId = 1)

### 4. Documentation Updates

**Files**:
- `claudedocs/AdminOperations/ADMIN_NON_SPONSORED_ANALYSIS_DETAIL_API.md` - New comprehensive API documentation
- `claudedocs/ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md` - Updated table of contents and endpoint section

---

## Deployment Steps

### Step 1: Execute SQL Script on Staging Database ⏳

**Action Required**: Run the SQL script to create claim 140.

```bash
# Connect to Railway PostgreSQL staging database
psql -h [staging-host] -U [username] -d [database-name]

# Execute the script
\i claudedocs/AdminOperations/ADD_ADMIN_NON_SPONSORED_ANALYSIS_DETAIL_CLAIM.sql
```

**Expected Output**:
```
NOTICE:  Claim 140 (GetNonSponsoredAnalysisDetailQuery) added successfully
NOTICE:  Claim 140 granted to Administrators group
```

### Step 2: Verify Claim Created ✅

**Action Required**: Run verification query to confirm claim exists.

```sql
-- Verification Query
SELECT oc."Id", oc."Name", oc."Alias", oc."Description",
       CASE WHEN gc."GroupId" IS NOT NULL THEN 'YES - GroupId: ' || gc."GroupId"
            ELSE 'NO - MISSING' END as "HasGroupClaim"
FROM "OperationClaims" oc
LEFT JOIN "GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Id" = 140;
```

**Expected Result**:

| Id  | Name | Alias | Description | HasGroupClaim |
|-----|------|-------|-------------|---------------|
| 140 | GetNonSponsoredAnalysisDetailQuery | Admin Non-Sponsored Analysis Detail View | Admin olarak sponsorsuz analiz detayı görüntüleme (farmer görünümü ile aynı) | YES - GroupId: 1 |

### Step 3: Deploy Code Changes 🚀

**Action Required**: Deploy the updated code to staging.

```bash
# Code already pushed to remote
git checkout enhancement-for-admin-operations
git pull origin enhancement-for-admin-operations

# Deploy via Railway (automatic on push to branch linked to staging)
```

**Files Modified**:
- `Business/Handlers/AdminSponsorship/Queries/GetNonSponsoredAnalysisDetailQuery.cs` (NEW)
- `WebAPI/Controllers/AdminSponsorshipController.cs`
- `claudedocs/AdminOperations/ADMIN_NON_SPONSORED_ANALYSIS_DETAIL_API.md` (NEW)
- `claudedocs/ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md`

### Step 4: Clear Admin Claims Cache 🔄

**Action Required**: Admin user must logout and login to refresh claims cache.

**Why**: User claims are cached with key `CacheKeys.UserIdForClaim={userId}` for performance. New claim won't be available until cache is refreshed.

**Steps**:
1. Admin user logs out completely
2. Admin user logs back in
3. JWT token will include claim 140
4. Cache will be repopulated with new claim

### Step 5: Test Endpoint 🧪

**Action Required**: Verify endpoint works with admin user.

#### Test 1: Get Non-Sponsored Analysis Detail (Success)

```bash
# Get a non-sponsored analysis ID from the list endpoint first
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses?page=1&pageSize=1" \
  -H "Authorization: Bearer {admin-jwt-token}"

# Use the plantAnalysisId from the response
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses/5678" \
  -H "Authorization: Bearer {admin-jwt-token}"
```

**Expected**: `200 OK` with complete `PlantAnalysisDetailDto` response

**Response Should Include**:
- `id`, `analysisId`, `analysisDate`, `analysisStatus`
- `plantIdentification` (species, variety, growthStage)
- `healthAssessment` (vigorScore, severity, stressIndicators)
- `nutrientStatus` (all nutrient levels)
- `pestDisease` (detected pests and diseases)
- `summary` (overallHealthScore, primaryConcern, prognosis)
- `recommendations` (immediate, shortTerm, preventive)
- `imageInfo`, `processingInfo`
- `sponsorshipMetadata` should be `null`

#### Test 2: Try to Access Sponsored Analysis (Should Fail)

```bash
# Try to access a sponsored analysis (has SponsorId)
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses/{sponsored-analysis-id}" \
  -H "Authorization: Bearer {admin-jwt-token}"
```

**Expected**: `404 Not Found` with message "Non-sponsored analysis not found"

#### Test 3: Try Without Admin Token (Should Fail)

```bash
# Try with farmer token
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses/5678" \
  -H "Authorization: Bearer {farmer-jwt-token}"
```

**Expected**: `403 Forbidden` - User doesn't have claim 140

#### Test 4: Verify Same View as Farmer

```bash
# Get analysis as admin
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses/5678" \
  -H "Authorization: Bearer {admin-jwt-token}" > admin-view.json

# Get same analysis as farmer (if accessible)
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/plant-analyses/5678" \
  -H "Authorization: Bearer {farmer-jwt-token}" > farmer-view.json

# Compare responses (should be identical structure)
diff admin-view.json farmer-view.json
```

**Expected**: Responses should have same structure and data

---

## Rollback Plan

If issues occur after deployment:

### Revert Code Changes

```bash
git checkout enhancement-for-admin-operations
git revert 23adc17
git push origin enhancement-for-admin-operations
```

### Revert Database Changes (If Needed)

```sql
-- Remove claim 140 from group
DELETE FROM "GroupClaims" WHERE "ClaimId" = 140;

-- Remove claim 140
DELETE FROM "OperationClaims" WHERE "Id" = 140;
```

---

## Technical Notes

### SecuredOperation Aspect Mechanism

```csharp
// Aspect checks if user has claim with name matching handler class name (without "Handler" suffix)
[SecuredOperation(Priority = 1)]
public class GetNonSponsoredAnalysisDetailQueryHandler : IRequestHandler<...>
{
    // Aspect will check for claim: "GetNonSponsoredAnalysisDetailQuery"
}
```

### Query Reuse Pattern

The handler delegates to the existing farmer query for consistency:

```csharp
// Reuses farmer's query to ensure identical view
var farmerQuery = new GetPlantAnalysisDetailQuery { Id = request.PlantAnalysisId };
var result = await _mediator.Send(farmerQuery, cancellationToken);
```

**Benefits**:
- No code duplication
- Guaranteed consistency with farmer view
- Automatic updates if farmer view changes
- Maintains single source of truth

### Data Verification

Before returning details, the handler verifies the analysis is truly non-sponsored:

```csharp
p.Status &&                          // Active analysis
string.IsNullOrEmpty(p.SponsorId) && // No sponsor ID
p.SponsorshipCodeId == null &&       // No sponsorship code
p.SponsorUserId == null              // No sponsor user
```

### Claims Cache Key

```csharp
// Cache key format used by SecuredOperation aspect
$"{CacheKeys.UserIdForClaim}={userId}"
```

---

## Success Criteria

✅ SQL script executes without errors
✅ Claim 140 exists with GroupId=1
✅ Admin user can access endpoint (200 OK, not 403)
✅ Response includes complete PlantAnalysisDetailDto structure
✅ Sponsored analyses return 404 (not accessible via this endpoint)
✅ Non-admin users receive 403 Forbidden
✅ Response matches farmer's view structure
✅ No impact on existing endpoints (claims 133-139)

---

## Related Documentation

- [API Documentation](ADMIN_NON_SPONSORED_ANALYSIS_DETAIL_API.md) - Complete API guide with examples
- [Admin Sponsor View API](ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md) - Full admin operations API
- [Previous Deployment Checklist](DEPLOYMENT_CHECKLIST.md) - Admin authorization fixes
- [SQL Script](ADD_ADMIN_NON_SPONSORED_ANALYSIS_DETAIL_CLAIM.sql) - Claim creation script

---

## Status

**Last Updated**: 2025-11-10
**Branch**: `enhancement-for-admin-operations`
**Build Status**: ✅ Successful (warnings only, no errors)
**Awaiting**: SQL script execution on staging database
**Next Step**: Execute Step 1 (SQL script) then proceed with testing
