# Bulk Farmer Invitation API Documentation

## Overview
This document compares the two bulk farmer invitation endpoints available in the ZiraAI platform:
1. **Sponsor Bulk Invitation** - Sponsors create bulk invitations for farmers via Excel upload
2. **Admin Bulk Invitation** - Admins create bulk invitations on behalf of sponsors via JSON payload

Both endpoints use the **same phone normalization system** (E.164 format: `+90XXXXXXXXXX`) to ensure consistency.

---

## 1. Sponsor Bulk Farmer Invitation (Excel Upload)

### Endpoint
```
POST /api/Sponsorship/farmer/invitations/bulk
```

### Authentication
- **Required**: Yes (JWT Bearer Token)
- **Roles**: `Sponsor` or `Admin`
- **Authorization Header**: `Bearer {access_token}`

### Request Format
**Content-Type**: `multipart/form-data`

#### Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `excelFile` | File | ✅ Yes | Excel file (.xlsx, .xls) containing farmer data |
| `channel` | String | ❌ No | Delivery channel: `SMS` or `WhatsApp` (default: `SMS`) |
| `customMessage` | String | ❌ No | Custom SMS/WhatsApp message template (overrides default) |

#### Excel File Format
The Excel file must contain the following columns:

| Column Name | Required | Type | Description | Example |
|-------------|----------|------|-------------|---------|
| `Phone` | ✅ Yes | String | Farmer's phone number (Turkish format) | `05421396386` or `+905421396386` |
| `FarmerName` | ❌ No | String | Farmer's full name | `Ahmet Yılmaz` |
| `Email` | ❌ No | String | Farmer's email address | `ahmet@example.com` |
| `PackageTier` | ❌ No | String | Subscription tier: `S`, `M`, `L`, `XL` | `M` |
| `Notes` | ❌ No | String | Additional notes for tracking | `Bölge 1 - Antalya` |

**Important Notes**:
- ✅ Each farmer receives **exactly 1 code** (CodeCount is fixed at 1)
- ✅ Phone numbers are automatically normalized to E.164 format (`+90XXXXXXXXXX`)
- ✅ Maximum file size: **5 MB**
- ✅ Maximum rows: **2000 farmers per upload**

#### Supported Phone Formats (All normalized to +90XXXXXXXXXX)
- `05421396386` → `+905421396386`
- `+905421396386` → `+905421396386`
- `905421396386` → `+905421396386`
- `5421396386` → `+905421396386`
- `+90 542 139 6386` → `+905421396386`

### Request Example (cURL)
```bash
curl -X POST "https://api.ziraai.com/api/Sponsorship/farmer/invitations/bulk" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -F "excelFile=@farmers.xlsx" \
  -F "channel=SMS" \
  -F "customMessage=Merhaba {farmerName}, {sponsorName} size {codeCount} adet sponsorluk kodu gönderdi. Link: {deepLink}"
```

### Request Example (JavaScript/Fetch)
```javascript
const formData = new FormData();
formData.append('excelFile', fileInput.files[0]);
formData.append('channel', 'SMS'); // or 'WhatsApp'
formData.append('customMessage', 'Your custom message here');

const response = await fetch('https://api.ziraai.com/api/Sponsorship/farmer/invitations/bulk', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${accessToken}`
  },
  body: formData
});

const result = await response.json();
```

### Response Format
**Content-Type**: `application/json`

#### Success Response (200 OK)
```json
{
  "data": {
    "jobId": 123,
    "totalDealers": 45,
    "status": "Queued",
    "createdDate": "2025-01-07T10:30:00Z",
    "statusCheckUrl": "/api/Sponsorship/bulk-jobs/123/status"
  },
  "success": true,
  "message": "45 davet başarıyla sıraya alındı"
}
```

#### Response Fields
| Field | Type | Description |
|-------|------|-------------|
| `jobId` | Integer | Unique job identifier for tracking |
| `totalDealers` | Integer | Total number of farmers queued for invitation |
| `status` | String | Job status: `Queued`, `Processing`, `Completed`, `Failed` |
| `createdDate` | DateTime | Job creation timestamp (UTC) |
| `statusCheckUrl` | String | Endpoint to check job progress |

#### Error Responses
**400 Bad Request** - Invalid Excel file or missing required data
```json
{
  "success": false,
  "message": "Excel dosyası zorunludur"
}
```

**401 Unauthorized** - Missing or invalid JWT token
```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

