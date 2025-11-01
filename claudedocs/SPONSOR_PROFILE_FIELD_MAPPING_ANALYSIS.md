# Sponsor Profile Field Mapping Analysis

**Analysis Date**: 2025-11-01  
**Feature Branch**: `feature/sponsor-profile-edit`  
**Base Branch**: `staging`

## Executive Summary

Complete end-to-end analysis of sponsor profile field mapping from Entity → Command → DTO → Controller.

**Critical Finding**: 🚨 **15 out of 35 entity fields are NOT mapped in create/update flow**

---

## 1. SponsorProfile Entity Fields (Total: 35 fields)

### ✅ Core Information (5/5 mapped)
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `Id` | int | ❌ Auto | ✅ Yes | Auto-generated |
| `SponsorId` | int | ✅ Yes | ✅ Yes | From auth token |
| `CompanyName` | string | ✅ Yes | ✅ Yes | **Required** |
| `CompanyDescription` | string | ✅ Yes | ✅ Yes | |
| `SponsorLogoUrl` | string | ✅ Yes | ✅ Yes | |

### ⚠️ Contact Information (3/3 mapped)
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `ContactEmail` | string | ✅ Yes | ✅ Yes | Updates User.Email |
| `ContactPhone` | string | ✅ Yes | ✅ Yes | |
| `ContactPerson` | string | ✅ Yes | ✅ Yes | |

### ❌ Social Media Links (0/4 mapped) - **MISSING**
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `LinkedInUrl` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `TwitterUrl` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `FacebookUrl` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `InstagramUrl` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |

### ❌ Business Information (1/6 mapped) - **MOSTLY MISSING**
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `WebsiteUrl` | string | ✅ Yes | ✅ Yes | |
| `TaxNumber` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `TradeRegistryNumber` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `Address` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `City` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `Country` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |
| `PostalCode` | string | ❌ **NO** | ❌ **NO** | **NOT MAPPED** |

### ✅ Company Features (2/2 mapped)
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `CompanyType` | string | ✅ Yes | ✅ Yes | Default: "Agriculture" |
| `BusinessModel` | string | ✅ Yes | ✅ Yes | Default: "B2B" |

### ⚠️ Verification & Status (4/5 read-only)
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `IsVerifiedCompany` | bool | ⚙️ Set to false | ✅ Yes | System-controlled |
| `IsActive` | bool | ⚙️ Set to true | ✅ Yes | System-controlled |
| `IsVerified` | bool | ❌ NO | ❌ NO | Admin-only field |
| `VerificationDate` | DateTime? | ❌ NO | ❌ NO | Admin-only field |
| `VerificationNotes` | string | ❌ NO | ❌ NO | Admin-only field |

### ⚠️ Statistics (4/4 read-only) - Calculated fields
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `TotalPurchases` | int | ⚙️ Set to 0 | ✅ Yes | Auto-calculated |
| `TotalCodesGenerated` | int | ⚙️ Set to 0 | ✅ Yes | Auto-calculated |
| `TotalCodesRedeemed` | int | ⚙️ Set to 0 | ✅ Yes | Auto-calculated |
| `TotalInvestment` | decimal | ⚙️ Set to 0 | ✅ Yes | Auto-calculated |

### ⚠️ Audit Fields (6/6 system-managed)
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `CreatedDate` | DateTime | ⚙️ DateTime.Now | ✅ Yes | System-managed |
| `UpdatedDate` | DateTime? | ❌ NO | ✅ Yes | Should be set on update |
| `CreatedByUserId` | int? | ❌ NO | ❌ NO | System-managed |
| `UpdatedByUserId` | int? | ❌ NO | ❌ NO | Should be set on update |

### ℹ️ Special Fields
| Field | Type | Mapped in Command | Mapped in DTO | Notes |
|-------|------|------------------|---------------|-------|
| `Password` | string | ✅ Yes | ✅ Yes | **Only in Create DTO** - Updates User table |

---

## 2. Missing Field Summary

### 🔴 Critical Missing Fields (User-editable)
These fields exist in the entity but are **NOT** available for users to fill during profile creation/update:

1. **Social Media** (4 fields):
   - `LinkedInUrl`
   - `TwitterUrl`
   - `FacebookUrl`
   - `InstagramUrl`

2. **Business Details** (6 fields):
   - `TaxNumber`
   - `TradeRegistryNumber`
   - `Address`
   - `City`
   - `Country`
   - `PostalCode`

**Total Missing: 10 fields**

### 🟡 System-Managed Fields (Should NOT be user-editable)
These fields are intentionally excluded from user input:

- `IsVerified`, `VerificationDate`, `VerificationNotes` (Admin-only)
- `TotalPurchases`, `TotalCodesGenerated`, `TotalCodesRedeemed`, `TotalInvestment` (Calculated)
- `CreatedByUserId`, `UpdatedByUserId` (System audit)

