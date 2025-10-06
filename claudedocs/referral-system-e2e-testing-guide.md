# Referral System - End-to-End Testing Guide

## Table of Contents
1. [Test Environment Setup](#test-environment-setup)
2. [Test Scenarios](#test-scenarios)
3. [Happy Path Testing](#happy-path-testing)
4. [Edge Cases & Error Scenarios](#edge-cases--error-scenarios)
5. [Database Verification](#database-verification)
6. [Performance Testing](#performance-testing)
7. [Integration Testing](#integration-testing)

---

## Test Environment Setup

### Prerequisites

1. **Database:**
   - PostgreSQL 13+ running
   - ZiraAI database with referral system migrations applied
   - Test data seeded (2 users minimum)

2. **API Server:**
   - .NET 9.0 WebAPI running
   - Staging environment: `https://ziraai-staging.up.railway.app`
   - Local environment: `https://localhost:5001`

3. **Tools:**
   - Postman (recommended) or cURL
   - PostgreSQL client (pgAdmin/psql)
   - JWT token for authenticated requests

### Environment Variables

```bash
# Staging
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__DArchPgContext=<staging-db-connection>

# Configuration
Referral_Enabled=true
Referral_CreditsPerReferral=10
Referral_LinkExpiryDays=30
```

### Test Users Setup

```sql
-- Create test users
INSERT INTO "Users" ("Email", "FullName", "MobilePhones", "PasswordHash", "PasswordSalt", "Status", "CitizenId", "Address", "Notes", "AuthenticationProviderType", "RecordDate", "UpdateContactDate")
VALUES 
('referrer@test.com', 'Test Referrer', '05321111111', 'hash1', 'salt1', true, 0, 'Test Address', 'Test User', 'Person', NOW(), NOW()),
('referee@test.com', 'Test Referee', '05322222222', 'hash2', 'salt2', true, 0, 'Test Address', 'Test User', 'Person', NOW(), NOW());

-- Assign Farmer role
INSERT INTO "UserGroups" ("UserId", "GroupId")
SELECT u."UserId", g."Id"
FROM "Users" u, "Groups" g
WHERE u."Email" IN ('referrer@test.com', 'referee@test.com')
AND g."GroupName" = 'Farmer';

-- Create active subscription for referrer (required for rewards)
INSERT INTO "UserSubscriptions" ("UserId", "SubscriptionTierId", "StartDate", "EndDate", "IsActive", "AutoRenew", "PaymentMethod", "PaidAmount", "Currency", "CurrentDailyUsage", "CurrentMonthlyUsage", "LastUsageResetDate", "MonthlyUsageResetDate", "Status", "ReferralCredits", "CreatedDate", "CreatedUserId")
SELECT 
    u."UserId",
    (SELECT "Id" FROM "SubscriptionTiers" WHERE "TierName" = 'Small' LIMIT 1),
    NOW(),
    NOW() + INTERVAL '30 days',
    true,
    false,
    'Test',
    0,
    'TRY',
    0,
    0,
    NOW(),
    NOW(),
    'Active',
    0,
    NOW(),
    u."UserId"
FROM "Users" u
WHERE u."Email" = 'referrer@test.com';
```

### Get JWT Tokens

**Login as Referrer:**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "referrer@test.com",
    "password": "Test123!"
  }'
```

**Save the token:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "expiration": "2025-10-03T12:00:00"
  }
}
```

**Environment Variable:**
```bash
export REFERRER_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## Test Scenarios

### Scenario Matrix

| Test Case | Description | Expected Result | Priority |
|-----------|-------------|-----------------|----------|
| TS-001 | Generate referral link (SMS only) | Link created, SMS sent | P0 |
| TS-002 | Generate referral link (WhatsApp only) | Link created, WhatsApp sent | P0 |
| TS-003 | Generate referral link (Hybrid: SMS+WhatsApp) | Link created, both sent | P0 |
| TS-004 | Track referral click (valid code) | Click tracked | P0 |
| TS-005 | Track referral click (expired code) | Error: code expired | P1 |
| TS-006 | Track referral click (duplicate) | Success but not tracked | P1 |
| TS-007 | Validate referral code (valid) | Code is valid | P0 |
| TS-008 | Validate referral code (invalid) | Error: code not found | P1 |
| TS-009 | Register with referral code (email) | User created, tracking linked | P0 |
| TS-009A | Register with referral code (phone - OTP) | User created, tracking linked | P0 |
| TS-010 | Register with self-referral (email) | Error: cannot self-refer | P1 |
| TS-010A | Register with self-referral (phone - OTP) | Error: cannot self-refer | P1 |
| TS-010B | Duplicate phone registration | Error: already registered | P1 |
| TS-010C | Expired OTP during registration | Error: expired OTP | P1 |
| TS-011 | First analysis triggers validation | Tracking validated | P0 |
| TS-012 | Reward processed automatically | Credits added to referrer | P0 |
| TS-013 | Get referral statistics | Stats returned correctly | P1 |
| TS-014 | Get user referral codes | Codes list returned | P1 |
| TS-015 | Get credit breakdown | Breakdown returned | P1 |
| TS-016 | Disable referral code | Code disabled | P1 |
| TS-017 | Generate 50+ phone numbers | Max limit enforced | P2 |
| TS-018 | Invalid phone format | Validation error | P2 |

---

## Happy Path Testing

### Test Case TS-001: Generate Referral Link (SMS Only)

**Step 1: Generate Link**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $REFERRER_TOKEN" \
  -d '{
    "deliveryMethod": 1,
    "phoneNumbers": ["05321111111"],
    "customMessage": "ZiraAI test referral message"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Referral links generated and sent successfully",
  "data": {
    "referralCode": "ZIRA-A3B7K9",
    "deepLink": "ziraai://referral?code=ZIRA-A3B7K9",
    "playStoreLink": "https://play.google.com/store/apps/details?id=com.ziraai&referrer=ZIRA-A3B7K9",
    "expiresAt": "2025-11-02T10:30:00",
    "deliveryStatuses": [
      {
        "phoneNumber": "05321111111",
        "method": "SMS",
        "status": "Sent",
        "errorMessage": null
      }
    ]
  }
}
```

**Verification:**
```sql
-- Check ReferralCodes table
SELECT * FROM "ReferralCodes" 
WHERE "Code" = 'ZIRA-A3B7K9';

-- Expected:
-- UserId: <referrer_user_id>
-- IsActive: true
-- Status: 0 (Active)
-- ExpiresAt: ~30 days from now
```

**Save the code for next steps:**
```bash
export REFERRAL_CODE="ZIRA-A3B7K9"
```

---

### Test Case TS-003: Generate Referral Link (Hybrid: SMS+WhatsApp)

**Step 1: Generate Link**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $REFERRER_TOKEN" \
  -d '{
    "deliveryMethod": 3,
    "phoneNumbers": ["05323333333", "05324444444"],
    "customMessage": "Hybrid delivery test"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "referralCode": "ZIRA-X9Y2Z3",
    "deliveryStatuses": [
      {
        "phoneNumber": "05323333333",
        "method": "SMS",
        "status": "Sent",
        "errorMessage": null
      },
      {
        "phoneNumber": "05323333333",
        "method": "WhatsApp",
        "status": "Sent",
        "errorMessage": null
      },
      {
        "phoneNumber": "05324444444",
        "method": "SMS",
        "status": "Sent",
        "errorMessage": null
      },
      {
        "phoneNumber": "05324444444",
        "method": "WhatsApp",
        "status": "Sent",
        "errorMessage": null
      }
    ]
  }
}
```

**Verification:**
- Total delivery statuses: 4 (2 phones × 2 methods)
- All statuses: "Sent"

---

### Test Case TS-004: Track Referral Click (Valid Code)

**Step 1: Track Click (Public Endpoint - No Auth)**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/track-click \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ZIRA-A3B7K9",
    "ipAddress": "192.168.1.100",
    "deviceId": "test-device-uuid-001"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Click tracked successfully"
}
```

**Verification:**
```sql
-- Check ReferralTracking table
SELECT * FROM "ReferralTracking" 
WHERE "ReferralCodeId" = (
    SELECT "Id" FROM "ReferralCodes" WHERE "Code" = 'ZIRA-A3B7K9'
);

-- Expected:
-- IpAddress: 192.168.1.100
-- DeviceId: test-device-uuid-001
-- ClickedAt: ~now
-- Status: 0 (Clicked)
-- RefereeUserId: NULL (not registered yet)
```

---

### Test Case TS-007: Validate Referral Code (Valid)

**Step 1: Validate Code (Public Endpoint - No Auth)**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/validate \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ZIRA-A3B7K9"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Referral code is valid and active"
}
```

---

### Test Case TS-009: Register with Referral Code

**Step 1: Register New User**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newreferee@test.com",
    "password": "Test123!",
    "fullName": "New Referee User",
    "mobilePhones": "05325555555",
    "referralCode": "ZIRA-A3B7K9"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "User registered successfully"
}
```

**Verification:**
```sql
-- Check Users table
SELECT "UserId", "Email", "FullName", "RegistrationReferralCode" 
FROM "Users" 
WHERE "Email" = 'newreferee@test.com';

-- Expected:
-- RegistrationReferralCode: ZIRA-A3B7K9

-- Check ReferralTracking table
SELECT * FROM "ReferralTracking" 
WHERE "RefereeUserId" = (
    SELECT "UserId" FROM "Users" WHERE "Email" = 'newreferee@test.com'
);

-- Expected:
-- Status: 1 (Registered)
-- RegisteredAt: ~now
-- ClickedAt: <from previous click>
```

**Get referee token:**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newreferee@test.com",
    "password": "Test123!"
  }'

export REFEREE_TOKEN="<token_from_response>"
```

---

### Test Case TS-009A: Register with Referral Code (Phone - OTP)

**Step 1: Request OTP for Registration (with Referral Code)**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/register-phone \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05327777777",
    "fullName": "Phone Referee User",
    "referralCode": "ZIRA-A3B7K9"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "OTP sent to 05327777777. Code: 123456 (dev mode)"
}
```

**Console Output (Development):**
```
[RegisterWithPhone] Registration OTP requested for phone: 05327777777
[RegisterWithPhone] Phone number is unique, proceeding with registration...
[RegisterWithPhone] OTP generated and saved for phone: 05327777777, Code: 123456
```

**Save OTP code:**
```bash
export OTP_CODE="123456"
```

---

**Step 2: Verify OTP and Complete Registration**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/verify-phone-register \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05327777777",
    "code": 123456,
    "fullName": "Phone Referee User",
    "referralCode": "ZIRA-A3B7K9"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "expiration": "2025-10-03T13:00:00Z",
    "claims": [
      {
        "id": 1,
        "name": "GetPlantAnalyses"
      }
    ]
  }
}
```

**Verification - User Created:**
```sql
-- Check Users table
SELECT "UserId", "Email", "MobilePhones", "FullName", "RegistrationReferralCode"
FROM "Users"
WHERE "MobilePhones" = '05327777777';

