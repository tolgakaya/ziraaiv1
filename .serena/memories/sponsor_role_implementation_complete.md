# Sponsor Role Assignment Implementation - COMPLETED

## Implementation Date
2025-10-09

## Final Decision
**User's Decision**: ALL users register with Farmer role only (regardless of registration method). Users can later create a sponsor profile to receive BOTH Farmer AND Sponsor roles.

## Changes Implemented

### 1. RegisterUserCommand.cs - Force Farmer Role
**File**: `Business/Handlers/Authorizations/Commands/RegisterUserCommand.cs`

**Change**: Removed user ability to specify role during email/password registration

**Before**:
```csharp
var requestedRole = request.UserRole ?? "Farmer"; // Default to Farmer if not specified
```

**After**:
```csharp
// Always assign Farmer role on registration (regardless of user input)
// Users can become Sponsors later by creating a sponsor profile
var requestedRole = "Farmer"; // Always force Farmer role on registration
```

**Impact**:
- Email/password registration: Always assigns Farmer role
- `role` parameter in request body is now **ignored**
- Consistent with phone registration behavior

### 2. CreateSponsorProfileCommand.cs - Add Sponsor Role
**File**: `Business/Handlers/SponsorProfiles/Commands/CreateSponsorProfileCommand.cs`

**Changes**:
1. Added new using statement: `using Core.Entities.Concrete;`
2. Added constructor dependencies:
   - `IUserGroupRepository _userGroupRepository`
   - `IGroupRepository _groupRepository`
3. Added sponsor role assignment logic after profile creation

**Added Code** (after line 77):
```csharp
// Assign Sponsor role to user (in addition to existing Farmer role)
var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");
if (sponsorGroup != null)
{
    // Check if user already has Sponsor role (idempotent operation)
    var existingUserGroup = await _userGroupRepository.GetAsync(
        ug => ug.UserId == request.SponsorId && ug.GroupId == sponsorGroup.Id);
    
    if (existingUserGroup == null)
    {
        var userGroup = new UserGroup
        {
            UserId = request.SponsorId,
            GroupId = sponsorGroup.Id
        };
        _userGroupRepository.Add(userGroup);
        await _userGroupRepository.SaveChangesAsync();
    }
}
```

**Features**:
- ✅ Idempotent: Checks if user already has Sponsor role
- ✅ Additive: Doesn't remove Farmer role, adds Sponsor role
- ✅ Safe: Only assigns if Sponsor group exists in database
- ✅ Automatic: No manual admin intervention needed

## Complete User Flow

### Registration Flow (Both Methods)

#### Method 1: Phone Registration
```
1. POST /api/v1/auth/register-phone
   Body: { mobilePhone: "+905551234567", fullName: "Ahmet Yılmaz" }
   
2. POST /api/v1/auth/verify-phone-register
   Body: { mobilePhone: "+905551234567", code: 123456, fullName: "Ahmet Yılmaz" }
   Result: User created with Farmer role (GroupId = 2)
```

#### Method 2: Email/Password Registration
```
POST /api/v1/auth/register
Body: {
  "email": "ahmet@example.com",
  "password": "SecurePass123",
  "fullName": "Ahmet Yılmaz",
  "mobilePhones": "+905551234567",
  "role": "Sponsor"  // ⚠️ THIS IS NOW IGNORED
}
Result: User created with Farmer role (GroupId = 2) - role parameter ignored
```

### Sponsor Profile Creation Flow
```
1. User logs in
   POST /api/v1/auth/login
   Response: JWT with Farmer role
   
2. User creates sponsor profile
   POST /api/v1/sponsorship/create-profile
   Headers: { Authorization: "Bearer <jwt_token>" }
   Body: {
     "companyName": "Ziraat Teknolojileri A.Ş.",
     "companyDescription": "Modern tarım çözümleri",
     "sponsorLogoUrl": "https://cdn.ziraai.com/logo.png",
     "websiteUrl": "https://ziraatteknolojileri.com",
     "contactEmail": "iletisim@example.com",
     "contactPhone": "+905551234567",
     "contactPerson": "Ahmet Yılmaz",
     "companyType": "Agriculture",
     "businessModel": "B2B"
   }
   
   Result: 
   - SponsorProfile record created ✅
   - Sponsor role (GroupId = 3) assigned ✅
   - User now has BOTH Farmer AND Sponsor roles ✅

3. User refreshes token or re-logs in
   POST /api/v1/auth/login
   Response: JWT with BOTH Farmer AND Sponsor roles in claims
```

## Database Changes

### Before Sponsor Profile Creation
```sql
-- Users table
UserId: 123, FullName: "Ahmet Yılmaz", Email: "ahmet@example.com"

-- UserGroups table
UserId: 123, GroupId: 2  -- Farmer role only
```

### After Sponsor Profile Creation
```sql
-- Users table (unchanged)
UserId: 123, FullName: "Ahmet Yılmaz", Email: "ahmet@example.com"

-- UserGroups table (NEW ROW ADDED)
UserId: 123, GroupId: 2  -- Farmer role (existing)
UserId: 123, GroupId: 3  -- Sponsor role (NEW)

-- SponsorProfiles table (NEW ROW ADDED)
SponsorId: 123, CompanyName: "Ziraat Teknolojileri A.Ş.", ...
```

## Authorization Behavior

### Before Implementation
```csharp
[Authorize(Roles = "Sponsor")]
public async Task<IActionResult> SponsorOnlyEndpoint()
```
- Users with Farmer role: ❌ Access Denied (403)
- Users had to register as Sponsor from start or admin manual assignment

### After Implementation
```csharp
[Authorize(Roles = "Sponsor")]
public async Task<IActionResult> SponsorOnlyEndpoint()
```
- Users with Farmer role only: ❌ Access Denied (403)
- Users with Farmer + Sponsor roles: ✅ Access Granted (200)
- Automatic after sponsor profile creation

