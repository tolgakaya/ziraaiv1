# Sponsorship Code Inbox API Documentation

## Overview
This API allows farmers to view sponsorship codes that have been sent to their phone number via SMS or WhatsApp **before downloading the mobile app**. This enables farmers to:
- View all codes sent to their phone
- See sponsor information
- Check code expiry status
- Access redemption links
- Verify codes before app installation

## Endpoint

### Get Farmer Sponsorship Inbox

**URL:** `GET /api/v1/sponsorship/farmer-inbox`

**Authentication:** None (Public endpoint)

**Description:** Retrieves all sponsorship codes sent to a specific phone number

## Request Parameters

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `phone` | string | Yes | - | Farmer's phone number (any format accepted) |
| `includeUsed` | boolean | No | false | Include already redeemed codes |
| `includeExpired` | boolean | No | false | Include expired codes |

### Phone Number Format

The API accepts phone numbers in any of these formats:
- `05551234567` (Turkish format without +90)
- `+905551234567` (International format with country code)
- `555 123 45 67` (With spaces)
- `(555) 123-45-67` (With parentheses and dashes)

All formats are automatically normalized to `+905551234567` for matching.

## Response Format

### Success Response (200 OK)

```json
{
  "success": true,
  "message": "5 sponsorluk kodu bulundu",
  "data": [
    {
      "code": "SPCODE123ABC",
      "sponsorName": "AgriTech A.Ş.",
      "tierName": "Large",
      "sentDate": "2025-01-20T14:30:00Z",
      "sentVia": "SMS",
      "isUsed": false,
      "usedDate": null,
      "expiryDate": "2025-02-20T14:30:00Z",
      "redemptionLink": "https://ziraai.com/redeem/SPCODE123ABC",
      "recipientName": "Ahmet Yılmaz",
      "isExpired": false,
      "daysUntilExpiry": 25,
      "status": "Aktif"
    },
    {
      "code": "SPCODE456DEF",
      "sponsorName": "Farm Solutions Ltd.",
      "tierName": "Medium",
      "sentDate": "2025-01-15T10:15:00Z",
      "sentVia": "WhatsApp",
      "isUsed": true,
      "usedDate": "2025-01-16T08:20:00Z",
      "expiryDate": "2025-02-15T10:15:00Z",
      "redemptionLink": "https://ziraai.com/redeem/SPCODE456DEF",
      "recipientName": "Ahmet Yılmaz",
      "isExpired": false,
      "daysUntilExpiry": 20,
      "status": "Kullanıldı"
    }
  ]
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `code` | string | Unique sponsorship code |
| `sponsorName` | string | Company name of the sponsor |
| `tierName` | string | Subscription tier (Small, Medium, Large, XL) |
| `sentDate` | datetime | When the code was sent to farmer |
| `sentVia` | string | Delivery channel (SMS, WhatsApp) |
| `isUsed` | boolean | Whether code has been redeemed |
| `usedDate` | datetime? | When code was redeemed (null if not used) |
| `expiryDate` | datetime | Code expiration date |
| `redemptionLink` | string | Direct link to redeem the code |
| `recipientName` | string | Name of the farmer |
| `isExpired` | boolean | Computed: true if past expiry date |
| `daysUntilExpiry` | int | Computed: days remaining until expiry |
| `status` | string | Computed: "Aktif", "Kullanıldı", or "Süresi Doldu" |

### Empty Inbox Response (200 OK)

```json
{
  "success": true,
  "message": "Henüz sponsorluk kodu gönderilmemiş",
  "data": []
}
```

### Error Responses

#### 400 Bad Request - Missing Phone Number
```json
{
  "success": false,
  "message": "Telefon numarası gereklidir"
}
```

#### 500 Internal Server Error
```json
{
  "success": false,
  "message": "Sponsorluk kutusu yüklenirken hata oluştu"
}
```

## Example Requests

### Basic Request - Active Codes Only (Default)

```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=05551234567"
```

```javascript
// JavaScript/TypeScript
const response = await fetch(
  'https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=05551234567'
);
const data = await response.json();
```

```dart
// Flutter/Dart
final response = await http.get(
  Uri.parse('https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=05551234567'),
);
final data = jsonDecode(response.body);
```

### Include Used and Expired Codes

```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=%2B905551234567&includeUsed=true&includeExpired=true"
```

```javascript
const response = await fetch(
  'https://api.ziraai.com/api/v1/sponsorship/farmer-inbox' +
  '?phone=+905551234567&includeUsed=true&includeExpired=true'
);
```

### With International Format

```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=%2B905551234567"
```

## Use Cases

### 1. Pre-Registration Code Viewing
**Scenario:** Farmer receives SMS with sponsorship code before installing app

**Flow:**
1. Farmer receives SMS: "You received a ZiraAI sponsorship code! View at: https://web.ziraai.com/inbox?phone=05551234567"
2. Farmer clicks link, web app calls API
3. Displays all codes sent to their phone
4. Farmer can see sponsor details and redemption links

```javascript
// Web app code
async function showInbox(phoneFromUrl) {
  const response = await fetch(
    `/api/v1/sponsorship/farmer-inbox?phone=${phoneFromUrl}`
  );
  const { data: codes } = await response.json();

  // Display codes with status badges
  codes.forEach(code => {
    displayCode({
      ...code,
      statusColor: code.isUsed ? 'gray' :
                   code.isExpired ? 'red' : 'green'
    });
  });
}
```

### 2. Mobile App Initial Sync
**Scenario:** User installs app and wants to see pending codes

**Flow:**
1. User registers with phone number
2. App immediately calls inbox API
3. Shows pending codes in notification badge
4. User can redeem directly from app

```dart
// Flutter code
Future<List<SponsorshipCode>> syncInbox() async {
  final phone = await getUserPhone();
  final response = await http.get(
    Uri.parse('$apiBase/sponsorship/farmer-inbox?phone=$phone'),
  );

  if (response.statusCode == 200) {
    final result = jsonDecode(response.body);
    final codes = (result['data'] as List)
        .map((json) => SponsorshipCode.fromJson(json))
        .toList();

    // Show notification badge with count
    final activeCodes = codes.where((c) =>
      !c.isUsed && !c.isExpired
    ).length;

    updateBadgeCount(activeCodes);
    return codes;
  }

  throw Exception('Failed to load inbox');
}
```

### 3. Code Verification Before Redemption
**Scenario:** Farmer wants to verify code details before redeeming

```javascript
async function verifyAndDisplay(phone) {
  const response = await fetch(
    `/api/v1/sponsorship/farmer-inbox?phone=${phone}`
  );
  const { data: codes } = await response.json();

  // Group by status
  const active = codes.filter(c => !c.isUsed && !c.isExpired);
  const used = codes.filter(c => c.isUsed);
  const expired = codes.filter(c => c.isExpired);

  return {
    active: active.length,
    used: used.length,
    expired: expired.length,
    canRedeem: active.length > 0
  };
}
```

## Frontend Integration Guide

### Display Status Badge

```typescript
function getStatusBadge(code: SponsorshipCode): BadgeProps {
  if (code.isUsed) {
    return {
      text: 'Kullanıldı',
      color: 'gray',
      icon: 'check-circle'
    };
  }

  if (code.isExpired) {
    return {
      text: 'Süresi Doldu',
      color: 'red',
      icon: 'x-circle'
    };
  }

  // Active code - show urgency
  if (code.daysUntilExpiry <= 3) {
    return {
      text: `${code.daysUntilExpiry} gün kaldı`,
      color: 'orange',
      icon: 'alert'
    };
  }

  return {
    text: 'Aktif',
    color: 'green',
    icon: 'check'
  };
}
```

### Sort Codes by Priority

```typescript
function sortCodesByPriority(codes: SponsorshipCode[]): SponsorshipCode[] {
  return codes.sort((a, b) => {
    // Active codes first
    if (a.isUsed !== b.isUsed) {
      return a.isUsed ? 1 : -1;
    }

    // Then by expiry urgency (closest to expiry first)
    if (!a.isUsed && !b.isUsed) {
      return a.daysUntilExpiry - b.daysUntilExpiry;
    }

    // Finally by sent date (newest first)
    return new Date(b.sentDate).getTime() - new Date(a.sentDate).getTime();
  });
}
```

## Performance Notes

### Database Queries
The endpoint performs **3 database queries total** regardless of result count:
1. Query codes by phone number (with filters)
2. Batch query sponsor profiles (all sponsors in single query)
3. Batch query subscription tiers (all tiers in single query)

**Performance characteristics:**
- Database index on `RecipientPhone` (recommended for optimal performance)
- No N+1 query problem (batch queries for relationships)
- Response time: ~50-100ms for typical inbox (5-10 codes)

### Caching Recommendations

**Client-side caching:**
- Cache duration: 5 minutes
- Invalidate on: code redemption, new code notification

```typescript
// Example caching strategy
class InboxCache {
  private cache = new Map<string, CachedInbox>();
  private TTL = 5 * 60 * 1000; // 5 minutes

