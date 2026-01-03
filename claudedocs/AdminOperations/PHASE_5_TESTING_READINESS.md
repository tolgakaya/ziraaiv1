# Phase 5: Testing & Verification - Readiness Guide

**Date**: 2026-01-02
**Status**: ⏳ Ready to Start
**Prerequisites**: ✅ All Complete (Phases 0, 1, 2, 4)
**Build Status**: ✅ 0 errors, 0 warnings

---

## 📋 Overview

Phase 5 involves deploying and testing the Farmer Invitation System on staging environment, verifying all functionality works correctly, and ensuring backward compatibility with existing features.

---

## ✅ Pre-Deployment Checklist

### Code Readiness
- [x] All handlers implemented and compiled
- [x] Configuration added for all environments
- [x] Build successful (0 errors, 0 warnings)
- [x] Operation claims SQL script ready
- [x] Database migration SQL script ready

### Documentation Readiness
- [x] Phase 2 completion summary created
- [x] Phase 4 completion summary created
- [x] Railway environment variables documented
- [x] Development plan updated (57% complete)

### Files Ready for Deployment
**SQL Scripts** (2 files):
1. `claudedocs/AdminOperations/001_farmer_invitation_system.sql` - Table creation
2. `claudedocs/AdminOperations/004_farmer_invitation_operation_claims.sql` - Permission setup

**Configuration Files** (2 files):
1. `WebAPI/appsettings.json` - Development config
2. `WebAPI/appsettings.Staging.json` - Staging config

**Code Files** (17 files):
- 2 configuration service files
- 4 DTO files
- 4 command files
- 4 query files
- 1 controller modification
- 2 dependency registration modifications

---

## 🗄️ Database Migration Steps

### Step 1: Connect to Staging Database

```bash
# Using psql command line
psql -h <railway-staging-host> -U <user> -d <database>

# Or use Railway CLI
railway connect postgres --environment staging
```

### Step 2: Run Table Creation Migration

**File**: `claudedocs/AdminOperations/001_farmer_invitation_system.sql`

**What it does**:
- Creates `FarmerInvitation` table with all columns
- Adds foreign key constraints to Users and SponsorshipCodes
- Creates indexes for performance:
  - `IX_FarmerInvitation_SponsorId` (lookup by sponsor)
  - `IX_FarmerInvitation_InvitationToken` (UNIQUE, token lookup)
  - `IX_FarmerInvitation_Status` (filter by status)
  - `IX_FarmerInvitation_Phone` (lookup by phone)
  - `IX_FarmerInvitation_AcceptedByUserId` (lookup by farmer)
  - `IX_FarmerInvitation_ExpiryDate` (expiry checks)

**Verification Queries**:
```sql
-- Verify table exists
SELECT * FROM information_schema.tables
WHERE table_name = 'FarmerInvitation';

-- Verify indexes created
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'FarmerInvitation';

-- Expected: 7 indexes (1 primary key + 6 custom)
```

### Step 3: Add ReservedForFarmerInvitationId to SponsorshipCodes

**SQL** (from migration file):
```sql
ALTER TABLE "SponsorshipCodes"
ADD COLUMN "ReservedForFarmerInvitationId" INTEGER NULL;

CREATE INDEX "IX_SponsorshipCodes_ReservedForFarmerInvitationId"
ON "SponsorshipCodes"("ReservedForFarmerInvitationId")
WHERE "ReservedForFarmerInvitationId" IS NOT NULL;
```

**Verification**:
```sql
-- Verify column added
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'SponsorshipCodes'
  AND column_name = 'ReservedForFarmerInvitationId';

-- Expected: 1 row, nullable integer column
```

### Step 4: Run Operation Claims Migration

**File**: `claudedocs/AdminOperations/004_farmer_invitation_operation_claims.sql`

**Pre-flight Checks**:
```sql
-- Check current max claim ID
SELECT MAX("Id") as "CurrentMaxClaimId"
FROM public."OperationClaims";
-- Expected: 187 or lower

-- Check for conflicts
SELECT "Id", "Name"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 188 AND 191;
-- Expected: 0 rows
```

**What it does**:
- Creates 4 operation claims (IDs 188-191)
- Assigns claims to appropriate groups:
  - Sponsors: CreateFarmerInvitationCommand (188), GetFarmerInvitationsQuery (190)
  - Farmers: AcceptFarmerInvitationCommand (189)
  - Admins: All claims (188-191)

