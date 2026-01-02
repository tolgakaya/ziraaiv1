# Farmer Invitations API - Complete Reference Guide

**Last Updated**: 2026-01-02
**API Version**: 1.0
**Environment**: Staging (ziraai-api-sit.up.railway.app)

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [REST API Endpoints](#rest-api-endpoints)
4. [Data Models](#data-models)
5. [Error Handling](#error-handling)
6. [Mobile Integration Examples](#mobile-integration-examples)
7. [Testing Guide](#testing-guide)
8. [Comparison with Dealer Invitations](#comparison-with-dealer-invitations)

---

## 🎯 Overview

The Farmer Invitations system provides token-based invitation flow to replace SMS listener functionality (Google Play SDK 35+ compliance).

**Key Features:**
- ✅ Token-based deep linking (no SMS permission required)
- ✅ Phone number verification during acceptance
- ✅ Automatic code assignment to farmers
- ✅ Support for unregistered users (view invitation before signup)
- ✅ 7-day expiration with countdown display
- ✅ Tier-based code allocation (S, M, L, XL)

**System Architecture:**
1. **Sponsor** creates invitation → generates token → SMS/WhatsApp sent
2. **Unregistered User** clicks link → views invitation details (public endpoint)
3. **Farmer** registers/logs in → accepts invitation → codes assigned automatically

---

## 🔐 Authentication

### Endpoint Security Levels

| Endpoint | Authentication | Role Required | Notes |
|----------|----------------|---------------|-------|
| Create Invitation | JWT Required | Sponsor, Admin | Creates invitation |
| List Invitations | JWT Required | Sponsor, Admin | View own invitations |
| Get Invitation Details | **Public** | None | Unregistered users can view |
| Accept Invitation | JWT Required | Farmer, Admin | Phone verification |
| My Pending Invitations | JWT Required | Farmer, Admin | View own pending |

### Required JWT Claims

```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "42",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "Ahmet Yılmaz",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Farmer",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone": "05551234567"
}
```

**Important Claims:**
- `nameidentifier` - User ID
- `mobilephone` - User phone (CRITICAL for acceptance verification)
- `role` - Must include "Farmer", "Sponsor", or "Admin"

### Token Usage

```http
Authorization: Bearer {your_jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

---

## 📡 REST API Endpoints

### 1. Create Farmer Invitation (Sponsor)

Sponsor creates an invitation and reserves codes for a farmer.

#### Endpoint

```
POST /api/v1/sponsorship/farmer-invitations
```

#### Request Headers

```http
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
  "codeCount": 10,
  "packageTier": "M",
  "notes": "Yeni çiftçi - ilk davet"
}
```

#### Request Fields

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `phone` | string | **Yes** | Farmer's phone number | Turkish mobile format |
| `farmerName` | string | No | Farmer's display name | Max 100 chars |
| `email` | string | No | Farmer's email | Valid email format |
| `codeCount` | integer | **Yes** | Number of codes to assign | Min: 1, Max: 100 |
| `packageTier` | string | No | Tier filter: "S", "M", "L", "XL" | If null, any tier |
| `notes` | string | No | Internal notes | Max 500 chars |

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 15,
    "invitationToken": "FARMER-a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4",
    "invitationLink": "https://ziraai.com/farmer-invite/a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4",
    "phone": "05551234567",
    "farmerName": "Ahmet Yılmaz",
    "email": "ahmet@example.com",
    "codeCount": 10,
    "packageTier": "M",
    "status": "Pending",
    "createdAt": "2026-01-02T10:30:00.000Z",
    "expiryDate": "2026-01-09T10:30:00.000Z",
    "smsSent": true,
    "smsDeliveryStatus": "Sent",
    "linkSentDate": "2026-01-02T10:30:01.000Z",
    "linkSentVia": "SMS"
  },
  "success": true,
  "message": "Farmer invitation created successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `invitationId` | integer | Unique invitation ID |
| `invitationToken` | string | 32-char hex token (prefixed with "FARMER-") |
| `invitationLink` | string | Full deep link URL for mobile app |
| `phone` | string | Farmer's phone (normalized) |
| `farmerName` | string | Farmer's name |
| `email` | string | Farmer's email |
| `codeCount` | integer | Number of codes reserved |
| `packageTier` | string | Tier filter or null |
| `status` | string | Always "Pending" on creation |
| `createdAt` | DateTime | ISO 8601 format |
| `expiryDate` | DateTime | ISO 8601 format (CreatedAt + 7 days) |
| `smsSent` | boolean | Whether SMS was sent |
| `smsDeliveryStatus` | string | "Sent", "Failed", "Pending" |
| `linkSentDate` | DateTime | When link was sent |
| `linkSentVia` | string | "SMS", "WhatsApp", "Email" |

#### Business Logic

1. **Code Reservation**:
   - System searches sponsor's available codes
   - If `packageTier` specified → only codes matching tier
   - If `packageTier` null → any available codes
   - Codes sorted by expiry date (FIFO)
   - Reserved codes marked as "Reserved" status

2. **Token Generation**:
   - Format: `FARMER-{Guid.NewGuid().ToString("N")}`
   - Globally unique 32-character hex string
   - Used in deep link: `ziraai://farmer-invite/{token}`

3. **SMS Delivery**:
   - Message: "Sponsor {CompanyName} invited you to ZiraAI. Click: {link}"
   - Automatic delivery via configured SMS provider
   - Delivery status tracked in response

4. **Expiry**:
   - Default: 7 days from creation
   - Configurable via `FARMERINVITATION__TOKENEXPIRYDAYS` env var
   - Expired invitations cannot be accepted

#### Error Responses

**400 Bad Request - Insufficient Codes:**

```json
{
  "data": null,
  "success": false,
  "message": "Sponsor does not have enough available codes (requested: 10, available: 5)"
}
```

**400 Bad Request - Invalid Phone:**

```json
{
  "data": null,
  "success": false,
  "message": "Invalid phone number format"
}
```

**401 Unauthorized:**

```json
{
  "message": "Unauthorized"
}
```

**403 Forbidden - Wrong Role:**

```json
{
  "message": "Forbidden"
}
```

---

### 2. Get Invitation Details (Public - Unregistered Users)

View invitation details before registration/login. **No authentication required.**

#### Endpoint

```
GET /api/v1/sponsorship/farmer-invite/{token}
```

#### Path Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `token` | string | 32-character invitation token (without "FARMER-" prefix) | `a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4` |

#### Request Example

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer-invite/a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4" \
  -H "x-dev-arch-version: 1.0"
```

**Note:** No `Authorization` header required - this is a public endpoint.

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 15,
    "sponsorName": "Tarım Şirketi A.Ş.",
    "codeCount": 10,
    "packageTier": "M",
    "expiresAt": "2026-01-09T10:30:00.000Z",
    "remainingDays": 6,
    "status": "Pending",
    "canAccept": true,
    "message": "This invitation is valid and can be accepted",
    "createdAt": "2026-01-02T10:30:00.000Z"
  },
  "success": true,
  "message": "Invitation details retrieved successfully"
}
```

#### Response Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `invitationId` | integer | Unique invitation ID | 15 |
| `sponsorName` | string | Sponsor company name | "Tarım Şirketi A.Ş." |
| `codeCount` | integer | Number of codes to receive | 10 |
| `packageTier` | string | Tier level or null | "M" or null |
| `expiresAt` | DateTime | Expiry timestamp (ISO 8601) | "2026-01-09T10:30:00.000Z" |
| `remainingDays` | integer | Days until expiry | 6 (can be negative) |
| `status` | string | Invitation status | "Pending", "Accepted", "Expired" |
| `canAccept` | boolean | Whether invitation can be accepted | true/false |
| `message` | string | User-friendly status message | "Valid" or "Expired" |
| `createdAt` | DateTime | Creation timestamp | "2026-01-02T10:30:00.000Z" |

#### Business Logic

1. **Status Validation**:
   - `canAccept = true` if:
     - Status is "Pending"
     - ExpiryDate > DateTime.Now
   - `canAccept = false` if:
     - Status is "Accepted", "Expired", or "Cancelled"
     - ExpiryDate <= DateTime.Now

2. **Remaining Days Calculation**:
   - `remainingDays = (ExpiryDate - DateTime.Now).Days`
   - Can be negative if expired
   - Display in UI: "6 days remaining" or "Expired 2 days ago"

3. **Use Case**:
   - Unregistered user clicks SMS link
   - Sees sponsor name and code count
   - Decides to register/login to accept

#### Error Responses

**404 Not Found - Invalid Token:**

```json
{
  "data": null,
  "success": false,
  "message": "Invitation not found"
}
```

**400 Bad Request - Expired:**

```json
{
  "data": {
    "invitationId": 15,
    "sponsorName": "Tarım Şirketi A.Ş.",
    "codeCount": 10,
    "packageTier": "M",
    "expiresAt": "2025-12-25T10:30:00.000Z",
    "remainingDays": -8,
    "status": "Expired",
    "canAccept": false,
    "message": "This invitation has expired",
    "createdAt": "2025-12-18T10:30:00.000Z"
  },
  "success": false,
  "message": "Invitation has expired"
}
```

---

### 3. Accept Farmer Invitation (Farmer)

Farmer accepts invitation after login. Phone number must match invitation.

#### Endpoint

```
POST /api/v1/sponsorship/farmer-invitations/accept
```

#### Request Headers

```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

#### Request Body

```json
{
  "invitationToken": "a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4"
}
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `invitationToken` | string | **Yes** | 32-char hex token (without "FARMER-" prefix) |

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 15,
    "status": "Accepted",
    "acceptedDate": "2026-01-02T11:00:00.000Z",
    "totalCodesAssigned": 10,
    "sponsorshipCodes": [
      "ZIRA-M-ABC123",
      "ZIRA-M-ABC124",
      "ZIRA-M-ABC125",
      "ZIRA-M-ABC126",
      "ZIRA-M-ABC127",
      "ZIRA-M-ABC128",
      "ZIRA-M-ABC129",
      "ZIRA-M-ABC130",
      "ZIRA-M-ABC131",
      "ZIRA-M-ABC132"
    ],
    "codesByTier": {
      "M": 10
    },
    "message": "Invitation accepted successfully. 10 codes assigned."
  },
  "success": true,
  "message": "Invitation accepted successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `invitationId` | integer | Invitation ID |
| `status` | string | Always "Accepted" on success |
| `acceptedDate` | DateTime | Acceptance timestamp (ISO 8601) |
| `totalCodesAssigned` | integer | Total number of codes transferred |
| `sponsorshipCodes` | array[string] | Actual sponsorship codes assigned |
| `codesByTier` | object | Breakdown by tier: `{ "S": 5, "M": 3, "L": 2 }` |
| `message` | string | Success message with count |

#### Business Logic

1. **Phone Verification**:
   - Extracts phone from JWT claims (`mobilephone`)
   - Normalizes both JWT phone and invitation phone
   - Normalization: `+90 555 123 4567` → `05551234567`
   - Must match EXACTLY after normalization

2. **Status Validation**:
   - Invitation must be "Pending"
   - ExpiryDate > DateTime.Now
   - Not already accepted

3. **Code Assignment**:
   - Finds reserved codes for this invitation
   - Updates code status from "Reserved" to "Active"
   - Assigns codes to farmer's user ID
   - Creates `FarmerSponsorshipCode` records

4. **Atomicity**:
   - All operations in single transaction
   - If any step fails → rollback entire operation
   - No partial code assignments

#### Error Responses

**400 Bad Request - Phone Mismatch:**

```json
{
  "data": null,
  "success": false,
  "message": "Phone number does not match invitation (Expected: 05551234567, Got: 05559876543)"
}
```

**400 Bad Request - Already Accepted:**

```json
{
  "data": null,
  "success": false,
  "message": "Invitation has already been accepted"
}
```

**400 Bad Request - Expired:**

```json
{
  "data": null,
  "success": false,
  "message": "Invitation has expired"
}
```

**404 Not Found - Invalid Token:**

```json
{
  "data": null,
  "success": false,
  "message": "Invitation not found"
}
```

**401 Unauthorized:**

```json
{
  "message": "Unauthorized"
}
```

**403 Forbidden - Wrong Role:**

```json
{
  "message": "Forbidden"
}
```

---

### 4. Get My Pending Invitations (Farmer)

Farmer views all pending invitations sent to their phone number.

#### Endpoint

```
GET /api/v1/sponsorship/farmer/my-invitations
```

#### Request Headers

```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

#### Request Example

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer/my-invitations" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "x-dev-arch-version: 1.0"
```

#### Success Response (200 OK)

**When invitations found:**

```json
{
  "data": [
    {
      "id": 15,
      "phone": "05551234567",
      "farmerName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "status": "Pending",
      "codeCount": 10,
      "packageTier": "M",
      "acceptedByUserId": null,
      "acceptedDate": null,
      "createdDate": "2026-01-02T10:30:00.000Z",
      "expiryDate": "2026-01-09T10:30:00.000Z",
      "linkDelivered": true,
      "linkSentDate": "2026-01-02T10:30:01.000Z"
    },
    {
      "id": 12,
      "phone": "05551234567",
      "farmerName": "Ahmet Yılmaz",
      "email": null,
      "status": "Pending",
      "codeCount": 5,
      "packageTier": null,
      "acceptedByUserId": null,
      "acceptedDate": null,
      "createdDate": "2026-01-01T14:20:00.000Z",
      "expiryDate": "2026-01-08T14:20:00.000Z",
      "linkDelivered": true,
      "linkSentDate": "2026-01-01T14:20:05.000Z"
    }
  ],
  "success": true,
  "message": "2 pending invitation(s) found"
}
```

**When no invitations:**

```json
{
  "data": [],
  "success": true,
  "message": "No pending invitations found"
}
```

#### Response Fields

| Field | Type | Description | Nullable |
|-------|------|-------------|----------|
| `id` | integer | Invitation ID | No |
| `phone` | string | Farmer's phone (normalized) | No |
| `farmerName` | string | Farmer's name | Yes |
| `email` | string | Farmer's email | Yes |
| `status` | string | Always "Pending" in this endpoint | No |
| `codeCount` | integer | Number of codes to receive | No |
| `packageTier` | string | Tier filter or null | Yes |
| `acceptedByUserId` | integer | Always null (not accepted yet) | Yes |
| `acceptedDate` | DateTime | Always null (not accepted yet) | Yes |
| `createdDate` | DateTime | Creation timestamp (ISO 8601) | No |
| `expiryDate` | DateTime | Expiry timestamp (ISO 8601) | No |
| `linkDelivered` | boolean | SMS delivery status | No |
| `linkSentDate` | DateTime | When SMS was sent | Yes |

#### Business Logic

1. **Phone Extraction**:
   - Gets phone from JWT claims (`mobilephone`)
   - Normalizes phone: `+90 555 123 4567` → `05551234567`

2. **Filtering**:
   - Only invitations where:
     - `Phone = normalizedUserPhone`
     - `Status = "Pending"`
     - Not expired (automatic in database query)

3. **Sorting**:
   - Ordered by `CreatedDate` DESC (newest first)

4. **Use Case**:
   - Farmer opens app → sees all pending invitations
   - Can accept multiple invitations from different sponsors
   - Shows total codes available across all invitations

#### Error Responses

**400 Bad Request - No Phone:**

```json
{
  "data": null,
  "success": false,
  "message": "User phone number not found"
}
```

**401 Unauthorized:**

```json
{
  "message": "Unauthorized"
}
```

**403 Forbidden - Wrong Role:**

```json
{
  "message": "Forbidden"
}
```

---

### 5. List Farmer Invitations (Sponsor)

Sponsor views all their sent invitations with filtering.

#### Endpoint

```
GET /api/v1/sponsorship/farmer-invitations?status={status}&page={page}&pageSize={pageSize}
```

#### Request Headers

```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

#### Query Parameters

| Parameter | Type | Required | Description | Default |
|-----------|------|----------|-------------|---------|
| `status` | string | No | Filter by status: "Pending", "Accepted", "Expired", "Cancelled" | All statuses |
| `page` | integer | No | Page number (1-based) | 1 |
| `pageSize` | integer | No | Items per page | 20 |

#### Request Example

```bash
# Get all pending invitations (page 1)
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer-invitations?status=Pending&page=1&pageSize=20" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "x-dev-arch-version: 1.0"
```

#### Success Response (200 OK)

```json
{
  "data": {
    "invitations": [
      {
        "id": 15,
        "phone": "05551234567",
        "farmerName": "Ahmet Yılmaz",
        "email": "ahmet@example.com",
        "status": "Pending",
        "codeCount": 10,
        "packageTier": "M",
        "acceptedByUserId": null,
        "acceptedDate": null,
        "createdDate": "2026-01-02T10:30:00.000Z",
        "expiryDate": "2026-01-09T10:30:00.000Z",
        "linkDelivered": true,
        "linkSentDate": "2026-01-02T10:30:01.000Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "success": true,
  "message": "Found 1 invitation(s)"
}
```

#### Response Fields

Same as "Get My Pending Invitations" but with pagination wrapper.

#### Business Logic

1. **Sponsor Filtering**:
   - Only returns invitations created by authenticated sponsor
   - Uses `SponsorId` from invitation record

2. **Status Filtering**:
   - If `status` parameter provided → filter by exact match
   - If `status` omitted → return all statuses

3. **Pagination**:
   - Total count calculated before pagination
   - Results sliced by `page` and `pageSize`
   - `totalPages = ceiling(totalCount / pageSize)`

---

## 📦 Data Models

### CreateFarmerInvitationDto

```csharp
public class CreateFarmerInvitationDto
{
    public string Phone { get; set; }
    public string FarmerName { get; set; }
    public string Email { get; set; }
    public int CodeCount { get; set; }
    public string PackageTier { get; set; }
    public string Notes { get; set; }
}
```

### FarmerInvitationResponseDto

```csharp
public class FarmerInvitationResponseDto
{
    public int InvitationId { get; set; }
    public string InvitationToken { get; set; }
    public string InvitationLink { get; set; }
    public string Phone { get; set; }
    public string FarmerName { get; set; }
    public string Email { get; set; }
    public int CodeCount { get; set; }
    public string PackageTier { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool SmsSent { get; set; }
    public string SmsDeliveryStatus { get; set; }
    public DateTime? LinkSentDate { get; set; }
    public string LinkSentVia { get; set; }
}
```

### FarmerInvitationDetailDto

```csharp
public class FarmerInvitationDetailDto
{
    public int InvitationId { get; set; }
    public string SponsorName { get; set; }
    public int CodeCount { get; set; }
    public string PackageTier { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RemainingDays { get; set; }
    public string Status { get; set; }
    public bool CanAccept { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### FarmerInvitationAcceptResponseDto

```csharp
public class FarmerInvitationAcceptResponseDto
{
    public int InvitationId { get; set; }
    public string Status { get; set; }
    public DateTime AcceptedDate { get; set; }
    public int TotalCodesAssigned { get; set; }
    public List<string> SponsorshipCodes { get; set; }
    public Dictionary<string, int> CodesByTier { get; set; }
    public string Message { get; set; }
}
```

### FarmerInvitationListDto

```csharp
public class FarmerInvitationListDto
{
    public int Id { get; set; }
    public string Phone { get; set; }
    public string FarmerName { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
    public int CodeCount { get; set; }
    public string PackageTier { get; set; }
    public int? AcceptedByUserId { get; set; }
    public DateTime? AcceptedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool LinkDelivered { get; set; }
    public DateTime? LinkSentDate { get; set; }
}
```

### FarmerInvitationsPaginatedDto

```csharp
public class FarmerInvitationsPaginatedDto
{
    public List<FarmerInvitationListDto> Invitations { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

---

## ⚠️ Error Handling

### REST API Error Handling

```dart
try {
  final response = await dio.post(
    '/api/v1/sponsorship/farmer-invitations/accept',
    data: {'invitationToken': token},
    options: Options(headers: {'Authorization': 'Bearer $jwtToken'}),
  );

  if (response.data['success'] == true) {
    // Success - codes assigned
    final codes = response.data['data']['sponsorshipCodes'];
    showSuccess('${codes.length} kod başarıyla alındı!');
  } else {
    // Business logic error
    showError(response.data['message']);
  }
} on DioException catch (e) {
  if (e.response?.statusCode == 400) {
    final message = e.response?.data['message'] ?? 'Geçersiz istek';
    if (message.contains('Phone number does not match')) {
      showError('Telefon numarası uyuşmuyor');
    } else if (message.contains('already been accepted')) {
      showError('Bu davet zaten kabul edilmiş');
    } else if (message.contains('expired')) {
      showError('Davet süresi dolmuş');
    } else {
      showError(message);
    }
  } else if (e.response?.statusCode == 401) {
    // Token expired - redirect to login
    navigateToLogin();
  } else if (e.response?.statusCode == 404) {
    showError('Davet bulunamadı');
  } else {
    showError('Davet kabul edilemedi');
  }
}
```

---

## 📱 Mobile Integration Examples

### Flutter Example - Complete Flow

```dart
import 'package:dio/dio.dart';

class FarmerInvitationService {
  final Dio _dio;
  final String? _jwtToken;

  FarmerInvitationService(this._dio, this._jwtToken);

  // 1. View invitation details (PUBLIC - before login)
  Future<FarmerInvitationDetailDto?> getInvitationDetails(String token) async {
    try {
      final response = await _dio.get(
        '/api/v1/sponsorship/farmer-invite/$token',
        options: Options(headers: {'x-dev-arch-version': '1.0'}),
      );

      if (response.data['success'] == true) {
        return FarmerInvitationDetailDto.fromJson(response.data['data']);
      }
      return null;
    } catch (e) {
      print('Error fetching invitation details: $e');
      return null;
    }
  }

  // 2. Get my pending invitations (AUTHENTICATED - after login)
  Future<List<FarmerInvitationListDto>> getMyPendingInvitations() async {
    if (_jwtToken == null) throw Exception('Not authenticated');

    try {
      final response = await _dio.get(
        '/api/v1/sponsorship/farmer/my-invitations',
        options: Options(headers: {
          'Authorization': 'Bearer $_jwtToken',
          'x-dev-arch-version': '1.0',
        }),
      );

      if (response.data['success'] == true) {
        final List<dynamic> invitations = response.data['data'];
        return invitations
            .map((json) => FarmerInvitationListDto.fromJson(json))
            .toList();
      }
      return [];
    } catch (e) {
      print('Error fetching pending invitations: $e');
      return [];
    }
  }

  // 3. Accept invitation (AUTHENTICATED)
  Future<FarmerInvitationAcceptResponseDto?> acceptInvitation(
    String token,
  ) async {
    if (_jwtToken == null) throw Exception('Not authenticated');

    try {
      final response = await _dio.post(
        '/api/v1/sponsorship/farmer-invitations/accept',
        data: {'invitationToken': token},
        options: Options(headers: {
          'Authorization': 'Bearer $_jwtToken',
          'x-dev-arch-version': '1.0',
        }),
      );

      if (response.data['success'] == true) {
        return FarmerInvitationAcceptResponseDto.fromJson(
          response.data['data'],
        );
      }
      return null;
    } catch (e) {
      print('Error accepting invitation: $e');
      rethrow;
    }
  }
}

// DTO Models
class FarmerInvitationDetailDto {
  final int invitationId;
  final String sponsorName;
  final int codeCount;
  final String? packageTier;
  final DateTime expiresAt;
  final int remainingDays;
  final String status;
  final bool canAccept;
  final String message;
  final DateTime createdAt;

  FarmerInvitationDetailDto({
    required this.invitationId,
    required this.sponsorName,
    required this.codeCount,
    this.packageTier,
    required this.expiresAt,
    required this.remainingDays,
    required this.status,
    required this.canAccept,
    required this.message,
    required this.createdAt,
  });

  factory FarmerInvitationDetailDto.fromJson(Map<String, dynamic> json) {
    return FarmerInvitationDetailDto(
      invitationId: json['invitationId'],
      sponsorName: json['sponsorName'],
      codeCount: json['codeCount'],
      packageTier: json['packageTier'],
      expiresAt: DateTime.parse(json['expiresAt']),
      remainingDays: json['remainingDays'],
      status: json['status'],
      canAccept: json['canAccept'],
      message: json['message'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}

class FarmerInvitationListDto {
  final int id;
  final String phone;
  final String? farmerName;
  final String? email;
  final String status;
  final int codeCount;
  final String? packageTier;
  final int? acceptedByUserId;
  final DateTime? acceptedDate;
  final DateTime createdDate;
  final DateTime expiryDate;
  final bool linkDelivered;
  final DateTime? linkSentDate;

  FarmerInvitationListDto({
    required this.id,
    required this.phone,
    this.farmerName,
    this.email,
    required this.status,
    required this.codeCount,
    this.packageTier,
    this.acceptedByUserId,
    this.acceptedDate,
    required this.createdDate,
    required this.expiryDate,
    required this.linkDelivered,
    this.linkSentDate,
  });

  factory FarmerInvitationListDto.fromJson(Map<String, dynamic> json) {
    return FarmerInvitationListDto(
      id: json['id'],
      phone: json['phone'],
      farmerName: json['farmerName'],
      email: json['email'],
      status: json['status'],
      codeCount: json['codeCount'],
      packageTier: json['packageTier'],
      acceptedByUserId: json['acceptedByUserId'],
      acceptedDate: json['acceptedDate'] != null
          ? DateTime.parse(json['acceptedDate'])
          : null,
      createdDate: DateTime.parse(json['createdDate']),
      expiryDate: DateTime.parse(json['expiryDate']),
      linkDelivered: json['linkDelivered'],
      linkSentDate: json['linkSentDate'] != null
          ? DateTime.parse(json['linkSentDate'])
          : null,
    );
  }
}

class FarmerInvitationAcceptResponseDto {
  final int invitationId;
  final String status;
  final DateTime acceptedDate;
  final int totalCodesAssigned;
  final List<String> sponsorshipCodes;
  final Map<String, int> codesByTier;
  final String message;

  FarmerInvitationAcceptResponseDto({
    required this.invitationId,
    required this.status,
    required this.acceptedDate,
    required this.totalCodesAssigned,
    required this.sponsorshipCodes,
    required this.codesByTier,
    required this.message,
  });

  factory FarmerInvitationAcceptResponseDto.fromJson(
    Map<String, dynamic> json,
  ) {
    return FarmerInvitationAcceptResponseDto(
      invitationId: json['invitationId'],
      status: json['status'],
      acceptedDate: DateTime.parse(json['acceptedDate']),
      totalCodesAssigned: json['totalCodesAssigned'],
      sponsorshipCodes: List<String>.from(json['sponsorshipCodes']),
      codesByTier: Map<String, int>.from(json['codesByTier']),
      message: json['message'],
    );
  }
}
```

### Deep Link Handling

```dart
// In main.dart or app_router.dart
void setupDeepLinkHandling() {
  // Listen for deep links when app is running
  uriLinkStream.listen((Uri? uri) {
    if (uri != null) {
      handleFarmerInviteLink(uri);
    }
  });

  // Check for deep link when app starts
  getInitialUri().then((Uri? uri) {
    if (uri != null) {
      handleFarmerInviteLink(uri);
    }
  });
}

void handleFarmerInviteLink(Uri uri) {
  // Deep link format: ziraai://farmer-invite/{token}
  // Or web link: https://ziraai.com/farmer-invite/{token}

  if (uri.pathSegments.length >= 2 && uri.pathSegments[0] == 'farmer-invite') {
    final token = uri.pathSegments[1];

    // Navigate to invitation detail screen
    navigateToInvitationDetail(token);
  }
}

Future<void> navigateToInvitationDetail(String token) async {
  // 1. Fetch invitation details (public endpoint - no auth needed)
  final invitation = await farmerInvitationService.getInvitationDetails(token);

  if (invitation == null) {
    showError('Davet bulunamadı');
    return;
  }

  // 2. Check if user is logged in
  final isLoggedIn = await authService.isAuthenticated();

  if (!isLoggedIn) {
    // Show invitation details and prompt to login/register
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => InvitationPreviewScreen(
          invitation: invitation,
          token: token,
          onLoginTap: () => navigateToLogin(returnToken: token),
          onRegisterTap: () => navigateToRegister(returnToken: token),
        ),
      ),
    );
  } else {
    // User is logged in - show accept screen
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => InvitationAcceptScreen(
          invitation: invitation,
          token: token,
        ),
      ),
    );
  }
}
```

---

## 🧪 Testing Guide

### Test Scenario 1: Unregistered User Flow

**Setup:**
1. Sponsor creates invitation for phone: 05551234567
2. SMS sent with deep link

**Test Steps:**
```bash
# 1. Get invitation details (NO AUTH)
TOKEN="a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4"
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer-invite/$TOKEN" \
  -H "x-dev-arch-version: 1.0"

# 2. Verify response
# - sponsorName displayed
# - codeCount shown
# - canAccept: true if not expired
# - remainingDays calculated correctly

# 3. User registers with phone: 05551234567

# 4. User accepts invitation (WITH AUTH)
JWT="user_jwt_token_here"
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer-invitations/accept" \
  -H "Authorization: Bearer $JWT" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{"invitationToken":"'$TOKEN'"}'

# 5. Verify response
# - sponsorshipCodes array contains actual codes
# - totalCodesAssigned matches codeCount
# - status: "Accepted"
```

**Expected Result:**
- ✅ Details visible without authentication
- ✅ Registration possible with invitation phone
- ✅ Acceptance successful after login
- ✅ Codes transferred to farmer account

---

### Test Scenario 2: Get My Pending Invitations

**Setup:**
1. Sponsor A sends invitation to 05551234567 (10 codes, tier M)
2. Sponsor B sends invitation to 05551234567 (5 codes, no tier)
3. Farmer logs in with phone: 05551234567

**Test Steps:**
```bash
# 1. Get JWT token from farmer login
JWT="farmer_jwt_token_here"

# 2. Call my-invitations endpoint
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer/my-invitations" \
  -H "Authorization: Bearer $JWT" \
  -H "x-dev-arch-version: 1.0"

# 3. Verify response
# - 2 invitations returned
# - Total codes: 15 (10 + 5)
# - Both have status: "Pending"
# - Sorted by createdDate DESC
```

**Expected Result:**
- ✅ Both invitations returned
- ✅ Only user's phone matched
- ✅ No expired invitations
- ✅ Newest first ordering

---

### Test Scenario 3: Phone Normalization

**Setup:**
1. Invitation created for phone: `+90 555 123 4567`
2. User registers with phone: `05551234567`

**Test Steps:**
1. Backend normalizes invitation phone: `+90 555 123 4567` → `05551234567`
2. Backend normalizes JWT phone: `05551234567` → `05551234567`
3. Match successful ✅

**Expected Result:**
- ✅ User can see invitation in my-invitations
- ✅ User can accept invitation (phone verification passes)
- ✅ No format mismatch errors

---

### Test Scenario 4: Expiration Handling

**Setup:**
1. Invitation created 8 days ago (expired)
2. Invitation created 2 days ago (valid)

**Test Steps:**
```bash
# 1. Get expired invitation details
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer-invite/expired_token" \
  -H "x-dev-arch-version: 1.0"

# 2. Verify response
# - canAccept: false
# - remainingDays: -1 (negative)
# - message: "This invitation has expired"

# 3. Attempt to accept (should fail)
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/farmer-invitations/accept" \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{"invitationToken":"expired_token"}'

# 4. Verify error
# - 400 Bad Request
# - message: "Invitation has expired"
```

**Expected Result:**
- ✅ Expired invitation visible in details
- ✅ canAccept flag correctly set to false
- ✅ Acceptance blocked by server
- ✅ Clear error message

---

## 🔄 Comparison with Dealer Invitations

### Similarities

| Feature | Farmer Invitations | Dealer Invitations |
|---------|-------------------|-------------------|
| **Authentication** | JWT Bearer required (except public detail endpoint) | JWT Bearer required (except public detail endpoint) |
| **Phone Normalization** | `+90 555 123 4567` → `05551234567` | `+90 555 686 6386` → `905556866386` |
| **Token Format** | 32-character hex string | 32-character hex string |
| **Expiry** | 7 days configurable | 7 days configurable |
| **Status Lifecycle** | Pending → Accepted/Expired/Cancelled | Pending → Accepted/Expired/Cancelled |
| **Deep Linking** | `ziraai://farmer-invite/{token}` | `ziraai://dealer-invite/{token}` |
| **Public Detail Endpoint** | ✅ Yes - unregistered users can view | ✅ Yes - unregistered users can view |

### Key Differences

| Aspect | Farmer Invitations | Dealer Invitations |
|--------|-------------------|-------------------|
| **Real-time Notifications** | ❌ No SignalR (REST-only) | ✅ Yes - SignalR Hub |
| **Phone Normalization Format** | `05551234567` (0 prefix) | `905556866386` (90 prefix) |
| **Token Prefix** | `FARMER-` | `DEALER-` |
| **Target Audience** | Farmers (code recipients) | Dealers (code distributors) |
| **Code Assignment** | Automatic on acceptance | Manual transfer by dealer |
| **Acceptance Verification** | Phone number must match exactly | Email or phone match |
| **Response on Accept** | Returns actual codes + tier breakdown | Returns success status |
| **Tier Support** | ✅ Yes - S, M, L, XL filtering | ✅ Yes - S, M, L, XL filtering |

### Why No SignalR for Farmer Invitations?

**Dealer Invitations:**
- Dealers receive invitations frequently
- Need real-time notifications for immediate response
- Multiple dealers may share email/phone (business accounts)

**Farmer Invitations:**
- Farmers receive invitations less frequently
- Pull-based model sufficient (check on app open)
- Simpler implementation for mobile team
- Phone verification ensures 1-to-1 mapping

---

## 🔧 Troubleshooting

### Issue: Phone verification fails during acceptance

**Check:**
1. JWT token has `mobilephone` claim
2. User's phone matches invitation phone (after normalization)
3. Phone formats: `+90 555 123 4567`, `05551234567`, `0 555 123 45 67`

**Solution:**
```sql
-- Check invitation phone format
SELECT Id, Phone, FarmerName, Status
FROM FarmerInvitations
WHERE Id = 15;

-- Check user phone format
SELECT UserId, MobilePhones
FROM Users
WHERE UserId = 42;

-- Normalization should convert both to: 05551234567
```

---

### Issue: Invitation not found in my-invitations

**Check:**
1. Invitation status is "Pending"
2. Invitation not expired
3. Phone matches exactly after normalization

**Solution:**
```sql
-- Find invitations for phone
SELECT * FROM FarmerInvitations
WHERE Phone = '05551234567'
  AND Status = 'Pending'
  AND ExpiryDate > NOW();
```

---

### Issue: Cannot accept already accepted invitation

**Expected Behavior:**
- Invitations can only be accepted once
- Status changes from "Pending" to "Accepted"
- `AcceptedByUserId` and `AcceptedDate` populated

**Solution:**
- This is intentional - check invitation status before showing "Accept" button
- Use `canAccept` flag from detail endpoint

---

## 📚 Related Documentation

- [Farmer Invitation System - Backend Implementation](./FARMER_INVITATION_SYSTEM_BACKEND_IMPLEMENTATION.md)
- [Dealer Invitations API Reference](../Dealers/DEALER_INVITATIONS_API_COMPLETE_REFERENCE.md)
- [Sponsorship System Complete Documentation](../SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [Environment Configuration Guide](../environment-configuration.md)

---

**Document Version**: 1.0
**Last Updated**: 2026-01-02
**Changes**:
- Initial release
- Complete API reference for all 5 farmer invitation endpoints
- Flutter integration examples with DTO models
- Deep link handling examples
- Comprehensive testing guide
- Comparison with dealer invitation system