  async getInbox(phone: string): Promise<SponsorshipCode[]> {
    const cached = this.cache.get(phone);

    if (cached && Date.now() - cached.timestamp < this.TTL) {
      return cached.codes;
    }

    const codes = await fetchInbox(phone);
    this.cache.set(phone, { codes, timestamp: Date.now() });

    return codes;
  }

  invalidate(phone: string) {
    this.cache.delete(phone);
  }
}
```

## Security Considerations

### No Authentication Required
This endpoint is **intentionally public** because:
1. Farmers may not have app installed yet
2. Phone number acts as identifier
3. Codes are already sent to farmer's phone (possession implies authorization)
4. Redemption link is non-sensitive (requires separate redemption flow)

### Privacy Protection
- Phone number is required to view codes
- Only codes sent to that specific phone are returned
- No sensitive sponsor data is exposed (only company name)

### Rate Limiting
**Recommendation:** Apply rate limiting to prevent abuse
- 10 requests per minute per IP
- 50 requests per hour per phone number

```nginx
# Nginx rate limiting example
limit_req_zone $binary_remote_addr zone=inbox_ip:10m rate=10r/m;
limit_req_zone $arg_phone zone=inbox_phone:10m rate=50r/h;

location /api/v1/sponsorship/farmer-inbox {
    limit_req zone=inbox_ip burst=5;
    limit_req zone=inbox_phone burst=10;
}
```

## Testing

### Test Cases

#### 1. Valid Phone Number
```bash
# Test: Returns codes for valid phone
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=05551234567"