**Verification Queries**:
```sql
-- Verify all 4 claims created
SELECT "Id", "Name", "Alias", "Description"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 188 AND 191
ORDER BY "Id";
-- Expected: 4 rows

-- Verify group assignments
SELECT
    g."GroupName",
    oc."Id" as "ClaimId",
    oc."Name" as "ClaimName"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Id" BETWEEN 188 AND 191
ORDER BY oc."Id", g."GroupName";
-- Expected: 8 rows (188→Admin+Sponsors, 189→Admin+Farmers, 190→Admin+Sponsors, 191→Admin)
```

---

## 🚀 Railway Deployment Steps

### Step 1: Set Environment Variables

**Using Railway Dashboard**:
1. Navigate to staging environment
2. Go to Variables tab
3. Add the following:

```bash
FARMERINVITATION__DEEPLINKBASEURL=https://ziraai-api-sit.up.railway.app/farmer-invite/
FARMERINVITATION__TOKENEXPIRYDAYS=7
```

**Using Railway CLI**:
```bash
railway variables set FARMERINVITATION__DEEPLINKBASEURL=https://ziraai-api-sit.up.railway.app/farmer-invite/ --environment staging
railway variables set FARMERINVITATION__TOKENEXPIRYDAYS=7 --environment staging
```

**Verify**:
```bash
railway variables --environment staging | grep FARMERINVITATION
```

### Step 2: Deploy to Staging

**Option A: Automatic Deployment** (if CI/CD configured)
```bash
git push origin staging
```

**Option B: Manual Deployment**
```bash
railway up --environment staging
```

### Step 3: Verify Deployment

**Check Logs**:
```bash
railway logs --environment staging
```

**Look for**:
- "Application started" message
- No startup errors
- Configuration service registration confirmation

---

## 🧪 Backend Testing Plan

### Test Environment Setup

**Base URL**: `https://ziraai-api-sit.up.railway.app`

**Required Headers**:
```
Content-Type: application/json
Authorization: Bearer {token}
```

**Test Users Required**:
- 1 Sponsor account (can create invitations)
- 1 Farmer account (can accept invitations)
- 1 Admin account (can view all)

### Test Scenario 1: Create Farmer Invitation (Sponsor)

**Endpoint**: `POST /api/Sponsorship/farmer/invite`

**Prerequisites**:
- Sponsor must have unused sponsorship codes
- Sponsor must be authenticated

**Request**:
```json
{
  "phone": "05551234567",
  "farmerName": "Test Farmer",
  "codeCount": 3,
  "packageTier": "M"
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "message": "Farmer invitation created and SMS sent successfully",
  "data": {
    "invitationId": 1,
    "invitationToken": "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6",
    "farmerName": "Test Farmer",
    "phone": "05551234567",
    "codeCount": 3,
    "packageTier": "M",
    "status": "Pending",
    "expiryDate": "2026-01-09T12:00:00Z",
    "invitationLink": "https://ziraai-api-sit.up.railway.app/farmer-invite/a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6",
    "smsSent": true
  }
}
```

**Verification Checklist**:
- [ ] Response has 200 status code
- [ ] invitationToken is 32-character GUID format
- [ ] invitationLink contains correct staging URL
- [ ] expiryDate is 7 days from creation
- [ ] smsSent is true
- [ ] SMS received on provided phone number
- [ ] SMS contains correct deep link

**Database Verification**:
```sql
SELECT
    "Id",
    "SponsorId",
    "Phone",
    "FarmerName",
    "InvitationToken",
    "Status",
    "CodeCount",
    "PackageTier",
    "ExpiryDate",
    "LinkSentDate",
    "LinkDelivered"
FROM "FarmerInvitation"
WHERE "Phone" = '05551234567'
ORDER BY "CreatedDate" DESC
LIMIT 1;
```

**Expected Database State**:
- Status = 'Pending'
- LinkSentDate is populated
- LinkDelivered = true
- ExpiryDate = CreatedDate + 7 days

**Code Reservation Verification**:
```sql
SELECT
    "Id",
    "Code",
    "PackageTier",
    "IsUsed",
    "ReservedForFarmerInvitationId"
FROM "SponsorshipCodes"
WHERE "ReservedForFarmerInvitationId" = 1;
-- Expected: 3 rows with PackageTier = 'M', IsUsed = false
```

