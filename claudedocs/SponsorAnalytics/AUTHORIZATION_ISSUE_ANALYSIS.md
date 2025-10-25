# Authorization Issue Analysis - Sponsor Analytics Endpoints

**Date:** 2025-01-25  
**Issue:** "AuthorizationsDenied" errors on sponsor analytics endpoints  
**Status:** Root cause identified, fix required

---

## Problem Summary

Testing sponsor analytics endpoints in staging environment revealed authorization failures:

✅ **Working:** Dashboard Summary (`/api/v1/sponsorship/dashboard-summary`)  
❌ **Failing:** Package Distribution Statistics (`/api/v1/sponsorship/package-statistics`)  
❌ **Failing:** Code Analysis Statistics (`/api/v1/sponsorship/code-analysis-statistics`)  
❌ **Failing:** Messaging Analytics (`/api/v1/sponsorship/messaging-analytics`)  
❌ **Failing:** Impact Analytics (`/api/v1/sponsorship/impact-analytics`)  
❌ **Failing:** Temporal Analytics (`/api/v1/sponsorship/temporal-analytics`)  
❌ **Failing:** ROI Analytics (`/api/v1/sponsorship/roi-analytics`)

**Error Response:**
```json
{
  "success": false,
  "message": "AuthorizationsDenied"
}
```

---

## Root Cause

### Authentication vs Authorization
- **Authentication:** ✅ Working correctly
  - User 1114 (ID: 159) successfully authenticated
  - JWT token contains correct roles: `["Farmer", "Sponsor"]`
  - Token verified via `/api/v1/sponsorship/debug/user-info`

- **Authorization:** ❌ Failing due to missing operation claims

### ZiraAI Authorization System

The system uses a **two-layer authorization** approach:

1. **Controller Level** - `[Authorize(Roles = "Sponsor,Admin")]`
   - ✅ User passes this check (has "Sponsor" role)

2. **Handler Level** - `[SecuredOperation(Priority = 1)]`
   - ❌ User fails this check (missing operation claims)

### SecuredOperation Aspect Logic

Located in: `Business/BusinessAspects/SecuredOperation.cs`

```csharp
protected override void OnBefore(IInvocation invocation)
{
    // 1. Get user ID from JWT token
    var userId = _httpContextAccessor.HttpContext?.User.Claims
        .FirstOrDefault(x => x.Type.EndsWith("nameidentifier"))?.Value;

    if (userId == null)
    {
        throw new SecurityException(Messages.AuthorizationsDenied); // Not our issue
    }

    // 2. Get operation claims from cache for this user
    var oprClaims = _cacheManager.Get<IEnumerable<string>>($"{CacheKeys.UserIdForClaim}={userId}");

    // 3. Get the handler class name being invoked
    var operationName = invocation.TargetType.ReflectedType.Name;
    // Example: "GetPackageDistributionStatisticsQueryHandler"

    // 4. Check if user has this specific operation claim
    if (oprClaims.Contains(operationName))
    {
        return; // ✅ Authorization successful
    }

    throw new SecurityException(Messages.AuthorizationsDenied); // ❌ THIS IS WHERE IT FAILS
}
```

**The Issue:** User's operation claims cache does NOT contain the handler class names for the new analytics endpoints.

---

## Missing Operation Claims

The following handler class names need to be added as operation claims and assigned to the "Sponsor" role:

| Handler Class Name | Endpoint | Status |
|--------------------|----------|--------|
| `GetSponsorDashboardSummaryQueryHandler` | `/dashboard-summary` | ✅ Exists |
| `GetPackageDistributionStatisticsQueryHandler` | `/package-statistics` | ❌ Missing |
| `GetCodeAnalysisStatisticsQueryHandler` | `/code-analysis-statistics` | ❌ Missing |
| `GetSponsorMessagingAnalyticsQueryHandler` | `/messaging-analytics` | ❌ Missing |
| `GetSponsorImpactAnalyticsQueryHandler` | `/impact-analytics` | ❌ Missing |
| `GetSponsorTemporalAnalyticsQueryHandler` | `/temporal-analytics` | ❌ Missing |
| `GetSponsorROIAnalyticsQueryHandler` | `/roi-analytics` | ❌ Missing |

---

## Required Fix

### Option 1: Database Migration (RECOMMENDED)

Create a migration to:
1. Insert new operation claims for each handler
2. Assign them to the "Sponsor" role
3. Update user operation claims cache

**Migration Name:** `AddSponsorAnalyticsOperationClaims`