**500 Internal Server Error** - Server-side processing error
```json
{
  "success": false,
  "message": "Toplu davet işlemi sırasında hata oluştu"
}
```

### Processing Flow
1. **Excel Upload**: Frontend uploads Excel file with farmer data
2. **Parsing**: Backend parses Excel and validates phone numbers (normalizes to +90XXXXXXXXXX)
3. **Code Reservation**: System reserves sponsorship codes for each farmer
4. **Queue**: Invitations are queued in RabbitMQ for asynchronous processing
5. **Worker Processing**: Background worker sends SMS/WhatsApp invitations
6. **Status Tracking**: Job status can be checked via `statusCheckUrl`

### Business Rules
- ✅ Sponsor must have sufficient unused sponsorship codes
- ✅ Each farmer gets **exactly 1 code** per invitation (fixed, not configurable)
- ✅ Phone numbers are deduplicated within the same upload batch
- ✅ Invalid phone numbers are skipped with error logging
- ✅ Package tier filtering applies (if specified in Excel)
- ✅ All phone numbers stored in E.164 format (+90XXXXXXXXXX)
- ✅ Processing is **asynchronous** via RabbitMQ queue
- ✅ Job status can be tracked using the returned `statusCheckUrl`

---

## 2. Admin Bulk Farmer Invitation (JSON Payload)

### Endpoint
```
POST /api/Sponsorship/admin/farmer/invitations/bulk
```

### Authentication
- **Required**: Yes (JWT Bearer Token)
- **Roles**: `Admin` only
- **Authorization Header**: `Bearer {access_token}`

### Request Format
**Content-Type**: `application/json`

#### Request Body
```json
{
  "sponsorId": 45,
  "recipients": [
    {
      "phone": "05421396386",
      "farmerName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "codeCount": 3,
      "packageTier": "M",
      "notes": "Premium customer - Antalya region"
    },
    {
      "phone": "+905069468693",
      "farmerName": "Mehmet Demir",
      "email": "mehmet@example.com",
      "codeCount": 5,
      "packageTier": "L",
      "notes": "VIP customer"
    }
  ],
  "channel": "SMS",
  "customMessage": "Merhaba {farmerName}, {sponsorName} tarafından {codeCount} adet kod gönderildi."
}
```

#### Request Parameters
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sponsorId` | Integer | ✅ Yes | Target sponsor ID (admin creates on behalf of this sponsor) |
| `recipients` | Array | ✅ Yes | Array of farmer recipient objects |
| `channel` | String | ❌ No | Delivery channel: `SMS` or `WhatsApp` (default: `SMS`) |
| `customMessage` | String | ❌ No | Custom message template |

#### Recipient Object Schema
| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `phone` | String | ✅ Yes | Farmer's phone number | `05421396386` |
| `farmerName` | String | ❌ No | Farmer's full name | `Ahmet Yılmaz` |
| `email` | String | ❌ No | Farmer's email | `ahmet@example.com` |
| `codeCount` | Integer | ✅ Yes | Number of codes to assign (1-100) | `3` |
| `packageTier` | String | ❌ No | Tier filter: `S`, `M`, `L`, `XL` | `M` |
| `notes` | String | ❌ No | Admin notes | `Premium customer` |

**Note**: Phone numbers will be automatically normalized to E.164 format (`+90XXXXXXXXXX`) regardless of input format.

### Request Example (cURL)
```bash
curl -X POST "https://api.ziraai.com/api/Sponsorship/admin/farmer/invitations/bulk" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "sponsorId": 45,
    "recipients": [
      {
        "phone": "05421396386",
        "farmerName": "Ahmet Yılmaz",
        "codeCount": 3,
        "packageTier": "M"
      }
    ],
    "channel": "SMS"
  }'
