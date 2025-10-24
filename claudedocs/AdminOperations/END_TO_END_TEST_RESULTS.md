# Admin Operations API - End-to-End Test Results

**Test Date:** 2025-10-23
**Tester:** Admin User (bilgitap@hotmail.com)
**Environment:** Staging (https://ziraai-api-sit.up.railway.app)
**API Version:** 1.0 (Header: x-dev-arch-version: 1.0)

---

## Test Configuration

**Base URL:** `https://ziraai-api-sit.up.railway.app`
**Admin Credentials:**
- Email: `bilgitap@hotmail.com`
- Password: `T0m122718817*-`

**Important Headers:**
- `Authorization: Bearer {token}`
- `x-dev-arch-version: 1.0`
- `Content-Type: application/json`

---

## Scenario 0: Admin Authentication

### 0.1 Admin Login

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/v1/Auth/Login
Content-Type: application/json

{
  "email": "bilgitap@hotmail.com",
  "password": "T0m122718817*-"
}
```

**Response:**
```json
{
  "data": {
    "claims": [
      "Admin",
      "admin.analytics.view",
      "admin.audit.view",
      "admin.plantanalysis.manage",
      "admin.sponsorship.manage",
      "admin.subscriptions.manage",
      "admin.users.manage",
      "PlantAnalysisCreate",
      "PlantAnalysisView",
      "SubscriptionView"
    ],
    "token": "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9...",
    "expiration": "2025-10-23T15:47:08.7614083+00:00",
    "refreshToken": "yyjaTU0wbtkAZJnY+GOfHv/WwGOn/h7XDm+gvldx/yA="
  },
  "success": true,
  "message": "SuccessfulLogin"
}
```

**Status:** ✅ PASS
**Notes:**
- Token expires in 60 minutes
- Admin has all required claims
- Token saved for subsequent requests

---

## Scenario 1: User Lifecycle Management (General User)

### 1.1 Get All Active Users

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users?page=1&pageSize=5&isActive=true
Authorization: Bearer eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": [
    {
      "userId": 166,
      "fullName": "Tolga KAYA",
      "email": "bilgitap@hotmail.com",
      "mobilePhones": "+905069468693",
      "address": "Not specified",
      "notes": "Registered via API",
      "gender": 0,
      "status": true,
      "refreshToken": "jI2LUYautvuhz66qhKajLynrZ0cuPeARHkgjyazei+4="
    },
    {
      "userId": 165,
      "fullName": "User 1113",
      "email": "05061111113@phone.ziraai.com",
      "mobilePhones": "05061111113",
      "address": "Not specified",
      "notes": "Registered via phone",
      "gender": 0,
      "status": true,
      "refreshToken": "13gERWE41T5az0Q9Sz/ELmBd8AAsyruRiaqonQUAgao="
    }
  ],
  "success": true,
  "message": "Users retrieved successfully"
}
```

**Status:** ✅ PASS
**Validation Checklist:**
- [x] Status Code: 200
- [x] Response contains array of users
- [x] Users have `status: true` (active)
- [x] Pagination working (5 users returned)
- [x] Total user count > 0

---

### 1.2 Search for Specific User

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/search?searchTerm=test&page=1&pageSize=10
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Search results contain "test" in name/email/phone
- [ ] Case-insensitive search works

---

### 1.2.1 Register Test User

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/v1/Auth/Register
Content-Type: application/json

{
  "fullName": "Test User General",
  "email": "testuser.general@test.com",
  "password": "TestPass123!",
  "mobilePhones": "+905559999001"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Save Values:**
- `testUserId_general`: [PASTE USER ID HERE]

**Validation Checklist:**
- [ ] Status Code: 200
- [ ] User created successfully
- [ ] UserId returned
- [ ] Email confirmed

---

### 1.3 Get User Details

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_general}
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] User details match registration
- [ ] `isActive: true`
- [ ] `deactivatedDate: null`

---

### 1.4 Deactivate User

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_general}/deactivate
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "reason": "TEST: Temporary deactivation for testing purposes"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Success message returned
- [ ] User deactivated

---

### 1.5 Verify Audit Log Entry

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/audit/target/{testUserId_general}?page=1&pageSize=10
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Audit log contains "DeactivateUser" action
- [ ] AdminUserId = 166 (bilgitap@hotmail.com)
- [ ] TargetUserId matches test user
- [ ] Reason field contains "TEST:"
- [ ] BeforeState and AfterState present

---

### 1.6 Reactivate User

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_general}/reactivate
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "reason": "TEST: Reactivating after test completion"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Success message returned
- [ ] User reactivated

---

### 1.7 Verify User Status

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_general}
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] `isActive: true`
- [ ] `deactivatedDate: null`
- [ ] `deactivatedByAdminId: null`

