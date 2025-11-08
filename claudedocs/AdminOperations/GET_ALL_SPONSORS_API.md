# Get All Sponsors API Documentation

**Version:** 1.0
**Created:** 2025-11-08
**Endpoint:** `GET /api/admin/sponsorship/sponsors`
**Handler:** `GetAllSponsorsQuery`
**Authorization:** Admin only (Claim ID: 107)

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Request Details](#request-details)
4. [Response Details](#response-details)
5. [Examples](#examples)
6. [Error Handling](#error-handling)
7. [Use Cases](#use-cases)

---

## Overview

This endpoint retrieves a paginated list of all users with the **Sponsor role** (GroupId = 3) in the system. It provides filtering capabilities for active status and user status, making it ideal for admin dashboards and sponsor management workflows.

### Key Features

✅ **Group-Based Filtering** - Only returns users with GroupId = 3 (Sponsors)
✅ **Pagination** - Supports page-based navigation with configurable page size
✅ **Status Filtering** - Filter by active status and user status
✅ **Security** - Requires admin authorization with specific operation claim
✅ **Performance** - Optimized queries with DTO projection
✅ **Safe Data Transfer** - Prevents DateTime infinity value errors

### Technical Details

- **HTTP Method:** GET
- **Route:** `/api/admin/sponsorship/sponsors`
- **Controller:** `AdminSponsorshipController`
- **Base Route:** `/api/admin/sponsorship`
- **Handler File:** `Business/Handlers/AdminSponsorship/Queries/GetAllSponsorsQuery.cs`
- **Response Type:** `IDataResult<IEnumerable<UserDto>>`

---

## Authentication

### Required Headers

```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

### Required Claims

- **Claim ID:** 107
- **Claim Name:** `GetAllSponsorsQuery`
- **Assigned To:** Administrators group (GroupId = 1)

### Authorization Flow

1. JWT token validated
2. User ID extracted from token
3. User's claims fetched from cache
4. `GetAllSponsorsQuery` claim verified
5. If claim missing → **401 Unauthorized**
6. If claim present → Request proceeds

### Important Notes

⚠️ **After SQL migration**: Admin users MUST logout/login to refresh claim cache
⚠️ **Token expiry**: 60 minutes (access token), 180 minutes (refresh token)
⚠️ **Missing aspects**: If `[PerformanceAspect(5)]` is missing, endpoint will return 401

---

## Request Details

### Endpoint URL

```
GET https://{base_url}/api/admin/sponsorship/sponsors
```

**Environments:**
- **Development:** `https://localhost:5001/api/admin/sponsorship/sponsors`
- **Staging:** `https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors`
- **Production:** `https://api.ziraai.com/api/admin/sponsorship/sponsors`

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number for pagination (must be ≥ 1) |
| `pageSize` | int | No | 50 | Number of items per page (1-100) |
| `isActive` | bool? | No | null | Filter by active status (true/false/null) |
| `status` | string | No | null | Filter by user status ("Active"/"Inactive"/null) |

### Parameter Details

#### `page` (int, optional)
- **Default:** 1
- **Minimum:** 1
- **Description:** Page number for pagination
- **Example:** `page=2` returns the second page of results

#### `pageSize` (int, optional)
- **Default:** 50
- **Range:** 1-100 (recommended)
- **Description:** Number of sponsors to return per page
- **Example:** `pageSize=20` returns 20 sponsors per page

#### `isActive` (bool?, optional)
- **Default:** null (all users)
- **Values:**
  - `true` - Only active sponsors
  - `false` - Only inactive sponsors
  - `null` or omitted - All sponsors
- **Example:** `isActive=true` returns only active sponsors

#### `status` (string, optional)
- **Default:** null (all statuses)
- **Values:** "Active", "Inactive", or null
- **Case Sensitive:** No
- **Example:** `status=Active` returns only sponsors with status = Active

### Request Headers

```http
GET /api/admin/sponsorship/sponsors?page=1&pageSize=20&isActive=true HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
Accept: application/json
```

### No Request Body

This is a GET request and does not accept a request body.

---

## Response Details

### Success Response (200 OK)

#### Response Structure

```json
{
  "success": true,
  "message": "Retrieved {count} sponsors successfully",
  "data": [
    {
      "userId": 159,
      "fullName": "string",
      "email": "string",
      "mobilePhones": "string",
      "address": "string",
      "notes": "string",
      "gender": 0,
      "status": true,
      "isActive": true,
      "password": null,
      "refreshToken": null
    }
  ]
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Always `true` for successful requests |
| `message` | string | Success message with count of sponsors |
| `data` | array | Array of UserDto objects |

#### UserDto Object

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `userId` | int | No | Unique user identifier |
| `fullName` | string | Yes | User's full name |
| `email` | string | Yes | User's email address |
| `mobilePhones` | string | Yes | User's mobile phone number(s) |
| `address` | string | Yes | User's address |
| `notes` | string | Yes | Admin notes about the user |
| `gender` | int | No | Gender (0=Not specified, 1=Male, 2=Female) |
| `status` | boolean | No | User status (true=Active, false=Inactive) |
| `isActive` | boolean | No | Account active status |
| `password` | string | Yes | Always null (security) |
| `refreshToken` | string | Yes | Always null (security) |

### Error Responses

#### 401 Unauthorized

**Cause:** Missing or invalid JWT token, or missing claim

```json
{
  "success": false,
  "message": "AuthorizationsDenied",
  "data": null
}
```

**Solutions:**
1. Ensure valid JWT token in Authorization header
2. Verify admin user has `GetAllSponsorsQuery` claim
3. Logout/login if claim was recently added to database
4. Check token expiry

#### 404 Not Found

**Cause:** Incorrect endpoint URL

```json
{
  "success": false,
  "message": "Not Found",
  "data": null
}
```

**Solutions:**
1. Verify endpoint URL: `/api/admin/sponsorship/sponsors`
2. Check route is correctly registered in controller

#### 500 Internal Server Error

**Cause:** Database connection error, handler exception

```json
{
  "success": false,
  "message": "An error occurred while processing your request",
  "data": null
}
```

**Solutions:**
1. Check database connection
2. Review server logs for stack trace
3. Verify all dependencies are injected correctly

---

## Examples

### Example 1: Get First Page (Default)

**Request:**
```http
GET /api/admin/sponsorship/sponsors HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjE1NSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJBZG1pbiBVc2VyIiwiZW1haWwiOiJhZG1pbkB6aXJhYWkuY29tIiwibmJmIjoxNzMxMDgzMjAwLCJleHAiOjE3MzEwODY4MDAsImlzcyI6InppcmFhaS5jb20iLCJhdWQiOiJ6aXJhYWkuY29tIn0.xyz123
x-dev-arch-version: 1.0
Accept: application/json
```

**Response:**
```json
{
  "success": true,
  "message": "Retrieved 15 sponsors successfully",
  "data": [
    {
      "userId": 159,
      "fullName": "John Sponsor",
      "email": "john@sponsor.com",
      "mobilePhones": "+905551234567",
      "address": "Istanbul, Turkey",
      "notes": "Premium sponsor",
      "gender": 1,
      "status": true,
      "isActive": true,
      "password": null,
      "refreshToken": null
    },
    {
      "userId": 158,
      "fullName": "Jane Dealer",
      "email": "jane@dealer.com",
      "mobilePhones": "+905559876543",
      "address": "Ankara, Turkey",
      "notes": "Active dealer account",
      "gender": 2,
      "status": true,
      "isActive": true,
      "password": null,
      "refreshToken": null
    }
  ]
}
```

---

### Example 2: Get Active Sponsors Only

**Request:**
```http
GET /api/admin/sponsorship/sponsors?isActive=true&pageSize=10 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer {token}
x-dev-arch-version: 1.0
Accept: application/json
```

**cURL:**
```bash
curl -X GET 'https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors?isActive=true&pageSize=10' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...' \
  -H 'x-dev-arch-version: 1.0' \
  -H 'Accept: application/json'
```

**Response:**
```json
{
  "success": true,
  "message": "Retrieved 8 sponsors successfully",
  "data": [
    {
      "userId": 159,
      "fullName": "Active Sponsor 1",
      "email": "active1@sponsor.com",
      "mobilePhones": "+905551111111",
      "address": "Istanbul",
      "notes": "",
      "gender": 1,
      "status": true,
      "isActive": true,
      "password": null,
      "refreshToken": null
    }
  ]
}
```

---

### Example 3: Pagination - Second Page

**Request:**
```http
GET /api/admin/sponsorship/sponsors?page=2&pageSize=20 HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**JavaScript (Fetch):**
```javascript
const response = await fetch('https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors?page=2&pageSize=20', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
    'x-dev-arch-version': '1.0',
    'Accept': 'application/json'
  }
});

const result = await response.json();
console.log(`Retrieved ${result.data.length} sponsors from page 2`);
```

**Response:**
```json
{
  "success": true,
  "message": "Retrieved 20 sponsors successfully",
  "data": [
    // ... 20 sponsor objects from page 2
  ]
}
```

---

### Example 4: Filter by Status

**Request:**
```http
GET /api/admin/sponsorship/sponsors?status=Active&isActive=true HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Python (Requests):**
```python
import requests

url = "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors"
headers = {
    "Authorization": f"Bearer {token}",
    "x-dev-arch-version": "1.0",
    "Accept": "application/json"
}
params = {
    "status": "Active",
    "isActive": True
}

response = requests.get(url, headers=headers, params=params)
data = response.json()

print(f"Success: {data['success']}")
print(f"Message: {data['message']}")
print(f"Sponsors count: {len(data['data'])}")
```

**Response:**
```json
{
  "success": true,
  "message": "Retrieved 12 sponsors successfully",
  "data": [
    // ... sponsor objects with status=Active and isActive=true
  ]
}
```

---

### Example 5: Postman Collection

**Request Configuration:**

```
Method: GET
URL: {{base_url}}/api/admin/sponsorship/sponsors
Headers:
  - Authorization: Bearer {{admin_token}}
  - x-dev-arch-version: 1.0
  - Accept: application/json

Query Params:
  - page: 1
  - pageSize: 20
  - isActive: true
  - status: Active

Tests Script:
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has success=true", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.eql(true);
});

pm.test("Data is array", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.data).to.be.an('array');
});

pm.test("All users have GroupId=3 (Sponsors)", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.data.length).to.be.above(0);
});
```

---

## Error Handling

### Common Issues and Solutions

#### Issue 1: 401 Unauthorized After SQL Migration

**Symptoms:**
```json
{
  "success": false,
  "message": "AuthorizationsDenied"
}
```

**Root Cause:** User's claim cache not refreshed after adding claim to database

**Solution:**
1. Admin user must **logout** from application
2. **Login** again to refresh claim cache
3. Claims are cached with key: `CacheKeys.UserIdForClaim={userId}`
4. Retry the request

**Prevention:** Always logout/login after running SQL migrations that add new claims

---

#### Issue 2: Empty Data Array

**Symptoms:**
```json
{
  "success": true,
  "message": "Retrieved 0 sponsors successfully",
  "data": []
}
```

**Root Cause:** No users with GroupId = 3 in database, or all filtered out

**Solution:**
1. Verify users exist with GroupId = 3:
   ```sql
   SELECT u.UserId, u.FullName, u.Email
   FROM Users u
   INNER JOIN UserGroups ug ON u.UserId = ug.UserId
   WHERE ug.GroupId = 3;
   ```
2. Check filter parameters (isActive, status) aren't too restrictive
3. Ensure test data includes sponsor users

---

#### Issue 3: PerformanceAspect Missing → 401 Error

**Symptoms:**
- Database has correct claim (ID 107)
- User is in Administrators group
- User logged out/in
- **Still getting 401 Unauthorized**

**Root Cause:** Handler missing `[PerformanceAspect(5)]` attribute

**Verification:**
```csharp
// ❌ WRONG - Will cause 401
[SecuredOperation(Priority = 1)]
[LogAspect(typeof(FileLogger))]
public async Task<IDataResult<...>> Handle(...)