**Required SQL:**
```sql
-- 1. Insert Operation Claims
INSERT INTO "OperationClaims" ("Name", "Alias", "Description", "CreatedDate", "UpdatedDate", "Status")
VALUES 
('GetPackageDistributionStatisticsQueryHandler', 'Package Distribution Statistics', 'View package distribution analytics', NOW(), NOW(), 1),
('GetCodeAnalysisStatisticsQueryHandler', 'Code Analysis Statistics', 'View code-level analysis statistics', NOW(), NOW(), 1),
('GetSponsorMessagingAnalyticsQueryHandler', 'Messaging Analytics', 'View messaging analytics', NOW(), NOW(), 1),
('GetSponsorImpactAnalyticsQueryHandler', 'Impact Analytics', 'View impact analytics', NOW(), NOW(), 1),
('GetSponsorTemporalAnalyticsQueryHandler', 'Temporal Analytics', 'View temporal analytics', NOW(), NOW(), 1),
('GetSponsorROIAnalyticsQueryHandler', 'ROI Analytics', 'View ROI analytics', NOW(), NOW(), 1);

-- 2. Get Sponsor Role ID
-- Assuming Sponsor role ID is known (e.g., ID = 3)

-- 3. Assign Claims to Sponsor Role
INSERT INTO "OperationClaimGroups" ("RoleId", "OperationClaimId", "CreatedDate", "UpdatedDate", "Status")
SELECT 
    (SELECT "Id" FROM "Groups" WHERE "GroupName" = 'Sponsor' LIMIT 1),
    oc."Id",
    NOW(),
    NOW(),
    1
FROM "OperationClaims" oc
WHERE oc."Name" IN (
    'GetPackageDistributionStatisticsQueryHandler',
    'GetCodeAnalysisStatisticsQueryHandler',
    'GetSponsorMessagingAnalyticsQueryHandler',
    'GetSponsorImpactAnalyticsQueryHandler',
    'GetSponsorTemporalAnalyticsQueryHandler',
    'GetSponsorROIAnalyticsQueryHandler'
);
```

### Option 2: OperationClaimCreatorMiddleware

The system has automatic operation claim creation via `OperationClaimCreatorMiddleware.cs`.

**Location:** `Business/Helpers/OperationClaimCreatorMiddleware.cs`

This middleware automatically:
1. Scans all handler classes at startup
2. Creates operation claims for them
3. BUT: Does NOT assign them to roles

**Action Required:** 
- After operation claims are auto-created, manually assign them to "Sponsor" role via Admin panel or direct SQL

---

## Verification Steps

After applying the fix:

1. **Restart the application** to refresh operation claims cache
2. **Re-authenticate** to get fresh JWT token with updated operation claims
3. **Test each endpoint** with the new token
4. **Verify** successful responses instead of "AuthorizationsDenied"

**Test Command:**
```bash
TOKEN="<new_token_after_fix>"

curl -s -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/package-statistics" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0" | jq .
```

---

## Recommended Actions

**Immediate (for Testing):**
1. Create and run the migration on staging database
2. Restart staging API to clear caches
3. Re-authenticate test user
4. Complete all 18 test scenarios

**Long-term (for Production):**
1. Include operation claim assignment in deployment checklist
2. Document operation claim requirements for new features
3. Consider automated testing for operation claim coverage

---

## Technical Details

### Database Tables Involved
- `OperationClaims` - Stores all operation claims
- `Groups` - Stores roles (Admin, Sponsor, Farmer)
- `OperationClaimGroups` - Maps operation claims to roles
- `UserOperationClaims` - (Optional) Direct user-to-claim mapping

### Cache Keys
- Format: `UserIdForClaim={userId}`
- TTL: Based on system configuration
- Populated from: `Groups` → `OperationClaimGroups` → `OperationClaims`

### Authorization Flow
```
HTTP Request
  ↓
[Authorize] Filter (Check JWT Role)
  ↓
MediatR Send (Invoke Handler)
  ↓
[SecuredOperation] Aspect (Check Operation Claim)
  ↓
Handler Execution (If authorized)
```

---

## Related Files

- `Business/BusinessAspects/SecuredOperation.cs` - Authorization aspect
- `Business/Helpers/OperationClaimCreatorMiddleware.cs` - Auto-creation logic
- `WebAPI/Controllers/SponsorshipController.cs` - Controller with [Authorize] attributes
- All Query Handlers in `Business/Handlers/Sponsorship/Queries/` - Have [SecuredOperation]

---

## Contact

If this issue persists after applying the fix, check:
1. Database migration was successful
2. Application restarted after migration
3. Cache was properly cleared
4. User re-authenticated with fresh token