-- Expected:
-- Email: 05327777777@phone.ziraai.com
-- RegistrationReferralCode: ZIRA-A3B7K9
-- PasswordHash: empty array (no password)
-- AuthenticationProviderType: Phone
```

**Verification - Referral Linked:**
```sql
-- Check ReferralTracking table
SELECT * FROM "ReferralTracking"
WHERE "RefereeUserId" = (
    SELECT "UserId" FROM "Users" WHERE "MobilePhones" = '05327777777'
);

-- Expected:
-- Status: 1 (Registered)
-- RegisteredAt: ~now
-- ClickedAt: <from previous click>
-- ReferralCodeId: matches ZIRA-A3B7K9
```

**Save referee token:**
```bash
export PHONE_REFEREE_TOKEN="<token_from_response>"
```

---

### Test Case TS-011 & TS-012: First Analysis → Validation → Reward

**Step 1: Perform First Plant Analysis (as Referee)**

```bash
curl -X POST https://ziraai-staging.up.railway.app/api/v1/plantanalyses/analyze \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $REFEREE_TOKEN" \
  -d '{
    "image": "<base64_encoded_image>",
    "cropType": "Tomato",
    "location": "Test Farm"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Analysis completed successfully",
  "data": {
    "id": 123,
    "analysisId": "guid-here",
    "plantSpecies": "Tomato",
    "healthAssessment": {...}
  }
}
```

**Verification - Tracking Status Updated:**
```sql
-- Check tracking status
SELECT 
    rt."Status",
    rt."ClickedAt",
    rt."RegisteredAt",
    rt."ValidatedAt",
    rt."RewardProcessedAt"
