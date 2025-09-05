# WhatsApp Sponsor Request System - API Reference

## Base URL
```
Production: https://api.ziraai.com
Development: https://localhost:5001
```

## Authentication
All endpoints (except public deeplink processing) require JWT Bearer authentication:
```
Authorization: Bearer {jwt_token}
```

## API Endpoints

### 1. Create Sponsor Request

**Endpoint**: `POST /api/sponsor-request/create`  
**Authorization**: Farmer, Admin  
**Description**: Creates a new sponsorship request and generates WhatsApp deeplink

#### Request Body
```json
{
  "sponsorPhone": "+905551234567",
  "requestMessage": "Custom message (optional)",
  "requestedTierId": 2
}
```

#### Request Schema
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sponsorPhone` | string | ✅ | Sponsor's WhatsApp number in E.164 format |
| `requestMessage` | string | ❌ | Custom request message (uses default if empty) |
| `requestedTierId` | integer | ✅ | Desired subscription tier ID (1-4) |

#### Response (Success)
```json
{
  "success": true,
  "message": "Sponsor request created successfully",
  "data": "https://wa.me/+905551234567?text=Message%20with%20deeplink"
}
```

#### Response (Error)
```json
{
  "success": false,
  "message": "A pending request already exists for this sponsor",
  "data": null
}
```

#### Error Cases
- `400`: Invalid phone number format
- `404`: Sponsor not found
- `409`: Pending request already exists
- `429`: Daily request limit exceeded

---

### 2. Process Deeplink

**Endpoint**: `GET /api/sponsor-request/process/{hashedToken}`  
**Authorization**: Public (no auth required)  
**Description**: Validates WhatsApp deeplink token and returns request details

#### URL Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `hashedToken` | string | HMAC-SHA256 token from deeplink URL |

#### Response (Success)
```json
{
  "success": true,
  "message": "Request validated successfully",
  "data": {
    "id": 123,
    "farmerId": 45,
    "sponsorId": 12,
    "farmerPhone": "+905551234567",
    "sponsorPhone": "+905557654321",
    "farmerName": "Ahmet Demir",
    "requestMessage": "ZiraAI analiz talebim var, sponsor olur musunuz?",
    "requestDate": "2025-08-13T14:30:00Z",
    "status": "Pending",
    "requestedTierId": 2
  }
}
```

#### Response (Error)
```json
{
  "success": false,
  "message": "Request token has expired",
  "data": null
}
```

#### Error Cases
- `400`: Invalid token format
- `404`: Token not found
- `410`: Token expired (>24 hours)
- `409`: Request already processed

---

### 3. Get Pending Requests

**Endpoint**: `GET /api/sponsor-request/pending`  
**Authorization**: Sponsor, Admin  
**Description**: Retrieves pending sponsor requests for current sponsor

#### Query Parameters
| Parameter | Type | Optional | Description |
|-----------|------|----------|-------------|
| `page` | integer | ✅ | Page number (default: 1) |
| `pageSize` | integer | ✅ | Items per page (default: 20, max: 50) |

#### Response (Success)
```json
{
  "success": true,
  "message": "Found 3 pending requests",
  "data": [
    {
      "id": 123,
      "farmerId": 45,
      "sponsorId": 12,
      "farmerPhone": "+905551234567",
      "sponsorPhone": "+905557654321",
      "farmerName": "Ahmet Demir",
      "requestMessage": "ZiraAI analiz talebim var",
      "requestDate": "2025-08-13T14:30:00Z",
      "status": "Pending",
      "requestedTierId": 2
    }
  ]
}
```

#### Error Cases
- `401`: Unauthorized (invalid token)
- `403`: Forbidden (wrong role)
- `500`: Internal server error

---

### 4. Approve Sponsor Requests

**Endpoint**: `POST /api/sponsor-request/approve`  
**Authorization**: Sponsor, Admin  
**Description**: Approves one or multiple sponsor requests and generates sponsorship codes

#### Request Body
```json
{
  "requestIds": [123, 124, 125],
  "subscriptionTierId": 2,
  "approvalNotes": "Approved for Q3 2025 season"
}
```

#### Request Schema
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `requestIds` | integer[] | ✅ | List of request IDs to approve |
| `subscriptionTierId` | integer | ✅ | Subscription tier to assign (1-4) |
| `approvalNotes` | string | ❌ | Optional notes for approval |

#### Response (Success)
```json
{
  "success": true,
  "message": "3 requests approved successfully",
  "data": {
    "approvedCount": 3,
    "generatedCodes": [
      {
        "requestId": 123,
        "sponsorshipCode": "SP-12-456789",
        "farmerId": 45
      }
    ]
  }
}
```

#### Business Logic
1. Validates sponsor owns the requests
2. Updates request status to "Approved"
3. Generates unique sponsorship codes
4. Creates subscription records for farmers
5. Triggers notification system

#### Error Cases
- `400`: Invalid request IDs or tier
- `403`: Sponsor doesn't own requests
- `404`: Request not found or not pending
- `500`: Database transaction failure

---

### 5. Reject Sponsor Requests

**Endpoint**: `POST /api/sponsor-request/reject`  
**Authorization**: Sponsor, Admin  
**Description**: Rejects sponsor requests with optional reason

#### Request Body
```json
{
  "requestIds": [123, 124],
  "rejectionReason": "Budget constraints for Q3"
}
```

#### Response (Success)
```json
{
  "success": true,
  "message": "2 requests rejected successfully",
  "data": {
    "rejectedCount": 2
  }
}
```

#### Note
Currently returns placeholder response. Full implementation pending.

---

### 6. Generate WhatsApp Message URL

**Endpoint**: `GET /api/sponsor-request/{requestId}/whatsapp-message`  
**Authorization**: Farmer, Admin  
**Description**: Generates WhatsApp message URL for sending request to sponsor

#### URL Parameters
| Parameter | Type | Description |
|-----------|------|-------------|
| `requestId` | integer | Sponsor request ID |

#### Response (Success)
```json
{
  "success": true,
  "message": "WhatsApp message URL generated",
  "data": "https://wa.me/+905557654321?text=ZiraAI%20sponsor%20request...%0A%0AOnaylamak%20için%20tıklayın:%20https://ziraai.com/sponsor-request/abc123"
}
```

## Data Transfer Objects (DTOs)

### SponsorRequestDto
```csharp
public class SponsorRequestDto
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int SponsorId { get; set; }
    public string FarmerPhone { get; set; }
    public string SponsorPhone { get; set; }
    public string FarmerName { get; set; }
    public string RequestMessage { get; set; }
    public DateTime RequestDate { get; set; }
    public string Status { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public int? ApprovedSubscriptionTierId { get; set; }
    public string ApprovalNotes { get; set; }
    public string GeneratedSponsorshipCode { get; set; }
}
```

### CreateSponsorRequestDto
```csharp
public class CreateSponsorRequestDto
{
    public string SponsorPhone { get; set; }    // E.164 format: +905551234567
    public string RequestMessage { get; set; }  // Optional custom message
    public int RequestedTierId { get; set; }    // 1=S, 2=M, 3=L, 4=XL
}
```

### ApproveSponsorRequestDto
```csharp
public class ApproveSponsorRequestDto
{
    public List<int> RequestIds { get; set; }   // Multiple request approval
    public int SubscriptionTierId { get; set; } // Tier to assign
    public string ApprovalNotes { get; set; }   // Optional approval notes
}
```

## Error Response Format

### Standard Error Response
```json
{
  "success": false,
  "message": "Descriptive error message",
  "data": null,
  "errors": [
    {
      "field": "sponsorPhone",
      "message": "Invalid phone number format"
    }
  ]
}
```

### Error Codes
| HTTP Code | Description | Common Causes |
|-----------|-------------|---------------|
| `400` | Bad Request | Invalid input, validation errors |
| `401` | Unauthorized | Missing or invalid JWT token |
| `403` | Forbidden | Insufficient permissions |
| `404` | Not Found | Resource doesn't exist |
| `409` | Conflict | Duplicate request, business rule violation |
| `422` | Unprocessable Entity | Valid format but business logic error |
| `429` | Too Many Requests | Rate limit exceeded |
| `500` | Internal Server Error | Database or service failure |

## Rate Limiting

### Request Limits
- **Farmers**: 10 requests per day per farmer
- **Sponsors**: Unlimited pending request queries
- **Approval Operations**: No specific limits (business logic protected)

### Rate Limit Response
```json
{
  "success": false,
  "message": "Daily request limit exceeded (10 requests). Reset at midnight.",
  "data": {
    "currentCount": 10,
    "dailyLimit": 10,
    "resetTime": "2025-08-14T00:00:00Z"
  }
}
```

## Security Considerations

### Token Security
- **HMAC-SHA256**: Cryptographically secure token generation
- **24-hour Expiry**: Prevents replay attacks
- **URL-safe Encoding**: WhatsApp compatibility
- **Secret Rotation**: Configurable HMAC secret

### Phone Number Security
- **E.164 Format**: International standard (+countrycodephonenumber)
- **Validation**: Server-side format validation
- **Sanitization**: XSS prevention in messages
- **Privacy**: Phone numbers only visible to authorized users

### Authorization Levels
- **Public**: Only deeplink processing
- **Farmer**: Create requests, view own requests
- **Sponsor**: View/approve own pending requests
- **Admin**: Full access to all operations

## Integration Guidelines

### WhatsApp Message Format
```
Message Structure:
{RequestMessage}