# Expected: 200 OK with array of codes
```

#### 2. Phone Number Format Variations
```bash
# Test: Accepts different formats
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=%2B905551234567"
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=555%20123%2045%2067"

# Expected: All return same codes (normalized)
```

#### 3. No Codes Sent
```bash
# Test: Phone with no codes
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=05559999999"

# Expected: 200 OK with empty array and message "Henüz sponsorluk kodu gönderilmemiş"
```

#### 4. Include Used Codes
```bash
# Test: Show used codes
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox?phone=05551234567&includeUsed=true"

# Expected: Includes codes with isUsed=true
```

#### 5. Missing Phone Number
```bash
# Test: Missing required parameter
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/farmer-inbox"

# Expected: 400 Bad Request
```

### Postman Collection

```json
{
  "info": {
    "name": "Sponsorship Inbox API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Get Active Codes Only",
      "request": {
        "method": "GET",
        "url": {
          "raw": "{{apiBase}}/api/v1/sponsorship/farmer-inbox?phone=05551234567",
          "host": ["{{apiBase}}"],
          "path": ["api", "v1", "sponsorship", "farmer-inbox"],
          "query": [
            {
              "key": "phone",
              "value": "05551234567"
            }
          ]
        }
      }
    },
    {
      "name": "Get All Codes (Including Used)",
      "request": {
        "method": "GET",
        "url": {
          "raw": "{{apiBase}}/api/v1/sponsorship/farmer-inbox?phone=05551234567&includeUsed=true&includeExpired=true",
          "query": [
            {
              "key": "phone",
              "value": "05551234567"
            },
            {
              "key": "includeUsed",
              "value": "true"
            },
            {
              "key": "includeExpired",
              "value": "true"
            }
          ]
        }
      }
    }
  ]
}
```

## Troubleshooting

### Issue: Returns empty array but farmer received SMS

**Possible causes:**
1. Phone number format mismatch in database
2. `LinkDelivered` flag not set to true
3. Different phone number used for sending

**Solution:**
```sql
-- Check what format phone is stored in database
SELECT RecipientPhone, LinkDelivered, Code, LinkSentDate
FROM SponsorshipCodes
WHERE RecipientPhone LIKE '%5551234567%';

-- Verify phone normalization matches
-- Both should normalize to same format: +905551234567
```

### Issue: Slow response time

**Possible causes:**
1. Missing database index on `RecipientPhone`
2. Large number of codes for single phone
3. N+1 query problem (check batch queries)

**Solution:**
```sql
-- Add index for performance
CREATE INDEX idx_sponsorshipcodes_recipientphone
ON SponsorshipCodes(RecipientPhone, LinkDelivered, IsUsed, ExpiryDate);

-- Verify index usage
EXPLAIN ANALYZE
SELECT * FROM SponsorshipCodes
WHERE RecipientPhone = '+905551234567'
  AND LinkDelivered = true;
```

### Issue: Codes show wrong sponsor name

**Possible causes:**
1. Sponsor profile deleted but codes remain
2. Data inconsistency in sponsor_id

**Solution:**
- Handler returns "Unknown Sponsor" for missing profiles
- Check sponsor profile exists: `SELECT * FROM SponsorProfiles WHERE SponsorId = X`

## Related Documentation

- [SendSponsorshipLink API](./API_SendSponsorshipLink.md) - How codes are sent to farmers
- [Code Redemption API](./API_RedeemCode.md) - How farmers redeem codes
- [Implementation Plan](./SponsorshipInbox_Implementation_Plan.md) - Technical implementation details
- [Environment Configuration](../environment-configuration.md) - Deep link and URL configuration

## Change Log

### Version 1.0.0 (2025-01-24)
- Initial implementation
- Public endpoint (no authentication)
- Phone normalization support
- Batch query optimization
- Computed status fields