### Test Scenario 2: Get Invitation Details (Public/Anonymous)

**Endpoint**: `GET /api/Sponsorship/farmer/invitation-details?token={token}`

**Prerequisites**:
- Valid invitation token from Test Scenario 1
- NO authentication required (AllowAnonymous)

**Request**:
```
GET /api/Sponsorship/farmer/invitation-details?token=a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "sponsorName": "Test Sponsor Company",
    "farmerName": "Test Farmer",
    "codeCount": 3,
    "packageTier": "M",
    "status": "Pending",
    "expiryDate": "2026-01-09T12:00:00Z",
    "canAccept": true,
    "message": "Bu davetiye geçerlidir. Kabul etmek için giriş yapın."
  }
}
```

**Verification Checklist**:
- [ ] Response has 200 status code
- [ ] NO authorization header required
- [ ] sponsorName is visible (not sensitive)
- [ ] canAccept is true
- [ ] message is in Turkish
- [ ] NO sensitive data exposed (SponsorId, InvitationId, Phone)

**Edge Case Tests**:

**Expired Token**:
```sql
-- Manually expire an invitation for testing
UPDATE "FarmerInvitation"
SET "ExpiryDate" = NOW() - INTERVAL '1 day'
WHERE "InvitationToken" = 'test-expired-token';
```

**Expected Response**:
```json
{
  "success": true,
  "data": {
    "canAccept": false,
    "message": "Bu davetiye süresi dolmuş."
  }
}
```

**Invalid Token**:
```
GET /api/Sponsorship/farmer/invitation-details?token=invalid-token-12345
```

**Expected Response** (404 Not Found):
```json
{
  "success": false,
  "message": "Invitation not found"
}
```

### Test Scenario 3: Accept Farmer Invitation (Farmer)

**Endpoint**: `POST /api/Sponsorship/farmer/accept-invitation`

**Prerequisites**:
- Valid invitation token
- Farmer must be authenticated
- Farmer's phone must match invitation phone

**Request**:
```json
{
  "invitationToken": "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6"
}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "message": "Invitation accepted successfully. 3 codes assigned to your account.",
  "data": {
    "invitationId": 1,
    "assignedCodeCount": 3,
    "packageTier": "M",
    "codes": [
      "CODE-M-ABC123",
      "CODE-M-DEF456",
      "CODE-M-GHI789"
    ]
  }
}
```

**Verification Checklist**:
- [ ] Response has 200 status code
- [ ] assignedCodeCount matches original codeCount
- [ ] codes array has correct length
- [ ] All codes have correct PackageTier

**Database Verification - Invitation**:
```sql
SELECT
    "Id",
    "Status",
    "AcceptedByUserId",
    "AcceptedDate"
FROM "FarmerInvitation"
WHERE "InvitationToken" = 'a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6';
```

**Expected State**:
- Status = 'Accepted'
- AcceptedByUserId = farmer's UserId
- AcceptedDate is populated

**Database Verification - Codes**:
```sql
SELECT
    "Id",
    "Code",
    "IsUsed",
    "RedeemedBy",
    "RedemptionDate",
    "DistributionChannel",
    "DistributedTo",
    "LinkSentDate"
FROM "SponsorshipCodes"
WHERE "ReservedForFarmerInvitationId" = 1;
```

**Expected State (CRITICAL for backward compatibility)**:
- IsUsed = true
- RedeemedBy = farmer's UserId
- RedemptionDate is populated
- DistributionChannel = 'FarmerInvitation'
- DistributedTo = farmer's phone
- LinkSentDate = invitation.LinkSentDate (preserved!)

**Phone Mismatch Test**:
```json
// Use different farmer account with different phone
{
  "invitationToken": "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6"
}
```

