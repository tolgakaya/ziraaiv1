# Dealer Code Distribution System - Testing Checklist

**Version**: 1.0  
**Last Updated**: 2025-10-26  
**Environment**: Staging (ziraai-api-sit.up.railway.app)

## Pre-Deployment Checklist

### ✅ Phase 1-9 Complete
- [x] Database schema changes (001-003 SQL scripts)
- [x] Entity updates (DealerInvitation, SponsorshipCode, PlantAnalysis)
- [x] DTOs created (7 files)
- [x] Commands and Queries (7 handlers)
- [x] Repository and DbContext updates
- [x] Business logic implementation
- [x] Dependency injection registration
- [x] Controller endpoints (7 REST APIs)
- [x] Authorization SQL script (004)
- [x] Messaging filter updates
- [x] API documentation complete
- [x] Build checkpoints passed (7/7)

### ⏳ SQL Migration Execution
**Status**: PENDING MANUAL EXECUTION

#### Migration Order
Execute scripts in this exact order on staging database:

1. **001_add_dealerid_columns.sql**
   - Adds `DealerId`, `TransferredAt`, `TransferredByUserId` to `SponsorshipCodes`
   - Adds `DealerId` to `PlantAnalyses`
   - Creates indexes for performance

2. **002_create_dealer_invitations.sql**
   - Creates `DealerInvitations` table
   - Sets up constraints and indexes

3. **003_verification_queries.sql**
   - Run verification queries to confirm schema changes
   - Check backward compatibility (all existing rows should have NULL DealerId)

4. **004_dealer_authorization.sql**
   - Creates 7 OperationClaims
   - Assigns claims to Sponsor (GroupId=3) and Admin (GroupId=1) groups
   - Runs verification queries

#### Execution Checklist
- [ ] Backup staging database before migration
- [ ] Execute 001_add_dealerid_columns.sql
- [ ] Verify columns added: `SELECT * FROM "SponsorshipCodes" LIMIT 1;`
- [ ] Execute 002_create_dealer_invitations.sql
- [ ] Verify table created: `SELECT COUNT(*) FROM "DealerInvitations";`
- [ ] Execute 003_verification_queries.sql
- [ ] Review verification results (all DealerId should be NULL)
- [ ] Execute 004_dealer_authorization.sql
- [ ] Verify claims created: `SELECT COUNT(*) FROM "OperationClaims" WHERE "Name" LIKE '%Dealer%';`
- [ ] Verify group assignments: `SELECT COUNT(*) FROM "GroupClaims" WHERE "ClaimId" IN (SELECT "Id" FROM "OperationClaims" WHERE "Name" LIKE '%Dealer%');`

---

## Testing Plan

### 1. Backward Compatibility Tests

#### Test 1.1: Existing Sponsor Functionality
**Objective**: Verify existing sponsors can still purchase and use codes without dealer features

**Test Steps**:
1. Login as existing sponsor (no dealer network)
2. Purchase sponsorship package
3. View codes: `GET /api/v1/sponsorship/purchases/{id}`
4. Activate code for farmer
5. View sponsored analyses list: `GET /api/v1/sponsorship/analyses/list`
6. Verify messaging still works

**Expected Results**:
- ✅ All existing endpoints work without changes
- ✅ DealerId is NULL for all existing codes
- ✅ Sponsored analyses list shows all analyses without DealerId filter
- ✅ Messaging functionality unaffected

**Success Criteria**: Existing sponsor workflow unchanged

---

#### Test 1.2: Existing Codes Remain Usable
**Objective**: Verify codes purchased before migration can still be activated

**Test Steps**:
1. Query existing codes: `SELECT * FROM "SponsorshipCodes" WHERE "DealerId" IS NULL LIMIT 5;`
2. Attempt to activate one code with farmer
3. Verify analysis created successfully
4. Check `PlantAnalyses` table for new analysis

**Expected Results**:
- ✅ Old codes (DealerId=NULL) can be activated
- ✅ New analysis has DealerId=NULL
- ✅ Sponsor can view and message farmer normally

