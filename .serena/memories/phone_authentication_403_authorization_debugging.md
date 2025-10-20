# Phone Authentication 403 Authorization Debugging Session

## Session Date
2025-10-04

## Issues Resolved

### 1. Phone Authentication - FullName Optional Field
**Problem:** `ReferralCode` field was required in `RegisterWithPhoneRequest`
**Root Cause:** DTO properties were not nullable
**Solution:** Made `FullName` and `ReferralCode` nullable in both DTO and Command
**Files Changed:**
- `WebAPI/Controllers/AuthController.cs` - Added `#nullable enable`, made fields nullable
- `Business/Handlers/Authorizations/Commands/RegisterWithPhoneCommand.cs` - Made properties nullable

### 2. Critical FullName NOT NULL Constraint
**Problem:** Database constraint violation when FullName was null
```
23502: null value in column "FullName" of relation "Users" violates not-null constraint
```
**Root Cause:** Database requires FullName but code made it optional
**Solution:** Generate default FullName if not provided
```csharp
var fullName = !string.IsNullOrWhiteSpace(request.FullName)
    ? request.FullName
    : $"User {normalizedPhone.Substring(normalizedPhone.Length - 4)}";
```
**Files Changed:**
- `Business/Handlers/Authorizations/Commands/VerifyPhoneRegisterCommand.cs`

### 3. Critical IsUsed Transaction Issue
**Problem:** Invalid Code error after first registration failure
**Root Cause:** 
- `IsUsed = true` was set BEFORE user creation
- When user creation failed, transaction rolled back
- But `IsUsed = true` persisted in database (orphaned state)
- Next verify attempts found `IsUsed = true` → Invalid Code

**Solution:** Move `IsUsed = true` to END of function
```csharp
// OLD (WRONG):
1. IsUsed = true
2. SaveChanges()  ← Successful
3. User create    ← FAIL
4. Transaction rollback ← IsUsed = true remained!

// NEW (CORRECT):
1. User create
2. Group assignment
3. Trial subscription
4. Referral linking
5. Token generation
6. IsUsed = true  ← ONLY after everything succeeds
7. SaveChanges()
```
**Files Changed:**
- `Business/Handlers/Authorizations/Commands/VerifyPhoneRegisterCommand.cs` (line 217-220)

### 4. Phone Normalization Consistency
**Problem:** OTP lookup failed due to phone format mismatch
**Root Cause:**
- Login stored OTP with `user.MobilePhones` (may be +905xx, 905xx, etc.)
- Verify searched with normalized phone (05xx format)
- Format mismatch → OTP not found

**Solution:** Always use normalized phone for OTP operations
**Files Changed:**
- `Business/Services/Authentication/PhoneAuthenticationProvider.cs` - Use normalizedPhone for PrepareOneTimePassword
- `Business/Handlers/Authorizations/Commands/RegisterWithPhoneCommand.cs` - Added NormalizePhoneNumber method
- `Business/Handlers/Authorizations/Commands/VerifyPhoneRegisterCommand.cs` - Added NormalizePhoneNumber method

**Normalization Logic:**
```csharp
+905321234567 → 05321234567
905321234567  → 05321234567
5321234567    → 05321234567
05321234567   → 05321234567
```

### 5. OTP Timeout Too Short
**Problem:** Users getting "Invalid Code" due to short timeout
**Root Cause:** OTP timeout was 100 seconds (1.67 minutes)
**Solution:** Changed to 5 minutes for consistency with registration flow
**Files Changed:**
- `Business/Services/Authentication/AuthenticationProviderBase.cs` (line 32)
```csharp
// OLD: m.SendDate.AddSeconds(100) > date
// NEW: m.SendDate.AddMinutes(5) > date
```

## Active Investigation

### 6. PlantAnalyses 403 Forbidden Issue
**Problem:** Phone-authenticated users getting 403 on GET `/PlantAnalyses/{id}`
**Status:** Debug logging added, awaiting test results

**Authorization Logic:**
```csharp
// PlantAnalysesController.cs:371-398
1. Get userId from token (ClaimTypes.NameIdentifier)
2. Check if Admin → Allow all
3. Check if Sponsor → Allow if sponsored
4. Check if Farmer → Allow only own analyses (result.Data.UserId == userId)
5. Otherwise → 403 Forbidden
```

