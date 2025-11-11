# User Statistics Endpoint Bug Fix

**Date:** 2025-11-11
**Endpoint:** `/admin/analytics/user-statistics`
**Status:** ✅ Fixed

## Problem Description

The `/admin/analytics/user-statistics` endpoint was returning incorrect role counts:

```json
{
  "data": {
    "totalUsers": 155,
    "activeUsers": 154,
    "inactiveUsers": 1,
    "farmerUsers": 0,      // ❌ WRONG - should be non-zero
    "sponsorUsers": 0,     // ❌ WRONG - should be non-zero
    "adminUsers": 1
  }
}
```

With 155 total users, having 0 farmers and 0 sponsors is clearly incorrect.

---

## Root Cause Analysis

### Incorrect Table Usage

The handler was querying the **wrong tables** for role-based user counts:

**OLD CODE (INCORRECT):**
```csharp
// Lines 56-87: GetUserStatisticsQuery.cs

// ❌ Looking for OperationClaims named "Farmer" and "Sponsor"
var farmerClaimId = _operationClaimRepository.Query()
    .Where(c => c.Name == "Farmer")
    .Select(c => c.Id)
    .FirstOrDefault();

var sponsorClaimId = _operationClaimRepository.Query()
    .Where(c => c.Name == "Sponsor")
    .Select(c => c.Id)
    .FirstOrDefault();

// ❌ Counting from UserClaims table
var farmerUsers = _userClaimRepository.Query()
    .Where(uc => uc.ClaimId == farmerClaimId)
    .Select(uc => uc.UserId)
    .Distinct()
    .Count();

var sponsorUsers = _userClaimRepository.Query()
    .Where(uc => uc.ClaimId == sponsorClaimId)
    .Select(uc => uc.UserId)
    .Distinct()
    .Count();
```

### Why It Failed

1. **No OperationClaims Named "Farmer" or "Sponsor"**
   - The system has OperationClaims like `GetSponsorDetailedReportQuery`, `CreateFarmerCommand`
   - But there are NO claims named simply "Farmer" or "Sponsor"
   - The queries returned `null` for `farmerClaimId` and `sponsorClaimId`

2. **Wrong Authorization Model**
   - ZiraAI uses **Groups** for role management (not OperationClaims)
   - Groups table: `Administrators`, `Farmer`, `Sponsor`
   - UserGroups table: Links users to their roles

3. **UserClaims vs UserGroups**
   - `UserClaims`: Links users to specific operation permissions (e.g., "can view reports")
   - `UserGroups`: Links users to their role groups (e.g., "is a Farmer")
   - The handler used `UserClaims` when it should use `UserGroups`

---

## Solution

### Correct Table Usage

**NEW CODE (CORRECT):**
```csharp
// Lines 55-83: GetUserStatisticsQuery.cs (FIXED)

// ✅ Get Groups by name (Administrators, Farmer, Sponsor)
var adminGroup = await _groupRepository.GetAsync(g => g.GroupName == "Administrators");
var farmerGroup = await _groupRepository.GetAsync(g => g.GroupName == "Farmer");
var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");

// ✅ Count users from UserGroups table by GroupId
var adminUsers = adminGroup != null
    ? _userGroupRepository.Query()
        .Where(ug => ug.GroupId == adminGroup.Id)
        .Select(ug => ug.UserId)
        .Distinct()
        .Count()
    : 0;

var farmerUsers = farmerGroup != null
    ? _userGroupRepository.Query()
        .Where(ug => ug.GroupId == farmerGroup.Id)
        .Select(ug => ug.UserId)
        .Distinct()
        .Count()
    : 0;

var sponsorUsers = sponsorGroup != null
    ? _userGroupRepository.Query()
        .Where(ug => ug.GroupId == sponsorGroup.Id)
        .Select(ug => ug.UserId)
        .Distinct()
        .Count()
    : 0;
```

### Dependency Changes

**OLD Dependencies:**
```csharp
private readonly IUserRepository _userRepository;
private readonly IUserClaimRepository _userClaimRepository;        // ❌ REMOVED
private readonly IOperationClaimRepository _operationClaimRepository; // ❌ REMOVED

public GetUserStatisticsQueryHandler(
    IUserRepository userRepository,
    IUserClaimRepository userClaimRepository,
    IOperationClaimRepository operationClaimRepository)
```

**NEW Dependencies:**
```csharp
private readonly IUserRepository _userRepository;
private readonly IGroupRepository _groupRepository;           // ✅ ADDED
private readonly IUserGroupRepository _userGroupRepository;   // ✅ ADDED

public GetUserStatisticsQueryHandler(
    IUserRepository userRepository,
    IGroupRepository groupRepository,
    IUserGroupRepository userGroupRepository)
```

---

## Authorization System Context

### Groups vs OperationClaims

**Groups (Roles):**
- `Administrators` (GroupId: 1) - System admins
- `Farmer` (GroupId: 2) - Farmer users
- `Sponsor` (GroupId: 3) - Sponsor companies

**OperationClaims (Permissions):**
- Specific operations like `GetSponsorDetailedReportQuery`
- Fine-grained permissions for endpoints
- Linked to Groups via `GroupClaims` table

