# Backend API Enhancement Request: Reserved Codes Filtering

**Date**: 2025-11-05  
**Priority**: HIGH  
**Affected Endpoint**: `GET /api/v1/sponsorship/codes`

---

## Problem Statement

### Current Behavior ❌
`/api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true` endpoint'i şu anda **dealer invitation için rezerve edilmiş kodları da** döndürüyor.

**Örnek Veri (codes1.json'dan):**
```json
{
  "id": 926,
  "code": "AGRI-2025-8538C9AC",
  "reservedForInvitationId": 147,
  "reservedAt": "2025-11-04T09:15:23.391091",
  "isUsed": false,
  "isActive": true
}
```

### Impact
- **Çiftçilere kod dağıtımı** yapılırken **dealer'a rezerve kodlar** da gösteriliyor
- Bu kodlar **dealer invitation** için ayrılmış olup, çiftçilere **dağıtılmamalı**
- Frontend'de pagination ve total count hesaplamaları **bozuluyor**

---

## Current Query Parameters

```
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&page=1&pageSize=50
```

| Parameter | Current Behavior |
|-----------|-----------------|
| `onlyUnsent` | ✅ Filters `isUsed = false` and `linkDelivered = false` |
| `excludeDealerTransferred` | ✅ Excludes codes transferred TO dealers |
| ❌ Missing | **No filter for codes RESERVED FOR dealers** |

---

## Requested Enhancement

### New Query Parameter: `excludeReserved`

**Parameter Name**: `excludeReserved`  
**Type**: `boolean`  
**Default**: `false` (backward compatible)  
**Description**: When `true`, excludes codes that are reserved for dealer invitations

### Backend Filtering Logic

```csharp
// Pseudocode
var query = context.SponsorshipCodes.Where(c => c.SponsorId == sponsorId);

if (onlyUnsent) 
{
    query = query.Where(c => !c.IsUsed && !c.LinkDelivered);
}

if (excludeDealerTransferred) 
{
    query = query.Where(c => c.DealerTransferId == null);
}

// 🆕 NEW FILTER
if (excludeReserved) 
{
    query = query.Where(c => c.ReservedForInvitationId == null && c.ReservedAt == null);
    // OR simply: query = query.Where(c => c.ReservedForInvitationId == null);
}

return await query.ToListAsync();
```

---

## Updated API Request

### Frontend Will Call:
```
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&excludeReserved=true&page=1&pageSize=50
```

### Expected Behavior:
| Condition | Included? |
|-----------|-----------|
| `isUsed = false` AND `linkDelivered = false` | ✅ Yes |
| `dealerTransferId != null` | ❌ No (transferred to dealer) |
| `reservedForInvitationId != null` | ❌ No (reserved for dealer invitation) |
| `reservedAt != null` | ❌ No (reserved for dealer invitation) |

---

## Data Analysis (from codes1.json)

**API Response**: 50 codes returned  
**Reserved Codes**: 39 codes with `reservedForInvitationId` set  
**Available for Distribution**: Only 11 codes

### Reserved Code Examples:
```json
// invitationId: 147
{ "id": 926, "reservedForInvitationId": 147, "reservedAt": "2025-11-04T09:15:23.391091" }

// invitationId: 146  
{ "id": 925, "reservedForInvitationId": 146, "reservedAt": "2025-11-04T09:15:23.367056" }

// invitationId: 145
{ "id": 924, "reservedForInvitationId": 145, "reservedAt": "2025-11-04T09:15:23.338712" }

// ... 36 more reserved codes
```

### With New Filter:
```
✅ Backend returns: 11 codes (only unreserved)
✅ totalCount: 11 (correct pagination)
✅ Frontend displays: 11 codes (accurate UI)
```

---

## Database Schema Reference

### SponsorshipCodes Table Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Primary key |
| `code` | string | Redemption code (e.g., "AGRI-2025-8538C9AC") |
| `isUsed` | bool | Whether farmer has redeemed this code |
| `linkDelivered` | bool | Whether code was sent via SMS/WhatsApp |
| `dealerTransferId` | int? | Set when code is transferred TO a dealer |
| `reservedForInvitationId` | int? | 🆕 Set when code is reserved FOR dealer invitation |
| `reservedAt` | DateTime? | 🆕 Timestamp of reservation |

---

## Implementation Checklist

### Backend Changes Required:

- [ ] Add `excludeReserved` query parameter to `GET /api/v1/sponsorship/codes`
- [ ] Implement filtering logic: `WHERE reservedForInvitationId IS NULL`
- [ ] Update Swagger/OpenAPI documentation
- [ ] Ensure `totalCount` reflects filtered count (critical for pagination)
- [ ] Add unit tests for reservation filtering
- [ ] Test pagination with mixed reserved/unreserved codes

### Optional Enhancements:

- [ ] Add `includeReserved` parameter for admin views (show reserved codes separately)
- [ ] Add reservation metadata to response (e.g., `reservationInfo` object)
- [ ] Create separate endpoint for reserved codes management

---

## API Response Format (No Change Required)

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 981,
        "code": "AGRI-2025-52834B45",
        "isUsed": true,
        "reservedForInvitationId": null,  // ✅ Not reserved
        "reservedAt": null
      }
    ],
    "totalCount": 11,  // ✅ Accurate count (only unreserved)
    "page": 1,
    "pageSize": 50,
    "totalPages": 1,
    "hasNextPage": false
  },
  "message": "11 unsent codes available for distribution"
}
```

---

## Frontend Changes (After Backend Fix)

### Current Mobile API Call:
```dart
// lib/features/sponsorship/data/services/sponsor_service.dart
final response = await _dio.get(
  '${ApiConfig.apiBaseUrl}${ApiConfig.sponsorshipCodes}',
  queryParameters: {
    'onlyUnsent': true,
    'excludeDealerTransferred': true,
    'page': page,
    'pageSize': pageSize,
  },
);
```

### Updated Mobile API Call:
```dart
final response = await _dio.get(
  '${ApiConfig.apiBaseUrl}${ApiConfig.sponsorshipCodes}',
  queryParameters: {
    'onlyUnsent': true,
    'excludeDealerTransferred': true,
    'excludeReserved': true,  // 🆕 NEW PARAMETER
    'page': page,
    'pageSize': pageSize,
  },
);
```

---

## Testing Scenarios

### Test Case 1: All Codes Reserved
**Setup**: Package with 50 codes, all reserved for invitations  
**Request**: `?onlyUnsent=true&excludeReserved=true`  
**Expected**: 
```json
{
  "data": {
    "items": [],
    "totalCount": 0,
    "message": "No unreserved codes available"
  }
}
```

### Test Case 2: Mixed Reserved/Unreserved
**Setup**: Package with 50 codes, 39 reserved, 11 unreserved  
**Request**: `?onlyUnsent=true&excludeReserved=true`  
**Expected**:
```json
{
  "data": {
    "items": [ /* 11 unreserved codes */ ],
    "totalCount": 11,
    "totalPages": 1
  }
}
```

### Test Case 3: Pagination with Reserved Codes
**Setup**: 100 codes total, 60 reserved, 40 unreserved  
**Request**: `?excludeReserved=true&page=1&pageSize=25`  
**Expected**:
```json
{
  "data": {
    "items": [ /* 25 unreserved codes */ ],
    "totalCount": 40,  // Only unreserved count
    "totalPages": 2,   // 40 / 25 = 2 pages
    "hasNextPage": true
  }
}
```

### Test Case 4: Backward Compatibility
**Request**: `?onlyUnsent=true` (without excludeReserved)  
**Expected**: All codes including reserved (current behavior maintained)

---

## Business Logic Clarification Needed

### Questions for Backend Team:

1. **Reservation Expiry**: Do reserved codes have an expiry time? Should expired reservations be auto-released?
   
2. **Reservation Priority**: If a code is both `dealerTransferred` and `reserved`, which takes precedence?

3. **Admin Access**: Should admin users see reserved codes with special filtering?

4. **Reservation Visibility**: Should we add a separate endpoint to view reservation details?
   ```
   GET /api/v1/dealer-invitations/{invitationId}/reserved-codes
   ```

---

## Rollout Plan

### Phase 1: Backend Implementation (Week 1)
- [ ] Add `excludeReserved` parameter
- [ ] Update filtering logic
- [ ] Update API documentation
- [ ] Deploy to staging environment

### Phase 2: Frontend Integration (Week 1)
- [ ] Update mobile API calls
- [ ] Remove client-side filtering logic
- [ ] Test with staging backend
- [ ] Verify pagination accuracy

### Phase 3: Production Deployment (Week 2)
- [ ] Deploy backend changes
- [ ] Deploy mobile app update
- [ ] Monitor API performance
- [ ] Verify reservation system integrity

---

## Success Criteria

✅ **Functional Requirements:**
- Reserved codes NOT shown in farmer distribution screen
- Pagination works correctly with accurate counts
- Dealer invitation system unaffected
- Backward compatibility maintained

✅ **Performance Requirements:**
- No significant performance degradation
- Database query optimized with proper indexes
- API response time < 500ms

✅ **Data Integrity:**
- Reserved codes remain untouched
- No accidental reservation releases
- Audit trail maintained for reservations

---

## Contact & Questions

**Mobile Team**: Ready to implement frontend changes upon backend completion  
**Testing**: Test data available in `claudedocs/codes1.json`  
**Priority**: HIGH - Blocks farmer code distribution feature

---

**Prepared by**: Claude (Mobile Development Assistant)  
**Date**: 2025-11-05  
**Status**: Awaiting Backend Implementation
