# Admin User Search API Documentation

## Endpoint Overview

**URL:** `/api/admin/users/search`  
**Method:** `GET`  
**Authentication:** Required (Admin role)  
**Base URL (Staging):** `https://ziraai-api-sit.up.railway.app`  
**Base URL (Production):** `https://api.ziraai.com`

---

## Description

This endpoint allows admin users to search for users by email, full name, or mobile phone number. The search performs a partial match (contains) across all three fields, making it easy to find users with incomplete information.

## 🔒 Security Note

**Admin User Exclusion**: This endpoint automatically excludes users with Admin role (ClaimId = 1) from search results. This is a critical security feature to prevent admins from viewing or managing other admin accounts.

- Admin users will **never** appear in search results
- This protection is applied at the database query level
- Attempting to search for admin users by email, name, or phone will return no results

---

## Request Parameters

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchTerm` | string | ✅ Yes | - | Search term to find users by email, name, or phone |
| `page` | integer | ❌ No | 1 | Page number for pagination |
| `pageSize` | integer | ❌ No | 50 | Number of results per page |

### Headers

| Header | Required | Value | Description |
|--------|----------|-------|-------------|
| `Authorization` | ✅ Yes | `Bearer {token}` | Admin JWT token |
| `x-dev-arch-version` | ✅ Yes | `1.0` | API version header |
| `Content-Type` | ✅ Yes | `application/json` | Content type |

---

## Search Behavior

The search is performed across **three fields**:

1. **Email Address** - Partial match, case-insensitive
2. **Full Name** - Partial match, case-insensitive
3. **Mobile Phone** - Exact or partial match

### Search Characteristics:

- ✅ **Case Insensitive**: `AHMET`, `ahmet`, `Ahmet` all match
- ✅ **Partial Match**: `0504` finds all phones starting with 0504
- ✅ **Multi-field**: Searches all three fields simultaneously
- ❌ **Empty Search Not Allowed**: Returns error if searchTerm is empty

---

## Request Examples

### Example 1: Search by Phone Number

```http
GET /api/admin/users/search?searchTerm=05046866386&page=1&pageSize=50 HTTP/1.1
Host: https://ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
Content-Type: application/json
```

### Example 2: Search by Email

```http
GET /api/admin/users/search?searchTerm=ahmet@example.com&page=1&pageSize=20 HTTP/1.1
Host: https://ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
Content-Type: application/json
```

### Example 3: Search by Name

```http
GET /api/admin/users/search?searchTerm=ahmet&page=1&pageSize=50 HTTP/1.1
Host: https://ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
Content-Type: application/json
```

### Example 4: Partial Search

```http
GET /api/admin/users/search?searchTerm=0504&page=1&pageSize=50 HTTP/1.1
Host: https://ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
Content-Type: application/json
```
*This will return all users with phone numbers containing "0504"*

---

## Response Format

### Success Response (200 OK)

```json
{
  "data": [
    {
      "id": 165,
      "email": "user1113@example.com",
      "fullName": "Ahmet Yılmaz",
      "mobilePhones": "05046866386",
      "status": true,
      "recordDate": "2025-03-25T10:30:00Z",
      "roles": ["Farmer"]
    },
    {
      "id": 178,
      "email": "ahmet.kaya@example.com",
      "fullName": "Ahmet Kaya",
      "mobilePhones": "05321234567",
      "status": true,
      "recordDate": "2025-03-20T14:20:00Z",
      "roles": ["Farmer", "Sponsor"]
    }
  ],
  "success": true,
  "message": "Found 2 users"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `data` | array | Array of user objects matching search criteria |
| `success` | boolean | Indicates if the request was successful |
| `message` | string | Descriptive message about the result |

### User Object Fields

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `id` | integer | No | Unique user identifier |
| `email` | string | No | User's email address |
| `fullName` | string | No | User's full name |
| `mobilePhones` | string | Yes | User's mobile phone number(s) |
| `status` | boolean | No | User account status (true=active, false=inactive) |
| `recordDate` | string (ISO 8601) | No | User registration date/time |
| `roles` | array[string] | No | User roles (e.g., "Farmer", "Sponsor", "Admin") |

---

## Error Responses

### 400 Bad Request - Empty Search Term

```json
{
  "data": null,
  "success": false,
  "message": "Search term cannot be empty"
}
```

**Cause:** `searchTerm` parameter is missing or empty

---

### 401 Unauthorized

```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "00-abc123..."
}
```

**Causes:**
- Missing `Authorization` header
- Invalid or expired JWT token
- Token doesn't have Admin role

---

### 403 Forbidden

```json
{
  "success": false,
  "message": "You are not authorized to perform this operation"
}
```

**Cause:** User doesn't have Admin role in their JWT claims

---

### 404 Not Found - No Results

```json
{
  "data": [],
  "success": true,
  "message": "Found 0 users"
}
```

**Note:** This is actually a success response, just with no matching results.

---

## Integration Examples

### JavaScript (Fetch API)

```javascript
async function searchUsers(searchTerm, page = 1, pageSize = 50) {
  const params = new URLSearchParams({
    searchTerm: searchTerm,
    page: page.toString(),
    pageSize: pageSize.toString()
  });

  const response = await fetch(
    `https://ziraai-api-sit.up.railway.app/api/admin/users/search?${params}`,
    {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${adminToken}`,
        'x-dev-arch-version': '1.0',
        'Content-Type': 'application/json'
      }
    }
  );

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const result = await response.json();
  return result;
}

// Usage
try {
  const result = await searchUsers('05046866386');
  console.log(`Found ${result.data.length} users:`, result.data);
} catch (error) {
  console.error('Search failed:', error);
}
```

---

### TypeScript (Axios)

```typescript
import axios, { AxiosResponse } from 'axios';

interface UserDto {
  id: number;
  email: string;
  fullName: string;
  mobilePhones: string | null;
  status: boolean;
  recordDate: string;
  roles: string[];
}

interface SearchUsersResponse {
  data: UserDto[];
  success: boolean;
  message: string;
}

const searchUsers = async (
  searchTerm: string,
  page: number = 1,
  pageSize: number = 50
): Promise<SearchUsersResponse> => {
  try {
    const response: AxiosResponse<SearchUsersResponse> = await axios.get(
      '/api/admin/users/search',
      {
        params: {
          searchTerm,
          page,
          pageSize
        },
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'x-dev-arch-version': '1.0'
        }
      }
    );
    
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      console.error('API Error:', error.response?.data);
      throw new Error(error.response?.data?.message || 'Search failed');
    }
    throw error;
  }
};

// Usage
const result = await searchUsers('05046866386');
console.log(`Found ${result.data.length} users`);
result.data.forEach(user => {
  console.log(`- ${user.fullName} (${user.email})`);
});
```

---

### cURL

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/users/search?searchTerm=05046866386&page=1&pageSize=50" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json"
```

---

## Pagination

The API supports pagination to handle large result sets efficiently.

### Pagination Parameters

- `page`: Page number (starts from 1)
- `pageSize`: Number of results per page (default: 50)

### Example: Getting Multiple Pages

```javascript
// Get first page (results 1-50)
const page1 = await searchUsers('ahmet', 1, 50);

// Get second page (results 51-100)
const page2 = await searchUsers('ahmet', 2, 50);

// Get third page (results 101-150)
const page3 = await searchUsers('ahmet', 3, 50);
```

### Recommended Page Sizes

| Use Case | Recommended pageSize |
|----------|---------------------|
| Quick search/autocomplete | 10-20 |
| Standard admin panel listing | 20-50 |
| Bulk operations | 50-100 |

---

## Common Use Cases

### Use Case 1: Phone Number Lookup

**Scenario:** Admin receives a support call and needs to find user by phone number.

```javascript
const phone = '05046866386';
const result = await searchUsers(phone, 1, 10);

if (result.success && result.data.length > 0) {
  const user = result.data[0];
  console.log(`Found user: ${user.fullName} (ID: ${user.id})`);
} else {
  console.log('No user found with this phone number');
}
```

---

### Use Case 2: Email Verification

**Scenario:** Check if email exists in the system.

```javascript
const email = 'user@example.com';
const result = await searchUsers(email, 1, 1);

if (result.data.length > 0) {
  console.log('Email exists in system');
} else {
  console.log('Email not found');
}
```

---

### Use Case 3: Name-based Search with Pagination

**Scenario:** Search for all users named "Ahmet" with pagination.

```javascript
const searchTerm = 'ahmet';
let page = 1;
const pageSize = 20;
let allUsers = [];

while (true) {
  const result = await searchUsers(searchTerm, page, pageSize);
  
  if (result.data.length === 0) {
    break; // No more results
  }
  
  allUsers = allUsers.concat(result.data);
  page++;
}

console.log(`Total users found: ${allUsers.length}`);
```

---

### Use Case 4: Partial Phone Search

**Scenario:** Find all users with a specific phone prefix (e.g., area code).

```javascript
// Find all users with phones starting with 0532
const result = await searchUsers('0532', 1, 50);

result.data.forEach(user => {
  console.log(`${user.fullName}: ${user.mobilePhones}`);
});
```

---

## Best Practices

### 1. Input Validation

Always validate and sanitize user input before sending to the API:

```javascript
function validateSearchTerm(searchTerm) {
  if (!searchTerm || searchTerm.trim().length === 0) {
    throw new Error('Search term cannot be empty');
  }
  
  if (searchTerm.length < 2) {
    throw new Error('Search term must be at least 2 characters');
  }
  
  return searchTerm.trim();
}

// Usage
const searchTerm = validateSearchTerm(userInput);
const result = await searchUsers(searchTerm);
```

---

### 2. Error Handling

Implement comprehensive error handling:

```javascript
async function safeSearchUsers(searchTerm) {
  try {
    const result = await searchUsers(searchTerm);
    return { success: true, data: result.data };
  } catch (error) {
    if (error.response?.status === 401) {
      return { success: false, error: 'Session expired. Please login again.' };
    } else if (error.response?.status === 403) {
      return { success: false, error: 'You do not have admin permissions.' };
    } else if (error.response?.status === 400) {
      return { success: false, error: 'Invalid search term.' };
    } else {
      return { success: false, error: 'Search failed. Please try again.' };
    }
  }
}
```

---

### 3. Debouncing for Autocomplete

When implementing search-as-you-type, use debouncing to reduce API calls:

```javascript
let searchTimeout;

function debounceSearch(searchTerm, delay = 300) {
  clearTimeout(searchTimeout);
  
  return new Promise((resolve) => {
    searchTimeout = setTimeout(async () => {
      const result = await searchUsers(searchTerm, 1, 10);
      resolve(result);
    }, delay);
  });
}

// Usage in a search input handler
inputElement.addEventListener('input', async (e) => {
  const searchTerm = e.target.value;
  
  if (searchTerm.length >= 2) {
    const result = await debounceSearch(searchTerm);
    displaySearchResults(result.data);
  }
});
```

---

### 4. Token Management

Always check token expiration before making requests:

```javascript
function isTokenExpired(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const expiry = payload.exp * 1000; // Convert to milliseconds
    return Date.now() >= expiry;
  } catch {
    return true;
  }
}

async function searchUsersWithTokenCheck(searchTerm) {
  if (isTokenExpired(adminToken)) {
    // Refresh token or redirect to login
    await refreshAdminToken();
  }
  
  return await searchUsers(searchTerm);
}
```

---

## Testing

### Test Scenarios

| Test Case | Input | Expected Result |
|-----------|-------|-----------------|
| Valid phone search | `searchTerm=05046866386` | Returns matching user(s) |
| Valid email search | `searchTerm=user@example.com` | Returns matching user(s) |
| Valid name search | `searchTerm=ahmet` | Returns all users with "ahmet" in name |
| Partial phone search | `searchTerm=0504` | Returns all users with phones containing "0504" |
| Empty search term | `searchTerm=` | Returns 400 error |
| No results | `searchTerm=nonexistent` | Returns empty array with success=true |
| Without auth token | (no Authorization header) | Returns 401 error |
| Non-admin user | (valid token, non-admin role) | Returns 403 error |
| Case insensitive | `searchTerm=AHMET` | Same results as `searchTerm=ahmet` |
| Pagination | `page=2&pageSize=10` | Returns results 11-20 |

---

## Troubleshooting

### Issue: "Search term cannot be empty" Error

**Problem:** Getting 400 error even with a search term  
**Solution:** Ensure parameter name is `searchTerm`, not `query`

❌ Wrong:
```
/api/admin/users/search?query=05046866386
```

✅ Correct:
```
/api/admin/users/search?searchTerm=05046866386
```

---

### Issue: 401 Unauthorized

**Problem:** Getting unauthorized error  
**Solutions:**
1. Check if token is included in Authorization header
2. Verify token format: `Bearer {token}` (with space)
3. Check if token has expired
4. Verify token contains Admin role claim

---

### Issue: 403 Forbidden

**Problem:** Token is valid but getting forbidden error  
**Solution:** User doesn't have Admin role. Check JWT claims:

```javascript
// Decode token to check roles
const payload = JSON.parse(atob(token.split('.')[1]));
console.log('Roles:', payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']);
// Should include "Admin"
```

---

### Issue: No Results Found

**Problem:** Search returns empty array  
**Solutions:**
1. Verify search term is correct
2. Try partial search (e.g., "050" instead of full phone)
3. Check if user actually exists in database
4. Test with different search terms (email, name)

---

## API Limits

| Limit Type | Value | Notes |
|------------|-------|-------|
| Max pageSize | No hard limit | Recommended: 50-100 for performance |
| Rate limiting | TBD | Contact backend team for current limits |
| Search term min length | 1 character | Recommended: 2+ for better results |
| Timeout | 30 seconds | Standard API timeout |

---

## Contact & Support

For integration support or questions:
- **Backend Team:** backend@ziraai.com
- **API Documentation:** https://docs.ziraai.com
- **Issue Tracking:** https://github.com/ziraai/issues

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-03-26 | Initial documentation |

---

## Related Endpoints

- `GET /api/admin/users` - Get all users with pagination
- `GET /api/admin/users/{userId}` - Get user by ID
- `POST /api/admin/users/{userId}/deactivate` - Deactivate user
- `POST /api/admin/users/{userId}/reactivate` - Reactivate user

See [FRONTEND_INTEGRATION_GUIDE_COMPLETE.md](./FRONTEND_INTEGRATION_GUIDE_COMPLETE.md) for complete admin API documentation.