FROM "ReferralTracking" rt
WHERE rt."RefereeUserId" = (
    SELECT "UserId" FROM "Users" WHERE "Email" = 'newreferee@test.com'
);

-- Expected:
-- Status: 3 (Rewarded)
-- ValidatedAt: ~now
-- RewardProcessedAt: ~now
```

**Verification - Reward Created:**
```sql
-- Check ReferralRewards table
SELECT 
    rr."ReferrerUserId",
    rr."RefereeUserId",
    rr."CreditAmount",
    rr."AwardedAt",
    u."Email" AS ReferrerEmail
FROM "ReferralRewards" rr
JOIN "Users" u ON rr."ReferrerUserId" = u."UserId"
WHERE rr."RefereeUserId" = (
    SELECT "UserId" FROM "Users" WHERE "Email" = 'newreferee@test.com'
);

-- Expected:
-- ReferrerEmail: referrer@test.com
-- CreditAmount: 10
-- AwardedAt: ~now
```

**Verification - Credits Added to Referrer:**
```sql
-- Check subscription credits
SELECT 
    u."Email",
    us."ReferralCredits"
FROM "UserSubscriptions" us
JOIN "Users" u ON us."UserId" = u."UserId"
WHERE u."Email" = 'referrer@test.com'
AND us."IsActive" = true;

