# Admin On-Behalf-Of (OBO) Endpoint Kullanım Kılavuzu

## 📋 Genel Bakış

Sistemde admin'lerin sponsor adına işlem yapabilmesi için **3 farklı OBO endpoint** bulunmaktadır:

### Farmer Invitation (Davet Oluşturma):
1. **Admin Farmer Invitations - JSON** - `/api/Sponsorship/admin/farmer/invitations/bulk`
2. **Admin Farmer Invitations - Excel** - `/api/Sponsorship/admin/farmer/invitations/bulk/excel?onBehalfOfSponsorId=X` ✨ **YENİ**

### Code Distribution (Kod Dağıtımı):
3. **Bulk Code Distribution - Excel** - `/api/Sponsorship/bulk-code-distribution?onBehalfOfSponsorId=X`

Her endpoint'in farklı input format, işleme şekli ve kullanım senaryoları vardır.

## 🚨 Kritik Hata Tespiti

Admin rolünde sponsor adına toplu farmer invitation gönderimi yaparken **yanlış endpoint** kullanılıyor.

## Sorun

❌ **Yanlış Kullanım** (Mevcut):
```
POST /api/v1/sponsorship/farmer/invitations/bulk?onBehalfOfSponsorId=6
Content-Type: multipart/form-data
File: farmers.xlsx
```

**Neden Yanlış:**
- Bu endpoint **sponsor'un kendi daveti** için tasarlanmış
- Excel dosyası kabul ediyor (multipart/form-data)
- `command.SponsorId = userId.Value;` ile **giriş yapan kullanıcının ID'sini** (admin=166) kullanıyor
- `onBehalfOfSponsorId` parametresi bu endpoint'te **tanımlı değil ve işlenmiyor**
- Sonuç: Admin (ID=166) kendi adına gönderim yapıyor, sponsor (ID=6) adına değil

##✅ Doğru Kullanım

### Admin Bulk Farmer Invitation Endpoint

```http
POST /api/Sponsorship/admin/farmer/invitations/bulk
Authorization: Bearer {admin_jwt_token}
Content-Type: application/json

{
  "sponsorId": 6,
  "channel": "SMS",
  "customMessage": "Optional custom message",
  "recipients": [
    {
      "phone": "05421396386",
      "farmerName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "codeCount": 5,
      "packageTier": "M",
      "notes": "Bölge 1 - Antalya"
    },
    {
      "phone": "05339876543",
      "farmerName": "Mehmet Demir",
      "codeCount": 10,
      "packageTier": "L"
    }
  ]
}
```

## Endpoint Karşılaştırması

| Özellik | Sponsor Bulk | Admin OBO Bulk |
|---------|-------------|----------------|
| **Endpoint** | `/api/Sponsorship/farmer/invitations/bulk` | `/api/Sponsorship/admin/farmer/invitations/bulk` |
| **Authorization** | `Sponsor` veya `Admin` | **Sadece `Admin`** |
| **Method** | POST | POST |
| **Content-Type** | `multipart/form-data` (Excel) | `application/json` |
| **Sponsor ID** | Otomatik (giriş yapan kullanıcı) | Request body'de belirtilir |
| **Code Count** | Sabit 1 | Variable 1-100 per recipient |
| **Processing** | Asynchronous (RabbitMQ) | Synchronous (immediate) |
| **Response** | Job ID + status URL | Detaylı per-recipient sonuç |
| **On-Behalf-Of** | ❌ Desteklenmiyor | ✅ `sponsorId` parametresi ile |

## Request Parametreleri

### Required Parameters

```typescript
interface AdminBulkFarmerInvitationRequest {
  sponsorId: number;  // Target sponsor ID (NOT admin's ID)
  recipients: AdminFarmerInvitationRecipient[];
  channel?: "SMS" | "WhatsApp";  // Default: "SMS"
  customMessage?: string;
}

interface AdminFarmerInvitationRecipient {
  phone: string;  // Required: +90XXXXXXXXXX or 05XXXXXXXXX
  farmerName?: string;
  email?: string;
  codeCount: number;  // Required: 1-100
  packageTier?: "S" | "M" | "L" | "XL";
  notes?: string;
}
```