Onaylamak için tıklayın: {DeeplinkURL}

Example:
ZiraAI yapay zeka ile bitki analizi yapmak istiyorum. 
Sponsor olur musunuz?

Onaylamak için tıklayın: https://ziraai.com/sponsor-request/abc123def456
```

### Mobile App Integration
```javascript
// React Native / Flutter integration
const createSponsorRequest = async (sponsorPhone, message, tierId) => {
  const response = await fetch('/api/sponsor-request/create', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      sponsorPhone,
      requestMessage: message,
      requestedTierId: tierId
    })
  });
  
  const result = await response.json();
  if (result.success) {
    // Open WhatsApp with generated URL
    Linking.openURL(result.data);
  }
};
```

### Webhook Integration (Future)
```json
{
  "eventType": "sponsor_request_created",
  "requestId": 123,
  "farmerId": 45,
  "sponsorId": 12,
  "timestamp": "2025-08-13T14:30:00Z"
}
```

## Testing Examples

### Postman Collection
```json
{
  "name": "WhatsApp Sponsor Request API",
  "requests": [
    {
      "name": "Create Request",
      "method": "POST",
      "url": "{{baseUrl}}/api/sponsor-request/create",
      "headers": {
        "Authorization": "Bearer {{farmerToken}}",
        "Content-Type": "application/json"
      },
      "body": {
        "sponsorPhone": "+905551234567",
        "requestMessage": "Test sponsor request",
        "requestedTierId": 2
      }
    }
  ]
}
```

### cURL Examples
```bash
# Create sponsor request
curl -X POST "https://api.ziraai.com/api/sponsor-request/create" \
  -H "Authorization: Bearer {jwt_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "sponsorPhone": "+905551234567",
    "requestMessage": "ZiraAI analiz talebim",
    "requestedTierId": 2
  }'

