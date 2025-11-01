# Postman Test Script: excludeDealerTransferred Parameter

**Test Date:** 2025-01-26  
**Feature:** Dealer Code Filtering  
**Endpoint:** `/api/v1/sponsorship/codes`

---

## Prerequisites

### Test Data Setup

You need a sponsor user with:
- **Sponsor ID:** 159 (User 1114)
- **Dealer ID:** 158 (User 1113)
- **Purchased codes:** Some codes with `DealerId = NULL`
- **Transferred codes:** Some codes with `DealerId = 158`

### Authentication Token

```bash
# Login as Sponsor (User 1114)
POST https://ziraai-api-sit.up.railway.app/api/v1/auth/login
Content-Type: application/json

{
  "mobilePhone": "+905551234567",  # Replace with actual phone
  "password": "your_password"
}

# Extract JWT token from response
# Use in subsequent requests as: Authorization: Bearer {token}
```

---

## Test Cases

### Test 1: Baseline - Get All Unsent Codes (Backward Compatibility)

**Purpose:** Verify existing behavior still works (should return both sponsor and dealer codes)

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true&page=1&pageSize=50
Authorization: Bearer {YOUR_TOKEN}
x-dev-arch-version: 1.0
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 940,
        "sponsorId": 159,
        "dealerId": null,        // ← Sponsor's own code
        "distributionDate": null
      },
      {
        "id": 945,
        "sponsorId": 159,
        "dealerId": 158,         // ← Code transferred to dealer
        "distributionDate": null
      }
    ],
    "totalCount": 50,            // ← Both types included
    "page": 1,
    "pageSize": 50
  }
}
```

**Validation:**
- ✅ Response includes codes with `dealerId = null` AND `dealerId = 158`
- ✅ `totalCount` reflects all codes
- ✅ Backward compatibility maintained

---

### Test 2: New Feature - Exclude Dealer-Transferred Codes

**Purpose:** Verify new parameter excludes dealer codes

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&page=1&pageSize=50
Authorization: Bearer {YOUR_TOKEN}
x-dev-arch-version: 1.0
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 940,
        "sponsorId": 159,
        "dealerId": null,        // ← Only sponsor's own codes
        "distributionDate": null
      },
      {
        "id": 941,
        "sponsorId": 159,
        "dealerId": null,        // ← Only sponsor's own codes
        "distributionDate": null
      }
    ],
    "totalCount": 30,            // ← Only own codes counted
    "page": 1,
    "pageSize": 50
  }
}
```

**Validation:**
- ✅ Response includes ONLY codes with `dealerId = null`
- ✅ NO codes with `dealerId = 158` present
- ✅ `totalCount` reflects only sponsor's own codes
- ✅ All items in response have `dealerId = null`

---

### Test 3: Pagination Accuracy

**Purpose:** Verify totalCount and pagination work correctly with filter

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&page=1&pageSize=10
Authorization: Bearer {YOUR_TOKEN}
x-dev-arch-version: 1.0
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "items": [ /* 10 items */ ],
    "totalCount": 30,
    "page": 1,
    "pageSize": 10,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Validation:**
- ✅ `items.length` = 10 (pageSize)
- ✅ `totalCount` = accurate count of sponsor's own codes
- ✅ `totalPages` = ceil(totalCount / pageSize) = 3
- ✅ `hasNextPage` = true

**Follow-up Request (Page 2):**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&page=2&pageSize=10
```

**Validation:**
- ✅ Different items returned (next 10 codes)
- ✅ `hasPreviousPage` = true
- ✅ `hasNextPage` = true

---

### Test 4: Other Filter Combinations

**Purpose:** Verify parameter works with other filters

**Request 4A: Unused + Exclude Dealer**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnused=true&excludeDealerTransferred=true
Authorization: Bearer {YOUR_TOKEN}
x-dev-arch-version: 1.0
```

**Validation:**
- ✅ Returns unused codes where `dealerId = null`

**Request 4B: Sent Expired + Exclude Dealer**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlySentExpired=true&excludeDealerTransferred=true
Authorization: Bearer {YOUR_TOKEN}
x-dev-arch-version: 1.0
```

**Validation:**
- ✅ Returns expired codes where `dealerId = null`

---

### Test 5: Dealer User Perspective

**Purpose:** Verify dealer can still see their transferred codes

**Setup:** Login as Dealer (User 1113, userId=158)

**Request:**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true
Authorization: Bearer {DEALER_TOKEN}
x-dev-arch-version: 1.0
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 945,
        "sponsorId": 159,
        "dealerId": 158,         // ← Dealer sees their codes
        "distributionDate": null
      }
    ],
    "totalCount": 20
  }
}
```

**Validation:**
- ✅ Dealer sees codes where `dealerId = 158`
- ✅ Query matches: `(SponsorId = 158 OR DealerId = 158)`
- ✅ Dealer functionality unaffected

---

### Test 6: Error Cases

**Test 6A: Invalid Page**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?page=0
Authorization: Bearer {YOUR_TOKEN}
```

**Expected:**
```json
{
  "success": false,
  "message": "Page must be greater than 0"
}
```

**Test 6B: Invalid PageSize**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?pageSize=300
Authorization: Bearer {YOUR_TOKEN}
```

**Expected:**
```json
{
  "success": false,
  "message": "Page size must be between 1 and 200"
}
```

**Test 6C: Unauthorized**
```http
GET https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true
# No Authorization header
```

**Expected:**
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

---

## Automated Test Script (Postman Collection)

```javascript
// Test Script for "Exclude Dealer Codes" Request
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has success=true", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.eql(true);
});

pm.test("All codes have dealerId=null", function () {
    var jsonData = pm.response.json();
    var items = jsonData.data.items;
    
    items.forEach(function(item) {
        pm.expect(item.dealerId).to.be.null;
    });
});

pm.test("TotalCount matches expected", function () {
    var jsonData = pm.response.json();
    // Adjust this based on your test data
    pm.expect(jsonData.data.totalCount).to.be.greaterThan(0);
});

pm.test("Pagination fields present", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.data).to.have.property('page');
    pm.expect(jsonData.data).to.have.property('pageSize');
    pm.expect(jsonData.data).to.have.property('totalPages');
    pm.expect(jsonData.data).to.have.property('totalCount');
});
```

---

## Test Results Template

| Test Case | Status | Notes |
|-----------|--------|-------|
| Test 1: Baseline (backward compat) | ⬜ PASS / ⬜ FAIL | |
| Test 2: Exclude dealer codes | ⬜ PASS / ⬜ FAIL | |
| Test 3: Pagination accuracy | ⬜ PASS / ⬜ FAIL | |
| Test 4A: Unused + exclude | ⬜ PASS / ⬜ FAIL | |
| Test 4B: Expired + exclude | ⬜ PASS / ⬜ FAIL | |
| Test 5: Dealer perspective | ⬜ PASS / ⬜ FAIL | |
| Test 6A: Invalid page | ⬜ PASS / ⬜ FAIL | |
| Test 6B: Invalid pageSize | ⬜ PASS / ⬜ FAIL | |
| Test 6C: Unauthorized | ⬜ PASS / ⬜ FAIL | |

---

## Quick Test Commands (curl)

### Test 1: Baseline
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

### Test 2: Exclude Dealer Codes
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

### Test 3: Pagination
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

---

## Notes

- Replace `YOUR_TOKEN` with actual JWT token from login response
- Replace `DEALER_TOKEN` with JWT token from dealer user login
- Staging URL: `https://ziraai-api-sit.up.railway.app`
- Production URL: TBD (update before production deployment)