### Response Format

```typescript
interface BulkFarmerInvitationResult {
  totalRequested: number;
  successCount: number;
  failedCount: number;
  results: FarmerInvitationSendResult[];
}

interface FarmerInvitationSendResult {
  phone: string;
  farmerName: string;
  codeCount: number;
  packageTier: string;
  success: boolean;
  invitationId?: number;
  invitationToken?: string;
  invitationLink?: string;
  errorMessage?: string;
  deliveryStatus: string;  // "Sent", "Failed - Insufficient Codes", etc.
}
```

## cURL Örnekleri

### Başarılı Admin OBO Request

```bash
curl -X POST "https://api.ziraai.com/api/Sponsorship/admin/farmer/invitations/bulk" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sponsorId": 6,
    "channel": "SMS",
    "recipients": [
      {
        "phone": "05421396386",
        "farmerName": "Ahmet Yılmaz",
        "codeCount": 5,
        "packageTier": "M"
      },
      {
        "phone": "05339876543",
        "farmerName": "Mehmet Demir",
        "codeCount": 10,
        "packageTier": "L"
      }
    ]
  }'
```

### Response Örneği

```json
{
  "success": true,
  "message": "📱 2 davet başarıyla gönderildi via SMS",
  "data": {
    "totalRequested": 2,
    "successCount": 2,
    "failedCount": 0,
    "results": [
      {
        "phone": "05421396386",
        "farmerName": "Ahmet Yılmaz",
        "codeCount": 5,
        "packageTier": "M",
        "success": true,
        "invitationId": 1234,
        "invitationToken": "a1b2c3d4e5f67890...",
        "invitationLink": "https://ziraai.com/ref/a1b2c3d4e5f67890...",
        "deliveryStatus": "Sent"
      },
      {
        "phone": "05339876543",
        "farmerName": "Mehmet Demir",
        "codeCount": 10,
        "packageTier": "L",
        "success": true,
        "invitationId": 1235,
        "invitationToken": "f7e6d5c4b3a21098...",
        "invitationLink": "https://ziraai.com/ref/f7e6d5c4b3a21098...",
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

## JavaScript/Fetch Örneği

```javascript
async function adminBulkCreateFarmerInvitations(sponsorId, recipients) {
  const response = await fetch('/api/Sponsorship/admin/farmer/invitations/bulk', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getAdminToken()}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      sponsorId: sponsorId,
      channel: 'SMS',
      recipients: recipients.map(r => ({
        phone: r.phone,
        farmerName: r.name,
        codeCount: r.codeCount,
        packageTier: r.tier,
        notes: r.notes
      }))
    })
  });

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const result = await response.json();

  console.log(`Total: ${result.data.totalRequested}`);
  console.log(`Success: ${result.data.successCount}`);
  console.log(`Failed: ${result.data.failedCount}`);

  // Process individual results
  result.data.results.forEach(item => {
    if (item.success) {
      console.log(`✅ ${item.farmerName}: ${item.invitationLink}`);
    } else {
      console.error(`❌ ${item.farmerName}: ${item.errorMessage}`);
    }
  });

  return result;
}