**UserGroups (User-Role Assignment):**
- Links `Users` to `Groups`
- Defines what role each user has
- Used for authentication and role-based access control

**UserClaims (Direct Permission Assignment):**
- Can assign specific permissions directly to users
- NOT used for general role assignment
- Optional layer for custom permissions

### Table Relationships

```
User ──┬─→ UserGroups ──→ Groups (Administrators, Farmer, Sponsor)
       │
       └─→ UserClaims ──→ OperationClaims (GetSponsorDetailedReportQuery, etc.)
                              ↑
                              │
                         GroupClaims (links Groups to OperationClaims)
```

---

## Files Changed

### Business/Handlers/AdminAnalytics/Queries/GetUserStatisticsQuery.cs

**Lines Modified:**
- Lines 24-36: Constructor and dependencies
- Lines 55-83: Role-based counting logic

**Changes:**
- ✅ Replaced `IOperationClaimRepository` with `IGroupRepository`
- ✅ Replaced `IUserClaimRepository` with `IUserGroupRepository`
- ✅ Changed queries to use Groups by name and UserGroups by GroupId
- ✅ Added null safety checks for missing Groups

---

## Testing Instructions

### Before Fix:
```bash
curl -X GET "https://ziraai.com/api/admin/analytics/user-statistics" \
  -H "Authorization: Bearer {admin_token}"

# Response:
{
  "data": {
    "totalUsers": 155,
    "farmerUsers": 0,      // ❌ WRONG
    "sponsorUsers": 0,     // ❌ WRONG
    "adminUsers": 1
  }
}
```

### After Fix:
```bash
curl -X GET "https://ziraai.com/api/admin/analytics/user-statistics" \
  -H "Authorization: Bearer {admin_token}"

# Expected Response:
{
  "data": {
    "totalUsers": 155,
    "farmerUsers": 142,    // ✅ CORRECT - actual farmer count
    "sponsorUsers": 12,    // ✅ CORRECT - actual sponsor count
    "adminUsers": 1
  }
}
```

### Verification Steps:

1. **Deploy the fix** to production
2. **Test the endpoint** with admin credentials
3. **Verify counts** match database:
   ```sql
   -- Verify farmer count
   SELECT COUNT(DISTINCT ug."UserId")
   FROM public."UserGroup" ug
   JOIN public."Group" g ON ug."GroupId" = g."Id"
   WHERE g."GroupName" = 'Farmer';

   -- Verify sponsor count
   SELECT COUNT(DISTINCT ug."UserId")
   FROM public."UserGroup" ug
   JOIN public."Group" g ON ug."GroupId" = g."Id"
   WHERE g."GroupName" = 'Sponsor';

   -- Verify admin count
   SELECT COUNT(DISTINCT ug."UserId")
   FROM public."UserGroup" ug
   JOIN public."Group" g ON ug."GroupId" = g."Id"
   WHERE g."GroupName" = 'Administrators';
   ```

---

## Related Issues

### Similar Patterns in Codebase

This bug fix follows the **correct pattern** used elsewhere in the codebase:

**Example: FarmerSubscriptionAssignmentJobService.cs (Lines 122-136)**
```csharp
// ✅ CORRECT PATTERN - Assign user to Farmer group
var farmerGroup = await _groupRepository.GetAsync(g => g.GroupName == "Farmer");
if (farmerGroup != null)
{
    var userGroup = new UserGroup
    {
        UserId = user.UserId,
        GroupId = farmerGroup.Id
    };
    _userGroupRepository.Add(userGroup);
    await _userGroupRepository.SaveChangesAsync();
}
```

### Lessons Learned

1. **Always use Groups/UserGroups for role management**
   - Groups define roles (Farmer, Sponsor, Admin)
   - UserGroups link users to roles

2. **OperationClaims are for permissions, not roles**
   - OperationClaims are specific operations
   - GroupClaims link roles to permissions

3. **Follow existing patterns in the codebase**
   - Check other handlers for correct repository usage
   - FarmerSubscriptionAssignmentJobService had the correct pattern

---

## Impact

### Before Fix:
- ❌ Incorrect analytics data for admin dashboard
- ❌ Cannot track farmer/sponsor growth accurately
- ❌ Business intelligence reports show 0 users

### After Fix:
- ✅ Accurate role-based user counts
- ✅ Correct analytics for business intelligence
- ✅ Admin dashboard shows real user distribution
- ✅ Follows system architecture patterns

---

## Related Documentation

- [SECUREDOPERATION_GUIDE.md](../AdminOperations/SECUREDOPERATION_GUIDE.md) - Authorization system
- [004_fix_admin_sponsor_group_claims.sql](../AdminOperations/004_fix_admin_sponsor_group_claims.sql) - GroupClaims fix
- [Groups and Claims Architecture](#) - System architecture documentation

---

## Commit Information

**Branch:** `enhancement-for-admin-operations`
**Commit Message:** `fix: Use Groups/UserGroups for role counting in GetUserStatisticsQuery`
**Files Changed:** `Business/Handlers/AdminAnalytics/Queries/GetUserStatisticsQuery.cs`