# Process deeplink
curl -X GET "https://api.ziraai.com/api/sponsor-request/process/abc123def456"

# Get pending requests (sponsor)
curl -X GET "https://api.ziraai.com/api/sponsor-request/pending" \
  -H "Authorization: Bearer {sponsor_jwt_token}"

# Approve requests
curl -X POST "https://api.ziraai.com/api/sponsor-request/approve" \
  -H "Authorization: Bearer {sponsor_jwt_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "requestIds": [123, 124],
    "subscriptionTierId": 2,
    "approvalNotes": "Approved for Q3 season"
  }'
```

## Configuration Reference

### Required Configuration (appsettings.json)
```json
{
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 10,
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "Yapay destekli ZiraAI kullanarak bitkilerimi analiz yapmak istiyorum. Sponsor olur musunuz?"
  },
  "Security": {
    "RequestTokenSecret": "your-secure-secret-key-here"
  }
}
```

### Environment Variables
```env
# Production
SPONSOR_REQUEST_SECRET=your-production-secret-key
SPONSOR_REQUEST_BASE_URL=https://ziraai.com/sponsor-request/

# Development
SPONSOR_REQUEST_SECRET=dev-secret-key
SPONSOR_REQUEST_BASE_URL=https://localhost:5001/sponsor-request/
```

## Subscription Tier Reference

### Available Tiers
| ID | Tier | Display Name | Daily Limit | Monthly Limit | Price (₺/month) |
|----|------|--------------|-------------|---------------|-----------------|
| 1 | S | Small | 5 | 50 | 99.99 |
| 2 | M | Medium | 20 | 200 | 299.99 |
| 3 | L | Large | 50 | 500 | 599.99 |
| 4 | XL | Extra Large | 200 | 2000 | 1499.99 |

## Status Lifecycle

### Request Status Flow
```
Pending → Approved → Active Subscription
       ↘ Rejected
       ↘ Expired (24h timeout)
