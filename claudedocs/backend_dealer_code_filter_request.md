# Backend API Enhancement Request: Dealer Code Filtering

## 📋 Problem Summary

**Current Issue:** `/api/v1/sponsorship/codes?onlyUnsent=true` endpoint returns BOTH sponsor-purchased codes AND dealer-transferred codes in the same response. This causes sponsor users to see dealer codes in their "New Codes" distribution section.

**Business Impact:**
- Sponsors should ONLY distribute codes they purchased directly
- Dealer-transferred codes must be kept separate (shown in "Dealer Codes" section)
- Currently filtering on frontend, but this breaks pagination and total count accuracy

---

## 🎯 Requested Change

### Endpoint
```
GET /api/v1/sponsorship/codes
```

### Current Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `onlyUnsent` | boolean | Filter codes where `DistributionDate IS NULL` |
| `onlySentExpired` | boolean | Filter expired codes that were sent but not redeemed |
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 50) |

### New Parameter (REQUESTED)
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `excludeDealerTransferred` | boolean | `false` | Exclude codes that have been transferred to dealers via invitation system |

---

## 💡 Implementation Details

### Database Filter Logic

When `excludeDealerTransferred=true`, add this filter to the WHERE clause:

```sql
-- Option 1: If you track dealer transfer with a foreign key
WHERE ...
  AND (DealerId IS NULL OR DealerId = 0)

-- Option 2: If you track dealer transfer with a date field
WHERE ...
  AND DealerTransferDate IS NULL

-- Option 3: If you use a junction table for dealer codes
WHERE ...
  AND SponsorshipCodeId NOT IN (
    SELECT SponsorshipCodeId
    FROM DealerSponsorshipCodes
  )
```

### Response Format
No changes needed - existing paginated format is correct:

```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 100,
    "page": 1,
    "pageSize": 50,
    "totalPages": 2,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**Important:** `totalCount` must reflect the FILTERED count, not the total before filtering.

---

## 📱 Frontend Usage Examples

### Before (Current - Broken)
```
GET /api/v1/sponsorship/codes?onlyUnsent=true&page=1&pageSize=50
Response: Returns 50 codes (30 sponsor + 20 dealer mixed)
Frontend filters: Shows only 30 codes, but totalCount=100 is wrong
```

### After (Requested - Fixed)
```
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true&page=1&pageSize=50
Response: Returns 50 sponsor codes only, totalCount=80 (accurate)
```

### Dealer Codes Endpoint (Already Exists)
```
GET /api/v1/sponsorship/dealer/my-codes?onlyUnsent=true&page=1&pageSize=50
Response: Returns dealer-transferred codes only
```

---

## ✅ Backward Compatibility

- Default value `excludeDealerTransferred=false` maintains current behavior
- Existing API consumers won't break
- Mobile app will explicitly set `excludeDealerTransferred=true` for sponsor code distribution

---

## 🔍 Testing Checklist

### Test Case 1: Sponsor with Both Code Types
**Setup:**
- Sponsor has 30 purchased codes (not sent)
- Sponsor has transferred 20 codes to a dealer

**Request:**
```
GET /api/v1/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true
```

**Expected:**
- `totalCount`: 30 (only purchased codes)
- `items.length`: 30 (or pageSize, whichever is smaller)
- NO dealer-transferred codes in response

### Test Case 2: Dealer User Perspective
**Request:**
```
GET /api/v1/sponsorship/dealer/my-codes?onlyUnsent=true
```

**Expected:**
- Returns 20 dealer-transferred codes
- Uses separate endpoint, unaffected by this change

### Test Case 3: Default Behavior (No Parameter)
**Request:**
```
GET /api/v1/sponsorship/codes?onlyUnsent=true
```

**Expected:**
- Returns ALL unsent codes (sponsor + dealer mixed)
- Maintains backward compatibility

---

## 📌 Priority: HIGH

**Reason:** Frontend filtering breaks pagination and causes incorrect total counts. This is a data integrity issue that must be resolved at the source (backend).

**ETA Request:** 2-3 days (simple WHERE clause addition)

---

## 📞 Contact

Mobile team contact: [Your Name/Team]
Related Mobile PR: `feature/dealer-central-tier-enhancement`

---

## 🔗 Related Documentation

- Dealer invitation system: `/api/v1/sponsorship/dealer/invitation-details`
- Dealer code transfer: `/api/v1/sponsorship/dealer/accept-invitation`
- Code distribution flow: See mobile app `code_distribution_screen.dart`