```

### Request Example (JavaScript/Fetch)
```javascript
const payload = {
  sponsorId: 45,
  recipients: [
    {
      phone: "05421396386",
      farmerName: "Ahmet Yılmaz",
      email: "ahmet@example.com",
      codeCount: 3,
      packageTier: "M",
      notes: "Premium customer"
    }
  ],
  channel: "SMS",
  customMessage: "Custom template here"
};

const response = await fetch('https://api.ziraai.com/api/Sponsorship/admin/farmer/invitations/bulk', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(payload)
});

const result = await response.json();
```

### Response Format
**Content-Type**: `application/json`

#### Success Response (200 OK)
```json
{
  "data": {
    "totalRequested": 2,
    "successCount": 2,
    "failedCount": 0,
    "results": [
      {
        "phone": "+905421396386",
        "farmerName": "Ahmet Yılmaz",
        "codeCount": 3,
        "packageTier": "M",
        "success": true,
        "invitationId": 156,
        "invitationToken": "a3f5c8e1d9b7a6f4e2c8d1b9a7f5e3c1",
        "invitationLink": "https://ziraai.com/ref/a3f5c8e1d9b7a6f4e2c8d1b9a7f5e3c1",
        "errorMessage": null,
        "deliveryStatus": "Sent"
      },
      {
        "phone": "+905069468693",
        "farmerName": "Mehmet Demir",
        "codeCount": 5,
        "packageTier": "L",
        "success": true,
        "invitationId": 157,
        "invitationToken": "b4g6d9f2e0c8b7a5f3d0c2b8a6f4e2d0",
        "invitationLink": "https://ziraai.com/ref/b4g6d9f2e0c8b7a5f3d0c2b8a6f4e2d0",
        "errorMessage": null,
        "deliveryStatus": "Sent"
      }
    ]
  },
  "success": true,
  "message": "📱 2 davet başarıyla gönderildi via SMS"
}
```

#### Response Fields
| Field | Type | Description |
|-------|------|-------------|
| `totalRequested` | Integer | Total recipients in the request |
| `successCount` | Integer | Number of successfully sent invitations |
| `failedCount` | Integer | Number of failed invitations |
| `results` | Array | Detailed results for each recipient |

#### Result Item Fields
| Field | Type | Description |
|-------|------|-------------|
| `phone` | String | Normalized phone number (+90XXXXXXXXXX) |
| `farmerName` | String | Farmer's name |
| `codeCount` | Integer | Number of codes assigned |
| `packageTier` | String | Tier (S/M/L/XL) |
| `success` | Boolean | Whether invitation was sent successfully |
| `invitationId` | Integer | Database ID of created invitation |
| `invitationToken` | String | Unique 32-character invitation token |
| `invitationLink` | String | Full deep link URL for mobile app |
| `errorMessage` | String | Error details (if `success: false`) |
| `deliveryStatus` | String | `Sent`, `Failed - SMS Error`, `Failed - Insufficient Codes`, etc. |

#### Partial Success Response
```json
{
  "data": {
    "totalRequested": 3,
    "successCount": 2,
    "failedCount": 1,
    "results": [
      {
        "phone": "+905421396386",
        "success": true,
        "deliveryStatus": "Sent"
      },
      {
        "phone": "+905069468693",
        "success": false,
        "errorMessage": "Yetersiz kod (M tier). Mevcut: 2, İstenen: 5",
        "deliveryStatus": "Failed - Insufficient Codes"
      },
      {
        "phone": "+905392027178",
        "success": true,
        "deliveryStatus": "Sent"
      }
    ]
  },
  "success": true,
  "message": "📱 2 davet başarıyla gönderildi via SMS"
}
```

#### Error Responses
**400 Bad Request** - Invalid request data
```json
{
  "success": false,
  "message": "Hiç alıcı belirtilmedi"
}
```

**401 Unauthorized** - Missing or invalid JWT token
```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