**Expected Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Phone number does not match invitation"
}
```

### Test Scenario 4: List Farmer Invitations (Sponsor)

**Endpoint**: `GET /api/Sponsorship/farmer/invitations?status=Pending`

**Prerequisites**:
- Sponsor must be authenticated
- Sponsor has created invitations

**Request Options**:
```
GET /api/Sponsorship/farmer/invitations
GET /api/Sponsorship/farmer/invitations?status=Pending
GET /api/Sponsorship/farmer/invitations?status=Accepted
GET /api/Sponsorship/farmer/invitations?status=Expired
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "farmerName": "Test Farmer",
      "phone": "05551234567",
      "codeCount": 3,
      "packageTier": "M",
      "status": "Pending",
      "createdDate": "2026-01-02T12:00:00Z",
      "expiryDate": "2026-01-09T12:00:00Z",
      "linkSentDate": "2026-01-02T12:00:00Z",
      "linkDelivered": true
    }
  ]
}
```

**Verification Checklist**:
- [ ] Response has 200 status code
- [ ] Only sponsor's own invitations returned
- [ ] Status filter works correctly
- [ ] Results ordered by CreatedDate DESC

**Cross-Sponsor Security Test**:
```
// Login as different sponsor
// Try to access first sponsor's invitations
GET /api/Sponsorship/farmer/invitations
```

**Expected**: Should only see own invitations, not other sponsors'

---

## 🔄 Backward Compatibility Testing

### Critical Requirement
> "code gönderimi, code kullanımı ve ilgili istatistiklerin de aynı şekilde devam ediyo olması lazım ayrıca mevcut featurelar bozulmamalı"

All existing features must continue working without changes.

### Test 1: SendSponsorshipLinkCommand Still Works

**Endpoint**: `POST /api/Sponsorship/send-link`

**Request**:
```json
{
  "farmerPhone": "05559876543",
  "codeCount": 2,
  "packageTier": "S"
}
```

**Expected**: Should work exactly as before, no changes

**Database Check**:
```sql
SELECT
    "Code",
    "IsUsed",
    "LinkSentDate",
    "DistributionChannel",
    "ReservedForFarmerInvitationId"
FROM "SponsorshipCodes"
WHERE "DistributedTo" = '05559876543';
```

**Expected State**:
- LinkSentDate is populated
- DistributionChannel = 'SMS' or 'WhatsApp'
- ReservedForFarmerInvitationId = NULL (not using new system)

### Test 2: RedeemSponsorshipCodeCommand Still Works

**Endpoint**: `POST /api/Sponsorship/redeem`

**Request**:
```json
{
  "code": "CODE-S-XYZ789"
}
```

**Expected**: Should work exactly as before for both old and new codes

### Test 3: GetLinkStatisticsQuery Works with Mixed Codes

**Endpoint**: `GET /api/Sponsorship/sponsor/link-statistics`

**What to Verify**:
- Statistics include both old codes (via SendSponsorshipLinkCommand)
- Statistics include new codes (via FarmerInvitation)
- All codes have LinkSentDate populated
- Counts are accurate

**Database Query**:
```sql
SELECT
    COUNT(*) FILTER (WHERE "LinkSentDate" IS NOT NULL) as "TotalLinksSent",
    COUNT(*) FILTER (WHERE "LinkSentDate" IS NOT NULL AND "IsUsed" = true) as "LinksRedeemed",
    COUNT(*) FILTER (WHERE "DistributionChannel" = 'FarmerInvitation') as "NewSystemCodes",
    COUNT(*) FILTER (WHERE "DistributionChannel" IN ('SMS', 'WhatsApp', 'Email')) as "OldSystemCodes"