---

## 3. Current Command/DTO Structure

### CreateSponsorProfileCommand (11 fields)
```csharp
public class CreateSponsorProfileCommand : IRequest<IResult>
{
    public int SponsorId { get; set; }              // ✅ From token
    public string CompanyName { get; set; }          // ✅
    public string CompanyDescription { get; set; }   // ✅
    public string SponsorLogoUrl { get; set; }       // ✅
    public string WebsiteUrl { get; set; }           // ✅
    public string ContactEmail { get; set; }         // ✅
    public string ContactPhone { get; set; }         // ✅
    public string ContactPerson { get; set; }        // ✅
    public string CompanyType { get; set; }          // ✅
    public string BusinessModel { get; set; }        // ✅
    public string Password { get; set; }             // ✅ Special
}
```

### CreateSponsorProfileDto (10 fields)
```csharp
public class CreateSponsorProfileDto : IDto
{
    public string CompanyName { get; set; }          // ✅
    public string CompanyDescription { get; set; }   // ✅
    public string SponsorLogoUrl { get; set; }       // ✅
    public string WebsiteUrl { get; set; }           // ✅
    public string ContactEmail { get; set; }         // ✅
    public string ContactPhone { get; set; }         // ✅
    public string ContactPerson { get; set; }        // ✅
    public string CompanyType { get; set; }          // ✅
    public string BusinessModel { get; set; }        // ✅
    public string Password { get; set; }             // ✅
}
```

### SponsorProfileDto (Response - 20 fields)
```csharp
public class SponsorProfileDto : IDto
{
    public int Id { get; set; }
    public int SponsorId { get; set; }
    public string CompanyName { get; set; }
    public string CompanyDescription { get; set; }
    public string SponsorLogoUrl { get; set; }
    public string WebsiteUrl { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }
    public string ContactPerson { get; set; }
    public string CompanyType { get; set; }
    public string BusinessModel { get; set; }
    public bool IsVerifiedCompany { get; set; }
    public bool IsActive { get; set; }
    public int TotalPurchases { get; set; }
    public int TotalCodesGenerated { get; set; }
    public int TotalCodesRedeemed { get; set; }
    public decimal TotalInvestment { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
```

**Missing in Response DTO**: Social media links, business address fields, tax numbers, verification fields

---

## 4. Update Flow Analysis

### ❌ **NO UPDATE COMMAND EXISTS**

Current state:
- ✅ `CreateSponsorProfileCommand` - Creates new profile
- ❌ **Missing**: `UpdateSponsorProfileCommand` - Does NOT exist
- ✅ `GetSponsorProfileQuery` - Retrieves profile

### Current Controller Endpoints

**File**: `WebAPI/Controllers/SponsorshipController.cs`

#### 1. Create Profile (Line 102)
```csharp
[HttpPost("create-profile")]
[Authorize(Roles = "Farmer,Admin")]
public async Task<IActionResult> CreateSponsorProfile([FromBody] CreateSponsorProfileDto dto)
```
- ✅ Uses `CreateSponsorProfileCommand`
- ✅ Requires Farmer or Admin role
- ✅ Maps 10 fields from DTO to Command
- ⚠️ Missing 10 business/social fields

#### 2. Get Profile (Line 841)
```csharp
[HttpGet("profile")]
[Authorize(Roles = "Sponsor,Admin")]
public async Task<IActionResult> GetSponsorProfile()
```
- ✅ Uses `GetSponsorProfileQuery`
- ✅ Returns `SponsorProfileDto` (20 fields)
- ⚠️ Response missing 15 entity fields

#### 3. Create or Update Profile (Line 863)
```csharp
[HttpPost("profile")]
[Authorize(Roles = "Sponsor,Admin")]
public async Task<IActionResult> CreateOrUpdateSponsorProfile([FromBody] CreateSponsorProfileCommand command)
```
- ⚠️ **Misleading name** - Only creates, doesn't update
- ✅ Uses same `CreateSponsorProfileCommand`
- ❌ Will fail if profile already exists: `Messages.SponsorProfileAlreadyExists`
- ❌ No actual update logic

---

## 5. Issues & Gaps

### 🚨 Critical Issues

1. **No Update Functionality**
   - Endpoint named "CreateOrUpdate" but only creates
   - Returns error if profile exists
   - No way to update existing profile

2. **Missing User-Editable Fields**
   - 10 important business fields not available
   - Social media links cannot be set
   - Address/tax information cannot be provided

3. **Incomplete Response DTO**
   - Social media URLs not returned in GET response
   - Business address fields not returned
   - Tax/registry numbers not returned

### ⚠️ Design Issues

