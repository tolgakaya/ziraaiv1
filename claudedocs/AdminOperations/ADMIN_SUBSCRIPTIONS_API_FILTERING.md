# Admin Subscriptions API - Filtering Guide

**Endpoint:** `GET /api/admin/subscriptions`  
**Auth Required:** Yes (Admin role)  
**Version:** 1.0

---

## Overview

This endpoint retrieves all user subscriptions with pagination and filtering capabilities. Admins can filter subscriptions by status, active state, and sponsorship type.

---

## Request Parameters

### Pagination Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | integer | No | 1 | Page number (starts from 1) |
| `pageSize` | integer | No | 50 | Number of records per page (max: 100) |

### Filter Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `status` | string | No | null | Filter by subscription status |
| `isActive` | boolean | No | null | Filter by active/inactive state |
| `isSponsoredSubscription` | boolean | No | null | Filter by sponsorship type |

---

## Filter Details

### 1. Status Filter (`status`)

Filters subscriptions by their current status.

**Parameter:** `status` (string)  
**Case Sensitive:** Yes  
**Valid Values:**
- `"active"` - Currently active subscriptions
- `"expired"` - Expired subscriptions
- `"cancelled"` - Manually cancelled subscriptions
- `"pending"` - Pending activation subscriptions

**Examples:**
```
GET /api/admin/subscriptions?status=active
GET /api/admin/subscriptions?status=expired
GET /api/admin/subscriptions?status=cancelled&page=1&pageSize=20
```

**Sample Response (status=active):**
```json
{
  "data": [
    {
      "subscriptionId": 123,
      "userId": 456,
      "subscriptionTierId": 3,
      "status": "active",
      "isActive": true,
      "startDate": "2025-01-01T00:00:00",
      "endDate": "2025-02-01T00:00:00",
      "isSponsoredSubscription": false
    }
  ],
  "success": true,
  "message": "Subscriptions retrieved successfully"
}
```

---

### 2. Active State Filter (`isActive`)

Filters subscriptions by their active/inactive state. This is independent of the status field.

**Parameter:** `isActive` (boolean)  
**Valid Values:**
- `true` - Only active subscriptions
- `false` - Only inactive subscriptions
- Not provided (null) - All subscriptions

**Behavior:**
- `isActive=true`: Returns subscriptions that are currently active and usable
- `isActive=false`: Returns subscriptions that have been deactivated (manually or automatically)

**Examples:**
```
GET /api/admin/subscriptions?isActive=true
GET /api/admin/subscriptions?isActive=false
GET /api/admin/subscriptions?isActive=true&page=2&pageSize=30
```

**Sample Response (isActive=false):**
```json
{
  "data": [
    {
      "subscriptionId": 789,
      "userId": 101,
      "subscriptionTierId": 2,
      "status": "active",
      "isActive": false,
      "startDate": "2025-01-01T00:00:00",
      "endDate": "2025-02-01T00:00:00",
      "isSponsoredSubscription": false,
      "deactivationReason": "Payment failure"
    }
  ],
  "success": true,
  "message": "Subscriptions retrieved successfully"
}
```

**Note:** A subscription can have `status="active"` but `isActive=false` if it was manually deactivated by an admin.

---

### 3. Sponsored Subscription Filter (`isSponsoredSubscription`)

Filters subscriptions by whether they were provided by a sponsor or purchased directly by the user.

**Parameter:** `isSponsoredSubscription` (boolean)  
**Valid Values:**
- `true` - Only sponsored subscriptions (provided by sponsors)
- `false` - Only self-purchased subscriptions
- Not provided (null) - All subscriptions

**Behavior:**
- `isSponsoredSubscription=true`: Returns only subscriptions that were activated through sponsor codes
- `isSponsoredSubscription=false`: Returns only subscriptions purchased/assigned directly by users

**Examples:**
```
GET /api/admin/subscriptions?isSponsoredSubscription=true
GET /api/admin/subscriptions?isSponsoredSubscription=false
GET /api/admin/subscriptions?isSponsoredSubscription=true&status=active
```

**Sample Response (isSponsoredSubscription=true):**
```json
{
  "data": [
    {
      "subscriptionId": 456,
      "userId": 789,
      "subscriptionTierId": 4,
      "status": "active",
      "isActive": true,
      "startDate": "2025-01-15T00:00:00",
      "endDate": "2025-02-15T00:00:00",
      "isSponsoredSubscription": true,
      "sponsorId": 159,
      "sponsorshipCode": "AGRI-2025-ABC123"
    }
  ],
  "success": true,
  "message": "Subscriptions retrieved successfully"
}
```

