# Sponsor Role Assignment Decision & System Analysis

## Final Decision (2025-10-09)

**User's Decision**: Kullanıcı kesin olarak ne olursa olsun ilk kayıtta sadece Farmer rolü ile kaydolur. Daha sonra giriş yaptıktan sonra sponsor olmak isterse sponsorluk profili oluşturur ve bu sırada bu kullanıcıya Sponsor rolü de eklenir. Yani iki rol birden olur (Farmer + Sponsor).

### Key Points:
1. **Initial Registration**: All users register with Farmer role only via phone registration
2. **Sponsor Profile Creation**: After login, user can create sponsor profile
3. **Dual Roles**: User gets BOTH Farmer AND Sponsor roles (not replacing, adding)
4. **No Special Endpoint**: No separate register-phone-sponsor endpoint needed

## Current System Analysis

### ✅ Existing Endpoints & Flow

#### 1. Phone Registration (Working)
- **Endpoint**: `POST /api/v1/auth/register-phone` → `POST /api/v1/auth/verify-phone-register`
- **Handler**: `Business/Handlers/Authorizations/Commands/VerifyPhoneRegisterCommand.cs`
- **Behavior**: Assigns Farmer role (GroupId = 2) automatically
- **Status**: ✅ Working as expected

#### 2. Sponsor Profile Creation (Working)
- **Endpoint**: `POST /api/v1/sponsorship/create-profile`
- **Controller**: `WebAPI/Controllers/SponsorshipController.cs:45-86`
- **Handler**: `Business/Handlers/SponsorProfiles/Commands/CreateSponsorProfileCommand.cs`
- **Payload**:
```json
{
  "companyName": "Acme Corp",
  "companyDescription": "Agricultural solutions provider",
  "sponsorLogoUrl": "https://...",
  "websiteUrl": "https://...",
  "contactEmail": "contact@acme.com",
  "contactPhone": "+905551234567",
  "contactPerson": "John Doe",
  "companyType": "Agriculture",
  "businessModel": "B2B"
}
```
- **Current Behavior**: 
  - Creates SponsorProfile record
  - Links to authenticated user's ID
  - **Does NOT assign Sponsor role to UserGroups table**
- **Status**: ⚠️ **MISSING SPONSOR ROLE ASSIGNMENT**

### ❌ Critical Gap Identified

**Problem**: `CreateSponsorProfileCommand` does NOT add Sponsor role to user.

**Current Code** (lines 30-73):
- Only injects `ISponsorProfileRepository`
- Creates `SponsorProfile` entity
- Saves to database
- **Missing**: UserGroup assignment with GroupId = 3 (Sponsor)

**What's Missing**:
```csharp
// MISSING CODE - needs to be added:
private readonly IUserGroupRepository _userGroupRepository;
private readonly IGroupRepository _groupRepository;

// In Handle method after profile creation:
var sponsorGroup = await _groupRepository.GetAsync(g => g.GroupName == "Sponsor");
if (sponsorGroup != null)
{
    var userGroup = new UserGroup
    {
        UserId = request.SponsorId,
        GroupId = sponsorGroup.Id  // GroupId = 3 for Sponsor
    };
    _userGroupRepository.Add(userGroup);
    await _userGroupRepository.SaveChangesAsync();
}
```

### Required Implementation

#### Step 1: Update CreateSponsorProfileCommand Handler
**File**: `Business/Handlers/SponsorProfiles/Commands/CreateSponsorProfileCommand.cs`

**Changes Needed**:
1. Add constructor dependencies:
   - `IUserGroupRepository _userGroupRepository`
   - `IGroupRepository _groupRepository`
2. Add sponsor role assignment logic after profile creation
3. Handle edge cases:
   - User already has Sponsor role (idempotent)
   - Sponsor group doesn't exist in database

#### Step 2: Verify Database Schema
**Tables Involved**:
- `Users` (existing user from phone registration)
- `Groups` (ensure GroupId = 3, GroupName = "Sponsor" exists)
- `UserGroups` (add new record: UserId + GroupId = 3)
- `SponsorProfiles` (already being created correctly)

## Complete Sponsor Onboarding Flow

### Flow Diagram:
```
1. Phone Registration
   ├─ POST /api/v1/auth/register-phone (send SMS code)
   └─ POST /api/v1/auth/verify-phone-register
      └─ Creates User with Farmer role (GroupId = 2)
      
2. Login
   └─ POST /api/v1/auth/login
      └─ Returns JWT with Farmer role
      
3. Create Sponsor Profile (REQUIRES FIX)
   └─ POST /api/v1/sponsorship/create-profile
      ├─ Creates SponsorProfile record
      └─ ⚠️ MUST ADD: Assign Sponsor role (GroupId = 3)
      
4. Token Refresh (for new role)
   └─ User gets new token with BOTH roles:
      - Farmer (GroupId = 2)
      - Sponsor (GroupId = 3)
```

### Endpoint Details

| Step | Endpoint | Payload | Response | Role |
|------|----------|---------|----------|------|
| 1 | `POST /api/v1/auth/register-phone` | `{ mobilePhone, fullName }` | `{ success, message }` | None |
| 2 | `POST /api/v1/auth/verify-phone-register` | `{ mobilePhone, code, fullName, referralCode? }` | `{ accessToken, refreshToken }` | Farmer |
| 3 | `POST /api/v1/sponsorship/create-profile` | `CreateSponsorProfileDto` | `{ success, message }` | Farmer + Sponsor |

### Required Payload Examples

**Step 3 - Create Sponsor Profile**:
```json
{
  "companyName": "Ziraat Teknolojileri A.Ş.",
  "companyDescription": "Modern tarım çözümleri sağlayıcısı",
  "sponsorLogoUrl": "https://cdn.ziraai.com/logos/ziraat-tech.png",
  "websiteUrl": "https://ziraatteknolojileri.com",
  "contactEmail": "iletisim@ziraatteknolojileri.com",
  "contactPhone": "+905551234567",
  "contactPerson": "Ahmet Yılmaz",
  "companyType": "Agriculture",
  "businessModel": "B2B"
}
```

## Action Items

### HIGH PRIORITY - MUST IMPLEMENT
1. ✅ Identify missing sponsor role assignment in CreateSponsorProfileCommand
2. ⚠️ **IMPLEMENT**: Add sponsor role assignment logic to handler
3. ⚠️ **TEST**: Verify dual role assignment works correctly
4. ⚠️ **VERIFY**: JWT token includes both Farmer and Sponsor roles after profile creation

### Database Verification
- Check Groups table has: `Id = 3, GroupName = "Sponsor"`
- Verify UserGroups allows multiple roles per user
- Ensure no unique constraint on UserId in UserGroups

### Testing Checklist
1. Register new user via phone → verify has Farmer role only
2. Login → verify JWT has Farmer role
3. Create sponsor profile → verify UserGroups has TWO records (Farmer + Sponsor)
4. Refresh token or re-login → verify JWT has BOTH roles
5. Test sponsor endpoints require "Sponsor" role → verify access granted

## References
- Phone registration handler: `Business/Handlers/Authorizations/Commands/VerifyPhoneRegisterCommand.cs:155-165`
- Sponsor profile creation: `Business/Handlers/SponsorProfiles/Commands/CreateSponsorProfileCommand.cs`
- Controller endpoint: `WebAPI/Controllers/SponsorshipController.cs:45-86`
- Available repositories: `IUserGroupRepository`, `IGroupRepository`
