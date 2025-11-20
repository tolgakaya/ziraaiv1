# Admin Operations - Deployment Checklist

## Overview
This document provides a checklist for deploying the admin authorization fixes for sponsor view operations.

**Branch**: `enhancement-for-admin-operations`
**Commit**: `5389ffb - fix: Enable admin access to message attachments and fix operation claims SQL`
**Date**: 2025-11-09

---

## Changes Summary

### 1. FilesController.cs - Admin Authorization for Message Attachments
**File**: [WebAPI/Controllers/FilesController.cs](../../WebAPI/Controllers/FilesController.cs)

**Issue**: Admin users were receiving `403 Forbidden` when accessing message attachments and voice messages in sponsor-farmer conversations.

**Fix**: Added admin role check to authorization logic in two methods:
- `GetAttachment()` - Line 147-157
- `GetVoiceMessage()` - Line 68-78

**Logic**:
```csharp
var isAdmin = User.HasClaim(c => c.Type.EndsWith("role") && c.Value == "Admin");
var isParticipant = message.FromUserId == userId.Value || message.ToUserId == userId.Value;

if (!isParticipant && !isAdmin)
{
    return Forbid();
}
```

### 2. ADD_ADMIN_SPONSOR_VIEW_CLAIMS.sql - Database Schema Fixes
**File**: [claudedocs/AdminOperations/ADD_ADMIN_SPONSOR_VIEW_CLAIMS.sql](ADD_ADMIN_SPONSOR_VIEW_CLAIMS.sql)

**Issue**: SQL script failed with PostgreSQL error due to non-existent columns and wrong data in claim 139.