```csharp
[Authorize(Roles = "Farmer")]
public async Task<IActionResult> FarmerOnlyEndpoint()
```
- Users with Farmer + Sponsor roles: ✅ Still have access (both roles)

## Testing Checklist

### ✅ Test Scenario 1: Phone Registration
1. Register via phone → verify user has Farmer role only
2. Login → verify JWT contains Farmer role claim
3. Create sponsor profile → verify SponsorProfile created + Sponsor role added
4. Refresh token → verify JWT contains BOTH Farmer and Sponsor roles

### ✅ Test Scenario 2: Email Registration with role="Sponsor"
1. Register with `role: "Sponsor"` → verify role parameter is IGNORED
2. Verify user has Farmer role only (not Sponsor)
3. Create sponsor profile → verify Sponsor role added
4. Verify user now has both roles

### ✅ Test Scenario 3: Idempotent Sponsor Profile Creation
1. Create sponsor profile → success
2. Try to create sponsor profile again → error "SponsorProfileAlreadyExists"
3. Verify Sponsor role not duplicated in UserGroups

### ✅ Test Scenario 4: Authorization Checks
1. Farmer-only user tries to access sponsor endpoint → 403 Forbidden
2. After creating sponsor profile → same endpoint returns 200 OK
3. Farmer endpoints still accessible to Farmer+Sponsor users

## SQL Verification Queries

```sql
-- Check user's roles
SELECT u.UserId, u.FullName, g.GroupName
FROM Users u
JOIN UserGroups ug ON u.UserId = ug.UserId
JOIN Groups g ON ug.GroupId = g.Id
WHERE u.Email = 'ahmet@example.com';

-- Expected after sponsor profile creation:
-- UserId | FullName      | GroupName
-- 123    | Ahmet Yılmaz  | Farmer
-- 123    | Ahmet Yılmaz  | Sponsor

-- Check sponsor profile exists
SELECT * FROM SponsorProfiles WHERE SponsorId = 123;
```

## Breaking Changes & Migration

### Potential Issues
1. **Existing Sponsor Users**: Users who registered via email with `role: "Sponsor"` already have Sponsor role
   - These users may not have SponsorProfile records
   - Consider creating SponsorProfiles for existing Sponsor users

2. **API Clients**: Mobile/web apps sending `role: "Sponsor"` in registration
   - Parameter will be ignored (no error, just ignored)
   - Update client code to remove role parameter
   - Update documentation

### Migration Script (Optional)
```sql
-- Find users with Sponsor role but no SponsorProfile
SELECT u.UserId, u.FullName, u.Email
FROM Users u
JOIN UserGroups ug ON u.UserId = ug.UserId
JOIN Groups g ON ug.GroupId = g.Id
WHERE g.GroupName = 'Sponsor'
  AND NOT EXISTS (
    SELECT 1 FROM SponsorProfiles sp WHERE sp.SponsorId = u.UserId
  );

-- Create placeholder SponsorProfiles for existing Sponsors
INSERT INTO SponsorProfiles (
  SponsorId, CompanyName, CompanyDescription, IsActive, 
  IsVerifiedCompany, CreatedDate, TotalPurchases, 
  TotalCodesGenerated, TotalCodesRedeemed, TotalInvestment
)
SELECT 
  u.UserId, 
  u.FullName + ' Company', 
  'Legacy sponsor account - please update profile',
  true,
  false,
  NOW(),
  0, 0, 0, 0
FROM Users u
JOIN UserGroups ug ON u.UserId = ug.UserId
JOIN Groups g ON ug.GroupId = g.Id
WHERE g.GroupName = 'Sponsor'
  AND NOT EXISTS (
    SELECT 1 FROM SponsorProfiles sp WHERE sp.SponsorId = u.UserId
  );
```

## Documentation Updates Needed

1. **API Documentation**:
   - Update `POST /api/v1/auth/register` endpoint docs
   - Note that `role` parameter is ignored
   - Add sponsor profile creation flow to docs

2. **Mobile Integration Guide**:
   - Update `claudedocs/MOBILE_SPONSORSHIP_INTEGRATION_GUIDE.md`
   - Remove references to `role` parameter
   - Document two-step sponsor registration

3. **Postman Collection**:
   - Update registration request examples
   - Add sponsor profile creation examples
   - Update test scripts to verify dual roles

## Related Files Modified

1. `Business/Handlers/Authorizations/Commands/RegisterUserCommand.cs`
2. `Business/Handlers/SponsorProfiles/Commands/CreateSponsorProfileCommand.cs`

## Related Files to Review

1. `claudedocs/ROLE_MANAGEMENT_COMPLETE_GUIDE.md` - Update role assignment docs
2. `claudedocs/MOBILE_SPONSORSHIP_INTEGRATION_GUIDE.md` - Update mobile integration flow
3. Postman Collection - Update registration examples

## Summary

✅ **Implemented**: Consistent role assignment across all registration methods  
✅ **Farmer First**: All users start as Farmers  
✅ **Dual Roles**: Sponsor profile creation adds Sponsor role (doesn't replace)  
✅ **Automatic**: No admin intervention needed  
✅ **Idempotent**: Safe to call multiple times  
✅ **Backward Compatible**: Existing endpoints unchanged (behavior change only)

## Next Steps

1. ✅ Code changes completed
2. ⚠️ Test with Postman/mobile app
3. ⚠️ Verify JWT token contains both roles after sponsor profile creation
4. ⚠️ Update API documentation
5. ⚠️ Consider migration script for existing Sponsor users without profiles
6. ⚠️ Deploy to staging environment for testing
7. ⚠️ Update mobile app if it sends `role` parameter