**403 Forbidden** - Non-admin user attempting admin endpoint
```json
{
  "success": false,
  "message": "Admin yetkisi gereklidir"
}
```

**500 Internal Server Error**
```json
{
  "success": false,
  "message": "Toplu davet gönderimi sırasında hata oluştu"
}
```

### Processing Flow
1. **Admin Authorization**: System validates admin JWT token
2. **Sponsor Validation**: Verifies target sponsor exists and has codes
3. **Recipient Processing**: For each recipient:
   - Phone normalization to +90XXXXXXXXXX format
   - Tier validation (S/M/L/XL)
   - Code availability check
   - Invitation record creation
   - Code reservation
   - SMS/WhatsApp delivery
4. **Audit Logging**: Complete admin action audit trail
5. **Synchronous Response**: Immediate results with success/failure breakdown

### Business Rules
- ✅ **Admin only** operation (not available to sponsors)
- ✅ Creates invitations **on behalf of** specified sponsor
- ✅ Each recipient can request **1-100 codes** (variable, configurable per recipient)
- ✅ Tier-specific code filtering applies (if `packageTier` is specified)
- ✅ Insufficient codes result in failure for that specific recipient only
- ✅ **Synchronous processing** (immediate response, no queue)
- ✅ Complete audit trail with admin context (IP, UserAgent, timestamp)
- ✅ All phone numbers normalized to E.164 format (+90XXXXXXXXXX)
- ✅ Partial success supported: Some recipients can succeed while others fail

---

## 3. Feature Comparison Table

| Feature | Sponsor Bulk (Excel) | Admin Bulk (JSON) |
|---------|---------------------|-------------------|
| **Endpoint** | `/api/Sponsorship/farmer/invitations/bulk` | `/api/Sponsorship/admin/farmer/invitations/bulk` |
| **Method** | POST (multipart/form-data) | POST (application/json) |
| **Authorization** | Sponsor or Admin | Admin only |
| **Input Format** | Excel file (.xlsx, .xls) | JSON payload |
| **Max Recipients** | 2000 rows per file (5MB max) | Recommended: <100 per request |
| **Processing** | Asynchronous (RabbitMQ queue) | Synchronous (immediate response) |
| **Response** | Job ID + status tracking | Detailed per-recipient results |
| **Code Count** | **Fixed: 1 code per farmer** | **Variable: 1-100 codes per farmer** |
| **Tier Filtering** | Optional (Excel column) | Optional (per recipient) |
| **Custom Message** | Single for all recipients | Single for all recipients |
| **Channel Support** | SMS or WhatsApp | SMS or WhatsApp |
| **Phone Normalization** | ✅ Automatic (E.164) | ✅ Automatic (E.164) |
| **Audit Logging** | Sponsor action logged | Admin on-behalf-of logged |
| **Status Tracking** | Async job status endpoint | Immediate result in response |
| **Error Handling** | Logged, job continues | Per-recipient in response |
| **Use Case** | Large-scale farmer onboarding | Admin support & corrections |

---

## 4. Phone Number Normalization

Both endpoints use the **PhoneNumberHelper** utility to ensure consistent E.164 format.

### Normalization Rules
All phone numbers are converted to: `+90XXXXXXXXXX`

#### Input Examples
| Input Format | Output (Normalized) |
|--------------|---------------------|
| `05421396386` | `+905421396386` |
| `+905421396386` | `+905421396386` |
| `905421396386` | `+905421396386` |
| `5421396386` | `+905421396386` |
| `+90 542 139 6386` | `+905421396386` |
| `0542-139-6386` | `+905421396386` |

### Why Normalization Matters
- ✅ **Database Consistency**: All phone numbers stored in same format
- ✅ **Query Accuracy**: Farmers can find all their invitations regardless of input format
- ✅ **Deduplication**: Same phone number = same farmer (prevents duplicates)
- ✅ **International Standard**: E.164 format works globally

