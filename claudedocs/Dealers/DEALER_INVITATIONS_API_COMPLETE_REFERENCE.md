# Dealer Invitations API - Complete Reference Guide

**Last Updated**: 2025-10-31
**API Version**: 1.0
**Environment**: Staging (ziraai-api-sit.up.railway.app)

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [REST API Endpoints](#rest-api-endpoints)
4. [SignalR Real-time Events](#signalr-real-time-events)
5. [Data Models](#data-models)
6. [Error Handling](#error-handling)
7. [Mobile Integration Examples](#mobile-integration-examples)
8. [Testing Guide](#testing-guide)

---

## 🎯 Overview

The Dealer Invitations system provides two complementary features:

1. **REST API**: Query user's pending invitations on-demand
2. **SignalR Hub**: Receive real-time notifications when new invitations are created

**Key Features:**
- ✅ Cross-device support (invitations tied to email/phone, not SMS)
- ✅ Real-time push notifications via SignalR
- ✅ Automatic filtering of expired/accepted invitations
- ✅ Support for both email and phone-based authentication

---

## 🔐 Authentication

Both REST API and SignalR require JWT Bearer token authentication.

### Required JWT Claims

```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "172",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "User 1113",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["Farmer", "Sponsor"],
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone": "05556866386",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "05556866386@phone.ziraai.com"
}
```

**Important Claims:**
- `nameidentifier` - User ID
- `emailaddress` - User email (used for matching invitations)
- `mobilephone` - User phone (used for matching invitations)
- `role` - Must include "Dealer", "Farmer", or "Sponsor"

### Token Usage

```http
Authorization: Bearer {your_jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

---

## 📡 REST API Endpoints

### Get My Pending Invitations

Retrieve all pending dealer invitations for the authenticated user.

#### Endpoint

```
GET /api/v1/sponsorship/dealer/invitations/my-pending
```

#### Request Headers

```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

#### Request Example

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations/my-pending" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json"
```

#### Success Response (200 OK)

**When invitations found:**

```json
{
  "data": {
    "invitations": [
      {
        "invitationId": 8,
        "token": "34290e9b7617451586b206fa298333fa",
        "sponsorCompanyName": "dort tarim",
        "codeCount": 12,
        "packageTier": null,
        "expiresAt": "2025-11-07T08:46:10.617",
        "remainingDays": 6,
        "status": "Pending",
        "dealerEmail": "05556866386@phone.ziraai.com",
        "dealerPhone": "+905556866386",
        "createdAt": "2025-10-31T08:46:10.617"
      }
    ],
    "totalCount": 1
  },
  "success": true,
  "message": "Found 1 pending invitation(s)"
}
```

**When no invitations:**

```json
{
  "data": {
    "invitations": [],
    "totalCount": 0
  },
  "success": true,
  "message": "Found 0 pending invitation(s)"
}
```

#### Response Fields

| Field | Type | Description | Nullable |
|-------|------|-------------|----------|
| `invitationId` | integer | Unique invitation ID | No |
| `token` | string | 32-character hex token (use as `DEALER-{token}` for deep link) | No |
| `sponsorCompanyName` | string | Company name of inviting sponsor | No |
| `codeCount` | integer | Number of sponsorship codes to be transferred | No |
| `packageTier` | string | Tier filter: "S", "M", "L", "XL" or null (any tier) | Yes |
| `expiresAt` | DateTime | ISO 8601 format - invitation expiry date | No |
| `remainingDays` | integer | Days until expiry (can be negative if expired) | No |
| `status` | string | Always "Pending" in this endpoint | No |
| `dealerEmail` | string | Email where invitation was sent | Yes |
| `dealerPhone` | string | Phone where invitation was sent (international format) | Yes |
| `createdAt` | DateTime | ISO 8601 format - invitation creation date | No |

#### Business Logic

1. **Filtering**: Returns only invitations where:
   - `Status = "Pending"`
   - `ExpiryDate > DateTime.Now`
   - Matches user's email OR phone from JWT

2. **Matching Logic**:
   - If user has email in JWT → matches `dealerEmail`
   - If user has phone in JWT → matches `dealerPhone`
   - If user has both → matches either field

3. **Sorting**: Results ordered by `expiresAt` ASC (expiring soon first)

#### Error Responses

**400 Bad Request - No Email/Phone in JWT:**

```json
{
  "data": null,
  "success": false,
  "message": "Email veya telefon bilgisi bulunamadı"
}
```

**401 Unauthorized:**

```json
{
  "message": "Unauthorized"
}
```

**403 Forbidden - Invalid Role:**

```json
{
  "message": "Forbidden"
}
```

**500 Internal Server Error:**

```json
{
  "data": null,
  "success": false,
  "message": "Bekleyen davetiyeler alınırken hata oluştu"
}
```

---

## 🔔 SignalR Real-time Events

### Connection Setup

#### Hub URL

```
wss://ziraai-api-sit.up.railway.app/hubs/notification
```

**Production:**
```
wss://api.ziraai.com/hubs/notification
```

#### Connection Requirements

- ✅ JWT Bearer token authentication
- ✅ Automatic reconnection enabled
- ✅ WebSocket transport (fallback to long polling)

#### Connection Flow

1. Client connects with JWT token
2. Server extracts email and phone from JWT claims
3. Server automatically adds client to groups:
   - `email_{email}` if email exists
   - `phone_{normalizedPhone}` if phone exists
4. Connection established - ready to receive events

### Event: NewDealerInvitation

Sent when a sponsor creates a new dealer invitation.

#### Event Name

```
NewDealerInvitation
```

#### Event Payload

```json
{
  "invitationId": 8,
  "token": "34290e9b7617451586b206fa298333fa",
  "sponsorCompanyName": "dort tarim",
  "codeCount": 12,
  "packageTier": null,
  "expiresAt": "2025-11-07T08:46:10.617",
  "remainingDays": 6,
  "status": "Pending",
  "dealerEmail": "05556866386@phone.ziraai.com",
  "dealerPhone": "+905556866386",
  "createdAt": "2025-10-31T08:46:10.617"
}
```

#### Payload Fields

Same structure as REST API response - see [Response Fields](#response-fields) above.

#### Group Targeting

SignalR sends notifications to two groups:

1. **Email Group**: `email_{email}`
   - Example: `email_user@example.com`
   - Matched with `dealerEmail` field

2. **Phone Group**: `phone_{normalizedPhone}`
   - Example: `phone_905556866386`
   - Normalization: Remove `+`, `-`, `(`, `)`, spaces
   - Example: `+90 555 686 6386` → `phone_905556866386`

**Important**: Phone normalization is synchronized between Hub and Notification Service (fixed in commit 8261ffb).

### Hub Methods

#### Ping

Keep connection alive and test connectivity.

```javascript
await connection.invoke("Ping");
```

Returns: `void` (no response, just keeps connection alive)

---

## 📦 Data Models

### DealerInvitationSummaryDto

```csharp
public class DealerInvitationSummaryDto
{
    public int InvitationId { get; set; }
    public string Token { get; set; }
    public string SponsorCompanyName { get; set; }
    public int CodeCount { get; set; }
    public string PackageTier { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RemainingDays { get; set; }
    public string Status { get; set; }
    public string DealerEmail { get; set; }
    public string DealerPhone { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### PendingInvitationsResponseDto

```csharp
public class PendingInvitationsResponseDto
{
    public List<DealerInvitationSummaryDto> Invitations { get; set; }
    public int TotalCount { get; set; }
}
```

---

## ⚠️ Error Handling

### REST API Error Handling

```dart
try {
  final response = await dio.get(
    '/api/v1/sponsorship/dealer/invitations/my-pending',
    options: Options(headers: {'Authorization': 'Bearer $token'}),
  );

  if (response.data['success'] == true) {
    // Handle success
    final invitations = response.data['data']['invitations'];
  } else {
    // Handle business logic error
    showError(response.data['message']);
  }
} on DioException catch (e) {
  if (e.response?.statusCode == 401) {
    // Token expired - redirect to login
    navigateToLogin();
  } else if (e.response?.statusCode == 403) {
    // Insufficient permissions
    showError('Yetkisiz erişim');
  } else {
    // Other errors
    showError('Bekleyen davetiyeler alınamadı');
  }
}
```

### SignalR Error Handling

```dart
_hubConnection!.onclose((error) {
  if (error != null) {
    print('SignalR connection closed with error: $error');
    // Attempt reconnection or show offline UI
  }
});

_hubConnection!.onreconnecting((error) {
  print('SignalR reconnecting...');
  // Show "Reconnecting..." UI
});

_hubConnection!.onreconnected((connectionId) {
  print('SignalR reconnected');
  // Refresh data if needed
});
```

---

## 📱 Mobile Integration Examples

### Flutter Example

```dart
import 'package:dio/dio.dart';
import 'package:signalr_netcore/signalr_client.dart';

class DealerInvitationService {
  final Dio _dio;
  final String _jwtToken;
  HubConnection? _hubConnection;

  DealerInvitationService(this._dio, this._jwtToken);

  // REST API: Get pending invitations
  Future<List<DealerInvitationSummaryDto>> getPendingInvitations() async {
    try {
      final response = await _dio.get(
        '/api/v1/sponsorship/dealer/invitations/my-pending',
        options: Options(headers: {
          'Authorization': 'Bearer $_jwtToken',
          'x-dev-arch-version': '1.0',
        }),
      );

      if (response.data['success'] == true) {
        final List<dynamic> invitations =
            response.data['data']['invitations'];
        return invitations
            .map((json) => DealerInvitationSummaryDto.fromJson(json))
            .toList();
      }
      return [];
    } catch (e) {
      print('Error fetching invitations: $e');
      return [];
    }
  }

  // SignalR: Connect and listen
  Future<void> connectSignalR() async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          'https://ziraai-api-sit.up.railway.app/hubs/notification',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => _jwtToken,
            logging: (level, message) => print('SignalR: $message'),
          ),
        )
        .withAutomaticReconnect()
        .build();

    // Listen for new invitations
    _hubConnection!.on('NewDealerInvitation', _handleNewInvitation);

    // Start connection
    await _hubConnection!.start();
    print('✅ SignalR connected');
  }

  void _handleNewInvitation(List<Object>? arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;
    final invitation = DealerInvitationSummaryDto.fromJson(data);

    print('📩 New invitation from ${invitation.sponsorCompanyName}');

    // Show local notification
    _showNotification(invitation);

    // Refresh UI
    _refreshInvitationsList();
  }

  void _showNotification(DealerInvitationSummaryDto invitation) {
    // Implement local notification
    // Use flutter_local_notifications package
  }

  void _refreshInvitationsList() {
    // Trigger UI refresh or state management update
  }

  Future<void> disconnect() async {
    await _hubConnection?.stop();
  }
}

// DTO Model
class DealerInvitationSummaryDto {
  final int invitationId;
  final String token;
  final String sponsorCompanyName;
  final int codeCount;
  final String? packageTier;
  final DateTime expiresAt;
  final int remainingDays;
  final String status;
  final String? dealerEmail;
  final String? dealerPhone;
  final DateTime createdAt;

  DealerInvitationSummaryDto({
    required this.invitationId,
    required this.token,
    required this.sponsorCompanyName,
    required this.codeCount,
    this.packageTier,
    required this.expiresAt,
    required this.remainingDays,
    required this.status,
    this.dealerEmail,
    this.dealerPhone,
    required this.createdAt,
  });

  factory DealerInvitationSummaryDto.fromJson(Map<String, dynamic> json) {
    return DealerInvitationSummaryDto(
      invitationId: json['invitationId'],
      token: json['token'],
      sponsorCompanyName: json['sponsorCompanyName'],
      codeCount: json['codeCount'],
      packageTier: json['packageTier'],
      expiresAt: DateTime.parse(json['expiresAt']),
      remainingDays: json['remainingDays'],
      status: json['status'],
      dealerEmail: json['dealerEmail'],
      dealerPhone: json['dealerPhone'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}
```

---

## 🧪 Testing Guide

### Test Scenario 1: Get Pending Invitations

**Setup:**
1. Login as user with email/phone
2. Ensure at least one pending invitation exists

**Test Steps:**
```bash
# 1. Get JWT token from login
TOKEN="your_jwt_token_here"

# 2. Call my-pending endpoint
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations/my-pending" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0"

# 3. Verify response
# - success: true
# - invitations array contains pending invitations
# - totalCount matches array length
```

**Expected Result:**
- Status: 200 OK
- Response contains pending invitations
- Only invitations matching user email/phone
- Only non-expired pending invitations

### Test Scenario 2: SignalR Real-time Notification

**Setup:**
1. Mobile app connected to SignalR hub
2. User A logged in (phone: +905556866386)

**Test Steps:**
1. Mobile: Connect to `/hubs/notification` with JWT
2. Mobile: Verify connection in logs: `📡 SignalR NotificationHub connected`
3. Backend: Sponsor sends invitation to +905556866386
4. Mobile: Listen for `NewDealerInvitation` event
5. Mobile: Verify event received with correct data

**Expected Result:**
- Mobile receives notification immediately
- Event payload matches invitation data
- No notification if phone/email doesn't match

### Test Scenario 3: Phone Normalization

**Setup:**
1. User phone in JWT: `05556866386`
2. Invitation sent to: `+905556866386`

**Test Steps:**
1. Backend normalizes both to: `905556866386`
2. User added to group: `phone_905556866386`
3. Notification sent to: `phone_905556866386`
4. Match successful ✅

**Expected Result:**
- User receives notification despite format difference
- No duplicate notifications

---

## 📊 Backend Logs Reference

### Successful Invitation Creation with SignalR

```log
2025-10-31 08:46:10.273 [INF] 📨 Sponsor 159 sending dealer invitation via SMS to +905556866386
2025-10-31 08:46:10.617 [INF] ✅ Created invitation 8 with token 34290e9b7617451586b206fa298333fa
2025-10-31 08:46:10.619 [INF] 📣 Sending SignalR notification for dealer invitation 8
2025-10-31 08:46:10.651 [INF] 📧 Sending to email group: email_05556866386@phone.ziraai.com
2025-10-31 08:46:10.654 [INF] 📱 Sending to phone group: phone_905556866386
2025-10-31 08:46:10.655 [INF] ✅ SignalR notification sent successfully for invitation 8
```

### Successful SignalR Connection

```log
2025-10-31 08:46:05.123 [INF] 📡 SignalR NotificationHub connected - UserId: 172, Email: 05556866386@phone.ziraai.com, Phone: 05556866386, ConnectionId: abc123
2025-10-31 08:46:05.125 [INF] ✅ User 172 added to group: email_05556866386@phone.ziraai.com
2025-10-31 08:46:05.127 [INF] ✅ User 172 added to group: phone_905556866386
```

---

## 🔧 Troubleshooting

### Issue: No invitations returned from API

**Check:**
1. JWT token has email or phone claim
2. User's email/phone matches invitation email/phone
3. Invitations are not expired
4. Invitations have status "Pending"

**Solution:**
```sql
-- Check invitations in database
SELECT * FROM DealerInvitations
WHERE (Email = 'user@email.com' OR Phone = '+905556866386')
  AND Status = 'Pending'
  AND ExpiryDate > NOW();
```

### Issue: SignalR notification not received

**Check:**
1. SignalR connection established (check logs)
2. User added to correct groups (check connection logs)
3. Phone normalization matching (905556866386 vs 05556866386)
4. Notification sent to correct group (check backend logs)

**Solution:**
- Verify phone normalization in logs: `phone_905556866386`
- Both Hub and NotificationService should use same normalization
- Fixed in commit 8261ffb

### Issue: 401 Unauthorized

**Check:**
1. JWT token not expired
2. Token includes in Authorization header
3. Token format: `Bearer {token}`

**Solution:**
```bash
# Check token expiry
echo $TOKEN | base64 -d | jq .exp
```

---

## 📚 Related Documentation

- [Backend Requirements - SignalR Solution](./BACKEND_REQUIREMENTS_DEALER_INVITATIONS_SIGNALR.md)
- [Mobile Integration Guide](./MOBILE_INTEGRATION_DEALER_INVITATIONS.md)
- [Sponsorship System Complete Documentation](../SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)

---

**Document Version**: 2.0
**Last Updated**: 2025-10-31
**Changes**:
- Fixed phone normalization sync (commit 8261ffb)
- Added complete API reference with real examples
- Added SignalR event documentation
- Added Flutter integration examples
- Added comprehensive troubleshooting guide
