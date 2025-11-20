# Mobile Integration Guide: Dealer Invitations with SignalR

## Overview

Bu doküman, dealer invitation sisteminin mobil uygulamaya entegrasyonu için gerekli tüm teknik detayları içerir. Sistem iki ana özellik sunar:

1. **REST API**: Kullanıcının bekleyen davetiyelerini sorgulama
2. **SignalR Hub**: Yeni davetiye oluşturulduğunda gerçek zamanlı bildirim alma

## 🔐 Authentication

Her iki özellik için de JWT Bearer token authentication gereklidir.

```
Authorization: Bearer {your_jwt_token}
```

Token'da şu claim'ler bulunmalıdır:
- `ClaimTypes.NameIdentifier` (userId)
- `ClaimTypes.Email` (kullanıcı email'i)
- `ClaimTypes.MobilePhone` (kullanıcı telefonu)

---

## 1️⃣ REST API: Get Pending Invitations

### Endpoint

```
GET /api/v1/dealer/invitations/my-pending
```

### Headers

```
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

### Authorization

**Required Roles:** `Dealer`, `Farmer`, veya `Sponsor`

### Query Parameters

Yok. Kullanıcı bilgileri JWT token'dan otomatik olarak çıkarılır.

### Request Example

```http
GET https://ziraai-api-sit.up.railway.app/api/v1/dealer/invitations/my-pending HTTP/1.1
Authorization: Bearer eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
Content-Type: application/json
```

### Success Response (200 OK)

```json
{
  "data": {
    "invitations": [
      {
        "invitationId": 123,
        "token": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
        "sponsorCompanyName": "Agro Tech Ltd",
        "codeCount": 50,
        "packageTier": "M",
        "expiresAt": "2025-11-06T14:30:00",
        "remainingDays": 5,
        "status": "Pending",
        "dealerEmail": "dealer@example.com",
        "dealerPhone": "+905551234567",
        "createdAt": "2025-10-30T10:15:00"
      },
      {
        "invitationId": 124,
        "token": "p6o5n4m3l2k1j0i9h8g7f6e5d4c3b2a1",
        "sponsorCompanyName": "Green Farm Solutions",
        "codeCount": 100,
        "packageTier": "L",
        "expiresAt": "2025-11-01T09:00:00",
        "remainingDays": 1,
        "status": "Pending",
        "dealerEmail": "dealer@example.com",
        "dealerPhone": "+905551234567",
        "createdAt": "2025-10-25T15:45:00"
      }
    ],
    "totalCount": 2
  },
  "success": true,
  "message": "Bekleyen davetiyeler başarıyla getirildi"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `invitationId` | int | Davetiye ID'si |
| `token` | string | 32 karakterli davetiye token'ı (DEALER-{token} formatında deep link için kullanılır) |
| `sponsorCompanyName` | string | Davet eden sponsorun şirket adı |
| `codeCount` | int | Transfer edilecek kod sayısı |
| `packageTier` | string | Paket tier'ı: S, M, L, XL (null olabilir) |
| `expiresAt` | DateTime | Davetiyenin son geçerlilik tarihi |
| `remainingDays` | int | Kalan gün sayısı (negatif olabilir - süresi geçmiş) |
| `status` | string | Davetiye durumu: "Pending", "Accepted", "Expired", "Cancelled" |
| `dealerEmail` | string | Davetiye gönderilen email |
| `dealerPhone` | string | Davetiye gönderilen telefon |
| `createdAt` | DateTime | Davetiye oluşturulma tarihi |

### Error Responses

#### 400 Bad Request - Email/Phone Bulunamadı

```json
{
  "data": null,
  "success": false,
  "message": "Email veya telefon bilgisi bulunamadı"
}
```

**Neden:** JWT token'da ne email ne de telefon claim'i var.

#### 401 Unauthorized

```json
{
  "message": "Unauthorized"
}
```

**Neden:** Token geçersiz, eksik veya süresi dolmuş.

#### 403 Forbidden

```json
{
  "message": "Forbidden"
}
```

**Neden:** Kullanıcının rolü `Dealer`, `Farmer` veya `Sponsor` değil.

#### 500 Internal Server Error

```json
{
  "data": null,
  "success": false,
  "message": "Bekleyen davetiyeler alınırken hata oluştu"
}
```

### Önemli Notlar

1. **Filtreleme Mantığı**: API, JWT token'daki email VE/VEYA telefon ile eşleşen davetiyeleri döner
2. **Sıralama**: Davetiyeler son geçerlilik tarihine göre sıralanır (en yakın süre dolacak olanlar önce)
3. **Otomatik Filtreleme**: Sadece `Status="Pending"` ve `ExpiryDate > Now` olan davetiyeler döner
4. **Boş Liste**: Eğer bekleyen davetiye yoksa `invitations: []` ve `totalCount: 0` döner

---

## 2️⃣ SignalR Hub: Real-time Notifications

### Hub URL

```
wss://ziraai-api-sit.up.railway.app/hubs/notification
```

**Production:**
```
wss://ziraai.com/hubs/notification
```

### Connection Setup

#### Flutter/Dart Example

```dart
import 'package:signalr_netcore/signalr_client.dart';

class DealerNotificationService {
  HubConnection? _hubConnection;
  String _jwtToken;

  DealerNotificationService(this._jwtToken);

  Future<void> connect() async {
    // SignalR connection setup
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          "https://ziraai-api-sit.up.railway.app/hubs/notification",
          options: HttpConnectionOptions(
            accessTokenFactory: () async => _jwtToken,
            logging: (level, message) => print('SignalR: $message'),
          ),
        )
        .withAutomaticReconnect()
        .build();

    // Listen for new dealer invitations
    _hubConnection!.on("NewDealerInvitation", _handleNewInvitation);

    // Connection state handlers
    _hubConnection!.onclose((error) {
      print('SignalR connection closed: $error');
    });

    _hubConnection!.onreconnecting((error) {
      print('SignalR reconnecting: $error');
    });

    _hubConnection!.onreconnected((connectionId) {
      print('SignalR reconnected: $connectionId');
    });

    // Start connection
    await _hubConnection!.start();
    print('SignalR connected');
  }

  void _handleNewInvitation(List<Object>? arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    print('📩 New dealer invitation received:');
    print('Invitation ID: ${data['invitationId']}');
    print('Sponsor: ${data['sponsorCompanyName']}');
    print('Code Count: ${data['codeCount']}');
    print('Expires: ${data['expiresAt']}');

    // Show local notification
    _showNotification(data);

    // Update UI or trigger data refresh
    _refreshInvitations();
  }

  void _showNotification(Map<String, dynamic> data) {
    // Implement your local notification logic
    // Example: flutter_local_notifications
  }

  void _refreshInvitations() {
    // Trigger UI update or API call to refresh invitation list
  }

  Future<void> disconnect() async {
    await _hubConnection?.stop();
  }

  Future<void> ping() async {
    // Optional: Keep connection alive
    await _hubConnection?.invoke("Ping");
  }
}
```

#### React Native Example

```javascript
import * as signalR from '@microsoft/signalr';

class DealerNotificationService {
  constructor(jwtToken) {
    this.jwtToken = jwtToken;
    this.connection = null;
  }

  async connect() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://ziraai-api-sit.up.railway.app/hubs/notification', {
        accessTokenFactory: () => this.jwtToken,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Listen for new dealer invitations
    this.connection.on('NewDealerInvitation', (data) => {
      console.log('📩 New dealer invitation:', data);
      this.handleNewInvitation(data);
    });

    // Connection handlers
    this.connection.onclose((error) => {
      console.error('SignalR connection closed:', error);
    });

    this.connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting:', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
    });

    // Start connection
    await this.connection.start();
    console.log('SignalR connected');
  }

  handleNewInvitation(data) {
    const {
      invitationId,
      token,
      sponsorCompanyName,
      codeCount,
      packageTier,
      expiresAt,
      remainingDays,
      status,
      dealerEmail,
      dealerPhone,
      createdAt
    } = data;

    // Show push notification
    this.showPushNotification({
      title: 'New Dealer Invitation',
      message: `${sponsorCompanyName} sent you an invitation with ${codeCount} codes`,
      data: { invitationId, token }
    });

    // Update app state or trigger data refresh
    this.refreshInvitations();
  }

  showPushNotification(notification) {
    // Implement your push notification logic
    // Example: react-native-push-notification
  }

  refreshInvitations() {
    // Trigger UI update or API call
  }

  async disconnect() {
    await this.connection?.stop();
  }

  async ping() {
    // Optional: Keep connection alive
    await this.connection?.invoke('Ping');
  }
}

// Usage
const jwtToken = 'your_jwt_token_here';
const notificationService = new DealerNotificationService(jwtToken);
await notificationService.connect();
```

### SignalR Event: `NewDealerInvitation`

Bu event, yeni bir dealer davetiyesi oluşturulduğunda otomatik olarak tetiklenir.

#### Event Payload

```json
{
  "invitationId": 125,
  "token": "x9y8z7w6v5u4t3s2r1q0p9o8n7m6l5k4",
  "sponsorCompanyName": "Mega Agro Corp",
  "codeCount": 75,
  "packageTier": "L",
  "expiresAt": "2025-11-07T16:00:00",
  "remainingDays": 7,
  "status": "Pending",
  "dealerEmail": "newdealer@example.com",
  "dealerPhone": "+905559876543",
  "createdAt": "2025-10-30T16:00:00"
}
```

### Connection Groups

SignalR hub, kullanıcıları email ve telefon bilgilerine göre gruplara ekler:

- **Email Group:** `email_{email}` (örn: `email_dealer@example.com`)
- **Phone Group:** `phone_{normalizedPhone}` (örn: `phone_905551234567`)

**Phone Normalization:**
- Boşluk, tire, parantez ve + işaretleri kaldırılır
- `+905551234567` → `905551234567`
- `0555 123 4567` → `905551234567` (başında 90 yoksa eklenir)

### Hub Methods

#### `Ping()`

Connection sağlığını test etmek için kullanılır.

```dart
await _hubConnection.invoke("Ping");
```

Yanıt gelmez, sadece connection'ın aktif olduğunu doğrular.

### Connection Lifecycle

1. **OnConnectedAsync**: Kullanıcı bağlandığında otomatik olarak email ve phone gruplarına eklenir
2. **OnDisconnectedAsync**: Kullanıcı bağlantıyı kestiğinde gruplardan çıkarılır
3. **Automatic Reconnect**: SignalR kütüphanesi bağlantı koptuğunda otomatik yeniden bağlanır

### Error Handling

```dart
_hubConnection!.onclose((error) {
  if (error != null) {
    print('Connection closed with error: $error');
    // Implement retry logic or show error to user
  } else {
    print('Connection closed gracefully');
  }
});
```

---

## 📱 Integration Flow

### Uygulama Başlangıcında

```dart
1. User login → JWT token al
2. SignalR connection başlat
3. REST API ile mevcut pending invitations'ları çek
4. UI'da listeyi göster
```

### Yeni Davetiye Geldiğinde

```dart
1. SignalR "NewDealerInvitation" eventi gelir
2. Local notification göster
3. REST API'yi yeniden çağır veya listeye manuel ekle
4. UI'ı güncelle
```

### Best Practices

1. **Background Connection**: SignalR connection'ı app background'a gittiğinde kapat, foreground'a döndüğünde aç
2. **Token Refresh**: JWT token yenilendiğinde SignalR connection'ı yeniden başlat
3. **Offline Handling**: Connection koptuğunda REST API ile poll yaparak veri senkronizasyonunu sağla
4. **Duplicate Prevention**: Aynı `invitationId` için notification'ı birden fazla gösterme

---

## 🧪 Testing

### Postman ile REST API Test

```bash
# Request
GET https://ziraai-api-sit.up.railway.app/api/v1/dealer/invitations/my-pending
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
  x-dev-arch-version: 1.0
  Content-Type: application/json
```

### SignalR Test (Browser Console)

```javascript
// 1. Include SignalR library
<script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>

// 2. Test connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://ziraai-api-sit.up.railway.app/hubs/notification", {
    accessTokenFactory: () => "YOUR_JWT_TOKEN"
  })
  .build();

connection.on("NewDealerInvitation", (data) => {
  console.log("New invitation:", data);
});

connection.start().then(() => {
  console.log("Connected!");
});

// 3. Test ping
connection.invoke("Ping");
```

### Test Senaryo

1. **User A** ile login yap (email: `dealer@test.com`)
2. SignalR connection başlat
3. **Sponsor** user olarak `/api/v1/sponsorship/dealer/invite-via-sms` endpoint'ini çağır (User A'nın email'ini kullanarak)
4. **User A** client'ında `NewDealerInvitation` eventi geldiğini doğrula
5. REST API ile pending invitations'ları çek ve yeni davetiyenin geldiğini doğrula

---

## 🔧 Troubleshooting

### SignalR Connection Başarısız

**Problem:** `401 Unauthorized` hatası

**Çözüm:**
- JWT token'ın geçerli olduğunu doğrula
- Token'ın `Authorization` header'ında değil, `accessTokenFactory` ile gönderildiğini kontrol et
- Token'ın `ClaimTypes.Email` veya `ClaimTypes.MobilePhone` claim'ine sahip olduğunu doğrula

### Notification Gelmiyor

**Problem:** Davetiye oluşturuldu ama SignalR event gelmedi

**Troubleshooting:**
1. SignalR connection'ın aktif olduğunu doğrula
2. Backend log'larında `📣 Sending SignalR notification` log'unu ara
3. User'ın email/phone bilgisinin davetiye ile eşleştiğini kontrol et
4. Phone normalization'ın doğru çalıştığını kontrol et (+90 vs 0 farkı)

### REST API Boş Döndü

**Problem:** `/my-pending` endpoint boş liste döndürüyor

**Kontrol:**
1. JWT token'da email veya phone claim'i var mı?
2. Davetiye `Status="Pending"` ve `ExpiryDate > Now` koşullarını sağlıyor mu?
3. Davetiye email/phone bilgisi JWT'deki ile eşleşiyor mu?

---

## 📊 Example Integration Timeline

```
App Launch
  └─> Login (JWT token al)
      └─> SignalR Connect
          └─> REST API: Get Pending Invitations
              └─> UI: Show invitation list

Background
  └─> SignalR: NewDealerInvitation event
      └─> Show push notification
          └─> User taps notification
              └─> App foreground
                  └─> REST API: Refresh invitations
                      └─> UI: Update list
                          └─> User accepts invitation
                              └─> Navigate to acceptance flow
```

---

## 📚 References

- [SignalR for Flutter](https://pub.dev/packages/signalr_netcore)
- [SignalR for React Native](https://www.npmjs.com/package/@microsoft/signalr)
- [ASP.NET Core SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr)

---

## ⚠️ Important Notes

1. **Security**: JWT token'ı güvenli şekilde sakla (secure storage, keychain)
2. **Performance**: SignalR connection'ı sadece app aktifken açık tut
3. **Data Sync**: SignalR bağlantı problemi yaşanırsa REST API ile fallback yap
4. **Testing**: Staging environment'ta test et (`ziraai-api-sit.up.railway.app`)
5. **Production**: Production'a geçmeden önce URL'leri güncelle

---

**Son Güncelleme:** 30 Ekim 2025
**API Version:** 1.0
**Backend Commit:** d1f21f5