// Usage
await adminBulkCreateFarmerInvitations(6, [
  { phone: '05421396386', name: 'Ahmet Yılmaz', codeCount: 5, tier: 'M' },
  { phone: '05339876543', name: 'Mehmet Demir', codeCount: 10, tier: 'L' }
]);
```

## Validation & Error Handling

### Common Errors

| HTTP Status | Error Message | Neden | Çözüm |
|-------------|---------------|-------|--------|
| 401 | Unauthorized | Admin JWT token yok/geçersiz | Token yenile |
| 403 | Forbidden | User Admin rolünde değil | Admin yetkisi gerekiyor |
| 400 | "Hiç alıcı belirtilmedi" | Recipients array boş | En az 1 recipient ekle |
| 400 | "Yetersiz kod. Mevcut: X, İstenen: Y" | Sponsor'da yeterli code yok | Sponsor'a code satın aldır |
| 400 | "Geçersiz tier: Z" | Tier S/M/L/XL dışında | Geçerli tier kullan |

### Partial Success Handling

Admin endpoint **partial success** destekliyor. Bazı recipients başarısız olabilir ama diğerleri başarılı olur:

```json
{
  "success": true,
  "message": "📱 2 davet başarıyla gönderildi via SMS",
  "data": {
    "totalRequested": 3,
    "successCount": 2,
    "failedCount": 1,
    "results": [
      {
        "phone": "05421396386",
        "success": true,
        "deliveryStatus": "Sent"
      },
      {
        "phone": "05339876543",
        "success": false,
        "errorMessage": "Yetersiz kod (M tier). Mevcut: 0, İstenen: 100",
        "deliveryStatus": "Failed - Insufficient Codes"
      },
      {
        "phone": "05551234567",
        "success": true,
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

**Frontend handling:**
```javascript
const result = await adminBulkCreateFarmerInvitations(sponsorId, recipients);

if (result.data.failedCount > 0) {
  const failures = result.data.results.filter(r => !r.success);
  console.warn(`${failures.length} invitation(s) failed:`);
  failures.forEach(f => {
    console.error(`- ${f.phone}: ${f.errorMessage}`);
  });

  // Show warning modal to user with retry option
  showPartialSuccessModal(result.data.successCount, result.data.failedCount, failures);
}
```

## Audit Logging

Admin OBO işlemleri kapsamlı audit log oluşturur:

- Admin User ID
- Target Sponsor ID
- IP Address
- User Agent
- Request Path
- Timestamp
- Success/Failure counts
- İndividual recipient results

Log format:
```
📤 ADMIN 166 creating 5 farmer invitations on behalf of sponsor 6 via SMS
✅ ADMIN created invitation 1234 for +905421396386
✅ ADMIN sent invitation 1234 to +905421396386
📧 ADMIN bulk farmer invitations completed. Success: 5, Failed: 0
```

## Security Considerations

### Authorization Flow

1. ✅ User must have **Admin** role
2. ✅ Valid JWT token required
3. ⚠️ **MISSING**: Sponsor existence validation
4. ⚠️ **MISSING**: Sponsor active status check
5. ⚠️ **MISSING**: Admin permission to act on behalf of specific sponsor

**TODO**: Backend'e sponsor validation eklenmeli:

```csharp
// Validate sponsor exists and has Sponsor role
var sponsorUser = await _userRepository.GetAsync(u => u.Id == request.SponsorId);
if (sponsorUser == null)
    return new ErrorDataResult<BulkFarmerInvitationResult>("Belirtilen sponsor bulunamadı");

if (!sponsorUser.Roles.Contains("Sponsor"))
    return new ErrorDataResult<BulkFarmerInvitationResult>("Belirtilen kullanıcı sponsor değil");

if (!sponsorUser.Status)
    return new ErrorDataResult<BulkFarmerInvitationResult>("Sponsor hesabı aktif değil");
```

## Migration Guide

### Eski Kod (Yanlış)

```javascript
// ❌ YANLIŞ - Sponsor endpoint kullanıyor
const formData = new FormData();
formData.append('excelFile', file);
formData.append('channel', 'SMS');

await fetch('/api/v1/sponsorship/farmer/invitations/bulk?onBehalfOfSponsorId=6', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${adminToken}` },
  body: formData
});
```

### Yeni Kod (Doğru)

```javascript
// ✅ DOĞRU - Admin OBO endpoint kullanıyor
const recipients = await parseExcelFile(file);  // Frontend'de parse et

await fetch('/api/Sponsorship/admin/farmer/invitations/bulk', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${adminToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    sponsorId: 6,
    channel: 'SMS',
    recipients: recipients
  })
});
```

## FAQ

**Q: Neden Excel upload yerine JSON kullanılıyor?**
A: Admin OBO endpoint synchronous processing yapıyor ve immediate feedback dönüyor. Excel parsing async queue gerektiriyor, bu da OBO audit trail için uygun değil.

**Q: Sponsor kendi adına Excel ile toplu gönderim yapabilir mi?**
A: Evet, sponsor'lar `/api/Sponsorship/farmer/invitations/bulk` endpoint'ini Excel file upload ile kullanabilir. Bu asynchronous processing yapar.

**Q: Admin hem Excel hem JSON kullanabilir mi?**
A: Hayır, admin OBO için sadece JSON endpoint var. Excel kullanmak isterse frontend'de parse edip JSON'a çevirmeli.

**Q: onBehalfOfSponsorId parametresi neden çalışmıyor?**
A: Çünkü bu parametre sponsor bulk endpoint'inde tanımlı değil. Admin OBO için `sponsorId` request body'de gönderilmeli.

---

## 2. ✨ Admin OBO: Farmer Invitations (Excel Upload) - YENİ

### Doğru Kullanım

Admin sponsor adına Excel ile bulk farmer invitation oluştururken:

```http
POST /api/Sponsorship/admin/farmer/invitations/bulk/excel?onBehalfOfSponsorId=6
Authorization: Bearer {admin_jwt_token}
Content-Type: multipart/form-data

