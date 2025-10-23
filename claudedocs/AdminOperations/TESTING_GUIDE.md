# Admin Operations API - Testing Guide

**Version:** 1.0
**Last Updated:** 2025-01-23
**Branch:** feature/step-by-step-admin-operations

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Test Scenarios](#test-scenarios)
4. [Validation Checklists](#validation-checklists)
5. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools
- ✅ Postman (v10.0+) or similar API client
- ✅ Database access (PostgreSQL)
- ✅ Admin account with proper permissions

### Required Access
- ✅ Admin JWT token
- ✅ Test user accounts (Farmer, Sponsor)
- ✅ Development/Staging environment access

---

## Environment Setup

### 1. Import Postman Collection

1. Open Postman
2. Click **Import** → **File**
3. Select `ZiraAI_Admin_Operations_API.postman_collection.json`
4. Collection will be imported with 31 endpoints

### 2. Configure Environment Variables

Create a new Postman environment with these variables:

```json
{
  "baseUrl": "https://localhost:5001",
  "adminToken": "",
  "testUserId": "123",
  "testSubscriptionId": "456",
  "testSponsorId": "234",
  "testPurchaseId": "892"
}
```

### 3. Get Admin Token

**Step 1:** Run authentication request
```http
POST {{baseUrl}}/api/auth/login
{
  "email": "admin@ziraai.com",
  "password": "Admin@123"
}
```

**Step 2:** Token is automatically saved to environment variable `{{adminToken}}`

**Step 3:** Verify token
```javascript
// In Postman Tests tab
pm.test("Token saved", function() {
    pm.expect(pm.environment.get("adminToken")).to.not.be.empty;
});
```

---

## Test Scenarios

### Scenario 1: User Lifecycle Management

**Objective:** Test complete user management workflow

#### Test Steps

**1.1 Get All Active Users**
```http
GET {{baseUrl}}/api/admin/users?page=1&pageSize=20&isActive=true
```

**Expected:**
- ✅ Status Code: 200
- ✅ Response contains user list
- ✅ Pagination info included

**Validation:**
```javascript
pm.test("Status code is 200", () => {
    pm.response.to.have.status(200);
});

pm.test("Response has user data", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;
    pm.expect(jsonData.data).to.be.an('array');
});

pm.test("Users have required fields", () => {
    const jsonData = pm.response.json();
    const user = jsonData.data[0];
    pm.expect(user).to.have.property('userId');
    pm.expect(user).to.have.property('fullName');
    pm.expect(user).to.have.property('isActive', true);
});
```

---

**1.2 Search for Specific User**
```http
GET {{baseUrl}}/api/admin/users/search?searchTerm=ahmet
```

**Expected:**
- ✅ Status Code: 200
- ✅ Results match search term
- ✅ Case-insensitive search

**Validation:**
```javascript
pm.test("Search returns results", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.data.length).to.be.above(0);
});

pm.test("Results contain search term", () => {
    const jsonData = pm.response.json();
    const searchTerm = "ahmet".toLowerCase();
    jsonData.data.forEach(user => {
        const inName = user.fullName.toLowerCase().includes(searchTerm);
        const inEmail = user.email.toLowerCase().includes(searchTerm);
        pm.expect(inName || inEmail).to.be.true;
    });
});
```

---

**1.3 Get User Details**
```http
GET {{baseUrl}}/api/admin/users/{{testUserId}}
```

**Expected:**
- ✅ Status Code: 200
- ✅ Full user details returned
- ✅ Admin action fields present

**Validation:**
```javascript
pm.test("User details complete", () => {
    const jsonData = pm.response.json();
    const user = jsonData.data;

    pm.expect(user).to.have.property('userId');
    pm.expect(user).to.have.property('fullName');
    pm.expect(user).to.have.property('email');
    pm.expect(user).to.have.property('isActive');
    pm.expect(user).to.have.property('createdDate');
});
```

---

**1.4 Deactivate User**
```http
POST {{baseUrl}}/api/admin/users/{{testUserId}}/deactivate
{
  "reason": "TEST: Temporary deactivation for testing"
}
```

**Expected:**
- ✅ Status Code: 200
- ✅ Success message returned
- ✅ Audit log created

**Validation:**
```javascript
pm.test("User deactivated successfully", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;
    pm.expect(jsonData.message).to.include("deactivated");
});

// Verify in database
// SELECT * FROM "Users" WHERE "UserId" = {{testUserId}}
// Expected: IsActive = false, DeactivatedDate = now, DeactivatedByAdminId = admin_id
```

---

**1.5 Verify Audit Log Entry**
```http
GET {{baseUrl}}/api/admin/audit/target/{{testUserId}}?page=1&pageSize=10
```

**Expected:**
- ✅ Status Code: 200
- ✅ DeactivateUser action logged
- ✅ Reason field populated
- ✅ Admin details captured

**Validation:**
```javascript
pm.test("Audit log entry created", () => {
    const jsonData = pm.response.json();
    const logs = jsonData.data;

    const deactivateLog = logs.find(log => log.action === "DeactivateUser");
    pm.expect(deactivateLog).to.exist;
    pm.expect(deactivateLog.targetUserId).to.equal(parseInt(pm.environment.get("testUserId")));
    pm.expect(deactivateLog.reason).to.include("TEST");
});
```

---

**1.6 Reactivate User**
```http
POST {{baseUrl}}/api/admin/users/{{testUserId}}/reactivate
{
  "reason": "TEST: Reactivating after test completion"
}
```

**Expected:**
- ✅ Status Code: 200
- ✅ User reactivated
- ✅ Reactivation logged

**Validation:**
```javascript
pm.test("User reactivated successfully", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;
    pm.expect(jsonData.message).to.include("reactivated");
});

// Verify in database
// SELECT * FROM "Users" WHERE "UserId" = {{testUserId}}
// Expected: IsActive = true, DeactivatedDate = null, DeactivatedByAdminId = null
```

---

**1.7 Verify User Status**
```http
GET {{baseUrl}}/api/admin/users/{{testUserId}}
```

**Expected:**
- ✅ isActive = true
- ✅ deactivatedDate = null
- ✅ deactivatedByAdminId = null

---

### Scenario 2: Subscription Management

**Objective:** Test subscription assignment and management

#### Test Steps

**2.1 Assign Premium Subscription**
```http
POST {{baseUrl}}/api/admin/subscriptions/assign
{
  "userId": {{testUserId}},
  "tierName": "M",
  "durationDays": 30,
  "reason": "TEST: Premium trial for testing"
}
```

**Expected:**
- ✅ Status Code: 200
- ✅ Subscription created
- ✅ Start and end dates correct
- ✅ Tier limits applied

**Validation:**
```javascript
pm.test("Subscription assigned successfully", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;

    const subscription = jsonData.data;
    pm.expect(subscription.userId).to.equal(parseInt(pm.environment.get("testUserId")));
    pm.expect(subscription.tierName).to.equal("M");
    pm.expect(subscription.status).to.equal("Active");

    // Save subscription ID for later tests
    pm.environment.set("testSubscriptionId", subscription.id);
});

pm.test("Dates are valid", () => {
    const subscription = pm.response.json().data;
    const startDate = new Date(subscription.startDate);
    const endDate = new Date(subscription.endDate);
    const diffDays = (endDate - startDate) / (1000 * 60 * 60 * 24);

    pm.expect(diffDays).to.be.closeTo(30, 1);
});
```

---

**2.2 Verify Subscription Active**
```http
GET {{baseUrl}}/api/admin/subscriptions/{{testSubscriptionId}}
```

**Expected:**
- ✅ Status: Active
- ✅ Usage counters = 0
- ✅ Limits set correctly

**Validation:**
```javascript
pm.test("Subscription is active", () => {
    const subscription = pm.response.json().data;
    pm.expect(subscription.status).to.equal("Active");
    pm.expect(subscription.dailyUsage).to.equal(0);
    pm.expect(subscription.monthlyUsage).to.equal(0);
    pm.expect(subscription.dailyLimit).to.be.above(0);
});
```

---

**2.3 Extend Subscription**
```http
POST {{baseUrl}}/api/admin/subscriptions/{{testSubscriptionId}}/extend
{
  "additionalDays": 15,
  "reason": "TEST: Testing subscription extension"
}
```

**Expected:**
- ✅ End date extended by 15 days
- ✅ Extension logged

**Validation:**
```javascript
pm.test("Subscription extended", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;

    const data = jsonData.data;
    const oldDate = new Date(data.oldEndDate);
    const newDate = new Date(data.newEndDate);
    const diffDays = (newDate - oldDate) / (1000 * 60 * 60 * 24);

    pm.expect(diffDays).to.be.closeTo(15, 1);
});
```

---

**2.4 Get Subscription Statistics**
```http
GET {{baseUrl}}/api/admin/analytics/subscriptions?startDate=2025-01-01
```

**Expected:**
- ✅ Total subscriptions count increased
- ✅ Active subscriptions count correct
- ✅ Tier breakdown accurate

**Validation:**
```javascript
pm.test("Statistics include test subscription", () => {
    const stats = pm.response.json().data;
    pm.expect(stats.totalSubscriptions).to.be.above(0);
    pm.expect(stats.activeSubscriptions).to.be.above(0);
    pm.expect(stats.subscriptionsByTier).to.have.property("M");
});
```

---

**2.5 Cancel Subscription**
```http
POST {{baseUrl}}/api/admin/subscriptions/{{testSubscriptionId}}/cancel
{
  "reason": "TEST: Cleaning up test subscription",
  "refundAmount": 0
}
```

**Expected:**
- ✅ Status: Cancelled
- ✅ Cancellation logged

---

### Scenario 3: Sponsor On-Behalf-Of Operations

**Objective:** Test complete sponsor package creation and code distribution

#### Test Steps

**3.1 Create Purchase On Behalf Of Sponsor**
```http
POST {{baseUrl}}/api/admin/sponsorship/purchases/create-on-behalf-of
{
  "sponsorId": {{testSponsorId}},
  "subscriptionTierId": 3,
  "quantity": 10,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TEST-2025-0123-001",
  "companyName": "Test Sponsorship Company",
  "taxNumber": "1234567890",
  "invoiceAddress": "Test Address, Istanbul",
  "codePrefix": "TEST",
  "validityDays": 365,
  "notes": "TEST: Demo purchase for testing OBO functionality"
}
```

**Expected:**
- ✅ Purchase created with status: Active
- ✅ PaymentStatus: Completed
- ✅ isOnBehalfOf: true
- ✅ Codes not yet generated

**Validation:**
```javascript
pm.test("Purchase created successfully", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;

    const purchase = jsonData.data;
    pm.expect(purchase.sponsorId).to.equal(parseInt(pm.environment.get("testSponsorId")));
    pm.expect(purchase.quantity).to.equal(10);
    pm.expect(purchase.paymentStatus).to.equal("Completed");
    pm.expect(purchase.status).to.equal("Active");

    // Save purchase ID
    pm.environment.set("testPurchaseId", purchase.id);
});

pm.test("Purchase has correct amount", () => {
    const purchase = pm.response.json().data;
    pm.expect(purchase.totalAmount).to.equal(999.90);
    pm.expect(purchase.currency).to.equal("TRY");
});
```

---

**3.2 Verify Purchase Created**
```http
GET {{baseUrl}}/api/admin/sponsorship/purchases/{{testPurchaseId}}
```

**Expected:**
- ✅ Purchase details match
- ✅ Company info populated
- ✅ Code prefix saved

**Validation:**
```javascript
pm.test("Purchase details correct", () => {
    const purchase = pm.response.json().data;
    pm.expect(purchase.companyName).to.equal("Test Sponsorship Company");
    pm.expect(purchase.codePrefix).to.equal("TEST");
    pm.expect(purchase.validityDays).to.equal(365);
});
```

---

**3.3 Check Audit Log for Purchase Creation**
```http
GET {{baseUrl}}/api/admin/audit/on-behalf-of?action=CreatePurchaseOnBehalfOf&entityType=SponsorshipPurchase
```

**Expected:**
- ✅ Audit entry exists
- ✅ isOnBehalfOf = true
- ✅ AdminUserId populated
- ✅ TargetUserId = sponsorId

**Validation:**
```javascript
pm.test("OBO audit log created", () => {
    const logs = pm.response.json().data;
    const purchaseLog = logs.find(log =>
        log.entityId === parseInt(pm.environment.get("testPurchaseId"))
    );

    pm.expect(purchaseLog).to.exist;
    pm.expect(purchaseLog.isOnBehalfOf).to.be.true;
    pm.expect(purchaseLog.targetUserId).to.equal(parseInt(pm.environment.get("testSponsorId")));
    pm.expect(purchaseLog.reason).to.include("TEST");
});
```

---

**3.4 Get Unused Codes from Purchase**

```sql
-- Database query to get unused codes
SELECT * FROM "SponsorshipCodes"
WHERE "SponsorshipPurchaseId" = {{testPurchaseId}}
  AND "IsUsed" = false
  AND "IsActive" = true
LIMIT 3;
```

**Expected:**
- ✅ At least 3 unused codes available
- ✅ Codes have correct prefix

---

**3.5 Bulk Send Codes to Farmers**
```http
POST {{baseUrl}}/api/admin/sponsorship/codes/bulk-send
{
  "sponsorId": {{testSponsorId}},
  "purchaseId": {{testPurchaseId}},
  "recipients": [
    {"phoneNumber": "+905551111111", "name": "Test Farmer 1"},
    {"phoneNumber": "+905552222222", "name": "Test Farmer 2"},
    {"phoneNumber": "+905553333333", "name": "Test Farmer 3"}
  ],
  "sendVia": "SMS"
}
```

**Expected:**
- ✅ Status Code: 200
- ✅ 3 codes sent
- ✅ Codes marked as sent
- ✅ Recipient info saved

**Validation:**
```javascript
pm.test("Codes sent successfully", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;
    pm.expect(jsonData.message).to.include("3 codes");
});

// Database verification
// SELECT * FROM "SponsorshipCodes"
// WHERE "SponsorshipPurchaseId" = {{testPurchaseId}}
//   AND "LinkSentDate" IS NOT NULL
// Expected: 3 records with RecipientPhone populated
```

---

**3.6 Get Sponsor Detailed Report**
```http
GET {{baseUrl}}/api/admin/sponsorship/sponsor/{{testSponsorId}}/report
```

**Expected:**
- ✅ Purchase appears in list
- ✅ Code statistics accurate
- ✅ Sent codes count = 3

**Validation:**
```javascript
pm.test("Sponsor report accurate", () => {
    const report = pm.response.json().data;

    // Check purchases
    const purchase = report.purchases.find(p =>
        p.id === parseInt(pm.environment.get("testPurchaseId"))
    );
    pm.expect(purchase).to.exist;
    pm.expect(purchase.codesSent).to.equal(3);
    pm.expect(purchase.codesGenerated).to.equal(10);

    // Check statistics
    pm.expect(report.totalCodesSent).to.be.at.least(3);
    pm.expect(report.codeDistribution.sent).to.be.at.least(3);
});
```

---

### Scenario 4: Bulk Operations

**Objective:** Test bulk processing efficiency

#### Test Steps

**4.1 Create Test Users**

```sql
-- Create 5 test users for bulk operations
INSERT INTO "Users" ("FullName", "Email", "MobilePhones", "IsActive", "CreatedDate")
VALUES
  ('Bulk Test User 1', 'bulk1@test.com', '+905551111111', true, NOW()),
  ('Bulk Test User 2', 'bulk2@test.com', '+905552222222', true, NOW()),
  ('Bulk Test User 3', 'bulk3@test.com', '+905553333333', true, NOW()),
  ('Bulk Test User 4', 'bulk4@test.com', '+905554444444', true, NOW()),
  ('Bulk Test User 5', 'bulk5@test.com', '+905555555555', true, NOW())
RETURNING "UserId";
```

---

**4.2 Bulk Deactivate Users**
```http
POST {{baseUrl}}/api/admin/users/bulk/deactivate
{
  "userIds": [101, 102, 103, 104, 105],
  "reason": "TEST: Bulk deactivation test"
}
```

**Expected:**
- ✅ All 5 users deactivated
- ✅ Individual audit logs created
- ✅ Success count = 5

**Validation:**
```javascript
pm.test("Bulk operation successful", () => {
    const jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;

    const data = jsonData.data;
    pm.expect(data.totalRequested).to.equal(5);
    pm.expect(data.successCount).to.equal(5);
    pm.expect(data.failedCount).to.equal(0);
});

pm.test("All users deactivated", () => {
    const results = pm.response.json().data.results;
    results.forEach(result => {
        pm.expect(result.success).to.be.true;
    });
});
```

---

**4.3 Verify Audit Logs**
```http
GET {{baseUrl}}/api/admin/audit?action=BulkDeactivateUsers&page=1&pageSize=10
```

**Expected:**
- ✅ 5 audit entries created
- ✅ Each has correct targetUserId

---

**4.4 Cleanup Test Users**

```sql
-- Delete test users
DELETE FROM "Users"
WHERE "Email" LIKE 'bulk%@test.com';
```

---

### Scenario 5: Analytics and Reporting

**Objective:** Validate statistics accuracy and CSV export

#### Test Steps

**5.1 Get User Statistics**
```http
GET {{baseUrl}}/api/admin/analytics/users?startDate=2024-12-01&endDate=2025-01-23
```

**Expected:**
- ✅ TotalUsers > 0
- ✅ ActiveUsers + InactiveUsers = TotalUsers
- ✅ NewUsersToday >= 0

**Validation:**
```javascript
pm.test("User statistics valid", () => {
    const stats = pm.response.json().data;

    pm.expect(stats.totalUsers).to.be.above(0);
    pm.expect(stats.activeUsers + stats.inactiveUsers).to.equal(stats.totalUsers);
    pm.expect(stats.newUsersToday).to.be.at.least(0);
});
```

---

**5.2 Get Sponsorship Statistics**
```http
GET {{baseUrl}}/api/admin/analytics/sponsorship
```

**Expected:**
- ✅ TotalPurchases includes test purchase
- ✅ TotalCodesGenerated >= test quantity
- ✅ CodeRedemptionRate between 0-100

**Validation:**
```javascript
pm.test("Sponsorship statistics valid", () => {
    const stats = pm.response.json().data;

    pm.expect(stats.totalPurchases).to.be.above(0);
    pm.expect(stats.codeRedemptionRate).to.be.within(0, 100);
    pm.expect(stats.totalCodesGenerated).to.be.at.least(10); // Our test purchase
});
```

---

**5.3 Export Statistics to CSV**
```http
GET {{baseUrl}}/api/admin/analytics/export?reportType=Sponsorship&startDate=2024-01-01&endDate=2025-01-23
```

**Expected:**
- ✅ Content-Type: text/csv
- ✅ File downloads successfully
- ✅ CSV format valid

**Validation:**
```javascript
pm.test("CSV export successful", () => {
    pm.response.to.have.status(200);
    pm.response.to.have.header("Content-Type", "text/csv");
});

pm.test("CSV contains data", () => {
    const csv = pm.response.text();
    pm.expect(csv).to.include("Metric,Value");
    pm.expect(csv).to.include("Total Purchases");
});
```

---

**5.4 Verify CSV Content**

Download the CSV and verify:
- ✅ Headers present
- ✅ All metrics included
- ✅ Values match API response
- ✅ Timestamp included

---

## Validation Checklists

### Pre-Test Checklist

- [ ] Database backup created
- [ ] Test environment configured
- [ ] Admin token obtained
- [ ] Test users created
- [ ] Environment variables set

### Post-Test Checklist

- [ ] Test data cleaned up
- [ ] Database state verified
- [ ] Audit logs reviewed
- [ ] Performance metrics captured
- [ ] Issues documented

### Audit Trail Verification

For every operation, verify:
- [ ] AdminOperationLog entry created
- [ ] Action field correct
- [ ] AdminUserId populated
- [ ] TargetUserId correct (if applicable)
- [ ] Reason field populated
- [ ] Timestamp accurate
- [ ] IP address captured
- [ ] BeforeState/AfterState present

### Database State Verification

After deactivate user:
```sql
SELECT "UserId", "IsActive", "DeactivatedDate", "DeactivatedByAdminId"
FROM "Users"
WHERE "UserId" = {{testUserId}};
```

After assign subscription:
```sql
SELECT "Id", "UserId", "TierName", "Status", "StartDate", "EndDate"
FROM "Subscriptions"
WHERE "UserId" = {{testUserId}}
ORDER BY "CreatedDate" DESC
LIMIT 1;
```

After create purchase:
```sql
SELECT p.*,
       (SELECT COUNT(*) FROM "SponsorshipCodes" WHERE "SponsorshipPurchaseId" = p."Id") as "CodeCount"
FROM "SponsorshipPurchases" p
WHERE p."Id" = {{testPurchaseId}};
```

---

## Troubleshooting

### Issue: 401 Unauthorized

**Symptoms:**
```json
{
  "success": false,
  "message": "Authorization token is required"
}
```

**Solutions:**
1. Verify admin token is set: `{{adminToken}}`
2. Re-run authentication request
3. Check token expiration
4. Verify Bearer token format in headers

---

### Issue: 403 Forbidden

**Symptoms:**
```json
{
  "success": false,
  "message": "Insufficient permissions"
}
```

**Solutions:**
1. Verify user has Admin role
2. Check operation claims in database
3. Review SecuredOperation aspect configuration
4. Ensure proper claim assignment

---

### Issue: Audit Log Not Created

**Symptoms:**
- Operation succeeds but no audit log

**Solutions:**
1. Check AdminAuditService registration
2. Verify database connection
3. Review transaction scope
4. Check for exceptions in logs

---

### Issue: CSV Export Fails

**Symptoms:**
- 500 error on export endpoint

**Solutions:**
1. Verify reportType parameter (Users/Subscriptions/Sponsorship)
2. Check date range validity
3. Ensure sufficient data exists
4. Review file write permissions

---

### Issue: Bulk Operation Partial Failure

**Symptoms:**
```json
{
  "totalRequested": 5,
  "successCount": 3,
  "failedCount": 2
}
```

**Solutions:**
1. Review individual failure reasons in results array
2. Check entity existence
3. Verify no FK constraints violated
4. Ensure entities not already in target state

---

## Performance Benchmarks

### Expected Response Times

| Endpoint | Expected Time | Max Acceptable |
|----------|--------------|----------------|
| Get All Users (50 items) | < 200ms | 500ms |
| Get User By ID | < 50ms | 150ms |
| Deactivate User | < 100ms | 300ms |
| Bulk Deactivate (10 users) | < 500ms | 1000ms |
| Get Statistics | < 300ms | 800ms |
| Export CSV | < 1000ms | 3000ms |
| Create Purchase OBO | < 500ms | 1500ms |
| Bulk Send Codes (10) | < 1000ms | 3000ms |

### Database Query Performance

```sql
-- Check slow queries
SELECT query, mean_exec_time, calls
FROM pg_stat_statements
WHERE query LIKE '%AdminOperationLog%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

---

## Test Data Cleanup

### After Testing Complete

```sql
-- Clean up test users
DELETE FROM "Users" WHERE "Email" LIKE '%@test.com';

-- Clean up test subscriptions
DELETE FROM "Subscriptions" WHERE "Notes" LIKE 'TEST:%';

-- Clean up test purchases
DELETE FROM "SponsorshipPurchases" WHERE "Notes" LIKE 'TEST:%';

-- Clean up test codes
DELETE FROM "SponsorshipCodes" WHERE "CodePrefix" = 'TEST';

-- Clean up test audit logs
DELETE FROM "AdminOperationLogs" WHERE "Reason" LIKE 'TEST:%';
```

---

## Appendix: Test User Setup

### Create Test Admin User

```sql
INSERT INTO "Users" ("FullName", "Email", "MobilePhones", "IsActive", "CreatedDate")
VALUES ('Test Admin', 'admin@test.com', '+905550000000', true, NOW())
RETURNING "UserId";

-- Get UserId from above, then assign Admin role
INSERT INTO "UserOperationClaims" ("UserId", "OperationClaimId")
SELECT :userId, "Id"
FROM "OperationClaims"
WHERE "Name" LIKE 'admin.%';
```

### Create Test Farmer

```sql
INSERT INTO "Users" ("FullName", "Email", "MobilePhones", "IsActive", "CreatedDate")
VALUES ('Test Farmer', 'farmer@test.com', '+905551111111', true, NOW())
RETURNING "UserId";
```

### Create Test Sponsor

```sql
INSERT INTO "Users" ("FullName", "Email", "MobilePhones", "IsActive", "CreatedDate")
VALUES ('Test Sponsor Company', 'sponsor@test.com', '+905552222222', true, NOW())
RETURNING "UserId";
```

---

**End of Testing Guide**

For questions or issues, please refer to the main documentation or create an issue in the GitHub repository.
