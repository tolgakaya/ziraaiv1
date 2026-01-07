# Admin On-Behalf-Of (OBO) Endpoint Kullanım Kılavuzu

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

## Sonuç

✅ **Admin OBO için doğru endpoint:**
```
POST /api/Sponsorship/admin/farmer/invitations/bulk
Content-Type: application/json
Body: { "sponsorId": X, "recipients": [...] }
```

❌ **Yanlış endpoint (kullanmayın):**
```
POST /api/Sponsorship/farmer/invitations/bulk?onBehalfOfSponsorId=X
Content-Type: multipart/form-data
```

Frontend ekibi bu endpoint değişikliğini uygulamalıdır.