FormData:
- excelFile: farmers.xlsx
- channel: SMS (optional, default: SMS)
- customMessage: Custom message text (optional)
```

### Endpoint Özellikleri

| Özellik | Detay |
|---------|-------|
| **Endpoint** | `/api/Sponsorship/admin/farmer/invitations/bulk/excel` |
| **Method** | POST |
| **Authorization** | **Sadece `Admin`** |
| **Content-Type** | `multipart/form-data` |
| **OBO Parameter** | `onBehalfOfSponsorId` (query param, admin için required) |
| **Input** | Excel file + channel + custom message |
| **Processing** | **Asynchronous (RabbitMQ queue)** |
| **Response** | Job ID + status tracking URL |
| **Code Count** | **Fixed: 1 code per farmer** |

### Sponsor Excel Endpoint ile Farkları

Bu endpoint, sponsor'un kendi adına Excel yükleme endpoint'i ile **tamamen aynı işlevselliğe** sahiptir. Tek fark, admin'in `onBehalfOfSponsorId` ile hedef sponsor'u belirtmesidir.

| Feature | Sponsor Excel | Admin OBO Excel |
|---------|--------------|-----------------|
| **Endpoint** | `/farmer/invitations/bulk` | `/admin/farmer/invitations/bulk/excel` |
| **Authorization** | Sponsor or Admin | **Admin only** |
| **Sponsor ID** | Auto (logged-in user) | Query param: `onBehalfOfSponsorId` |
| **Service** | `BulkFarmerInvitationService` | **Same service** |
| **Processing** | Async (RabbitMQ) | Async (RabbitMQ) |
| **Code Count** | 1 per farmer | 1 per farmer |
| **Excel Format** | Phone, Name, Email, Tier, Notes | **Same format** |
| **Audit Logging** | Standard | **Admin audit trail** |

### Excel Format

Excel dosyası şu kolonları içermelidir:

| Column | Required | Description | Example |
|--------|----------|-------------|---------|
| Phone | ✅ Yes | Farmer phone | `05421396386` |
| FarmerName | ❌ No | Farmer name | `Ahmet Yılmaz` |
| Email | ❌ No | Farmer email | `ahmet@example.com` |
| PackageTier | ❌ No | Tier (S/M/L/XL) | `M` |
| Notes | ❌ No | Additional notes | `Bölge 1 - Antalya` |

**Constraints:**
- Max file size: **5 MB**
- Max rows: **2000 farmers**
- Phone format: Supports all Turkish formats (auto-normalized to E.164)
- Code count: **Fixed at 1 per farmer**

### cURL Example

```bash
curl -X POST "https://api.ziraai.com/api/Sponsorship/admin/farmer/invitations/bulk/excel?onBehalfOfSponsorId=6" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -F "excelFile=@farmers.xlsx" \
  -F "channel=SMS" \
  -F "customMessage=Özel davet mesajınız"
