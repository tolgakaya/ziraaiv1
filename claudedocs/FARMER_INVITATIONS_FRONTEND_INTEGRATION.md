# Farmer Invitations - Frontend Integration Guide

**Last Updated**: 2026-01-03
**API Version**: 1.0
**Target Audience**: Frontend (Web) Development Team

---

## 📋 Table of Contents

1. [Overview & Comparison](#overview--comparison)
2. [API Endpoints Reference](#api-endpoints-reference)
3. [Request/Response Structures](#requestresponse-structures)
4. [Authentication & Authorization](#authentication--authorization)
5. [Business Logic & Validation](#business-logic--validation)
6. [Error Handling](#error-handling)
7. [Integration Workflows](#integration-workflows)
8. [Best Practices](#best-practices)

---

## 🎯 Overview & Comparison

### What is Farmer Invitations?

Farmer Invitations sistemi, **sponsorların çiftçilere token-based davetiye gönderip sponsorluk kodlarını transfer etmesini** sağlar. Google Play SDK 35+ uyumluluğu için SMS listener yerine deep link kullanır.

### 🔄 Send Link vs Farmer Invitation: What Changed?

#### ❌ OLD WAY: Send Link (Still Active, But Not Recommended)

**How It Worked:**
```
1. Sponsor: "Send Code" feature
   └─> POST /api/v1/sponsorship/send-link
       └─> Backend sends REAL CODE via SMS
           SMS to Farmer: "Your code: SPONSOR-ABC-123"

2. Farmer: Receives SMS with code
   └─> Opens app manually
   └─> Goes to "Redeem Code" screen
   └─> Types or pastes: SPONSOR-ABC-123
       (SMS listener auto-filled on SDK <35)
   └─> POST /api/v1/sponsorship/redeem
       └─> Code redeemed, subscription activated
```

**Problems:**
- ❌ Google Play SDK 35+ doesn't allow SMS listener
- ❌ Farmer must manually copy-paste code (bad UX)
- ❌ Only 1 code per SMS
- ❌ SMS can be lost or deleted

---

#### ✅ NEW WAY: Farmer Invitation (Recommended)

**How It Works:**
```
1. Sponsor: "Send Invitation" feature
   └─> POST /api/v1/sponsorship/farmer/invite
       Request: { phone, codeCount: 50 }
       └─> Backend sends DEEP LINK via SMS (NOT code!)
           SMS to Farmer: "Agro Tech sent you 50 codes!
                           https://ziraai.com/farmer-invite/abc123..."

2. Farmer: Receives SMS with deep link
   └─> Taps link → Mobile app opens automatically
   └─> Shows invitation details (sponsor, 50 codes, expiry)
   └─> If not logged in → Login → Return to invitation
   └─> Taps "Accept" button
   └─> POST /api/v1/sponsorship/farmer/accept-invitation
       └─> Backend assigns all 50 codes automatically!
           Success: "50 codes added to your account!"
```

**Benefits:**
- ✅ No SMS listener needed (uses deep links)
- ✅ Google Play SDK 35+ compatible
- ✅ Bulk code distribution (1-1000 codes per invitation)
- ✅ Single tap acceptance (no manual code entry)
- ✅ Cross-device support (link works on any device)
- ✅ Better tracking (invitation status, expiry, audit)
- ✅ Admin can send on behalf of sponsor (bulk operations)

---

### Comparison Table

| Aspect | Old (Send Link) | New (Farmer Invitation) |
|--------|-----------------|-------------------------|
| **What sponsor sends** | Real sponsorship code | Invitation token (deep link) |
| **SMS content** | "Code: SPONSOR-ABC-123" | "50 codes! [tap link]" |
| **Farmer action** | Copy code → Paste → Redeem | Tap link → Accept |
| **Codes per operation** | 1 code | 1 to 1000 codes |
| **SDK 35+ compatible** | ❌ No (needs SMS listener) | ✅ Yes (uses deep links) |
| **Manual work** | Copy-paste code | Just tap "Accept" |
| **Bulk support** | ❌ No | ✅ Yes (Excel upload) |
| **Admin support** | ❌ No | ✅ Yes (on-behalf-of) |
| **Sponsor endpoint** | POST `/send-link` | POST `/farmer/invite` |
| **Farmer endpoint** | POST `/redeem` | POST `/farmer/accept-invitation` |
| **Backend flow** | Send code → Farmer redeems | Reserve codes → Farmer accepts |
| **Code reservation** | ❌ No | ✅ Yes (prevents double-use) |
| **Status** | Active (backward compatibility) | **Recommended** (primary method) |

---

### When to Use Which?

#### Use Send Link (Old) When:
- ❌ **Not recommended** - Only for backward compatibility
- Supporting legacy sponsors still using old UI
- (Even for single codes, invitation is better)

#### Use Farmer Invitation (New) When:
- ✅ **Recommended for all new implementations**
- Bulk code distribution (1-1000 codes)
- Google Play SDK 35+ requirement
- Better user experience needed
- Admin needs to send on behalf of sponsor
- Tracking and audit logging required

---

### Migration Path

**Frontend Changes:**
```
Old UI: "Send Code to Farmer"
├─> Input: Phone number
├─> Action: Send 1 code
└─> Endpoint: POST /api/v1/sponsorship/send-link

New UI: "Send Invitation to Farmer"
├─> Input: Phone, Code Count (1-1000), Tier (optional), Notes
├─> Action: Send invitation with N codes
└─> Endpoint: POST /api/v1/sponsorship/farmer/invite

New UI (Bulk): "Bulk Send Invitations"
├─> Input: Excel file upload or manual entry
├─> Action: Send invitations to multiple farmers
└─> Endpoint: POST /api/v1/sponsorship/farmer/invitations/bulk
```

**No Breaking Changes:**
- Old `/send-link` endpoint still works
- Old `/redeem` endpoint still works
- Both systems run in parallel
- Gradual migration possible

### Dealer vs Farmer Invitations Comparison

| Feature | Dealer Invitations | Farmer Invitations |
|---------|-------------------|-------------------|
| **Purpose** | Bayilere kod transferi | Çiftçilere kod transferi |
| **Target Role** | Dealer | Farmer |
| **Sponsor Action** | `/api/v1/sponsorship/dealer/invite-via-sms` | `/api/v1/sponsorship/farmer/invite` |
| **Bulk Support** | ❌ No | ✅ Yes (`/farmer/invitations/bulk`) |
| **Admin Support** | ❌ No | ✅ Yes (`/admin/farmer/invitations/bulk`) |
| **Acceptance** | `/api/v1/dealer/invitations/accept` | `/api/v1/sponsorship/farmer/accept-invitation` |
| **List Endpoint** | `/api/v1/dealer/invitations/my-pending` | `/api/v1/sponsorship/farmer/my-invitations` |
| **Details Endpoint** | ❌ No | ✅ Yes (`/farmer/invitation-details`) |
| **SignalR Support** | ✅ Yes (NewDealerInvitation) | ❌ Not Yet (Future) |
| **Phone Normalization** | Turkish format (+90/0) | Turkish format (+90/0) |
| **Token Format** | 32-char hex | 32-char hex |
| **Expiry** | 7 days (default) | 7 days (default) |
| **Code Reservation** | ❌ No | ✅ Yes (codes reserved on creation) |

### Key Differences

1. **Bulk Operations**: Farmer invitations destekler, dealer desteklemez
2. **Admin On-Behalf**: Farmer'da admin sponsor adına toplu davetiye gönderebilir
3. **Public Details**: Farmer invitation'da token ile public detay görüntüleme var
4. **Code Reservation**: Farmer'da kodlar davetiye oluşturulurken rezerve edilir
5. **SignalR**: Dealer'da real-time notification var, farmer'da henüz yok

---

## 📡 API Endpoints Reference

### 1. Create Individual Farmer Invitation

**Sponsor** rolündeki kullanıcı tek bir çiftçiye davetiye gönderir.

#### Endpoint
```
POST /api/v1/sponsorship/farmer/invite
```

#### Authorization
- **Roles**: `Sponsor`, `Admin`
- **Headers**:
  ```
  Authorization: Bearer {jwt_token}
  x-dev-arch-version: 1.0
  Content-Type: application/json
  ```

#### Request Body
```json
{
  "phone": "05551234567",
  "farmerName": "Ahmet Yılmaz",
  "email": "ahmet@example.com",
  "codeCount": 50,
  "packageTier": "M",
  "notes": "VIP müşteri için özel davet"
}
```

#### Request Fields

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `phone` | string | ✅ Yes | Çiftçi telefon numarası (Türk formatı) | `"05551234567"` veya `"+905551234567"` |
| `farmerName` | string | ✅ Yes | Çiftçi adı | `"Ahmet Yılmaz"` |
| `email` | string | ❌ No | Çiftçi email adresi (opsiyonel) | `"ahmet@example.com"` |
| `codeCount` | int | ✅ Yes | Transfer edilecek kod sayısı | `50` |
| `packageTier` | string | ❌ No | Paket tier filtresi: S, M, L, XL (null = any) | `"M"` |
| `notes` | string | ❌ No | Sponsor notu (max 500 karakter) | `"VIP müşteri"` |

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 45,
    "invitationToken": "a1b2c3d4e5f6789012345678901234ab",
    "phone": "05551234567",
    "farmerName": "Ahmet Yılmaz",
    "codeCount": 50,
    "packageTier": "M",
    "expiryDate": "2026-01-10T14:30:00",
    "status": "Pending",
    "deepLink": "https://ziraai.com/farmer-invite/a1b2c3d4e5f6789012345678901234ab",
    "smsDeliveryStatus": "Sent",
    "smsSentAt": "2026-01-03T14:30:00",
    "reservedCodeIds": [1234, 1235, 1236, 1237, 1238]
  },
  "success": true,
  "message": "Farmer invitation sent successfully via SMS"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `invitationId` | int | Davetiye unique ID |
| `invitationToken` | string | 32-char hex token (deep link için kullanılır) |
| `phone` | string | Normalize edilmiş telefon numarası |
| `farmerName` | string | Çiftçi adı |
| `codeCount` | int | Transfer edilecek kod sayısı |
| `packageTier` | string | Tier filtresi (null olabilir) |
| `expiryDate` | DateTime | Davetiye geçerlilik süresi (ISO 8601) |
| `status` | string | Davetiye durumu: `"Pending"`, `"Accepted"`, `"Expired"`, `"Cancelled"` |
| `deepLink` | string | Mobil uygulamada açılacak deep link |
| `smsDeliveryStatus` | string | SMS gönderim durumu: `"Sent"`, `"Failed"`, `"Pending"` |
| `smsSentAt` | DateTime | SMS gönderim zamanı |
| `reservedCodeIds` | int[] | Rezerve edilen sponsorluk kodu ID'leri |

#### Error Responses

**400 Bad Request - Insufficient Codes**
```json
{
  "data": null,
  "success": false,
  "message": "Insufficient available codes. Requested: 50, Available: 30"
}
```

**400 Bad Request - Invalid Phone**
```json
{
  "data": null,
  "success": false,
  "message": "Invalid phone number format"
}
```

**401 Unauthorized**
```json
{
  "message": "Unauthorized"
}
```

**403 Forbidden**
```json
{
  "message": "Forbidden"
}
```

---

### 2. Bulk Create Farmer Invitations

**Sponsor** rolündeki kullanıcı toplu davetiye gönderir (Excel upload benzeri).

#### Endpoint
```
POST /api/v1/sponsorship/farmer/invitations/bulk
```

#### Authorization
- **Roles**: `Sponsor`, `Admin`
- **Headers**:
  ```
  Authorization: Bearer {jwt_token}
  x-dev-arch-version: 1.0
  Content-Type: application/json
  ```

#### Request Body
```json
{
  "recipients": [
    {
      "phone": "05551234567",
      "farmerName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "codeCount": 50,
      "packageTier": "M",
      "notes": "VIP müşteri"
    },
    {
      "phone": "05559876543",
      "farmerName": "Mehmet Demir",
      "email": null,
      "codeCount": 30,
      "packageTier": null,
      "notes": null
    }
  ],
  "channel": "SMS"
}
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `recipients` | array | ✅ Yes | Davetiye alıcıları listesi (max 2000) |
| `recipients[].phone` | string | ✅ Yes | Çiftçi telefonu |
| `recipients[].farmerName` | string | ✅ Yes | Çiftçi adı |
| `recipients[].email` | string | ❌ No | Çiftçi email |
| `recipients[].codeCount` | int | ✅ Yes | Kod sayısı (min: 1) |
| `recipients[].packageTier` | string | ❌ No | Tier filtresi: S, M, L, XL |
| `recipients[].notes` | string | ❌ No | Sponsor notu (max 500 char) |
| `channel` | string | ✅ Yes | Gönderim kanalı: `"SMS"` veya `"WhatsApp"` |

#### Success Response (200 OK)

```json
{
  "data": {
    "successCount": 98,
    "failedCount": 2,
    "totalCount": 100,
    "successfulInvitations": [
      {
        "invitationId": 50,
        "phone": "05551234567",
        "farmerName": "Ahmet Yılmaz",
        "codeCount": 50,
        "invitationToken": "abc123...",
        "deepLink": "https://ziraai.com/farmer-invite/abc123..."
      }
    ],
    "failedInvitations": [
      {
        "phone": "invalid_phone",
        "farmerName": "Invalid User",
        "errorMessage": "Invalid phone number format",
        "errorCode": "INVALID_PHONE"
      }
    ],
    "totalReservedCodes": 4850
  },
  "success": true,
  "message": "Bulk invitation process completed. Success: 98, Failed: 2"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `successCount` | int | Başarılı davetiye sayısı |
| `failedCount` | int | Başarısız davetiye sayısı |
| `totalCount` | int | Toplam işlem sayısı |
| `successfulInvitations` | array | Başarılı davetiyelerin detayları |
| `failedInvitations` | array | Başarısız davetiyelerin hata detayları |
| `totalReservedCodes` | int | Toplam rezerve edilen kod sayısı |

#### Error Responses

**400 Bad Request - Empty Recipients**
```json
{
  "data": null,
  "success": false,
  "message": "Recipients list cannot be empty"
}
```

**400 Bad Request - Too Many Recipients**
```json
{
  "data": null,
  "success": false,
  "message": "Maximum 2000 recipients allowed per batch"
}
```

---

### 3. Admin Bulk Create (On Behalf of Sponsor)

**Admin** rolündeki kullanıcı sponsor adına toplu davetiye gönderir. Audit logging için ek bilgiler kaydedilir.

#### Endpoint
```
POST /api/v1/sponsorship/admin/farmer/invitations/bulk
```

#### Authorization
- **Roles**: `Admin` ONLY
- **Headers**:
  ```
  Authorization: Bearer {jwt_token}
  x-dev-arch-version: 1.0
  Content-Type: application/json
  ```

#### Request Body
```json
{
  "sponsorId": 123,
  "recipients": [
    {
      "phone": "05551234567",
      "farmerName": "Ahmet Yılmaz",
      "codeCount": 50,
      "packageTier": "M"
    }
  ],
  "channel": "SMS",
  "adminNotes": "Emergency bulk send for sponsor request #456"
}
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sponsorId` | int | ✅ Yes | Hedef sponsor ID (admin adına işlem yapılacak) |
| `recipients` | array | ✅ Yes | Alıcılar listesi (max 2000) |
| `channel` | string | ✅ Yes | `"SMS"` veya `"WhatsApp"` |
| `adminNotes` | string | ❌ No | Admin notu (audit log için, max 1000 char) |

**Not**: `recipients` array yapısı normal bulk endpoint ile aynıdır.

#### Success Response (200 OK)

Yanıt yapısı normal bulk endpoint ile aynıdır, ancak backend'de **audit log** kaydedilir:
- Admin user ID
- IP address
- User agent
- Request path
- Admin notes

---

### 4. Get Sponsor's Farmer Invitations

**Sponsor** kendi gönderdiği davetiyeleri listeler. Status filtreleme desteği vardır.

#### Endpoint
```
GET /api/v1/sponsorship/farmer/invitations?status={status}
```

#### Authorization
- **Roles**: `Sponsor`, `Admin`
- **Headers**:
  ```
  Authorization: Bearer {jwt_token}
  x-dev-arch-version: 1.0
  ```

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `status` | string | ❌ No | Durum filtresi: `Pending`, `Accepted`, `Expired`, `Cancelled` | `?status=Pending` |

**Not**: Status parametresi verilmezse tüm davetiyeler döner.

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
      "createdDate": "2026-01-03T10:00:00",
      "expiryDate": "2026-01-10T10:00:00",
      "linkDelivered": true,
      "linkSentDate": "2026-01-03T10:00:05",
      "linkSentVia": "SMS"
    },
    {
      "id": 46,
      "phone": "05559876543",
      "farmerName": "Mehmet Demir",
      "email": null,
      "status": "Accepted",
      "codeCount": 30,
      "packageTier": null,
      "acceptedByUserId": 789,
      "acceptedDate": "2026-01-04T15:30:00",
      "createdDate": "2026-01-03T11:00:00",
      "expiryDate": "2026-01-10T11:00:00",
      "linkDelivered": true,
      "linkSentDate": "2026-01-03T11:00:05",
      "linkSentVia": "WhatsApp"
    }
  ],
  "success": true,
  "message": "Farmer invitations retrieved successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Davetiye ID |
| `phone` | string | Çiftçi telefonu |
| `farmerName` | string | Çiftçi adı |
| `email` | string | Çiftçi email (nullable) |
| `status` | string | `"Pending"`, `"Accepted"`, `"Expired"`, `"Cancelled"` |
| `codeCount` | int | Kod sayısı |
| `packageTier` | string | Tier filtresi (nullable) |
| `acceptedByUserId` | int | Kabul eden user ID (nullable) |
| `acceptedDate` | DateTime | Kabul tarihi (nullable) |
| `createdDate` | DateTime | Oluşturulma tarihi |
| `expiryDate` | DateTime | Geçerlilik süresi |
| `linkDelivered` | bool | Link gönderildi mi? |
| `linkSentDate` | DateTime | Link gönderim tarihi (nullable) |
| `linkSentVia` | string | Gönderim kanalı: `"SMS"`, `"WhatsApp"` (nullable) |

---

### 5. Get Invitation Details by Token (PUBLIC)

Token ile davetiye detaylarını **anonim** olarak görüntüler. Mobil uygulama login öncesi detayları göstermek için kullanır.

#### Endpoint
```
GET /api/v1/sponsorship/farmer/invitation-details?token={token}
```

#### Authorization
- **Roles**: NONE (Public endpoint - `[AllowAnonymous]`)
- **Headers**:
  ```
  x-dev-arch-version: 1.0
  Content-Type: application/json
  ```

**Not**: JWT token **GEREKMİYOR**.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `token` | string | ✅ Yes | 32-char hex invitation token | `?token=abc123...` |

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 45,
    "sponsorCompanyName": "Agro Tech Ltd",
    "codeCount": 50,
    "packageTier": "M",
    "expiryDate": "2026-01-10T10:00:00",
    "status": "Pending",
    "canAccept": true,
    "phone": "05551234567",
    "farmerName": "Ahmet Yılmaz"
  },
  "success": true,
  "message": "Invitation details retrieved successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `invitationId` | int | Davetiye ID |
| `sponsorCompanyName` | string | Davet eden sponsor şirket adı |
| `codeCount` | int | Transfer edilecek kod sayısı |
| `packageTier` | string | Tier filtresi (nullable) |
| `expiryDate` | DateTime | Geçerlilik süresi |
| `status` | string | Davetiye durumu |
| `canAccept` | bool | Kabul edilebilir mi? (Status=Pending ve ExpiryDate>Now) |
| `phone` | string | Hedef telefon (son 4 hane masked olabilir) |
| `farmerName` | string | Hedef çiftçi adı |

#### Error Responses

**400 Bad Request - Token Missing**
```json
{
  "data": null,
  "success": false,
  "message": "Token is required"
}
```

**400 Bad Request - Invalid Token**
```json
{
  "data": null,
  "success": false,
  "message": "Invitation not found or expired"
}
```

---

### 6. Get My Farmer Invitations (Farmer Endpoint)

**Farmer** rolündeki kullanıcı kendisine gönderilen pending davetiyeleri görüntüler.

#### Endpoint
```
GET /api/v1/sponsorship/farmer/my-invitations
```

#### Authorization
- **Roles**: `Farmer`, `Admin`
- **Headers**:
  ```
  Authorization: Bearer {jwt_token}
  x-dev-arch-version: 1.0
  ```

#### Query Parameters
None. Kullanıcı telefonu JWT token'dan otomatik çıkarılır.

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
      "createdDate": "2026-01-03T10:00:00",
      "expiryDate": "2026-01-10T10:00:00",
      "linkDelivered": true,
      "linkSentDate": "2026-01-03T10:00:05",
      "linkSentVia": "SMS"
    }
  ],
  "success": true,
  "message": "1 pending invitation(s) found"
}
```

**Not**: Yanıt yapısı GET `/farmer/invitations` ile aynıdır, ancak sadece **Pending** ve **geçerlilik süresi dolmamış** davetiyeler döner.

#### Business Logic

1. **Phone Extraction**: JWT token'daki `ClaimTypes.MobilePhone` claim'i kullanılır
2. **Phone Normalization**: Turkish format handling (+90 vs 0 prefix)
   - `+905551234567` → `05551234567`
   - `905551234567` → `05551234567`
3. **Filtering**: Sadece `Status="Pending"` ve `ExpiryDate > DateTime.Now`
4. **Sorting**: `CreatedDate DESC` (en yeni önce)

#### Error Responses

**400 Bad Request - Phone Not Found**
```json
{
  "data": null,
  "success": false,
  "message": "User phone number not found"
}
```

**400 Bad Request - User Not Found**
```json
{
  "data": null,
  "success": false,
  "message": "User not found"
}
```

---

### 7. Accept Farmer Invitation

**Farmer** rolündeki kullanıcı davetiyeyi kabul eder ve kodlar transfer edilir.

#### Endpoint
```
POST /api/v1/sponsorship/farmer/accept-invitation
```

#### Authorization
- **Roles**: ANY authenticated user (Farmer, Sponsor, Admin, Dealer)
- **Headers**:
  ```
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
| `invitationToken` | string | ✅ Yes | 32-char hex invitation token |

**Not**: `CurrentUserId` ve `CurrentUserPhone` backend tarafından JWT'den otomatik çıkarılır.

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
    "acceptedDate": "2026-01-03T15:45:00"
  },
  "success": true,
  "message": "Invitation accepted successfully. 50 codes assigned."
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `acceptedInvitationId` | int | Kabul edilen davetiye ID |
| `assignedCodes` | array | Transfer edilen kodlar (ilk 10 kod detay, rest sadece count) |
| `assignedCodes[].codeId` | int | Kod ID |
| `assignedCodes[].code` | string | Sponsorluk kodu |
| `assignedCodes[].packageTier` | string | Tier: S, M, L, XL |
| `assignedCodes[].packageName` | string | Paket adı (user-friendly) |
| `totalCodesAssigned` | int | Toplam transfer edilen kod sayısı |
| `sponsorCompanyName` | string | Sponsor şirket adı |
| `acceptedDate` | DateTime | Kabul edilme zamanı |

#### Error Responses

**400 Bad Request - Invalid Token**
```json
{
  "data": null,
  "success": false,
  "message": "Invalid invitation token"
}
```

**400 Bad Request - Expired Invitation**
```json
{
  "data": null,
  "success": false,
  "message": "Invitation has expired"
}
```

**400 Bad Request - Already Accepted**
```json
{
  "data": null,
  "success": false,
  "message": "Invitation already accepted"
}
```

**400 Bad Request - Phone Mismatch**
```json
{
  "data": null,
  "success": false,
  "message": "Phone number does not match invitation"
}
```

**500 Internal Server Error - Code Assignment Failed**
```json
{
  "data": null,
  "success": false,
  "message": "Failed to assign codes. Please contact support."
}
```

---

## 🔐 Authentication & Authorization

### JWT Token Requirements

Tüm endpoint'ler (invitation-details hariç) JWT Bearer token gerektirir.

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Required Claims

```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "123",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "John Doe",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["Sponsor"],
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone": "05551234567"
}
```

### Role-Based Access

| Endpoint | Sponsor | Farmer | Admin | Public |
|----------|---------|--------|-------|--------|
| POST `/farmer/invite` | ✅ | ❌ | ✅ | ❌ |
| POST `/farmer/invitations/bulk` | ✅ | ❌ | ✅ | ❌ |
| POST `/admin/farmer/invitations/bulk` | ❌ | ❌ | ✅ | ❌ |
| GET `/farmer/invitations` | ✅ | ❌ | ✅ | ❌ |
| GET `/farmer/invitation-details` | ✅ | ✅ | ✅ | ✅ |
| GET `/farmer/my-invitations` | ❌ | ✅ | ✅ | ❌ |
| POST `/farmer/accept-invitation` | ✅ | ✅ | ✅ | ❌ |

---

## ✅ Business Logic & Validation

### Phone Normalization

Backend Turkish phone format'ı normalize eder:

```
Input: "+905551234567" → Output: "05551234567"
Input: "905551234567"  → Output: "05551234567"
Input: "05551234567"   → Output: "05551234567"
Input: "0555 123 4567" → Output: "05551234567"
```

**Kurallar**:
1. `+90` prefix → `0` prefix'e çevrilir
2. Boşluk, tire, parantez kaldırılır
3. `90` ile başlayıp 12 hane ise → `0` prefix eklenir

### Code Reservation Logic

Davetiye oluşturulduğunda kodlar **rezerve edilir**:

1. **Tier Filter**: PackageTier belirtildiyse sadece o tier'dan seçilir
2. **Availability Check**: IsAssigned=false ve ReservedForDealerInvitationId=null
3. **Reservation**: `ReservedForFarmerInvitationId` ve `ReservedForFarmerAt` set edilir
4. **Assignment**: Kabul edildiğinde `FarmerInvitationId` ve `AssignedDate` set edilir

### Invitation Expiry

- **Default**: 7 gün (konfigürasyondan değiştirilebilir)
- **Auto-Expire**: Backend cron job ile süresi dolan davetiyeler `Expired` statüsüne çevrilir
- **Check Before Accept**: Kabul edilmeden önce expiry check yapılır

### SMS/WhatsApp Delivery

- **Channel**: `SMS` veya `WhatsApp` seçilebilir
- **Template**: Konfigürasyondan gelen template kullanılır
- **Deep Link**: `https://ziraai.com/farmer-invite/{token}` formatında
- **Retry Logic**: SMS başarısız olursa 3 kez retry yapılır

---

## ⚠️ Error Handling

### Common HTTP Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | Success | İşlem başarılı |
| 400 | Bad Request | Validation hatası, business rule ihlali |
| 401 | Unauthorized | JWT token yok veya geçersiz |
| 403 | Forbidden | Yetki yok (role check failed) |
| 500 | Internal Server Error | Backend exception |

### Error Response Format

Tüm hatalar aynı formatta döner:

```json
{
  "data": null,
  "success": false,
  "message": "Human-readable error message"
}
```

### Validation Errors

#### Phone Validation
```json
{
  "success": false,
  "message": "Invalid phone number format. Expected: 05XXXXXXXXX"
}
```

#### Code Count Validation
```json
{
  "success": false,
  "message": "Code count must be between 1 and 1000"
}
```

#### Tier Validation
```json
{
  "success": false,
  "message": "Invalid package tier. Allowed: S, M, L, XL"
}
```

### Business Logic Errors

#### Insufficient Codes
```json
{
  "success": false,
  "message": "Insufficient available codes. Requested: 50, Available: 30"
}
```

#### Already Accepted
```json
{
  "success": false,
  "message": "Invitation already accepted on 2026-01-02"
}
```

#### Expired Invitation
```json
{
  "success": false,
  "message": "Invitation expired on 2026-01-01"
}
```

---

## 🔄 Integration Workflows

### Workflow 1: Sponsor Sends Individual Invitation

```
1. Sponsor Login
   └─> GET /api/v1/auth/login
       └─> Receive JWT token

2. Create Invitation
   └─> POST /api/v1/sponsorship/farmer/invite
       Request: { phone, farmerName, codeCount, ... }
       └─> Backend:
           ├─> Validate inputs
           ├─> Check available codes
           ├─> Reserve codes (ReservedForFarmerInvitationId)
           ├─> Create invitation record
           ├─> Send SMS with deep link
           └─> Return invitation details

3. View Sent Invitations
   └─> GET /api/v1/sponsorship/farmer/invitations?status=Pending
       └─> Display list in UI
```

### Workflow 2: Sponsor Sends Bulk Invitations

```
1. Sponsor Login
   └─> GET /api/v1/auth/login

2. Prepare Recipients
   └─> Frontend:
       ├─> User uploads Excel file
       ├─> Parse Excel (client-side or server-side)
       └─> Convert to recipients array

3. Send Bulk Invitations
   └─> POST /api/v1/sponsorship/farmer/invitations/bulk
       Request: { recipients: [...], channel: "SMS" }
       └─> Backend:
           ├─> Validate all recipients
           ├─> Process each invitation (parallel)
           ├─> Reserve codes for each
           ├─> Send SMS/WhatsApp
           └─> Return success/failure breakdown

4. Review Results
   └─> Display:
       ├─> Success count: 98
       ├─> Failed count: 2
       └─> Failed items with error messages
```

### Workflow 3: Admin Sends on Behalf of Sponsor

```
1. Admin Login
   └─> GET /api/v1/auth/login (admin credentials)

2. Select Sponsor
   └─> GET /api/v1/admin/sponsors (list sponsors)
       └─> User selects sponsor ID

3. Prepare Recipients
   └─> Same as Workflow 2

4. Send Admin Bulk
   └─> POST /api/v1/sponsorship/admin/farmer/invitations/bulk
       Request: { sponsorId: 123, recipients: [...], adminNotes: "..." }
       └─> Backend:
           ├─> Same as normal bulk
           ├─> PLUS: Log audit trail (adminUserId, IP, notes)
           └─> Return results
```

### Workflow 4: Farmer Views & Accepts Invitation

```
1. Farmer Opens Deep Link
   └─> https://ziraai.com/farmer-invite/abc123...
       └─> Mobile app extracts token

2. View Invitation Details (Before Login)
   └─> GET /api/v1/sponsorship/farmer/invitation-details?token=abc123
       (No auth required)
       └─> Display:
           ├─> Sponsor name
           ├─> Code count
           ├─> Expiry date
           └─> "Login to Accept" button

3. Farmer Login
   └─> GET /api/v1/auth/login
       └─> Receive JWT token

4. Accept Invitation
   └─> POST /api/v1/sponsorship/farmer/accept-invitation
       Request: { invitationToken: "abc123..." }
       └─> Backend:
           ├─> Validate token
           ├─> Check expiry
           ├─> Verify phone match
           ├─> Get reserved codes
           ├─> Assign codes to farmer subscription
           ├─> Update invitation status to "Accepted"
           └─> Return assigned codes

5. View Assigned Codes
   └─> Display success message + code list
```

### Workflow 5: Farmer Checks Pending Invitations

```
1. Farmer Login
   └─> GET /api/v1/auth/login

2. Get My Invitations
   └─> GET /api/v1/sponsorship/farmer/my-invitations
       └─> Backend:
           ├─> Extract phone from JWT
           ├─> Normalize phone
           ├─> Query pending invitations (Status=Pending, not expired)
           └─> Return list

3. Display in UI
   └─> Show:
       ├─> Sponsor name
       ├─> Code count
       ├─> Expiry countdown
       └─> "Accept" button for each

4. Accept Selected Invitation
   └─> POST /api/v1/sponsorship/farmer/accept-invitation
       (Same as Workflow 4, step 4)
```

---

## 🎨 Best Practices

### 1. Token Handling

- **Never expose tokens in logs**: Mask token in frontend logs
- **Deep Link Format**: Always use `https://ziraai.com/farmer-invite/{token}`
- **Token Validation**: Check 32-char hex format before API call

```javascript
// Good
const isValidToken = /^[a-f0-9]{32}$/.test(token);
if (!isValidToken) {
  showError("Invalid invitation link");
  return;
}
```

### 2. Phone Number Input

- **Auto-format**: Format input as user types (`0555 123 4567`)
- **Validation**: Client-side validation before submit
- **Normalization**: Send normalized format to API (`05551234567`)

```javascript
// Good
const normalizePhone = (phone) => {
  return phone.replace(/[\s\-\(\)\+]/g, '');
};

const validatePhone = (phone) => {
  const normalized = normalizePhone(phone);
  return /^(0|90)?5\d{9}$/.test(normalized);
};
```

### 3. Bulk Operations

- **Progress Indicator**: Show upload + processing progress
- **Validation Preview**: Preview recipients before send
- **Error Handling**: Display failed items clearly with retry option
- **Max Recipients**: Enforce 2000 limit client-side

```javascript
// Good
if (recipients.length > 2000) {
  showError("Maximum 2000 recipients allowed. Please split into multiple batches.");
  return;
}
```

### 4. Error Display

- **User-Friendly Messages**: Translate backend errors to Turkish
- **Actionable Errors**: Suggest solutions
- **Error Codes**: Use for programmatic handling

```javascript
// Good
const handleError = (error) => {
  const errorMap = {
    "INVALID_PHONE": "Telefon numarası geçersiz. Lütfen kontrol edin.",
    "INSUFFICIENT_CODES": "Yeterli kod yok. Lütfen paket satın alın.",
    "EXPIRED": "Davetiye süresi dolmuş."
  };

  showError(errorMap[error.code] || error.message);
};
```

### 5. Loading States

- **Show Spinners**: During API calls
- **Disable Buttons**: Prevent double-submit
- **Timeout Handling**: 30-second timeout for bulk operations

```javascript
// Good
const handleSubmit = async () => {
  setLoading(true);
  setButtonDisabled(true);

  try {
    const response = await api.post('/farmer/invite', data);
    handleSuccess(response.data);
  } catch (error) {
    handleError(error);
  } finally {
    setLoading(false);
    setButtonDisabled(false);
  }
};
```

### 6. Date/Time Display

- **Timezone**: Backend döner UTC, frontend local'e çevir
- **Countdown**: Expiry için kalan süre göster (X gün kaldı)
- **Format**: Türkçe format kullan (`03 Ocak 2026, 14:30`)

```javascript
// Good
const formatExpiry = (expiryDate) => {
  const now = new Date();
  const expiry = new Date(expiryDate);
  const diffDays = Math.ceil((expiry - now) / (1000 * 60 * 60 * 24));

  if (diffDays < 0) return "Süresi dolmuş";
  if (diffDays === 0) return "Bugün sona eriyor";
  if (diffDays === 1) return "1 gün kaldı";
  return `${diffDays} gün kaldı`;
};
```

---

## 📊 Testing Checklist

### Individual Invitation
- [ ] Create invitation with valid data
- [ ] Create invitation with invalid phone (expect 400)
- [ ] Create invitation with insufficient codes (expect 400)
- [ ] Create invitation with invalid tier (expect 400)
- [ ] Verify SMS sent successfully
- [ ] Verify codes reserved in database

### Bulk Invitation
- [ ] Upload 100 valid recipients
- [ ] Upload with 1 invalid phone (expect partial success)
- [ ] Upload with >2000 recipients (expect 400)
- [ ] Verify success/failure breakdown
- [ ] Verify all successful SMS sent

### Admin Bulk
- [ ] Admin creates bulk on behalf of sponsor
- [ ] Verify audit log created
- [ ] Verify admin notes saved

### Invitation Viewing
- [ ] Sponsor views all invitations
- [ ] Filter by status (Pending, Accepted, Expired)
- [ ] Verify correct invitations returned

### Invitation Details (Public)
- [ ] Get details with valid token (no auth)
- [ ] Get details with invalid token (expect 400)
- [ ] Verify canAccept flag correct

### Farmer Invitations
- [ ] Farmer views pending invitations
- [ ] Verify only farmer's phone matches
- [ ] Verify expired invitations excluded

### Accept Invitation
- [ ] Accept with valid token
- [ ] Accept with expired token (expect 400)
- [ ] Accept already accepted (expect 400)
- [ ] Accept with wrong phone (expect 400)
- [ ] Verify codes assigned to subscription

---

## 🔧 Troubleshooting

### Problem: SMS Not Delivered

**Check**:
1. Backend logs: `📨 Sponsor X sending farmer invitation via SMS`
2. SMS provider logs
3. Phone number format valid?

**Solution**: Contact backend team to check SMS service status

### Problem: Codes Not Reserved

**Check**:
1. Response contains `reservedCodeIds` array?
2. Database: `SponsorshipCodes` table, `ReservedForFarmerInvitationId` field

**Solution**: Backend bug, report to backend team

### Problem: Acceptance Fails with Phone Mismatch

**Check**:
1. JWT token has correct phone claim?
2. Phone normalization consistent?

**Solution**: Ensure phone format matches (both +90 and 0 supported)

---

**Document Version**: 1.0
**Created**: 2026-01-03
**Author**: Backend Team
**Related Docs**:
- Mobile Integration Guide (next document)
- Dealer Invitations API Reference (comparison reference)
- Sponsorship System Documentation