---

## Combining Filters

All three filters can be combined to create precise queries.

### Example 1: Active Sponsored Subscriptions
```
GET /api/admin/subscriptions?isActive=true&isSponsoredSubscription=true
```

**Use Case:** View all currently active subscriptions that were provided by sponsors

### Example 2: Expired Non-Sponsored Subscriptions
```
GET /api/admin/subscriptions?status=expired&isSponsoredSubscription=false
```

**Use Case:** Find users whose self-purchased subscriptions have expired (for renewal campaigns)

### Example 3: Inactive Active Subscriptions (Edge Case)
```
GET /api/admin/subscriptions?status=active&isActive=false
```

**Use Case:** Find subscriptions that should be active by date but were manually deactivated

### Example 4: Cancelled Sponsored Subscriptions with Pagination
```
GET /api/admin/subscriptions?status=cancelled&isSponsoredSubscription=true&page=2&pageSize=25
```

**Use Case:** Audit cancelled sponsored subscriptions (page 2, 25 per page)

---

## Response Structure

### Success Response

```json
{
  "data": [
    {
      "subscriptionId": 123,
      "userId": 456,
      "subscriptionTierId": 3,
      "subscriptionTier": {
        "tierId": 3,
        "tierName": "Medium",
        "monthlyRequestLimit": 50,
        "dailyRequestLimit": 10
      },
      "status": "active",
      "isActive": true,
      "startDate": "2025-01-01T00:00:00Z",
      "endDate": "2025-02-01T00:00:00Z",
      "createdDate": "2024-12-28T10:30:00Z",
      "isSponsoredSubscription": false,
      "sponsorId": null,
      "autoRenew": true,
      "notes": "Assigned by admin"
    }
  ],
  "success": true,
  "message": "Subscriptions retrieved successfully"
}
```

### Error Response (Unauthorized)

```json
{
  "success": false,
  "message": "Unauthorized access. Admin role required."
}
```

### Empty Result Response

```json
{
  "data": [],
  "success": true,
  "message": "Subscriptions retrieved successfully"
}
```

---

## Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `subscriptionId` | integer | Unique subscription identifier |
| `userId` | integer | User who owns this subscription |
| `subscriptionTierId` | integer | Tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL) |
| `subscriptionTier` | object | Full tier details (nested object) |
| `status` | string | Subscription status (active/expired/cancelled/pending) |
| `isActive` | boolean | Whether subscription is currently active |
| `startDate` | datetime | Subscription start date (ISO 8601) |
| `endDate` | datetime | Subscription end date (ISO 8601) |
| `createdDate` | datetime | When subscription was created |
| `isSponsoredSubscription` | boolean | Whether provided by sponsor |
| `sponsorId` | integer (nullable) | Sponsor user ID (if sponsored) |
| `sponsorshipCode` | string (nullable) | Code used to activate (if sponsored) |
| `autoRenew` | boolean | Whether subscription auto-renews |
| `notes` | string (nullable) | Admin notes about subscription |

---

## Status vs IsActive Differences

Understanding the difference between `status` and `isActive` is crucial:

| Scenario | status | isActive | Meaning |
|----------|--------|----------|---------|
| Normal active subscription | `"active"` | `true` | Subscription is working normally |
| Expired subscription | `"expired"` | `false` | Subscription period ended |
| Admin deactivated | `"active"` | `false` | Valid period but manually disabled |
| Cancelled before expiry | `"cancelled"` | `false` | User/admin cancelled early |
| Pending activation | `"pending"` | `false` | Not yet started |

---

## Common Frontend Use Cases

### 1. Dashboard: Show Active Subscriptions Count
```javascript
// Get all active subscriptions
const response = await fetch('/api/admin/subscriptions?isActive=true&pageSize=1000');
const data = await response.json();
const activeCount = data.data.length;
```

### 2. Filter Dropdown: Sponsored vs Self-Purchased
```javascript
// User selects "Sponsored Only" from dropdown
const filterValue = 'sponsored'; // or 'self-purchased' or 'all'

let url = '/api/admin/subscriptions?page=1&pageSize=50';
if (filterValue === 'sponsored') {
  url += '&isSponsoredSubscription=true';
} else if (filterValue === 'self-purchased') {
  url += '&isSponsoredSubscription=false';
}

const response = await fetch(url);
```

