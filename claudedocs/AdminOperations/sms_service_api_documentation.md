# SMS Service API Documentation

## Overview

Bu doküman, SMS servis entegrasyonu için eklenen admin endpoint'lerini açıklar. Bu endpoint'ler SMS provider konfigürasyonunu test etmek ve izlemek için kullanılır.

**Base URL:** `/api/admin/sms`
**Authentication:** JWT Bearer Token (Admin role required)
**Authorization Claims:** `sms.admin.test`, `sms.admin.provider-info`

---

## Endpoints

### 1. Get SMS Provider Info

SMS provider bilgisini ve durumunu görüntüler.

#### Request

```
GET /api/admin/sms/provider
```

**Headers:**
```
Authorization: Bearer <token>
Content-Type: application/json
```

**Parameters:** None

#### Response

**Success (200 OK):**
```json
{
  "success": true,
  "message": null,
  "data": {
    "provider": "Netgsm",
    "isConfigured": true,
    "senderId": "ZIRAAI",
    "balance": 1500.50,
    "currency": "TL",
    "monthlyQuota": null,
    "usedQuota": null,
    "isActive": true,
    "statusMessage": "Provider aktif ve çalışıyor",
    "retrievedAt": "2025-11-19T14:30:00.000Z"
  }
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `provider` | string | Aktif provider adı (Mock, Netgsm, Turkcell) |
| `isConfigured` | boolean | Credential'ların yapılandırılıp yapılandırılmadığı |
| `senderId` | string | Gönderici adı / Mesaj başlığı |
| `balance` | decimal? | Hesap bakiyesi (varsa) |
| `currency` | string | Para birimi (TL, USD, vb.) |
| `monthlyQuota` | int? | Aylık SMS kotası |
| `usedQuota` | int? | Kullanılan kota |
| `isActive` | boolean | Provider'ın aktif olup olmadığı |
| `statusMessage` | string | Durum mesajı veya hata |
| `retrievedAt` | datetime | Bilgi alınma zamanı |

**Error (500 Internal Server Error):**
```json
{
  "success": false,
  "message": "Provider bilgisi alınamadı: Connection timeout",
  "data": null
}
```

---

### 2. Test SMS

Provider konfigürasyonunu doğrulamak için test SMS'i gönderir.

#### Request

```
POST /api/admin/sms/test
```

**Headers:**
```
Authorization: Bearer <token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "phoneNumber": "05371234567",
  "message": "Test SMS from ZiraAI"
}
```

**Request Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `phoneNumber` | string | Yes | Hedef telefon numarası |
| `message` | string | No | Özel mesaj (opsiyonel - varsayılan test mesajı) |

**Desteklenen Telefon Formatları:**
- `05371234567` (0 ile başlayan)
- `5371234567` (0'sız)
- `905371234567` (ülke kodu ile)
- `+905371234567` (+ ile)

#### Response

**Success (200 OK):**
```json
{
  "success": true,
  "message": "Test SMS başarıyla gönderildi.",
  "data": {
    "success": true,
    "messageId": "123456789",
    "provider": "Netgsm",
    "phoneNumber": "05371234567",
    "message": "Test SMS from ZiraAI",
    "sentAt": "2025-11-19T14:30:00.000Z",
    "errorMessage": null,
    "balance": 1500.25,
    "currency": "TL"
  }
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | SMS'in başarıyla gönderilip gönderilmediği |
| `messageId` | string | Provider'dan dönen mesaj ID (takip için) |
| `provider` | string | Kullanılan provider |
| `phoneNumber` | string | Hedef telefon numarası |
| `message` | string | Gönderilen mesaj içeriği |
| `sentAt` | datetime | Gönderim zamanı |
| `errorMessage` | string | Hata mesajı (başarısız ise) |
| `balance` | decimal? | Hesap bakiyesi |
| `currency` | string | Para birimi |

**Error (400 Bad Request):**
```json
{
  "success": false,
  "message": "SMS gönderilemedi: Geçersiz kullanıcı adı/şifre veya API erişim izni yok (Kod: 30)",
  "data": {
    "success": false,
    "messageId": null,
    "provider": "Netgsm",
    "phoneNumber": "05371234567",
    "message": "Test SMS from ZiraAI",
    "sentAt": "2025-11-19T14:30:00.000Z",
    "errorMessage": "Geçersiz kullanıcı adı/şifre veya API erişim izni yok. IP kısıtlaması olabilir."
  }
}
```

---

## Error Codes

### NetGSM Hata Kodları

| Code | Description |
|------|-------------|
| `00` | Başarılı gönderim |
| `20` | Mesaj metninde hata veya karakter limiti aşıldı |
| `30` | Geçersiz kullanıcı/şifre veya API erişim izni yok |
| `40` | Mesaj başlığı (sender ID) sistemde tanımlı değil |
| `50` | Yetersiz bakiye |
| `51` | Aynı mesaj aynı numaraya 24 saat içinde tekrar gönderilemez |
| `70` | Hatalı parametre |
| `80` | Gönderim zaman aşımı |
| `85` | Yinelenen gönderim engellendi |

---

## Configuration

### Environment Variables (Production)

Railway'de aşağıdaki environment variable'ları ayarlayın:

```bash
NETGSM_USERCODE=kullanici_adi
NETGSM_PASSWORD=sifre
NETGSM_MSGHEADER=ZIRAAI
NETGSM_API_URL=https://api.netgsm.com.tr  # Optional
```

### appsettings.json (Development)

```json
{
  "SmsService": {
    "Provider": "Netgsm"  // Mock, Netgsm, Turkcell
  },
  "SmsProvider": {
    "Netgsm": {
      "ApiUrl": "https://api.netgsm.com.tr",
      "UserCode": "",
      "Password": "",
      "MsgHeader": "ZIRAAI"
    }
  }
}
```

### Provider Seçimi

| Environment | Provider | Description |
|-------------|----------|-------------|
| Development | Mock | Konsola log, gerçek SMS gönderilmez |
| Staging | Mock/Netgsm | Test amaçlı |
| Production | Netgsm | Gerçek SMS gönderimi |

---

## Usage Examples

### cURL

**Get Provider Info:**
```bash
curl -X GET "https://api.ziraai.com/api/admin/sms/provider" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json"
```

**Test SMS:**
```bash
curl -X POST "https://api.ziraai.com/api/admin/sms/test" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "05371234567",
    "message": "Test mesajı"
  }'
```

### JavaScript/TypeScript

```typescript
// Get Provider Info
const getProviderInfo = async () => {
  const response = await fetch('/api/admin/sms/provider', {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  return response.json();
};

// Test SMS
const testSms = async (phoneNumber: string, message?: string) => {
  const response = await fetch('/api/admin/sms/test', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      phoneNumber,
      message
    })
  });
  return response.json();
};
```

### Flutter/Dart

```dart
// Get Provider Info
Future<Map<String, dynamic>> getProviderInfo() async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/admin/sms/provider'),
    headers: {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    },
  );
  return jsonDecode(response.body);
}

// Test SMS
Future<Map<String, dynamic>> testSms(String phoneNumber, {String? message}) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/admin/sms/test'),
    headers: {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'phoneNumber': phoneNumber,
      if (message != null) 'message': message,
    }),
  );
  return jsonDecode(response.body);
}
```

---

## Integration Notes

### Mobile/Frontend Teams

1. Bu endpoint'ler yalnızca Admin panel içindir
2. Farmer veya Sponsor kullanıcıları bu endpoint'lere erişemez
3. SMS gönderimi mevcut feature'larda (referral, OTP, sponsorship) otomatik olarak gerçekleşir
4. Bu endpoint'ler konfigürasyon doğrulaması ve izleme içindir

### Backend Integration

Mevcut SMS kullanan servisler değişiklik gerektirmez:
- `ReferralLinkService` - Referral SMS gönderimi
- `SendSponsorshipLinkCommand` - Sponsor kod dağıtımı
- `InviteDealerViaSmsCommand` - Bayi davet SMS'i
- OTP authentication - Doğrulama kodları

Tüm bu servisler `IMessagingServiceFactory.GetSmsService()` üzerinden konfigüre edilmiş provider'ı kullanır.

---

## Changelog

### v1.0.0 (2025-11-19)
- Initial SMS service integration
- NetGSM provider implementation
- Admin test endpoints
- Environment-based configuration
