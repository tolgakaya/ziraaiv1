# Dealer Code Distribution System - E2E Test Progress Report

**Date:** 2025-10-26
**Test Environment:** Railway Staging (ziraai-api-sit.up.railway.app)
**Branch:** feature/sponsorship-code-distribution-experiment

---

## Test Objective

End-to-End validation of the complete Dealer Code Distribution System:
1. Main sponsor transfers codes to dealer
2. Dealer distributes codes to farmers
3. Farmers perform plant analysis using distributed codes
4. Verify dealer can view only their distributed analyses
5. Verify main sponsor can view all analyses
6. Validate dealer analytics
7. Test tier-based messaging permissions

---

## Test Users

| Role | Phone | UserId | Notes |
|------|-------|--------|-------|
| Main Sponsor | 05411111114 | 159 | Has Purchase ID: 26 with codes |
| Dealer/Bayi | 05411111113 | 158 | Receives codes from main sponsor |
| Farmer/Çiftçi | 05061111113 | 165 | Receives codes from dealer |

---

## Completed Steps

### ✅ Step 1: Verify Main Sponsor Has Codes

**Status:** COMPLETED
**Purchase ID:** 26
**Result:** Main sponsor (UserId 159) confirmed to have available codes

### ✅ Step 2: Transfer Codes from Sponsor to Dealer

**Status:** COMPLETED
**Endpoint:** `POST /api/v1/sponsorship/dealer/transfer-codes`

**Request:**
```json
{
  "purchaseId": 26,
  "dealerId": 158,
  "codeCount": 5
}
```

**Response:**
```json
{
  "data": {
    "transferredCodeIds": [932, 933, 934, 935, 936],
    "transferredCount": 5,
    "dealerId": 158,
    "dealerName": "User 1113",
    "transferredAt": "2025-10-26T16:19:26.2847575+00:00"
  },
  "success": true,
  "message": "Successfully transferred 5 codes to dealer."
}
```

**Validation:**
- ✅ 5 codes successfully transferred
- ✅ Correct dealer ID (158)
- ✅ Transfer timestamp recorded
- ✅ Code IDs: 932, 933, 934, 935, 936

---

## Critical Issues Encountered and Resolved

### Issue 1: SecuredOperation Authorization Failure

**Problem:**
Transfer endpoint returned `AuthorizationsDenied` despite:
- Claim existing in database (`TransferCodesToDealerCommand`)
- Claim assigned to Sponsor group (GroupId=3)
- User 159 being in Sponsor group
- Claim cached in Redis

**Root Cause:**
`invocation.Method?.DeclaringType?.Name` was returning **interface name** `"IRequest\`2"` instead of **handler class name** `"TransferCodesToDealerCommandHandler"`.

**Investigation Steps:**
1. Verified claim exists in database ✅
2. Verified claim assigned to Sponsor group ✅
3. Verified User 159 in Sponsor group ✅
4. Added debug logging to SecuredOperation
5. Discovered operation name extraction was incorrect

**Solution:**
Changed from:
```csharp
var operationName = invocation.Method?.DeclaringType?.Name;
```

To:
```csharp
var operationName = invocation.TargetType?.Name;
```

**Files Modified:**
- `Business/BusinessAspects/SecuredOperation.cs` - Fixed operation name extraction
- Added `using Microsoft.Extensions.Logging;` for debug logging

**Commits:**
- `10d5289` - Added SecuredOperation logging
- `b87756e` - Fixed missing using directive
- `16a2723` - Fixed TargetType vs DeclaringType issue

---

## Technical Details

### Authorization Flow

1. **Login:** User authenticates via phone OTP
2. **Token Creation:** `PhoneAuthenticationProvider.CreateToken()`
   - Fetches claims via `UserRepository.GetClaimsAsync()`
   - Caches claims in Redis: `CacheKeys.UserIdForClaim={userId}`
3. **Authorization Check:** `SecuredOperation` aspect intercepts handler
   - Extracts userId from JWT token
   - Retrieves claims from Redis cache
   - Extracts operation name from handler class name
   - Removes "Handler" suffix to match claim name
   - Validates claim exists in user's cached claims

### Claim Naming Convention

**Pattern:** `{HandlerClassName}` - `"Handler"` = `{ClaimName}`

**Examples:**
- Handler: `TransferCodesToDealerCommandHandler`
- Claim: `TransferCodesToDealerCommand`

### SQL Migration

**File:** `claudedocs/Dealers/migrations/004_dealer_authorization.sql`

**Claims Created:**
1. TransferCodesToDealerCommand
2. CreateDealerInvitationCommand
3. ReclaimDealerCodesCommand
4. GetDealerPerformanceQuery
5. GetDealerSummaryQuery
6. GetDealerInvitationsQuery
7. SearchDealerByEmailQuery

**Group Assignments:**
- Sponsor Group (GroupId=3): All 7 claims
- Admin Group (GroupId=1): All 7 claims

---

## Database Verification Queries

