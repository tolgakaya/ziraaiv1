# API Enhancement: excludeDealerTransferred Parameter

**Implementation Date:** 2025-01-26  
**Feature Branch:** `feature/sponsorship-code-distribution-experiment`  
**Priority:** HIGH  

---

## Overview

Added `excludeDealerTransferred` parameter to sponsorship code filtering endpoints to enable sponsors to view only their directly-purchased codes, excluding codes transferred to dealers.

---

## Affected Endpoint

### GET /api/v1/sponsorship/codes

**Controller:** `WebAPI/Controllers/SponsorshipController.cs:265`  
**Authorization:** `[Authorize(Roles = "Sponsor,Admin")]`

---

## New Parameter

| Parameter | Type | Default | Required | Description |
|-----------|------|---------|----------|-------------|
| `excludeDealerTransferred` | boolean | `false` | No | When `true`, excludes codes that have been transferred to dealers (DealerId IS NOT NULL) |

---

## Complete Parameter List

```csharp
GET /api/v1/sponsorship/codes
    ?onlyUnused={bool}              // Filter unused codes
    &onlyUnsent={bool}              // Filter codes not yet distributed (DistributionDate IS NULL)
    &sentDaysAgo={int}              // Filter codes sent X days ago
    &onlySentExpired={bool}         // Filter expired codes that were sent but not redeemed
    &excludeDealerTransferred={bool} // NEW: Exclude dealer-transferred codes
    &page={int}                     // Page number (default: 1)
    &pageSize={int}                 // Items per page (default: 50, max: 200)
```

---

## Implementation Details

### Database Filtering Logic

**When `excludeDealerTransferred = true`:**
```sql
WHERE SponsorId = {sponsorId} 
  AND (DealerId IS NULL OR DealerId = 0)
```

**When `excludeDealerTransferred = false` (default):**
```sql
WHERE (SponsorId = {sponsorId} OR DealerId = {sponsorId})
```

### Affected Service Methods

All 5 code filtering methods now support this parameter:

1. **GetSponsorCodesAsync** - All codes (default view)
2. **GetUnusedSponsorCodesAsync** - Codes not redeemed
3. **GetUnsentSponsorCodesAsync** - Codes not distributed (DistributionDate IS NULL) ⭐ **PRIMARY USE CASE**
4. **GetSentButUnusedSponsorCodesAsync** - Codes sent X days ago but not redeemed
5. **GetSentExpiredCodesAsync** - Expired codes that were sent but not used

---

## Usage Examples

### Example 1: Get Unsent Codes (Sponsor's Own Only)

**Request:**
```http
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 940,
        "code": "AGRI-2025-ABC123",
        "sponsorId": 159,
        "dealerId": null,
        "subscriptionTierId": 2,
        "isUsed": false,
        "distributionDate": null,
        "expiryDate": "2025-02-25T10:30:00",
        "createdDate": "2025-01-26T10:30:00"
      }
    ],
    "totalCount": 30,
    "page": 1,
    "pageSize": 50,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "message": "30 unsent codes available for distribution"
}
```

### Example 2: Get All Unsent Codes (Including Dealer Codes)

**Request:**
```http
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=false
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 940,
        "code": "AGRI-2025-ABC123",
        "sponsorId": 159,
        "dealerId": null,
        "distributionDate": null
      },
      {
        "id": 945,
        "code": "AGRI-2025-DEF456",
        "sponsorId": 159,
        "dealerId": 158,
        "distributionDate": null
      }
    ],
    "totalCount": 50,
    "page": 1,
    "pageSize": 50
  },
  "message": "50 unsent codes available for distribution"
}
```

### Example 3: Get Unused Codes (Sponsor's Own Only)