-- Expected:
-- ReferralCredits: 10 (or +10 from previous value)
```

---

### Test Case TS-013: Get Referral Statistics

**Step 1: Get Stats (as Referrer)**

```bash
curl -X GET https://ziraai-staging.up.railway.app/api/referral/stats \
  -H "Authorization: Bearer $REFERRER_TOKEN"
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalReferrals": 1,
    "successfulReferrals": 1,
    "pendingReferrals": 0,
    "totalCreditsEarned": 10,
    "referralBreakdown": {
      "clicked": 1,
      "registered": 1,
      "validated": 1,
      "rewarded": 1
    }
  }
}
```

---

### Test Case TS-014: Get User Referral Codes

**Step 1: Get Codes (as Referrer)**

```bash
curl -X GET https://ziraai-staging.up.railway.app/api/referral/codes \
  -H "Authorization: Bearer $REFERRER_TOKEN"
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "code": "ZIRA-A3B7K9",
      "isActive": true,
      "createdAt": "2025-10-03T10:30:00",
      "expiresAt": "2025-11-02T10:30:00",
      "status": 0,
      "statusText": "Active",
      "usageCount": 0
    }
  ]
}
```

---

### Test Case TS-015: Get Credit Breakdown

**Step 1: Get Breakdown (as Referrer)**

```bash
curl -X GET https://ziraai-staging.up.railway.app/api/referral/credits \
  -H "Authorization: Bearer $REFERRER_TOKEN"
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalEarned": 10,
    "totalUsed": 0,
    "currentBalance": 10
  }
}
```

---

### Test Case TS-016: Disable Referral Code

**Step 1: Disable Code (as Referrer)**

```bash
curl -X DELETE https://ziraai-staging.up.railway.app/api/referral/disable/ZIRA-A3B7K9 \
  -H "Authorization: Bearer $REFERRER_TOKEN"
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Referral code disabled successfully"
}
```

**Verification:**
```sql
-- Check status updated
SELECT "Code", "IsActive", "Status" 
FROM "ReferralCodes" 
WHERE "Code" = 'ZIRA-A3B7K9';

-- Expected:
-- IsActive: false
-- Status: 2 (Disabled)
```

---

## Edge Cases & Error Scenarios

### Test Case TS-005: Track Click with Expired Code

**Setup:**
```sql
-- Create expired code
INSERT INTO "ReferralCodes" ("UserId", "Code", "IsActive", "CreatedAt", "ExpiresAt", "Status")
VALUES (
    (SELECT "UserId" FROM "Users" WHERE "Email" = 'referrer@test.com'),
    'ZIRA-EXPIR1',
    true,
    NOW() - INTERVAL '40 days',
    NOW() - INTERVAL '10 days',
    0
);
```

**Test:**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/track-click \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ZIRA-EXPIR1",
    "ipAddress": "192.168.1.100",
    "deviceId": "test-device-uuid-001"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Referral code has expired"
}
```

---

### Test Case TS-006: Track Duplicate Click

**Step 1: First Click**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/track-click \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ZIRA-A3B7K9",
    "ipAddress": "192.168.1.200",
    "deviceId": "duplicate-device"
  }'
```

**Expected:** 200 OK, tracking created

**Step 2: Duplicate Click (within 24h, same IP+Device)**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/track-click \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ZIRA-A3B7K9",
    "ipAddress": "192.168.1.200",
    "deviceId": "duplicate-device"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Click tracked successfully"
}
```

**Verification:**
```sql
-- Should only have 1 record for this IP+Device
SELECT COUNT(*) 
FROM "ReferralTracking" 
WHERE "IpAddress" = '192.168.1.200' 
AND "DeviceId" = 'duplicate-device';

-- Expected: 1 (not 2)
```

---

### Test Case TS-008: Validate Invalid Code

**Test:**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/validate \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ZIRA-INVALID"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Referral code not found"
}
```

---

### Test Case TS-010: Self-Referral Prevention

**Test:**
```bash
# Register with own referral code
curl -X POST https://ziraai-staging.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "selfreferrer@test.com",
    "password": "Test123!",
    "fullName": "Self Referrer",
    "mobilePhones": "05326666666",
    "referralCode": "<referrer_own_code>"
  }'
