# Farmer Profile Update Endpoint Documentation

**Generated Date**: 2025-11-13
**Status**: ✅ Currently Implemented (Generic User Endpoint)
**Comparison**: Limited compared to Sponsor Profile Endpoint

---

## Executive Summary

Farmers currently use a **generic user profile update endpoint** that provides basic profile editing capabilities with only **5 editable fields**. This is significantly limited compared to the Sponsor Profile endpoint which offers **27 rich profile fields** including social media, company information, and advanced features.

---

## 1. Current Farmer Profile Endpoint

### Endpoint Details
- **Route**: `PUT /api/users`
- **Controller**: [UsersController.cs:91](WebAPI/Controllers/UsersController.cs#L91)
- **Authorization**: `[SecuredOperation(Priority = 1)]` - Generic secured operation
- **Command**: `UpdateUserCommand`
- **DTO**: `UpdateUserDto`

### Request Structure

```http
PUT /api/users HTTP/1.1
Host: api.ziraai.com
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "userId": 123,
  "email": "farmer@example.com",
  "fullName": "John Doe",
  "mobilePhones": "+905551234567",
  "address": "123 Farm Street, Village Name",
  "notes": "Optional notes about the farmer"
}
```

### Response Structure

```json
{
  "success": true,
  "message": "Updated"
}
```

---

## 2. Editable Fields Analysis

### Current User Entity (Core/Entities/Concrete/User.cs)

| Field | Type | Editable via API | Description | Validation |
|-------|------|------------------|-------------|------------|
| `UserId` | `int` | ❌ No (ID only) | User identifier | Required (path) |
| `Email` | `string` | ✅ Yes | Email address | None |
| `FullName` | `string` | ✅ Yes | Full name | None |
| `MobilePhones` | `string` | ✅ Yes | Phone number | None |
| `Address` | `string` | ✅ Yes | Physical address | None |
| `Notes` | `string` | ✅ Yes | Additional notes | None |

### User Entity Fields NOT Editable

| Field | Type | Status | Why Not Editable |
|-------|------|--------|------------------|
| `CitizenId` | `long` | ❌ Not editable | National ID - security concern |
| `Status` | `bool` | ❌ Not editable | System managed |
| `BirthDate` | `DateTime?` | ❌ Not editable | Not in update DTO |
| `Gender` | `int?` | ❌ Not editable | Not in update DTO |
| `AvatarUrl` | `string` | ❌ Not editable | Separate upload endpoint needed |
| `AvatarThumbnailUrl` | `string` | ❌ Not editable | Auto-generated from avatar |
| `AvatarUpdatedDate` | `DateTime?` | ❌ Not editable | System managed |
| `RegistrationReferralCode` | `string` | ❌ Not editable | Set at registration only |
| `IsActive` | `bool` | ❌ Not editable | Admin-only field |
| `DeactivatedDate` | `DateTime?` | ❌ Not editable | Admin-only field |
| `DeactivatedBy` | `int?` | ❌ Not editable | Admin-only field |
| `DeactivationReason` | `string` | ❌ Not editable | Admin-only field |
| `RecordDate` | `DateTime` | ❌ Not editable | System managed |
| `UpdateContactDate` | `DateTime` | ❌ Not editable | System managed |
| `RefreshToken` | `string` | ❌ Not editable | Security - auth system only |
| `PasswordSalt` | `byte[]` | ❌ Not editable | Security - separate password endpoint |
| `PasswordHash` | `byte[]` | ❌ Not editable | Security - separate password endpoint |

---

## 3. Business Logic Analysis

### UpdateUserCommand Handler
**Location**: [Business/Handlers/Users/Commands/UpdateUserCommand.cs](Business/Handlers/Users/Commands/UpdateUserCommand.cs)

```csharp
public async Task<IResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
{
    // 1. Retrieve existing user
    var isThereAnyUser = await _userRepository.GetAsync(u => u.UserId == request.UserId);

    // 2. Update only allowed fields
    isThereAnyUser.FullName = request.FullName;
    isThereAnyUser.Email = request.Email;
    isThereAnyUser.MobilePhones = request.MobilePhones;
    isThereAnyUser.Address = request.Address;
    isThereAnyUser.Notes = request.Notes;

    // 3. Save changes
    _userRepository.Update(isThereAnyUser);
    await _userRepository.SaveChangesAsync();

    return new SuccessResult(Messages.Updated);
}
```

### Security Aspects
- **Aspect**: `[SecuredOperation(Priority = 1)]`
- **Cache Invalidation**: `[CacheRemoveAspect()]`
- **Logging**: `[LogAspect(typeof(FileLogger))]`

### Validation Rules
**Current State**: ❌ **NO VALIDATION**
- No FluentValidation validator exists
- No Data Annotations on UpdateUserDto
- No business rule validation (email format, phone format, etc.)
- No field length restrictions
- No null/empty checks

---

## 4. Comparison: Farmer vs Sponsor Profile Endpoints

### Feature Comparison Table

| Feature | Farmer (`PUT /api/users`) | Sponsor (`PUT /api/v1/sponsorship/update-profile`) | Gap |
|---------|---------------------------|-----------------------------------------------------|-----|
| **Editable Fields** | 5 fields | 27 fields | -22 fields |
| **Profile Picture** | ❌ No | ✅ Yes (SponsorLogoUrl) | Missing |
| **Biography/Description** | ❌ No | ✅ Yes (CompanyDescription) | Missing |
| **Contact Information** | ⚠️ Limited (Phone only) | ✅ Rich (Email, Phone, Person) | Limited |
| **Social Media Links** | ❌ No | ✅ Yes (4 platforms) | Missing |
| **Location Information** | ⚠️ Text only (Address) | ✅ Structured (Latitude/Longitude) | Limited |
| **Website URL** | ❌ No | ✅ Yes | Missing |
| **Business Details** | ❌ No | ✅ Yes (Type, Model, Industries) | Missing |
| **Validation** | ❌ No validator | ✅ Has validator | Missing |
| **Authorization** | Generic SecuredOperation | Role-specific (Sponsor,Admin) | Generic |
| **Dedicated Controller** | Generic UsersController | Dedicated SponsorshipController | No dedicated |

### Sponsor Profile Fields (27 total)

**Basic Information** (5 fields):
- CompanyName
- CompanyDescription
- SponsorLogoUrl
- WebsiteUrl
- CompanyType

**Contact Information** (3 fields):
- ContactEmail
- ContactPhone
- ContactPerson

**Social Media** (4 fields):
- LinkedInUrl
- TwitterUrl
- FacebookUrl
- InstagramUrl

**Business Information** (3 fields):
- BusinessModel
- TargetIndustries
- PrimaryCrops

**Location Information** (2 fields):
- Latitude
- Longitude

**Messaging Settings** (4 fields):
- PreferredCommunicationMethod
- MessageResponseTime
- AutoResponseEnabled
- AutoResponseMessage

**Tier Management** (2 fields):
- DesiredTierLevel
- UpgradeTierLevel

**System Fields** (4 fields - auto-managed):
- SponsorId (from JWT)
- UpdatedBy (from JWT)
- UpdatedDate (auto)
- IsActive (admin-only)

---

## 5. Missing Features for Farmers

### Critical Missing Features

1. **Profile Picture Management**
   - Current: Only AvatarUrl/AvatarThumbnailUrl in User entity (not editable)
   - Needed: Upload endpoint + update profile endpoint integration
   - Impact: Poor user experience, no visual identity

2. **Farm/Agricultural Information**
   - Current: Nothing farm-specific
   - Needed:
     - Farm name/description
     - Farm size (hectares)
     - Primary crops
     - Farming methods (organic, conventional, etc.)
     - Location coordinates (latitude/longitude)
   - Impact: Cannot provide personalized recommendations

3. **Communication Preferences**
   - Current: Only phone number
   - Needed:
     - Preferred communication method (SMS, Email, In-App)
     - Best time to contact
     - Language preference
   - Impact: Poor communication effectiveness

4. **Social Media Integration**
   - Current: Nothing
   - Needed: Optional social media links (similar to sponsors)
   - Impact: Limited farmer community building

5. **Validation & Data Quality**
   - Current: No validation at all
   - Needed:
     - Email format validation
     - Phone number format validation
     - Required field enforcement
     - Field length restrictions
   - Impact: Data quality issues, potential security risks

---

## 6. Technical Gaps & Risks

### Validation Gaps
```csharp
// Current: NO VALIDATION
public class UpdateUserDto : IDto
{
    public int UserId { get; set; }
    public string Email { get; set; }           // No email format check
    public string FullName { get; set; }        // No length limit
    public string MobilePhones { get; set; }    // No phone format check
    public string Address { get; set; }         // No length limit
    public string Notes { get; set; }           // No length limit
}
```

### Security Gaps
1. **No User Ownership Validation**: Any authenticated user could potentially update any other user's profile if they know the UserId
2. **No Role-Based Authorization**: Generic `SecuredOperation` doesn't enforce Farmer role
3. **No Input Sanitization**: Text fields accept any content without sanitization

### Data Quality Risks
1. **Invalid Email Addresses**: Can save malformed emails
2. **Invalid Phone Numbers**: No format enforcement
3. **Unlimited Text Length**: Can cause database issues
4. **No Business Rules**: No domain-specific validation

---

## 7. Recommended Improvements

### Priority 1: Security & Validation (CRITICAL)

**Add User Ownership Check**:
```csharp
// In UpdateUserCommandHandler
var currentUserId = _httpContextAccessor.HttpContext.User.GetUserId();
if (request.UserId != currentUserId && !IsAdmin())
{
    return new ErrorResult("You can only update your own profile");
}
```

**Add Validation**:
```csharp
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100)
            .MinimumLength(2);

        RuleFor(x => x.MobilePhones)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone format");

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
```

### Priority 2: Farmer-Specific Profile Endpoint

**Create Dedicated Farmer Profile Endpoint**:
- Route: `PUT /api/farmer/profile`
- Controller: Create `FarmerController` or add to existing controller
- DTO: `UpdateFarmerProfileDto` with farm-specific fields
- Include fields:
  - Profile picture (AvatarUrl)
  - Farm name/description
  - Location (latitude/longitude)
  - Primary crops
  - Communication preferences
  - Optional social media

### Priority 3: Profile Picture Upload

**Separate Avatar Upload Endpoint**:
- Route: `POST /api/farmer/avatar`
- Use existing file storage infrastructure (FreeImageHost/ImgBB/S3)
- Auto-generate thumbnail
- Update AvatarUrl, AvatarThumbnailUrl, AvatarUpdatedDate

---

## 8. Implementation Roadmap

### Phase 1: Security & Validation (1-2 days)
1. Add UpdateUserCommandValidator
2. Add user ownership check
3. Add role-based authorization
4. Add input sanitization

### Phase 2: Farmer Profile Enhancement (3-5 days)
1. Create UpdateFarmerProfileDto
2. Create UpdateFarmerProfileCommand
3. Add farm-specific fields to User entity or create FarmerProfile entity
4. Create dedicated farmer profile endpoint
5. Add database migration

### Phase 3: Avatar Upload (2-3 days)
1. Create avatar upload endpoint
2. Integrate with existing file storage
3. Add thumbnail generation
4. Update profile endpoint to handle avatar

### Phase 4: Testing & Documentation (1-2 days)
1. Unit tests for validation
2. Integration tests for endpoints
3. Update Postman collection
4. Update API documentation

**Total Estimate**: 7-12 days

---

## 9. Code References

### Current Implementation
- **Controller**: [WebAPI/Controllers/UsersController.cs:91](WebAPI/Controllers/UsersController.cs#L91)
- **Command**: [Business/Handlers/Users/Commands/UpdateUserCommand.cs](Business/Handlers/Users/Commands/UpdateUserCommand.cs)
- **DTO**: [Entities/Dtos/UpdateUserDto.cs](Entities/Dtos/UpdateUserDto.cs)
- **Entity**: [Core/Entities/Concrete/User.cs](Core/Entities/Concrete/User.cs)

### Sponsor Comparison
- **Controller**: [WebAPI/Controllers/SponsorshipController.cs:170](WebAPI/Controllers/SponsorshipController.cs#L170)
- **Command**: `Business/Handlers/Sponsorship/Commands/UpdateSponsorProfileCommand.cs`
- **DTO**: `Entities/Dtos/UpdateSponsorProfileDto.cs`

---

## 10. Conclusion

### Current State Summary
✅ **Working**: Basic profile update functionality exists
⚠️ **Limited**: Only 5 fields editable, no validation, generic endpoint
❌ **Missing**: Farm-specific fields, avatar upload, validation, security checks

### Gap Analysis
- **22 fewer fields** compared to sponsor profile
- **No validation** on any field
- **No dedicated endpoint** for farmers
- **Security concerns** with user ownership
- **Poor UX** without profile pictures and farm details

### Recommendation
**Implement a dedicated farmer profile endpoint with proper validation and security** to provide farmers with the same quality of profile management that sponsors currently enjoy. This will improve user experience, data quality, and system security.