### 3. Status Tabs: Active / Expired / Cancelled
```javascript
// User clicks on "Expired" tab
const selectedTab = 'expired'; // 'active', 'expired', 'cancelled'

const response = await fetch(
  `/api/admin/subscriptions?status=${selectedTab}&page=1&pageSize=50`
);
```

### 4. Multi-Filter Search Form
```javascript
const filters = {
  status: 'active',           // From status dropdown
  isActive: true,             // From active checkbox
  isSponsoredSubscription: null, // From sponsorship filter (null = all)
  page: 1,
  pageSize: 50
};

// Build query string
const params = new URLSearchParams();
params.append('page', filters.page);
params.append('pageSize', filters.pageSize);

if (filters.status) params.append('status', filters.status);
if (filters.isActive !== null) params.append('isActive', filters.isActive);
if (filters.isSponsoredSubscription !== null) {
  params.append('isSponsoredSubscription', filters.isSponsoredSubscription);
}

const response = await fetch(`/api/admin/subscriptions?${params}`);
```

### 5. Pagination with Filters
```javascript
const [currentPage, setCurrentPage] = useState(1);
const [filters, setFilters] = useState({
  status: 'active',
  isSponsoredSubscription: true
});

const fetchSubscriptions = async (page) => {
  const params = new URLSearchParams({
    page,
    pageSize: 50,
    status: filters.status,
    isSponsoredSubscription: filters.isSponsoredSubscription
  });
  
  const response = await fetch(`/api/admin/subscriptions?${params}`);
  return await response.json();
};

// User clicks "Next Page"
const handleNextPage = () => {
  setCurrentPage(prev => prev + 1);
  fetchSubscriptions(currentPage + 1);
};
```

---

## Query Performance Notes

- **Indexed Fields**: `Status`, `IsActive`, `IsSponsoredSubscription`, `CreatedDate`
- **Optimal Page Size**: 50 (default), max 100
- **Response Time**: 
  - Without filters: ~100-200ms
  - With 1-2 filters: ~150-300ms
  - With all 3 filters: ~200-400ms
- **Large Datasets**: Use pagination for datasets >1000 records

---

## Important Notes for Frontend

1. **Boolean Parameters**: When passing boolean filters, use lowercase `true` or `false`:
   - ✅ Correct: `?isActive=true`
   - ❌ Wrong: `?isActive=True` or `?isActive=1`

2. **Null vs Omitted**: To get all records (no filter), either:
   - Omit the parameter entirely: `?status=active` (no isActive param)
   - Do NOT send `null` or `undefined` as string

3. **Status Case Sensitivity**: Status values are case-sensitive:
   - ✅ Correct: `status=active`
   - ❌ Wrong: `status=Active` or `status=ACTIVE`

4. **Empty Results**: An empty array in `data` with `success=true` is valid (no matches found)

5. **Default Sorting**: Results are always sorted by `CreatedDate` descending (newest first)

6. **Tier Filtering**: Currently NOT supported. If needed, filter by `tierId` on frontend after fetching data

---

## Testing Examples

### cURL Examples

```bash
# Get all active subscriptions
curl -H "Authorization: Bearer YOUR_TOKEN" \
  "https://api.ziraai.com/api/admin/subscriptions?isActive=true"

# Get expired sponsored subscriptions
curl -H "Authorization: Bearer YOUR_TOKEN" \
  "https://api.ziraai.com/api/admin/subscriptions?status=expired&isSponsoredSubscription=true"

# Get page 2 of cancelled subscriptions (25 per page)
curl -H "Authorization: Bearer YOUR_TOKEN" \
  "https://api.ziraai.com/api/admin/subscriptions?status=cancelled&page=2&pageSize=25"
```

### Postman Collection Variable

```javascript
// In Postman Tests tab, extract total count
pm.test("Get subscriptions count", function () {
    var jsonData = pm.response.json();
    pm.environment.set("totalSubscriptions", jsonData.data.length);
});
```

---

## Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-07 | Initial documentation |

---

## Contact

For questions or feature requests regarding this API endpoint:
- **Backend Team**: backend@ziraai.com
- **API Documentation**: https://api.ziraai.com/swagger

---

**Note**: Tier-based filtering (`tierId`) is NOT currently supported. If this feature is required, please submit a feature request to the backend team.