**Fixes**:
1. ✅ Removed `CreatedAt` and `UpdatedAt` columns from all INSERT statements (columns don't exist in OperationClaims table)
2. ✅ Added UPDATE logic for claim 139 to fix wrong data (`TransferCodesToDealerCommand` → `SendMessageAsSponsorCommand`)
3. ✅ Script now safely handles both new insertions and fixing existing data

---

## Deployment Steps

### Step 1: Execute SQL Script on Staging Database ⏳
**Action Required**: Run the fixed SQL script to create claims 133-139.

```bash
# Connect to Railway PostgreSQL staging database
psql -h [staging-host] -U [username] -d [database-name]

# Execute the script
\i claudedocs/AdminOperations/ADD_ADMIN_SPONSOR_VIEW_CLAIMS.sql
```

**Expected Output**:
```
NOTICE:  Claim 133 (GetSponsorAnalysesAsAdminQuery) added successfully
NOTICE:  Claim 134 (GetSponsorAnalysisDetailAsAdminQuery) added successfully
NOTICE:  Claim 135 (GetSponsorMessagesAsAdminQuery) added successfully
NOTICE:  Claim 136 (GetNonSponsoredAnalysesQuery) added successfully
NOTICE:  Claim 137 (GetNonSponsoredFarmerDetailQuery) added successfully
NOTICE:  Claim 138 (GetSponsorshipComparisonAnalyticsQuery) added successfully
NOTICE:  Claim 139 updated from wrong data to SendMessageAsSponsorCommand (or added/skipped)
NOTICE:  Claim 133 granted to Administrators group
NOTICE:  Claim 134 granted to Administrators group
NOTICE:  Claim 135 granted to Administrators group
NOTICE:  Claim 136 granted to Administrators group
NOTICE:  Claim 137 granted to Administrators group
NOTICE:  Claim 138 granted to Administrators group
NOTICE:  Claim 139 granted to Administrators group
```

### Step 2: Verify Claims Created ✅
**Action Required**: Run verification query to confirm all claims exist.

```sql
-- Verification Query
SELECT oc."Id", oc."Name", oc."Alias",
       CASE WHEN gc."GroupId" IS NOT NULL THEN 'YES - GroupId: ' || gc."GroupId"
            ELSE 'NO - MISSING' END as "HasGroupClaim"
FROM "OperationClaims" oc
LEFT JOIN "GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Id" IN (133, 134, 135, 136, 137, 138, 139)
ORDER BY oc."Id";
```

**Expected Result**: All 7 rows should show `YES - GroupId: 1`

| Id  | Name | Alias | HasGroupClaim |
|-----|------|-------|---------------|
| 133 | GetSponsorAnalysesAsAdminQuery | Admin Sponsor Analyses View | YES - GroupId: 1 |
| 134 | GetSponsorAnalysisDetailAsAdminQuery | Admin Sponsor Analysis Detail View | YES - GroupId: 1 |
| 135 | GetSponsorMessagesAsAdminQuery | Admin Sponsor Messages View | YES - GroupId: 1 |
| 136 | GetNonSponsoredAnalysesQuery | Admin Non-Sponsored Analyses View | YES - GroupId: 1 |
| 137 | GetNonSponsoredFarmerDetailQuery | Admin Non-Sponsored Farmer Detail | YES - GroupId: 1 |
| 138 | GetSponsorshipComparisonAnalyticsQuery | Admin Sponsorship Comparison Analytics | YES - GroupId: 1 |
| 139 | SendMessageAsSponsorCommand | Admin Send Message As Sponsor | YES - GroupId: 1 |

### Step 3: Deploy Code Changes 🚀
**Action Required**: Deploy the updated `FilesController.cs` to staging.

```bash
# Switch to feature branch
git checkout enhancement-for-admin-operations

# Pull latest changes
git pull origin enhancement-for-admin-operations

# Deploy via Railway (automatic on push to branch linked to staging)
# OR manually deploy if needed
```

### Step 4: Clear Admin Claims Cache 🔄
**Action Required**: Admin user must logout and login to refresh claims cache.

**Why**: User claims are cached with key `CacheKeys.UserIdForClaim={userId}` for performance. New claims won't be available until cache is refreshed.

**Steps**:
1. Admin user logs out completely
2. Admin user logs back in
3. JWT token will include new claims
4. Cache will be repopulated with claims 133-139

### Step 5: Test Endpoints 🧪
**Action Required**: Verify all endpoints work with admin user.

#### Test 1: Sponsor Analyses List (Previously 401)
```http
GET /api/admin/sponsorship/sponsors/159/analyses?page=1&pageSize=20&sortBy=date&sortOrder=desc
Authorization: Bearer {admin-jwt-token}
```

**Expected**: `200 OK` with analyses list (not `401 Unauthorized`)

#### Test 2: Sponsor Analysis Detail
```http
GET /api/admin/sponsorship/sponsors/159/analyses/76
Authorization: Bearer {admin-jwt-token}
```

**Expected**: `200 OK` with analysis detail

#### Test 3: Sponsor Messages (Conversation)
```http
GET /api/admin/sponsorship/sponsors/159/messages?farmerUserId=170&plantAnalysisId=76&page=1&pageSize=20
Authorization: Bearer {admin-jwt-token}
```

**Expected**: `200 OK` with message list

#### Test 4: Message Attachment Access (Previously 403)
```http
GET /api/v1/files/attachments/109/0
Authorization: Bearer {admin-jwt-token}
```

**Expected**: `200 OK` with attachment file (not `403 Forbidden`)

#### Test 5: Voice Message Access
```http
GET /api/v1/files/voice-messages/{messageId}
Authorization: Bearer {admin-jwt-token}
```

**Expected**: `200 OK` with audio file

---

## Rollback Plan

If issues occur after deployment:

### Revert Code Changes
```bash
git checkout enhancement-for-admin-operations
git revert 5389ffb
git push origin enhancement-for-admin-operations
```

### Revert Database Changes (If Needed)
```sql
-- Remove claims 133-138 (keep 139 as it's used elsewhere)
DELETE FROM "GroupClaims" WHERE "ClaimId" IN (133, 134, 135, 136, 137, 138);
DELETE FROM "OperationClaims" WHERE "Id" IN (133, 134, 135, 136, 137, 138);

-- Restore old claim 139 data (if needed)
UPDATE "OperationClaims"
SET "Name" = 'TransferCodesToDealerCommand',
    "Alias" = 'dealer.transfer',
    "Description" = 'Dealer code transfer operation'
WHERE "Id" = 139;
```

---

## Technical Notes

### SecuredOperation Aspect Mechanism
```csharp
// Aspect checks if user has claim with name matching handler class name (without "Handler" suffix)
[SecuredOperation(Priority = 1)]
public class GetSponsorAnalysesAsAdminQueryHandler : IRequestHandler<...>
{
    // Aspect will check for claim: "GetSponsorAnalysesAsAdminQuery"
}
```

### Admin Role Detection Pattern
```csharp
// Works for any claim type ending with "role" (supports multiple claim schemas)
var isAdmin = User.HasClaim(c => c.Type.EndsWith("role") && c.Value == "Admin");
```

### Claims Cache Key
```csharp
// Cache key format used by SecuredOperation aspect
$"{CacheKeys.UserIdForClaim}={userId}"
```

---

## Success Criteria

✅ SQL script executes without errors
✅ All 7 claims exist with GroupId=1
✅ Admin user can access `/admin/sponsorship/sponsors/{id}/analyses` (200 OK, not 401)
✅ Admin user can access `/api/v1/files/attachments/{messageId}/{index}` (200 OK, not 403)
✅ Admin user can access `/api/v1/files/voice-messages/{messageId}` (200 OK, not 403)
✅ No impact on existing admin operations (claims 100-132)
✅ No impact on sponsor/farmer functionality

---

## Related Documentation

- [ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md](ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md) - Complete API documentation
- [SECUREDOPERATION_GUIDE.md](../../SECUREDOPERATION_GUIDE.md) - Authorization mechanism explained
- [ADD_ADMIN_SPONSOR_VIEW_CLAIMS.sql](ADD_ADMIN_SPONSOR_VIEW_CLAIMS.sql) - Claims creation script
- [FIX_MISSING_GROUP_CLAIMS_133-139.sql](FIX_MISSING_GROUP_CLAIMS_133-139.sql) - Backup verification script

---

## Status

**Last Updated**: 2025-11-09
**Branch**: `enhancement-for-admin-operations`
**Build Status**: ✅ Successful (warnings only, no errors)
**Awaiting**: SQL script execution on staging database