**Request:**
```http
GET /api/v1/sponsorship/codes?onlyUnused=true&excludeDealerTransferred=true&page=1&pageSize=20
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

---

## Mobile App Integration

### Code Distribution Screen

```dart
// When showing "New Codes" section for distribution
final response = await apiClient.get(
  '/api/v1/sponsorship/codes',
  queryParameters: {
    'onlyUnsent': true,
    'excludeDealerTransferred': true,  // NEW: Only show sponsor's own codes
    'page': currentPage,
    'pageSize': 50,
  },
);
```

### Dealer Codes Section (Separate)

```dart
// When showing "Dealer Codes" section
final response = await apiClient.get(
  '/api/v1/sponsorship/dealer/my-codes',
  queryParameters: {
    'onlyUnsent': true,
    'page': currentPage,
    'pageSize': 50,
  },
);
```

---

## Backward Compatibility

✅ **Fully backward compatible**

- Default value: `excludeDealerTransferred = false`
- Existing API consumers continue to work without changes
- Existing behavior preserved when parameter is not provided
- Mobile app will explicitly set `excludeDealerTransferred=true` for sponsor code distribution

---

## Test Scenarios

### Scenario 1: Sponsor with Mixed Codes

**Setup:**
- Sponsor (userId: 159) has 30 purchased codes (DealerId = NULL)
- Sponsor has transferred 20 codes to dealer (userId: 158)

**Test 1A: Without filter (backward compatibility)**
```bash
GET /api/v1/sponsorship/codes?onlyUnsent=true
Expected: totalCount = 50 (all codes)
```

**Test 1B: With filter (new behavior)**
```bash
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true
Expected: totalCount = 30 (only sponsor's own codes)
```

### Scenario 2: Dealer User Perspective

**Setup:**
- Dealer (userId: 158) received 20 codes from sponsor (userId: 159)

**Test 2A: Dealer uses main endpoint**
```bash
GET /api/v1/sponsorship/codes?onlyUnsent=true
Expected: totalCount = 20 (dealer sees transferred codes via DealerId check)
```

**Test 2B: Dealer uses dedicated endpoint**
```bash
GET /api/v1/sponsorship/dealer/my-codes?onlyUnsent=true
Expected: totalCount = 20 (same result)
```

### Scenario 3: Pagination Accuracy

**Setup:**
- Sponsor has 100 own codes + 50 dealer codes (total 150)

**Test 3: Verify pagination with filter**
```bash
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&page=1&pageSize=50
Expected: 
  - items.length = 50
  - totalCount = 100
  - totalPages = 2
  - All items have DealerId = null
```

---

## Error Cases

### Invalid Page/PageSize

**Request:**
```http
GET /api/v1/sponsorship/codes?page=0&pageSize=300
```

**Response:**
```json
{
  "success": false,
  "message": "Page must be greater than 0"
}
```

```json
{
  "success": false,
  "message": "Page size must be between 1 and 200"
}
```

### Unauthorized

**Request:**
```http
GET /api/v1/sponsorship/codes?onlyUnsent=true
Authorization: Bearer {invalid_or_expired_token}
```

**Response:**
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

---

## Code Changes Summary

### Files Modified

1. **WebAPI/Controllers/SponsorshipController.cs**
   - Added `excludeDealerTransferred` parameter to `GetSponsorshipCodes` method

2. **Business/Handlers/Sponsorship/Queries/GetSponsorshipCodesQuery.cs**
   - Added `ExcludeDealerTransferred` property to query DTO
   - Updated handler to pass parameter to service methods

3. **Business/Services/Sponsorship/ISponsorshipService.cs**
   - Updated 5 method signatures to include `excludeDealerTransferred` parameter

4. **Business/Services/Sponsorship/SponsorshipService.cs**
   - Implemented conditional filtering logic in 5 methods:
     - `GetSponsorCodesAsync`
     - `GetUnusedSponsorCodesAsync`
     - `GetUnsentSponsorCodesAsync` ⭐ **Primary use case**
     - `GetSentButUnusedSponsorCodesAsync`
     - `GetSentExpiredCodesAsync`

### Lines of Code Changed

- **Total files:** 4
- **Total methods updated:** 10 (1 controller + 1 query handler + 3 interface + 5 implementation)
- **Net new lines:** ~60 (filtering logic + parameter declarations)

---

## Deployment Notes

### Build Status
✅ **Build successful** - No compilation errors

### Migration Requirements
None - This is a code-only change with no database schema modifications

### Configuration Changes
None required

### Testing Checklist
- [x] Build verification passed
- [ ] Postman test - sponsor with own codes only
- [ ] Postman test - sponsor with mixed codes (backward compatibility)
- [ ] Postman test - dealer user perspective
- [ ] Postman test - pagination accuracy
- [ ] Staging deployment test
- [ ] Mobile app integration test

---

## Related Documentation

- [Dealer Distribution Patterns](dealer_distribution_patterns_complete.md)
- [Dealer Distribution Development Rules](dealer_distribution_development_rules_complete.md)
- [Backend Enhancement Request](backend_dealer_code_filter_request.md)

---

## Support & Questions

For questions about this implementation, contact backend team or refer to:
- Dealer system memory: `dealer_distribution_patterns_complete`
- Development rules: `dealer_distribution_development_rules_complete`