---

## 5. Integration Examples

### Frontend: Sponsor Excel Upload
```typescript
// React TypeScript Example
async function uploadBulkInvitations(file: File, channel: string) {
  const formData = new FormData();
  formData.append('excelFile', file);
  formData.append('channel', channel);

  try {
    const response = await fetch('/api/Sponsorship/farmer/invitations/bulk', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      },
      body: formData
    });

    const result = await response.json();

    if (result.success) {
      console.log(`Job ${result.data.jobId} created for ${result.data.totalDealers} farmers`);
      // Poll status endpoint
      pollJobStatus(result.data.jobId);
    } else {
      console.error('Upload failed:', result.message);
    }
  } catch (error) {
    console.error('Network error:', error);
  }
}

async function pollJobStatus(jobId: number) {
  const interval = setInterval(async () => {
    const response = await fetch(`/api/Sponsorship/bulk-jobs/${jobId}/status`, {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    });
    const status = await response.json();

    if (status.data.status === 'Completed' || status.data.status === 'Failed') {
      clearInterval(interval);
      console.log('Job finished:', status.data);
    }
  }, 5000); // Poll every 5 seconds
}
```

### Frontend: Admin JSON Bulk
```typescript
// React TypeScript Example
async function adminBulkInvite(sponsorId: number, recipients: any[]) {
  const payload = {
    sponsorId,
    recipients,
    channel: 'SMS'
  };

  try {
    const response = await fetch('/api/Sponsorship/admin/farmer/invitations/bulk', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('adminToken')}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(payload)
    });

    const result = await response.json();

    if (result.success) {
      console.log(`Success: ${result.data.successCount}, Failed: ${result.data.failedCount}`);

      // Display results
      result.data.results.forEach((r: any) => {
        if (r.success) {
          console.log(`✅ ${r.phone}: Invitation sent`);
        } else {
          console.error(`❌ ${r.phone}: ${r.errorMessage}`);
        }
      });
    }
  } catch (error) {
    console.error('Request failed:', error);
  }
}
```

---

## 6. Error Handling Best Practices

### Frontend Error Handling
```typescript
interface ApiError {
  success: false;
  message: string;
}

async function handleBulkInvitation(file: File) {
  try {
    const response = await fetch('/api/Sponsorship/farmer/invitations/bulk', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` },
      body: formData
    });

    if (!response.ok) {
      const error: ApiError = await response.json();

      switch (response.status) {
        case 400:
          alert(`Geçersiz veri: ${error.message}`);
          break;
        case 401:
          alert('Oturum süreniz doldu. Lütfen tekrar giriş yapın.');
          // Redirect to login
          break;
        case 403:
          alert('Bu işlem için yetkiniz yok.');
          break;
        case 500:
          alert('Sunucu hatası. Lütfen daha sonra tekrar deneyin.');
          break;
      }
      return;
    }

    const result = await response.json();
    // Handle success
  } catch (networkError) {
    alert('Ağ hatası. İnternet bağlantınızı kontrol edin.');
  }
}
```

---

## 7. Testing Scenarios

### Sponsor Excel Upload Testing
1. **Valid Excel**: 10 farmers with valid phone numbers → Success: 10 invitations queued
2. **Mixed Formats**: Phone numbers in various formats → All normalized to +90XXXXXXXXXX
3. **Invalid Tier**: PackageTier = "XXL" → Job created, invalid entries skipped
4. **Insufficient Codes**: 100 farmers but only 50 codes → 50 success, 50 failed
5. **Large File**: 1000+ farmers → Job queued, processed asynchronously

### Admin JSON Bulk Testing
1. **Valid Payload**: 5 recipients with valid data → Success: 5 invitations sent
2. **Partial Success**: 3 recipients, 1 has insufficient codes → Success: 2, Failed: 1
3. **Invalid Sponsor**: SponsorId = 999999 (non-existent) → 400 Bad Request
4. **Code Overflow**: Request 200 codes but sponsor has 100 → Failed with error message
5. **Duplicate Phones**: Same phone in multiple recipients → Each treated separately

---

## 8. Production Deployment Notes

### Environment Variables Required
```bash
# Referral System (Required for invitation links)
MobileApp__PlayStorePackageName=com.ziraai.app
Referral__DeepLinkBaseUrl=https://ziraai.com/ref/
Referral__FallbackDeepLinkBaseUrl=https://ziraai.com/ref/