**Possible Causes:**
1. Analysis belongs to different user
2. Token missing UserId claim
3. Farmer role not assigned during phone registration
4. PlantAnalysis.UserId is null or wrong

**Debug Logging Added:**
- AnalysisId, AnalysisUserId, CurrentUserId
- IsAdmin, IsSponsor flags
- All JWT claims in token
- Access denial reason with specific user IDs

**Files Changed:**
- `WebAPI/Controllers/PlantAnalysesController.cs` (line 376-396)

**Next Steps:**
1. Redeploy to Railway
2. Test with phone authentication
3. Check logs for authorization details
4. Verify Farmer group assignment
5. Check PlantAnalysis ownership

## Database Cleanup Required

**Orphaned OTP Records:**
```sql
-- Find orphaned records (IsUsed=true but no user created)
SELECT * FROM "MobileLogin"
WHERE "Provider" = 1  -- Phone
  AND "IsUsed" = true
  AND "SendDate" > NOW() - INTERVAL '1 hour'
  AND "ExternalUserId" NOT IN (
      SELECT "MobilePhones" FROM "Users" 
      WHERE "MobilePhones" IS NOT NULL
  );

-- Option 1: Delete orphaned records
DELETE FROM "MobileLogin"
WHERE "Provider" = 1
  AND "IsUsed" = true
  AND "SendDate" > NOW() - INTERVAL '1 hour'
  AND "ExternalUserId" NOT IN (SELECT "MobilePhones" FROM "Users");

-- Option 2: Reset IsUsed flag (safer)
UPDATE "MobileLogin"
SET "IsUsed" = false
WHERE "Provider" = 1
  AND "IsUsed" = true
  AND "SendDate" > NOW() - INTERVAL '1 hour';
```

## Commits
1. `e7a5f9a` - Phone authentication improvements and fullName optional field
2. `7a1389d` - Critical phone normalization fixes for OTP verification
3. `15d9879` - Make FullName and ReferralCode optional + add debug logging
4. `677239c` - Critical transaction and FullName constraint fixes
5. `21da32e` - Debug logging for PlantAnalyses authorization

## Key Learnings

### Phone Authentication Flow
1. **Register Flow:**
   - `/register-phone` → Generate OTP, save to MobileLogin
   - `/verify-phone-register` → Verify OTP, create User, assign Farmer group, create trial subscription
   - Returns JWT token with claims

2. **Login Flow:**
   - `/login-phone` → Generate OTP, save to MobileLogin
   - `/verify-phone-otp` → Verify OTP, return JWT token

### Critical Best Practices
1. **Database Constraints:** Always provide default values for NOT NULL fields
2. **Transaction Safety:** Only mark records as "used" AFTER all operations succeed
3. **Phone Normalization:** Use consistent format (05xxxxxxxxx) across all tables
4. **OTP Timeout:** 5 minutes is reasonable for user convenience
5. **Group Assignment:** Ensure phone-registered users get proper roles (Farmer)

### Token Claims Structure
```csharp
ClaimTypes.NameIdentifier → UserId (required for authorization)
ClaimTypes.Role → "Farmer", "Admin", "Sponsor"
ClaimTypes.Email → Generated from phone for phone auth
```

### Authorization Patterns
- Admin: Full access to all resources
- Sponsor: Access to sponsored resources only
- Farmer: Access to own resources only (ownership check required)

## Testing Recommendations

### Phone Registration Test
```http
POST /api/v1/Auth/register-phone
{ "mobilePhone": "05069468694" }

POST /api/v1/Auth/verify-phone-register
{ "mobilePhone": "05069468694", "code": 123456 }
```

### Phone Login Test
```http
POST /api/v1/Auth/login-phone
{ "mobilePhone": "05069468694" }

POST /api/v1/Auth/verify-phone-otp
{ "mobilePhone": "05069468694", "code": 123456 }
```

### Authorization Test
```http
GET /api/v1/PlantAnalyses/{id}
Authorization: Bearer {token}
```

## Open Questions
1. Is Farmer group being assigned correctly during phone registration?
2. Are all required claims (UserId, Role) included in phone auth tokens?
3. Does PlantAnalysis.UserId get set correctly during async analysis?
4. Should we add explicit error messages for authorization failures?