**Success Criteria**: Zero regression for existing code inventory

---

### 2. Method A: Manual Transfer (Existing Sponsor)

#### Test 2.1: Search Existing Dealer
**Endpoint**: `GET /api/v1/sponsorship/dealer/search?email={email}`

**Test Steps**:
1. Get valid sponsor JWT token (Admin or Main Sponsor)
2. Search for known sponsor by email
3. Verify `isSponsor` = true in response
4. Note `userId` for next test

**Expected Results**:
```json
{
  "data": {
    "userId": 165,
    "email": "dealer@example.com",
    "firstName": "User",
    "lastName": "1113",
    "isSponsor": true
  },
  "success": true,
  "message": "Dealer found successfully."
}
```

**Success Criteria**: Existing sponsor found and identified correctly

---

#### Test 2.2: Transfer Codes to Existing Dealer
**Endpoint**: `POST /api/v1/sponsorship/dealer/transfer-codes`

**Test Steps**:
1. Use dealerId from Test 2.1
2. Select valid purchaseId with available codes
3. Request transfer of 10 codes

**Request**:
```json
{
  "dealerId": 165,
  "purchaseId": 26,
  "codeCount": 10,
  "notes": "Test transfer for Q4 campaign"
}
```

**Expected Results**:
- ✅ 200 OK response
- ✅ `transferredCount` = 10
- ✅ `transferredCodes` array contains 10 code objects
- ✅ Each code has `transferredAt` timestamp
- ✅ `remainingAvailableCodes` reduced by 10

**Database Verification**:
```sql
SELECT "Id", "Code", "DealerId", "TransferredAt", "TransferredByUserId"
FROM "SponsorshipCodes"
WHERE "DealerId" = 165
LIMIT 10;
```

**Success Criteria**: Codes transferred, database updated correctly

---

#### Test 2.3: Dealer Uses Transferred Code
**Objective**: Verify dealer can activate code with their farmer

**Test Steps**:
1. Login as dealer (userId from Test 2.1)
2. View available codes
3. Activate one transferred code with test farmer
4. Verify analysis created with dealer's SponsorUserId but main sponsor visible

**Expected Results**:
- ✅ Dealer can see transferred codes in their inventory
- ✅ Code activation works normally
- ✅ Analysis shows dealer as SponsorUserId
- ✅ DealerId field populated in PlantAnalyses
- ✅ Main sponsor can view this analysis (read-only)

**Success Criteria**: Full code lifecycle works through dealer

---

### 3. Method B: Invitation Link

#### Test 3.1: Create Invite-Type Invitation
**Endpoint**: `POST /api/v1/sponsorship/dealer/invite`

**Test Steps**:
1. Create invitation with type "Invite"
2. Verify invitation created with 7-day expiry
3. Note invitation token

**Request**:
```json
{
  "invitationType": "Invite",
  "email": "testdealer@example.com",
  "phone": "+90555999888",
  "dealerName": "Test Dealer Company",
  "purchaseId": 26,
  "codeCount": 15
}
```

**Expected Results**:
- ✅ 200 OK response
- ✅ `invitationToken` generated (unique)
- ✅ `invitationLink` contains token
- ✅ `status` = "Pending"
- ✅ `expiryDate` = 7 days from now
- ✅ Codes NOT transferred yet

**Database Verification**:
```sql
SELECT * FROM "DealerInvitations"
WHERE "Email" = 'testdealer@example.com'
ORDER BY "CreatedDate" DESC
LIMIT 1;
```

**Success Criteria**: Invitation created, codes held until acceptance

---

#### Test 3.2: List Pending Invitations
**Endpoint**: `GET /api/v1/sponsorship/dealer/invitations?status=Pending`

**Test Steps**:
1. Query invitations with status filter
2. Verify Test 3.1 invitation appears

**Expected Results**:
- ✅ Array contains invitation from Test 3.1
- ✅ Invitation shows correct email, dealerName, codeCount
- ✅ Status = "Pending"

**Success Criteria**: Invitation listing works with status filter

---

### 4. Method C: AutoCreate