// ✅ CORRECT
[SecuredOperation(Priority = 1)]
[PerformanceAspect(5)]
[LogAspect(typeof(FileLogger))]
public async Task<IDataResult<...>> Handle(...)
```

**Solution:** Add missing aspect to handler and redeploy

---

## Use Cases

### Use Case 1: Admin Dashboard - Sponsor List

**Scenario:** Admin wants to view all active sponsors in the system

**Request:**
```http
GET /api/admin/sponsorship/sponsors?isActive=true&pageSize=50
```

**Workflow:**
1. Admin navigates to Sponsor Management dashboard
2. Frontend calls API with isActive=true
3. Display sponsors in paginated table
4. Show total count in header
5. Enable click-to-view-details functionality

---

### Use Case 2: Bulk Notification - Select Recipients

**Scenario:** Admin wants to send bulk notification to specific sponsors

**Request:**
```http
GET /api/admin/sponsorship/sponsors?isActive=true&pageSize=100
```

**Workflow:**
1. Load all active sponsors
2. Display with checkboxes for selection
3. Admin selects 10 sponsors
4. Compose notification message
5. Send to selected sponsor IDs
6. Track delivery status

---

### Use Case 3: Create Purchase On-Behalf-Of

**Scenario:** Admin creates manual purchase for sponsor (offline payment)

**Request:**
```http
GET /api/admin/sponsorship/sponsors?status=Active
```

**Workflow:**
1. Admin initiates "Create Purchase OBO" workflow
2. API loads active sponsors for dropdown/autocomplete
3. Admin selects sponsor from list
4. Completes purchase form with tier, quantity, price
5. Sets autoApprove=true for offline payment
6. System creates purchase and assigns to selected sponsor

---

### Use Case 4: Reporting - Sponsor Activity

**Scenario:** Generate monthly sponsor activity report

**Request:**
```http
GET /api/admin/sponsorship/sponsors?pageSize=1000
```

**Workflow:**
1. Load all sponsors (large page size for report)
2. For each sponsor, fetch purchase statistics
3. Calculate total revenue per sponsor
4. Calculate code distribution metrics
5. Generate PDF/Excel report
6. Email to stakeholders

---

### Use Case 5: Support - Search Sponsor

**Scenario:** Support team needs to find sponsor by email

**Request:**
```http
GET /api/admin/sponsorship/sponsors?pageSize=100
```

**Client-Side Filtering:**
```javascript
const sponsors = response.data.data;
const found = sponsors.filter(s =>
  s.email.toLowerCase().includes(searchTerm.toLowerCase())
);
```

**Better Approach:** Use dedicated search endpoint (future enhancement)

---

## Performance Considerations

### Query Optimization

- **IQueryable Chain:** Efficient database query construction
- **DTO Projection:** Projects to DTO before ToList() to minimize data transfer
- **Indexed Columns:** GroupId is indexed for fast filtering
- **Pagination:** Prevents loading entire table into memory

### Performance Monitoring

- **PerformanceAspect(5):** Logs warning if query exceeds 5 seconds
- **Recommended Page Size:** 20-50 items for optimal response time
- **Maximum Page Size:** 100 (higher values may cause timeout)

### Caching Strategy

- **No endpoint caching:** Data changes frequently
- **Claim caching:** User claims cached for token lifetime
- **Recommended:** Implement client-side caching for 1-2 minutes

---

## Security Notes

### Data Exposure

✅ **Password field:** Always null (never exposed)
✅ **RefreshToken field:** Always null (never exposed)
✅ **Admin users excluded:** Only Sponsor group users returned
⚠️ **PII data:** Contains email, phone, address (admin-only access justified)

### Authorization

- **SecuredOperation aspect:** Enforces claim-based authorization
- **Claim verification:** Checked on every request (no bypass)
- **Token validation:** JWT signature verified
- **Group isolation:** Only GroupId = 3 users returned

### Audit Logging

- **LogAspect:** All requests logged to file
- **AdminOperationLog:** Not created (read-only operation)
- **Performance tracking:** Slow queries logged for monitoring

---

## Related Endpoints

- **GET** `/api/admin/sponsorship/purchases` - List sponsorship purchases
- **POST** `/api/admin/sponsorship/purchases/create-on-behalf-of` - Create purchase for sponsor
- **GET** `/api/admin/sponsorship/codes` - List sponsorship codes
- **POST** `/api/admin/sponsorship/codes/bulk-send` - Send codes to farmers
- **GET** `/api/admin/sponsorship/sponsors/{sponsorId}/detailed-report` - Sponsor report (currently 404)

---

## Migration & Deployment

### SQL Migration Required

**File:** `claudedocs/AdminOperations/ADD_GET_ALL_SPONSORS_CLAIM.sql`

```sql
-- Insert claim
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 107, 'GetAllSponsorsQuery', 'Get All Sponsors', 'Query all users with Sponsor role (GroupId = 3)'
WHERE NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Name" = 'GetAllSponsorsQuery');

-- Assign to Administrators
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 107
WHERE NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 107);
```

### Deployment Checklist

- [ ] Run SQL migration on database
- [ ] Verify claim created (ID 107)
- [ ] Verify claim assigned to Administrators group
- [ ] Push code changes to repository
- [ ] Deploy to staging environment
- [ ] **Admin logout/login** (CRITICAL - cache refresh)
- [ ] Test endpoint: `GET /api/admin/sponsorship/sponsors`
- [ ] Verify 200 OK response (not 401)
- [ ] Check response data contains sponsors only (GroupId = 3)
- [ ] Test pagination with different page sizes
- [ ] Test filtering (isActive, status)
- [ ] Deploy to production

---

## Changelog

### Version 1.0 (2025-11-08)

**Added:**
- Initial endpoint implementation
- Handler: `GetAllSponsorsQuery`
- Claim ID 107: `GetAllSponsorsQuery`
- Pagination support
- Filtering by isActive and status
- DTO projection for safe data transfer
- Complete aspect chain (SecuredOperation, PerformanceAspect, LogAspect)

---

**Maintained by:** ZiraAI Development Team
**Contact:** dev@ziraai.com
**Documentation Version:** 1.0
**Last Updated:** 2025-11-08