1. **Update Logic Needed**
   - Need `UpdateSponsorProfileCommand`
   - Need `UpdateSponsorProfileDto`
   - Need proper `PUT` endpoint

2. **Field Validation**
   - No validation for email format
   - No validation for phone format
   - No validation for URL formats

3. **Audit Trail**
   - `UpdatedDate` not being set
   - `UpdatedByUserId` not being tracked

---

## 6. Recommendations

### Immediate Actions

1. **Create Update Command**
   ```csharp
   UpdateSponsorProfileCommand
   - Include all 10 missing user-editable fields
   - Set UpdatedDate = DateTime.Now
   - Set UpdatedByUserId = current user
   - Handle partial updates (PATCH semantics)
   ```

2. **Add Missing Fields to DTOs**
   ```csharp
   UpdateSponsorProfileDto / CreateSponsorProfileDto:
   + LinkedInUrl
   + TwitterUrl  
   + FacebookUrl
   + InstagramUrl
   + TaxNumber
   + TradeRegistryNumber
   + Address
   + City
   + Country
   + PostalCode
   ```

3. **Fix Controller Endpoint**
   ```csharp
   [HttpPut("profile")]  // Change from POST
   public async Task<IActionResult> UpdateSponsorProfile([FromBody] UpdateSponsorProfileDto dto)
   {
       // Use UpdateSponsorProfileCommand
       // Allow updating existing profile
   }
   ```

4. **Complete Response DTO**
   - Add all missing fields to `SponsorProfileDto`
   - Return complete profile information

### Implementation Priority

**Phase 1: Critical (Immediate)**
- ✅ Create `UpdateSponsorProfileCommand`
- ✅ Create `UpdateSponsorProfileDto`
- ✅ Add `PUT /api/v1/sponsorship/profile` endpoint
- ✅ Add missing fields to DTOs

**Phase 2: Enhancement**
- Add field validation rules
- Implement audit trail (UpdatedDate, UpdatedByUserId)
- Add PATCH support for partial updates
- Add file upload for logo

**Phase 3: Nice-to-Have**
- Email verification for ContactEmail changes
- Phone verification for ContactPhone changes
- Address validation/geocoding
- Tax number format validation

---

## 7. Proposed Solution Structure

### New Files to Create

1. **`Business/Handlers/SponsorProfiles/Commands/UpdateSponsorProfileCommand.cs`**
2. **`Entities/Dtos/UpdateSponsorProfileDto.cs`**
3. **`Business/Handlers/SponsorProfiles/ValidationRules/UpdateSponsorProfileValidator.cs`**

### Files to Modify

1. **`Entities/Dtos/SponsorProfileDto.cs`** - Add missing response fields
2. **`Entities/Dtos/CreateSponsorProfileDto.cs`** - Add missing input fields
3. **`WebAPI/Controllers/SponsorshipController.cs`** - Add PUT endpoint
4. **`Business/Handlers/SponsorProfiles/Commands/CreateSponsorProfileCommand.cs`** - Add missing fields

---

## 8. Field Mapping Matrix

| Entity Field | Create Command | Create DTO | Response DTO | Update Command | Update DTO | Notes |
|--------------|----------------|------------|--------------|----------------|------------|-------|
| Id | ❌ | ❌ | ✅ | ❌ | ❌ | Auto-generated |
| SponsorId | ✅ | ❌ | ✅ | ✅ | ❌ | From token |
| CompanyName | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| CompanyDescription | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| SponsorLogoUrl | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| WebsiteUrl | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| ContactEmail | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| ContactPhone | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| ContactPerson | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| LinkedInUrl | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| TwitterUrl | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| FacebookUrl | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| InstagramUrl | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| TaxNumber | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| TradeRegistryNumber | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| Address | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| City | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| Country | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| PostalCode | ❌ | ❌ | ❌ | ❌ | ❌ | **MISSING** |
| CompanyType | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| BusinessModel | ✅ | ✅ | ✅ | ❌ | ❌ | **NEED TO ADD** |
| IsVerifiedCompany | ⚙️ | ❌ | ✅ | ❌ | ❌ | Read-only |
| IsActive | ⚙️ | ❌ | ✅ | ❌ | ❌ | Read-only |
| Password | ✅ | ✅ | ❌ | ❌ | ❌ | Special case |

**Legend**:
- ✅ = Mapped and working
- ❌ = Not mapped / Missing
- ⚙️ = System-set (not user input)

---

## Conclusion

The sponsor profile system currently supports only **11 out of 35** entity fields for user input. An update mechanism does not exist, and 10 important business fields (social media, address, tax info) are completely missing from the flow.

**Next Steps**: Implement `UpdateSponsorProfileCommand` with all missing fields and create proper PUT endpoint.