FROM "SponsorshipCodes"
WHERE "SponsorId" = <sponsor-id>;
```

**Expected**: All counts accurate, both systems counted

### Test 4: GetPackageDistributionStatisticsQuery

**Endpoint**: `GET /api/Sponsorship/sponsor/package-distribution`

**What to Verify**:
- Package tier breakdown includes all codes
- Both old and new distribution methods counted
- DistributionChannel field properly tagged

### Test 5: GetSponsorTemporalAnalyticsQuery

**Endpoint**: `GET /api/Sponsorship/sponsor/temporal-analytics`

**What to Verify**:
- Time-series data includes both systems
- LinkSentDate used for temporal grouping
- No gaps in timeline

### Test 6: GetFarmerSponsorshipInboxQuery

**Endpoint**: `GET /api/Sponsorship/farmer/inbox`

**What to Verify**:
- Farmers see codes from both systems
- New codes show DistributionChannel = 'FarmerInvitation'
- Old codes show original distribution channels
- All codes have LinkSentDate

---

## 📊 Success Criteria

### Backend Testing
- [ ] All 4 endpoints respond correctly (200/400/404)
- [ ] SMS delivery works on staging
- [ ] Deep links format correctly
- [ ] Phone verification works
- [ ] Token expiry enforced
- [ ] Authorization enforced (Sponsor/Farmer/Public)

### Backward Compatibility
- [ ] SendSponsorshipLinkCommand still works
- [ ] RedeemSponsorshipCodeCommand still works
- [ ] GetLinkStatisticsQuery accurate with mixed codes
- [ ] GetPackageDistributionStatisticsQuery accurate
- [ ] GetSponsorTemporalAnalyticsQuery accurate
- [ ] GetFarmerSponsorshipInboxQuery shows all codes

### Database Integrity
- [ ] FarmerInvitation table created
- [ ] All indexes created (7 total)
- [ ] ReservedForFarmerInvitationId column added
- [ ] Operation claims created (4 total)
- [ ] Group claims assigned correctly

### Configuration
- [ ] Railway environment variables set
- [ ] Configuration service reads values correctly
- [ ] Deep link URLs correct per environment
- [ ] SMS template renders correctly

---

## 🐛 Known Issues & Troubleshooting

### Issue: SMS Not Delivered

**Symptoms**: smsSent = true but SMS not received

**Checks**:
1. Verify IMessagingServiceFactory configuration
2. Check SMS provider logs (Netgsm)
3. Verify phone number normalization
4. Check SMS provider quota/balance

**Debug Query**:
```sql
SELECT
    "Phone",
    "LinkSentDate",
    "LinkSentVia",
    "LinkDelivered"
FROM "FarmerInvitation"
WHERE "LinkSentDate" IS NOT NULL
ORDER BY "LinkSentDate" DESC
LIMIT 10;
```

### Issue: Deep Link Not Working

**Symptoms**: Link opens browser instead of app

**Checks**:
1. Verify mobile app assetlinks.json configured
2. Check package name matches environment
3. Verify deep link URL format (trailing slash)
4. Test on different devices/browsers

### Issue: Phone Match Failure

**Symptoms**: 400 error "Phone number does not match"

**Checks**:
1. Verify phone normalization logic
2. Check farmer's phone in database (+90 vs 0 format)
3. Test with both formats: "05551234567" and "+905551234567"

**Debug**:
```sql
-- Check farmer's phone in database
SELECT "UserId", "Phone" FROM "Users" WHERE "UserId" = <farmer-id>;

-- Check invitation phone
SELECT "Phone" FROM "FarmerInvitation" WHERE "Id" = <invitation-id>;
```

### Issue: Codes Not Reserved

**Symptoms**: No codes assigned even though sponsor has available codes

**Checks**:
1. Verify sponsor has enough unused codes
2. Check tier compatibility
3. Verify codes not already reserved

**Debug Query**:
```sql
SELECT
    COUNT(*) as "AvailableCodes",
    "PackageTier"
FROM "SponsorshipCodes"
WHERE "SponsorId" = <sponsor-id>
  AND "IsUsed" = false
  AND "ReservedForFarmerInvitationId" IS NULL
GROUP BY "PackageTier";
```

---

## 📝 Testing Checklist Summary

### Pre-Deployment
- [ ] SQL migrations reviewed
- [ ] Railway environment variables prepared
- [ ] Test user accounts ready

### Deployment
- [ ] Database migrations executed
- [ ] Railway environment variables set
- [ ] Application deployed to staging
- [ ] Deployment logs verified

### Backend Testing
- [ ] Scenario 1: Create invitation (Sponsor)
- [ ] Scenario 2: Get invitation details (Public)
- [ ] Scenario 3: Accept invitation (Farmer)
- [ ] Scenario 4: List invitations (Sponsor)

### Backward Compatibility
- [ ] SendSponsorshipLinkCommand verified
- [ ] RedeemSponsorshipCodeCommand verified
- [ ] GetLinkStatisticsQuery verified
- [ ] GetPackageDistributionStatisticsQuery verified
- [ ] GetSponsorTemporalAnalyticsQuery verified
- [ ] GetFarmerSponsorshipInboxQuery verified

### Final Verification
- [ ] All success criteria met
- [ ] No regressions in existing features
- [ ] Documentation updated
- [ ] Ready for Phase 6 (Documentation)

---

**Created**: 2026-01-02
**Phase**: 5 - Testing & Verification
**Status**: Ready to Execute