```

### JavaScript/Fetch Example

```javascript
async function adminBulkCreateFarmerInvitationsExcel(sponsorId, excelFile, channel = 'SMS', customMessage = null) {
  const formData = new FormData();
  formData.append('excelFile', excelFile);
  formData.append('channel', channel);
  if (customMessage) {
    formData.append('customMessage', customMessage);
  }

  const response = await fetch(
    `/api/Sponsorship/admin/farmer/invitations/bulk/excel?onBehalfOfSponsorId=${sponsorId}`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${getAdminToken()}`
      },
      body: formData
    }
  );

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const result = await response.json();

  console.log(`Job ID: ${result.data.jobId}`);
  console.log(`Total Farmers: ${result.data.totalDealers}`);
  console.log(`Status URL: ${result.data.statusCheckUrl}`);

  return result;
}

// Usage
const fileInput = document.querySelector('#excelFile');
await adminBulkCreateFarmerInvitationsExcel(
  6,
  fileInput.files[0],
  'WhatsApp',
  'ZiraAI ile tanışma zamanı!'
);
```

### Response Format

```typescript
interface BulkInvitationJobDto {
  jobId: number;
  totalDealers: number;  // Total farmers in the job
  status: string;  // "Queued", "Processing", "Completed"
  createdDate: string;
  statusCheckUrl: string;
}
```

### Response Example

```json
{
  "success": true,
  "message": "Bulk farmer invitation job created successfully",
  "data": {
    "jobId": 123,
    "totalDealers": 150,
    "status": "Queued",
    "createdDate": "2025-01-07T14:30:00Z",
    "statusCheckUrl": "/api/Sponsorship/farmer-invitation-job-status/123"
  }
}
```

### Status Tracking

Job durumunu kontrol etmek için status URL kullanılır:

```http
GET /api/Sponsorship/farmer-invitation-job-status/123
Authorization: Bearer {admin_jwt_token}
```

Admin tüm job'ları görebilir, sponsor sadece kendi job'larını görebilir.

### Validation Rules

**Admin Kullanımı:**
- ✅ `onBehalfOfSponsorId` query parameter **zorunlu** ve > 0 olmalı
- ✅ Admin role gerekli
- ✅ Excel file zorunlu
- ✅ Target sponsor'un yeterli kodu olmalı (1 per farmer)

**Common Errors:**

| HTTP Status | Error Message | Neden | Çözüm |
|-------------|---------------|-------|--------|
| 400 | "Admin users must specify valid onBehalfOfSponsorId query parameter" | Admin `onBehalfOfSponsorId` göndermedi veya ≤ 0 | Query param ekle |
| 400 | "Excel dosyası zorunludur" | File upload yok | Excel file ekle |
| 400 | "File too large" | File > 5MB | Dosya boyutunu küçült |
| 400 | "Too many rows" | Excel > 2000 row | Satır sayısını azalt |
| 400 | "Insufficient codes" | Sponsor'da yeterli kod yok | Sponsor'a kod satın aldır |
| 401 | Unauthorized | JWT token yok/geçersiz | Token yenile |
| 403 | Forbidden | User Admin rolünde değil | Admin yetkisi gerekiyor |

### Asynchronous Processing Flow

1. **Upload**: Excel dosyası upload edilir
2. **Validation**: File size, row count, format kontrol edilir
3. **Parsing**: Excel satırları parse edilir (header-based)
4. **Code Check**: Sponsor'un yeterli kodu olup olmadığı kontrol edilir
5. **Job Creation**: `BulkInvitationJob` entity oluşturulur
6. **Queue**: Her farmer için RabbitMQ'ya message publish edilir
7. **Response**: Job ID dönülür (immediate response)
8. **Background Processing**: Worker service mesajları işler
9. **SMS/WhatsApp Send**: Her farmer'a davet linki gönderilir
10. **Completion**: Job status "Completed" olur

### Audit Logging

Admin OBO Excel upload işlemleri kapsamlı audit log oluşturur:

```
📤 ADMIN Excel bulk farmer invitation request for sponsor 6
📊 ADMIN 166 processing Excel file: farmers.xlsx (245678 bytes) for sponsor 6 via SMS
✅ ADMIN 166 queued farmer invitations successfully for sponsor 6. JobId: 123, Count: 150
```

Audit log içeriği:
- **action**: "AdminBulkCreateFarmerInvitationsExcel"
- **adminUserId**: 166
- **targetUserId**: 6 (sponsor ID)
- **entityType**: "FarmerInvitation"
- **entityId**: 123 (job ID)
- **isOnBehalfOf**: true
- **reason**: "Admin created bulk farmer invitations via Excel upload (JobId: 123, Count: 150) via SMS"
- **afterState**: Job details, file info, channel

### JSON Endpoint ile Karşılaştırma

| Feature | JSON Endpoint | Excel Endpoint |
|---------|--------------|----------------|
| **URL** | `/admin/farmer/invitations/bulk` | `/admin/farmer/invitations/bulk/excel` |
| **Input** | JSON body | Excel file |
| **OBO Param** | `sponsorId` (body) | `onBehalfOfSponsorId` (query) |
| **Code Count** | Variable (1-100 per recipient) | Fixed (1 per farmer) |
| **Processing** | Synchronous | Asynchronous |
| **Response** | Per-recipient results | Job ID |
| **Max Recipients** | No hard limit | 2000 rows |
| **Use Case** | Variable code needs, immediate feedback | Bulk operations, 1 code per farmer |

### Ne Zaman Kullanılır?

**JSON Endpoint Tercih Et:**
- ✅ Her farmer'a farklı sayıda kod verilecek (1-100)
- ✅ Immediate per-recipient feedback gerekli
- ✅ Az sayıda recipient (< 50)
- ✅ Frontend'de recipient listesi hazır

**Excel Endpoint Tercih Et:**
- ✅ Her farmer'a 1 kod yeterli
- ✅ Büyük batch operations (100-2000 farmer)
- ✅ Excel formatında veri mevcut
- ✅ Async processing kabul edilebilir
- ✅ Frontend Excel parse etmek istemiyor

---

## 3. Admin OBO: Bulk Code Distribution (Excel Upload)

### Doğru Kullanım

Admin sponsor adına kod dağıtımı yaparken **query parameter** kullanmalıdır:

```http
POST /api/Sponsorship/bulk-code-distribution?onBehalfOfSponsorId=6
Authorization: Bearer {admin_jwt_token}
Content-Type: multipart/form-data

FormData:
- excelFile: farmers.xlsx
- sendSms: true (optional)
```

### Endpoint Özellikleri

| Özellik | Detay |
|---------|-------|
| **Endpoint** | `/api/Sponsorship/bulk-code-distribution` |
| **Method** | POST |
| **Authorization** | `Sponsor` veya `Admin` |
| **Content-Type** | `multipart/form-data` |
| **OBO Parameter** | `onBehalfOfSponsorId` (query param, admin için required) |
| **Input** | Excel file + optional SMS preference |
| **Processing** | Asynchronous (RabbitMQ queue) |
| **Response** | Job ID + status tracking URL |

### Excel Format

Excel dosyası şu kolonları içermelidir:

| Column | Required | Description | Example |
|--------|----------|-------------|---------|
| Email | ✅ Yes | Farmer email | `ahmet@example.com` |
| Phone | ✅ Yes | Farmer phone | `05421396386` |
| Name | ❌ No | Farmer name | `Ahmet Yılmaz` |

**Constraints:**
- Max file size: **10 MB**
- Max rows: **2000 farmers**
- Phone format: Supports all Turkish formats (auto-normalized)

### cURL Example

```bash
curl -X POST "https://api.ziraai.com/api/Sponsorship/bulk-code-distribution?onBehalfOfSponsorId=6" \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN" \
  -F "excelFile=@farmers.xlsx" \
  -F "sendSms=true"
```

### JavaScript/Fetch Example

```javascript
async function adminBulkDistributeCodes(sponsorId, excelFile, sendSms = true) {
  const formData = new FormData();
  formData.append('excelFile', excelFile);
  formData.append('sendSms', sendSms.toString());

  const response = await fetch(
    `/api/Sponsorship/bulk-code-distribution?onBehalfOfSponsorId=${sponsorId}`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${getAdminToken()}`
      },
      body: formData
    }
  );

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  const result = await response.json();

  console.log(`Job ID: ${result.data.jobId}`);
  console.log(`Total Farmers: ${result.data.totalFarmers}`);
  console.log(`Status URL: ${result.data.statusCheckUrl}`);

  return result;
}