# Messaging (SMS/WhatsApp)
SMS__ApiKey=your_sms_api_key
SMS__SenderId=ZIRAAI
WhatsApp__ApiKey=your_whatsapp_api_key

# RabbitMQ (For async sponsor bulk)
RabbitMQ__Host=localhost
RabbitMQ__Port=5672
RabbitMQ__Username=guest
RabbitMQ__Password=guest
```

### Database Indexes Recommended
```sql
-- Improve phone number query performance
CREATE INDEX idx_farmerinvitations_phone ON "FarmerInvitations" ("Phone");
CREATE INDEX idx_farmerinvitations_status ON "FarmerInvitations" ("Status");
CREATE INDEX idx_farmerinvitations_sponsorid ON "FarmerInvitations" ("SponsorId");

-- Composite index for common queries
CREATE INDEX idx_farmerinvitations_phone_status
  ON "FarmerInvitations" ("Phone", "Status");
```

---

## 9. Security Considerations

### Rate Limiting Recommended
- Sponsor Excel: Max 5 uploads per hour per sponsor
- Admin JSON: Max 100 requests per hour per admin
- Implement exponential backoff for failed attempts

### Validation Rules

#### Sponsor Excel Upload
- ✅ Phone numbers: Must be valid Turkish mobile (starts with 5XX after normalization)
- ✅ Code count: Always 1 (fixed, not configurable)
- ✅ Tier validation: Only S, M, L, XL accepted (if specified)
- ✅ Excel file size: Max **5MB**
- ✅ Excel rows: Max **2000 farmers** per upload

#### Admin JSON Bulk
- ✅ Phone numbers: Must be valid Turkish mobile (starts with 5XX after normalization)
- ✅ Code count: 1-100 per recipient (configurable per recipient)
- ✅ Tier validation: Only S, M, L, XL accepted (if specified)
- ✅ Recipients array: Recommended max 100 per request for performance

### Audit Logging
Both endpoints generate comprehensive audit logs:
- **Sponsor Action**: User ID, timestamp, recipient count, channel
- **Admin Action**: Admin ID, target sponsor, on-behalf-of context, IP address

---

## 10. FAQ

**Q: Why does admin endpoint process synchronously while sponsor uses queue?**
A: Admin operations are typically smaller volume and require immediate feedback for support scenarios. Sponsor uploads can be massive (1000+ farmers) and benefit from async processing.

**Q: Can sponsors specify different code counts per farmer?**
A: No, sponsor bulk (Excel upload) gives exactly 1 code per farmer. This is fixed and cannot be changed. Only the admin endpoint allows 1-100 codes per recipient.

**Q: What happens if the same phone number appears multiple times in Excel?**
A: Each row creates a separate invitation. The farmer will receive multiple invitations if phone is duplicated.

**Q: How long do invitation links remain valid?**
A: Configurable via `FarmerInvitation:TokenExpiryDays` (default: 30 days). Links expire after this period.

**Q: Can we change the SMS template?**
A: Yes, via `customMessage` parameter or by updating `FarmerInvitation:SmsTemplate` in database configuration.

---

## Support
For API issues or questions, contact:
- **Developer**: backend-team@ziraai.com
- **Documentation**: https://docs.ziraai.com/api
- **Postman Collection**: `ZiraAI_Complete_API_Collection_v6.1.json`

---

**Document Version**: 1.0
**Last Updated**: 2025-01-07
**API Version**: v1
**Breaking Changes**: Phone normalization implemented (2025-01-07)
