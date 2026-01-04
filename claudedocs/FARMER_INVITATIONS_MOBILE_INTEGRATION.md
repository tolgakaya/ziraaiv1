# Farmer Invitations - Mobile Integration Guide

**Last Updated**: 2026-01-03
**API Version**: 1.0
**Target Audience**: Mobile (iOS/Android) Development Team

---

## 📋 Table of Contents

1. [Overview & Comparison](#overview--comparison)
2. [Deep Link Configuration](#deep-link-configuration)
3. [API Endpoints Reference](#api-endpoints-reference)
4. [Request/Response Structures](#requestresponse-structures)
5. [Authentication Flow](#authentication-flow)
6. [User Journeys](#user-journeys)
7. [Error Handling](#error-handling)
8. [Testing Guide](#testing-guide)

---

## 🎯 Overview & Comparison

### What is Farmer Invitations?

Farmer Invitations, **sponsorların çiftçilere token-based davetiye göndererek sponsorluk kodlarını transfer etmesini** sağlar. Google Play SDK 35+ uyumluluğu için **SMS listener yerine deep link** kullanır.

### 🔄 Send Link vs Farmer Invitation: What Changed?

#### ❌ OLD SYSTEM (Still Active)

**Tekli Gönderim:**
```
Endpoint: POST /api/v1/sponsorship/send-link

Sponsor Action:
1. "Send Code" screen → Enter phone: 05551234567
2. Backend sends REAL CODE via SMS
   SMS: "Your code: SPONSOR-ABC-123"

Farmer Action:
3. Receives SMS with code
4. Opens app → "Redeem Code" screen
5. Types/pastes code: SPONSOR-ABC-123
6. POST /api/v1/sponsorship/redeem
   └─> 1 code redeemed
```

**Bulk Gönderim:**
```
Endpoint: POST /api/v1/sponsorship/bulk-code-distribution

Sponsor Action:
1. "Bulk Send" screen → Upload Excel (multiple farmers)
2. Backend sends REAL CODES to each farmer via SMS
   SMS: "Your code: SPONSOR-ABC-456"

Farmer Action:
3. Same as above (manual redeem for each code)
```

**Problems:**
- ❌ SMS listener doesn't work on SDK 35+
- ❌ Manual copy-paste for EACH code (bad UX)
- ❌ SMS can be lost or deleted
- ❌ 1 SMS = 1 code only

---

#### ✅ NEW SYSTEM (Recommended)

**Tekli Davetiye:**
```
Endpoint: POST /api/v1/sponsorship/farmer/invite

Sponsor Action:
1. "Send Invitation" screen
   └─> Phone: 05551234567
   └─> Code Count: 50 codes
2. Backend sends DEEP LINK via SMS
   SMS: "Agro Tech sent you 50 codes!
         https://ziraai.com/farmer-invite/abc123..."

Farmer Action:
3. Taps link → App opens automatically
4. Shows: "Agro Tech - 50 codes"
5. Taps "Accept"
6. POST /api/v1/sponsorship/farmer/accept-invitation
   └─> 50 codes assigned automatically!
```

**Bulk Davetiye:**
```
Endpoint: POST /api/v1/sponsorship/farmer/invitations/bulk

Sponsor Action:
1. "Bulk Invitations" screen → Upload Excel
2. Backend sends DEEP LINKS to each farmer
   Each SMS: "Sponsor X sent you Y codes! [link]"

Farmer Action:
3. Same as above (tap link → accept)
```

**Benefits:**
- ✅ No SMS listener needed (uses deep links)
- ✅ Google Play SDK 35+ compatible
- ✅ 1 tap = 50+ codes (bulk transfer!)
- ✅ Cross-device support
- ✅ No manual code entry
- ✅ Better tracking (status, expiry)

---

### System Comparison Table

| Feature | Old (Send Link) | Old (Bulk Distribution) | New (Farmer Invitation) | New (Bulk Invitation) |
|---------|-----------------|-------------------------|-------------------------|-----------------------|
| **Endpoint** | `/send-link` | `/bulk-code-distribution` | `/farmer/invite` | `/farmer/invitations/bulk` |
| **What sent** | 1 real code | N real codes | 1 deep link (N codes) | M deep links (N codes each) |
| **SMS content** | "Code: ABC-123" | "Code: ABC-456" (each farmer) | "50 codes! [link]" | "Y codes! [link]" (each) |
| **Farmer action** | Copy → Redeem | Copy → Redeem (repeat) | Tap → Accept | Tap → Accept |
| **SDK 35+ compatible** | ❌ No | ❌ No | ✅ Yes | ✅ Yes |
| **Codes per operation** | 1 | 1 per farmer | 1-1000 per farmer | Variable per farmer |
| **Status** | Active (legacy) | Active (legacy) | **Recommended** | **Recommended** |

---

### When to Use Which?

#### Old System (Not Recommended):
- ❌ Only for backward compatibility
- Legacy sponsors still using old mobile UI

#### New System (Recommended):
- ✅ **All new implementations**
- SDK 35+ requirement
- Better UX (single tap)
- Bulk operations (1-1000 codes per farmer)

### Why Deep Links Instead of SMS Listener?

| Aspect | Old (SMS Listener) | New (Deep Link) |
|--------|-------------------|-----------------|
| **Google Play SDK** | <35 (Deprecated) | ≥35 (Required) |
| **User Experience** | Auto-detect SMS code | User taps link |
| **Reliability** | SMS parsing issues | Direct deep link |
| **Cross-Device** | Single device only | Works on any device |
| **Privacy** | Requires SMS permission | No special permissions |

### Dealer vs Farmer Invitations (Mobile Perspective)

| Feature | Dealer Invitations | Farmer Invitations |
|---------|-------------------|-------------------|
| **Deep Link Format** | `DEALER-{token}` | `https://ziraai.com/farmer-invite/{token}` |
| **Target Screen** | Dealer acceptance flow | Farmer acceptance flow |
| **Pre-Login View** | ❌ No | ✅ Yes (`/invitation-details`) |
| **My Invitations** | `/dealer/invitations/my-pending` | `/farmer/my-invitations` |
| **Acceptance** | `/dealer/invitations/accept` | `/farmer/accept-invitation` |
| **SignalR Notifications** | ✅ Yes | ❌ Not Yet |
| **Role Required** | Dealer | Farmer (any authenticated user can accept) |

---

## 🔗 Deep Link Configuration

### Deep Link URL Format

```
https://ziraai.com/farmer-invite/{32-char-hex-token}
```

**Example**:
```
https://ziraai.com/farmer-invite/a1b2c3d4e5f6789012345678901234ab
```

### SMS Message Format

Çiftçi şu SMS'i alır:

```
Agro Tech Ltd size 50 adet sponsorluk kodu hediye etti!
Kabul etmek için: https://ziraai.com/farmer-invite/a1b2c3d4...
Geçerlilik: 7 gün
```

### iOS Configuration (Info.plist)

```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>ziraai</string>
    </array>
    <key>CFBundleURLName</key>
    <string>com.ziraai.app</string>
  </dict>
</array>

<!-- Universal Links -->
<key>com.apple.developer.associated-domains</key>
<array>
  <string>applinks:ziraai.com</string>
</array>
```

### Android Configuration (AndroidManifest.xml)

```xml
<activity android:name=".MainActivity">
  <!-- Deep Link Intent Filter -->
  <intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />

    <!-- HTTP/HTTPS Deep Link -->
    <data android:scheme="https"
          android:host="ziraai.com"
          android:pathPrefix="/farmer-invite/" />

    <!-- Custom Scheme -->
    <data android:scheme="ziraai"
          android:host="farmer-invite" />
  </intent-filter>
</activity>
```

### Flutter Deep Link Handling

```dart
import 'package:uni_links/uni_links.dart';

class DeepLinkService {
  StreamSubscription? _sub;

  void initDeepLinks() {
    // Handle app launch via deep link
    getInitialUri().then((Uri? uri) {
      if (uri != null) {
        _handleDeepLink(uri);
      }
    });

    // Handle deep link while app is running
    _sub = uriLinkStream.listen((Uri? uri) {
      if (uri != null) {
        _handleDeepLink(uri);
      }
    });
  }

  void _handleDeepLink(Uri uri) {
    // Example: https://ziraai.com/farmer-invite/abc123...
    if (uri.pathSegments.length >= 2 && uri.pathSegments[0] == 'farmer-invite') {
      final token = uri.pathSegments[1];
      _navigateToInvitation(token);
    }
  }

  void _navigateToInvitation(String token) {
    // Navigate to invitation details screen
    Navigator.pushNamed(
      context,
      '/farmer-invitation',
      arguments: {'token': token},
    );
  }

  void dispose() {
    _sub?.cancel();
  }
}
```

### React Native Deep Link Handling

```javascript
import { Linking } from 'react-native';

class DeepLinkService {
  constructor() {
    this.init();
  }

  init() {
    // Handle app launch via deep link
    Linking.getInitialURL().then((url) => {
      if (url) {
        this.handleDeepLink(url);
      }
    });

    // Handle deep link while app is running
    Linking.addEventListener('url', (event) => {
      this.handleDeepLink(event.url);
    });
  }

  handleDeepLink(url) {
    // Example: https://ziraai.com/farmer-invite/abc123...
    const match = url.match(/farmer-invite\/([a-f0-9]{32})/);
    if (match) {
      const token = match[1];
      this.navigateToInvitation(token);
    }
  }

  navigateToInvitation(token) {
    // Navigate to invitation details screen
    navigation.navigate('FarmerInvitation', { token });
  }
}
```

---

## 📡 API Endpoints Reference

### 1. Get Invitation Details (Pre-Login)

Kullanıcı deep link'e tıkladığında, **login yapmadan** davetiye detaylarını görüntüler.

#### Endpoint
```
GET /api/v1/sponsorship/farmer/invitation-details?token={token}
```

#### Authorization
**NONE** - Public endpoint (`[AllowAnonymous]`)

#### Request Example
```http
GET https://ziraai.com/api/v1/sponsorship/farmer/invitation-details?token=a1b2c3d4e5f6789012345678901234ab
x-dev-arch-version: 1.0
```

**Not**: JWT token **GEREKMİYOR**

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 45,
    "sponsorCompanyName": "Agro Tech Ltd",
    "codeCount": 50,
    "packageTier": "M",
    "expiryDate": "2026-01-10T10:00:00Z",
    "status": "Pending",
    "canAccept": true,
    "phone": "0555***4567",
    "farmerName": "Ahmet Yılmaz"
  },
  "success": true,
  "message": "Invitation details retrieved successfully"
}
```

#### Response Fields

| Field | Type | Description | Mobile Display |
|-------|------|-------------|----------------|
| `invitationId` | int | Davetiye ID | Hidden (internal use) |
| `sponsorCompanyName` | string | Sponsor şirket adı | **"Agro Tech Ltd"** (header) |
| `codeCount` | int | Kod sayısı | **"50 adet sponsorluk kodu"** |
| `packageTier` | string | Tier (S/M/L/XL) | **"Orta Paket"** badge |
| `expiryDate` | DateTime (ISO 8601) | Geçerlilik süresi | **"7 gün içinde geçerli"** |
| `status` | string | Durum | Hidden (use `canAccept`) |
| `canAccept` | bool | Kabul edilebilir mi? | Enable/disable "Kabul Et" button |
| `phone` | string | Hedef telefon (masked) | **"0555***4567"** (info) |
| `farmerName` | string | Hedef isim | **"Ahmet Yılmaz"** (info) |

#### UI Design Suggestions

```
┌─────────────────────────────────┐
│  🎁 Sponsorluk Davetiyesi       │
├─────────────────────────────────┤
│                                 │
│  Agro Tech Ltd                  │
│  size davetiye gönderdi         │
│                                 │
│  📦 50 adet sponsorluk kodu     │
│  🏷️ Orta Paket (M)              │
│                                 │
│  ⏰ 7 gün içinde geçerli        │
│  📱 Hedef: 0555***4567          │
│                                 │
│  ┌───────────────────────────┐ │
│  │   Giriş Yap ve Kabul Et   │ │
│  └───────────────────────────┘ │
│                                 │
│  ⓘ Daveti kabul etmek için     │
│    önce giriş yapmalısınız     │
└─────────────────────────────────┘
```

#### Error Responses

**400 Bad Request - Invalid Token**
```json
{
  "data": null,
  "success": false,
  "message": "Invitation not found or expired"
}
```

**Mobile Action**: Show error screen with "Davetiye bulunamadı veya süresi dolmuş" message.

---

### 2. Get My Pending Invitations (Farmer)

Login yapmış farmer kullanıcısı kendisine gelen **bekleyen** davetiyeleri listeler.

#### Endpoint
```
GET /api/v1/sponsorship/farmer/my-invitations
```

#### Authorization
**Required**: `Farmer`, `Admin` roles

**Headers**:
```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

#### Request Example
```http
GET https://ziraai.com/api/v1/sponsorship/farmer/my-invitations
Authorization: Bearer eyJhbGc...
x-dev-arch-version: 1.0
```

#### Success Response (200 OK)

```json
{
  "data": [
    {
      "id": 45,
      "phone": "05551234567",
      "farmerName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "status": "Pending",
      "codeCount": 50,
      "packageTier": "M",
      "acceptedByUserId": null,
      "acceptedDate": null,
      "createdDate": "2026-01-03T10:00:00Z",
      "expiryDate": "2026-01-10T10:00:00Z",
      "linkDelivered": true,
      "linkSentDate": "2026-01-03T10:00:05Z",
      "linkSentVia": "SMS"
    },
    {
      "id": 46,
      "phone": "05551234567",
      "farmerName": "Ahmet Yılmaz",
      "email": null,
      "status": "Pending",
      "codeCount": 30,
      "packageTier": "L",
      "acceptedByUserId": null,
      "acceptedDate": null,
      "createdDate": "2026-01-02T15:00:00Z",
      "expiryDate": "2026-01-09T15:00:00Z",
      "linkDelivered": true,
      "linkSentDate": "2026-01-02T15:00:10Z",
      "linkSentVia": "WhatsApp"
    }
  ],
  "success": true,
  "message": "2 pending invitation(s) found"
}
```

#### Response Fields

| Field | Type | Description | Mobile Use |
|-------|------|-------------|------------|
| `id` | int | Davetiye ID | Accept endpoint için |
| `phone` | string | Çiftçi telefonu | Info display |
| `farmerName` | string | Çiftçi adı | Info display |
| `email` | string | Email (nullable) | Info display |
| `status` | string | Always "Pending" | Badge display |
| `codeCount` | int | Kod sayısı | **"50 kod"** |
| `packageTier` | string | Tier (nullable) | **"Orta Paket"** badge |
| `acceptedByUserId` | int | Always null (pending) | Hidden |
| `acceptedDate` | DateTime | Always null (pending) | Hidden |
| `createdDate` | DateTime | Oluşturulma tarihi | **"3 Ocak 2026"** |
| `expiryDate` | DateTime | Geçerlilik süresi | **"7 gün kaldı"** countdown |
| `linkDelivered` | bool | SMS gönderildi mi? | SMS status icon |
| `linkSentDate` | DateTime | SMS gönderim zamanı | Info |
| `linkSentVia` | string | "SMS" / "WhatsApp" | Channel icon |

#### UI Design Suggestions (List Item)

```
┌─────────────────────────────────────┐
│ 🏢 Agro Tech Ltd                    │
│ 📦 50 kod · Orta Paket              │
│ ⏰ 7 gün kaldı                       │
│ 📅 3 Ocak 2026 · 📱 SMS             │
│                                     │
│ ┌─────────────┐ ┌───────────────┐ │
│ │  Detaylar   │ │   Kabul Et    │ │
│ └─────────────┘ └───────────────┘ │
└─────────────────────────────────────┘
```

#### Empty State

```json
{
  "data": [],
  "success": true,
  "message": "No pending invitation(s) found"
}
```

**Mobile UI**: Show empty state with "Henüz bekleyen davetiyeniz yok" message.

#### Business Logic

1. **Automatic Filtering**: Backend otomatik olarak:
   - `Status = "Pending"`
   - `ExpiryDate > DateTime.Now`
   - User phone eşleşmesi (JWT'den)
2. **Phone Normalization**: Backend Turkish format handle eder
   - `+905551234567` → `05551234567`
   - `905551234567` → `05551234567`
3. **Sorting**: `CreatedDate DESC` (en yeni önce)

---

### 3. Accept Farmer Invitation

Farmer davetiyeyi kabul eder, kodlar transfer edilir.

#### Endpoint
```
POST /api/v1/sponsorship/farmer/accept-invitation
```

#### Authorization
**Required**: Any authenticated user (Farmer, Sponsor, Admin, Dealer)

**Headers**:
```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

#### Request Body
```json
{
  "invitationToken": "a1b2c3d4e5f6789012345678901234ab"
}
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `invitationToken` | string | ✅ Yes | 32-char hex token (from deep link or invitation list) |

**Backend Auto-Populates**:
- `CurrentUserId`: JWT'den
- `CurrentUserPhone`: JWT'den

#### Success Response (200 OK)

```json
{
  "data": {
    "acceptedInvitationId": 45,
    "assignedCodes": [
      {
        "codeId": 1234,
        "code": "SPONSOR-ABC-123",
        "packageTier": "M",
        "packageName": "Orta Paket"
      },
      {
        "codeId": 1235,
        "code": "SPONSOR-ABC-124",
        "packageTier": "M",
        "packageName": "Orta Paket"
      }
    ],
    "totalCodesAssigned": 50,
    "sponsorCompanyName": "Agro Tech Ltd",
    "acceptedDate": "2026-01-03T15:45:00Z"
  },
  "success": true,
  "message": "Invitation accepted successfully. 50 codes assigned."
}
```

#### Response Fields

| Field | Type | Description | Mobile Display |
|-------|------|-------------|----------------|
| `acceptedInvitationId` | int | Kabul edilen davetiye ID | Hidden |
| `assignedCodes` | array | Transfer edilen kodlar (sample) | **List first 10** |
| `assignedCodes[].codeId` | int | Kod ID | Hidden |
| `assignedCodes[].code` | string | Sponsorluk kodu | **"SPONSOR-ABC-123"** |
| `assignedCodes[].packageTier` | string | Tier | **"M"** badge |
| `assignedCodes[].packageName` | string | Paket adı | **"Orta Paket"** |
| `totalCodesAssigned` | int | Toplam kod sayısı | **"Toplam 50 kod eklendi"** |
| `sponsorCompanyName` | string | Sponsor adı | **"Agro Tech Ltd"** |
| `acceptedDate` | DateTime | Kabul tarihi | **"3 Ocak 2026, 15:45"** |

#### UI Design Suggestions (Success Screen)

```
┌─────────────────────────────────┐
│       ✅ Başarılı!              │
├─────────────────────────────────┤
│                                 │
│  Agro Tech Ltd tarafından       │
│  gönderilen 50 adet             │
│  sponsorluk kodu hesabınıza     │
│  eklendi!                       │
│                                 │
│  📅 3 Ocak 2026, 15:45          │
│                                 │
│  📦 Eklenen Kodlar:             │
│  ┌─────────────────────────┐   │
│  │ SPONSOR-ABC-123  [M]    │   │
│  │ SPONSOR-ABC-124  [M]    │   │
│  │ SPONSOR-ABC-125  [M]    │   │
│  │ ...ve 47 kod daha       │   │
│  └─────────────────────────┘   │
│                                 │
│  ┌───────────────────────────┐ │
│  │  Kodlarımı Görüntüle      │ │
│  └───────────────────────────┘ │
│                                 │
└─────────────────────────────────┘
```

#### Error Responses

**400 Bad Request - Invalid Token**
```json
{
  "data": null,
  "success": false,
  "message": "Invalid invitation token"
}
```

**Mobile Action**: Show alert "Geçersiz davetiye kodu"

**400 Bad Request - Expired**
```json
{
  "data": null,
  "success": false,
  "message": "Invitation has expired"
}
```

**Mobile Action**: Show alert "Davetiye süresi dolmuş"

**400 Bad Request - Already Accepted**
```json
{
  "data": null,
  "success": false,
  "message": "Invitation already accepted"
}
```

**Mobile Action**: Show alert "Bu davetiye daha önce kabul edilmiş"

**400 Bad Request - Phone Mismatch**
```json
{
  "data": null,
  "success": false,
  "message": "Phone number does not match invitation"
}
```

**Mobile Action**: Show alert "Bu davetiye sizin telefon numaranıza gönderilmemiş"

**500 Internal Server Error**
```json
{
  "data": null,
  "success": false,
  "message": "Failed to assign codes. Please contact support."
}
```

**Mobile Action**: Show error screen with support contact button

---

### 4. Create Individual Farmer Invitation (Sponsor)

Sponsor kullanıcısı **tek bir farmer'a** davetiye göndermek için bu endpoint'i kullanır. Backend, invitation token oluşturur ve SMS ile deep link gönderir.

#### Endpoint
```
POST /api/v1/sponsorship/farmer/invite
```

#### Authorization
**Required**: `Sponsor`, `Admin` roles

**Headers**:
```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

#### Request Body
```json
{
  "phone": "05551234567",
  "codeCount": 50,
  "sendViaSms": true
}
```

#### Request Fields

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `phone` | string | ✅ Yes | Farmer telefon numarası (Turkish format) | 10-11 digits |
| `codeCount` | int | ✅ Yes | Gönderilecek kod sayısı | 1-1000 |
| `sendViaSms` | bool | ❌ No | SMS gönderilsin mi? | Default: `true` |

**Backend Auto-Populates**:
- `SponsorId`: JWT'den (current user ID)
- `Token`: Guid.NewGuid().ToString("N") (32-char hex)
- `ExpiryDate`: Now + 7 days (configurable)
- `Status`: "Pending"
- `CreatedDate`: Now

**Backend Processes**:
1. Sponsor'ın yeterli kodu var mı kontrol eder
2. `codeCount` kadar kodu reserve eder (ReservedForFarmerInvitationId set edilir)
3. FarmerInvitation kaydı oluşturur
4. SMS gönderir (if sendViaSms=true):
   ```
   {SponsorCompanyName} size {codeCount} adet sponsorluk kodu gönderdi!
   Kabul etmek için: https://ziraai.com/farmer-invite/{token}
   ```

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 45,
    "token": "a1b2c3d4e5f6789012345678901234ab",
    "phone": "05551234567",
    "codeCount": 50,
    "expiryDate": "2026-01-10T10:00:00Z",
    "deepLink": "https://ziraai.com/farmer-invite/a1b2c3d4e5f6789012345678901234ab",
    "smsSent": true
  },
  "success": true,
  "message": "Farmer invitation created and SMS sent successfully"
}
```

#### Response Fields

| Field | Type | Description | Mobile Display |
|-------|------|-------------|----------------|
| `invitationId` | int | Oluşturulan davetiye ID | Hidden (internal tracking) |
| `token` | string | 32-char hex token | Hidden (embedded in deep link) |
| `phone` | string | Hedef telefon | **"05551234567"** |
| `codeCount` | int | Gönderilen kod sayısı | **"50 kod gönderildi"** |
| `expiryDate` | DateTime (ISO 8601) | Geçerlilik süresi | **"10 Ocak 2026'ya kadar geçerli"** |
| `deepLink` | string | Complete deep link URL | Copyable link (optional) |
| `smsSent` | bool | SMS gönderildi mi? | **"✅ SMS gönderildi"** badge |

#### UI Flow (Sponsor Mobile App)

```
┌─────────────────────────────────┐
│  Davetiye Gönder                │
├─────────────────────────────────┤
│                                 │
│  📱 Telefon Numarası            │
│  ┌───────────────────────────┐ │
│  │ 0555 123 45 67            │ │
│  └───────────────────────────┘ │
│                                 │
│  📦 Kod Sayısı                  │
│  ┌───────────────────────────┐ │
│  │ 50                        │ │
│  └───────────────────────────┘ │
│                                 │
│  📲 SMS Gönder                  │
│  ☑ Aktif                       │
│                                 │
│  ┌───────────────────────────┐ │
│  │   Davetiye Gönder         │ │ ← Tap
│  └───────────────────────────┘ │
└─────────────────────────────────┘

         ↓ POST /farmer/invite

┌─────────────────────────────────┐
│  ✅ Davetiye Gönderildi         │
├─────────────────────────────────┤
│                                 │
│  📱 0555 123 45 67              │
│  📦 50 kod                      │
│  ⏰ 10 Ocak 2026                │
│                                 │
│  ✅ SMS gönderildi              │
│                                 │
│  ┌───────────────────────────┐ │
│  │   Tamam                   │ │
│  └───────────────────────────┘ │
└─────────────────────────────────┘
```

#### Error Responses

**400 Bad Request - Insufficient Codes**
```json
{
  "data": null,
  "success": false,
  "message": "Insufficient available codes. You have 30 codes but requested 50."
}
```

**Mobile Action**: Show alert "Yetersiz kod sayısı. Mevcut: 30, İstenen: 50"

**400 Bad Request - Invalid Phone**
```json
{
  "data": null,
  "success": false,
  "message": "Invalid phone number format"
}
```

**Mobile Action**: Show validation error on phone field

**400 Bad Request - Invalid Code Count**
```json
{
  "data": null,
  "success": false,
  "message": "Code count must be between 1 and 1000"
}
```

**Mobile Action**: Show validation error on code count field

**409 Conflict - Pending Invitation Exists**
```json
{
  "data": null,
  "success": false,
  "message": "A pending invitation already exists for this phone number"
}
```

**Mobile Action**: Show alert "Bu telefon numarasına bekleyen bir davetiye zaten var"

**500 Internal Server Error - SMS Failed**
```json
{
  "data": {
    "invitationId": 45,
    "token": "abc...",
    "smsSent": false
  },
  "success": false,
  "message": "Invitation created but SMS delivery failed"
}
```

**Mobile Action**: Show warning "Davetiye oluşturuldu ancak SMS gönderilemedi. Manuel olarak paylaşabilirsiniz." + Show deep link for manual sharing

---

### 5. Bulk Create Farmer Invitations (Sponsor)

Sponsor kullanıcısı **birden fazla farmer'a** davetiye göndermek için bu endpoint'i kullanır. Excel/JSON formatında toplu gönderim.

#### Endpoint
```
POST /api/v1/sponsorship/farmer/invitations/bulk
```

#### Authorization
**Required**: `Sponsor`, `Admin` roles

**Headers**:
```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

#### Request Body
```json
{
  "invitations": [
    {
      "phone": "05551234567",
      "codeCount": 50
    },
    {
      "phone": "05559876543",
      "codeCount": 100
    }
  ],
  "sendViaSms": true
}
```

#### Request Fields

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `invitations` | array | ✅ Yes | Davetiye listesi | Max 2000 items |
| `invitations[].phone` | string | ✅ Yes | Farmer telefon | 10-11 digits |
| `invitations[].codeCount` | int | ✅ Yes | Kod sayısı | 1-1000 per invitation |
| `sendViaSms` | bool | ❌ No | SMS gönderilsin mi? | Default: `true` |

**Backend Processes**:
1. Toplam kod sayısını hesaplar (Σ codeCount)
2. Sponsor'ın yeterli kodu var mı kontrol eder
3. Her farmer için:
   - Invitation token oluşturur
   - Kodları reserve eder
   - FarmerInvitation kaydı oluşturur
   - SMS gönderir (if sendViaSms=true)
4. Başarı/başarısızlık raporunu döner

#### Success Response (200 OK)

```json
{
  "data": {
    "totalInvitations": 2,
    "successfulInvitations": 2,
    "failedInvitations": 0,
    "totalCodesReserved": 150,
    "results": [
      {
        "phone": "05551234567",
        "invitationId": 45,
        "codeCount": 50,
        "status": "Success",
        "smsSent": true
      },
      {
        "phone": "05559876543",
        "invitationId": 46,
        "codeCount": 100,
        "status": "Success",
        "smsSent": true
      }
    ]
  },
  "success": true,
  "message": "Bulk farmer invitations created successfully. 2 successful, 0 failed."
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `totalInvitations` | int | Toplam davetiye sayısı |
| `successfulInvitations` | int | Başarılı davetiye sayısı |
| `failedInvitations` | int | Başarısız davetiye sayısı |
| `totalCodesReserved` | int | Toplam reserve edilen kod |
| `results` | array | Her davetiye için detaylı sonuç |
| `results[].status` | string | "Success" or "Failed" |

#### UI Flow (Sponsor Mobile App)

```
┌─────────────────────────────────┐
│  Toplu Davetiye Gönder          │
├─────────────────────────────────┤
│                                 │
│  📄 Excel Yükle                 │
│  ┌───────────────────────────┐ │
│  │ farmers.xlsx              │ │ ← Tap to select
│  └───────────────────────────┘ │
│                                 │
│  veya                           │
│                                 │
│  ➕ Manuel Ekle                 │
│  ┌─────────────────────────┐   │
│  │ 0555 123 45 67 | 50 kod │   │
│  │ 0555 987 65 43 | 100 kod│   │
│  └─────────────────────────┘   │
│                                 │
│  📊 Toplam: 2 davetiye          │
│  📦 Toplam: 150 kod             │
│                                 │
│  ┌───────────────────────────┐ │
│  │   Gönder                  │ │ ← Tap
│  └───────────────────────────┘ │
└─────────────────────────────────┘

         ↓ POST /farmer/invitations/bulk

┌─────────────────────────────────┐
│  ✅ Toplu Gönderim Tamamlandı   │
├─────────────────────────────────┤
│                                 │
│  ✅ Başarılı: 2                 │
│  ❌ Başarısız: 0                │
│  📦 Toplam Kod: 150             │
│                                 │
│  Detaylar:                      │
│  ┌─────────────────────────┐   │
│  │ ✅ 0555***4567 | 50 kod │   │
│  │ ✅ 0555***6543 | 100 kod│   │
│  └─────────────────────────┘   │
│                                 │
│  ┌───────────────────────────┐ │
│  │   Tamam                   │ │
│  └───────────────────────────┘ │
└─────────────────────────────────┘
```

#### Error Responses

**400 Bad Request - Insufficient Codes**
```json
{
  "data": null,
  "success": false,
  "message": "Insufficient codes. Available: 100, Requested: 150"
}
```

**Mobile Action**: Show alert "Yetersiz kod. Mevcut: 100, Gereken: 150"

**400 Bad Request - Too Many Invitations**
```json
{
  "data": null,
  "success": false,
  "message": "Maximum 2000 invitations allowed per request"
}
```

**Mobile Action**: Show alert "Maksimum 2000 davetiye gönderilebilir"

**207 Multi-Status - Partial Success**
```json
{
  "data": {
    "totalInvitations": 3,
    "successfulInvitations": 2,
    "failedInvitations": 1,
    "totalCodesReserved": 150,
    "results": [
      {
        "phone": "05551234567",
        "invitationId": 45,
        "codeCount": 50,
        "status": "Success",
        "smsSent": true
      },
      {
        "phone": "05559876543",
        "invitationId": 46,
        "codeCount": 100,
        "status": "Success",
        "smsSent": true
      },
      {
        "phone": "INVALID",
        "invitationId": null,
        "codeCount": 0,
        "status": "Failed",
        "errorMessage": "Invalid phone number format",
        "smsSent": false
      }
    ]
  },
  "success": true,
  "message": "Bulk operation completed with partial success. 2 successful, 1 failed."
}
```

**Mobile Action**: Show summary with expandable failed list

---

## 🔐 Authentication Flow

### Flow 1: User Opens Deep Link (Not Logged In)

```
1. User taps SMS link
   └─> https://ziraai.com/farmer-invite/abc123...

2. App opens, extracts token: "abc123..."

3. Check if user logged in
   └─> NOT logged in

4. Call PUBLIC endpoint (no auth)
   └─> GET /api/v1/sponsorship/farmer/invitation-details?token=abc123
       └─> Success: Display invitation details
           └─> Show "Giriş Yap ve Kabul Et" button

5. User taps "Giriş Yap"
   └─> Navigate to Login screen
       └─> Pass token as parameter

6. User completes login
   └─> Receive JWT token
       └─> Store token in secure storage

7. Auto-navigate back to invitation
   └─> Now call accept endpoint

8. Accept invitation
   └─> POST /api/v1/sponsorship/farmer/accept-invitation
       Request: { invitationToken: "abc123..." }
       Headers: { Authorization: "Bearer {jwt}" }
       └─> Success: Show assigned codes
           └─> Navigate to "My Codes" screen
```

### Flow 2: User Opens Deep Link (Already Logged In)

```
1. User taps SMS link
   └─> https://ziraai.com/farmer-invite/abc123...

2. App opens, extracts token: "abc123..."

3. Check if user logged in
   └─> YES, JWT token exists

4. Call invitation details (optional, for display)
   └─> GET /api/v1/sponsorship/farmer/invitation-details?token=abc123

5. Show confirmation dialog
   └─> "Agro Tech Ltd'den 50 kod kabul edilsin mi?"
       ┌─────────┐ ┌─────────┐
       │  İptal  │ │  Kabul  │
       └─────────┘ └─────────┘

6. User taps "Kabul"
   └─> POST /api/v1/sponsorship/farmer/accept-invitation
       Request: { invitationToken: "abc123..." }
       Headers: { Authorization: "Bearer {jwt}" }
       └─> Success: Show success screen
           └─> Navigate to "My Codes"
```

### Flow 3: User Checks Pending Invitations (In-App)

```
1. User navigates to "My Invitations" tab
   └─> Must be logged in (JWT token required)

2. Call farmer invitations endpoint
   └─> GET /api/v1/sponsorship/farmer/my-invitations
       Headers: { Authorization: "Bearer {jwt}" }
       └─> Success: Display list

3. User taps "Kabul Et" on an invitation
   └─> Extract token from list item
       └─> Same as Flow 2, step 6
```

### Token Storage (Security Best Practices)

#### iOS (Keychain)
```swift
import Security

class SecureStorage {
    func saveToken(_ token: String) {
        let data = token.data(using: .utf8)!
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: "jwt_token",
            kSecValueData as String: data
        ]
        SecItemDelete(query as CFDictionary)
        SecItemAdd(query as CFDictionary, nil)
    }

    func getToken() -> String? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrAccount as String: "jwt_token",
            kSecReturnData as String: true
        ]
        var result: AnyObject?
        SecItemCopyMatching(query as CFDictionary, &result)

        if let data = result as? Data {
            return String(data: data, encoding: .utf8)
        }
        return nil
    }
}
```

#### Android (EncryptedSharedPreferences)
```kotlin
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKeys

class SecureStorage(context: Context) {
    private val masterKeyAlias = MasterKeys.getOrCreate(MasterKeys.AES256_GCM_SPEC)

    private val sharedPreferences = EncryptedSharedPreferences.create(
        "secure_prefs",
        masterKeyAlias,
        context,
        EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
        EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
    )

    fun saveToken(token: String) {
        sharedPreferences.edit().putString("jwt_token", token).apply()
    }

    fun getToken(): String? {
        return sharedPreferences.getString("jwt_token", null)
    }
}
```

#### Flutter (flutter_secure_storage)
```dart
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureStorage {
  final _storage = FlutterSecureStorage();

  Future<void> saveToken(String token) async {
    await _storage.write(key: 'jwt_token', value: token);
  }

  Future<String?> getToken() async {
    return await _storage.read(key: 'jwt_token');
  }

  Future<void> deleteToken() async {
    await _storage.delete(key: 'jwt_token');
  }
}
```

---

## 🚀 User Journeys

### Journey 1: First-Time Farmer Receives Invitation

```
Day 1, 10:00 AM
├─> Sponsor creates invitation
│   └─> POST /api/v1/sponsorship/farmer/invite
│       Request: { phone: "05551234567", codeCount: 50 }

Day 1, 10:00 AM (5 seconds later)
├─> Farmer receives SMS
│   "Agro Tech Ltd size 50 adet sponsorluk kodu hediye etti!
│    Kabul etmek için: https://ziraai.com/farmer-invite/abc123...
│    Geçerlilik: 7 gün"

Day 1, 10:15 AM
├─> Farmer taps SMS link
│   └─> App opens (not installed? → App Store)
│       └─> Deep link handled
│           └─> GET /invitation-details?token=abc123 (no auth)
│               └─> Display:
│                   "🎁 Agro Tech Ltd
│                    size 50 kod hediye etti!"

Day 1, 10:16 AM
├─> Farmer has no account yet
│   └─> Taps "Kayıt Ol"
│       └─> Registration flow
│           └─> Phone: 05551234567
│               └─> OTP verification
│                   └─> Account created
│                       └─> JWT token received

Day 1, 10:20 AM
├─> Auto-redirect to invitation
│   └─> POST /farmer/accept-invitation
│       Request: { invitationToken: "abc123..." }
│       └─> Success!
│           ✅ 50 codes assigned
│           └─> Navigate to "My Codes"
│               └─> Farmer can now use codes
```

### Journey 2: Existing Farmer Receives Second Invitation

```
Day 5, 3:00 PM
├─> Sponsor creates invitation
│   └─> Farmer already has account + logged in app

Day 5, 3:00 PM (5 seconds later)
├─> Farmer receives SMS
│   └─> Taps link
│       └─> App already open, extracts token
│           └─> Check: User already logged in ✅
│               └─> GET /invitation-details (for display)
│                   └─> Show confirmation:
│                       "Green Farm Solutions
│                        size 30 kod hediye etti!
│                        Kabul edilsin mi?"

Day 5, 3:01 PM
├─> Farmer taps "Kabul Et"
│   └─> POST /farmer/accept-invitation
│       └─> Success!
│           └─> Show toast: "30 kod eklendi!"
│               └─> Auto-refresh "My Codes" screen
```

### Journey 3: Farmer Checks Pending Invitations In-App

```
Day 3, 9:00 AM
├─> Farmer opens app
│   └─> Navigates to "Davetiyeler" tab
│       └─> GET /farmer/my-invitations
│           └─> Display list:
│               ┌────────────────────────┐
│               │ Agro Tech Ltd          │
│               │ 50 kod · 4 gün kaldı   │
│               │ [Kabul Et]             │
│               ├────────────────────────┤
│               │ Green Farm             │
│               │ 30 kod · 2 gün kaldı   │
│               │ [Kabul Et]             │
│               └────────────────────────┘

Day 3, 9:02 AM
├─> Farmer taps first "Kabul Et"
│   └─> Confirmation dialog
│       └─> Taps "Onayla"
│           └─> POST /farmer/accept-invitation
│               └─> Success!
│                   └─> Remove from list
│                       └─> Show: "50 kod eklendi!"

Day 3, 9:03 AM
├─> List auto-refreshes
│   └─> Only 1 invitation remaining
│       (first one removed after acceptance)
```

### Journey 4: Expired Invitation Scenario

```
Day 10 (after 7-day expiry)
├─> Farmer taps old SMS link
│   └─> GET /invitation-details?token=abc123
│       └─> Response:
│           {
│             "canAccept": false,
│             "status": "Expired"
│           }
│       └─> Display:
│           ❌ "Bu davetiye süresi dolmuş"
│           └─> Show "Anasayfaya Dön" button

Alternative: Farmer checks in-app
├─> GET /farmer/my-invitations
│   └─> Response: []
│       (expired invitations auto-filtered by backend)
```

---

## ⚠️ Error Handling

### HTTP Status Codes

| Code | Meaning | Mobile Action |
|------|---------|---------------|
| 200 | Success | Show success UI |
| 400 | Bad Request | Show error message |
| 401 | Unauthorized | Redirect to login |
| 403 | Forbidden | Show "Yetkisiz erişim" |
| 500 | Server Error | Show "Bir hata oluştu" + retry button |

### Common Error Scenarios

#### Scenario 1: Token Not Found
```json
{
  "success": false,
  "message": "Invitation not found or expired"
}
```

**Mobile UI**:
```
┌─────────────────────────────────┐
│       ❌ Hata                   │
├─────────────────────────────────┤
│                                 │
│  Davetiye bulunamadı veya       │
│  süresi dolmuş.                 │
│                                 │
│  ┌───────────────────────────┐ │
│  │    Anasayfaya Dön         │ │
│  └───────────────────────────┘ │
│                                 │
└─────────────────────────────────┘
```

#### Scenario 2: Already Accepted
```json
{
  "success": false,
  "message": "Invitation already accepted"
}
```

**Mobile UI**: Show toast/snackbar with message, then auto-navigate to "My Codes"

#### Scenario 3: Phone Mismatch
```json
{
  "success": false,
  "message": "Phone number does not match invitation"
}
```

**Mobile UI**:
```
┌─────────────────────────────────┐
│       ⚠️ Uyarı                  │
├─────────────────────────────────┤
│                                 │
│  Bu davetiye farklı bir         │
│  telefon numarasına             │
│  gönderilmiş.                   │
│                                 │
│  Davetiye: 0555***7890          │
│  Hesabınız: 0555***4567         │
│                                 │
│  ┌───────────────────────────┐ │
│  │    Tamam                  │ │
│  └───────────────────────────┘ │
│                                 │
└─────────────────────────────────┘
```

#### Scenario 4: Network Error
```
DioException: Network error
```

**Mobile UI**:
```
┌─────────────────────────────────┐
│       📡 Bağlantı Hatası        │
├─────────────────────────────────┤
│                                 │
│  İnternet bağlantınızı          │
│  kontrol edip tekrar deneyin.   │
│                                 │
│  ┌───────────────────────────┐ │
│  │    Tekrar Dene            │ │
│  └───────────────────────────┘ │
│                                 │
└─────────────────────────────────┘
```

### Error Handling Code (Flutter)

```dart
Future<void> acceptInvitation(String token) async {
  try {
    setState(() => _isLoading = true);

    final response = await dio.post(
      '/api/v1/sponsorship/farmer/accept-invitation',
      data: {'invitationToken': token},
      options: Options(
        headers: {'Authorization': 'Bearer ${await _getToken()}'},
      ),
    );

    if (response.data['success'] == true) {
      _showSuccessScreen(response.data['data']);
    } else {
      _showErrorDialog(response.data['message']);
    }
  } on DioException catch (e) {
    if (e.response?.statusCode == 401) {
      _navigateToLogin(returnToken: token);
    } else if (e.response?.statusCode == 400) {
      _showErrorDialog(e.response?.data['message'] ?? 'Bir hata oluştu');
    } else if (e.type == DioExceptionType.connectionTimeout ||
               e.type == DioExceptionType.receiveTimeout) {
      _showNetworkErrorDialog();
    } else {
      _showErrorDialog('Beklenmeyen bir hata oluştu');
    }
  } finally {
    setState(() => _isLoading = false);
  }
}
```

---

## 🧪 Testing Guide

### Test Cases

#### TC-1: Deep Link Handling (Not Logged In)
**Steps**:
1. Clear app data (logout)
2. Send test SMS with deep link to test phone
3. Tap link on device

**Expected**:
- App opens (or redirects to App Store if not installed)
- Invitation details screen shown
- "Giriş Yap" button visible
- No error messages

**Verify**:
- Token extracted correctly from URL
- API call to `/invitation-details` successful (check network logs)
- Correct sponsor name, code count, expiry displayed

---

#### TC-2: Deep Link Handling (Already Logged In)
**Steps**:
1. Login to app
2. Tap deep link from SMS

**Expected**:
- App opens invitation screen immediately
- Confirmation dialog shown
- "Kabul Et" button enabled

**Verify**:
- JWT token sent in Authorization header
- No login screen shown

---

#### TC-3: Accept Invitation Flow
**Steps**:
1. Open invitation (logged in)
2. Tap "Kabul Et"
3. Confirm in dialog

**Expected**:
- Loading indicator shown
- Success screen appears
- Assigned codes displayed
- Total count shown

**Verify**:
- API call to `/accept-invitation` successful
- Response contains `assignedCodes` array
- Navigation to "My Codes" works

---

#### TC-4: View Pending Invitations
**Steps**:
1. Navigate to "Davetiyeler" tab
2. Wait for load

**Expected**:
- List of pending invitations shown
- Each item shows sponsor, code count, expiry
- "Kabul Et" button on each item

**Verify**:
- API call to `/my-invitations` successful
- Only pending invitations shown (no expired)
- Sorting by created date (newest first)

---

#### TC-5: Handle Expired Invitation
**Steps**:
1. Create test invitation with ExpiryDate = Now - 1 day (database)
2. Try to open via deep link

**Expected**:
- Error screen shown
- Message: "Davetiye süresi dolmuş"
- No "Kabul Et" button

**Verify**:
- `canAccept: false` in API response
- No crash or exception

---

#### TC-6: Handle Already Accepted
**Steps**:
1. Accept an invitation
2. Try to accept same invitation again (tap SMS link again)

**Expected**:
- Error message: "Bu davetiye daha önce kabul edilmiş"
- Or: Auto-navigate to "My Codes" with toast message

**Verify**:
- 400 Bad Request from API
- Graceful error handling

---

#### TC-7: Phone Mismatch
**Steps**:
1. Login with User A (phone: 05551111111)
2. Open invitation sent to User B (phone: 05552222222)

**Expected**:
- Error dialog shown
- Message includes both phone numbers (masked)
- "Tamam" button to dismiss

**Verify**:
- 400 Bad Request from API
- Error message clear to user

---

#### TC-8: Network Error Handling
**Steps**:
1. Turn off WiFi/mobile data
2. Try to accept invitation

**Expected**:
- Network error dialog shown
- "Tekrar Dene" button available

**Verify**:
- No app crash
- Retry button works when network restored

---

### Test Environment Setup

#### Staging API Base URL
```
https://ziraai-api-sit.up.railway.app
```

#### Test Accounts

Create test users:
- **Sponsor**: sponsor_test@ziraai.com / password123
- **Farmer**: farmer_test@ziraai.com / password123

#### Test Deep Links

Manual test link (replace token):
```
https://ziraai.com/farmer-invite/a1b2c3d4e5f6789012345678901234ab
```

#### Database Queries for Testing

Create test invitation (PostgreSQL):
```sql
-- Insert test invitation
INSERT INTO "FarmerInvitations" (
  "SponsorId", "Phone", "FarmerName", "InvitationToken",
  "Status", "CodeCount", "PackageTier", "CreatedDate", "ExpiryDate"
) VALUES (
  123, '05551234567', 'Test Farmer', 'a1b2c3d4e5f6789012345678901234ab',
  'Pending', 50, 'M', NOW(), NOW() + INTERVAL '7 days'
);

-- Check invitation status
SELECT * FROM "FarmerInvitations"
WHERE "InvitationToken" = 'a1b2c3d4e5f6789012345678901234ab';

-- Manually expire invitation
UPDATE "FarmerInvitations"
SET "ExpiryDate" = NOW() - INTERVAL '1 day'
WHERE "InvitationToken" = 'a1b2c3d4e5f6789012345678901234ab';
```

---

## 🎨 UI/UX Best Practices

### 1. Loading States

Always show loading indicators during API calls:

```dart
// Good
ElevatedButton(
  onPressed: _isLoading ? null : _acceptInvitation,
  child: _isLoading
    ? CircularProgressIndicator()
    : Text('Kabul Et'),
)
```

### 2. Countdown Timer for Expiry

Show dynamic countdown:

```dart
String formatExpiry(DateTime expiryDate) {
  final now = DateTime.now();
  final diff = expiryDate.difference(now);

  if (diff.isNegative) return 'Süresi dolmuş';
  if (diff.inDays == 0) return 'Bugün sona eriyor';
  if (diff.inDays == 1) return '1 gün kaldı';
  return '${diff.inDays} gün kaldı';
}
```

### 3. Phone Masking

Mask phone numbers for privacy:

```dart
String maskPhone(String phone) {
  if (phone.length < 7) return phone;
  final start = phone.substring(0, 4); // "0555"
  final end = phone.substring(phone.length - 4); // "4567"
  return '$start***$end'; // "0555***4567"
}
```

### 4. Pull-to-Refresh

Enable pull-to-refresh on invitation list:

```dart
RefreshIndicator(
  onRefresh: _loadInvitations,
  child: ListView.builder(...),
)
```

### 5. Empty States

Show helpful empty states:

```dart
if (invitations.isEmpty) {
  return Center(
    child: Column(
      children: [
        Icon(Icons.inbox_outlined, size: 64),
        SizedBox(height: 16),
        Text('Henüz bekleyen davetiyeniz yok'),
        SizedBox(height: 8),
        Text('Sponsorlar size kod gönderdiğinde burada görünür'),
      ],
    ),
  );
}
```

---

## 📚 API Quick Reference

| Method | Endpoint | Auth | Purpose |
|--------|----------|------|---------|
| GET | `/farmer/invitation-details?token=X` | ❌ No | View invitation before login |
| GET | `/farmer/my-invitations` | ✅ Yes (Farmer) | List pending invitations |
| POST | `/farmer/accept-invitation` | ✅ Yes (Any) | Accept invitation |

**Base URL**: `https://ziraai.com/api/v1/sponsorship`

---

## 🔧 Troubleshooting

### Problem: Deep link not opening app

**Check**:
1. iOS: Universal Links configured in Associated Domains?
2. Android: Intent filter in AndroidManifest.xml?
3. AASA file accessible at `https://ziraai.com/.well-known/apple-app-site-association`?

**Solution**: Re-check platform-specific deep link configuration

---

### Problem: "Phone mismatch" error

**Check**:
1. JWT token has phone claim?
2. Phone format normalized correctly?
3. Invitation created for same phone?

**Solution**: Ensure backend phone normalization is consistent

---

### Problem: Invitation not in "My Invitations" list

**Check**:
1. Invitation status is "Pending"?
2. ExpiryDate > now?
3. Phone matches user's phone?

**Solution**: Use database query to verify invitation data

---

**Document Version**: 1.0
**Created**: 2026-01-03
**Author**: Backend Team
**Related Docs**:
- Frontend Integration Guide
- Dealer Invitations Mobile Guide (comparison reference)