---

## Scenario 2: User Lifecycle Management (Farmer & Sponsor)

### 2.1 Get All Active Farmers

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users?page=1&pageSize=5&isActive=true
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Notes:** Filter farmers manually from response (check user role or subscription type)

**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Can identify farmer users
- [ ] Farmers have active status

---

### 2.1.2 Get All Active Sponsors

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users?page=1&pageSize=5&isActive=true
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Notes:** Filter sponsors manually from response

**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Can identify sponsor users
- [ ] Sponsors have active status

---

### 2.2 Search for Specific Farmer

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/search?searchTerm=farmer&page=1&pageSize=10
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Search results contain "farmer" keyword
- [ ] Results are farmer users

---

### 2.2.2 Search for Specific Sponsor

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/search?searchTerm=sponsor&page=1&pageSize=10
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Search results contain "sponsor" keyword
- [ ] Results are sponsor users

---

### 2.2.1a Register Test Farmer

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/v1/Auth/Register
Content-Type: application/json

{
  "fullName": "Test Farmer User",
  "email": "testfarmer@test.com",
  "password": "TestFarmer123!",
  "mobilePhones": "+905559999002"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Save Values:**
- `testUserId_farmer`: [PASTE USER ID HERE]

**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Farmer user created
- [ ] UserId returned

---

### 2.2.1b Register Test Sponsor

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/v1/Auth/Register
Content-Type: application/json

{
  "fullName": "Test Sponsor Company",
  "email": "testsponsor@test.com",
  "password": "TestSponsor123!",
  "mobilePhones": "+905559999003"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Save Values:**
- `testUserId_sponsor`: [PASTE USER ID HERE]

**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Sponsor user created
- [ ] UserId returned

---

### 2.3a Get Farmer Details

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_farmer}
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Farmer details correct
- [ ] `isActive: true`

---

### 2.3b Get Sponsor Details

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_sponsor}
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Sponsor details correct
- [ ] `isActive: true`

---

### 2.4a Deactivate Farmer

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_farmer}/deactivate
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "reason": "TEST: Farmer account deactivation test"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Farmer deactivated successfully

---

### 2.4b Deactivate Sponsor

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_sponsor}/deactivate
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "reason": "TEST: Sponsor account deactivation test"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Sponsor deactivated successfully

---

### 2.5a Verify Farmer Audit Log

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/audit/target/{testUserId_farmer}?page=1&pageSize=10
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] DeactivateUser action logged
- [ ] TargetUserId matches farmer
- [ ] Reason contains "TEST:"

---

### 2.5b Verify Sponsor Audit Log

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/audit/target/{testUserId_sponsor}?page=1&pageSize=10
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] DeactivateUser action logged
- [ ] TargetUserId matches sponsor
- [ ] Reason contains "TEST:"

---

### 2.6a Reactivate Farmer

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_farmer}/reactivate
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "reason": "TEST: Farmer account reactivation after test"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Farmer reactivated successfully

---

### 2.6b Reactivate Sponsor

**Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_sponsor}/reactivate
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "reason": "TEST: Sponsor account reactivation after test"
}
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Sponsor reactivated successfully

---

### 2.7a Verify Farmer Status

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_farmer}
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Farmer `isActive: true`
- [ ] `deactivatedDate: null`

---

### 2.7b Verify Sponsor Status

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/admin/users/{testUserId_sponsor}
Authorization: Bearer {admin_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
[PASTE RESPONSE HERE]
```

**Status:** ⏸️ PENDING
**Validation Checklist:**
- [ ] Status Code: 200
- [ ] Sponsor `isActive: true`
- [ ] `deactivatedDate: null`

---

## Test Summary

### Scenario 1: User Lifecycle Management (General User)
- **Total Steps:** 7
- **Passed:** 0
- **Failed:** 0
- **Pending:** 7

### Scenario 2: User Lifecycle Management (Farmer & Sponsor)
- **Total Steps:** 16
- **Passed:** 0
- **Failed:** 0
- **Pending:** 16

### Overall Summary
- **Total Test Cases:** 24 (including auth)
- **Pass Rate:** 0%
- **Completion Status:** Not Started

---

## Notes & Issues

### Issues Encountered:
1. [Add any issues here]

### Observations:
1. Admin token received successfully with all required claims
2. [Add observations as you test]

### Recommendations:
1. [Add recommendations after testing]

---

## Next Steps

1. Execute all pending test cases in Postman
2. Fill in responses and validation results
3. Update status for each test (✅ PASS, ❌ FAIL, ⚠️ WARNING)
4. Document any issues or unexpected behaviors
5. Calculate final pass rate
6. Create test summary report

---

**Test Executor:** [Your Name]
**Test Completion Date:** [Date]
**Final Status:** IN PROGRESS