```

**Expected Behavior:**
- User created successfully (registration doesn't fail)
- Referral tracking NOT linked
- Console logs: "Self-referral attempt blocked"

**Verification:**
```sql
-- Check no tracking created
SELECT COUNT(*) 
FROM "ReferralTracking" 
WHERE "RefereeUserId" = (
    SELECT "UserId" FROM "Users" WHERE "Email" = 'selfreferrer@test.com'
);

-- Expected: 0
```

---

### Test Case TS-010B: Duplicate Phone Registration

**Step 1: Complete First Registration**
```bash
# Request OTP
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/register-phone \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05328888888",
    "fullName": "First User"
  }'

# Verify OTP
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/verify-phone-register \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05328888888",
    "code": 123456,
    "fullName": "First User"
  }'
```

**Expected:** 200 OK, user created

---

**Step 2: Try to Register Again with Same Phone (Request OTP)**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/register-phone \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05328888888",
    "fullName": "Second User"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Phone number is already registered"
}
```

---

**Step 3: Try to Verify with Same Phone (Without OTP Request)**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/verify-phone-register \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05328888888",
    "code": 999999,
    "fullName": "Second User"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Phone number is already registered"
}
```

**Verification:**
```sql
-- Should only have 1 user with this phone
SELECT COUNT(*) FROM "Users" WHERE "MobilePhones" = '05328888888';
-- Expected: 1
```

---

### Test Case TS-010C: Expired OTP During Registration

**Step 1: Request OTP**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/register-phone \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05329999999",
    "fullName": "Expired OTP User"
  }'
```

**Expected:** 200 OK, OTP generated

---

**Step 2: Wait 6 Minutes (OTP Expires After 5 Minutes)**
```bash
sleep 360
```

---

**Step 3: Try to Verify with Expired OTP**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/v1/auth/verify-phone-register \
  -H "Content-Type: application/json" \
  -d '{
    "mobilePhone": "05329999999",
    "code": 123456,
    "fullName": "Expired OTP User"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "OTP code has expired"
}
```

**Verification:**
```sql
-- Check MobileLogin record
SELECT
    "ExternalUserId",
    "Code",
    "SendDate",
    "IsUsed",
    EXTRACT(EPOCH FROM (NOW() - "SendDate"))/60 AS MinutesOld
FROM "MobileLogins"
WHERE "ExternalUserId" = '05329999999'
ORDER BY "SendDate" DESC
LIMIT 1;

-- Expected: MinutesOld > 5
```

---

### Test Case TS-017: Max Phone Numbers Validation

**Test:**
```bash
# Generate with 51 phone numbers (over limit)
curl -X POST https://ziraai-staging.up.railway.app/api/referral/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $REFERRER_TOKEN" \
  -d '{
    "deliveryMethod": 1,
    "phoneNumbers": ["05321111111", "05322222222", ... (51 total)]
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Maximum 50 phone numbers allowed per request"
  ]
}
```

---

### Test Case TS-018: Invalid Phone Format

**Test:**
```bash
curl -X POST https://ziraai-staging.up.railway.app/api/referral/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $REFERRER_TOKEN" \
  -d '{
    "deliveryMethod": 1,
    "phoneNumbers": ["1234567890", "+905321234567", "invalid"]
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Phone number must start with 0 and contain only digits",
    "Phone number must be 10-11 digits (Turkish format: 05321234567)"
  ]
}
```

---

## Database Verification

### Complete Referral Journey Verification

```sql
-- Full journey query
WITH ReferralJourney AS (
    SELECT 
        rc."Code",
        u_referrer."Email" AS ReferrerEmail,
        u_referee."Email" AS RefereeEmail,
        rt."Status",
        rt."ClickedAt",
        rt."RegisteredAt",
        rt."ValidatedAt",
        rt."RewardProcessedAt",
        rr."CreditAmount",
        us."ReferralCredits" AS ReferrerCurrentCredits
    FROM "ReferralCodes" rc
    JOIN "Users" u_referrer ON rc."UserId" = u_referrer."UserId"
    LEFT JOIN "ReferralTracking" rt ON rc."Id" = rt."ReferralCodeId"
    LEFT JOIN "Users" u_referee ON rt."RefereeUserId" = u_referee."UserId"
    LEFT JOIN "ReferralRewards" rr ON rt."Id" = rr."ReferralTrackingId"
    LEFT JOIN "UserSubscriptions" us ON u_referrer."UserId" = us."UserId" AND us."IsActive" = true
    WHERE rc."Code" = 'ZIRA-A3B7K9'
)
SELECT * FROM ReferralJourney;
```

**Expected Output:**
| Code | ReferrerEmail | RefereeEmail | Status | ClickedAt | RegisteredAt | ValidatedAt | RewardProcessedAt | CreditAmount | ReferrerCurrentCredits |
|------|---------------|--------------|--------|-----------|--------------|-------------|-------------------|--------------|------------------------|
| ZIRA-A3B7K9 | referrer@test.com | newreferee@test.com | 3 | 2025-10-03 10:00 | 2025-10-03 10:05 | 2025-10-03 10:10 | 2025-10-03 10:10 | 10 | 10 |

---

### Analytics Queries

**Conversion Funnel:**
```sql
SELECT 
    COUNT(CASE WHEN "Status" >= 0 THEN 1 END) AS Clicks,
    COUNT(CASE WHEN "Status" >= 1 THEN 1 END) AS Registrations,
    COUNT(CASE WHEN "Status" >= 2 THEN 1 END) AS Validations,
    COUNT(CASE WHEN "Status" >= 3 THEN 1 END) AS Rewards,
    ROUND(
        COUNT(CASE WHEN "Status" >= 1 THEN 1 END)::NUMERIC / 
        NULLIF(COUNT(CASE WHEN "Status" >= 0 THEN 1 END), 0) * 100, 
        2
    ) AS ClickToRegisterRate,
    ROUND(
        COUNT(CASE WHEN "Status" >= 3 THEN 1 END)::NUMERIC / 
        NULLIF(COUNT(CASE WHEN "Status" >= 1 THEN 1 END), 0) * 100, 
        2
    ) AS RegisterToRewardRate