// Usage
const fileInput = document.querySelector('#excelFile');
await adminBulkDistributeCodes(6, fileInput.files[0], true);
```

### Response Format

```typescript
interface BulkCodeDistributionJobDto {
  jobId: number;
  totalFarmers: number;
  status: string;  // "Queued", "Processing", "Completed"
  createdDate: string;
  statusCheckUrl: string;
}
```

### Response Example

```json
{
  "success": true,
  "message": "Bulk code distribution job created successfully",
  "data": {
    "jobId": 42,
    "totalFarmers": 150,
    "status": "Queued",
    "createdDate": "2025-01-07T10:30:00Z",
    "statusCheckUrl": "/api/Sponsorship/bulk-distribution-status/42"
  }
}
```

### Status Tracking

Job status'u kontrol etmek için:

```http
GET /api/Sponsorship/bulk-distribution-status/42
Authorization: Bearer {admin_jwt_token}
```

Admin tüm job'ları görebilir, sponsor sadece kendi job'larını görebilir.

### Validation Rules

**Admin Kullanımı:**
- ✅ `onBehalfOfSponsorId` query parameter **zorunlu**
- ✅ Admin role required
- ✅ Target sponsor'un var olması kontrol edilmeli (TODO: backend validation eksik)

**Sponsor Kullanımı:**
- ❌ `onBehalfOfSponsorId` **kullanılmaz** (ignore edilir)
- ✅ Sponsor kendi ID'si ile işlem yapar
- ✅ Sponsor role yeterli

### Common Errors

| HTTP Status | Error Message | Neden | Çözüm |
|-------------|---------------|-------|--------|
| 400 | "Admin users must specify onBehalfOfSponsorId query parameter" | Admin `onBehalfOfSponsorId` göndermedi | Query param ekle |
| 400 | "Excel dosyası zorunludur" | File upload yok | Excel file ekle |
| 400 | "Insufficient codes available" | Sponsor'da yeterli kod yok | Sponsor'a kod satın aldır |
| 403 | Forbidden | Admin target sponsor'a erişemez | Admin permissions kontrol et |

---

## 4. OBO Endpoint Karşılaştırması

### 3 Endpoint'in Tam Karşılaştırması

| Feature | Admin JSON | Admin Excel ✨ | Code Distribution |
|---------|-----------|---------------|-------------------|
| **Endpoint** | `/admin/farmer/invitations/bulk` | `/admin/farmer/invitations/bulk/excel` | `/bulk-code-distribution` |
| **Purpose** | Farmer Invitation | Farmer Invitation | Code Distribution |
| **OBO Parameter** | `sponsorId` (body) | `onBehalfOfSponsorId` (query) | `onBehalfOfSponsorId` (query) |
| **Input Format** | JSON | Excel | Excel |
| **Authorization** | Admin only | Admin only | Sponsor or Admin |
| **Use Case** | Variable codes, immediate feedback | Bulk 1-code invitations | Distribute existing codes |
| **Code Source** | Auto-reserves | Auto-reserves | Uses existing codes |
| **Processing** | **Sync** | **Async** | Async |
| **Response** | Per-recipient results | Job ID + status URL | Job ID + status URL |
| **SMS/WhatsApp** | Always sends | Always sends | Optional (`sendSms`) |
| **Code Count** | Variable (1-100) | Fixed (1 per farmer) | Fixed (1 per farmer) |
| **Max Recipients** | No hard limit | 2000 rows | 2000 rows |

### Ne Zaman Hangisi Kullanılır?

**Admin Farmer Invitations - JSON:**
- ✅ Yeni farmer'lara davet gönderilecek
- ✅ Her farmer'a **farklı sayıda kod** verilecek (1-100)
- ✅ Tier-based filtering gerekiyor (S/M/L/XL)
- ✅ **Immediate per-recipient feedback** gerekli
- ✅ Az sayıda recipient (< 50)
- ✅ Admin müdahalesi/support senaryosu

**Admin Farmer Invitations - Excel:** ✨ **YENİ**
- ✅ Yeni farmer'lara davet gönderilecek
- ✅ Her farmer'a **1 kod** yeterli
- ✅ Büyük batch operations (100-2000 farmer)
- ✅ Excel formatında veri mevcut
- ✅ Async processing kabul edilebilir
- ✅ Sponsor'un kendi Excel endpoint'i ile aynı işlevsellik gerekli

**Code Distribution - Excel:**
- ✅ **Var olan farmer'lara** kod dağıtılacak (invitation değil)
- ✅ Existing codes assign edilecek
- ✅ Her farmer'a 1 kod yeterli
- ✅ Büyük batch operations (1000+ farmer)
- ✅ Excel ile toplu işlem yapılacak

---

## Sonuç

### ✅ Admin OBO Endpoint'leri (Doğru Kullanım)

**1. Farmer Invitations - JSON (Sync, Variable Codes):**
```
POST /api/Sponsorship/admin/farmer/invitations/bulk
Content-Type: application/json
Body: { "sponsorId": X, "recipients": [...] }
```

**2. Farmer Invitations - Excel (Async, 1 Code per Farmer):** ✨ **YENİ**
```
POST /api/Sponsorship/admin/farmer/invitations/bulk/excel?onBehalfOfSponsorId=X
Content-Type: multipart/form-data
FormData: { excelFile, channel, customMessage }
```

**3. Code Distribution - Excel (Assign Existing Codes):**
```
POST /api/Sponsorship/bulk-code-distribution?onBehalfOfSponsorId=X
Content-Type: multipart/form-data
FormData: { excelFile, sendSms }
```

### ❌ Yanlış Endpoint (KULLANMAYIN)

```
POST /api/Sponsorship/farmer/invitations/bulk?onBehalfOfSponsorId=X
Content-Type: multipart/form-data
```

**Neden Yanlış:**
- Bu endpoint sponsor'un **kendi daveti** için
- `onBehalfOfSponsorId` parametresi **tanımlı değil**
- Admin ID'si kullanılıyor, sponsor ID'si değil

### Frontend Aksiyonları

1. **Farmer Invitations (JSON):** Admin JSON endpoint kullan, per-recipient feedback al
2. **Farmer Invitations (Excel):** ✨ YENİ admin Excel endpoint kullan, job ID al
3. **Code Distribution:** Mevcut Excel endpoint'e `onBehalfOfSponsorId` query param ekle
4. Tüm endpoint'ler için admin authorization gerekli
5. Excel endpoint'leri için asynchronous job tracking implement et
6. Error handling ve status tracking implement et

### Önemli Notlar

- **Admin Excel endpoint** sponsor'un Excel endpoint'i ile **tamamen aynı işlevselliğe** sahiptir
- Tek fark: Admin `onBehalfOfSponsorId` parametresi ile hedef sponsor'u belirtir
- **Code Distribution ≠ Farmer Invitation**: Bunlar farklı use case'ler
- Frontend ekibi bu güncellemeleri uygulamalıdır