#### Test 4.1: Create AutoCreate Dealer
**Endpoint**: `POST /api/v1/sponsorship/dealer/invite`

**Test Steps**:
1. Create invitation with type "AutoCreate"
2. Verify account created immediately
3. Note auto-generated password

**Request**:
```json
{
  "invitationType": "AutoCreate",
  "email": "autocreatedealer@example.com",
  "dealerName": "AutoCreate Dealer LLC",
  "purchaseId": 26,
  "codeCount": 20
}
```

**Expected Results**:
- ✅ 200 OK response
- ✅ `status` = "Accepted" (immediately)
- ✅ `autoCreatedPassword` returned (12 characters, random)
- ✅ `createdDealerId` populated with new user ID
- ✅ Codes transferred immediately
- ✅ Message includes login credentials

**Database Verification**:
```sql
-- Verify user created
SELECT "UserId", "Email", "FullName", "Status"
FROM "Users"
WHERE "Email" = 'autocreatedealer@example.com';

-- Verify Sponsor role assigned
SELECT ug."UserId", g."GroupName"
FROM "UserGroup" ug
JOIN "Group" g ON ug."GroupId" = g."Id"
WHERE ug."UserId" = (SELECT "UserId" FROM "Users" WHERE "Email" = 'autocreatedealer@example.com');

-- Verify codes transferred
SELECT COUNT(*) FROM "SponsorshipCodes"
WHERE "DealerId" = (SELECT "UserId" FROM "Users" WHERE "Email" = 'autocreatedealer@example.com');
```

**Success Criteria**: Account created, role assigned, codes transferred automatically

---

#### Test 4.2: Login with AutoCreated Account
**Objective**: Verify auto-generated credentials work

**Test Steps**:
1. Use email and password from Test 4.1
2. Login via `POST /api/v1/auth/login`
3. Verify JWT token received
4. Verify Sponsor role in token claims

**Expected Results**:
- ✅ Login successful
- ✅ JWT token contains Sponsor role
- ✅ User can access sponsor endpoints

**Success Criteria**: AutoCreated account fully functional

---

### 5. Code Reclaim Tests

#### Test 5.1: Reclaim Unused Codes
**Endpoint**: `POST /api/v1/sponsorship/dealer/reclaim-codes`

**Test Steps**:
1. Transfer 10 codes to dealer (Test 2.2 dealer)
2. Dealer uses 3 codes (activates with farmers)
3. Main sponsor reclaims 5 unused codes

**Request**:
```json
{
  "dealerId": 165,
  "codeCount": 5,
  "reason": "End of Q4 campaign - unused codes"
}
```

**Expected Results**:
- ✅ 200 OK response
- ✅ `reclaimedCount` = 5
- ✅ `reclaimedCodes` array contains 5 code objects
- ✅ Codes removed from dealer's inventory

**Database Verification**:
```sql
-- Verify DealerId cleared for reclaimed codes
SELECT "Id", "Code", "DealerId", "IsUsed"
FROM "SponsorshipCodes"
WHERE "Id" IN (981, 982, 983, 984, 985); -- IDs from response
```

**Success Criteria**: Unused codes successfully reclaimed, database updated

---

#### Test 5.2: Cannot Reclaim Used Codes
**Objective**: Verify used codes cannot be reclaimed

**Test Steps**:
1. Dealer uses remaining 2 codes (from step 1)
2. Main sponsor attempts to reclaim 3 codes (only 0 unused remain)

**Expected Results**:
- ✅ 400 Bad Request
- ✅ Error message: "Not enough unused codes to reclaim. Requested: 3, Available: 0"

**Success Criteria**: Business rule enforcement - only unused codes reclaimable

---

### 6. Performance Analytics Tests

#### Test 6.1: Dealer Performance Details
**Endpoint**: `GET /api/v1/sponsorship/dealer/analytics/{dealerId}`

**Test Steps**:
1. Get analytics for dealer with transferred codes
2. Verify all metrics calculated correctly