FROM "ReferralTracking";
```

**Top Referrers:**
```sql
SELECT 
    u."Email",
    u."FullName",
    COUNT(rr."Id") AS TotalRewards,
    SUM(rr."CreditAmount") AS TotalCredits,
    us."ReferralCredits" AS CurrentBalance
FROM "Users" u
LEFT JOIN "ReferralRewards" rr ON u."UserId" = rr."ReferrerUserId"
LEFT JOIN "UserSubscriptions" us ON u."UserId" = us."UserId" AND us."IsActive" = true
GROUP BY u."UserId", u."Email", u."FullName", us."ReferralCredits"
HAVING COUNT(rr."Id") > 0
ORDER BY TotalCredits DESC;
```

---

## Performance Testing

### Load Test Scenario

**Tool:** Apache Bench or k6

**Scenario:** 100 concurrent requests to generate referral links

```bash
# Using Apache Bench
ab -n 100 -c 10 -T 'application/json' -H "Authorization: Bearer $REFERRER_TOKEN" \
   -p generate_payload.json \
   https://ziraai-staging.up.railway.app/api/referral/generate

# generate_payload.json
{
  "deliveryMethod": 1,
  "phoneNumbers": ["05321111111"],
  "customMessage": "Load test"
}
```

**Expected:**
- Requests per second: >50
- Average response time: <500ms
- Error rate: 0%
- All codes unique

**Verification:**
```sql
-- Check for duplicate codes (should be 0)
SELECT "Code", COUNT(*) 
FROM "ReferralCodes" 
GROUP BY "Code" 
HAVING COUNT(*) > 1;
```

---

### Database Performance

**Index Usage Verification:**
```sql
-- Check if indexes are being used
EXPLAIN ANALYZE
SELECT * FROM "ReferralTracking" 
WHERE "RefereeUserId" = 123 AND "Status" = 1;

-- Should show "Index Scan using IX_ReferralTracking_RefereeUserId"
```

**Query Performance:**
```sql
-- Stats query should complete in <100ms
EXPLAIN (ANALYZE, BUFFERS)
SELECT 
    COUNT(CASE WHEN "Status" >= 0 THEN 1 END) AS Clicks,
    COUNT(CASE WHEN "Status" >= 1 THEN 1 END) AS Registrations,
    COUNT(CASE WHEN "Status" >= 2 THEN 1 END) AS Validations,
    COUNT(CASE WHEN "Status" >= 3 THEN 1 END) AS Rewards
FROM "ReferralTracking"
WHERE "ReferralCodeId" IN (
    SELECT "Id" FROM "ReferralCodes" WHERE "UserId" = 123
);
```

---

## Integration Testing

### Test: Registration → Validation → Reward (Full Flow)

**Automated Test Script (bash):**

```bash
#!/bin/bash

# Configuration
API_BASE="https://ziraai-staging.up.railway.app"
REFERRER_EMAIL="autotest_referrer_$(date +%s)@test.com"
REFEREE_EMAIL="autotest_referee_$(date +%s)@test.com"

echo "=== Referral System Integration Test ==="