### Check Claim Assignment
```sql
-- File: claudedocs/Dealers/check_transfer_claim.sql
SELECT
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias"
FROM public."GroupClaims" gc
INNER JOIN "Groups" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 3 -- Sponsor group
  AND oc."Name" LIKE '%Transfer%'
ORDER BY oc."Name";
```

**Result:** ✅ TransferCodesToDealerCommand assigned to Sponsor group

### Verify User Claims (Simulating GetClaimsAsync)
```sql
-- File: claudedocs/Dealers/test_getclaims_user159.sql
SELECT DISTINCT oc."Name" as "ClaimName"
FROM public."Users" u
INNER JOIN public."UserGroups" ug ON u."UserId" = ug."UserId"
INNER JOIN public."GroupClaims" gc ON ug."GroupId" = gc."GroupId"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE u."UserId" = 159
UNION
SELECT oc."Name" as "ClaimName"
FROM public."Users" u
INNER JOIN public."UserClaims" uc ON u."UserId" = uc."UserId"
INNER JOIN public."OperationClaims" oc ON uc."ClaimId" = oc."Id"
WHERE u."UserId" = 159
ORDER BY "ClaimName";
```

**Result:** 24 claims including TransferCodesToDealerCommand ✅

---

## Logs Analysis

### PhoneAuthenticationProvider Log
```
[PhoneAuth:Claims] User 159 has 24 claims: CreateDealerInvitationCommand,
GetCodeAnalysisStatisticsQuery, GetCodeAnalysisStatisticsQueryHandler,
GetDealerInvitationsQuery, GetDealerPerformanceQuery, GetDealerSummaryQuery, ...
```

### SecuredOperation Debug Log (Before Fix)
```
[SecuredOperation] UserId: 159, Operation: IRequest`2, CachedClaims:
CreateDealerInvitationCommand, GetCodeAnalysisStatisticsQuery, ...,
TransferCodesToDealerCommand
```

**Analysis:** Operation name was `IRequest\`2` (interface) instead of `TransferCodesToDealerCommandHandler` (handler class).

### SecuredOperation Debug Log (After Fix)
Expected to show:
```
[SecuredOperation] UserId: 159, Operation: TransferCodesToDealerCommand,
CachedClaims: ..., TransferCodesToDealerCommand
```

---

## Pending Steps

### 🔄 Step 3: Dealer Distributes Code to Farmer
**Status:** IN PROGRESS
**Endpoint:** `POST /api/v1/sponsorship/send-link`
**Test:** Dealer (158) sends code 932 to Farmer (165)

### ⏳ Step 4: Farmer Performs Analysis
**Status:** PENDING
**Action:** Farmer redeems code and performs plant analysis

### ⏳ Step 5: Dealer Views Own Analyses
**Status:** PENDING
**Endpoint:** `GET /api/v1/PlantAnalyses/list?dealerId=158`
**Validation:** Only analyses from farmer using dealer's codes

### ⏳ Step 6: Main Sponsor Views All Analyses
**Status:** PENDING
**Endpoint:** `GET /api/v1/PlantAnalyses/list`
**Validation:** All analyses visible to main sponsor

### ⏳ Step 7: Verify Dealer Analytics
**Status:** PENDING
**Endpoints:**
- `GET /api/v1/sponsorship/dealer/performance/{dealerId}`
- `GET /api/v1/sponsorship/dealer/summary`

### ⏳ Step 8: Test Tier-Based Messaging
**Status:** PENDING
**Validation:** Messaging permissions based on subscription tier

---

## Key Learnings

1. **Castle DynamicProxy Behavior**: `invocation.Method.DeclaringType` returns interface type for intercepted methods, not the actual handler class. Use `invocation.TargetType` instead.

2. **Redis Cache Persistence**: Deploy/restart does not clear Redis cache (unlike in-memory cache). Claims cached during login persist across deployments.

3. **Claim Naming Convention**: Claims must match handler class name minus "Handler" suffix. Any deviation breaks authorization.

4. **Debug Logging Essential**: Adding detailed logging to SecuredOperation was critical for identifying the root cause.

5. **SQL Idempotency**: Using `WHERE NOT EXISTS` instead of `ON CONFLICT` for PostgreSQL compatibility when Name field has no UNIQUE constraint.

---

## Next Actions

1. Continue E2E test from Step 3 (Dealer → Farmer code distribution)
2. Remove debug logging from SecuredOperation after test completion
3. Document complete E2E test flow with all endpoints
4. Update SECUREDOPERATION_GUIDE.md with TargetType vs DeclaringType lesson

---

## Related Documentation

- `claudedocs/SECUREDOPERATION_GUIDE.md` - SecuredOperation usage guide
- `claudedocs/Dealers/migrations/004_dealer_authorization.sql` - Authorization setup
- `claudedocs/Dealers/application.log` - Runtime logs
- `Business/BusinessAspects/SecuredOperation.cs:45-47` - Operation name extraction

---

**Test Continues...**