**Expected Results**:
```json
{
  "data": {
    "dealerId": 165,
    "totalCodesReceived": 50,
    "codesUsed": 32,
    "codesAvailable": 18,
    "usageRate": 64.0,
    "totalFarmersSponsored": 28,
    "totalAnalyses": 70,
    "messagingStatistics": {
      "totalConversations": 25,
      "activeConversations": 15,
      "responseRate": 88.0
    }
  }
}
```

**Success Criteria**: Accurate metrics based on dealer's code usage

---

#### Test 6.2: Dealer Summary List
**Endpoint**: `GET /api/v1/sponsorship/dealer/summary`

**Test Steps**:
1. Query summary for sponsor with multiple dealers
2. Verify aggregated statistics

**Expected Results**:
- ✅ `totalDealers` count correct
- ✅ `activeDealers` calculated (activity in last 90 days)
- ✅ `totalCodesDistributed` sum matches
- ✅ `dealers` array contains all dealers
- ✅ Each dealer shows correct usage metrics

**Success Criteria**: Summary provides accurate oversight of dealer network

---

### 7. Messaging Filter Tests

#### Test 7.1: Filter by DealerId
**Endpoint**: `GET /api/v1/sponsorship/analyses/list?dealerId={id}`

**Test Steps**:
1. Dealer queries analyses with their own dealerId
2. Main sponsor queries analyses filtered by specific dealerId
3. Verify correct filtering

**Expected Results**:
- ✅ Dealer sees only their distributed analyses
- ✅ Main sponsor can filter by any dealer to monitor performance
- ✅ Messaging status populated correctly
- ✅ Backward compatible (no dealerId = all analyses)

**Success Criteria**: Dealer filter works correctly for both roles

---

### 8. Authorization Tests

#### Test 8.1: Sponsor Role Access
**Objective**: Verify Sponsor role has all dealer permissions

**Test Steps**:
1. Login as Sponsor
2. Test each dealer endpoint
3. Verify all operations allowed

**Expected Results**:
- ✅ POST /dealer/transfer-codes → 200 OK
- ✅ POST /dealer/invite → 200 OK
- ✅ POST /dealer/reclaim-codes → 200 OK
- ✅ GET /dealer/analytics/{id} → 200 OK
- ✅ GET /dealer/summary → 200 OK
- ✅ GET /dealer/invitations → 200 OK
- ✅ GET /dealer/search → 200 OK

**Success Criteria**: All endpoints accessible to Sponsor role

---

#### Test 8.2: Admin Role Access
**Objective**: Verify Admin role has full access

**Test Steps**:
1. Login as Admin
2. Test dealer operations on behalf of any sponsor

**Expected Results**:
- ✅ All dealer endpoints return 200 OK
- ✅ Admin can view/manage any sponsor's dealer network

**Success Criteria**: Admin role has full oversight capability

---

#### Test 8.3: Farmer Role Denied
**Objective**: Verify Farmer role cannot access dealer endpoints

**Test Steps**:
1. Login as Farmer
2. Attempt dealer operations

**Expected Results**:
- ✅ All dealer endpoints return 403 Forbidden
- ✅ Error message indicates insufficient permissions

**Success Criteria**: Role-based access control working correctly

---

## Edge Cases & Error Scenarios

### Edge Case 1: Transfer More Codes Than Available
**Request**: 100 codes when only 50 available  
**Expected**: 400 Bad Request - "Not enough available codes"