# Step 1: Register Referrer
echo "Step 1: Registering referrer..."
REFERRER_REGISTER=$(curl -s -X POST "$API_BASE/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$REFERRER_EMAIL\",
    \"password\": \"Test123!\",
    \"fullName\": \"Auto Test Referrer\",
    \"mobilePhones\": \"05321111111\"
  }")

echo "Referrer registered: $(echo $REFERRER_REGISTER | jq -r '.success')"

# Step 2: Login as Referrer
echo "Step 2: Logging in as referrer..."
REFERRER_LOGIN=$(curl -s -X POST "$API_BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$REFERRER_EMAIL\",
    \"password\": \"Test123!\"
  }")

REFERRER_TOKEN=$(echo $REFERRER_LOGIN | jq -r '.data.token')
echo "Referrer token obtained"

# Step 3: Generate Referral Link
echo "Step 3: Generating referral link..."
REFERRAL_GENERATE=$(curl -s -X POST "$API_BASE/api/referral/generate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $REFERRER_TOKEN" \
  -d '{
    "deliveryMethod": 1,
    "phoneNumbers": ["05329999999"],
    "customMessage": "Automated test"
  }')

REFERRAL_CODE=$(echo $REFERRAL_GENERATE | jq -r '.data.referralCode')
echo "Referral code generated: $REFERRAL_CODE"

# Step 4: Track Click
echo "Step 4: Tracking click..."
CLICK_TRACK=$(curl -s -X POST "$API_BASE/api/referral/track-click" \
  -H "Content-Type: application/json" \
  -d "{
    \"code\": \"$REFERRAL_CODE\",
    \"ipAddress\": \"192.168.1.$(shuf -i 1-254 -n 1)\",
    \"deviceId\": \"auto-test-device-$(date +%s)\"
  }")

echo "Click tracked: $(echo $CLICK_TRACK | jq -r '.success')"

# Step 5: Validate Code
echo "Step 5: Validating code..."
CODE_VALIDATE=$(curl -s -X POST "$API_BASE/api/referral/validate" \
  -H "Content-Type: application/json" \
  -d "{
    \"code\": \"$REFERRAL_CODE\"
  }")

echo "Code validated: $(echo $CODE_VALIDATE | jq -r '.success')"

# Step 6: Register Referee with Referral Code
echo "Step 6: Registering referee with referral code..."
REFEREE_REGISTER=$(curl -s -X POST "$API_BASE/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$REFEREE_EMAIL\",
    \"password\": \"Test123!\",
    \"fullName\": \"Auto Test Referee\",
    \"mobilePhones\": \"05322222222\",
    \"referralCode\": \"$REFERRAL_CODE\"
  }")

echo "Referee registered: $(echo $REFEREE_REGISTER | jq -r '.success')"

# Step 7: Login as Referee
echo "Step 7: Logging in as referee..."
REFEREE_LOGIN=$(curl -s -X POST "$API_BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$REFEREE_EMAIL\",
    \"password\": \"Test123!\"
  }")

REFEREE_TOKEN=$(echo $REFEREE_LOGIN | jq -r '.data.token')
echo "Referee token obtained"

# Step 8: Perform First Plant Analysis
echo "Step 8: Performing first plant analysis..."
# Note: Requires valid base64 image
ANALYSIS=$(curl -s -X POST "$API_BASE/api/v1/plantanalyses/analyze" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $REFEREE_TOKEN" \
  -d '{
    "image": "<base64_image_here>",
    "cropType": "Tomato",
    "location": "Test Farm"
  }')

echo "Analysis completed: $(echo $ANALYSIS | jq -r '.success')"

# Step 9: Check Referrer Stats
echo "Step 9: Checking referrer stats..."
sleep 2 # Wait for async reward processing
STATS=$(curl -s -X GET "$API_BASE/api/referral/stats" \
  -H "Authorization: Bearer $REFERRER_TOKEN")

echo "Referrer stats:"
echo $STATS | jq '.data'

# Step 10: Check Credit Breakdown
echo "Step 10: Checking credit breakdown..."
CREDITS=$(curl -s -X GET "$API_BASE/api/referral/credits" \
  -H "Authorization: Bearer $REFERRER_TOKEN")

echo "Credit breakdown:"
echo $CREDITS | jq '.data'

echo "=== Test Complete ==="
echo "Referrer: $REFERRER_EMAIL"
echo "Referee: $REFEREE_EMAIL"
echo "Referral Code: $REFERRAL_CODE"
```

**Run:**
```bash
chmod +x referral_integration_test.sh
./referral_integration_test.sh
```

---

## Test Cleanup

### Remove Test Data

```sql
-- Delete test tracking records
DELETE FROM "ReferralTracking" 
WHERE "ReferralCodeId" IN (
    SELECT rc."Id" 
    FROM "ReferralCodes" rc
    JOIN "Users" u ON rc."UserId" = u."UserId"
    WHERE u."Email" LIKE '%@test.com'
);