```

### Status Descriptions
- **Pending**: Waiting for sponsor decision
- **Approved**: Sponsor accepted, sponsorship code generated
- **Rejected**: Sponsor declined the request
- **Expired**: Token expired without action (24 hours)

## Business Rules

### 1. Request Creation Rules
- One pending request per farmer-sponsor pair
- Maximum 10 requests per farmer per day
- Sponsor must be active user with valid phone
- Farmer must be authenticated with Farmer role

### 2. Token Security Rules
- HMAC-SHA256 with configurable secret
- 24-hour expiry from creation time
- URL-safe Base64 encoding
- Unique token per request (timestamp + user data)

### 3. Approval Rules
- Only pending requests can be approved
- Sponsor can only approve their own requests
- Bulk approval supported for efficiency
- Automatic sponsorship code generation
- Creates active subscription for farmer

### 4. Data Integrity Rules
- Foreign key constraints enforced
- Phone number format validation (E.164)
- Request message length limit (1000 chars)
- Audit trail for all operations

## Integration Patterns

### Mobile App Integration
```typescript
// TypeScript interface for mobile app
interface SponsorRequestAPI {
  createRequest(data: CreateSponsorRequestDto): Promise<string>;
  processDeeplink(token: string): Promise<SponsorRequestDto>;
  getPendingRequests(): Promise<SponsorRequestDto[]>;
  approveRequests(data: ApproveSponsorRequestDto): Promise<ApprovalResult>;
}
```

### Backend Service Integration
```csharp
// Dependency injection setup
services.AddScoped<ISponsorRequestService, SponsorRequestService>();
services.AddScoped<ISponsorRequestRepository, SponsorRequestRepository>();

// MediatR handler registration (automatic)
services.AddMediatR(Assembly.GetExecutingAssembly());
```

### Database Integration
```sql
-- Required tables
CREATE TABLE SponsorRequests (
    Id SERIAL PRIMARY KEY,
    FarmerId INTEGER REFERENCES Users(UserId),
    SponsorId INTEGER REFERENCES Users(UserId),
    -- ... other columns
    CONSTRAINT UK_SponsorRequest_Pending UNIQUE(FarmerId, SponsorId, Status)
);

CREATE TABLE SponsorContacts (
    Id SERIAL PRIMARY KEY,
    SponsorId INTEGER REFERENCES Users(UserId),
    -- ... other columns
    CONSTRAINT UK_SponsorContact_Phone UNIQUE(SponsorId, PhoneNumber)
);
```

This API reference provides complete documentation for integrating with the WhatsApp Sponsor Request System, including all endpoints, request/response schemas, error handling, and integration patterns.