### Edge Case 2: Reclaim from Non-Existent Dealer
**Request**: DealerId = 9999 (doesn't exist)  
**Expected**: 404 Not Found - "Dealer not found"

### Edge Case 3: Search Non-Existent Email
**Request**: email=nonexistent@example.com  
**Expected**: 404 Not Found - "No user found with this email address"

### Edge Case 4: Expired Invitation Acceptance
**Request**: Accept invitation after 7 days  
**Expected**: 400 Bad Request - "Invitation expired"

### Edge Case 5: Transfer to Self
**Request**: Sponsor transfers codes to own dealerId  
**Expected**: 400 Bad Request - "Cannot transfer codes to yourself"

---

## Performance Tests

### Test P.1: Large Code Transfer
**Objective**: Transfer 100 codes in single operation

**Expected**:
- ✅ Response time < 3 seconds
- ✅ Transaction rollback on partial failure

### Test P.2: Analytics with Large Dataset
**Objective**: Dealer performance with 10,000+ analyses

**Expected**:
- ✅ Response time < 5 seconds
- ✅ Accurate calculations despite volume

---

## Deployment Steps

### Pre-Deployment
1. [ ] All build checkpoints passed (7/7 ✅)
2. [ ] Code review completed
3. [ ] Feature branch up to date with master
4. [ ] Staging database backup created
5. [ ] SQL migrations ready for execution

### Deployment to Staging
1. [ ] Merge feature branch to master: `git merge feature/sponsorship-code-distribution-experiment`
2. [ ] Push to remote: `git push origin master`
3. [ ] Railway auto-deployment triggered
4. [ ] Wait for deployment completion (~3 minutes)
5. [ ] Execute SQL migrations in order (001→002→003→004)
6. [ ] Verify migration success with verification queries

### Post-Deployment Verification
1. [ ] Health check: `GET /health`
2. [ ] Swagger UI accessible: `/swagger`
3. [ ] Test backward compatibility (Test 1.1, 1.2)
4. [ ] Test Method A (Tests 2.1-2.3)
5. [ ] Test Method C (Tests 4.1-4.2)
6. [ ] Verify authorization (Tests 8.1-8.3)
7. [ ] Check CloudWatch logs for errors
8. [ ] Monitor database connection pool
9. [ ] Verify no performance degradation

### Rollback Plan
If critical issues detected:
1. Revert master to previous commit
2. Railway auto-deploys previous version
3. Restore database backup
4. Investigate issues in feature branch

---

## Success Criteria Summary

### Functionality
- ✅ All 7 endpoints working correctly
- ✅ Three onboarding methods tested successfully
- ✅ Code transfer, reclaim, and analytics functional
- ✅ Backward compatibility maintained

### Performance
- ✅ No regression in existing functionality
- ✅ API response times within acceptable limits
- ✅ Database queries optimized with indexes

### Security
- ✅ Role-based access control enforced
- ✅ Authorization claims assigned correctly
- ✅ No unauthorized access possible

### Documentation
- ✅ API documentation complete
- ✅ Testing checklist created
- ✅ Development tracker updated
- ✅ SQL migration scripts documented

---

## Test Execution Log

| Test ID | Test Name | Status | Executed By | Date | Notes |
|---------|-----------|--------|-------------|------|-------|
| 1.1 | Backward Compatibility | ⏳ Pending | - | - | - |
| 1.2 | Existing Codes Usable | ⏳ Pending | - | - | - |
| 2.1 | Search Existing Dealer | ⏳ Pending | - | - | - |
| 2.2 | Transfer to Existing | ⏳ Pending | - | - | - |
| 2.3 | Dealer Uses Code | ⏳ Pending | - | - | - |
| 3.1 | Create Invitation | ⏳ Pending | - | - | - |
| 3.2 | List Invitations | ⏳ Pending | - | - | - |
| 4.1 | AutoCreate Dealer | ⏳ Pending | - | - | - |
| 4.2 | Login AutoCreated | ⏳ Pending | - | - | - |
| 5.1 | Reclaim Unused | ⏳ Pending | - | - | - |
| 5.2 | Cannot Reclaim Used | ⏳ Pending | - | - | - |
| 6.1 | Dealer Analytics | ⏳ Pending | - | - | - |
| 6.2 | Dealer Summary | ⏳ Pending | - | - | - |
| 7.1 | Messaging Filter | ⏳ Pending | - | - | - |
| 8.1 | Sponsor Access | ⏳ Pending | - | - | - |
| 8.2 | Admin Access | ⏳ Pending | - | - | - |
| 8.3 | Farmer Denied | ⏳ Pending | - | - | - |

---

**Document Version**: 1.0  
**Status**: READY FOR TESTING  
**Deployment Target**: Staging → Production  
**Last Review**: 2025-10-26