-- Delete test rewards
DELETE FROM "ReferralRewards" 
WHERE "ReferrerUserId" IN (
    SELECT "UserId" FROM "Users" WHERE "Email" LIKE '%@test.com'
);

-- Delete test codes
DELETE FROM "ReferralCodes" 
WHERE "UserId" IN (
    SELECT "UserId" FROM "Users" WHERE "Email" LIKE '%@test.com'
);

-- Delete test subscriptions
DELETE FROM "UserSubscriptions" 
WHERE "UserId" IN (
    SELECT "UserId" FROM "Users" WHERE "Email" LIKE '%@test.com'
);

-- Delete test user groups
DELETE FROM "UserGroups" 
WHERE "UserId" IN (
    SELECT "UserId" FROM "Users" WHERE "Email" LIKE '%@test.com'
);

-- Delete test users
DELETE FROM "Users" WHERE "Email" LIKE '%@test.com';
```

---

## Postman Collection

### Import Collection

Create a Postman collection with all test cases:

**Collection Structure:**
```
ZiraAI Referral System Tests
├── Authentication
│   ├── Register Referrer (Email)
│   ├── Login Referrer (Email)
│   ├── Register Referee (Email with code)
│   ├── Login Referee (Email)
│   ├── Register Referee (Phone OTP - Step 1: Request OTP)
│   ├── Register Referee (Phone OTP - Step 2: Verify OTP)
│   └── Login Referee (Phone OTP)
├── Referral Management
│   ├── Generate Link (SMS)
│   ├── Generate Link (WhatsApp)
│   ├── Generate Link (Hybrid)
│   └── Disable Code
├── Public Endpoints
│   ├── Track Click
│   └── Validate Code
├── Statistics
│   ├── Get Stats
│   ├── Get Codes
│   ├── Get Credits
│   └── Get Rewards
└── Error Scenarios
    ├── Expired Code
    ├── Invalid Code
    ├── Self-Referral
    └── Invalid Phone Format
```

**Collection Variables:**
- `base_url`: `https://ziraai-staging.up.railway.app`
- `referrer_token`: `{{token_from_login}}`
- `referee_token`: `{{token_from_login}}`
- `referral_code`: `{{code_from_generate}}`

---

## Test Checklist

### Pre-Testing
- [ ] Database migrations applied
- [ ] Test users created with subscriptions
- [ ] Configuration values verified
- [ ] SMS/WhatsApp services configured (or mocked)
- [ ] JWT tokens obtained

### Happy Path
- [ ] TS-001: Generate link (SMS) - PASSED
- [ ] TS-002: Generate link (WhatsApp) - PASSED
- [ ] TS-003: Generate link (Hybrid) - PASSED
- [ ] TS-004: Track click - PASSED
- [ ] TS-007: Validate code - PASSED
- [ ] TS-009: Register with code (email) - PASSED
- [ ] TS-009A: Register with code (phone - OTP) - PASSED
- [ ] TS-011: First analysis validation - PASSED
- [ ] TS-012: Reward processed - PASSED
- [ ] TS-013: Get stats - PASSED
- [ ] TS-014: Get codes - PASSED
- [ ] TS-015: Get credits - PASSED
- [ ] TS-016: Disable code - PASSED

### Error Scenarios
- [ ] TS-005: Expired code - PASSED
- [ ] TS-006: Duplicate click - PASSED
- [ ] TS-008: Invalid code - PASSED
- [ ] TS-010: Self-referral (email) - PASSED
- [ ] TS-010A: Self-referral (phone) - PASSED
- [ ] TS-010B: Duplicate phone registration - PASSED
- [ ] TS-010C: Expired OTP during registration - PASSED
- [ ] TS-017: Max phone limit - PASSED
- [ ] TS-018: Invalid phone format - PASSED

### Performance
- [ ] Load test: 100 concurrent requests - PASSED
- [ ] Database query performance <100ms - PASSED
- [ ] No duplicate codes generated - PASSED

### Integration
- [ ] Full flow end-to-end - PASSED
- [ ] Registration integration - PASSED
- [ ] Analysis integration - PASSED
- [ ] Credit usage integration - PASSED

---

**Test Environment:** Staging  
**Test Date:** 2025-10-03  
**Tester:** QA Team / Developer  
**Status:** Ready for Execution
