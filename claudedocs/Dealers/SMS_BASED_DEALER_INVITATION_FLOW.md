# SMS-Based Dealer Invitation Flow

**Version:** 1.0
**Date:** 2025-01-25
**Status:** ✅ Implemented

## Overview

SMS-based dealer invitation system allows sponsors to invite dealers via SMS/WhatsApp with automatic code transfer upon acceptance. The system follows the same patterns as the referral and send-link redemption flows.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Flow Diagram](#flow-diagram)
3. [API Endpoints](#api-endpoints)
4. [SMS Message Format](#sms-message-format)
5. [Deep Link Handling](#deep-link-handling)
6. [Mobile App Integration](#mobile-app-integration)
7. [Database Schema](#database-schema)
8. [Configuration](#configuration)
9. [Testing Guide](#testing-guide)
10. [Error Handling](#error-handling)

---

## Architecture Overview

### Components

```
┌─────────────────┐
│   Sponsor App   │ (Authenticated)
└────────┬────────┘
         │ 1. POST /dealer/invite-via-sms
         ▼
┌─────────────────────────────────────┐
│  InviteDealerViaSmsCommand          │
│  - Validates available codes        │
│  - Creates DealerInvitation         │
│  - Generates deep link              │
│  - Sends SMS via MessagingFactory   │
└────────┬────────────────────────────┘
         │ 2. SMS with DEALER-{token}
         ▼
┌─────────────────┐
│  Dealer Phone   │
└────────┬────────┘
         │ 3a. App installed → Deep link opens app
         │ 3b. App not installed → Install from Play Store → SMS reading
         ▼
┌─────────────────────────────────────┐
│  Mobile App (Flutter)               │
│  - Reads SMS (if permission granted)│
│  - Extracts DEALER-{token}          │
│  - User login/register              │
└────────┬────────────────────────────┘
         │ 4. GET /dealer/invitation-details?token={token}
         ▼
┌─────────────────────────────────────┐
│  GetDealerInvitationDetailsQuery    │
│  - No auth required (public)        │
│  - Returns sponsor info, code count │
└────────┬────────────────────────────┘
         │ 5. User accepts invitation
         ▼
┌─────────────────────────────────────┐
│  AcceptDealerInvitationCommand      │
│  - Validates token & email match    │
│  - Assigns Sponsor role if needed   │
│  - Transfers codes to dealer        │
│  - Updates invitation status        │
└─────────────────────────────────────┘
```

---

## Flow Diagram

### Scenario A: App Already Installed

```
Sponsor → API → SMS → Dealer Phone → Deep Link → App Opens
                                                      ↓
                                      [Login/Register if needed]
                                                      ↓
                                        GET /invitation-details
                                                      ↓
                                          [Accept Invitation]
                                                      ↓
                                      POST /accept-invitation
                                                      ↓
                                        ✅ Codes Transferred
```

### Scenario B: App Not Installed

```
Sponsor → API → SMS → Dealer Phone → [Click Play Store Link]
                                                      ↓
                                          [Install App]
                                                      ↓
                                      [App reads SMS on first launch]
                                                      ↓
                                       [Auto-extracts DEALER-{token}]
                                                      ↓
                                         [User registers/login]
                                                      ↓
                                     GET /invitation-details (auto)
                                                      ↓
                                       [Show invitation screen]
                                                      ↓
                                      POST /accept-invitation
                                                      ↓
                                        ✅ Codes Transferred
```

---

## API Endpoints

### 1. POST /api/Sponsorship/dealer/invite-via-sms

**Auth:** Required (Sponsor or Admin role)
**Purpose:** Create dealer invitation and send SMS

#### Request Body

```json
{
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "ABC Tarım Bayii",
  "purchaseId": 26,
  "codeCount": 10
}
```

#### Response (Success)

```json
{
  "data": {
    "invitationId": 15,
    "invitationToken": "a1b2c3d4e5f67890a1b2c3d4e5f67890",
    "invitationLink": "https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890",
    "email": "dealer@example.com",
    "phone": "+905551234567",
    "dealerName": "ABC Tarım Bayii",
    "codeCount": 10,
    "status": "Pending",
    "invitationType": "Invite",
    "createdAt": "2025-01-25T10:30:00",
    "expiryDate": "2025-02-01T10:30:00",
    "smsSent": true,
    "smsDeliveryStatus": "Sent"
  },
  "success": true,
  "message": "📱 Bayilik daveti +905551234567 numarasına SMS ile gönderildi"
}
```

#### Response (SMS Failed)

```json
{
  "data": {
    ...
    "smsSent": false,
    "smsDeliveryStatus": "Failed"
  },
  "success": true,
  "message": "⚠️ Davetiye oluşturuldu ancak SMS gönderilemedi. Linki manuel olarak iletebilirsiniz: https://..."
}
```

#### Error Responses

```json
// Insufficient codes
{
  "success": false,
  "message": "Yetersiz kod. Mevcut: 5, İstenen: 10"
}

// Unauthorized
{
  "success": false,
  "message": "Unauthorized"
}
```

---

### 2. GET /api/Sponsorship/dealer/invitation-details

**Auth:** None (Public endpoint, token-only validation)
**Purpose:** Get invitation details before login/acceptance

#### Query Parameters

- `token` (required): The invitation token from SMS

#### Request Example

```
GET /api/Sponsorship/dealer/invitation-details?token=a1b2c3d4e5f67890a1b2c3d4e5f67890
```

#### Response (Success)

```json
{
  "data": {
    "invitationId": 15,
    "sponsorCompanyName": "ABC Tarım A.Ş.",
    "codeCount": 10,
    "packageTier": "M",
    "expiresAt": "2025-02-01T10:30:00",
    "remainingDays": 6,
    "status": "Pending",
    "invitationMessage": "🎉 ABC Tarım A.Ş. sizi bayilik ağına katılmaya davet ediyor!",
    "dealerEmail": "dealer@example.com",
    "dealerPhone": "+905551234567",
    "createdAt": "2025-01-25T10:30:00"
  },
  "success": true,
  "message": "Davetiye bilgileri başarıyla alındı"
}
```

#### Error Responses

```json
// Token not found
{
  "success": false,
  "message": "Davetiye bulunamadı"
}

// Already accepted
{
  "success": false,
  "message": "Bu davetiye daha önce kabul edilmiş"
}

// Expired
{
  "success": false,
  "message": "Davetiyenin süresi dolmuş. Lütfen sponsor ile iletişime geçin"
}

// Rejected
{
  "success": false,
  "message": "Bu davetiye reddedilmiş"
}
```

---

### 3. POST /api/Sponsorship/dealer/accept-invitation

**Auth:** Required (Any authenticated user)
**Purpose:** Accept invitation and transfer codes to dealer

#### Request Body

```json
{
  "invitationToken": "a1b2c3d4e5f67890a1b2c3d4e5f67890"
}
```

**Note:** `CurrentUserId` and `CurrentUserEmail` are automatically extracted from JWT claims.

#### Response (Success)

```json
{
  "data": {
    "invitationId": 15,
    "dealerId": 158,
    "transferredCodeCount": 10,
    "transferredCodeIds": [945, 946, 947, 948, 949, 950, 951, 952, 953, 954],
    "acceptedAt": "2025-01-25T11:00:00",
    "message": "✅ Tebrikler! 10 adet kod hesabınıza transfer edildi. Artık bu kodları çiftçilere dağıtabilirsiniz."
  },
  "success": true,
  "message": "Bayilik daveti başarıyla kabul edildi"
}
```

#### Error Responses

```json
// Email mismatch (security check)
{
  "success": false,
  "message": "Bu davetiye size ait değil"
}

// Already accepted/expired
{
  "success": false,
  "message": "Davetiye bulunamadı veya daha önce kabul edilmiş/reddedilmiş"
}

// Insufficient codes
{
  "success": false,
  "message": "Yetersiz kod. İstenen: 10, Mevcut: 5"
}
```

---

## SMS Message Format

### Template Structure

```
🎁 {sponsorName} Bayilik Daveti!

Davet Kodunuz: DEALER-{token}

Hemen katılmak için tıklayın:
{deepLink}

Veya uygulamayı indirin:
{playStoreLink}
```

### Example SMS

```
🎁 ABC Tarım A.Ş. Bayilik Daveti!

Davet Kodunuz: DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890

Hemen katılmak için tıklayın:
https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app.staging
```

### Token Format

- **Pattern:** `DEALER-{32-character-hex-token}`
- **Example:** `DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890`
- **Purpose:** Easy to extract via SMS reading permission
- **Validity:** 7 days (configurable)

### Regex for Mobile App

```dart
// Flutter regex to extract dealer token from SMS
final dealerTokenRegex = RegExp(r'DEALER-([a-f0-9]{32})');

String? extractDealerToken(String smsBody) {
  final match = dealerTokenRegex.firstMatch(smsBody);
  return match?.group(1); // Returns the 32-char token without "DEALER-" prefix
}
```

---

## Deep Link Handling

### URL Structure

```
{baseUrl}/dealer-invitation/DEALER-{token}
```

### Environment-Specific URLs

| Environment | Base URL | Example |
|------------|----------|---------|
| Development | `https://localhost:5001/dealer-invitation/` | `https://localhost:5001/dealer-invitation/DEALER-abc123...` |
| Staging | `https://ziraai-api-sit.up.railway.app/dealer-invitation/` | `https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-abc123...` |
| Production | `https://ziraai.com/dealer-invitation/` | `https://ziraai.com/dealer-invitation/DEALER-abc123...` |

### Android Deep Link Configuration

```xml
<!-- AndroidManifest.xml -->
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />

    <!-- Staging -->
    <data
        android:scheme="https"
        android:host="ziraai-api-sit.up.railway.app"
        android:pathPrefix="/dealer-invitation/" />

    <!-- Production -->
    <data
        android:scheme="https"
        android:host="ziraai.com"
        android:pathPrefix="/dealer-invitation/" />

    <!-- App-specific scheme (fallback) -->
    <data
        android:scheme="ziraai"
        android:host="dealer-invitation" />
</intent-filter>
```

### iOS Universal Link Configuration

```json
// apple-app-site-association
{
  "applinks": {
    "apps": [],
    "details": [
      {
        "appID": "TEAM_ID.com.ziraai.app",
        "paths": ["/dealer-invitation/*"]
      }
    ]
  }
}
```

---

## Mobile App Integration

### Step 1: SMS Reading Permission (Android)

```dart
// pubspec.yaml
dependencies:
  telephony: ^0.2.0

// Request permission
import 'package:telephony/telephony.dart';

final telephony = Telephony.instance;

Future<void> requestSmsPermission() async {
  bool? permissionGranted = await telephony.requestPhoneAndSmsPermissions;
  if (permissionGranted == true) {
    // Listen for incoming SMS
    telephony.listenIncomingSms(
      onNewMessage: onSmsReceived,
      listenInBackground: false,
    );
  }
}

void onSmsReceived(SmsMessage message) {
  final body = message.body;

  // Extract dealer token
  final dealerTokenRegex = RegExp(r'DEALER-([a-f0-9]{32})');
  final match = dealerTokenRegex.firstMatch(body ?? '');

  if (match != null) {
    final token = match.group(1);
    // Store token for later use after login
    _storePendingDealerInvitation(token);
  }
}
```

### Step 2: Deep Link Handling

```dart
// main.dart
import 'package:uni_links/uni_links.dart';

class MyApp extends StatefulWidget {
  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  StreamSubscription? _linkSubscription;

  @override
  void initState() {
    super.initState();
    _initDeepLinkListener();
    _handleInitialLink();
  }

  Future<void> _initDeepLinkListener() async {
    _linkSubscription = linkStream.listen((String? link) {
      if (link != null) {
        _handleDeepLink(link);
      }
    });
  }

  Future<void> _handleInitialLink() async {
    try {
      final initialLink = await getInitialLink();
      if (initialLink != null) {
        _handleDeepLink(initialLink);
      }
    } catch (e) {
      print('Error handling initial link: $e');
    }
  }

  void _handleDeepLink(String link) {
    // Extract token from deep link
    // Example: https://ziraai.com/dealer-invitation/DEALER-abc123...
    final uri = Uri.parse(link);

    if (uri.path.startsWith('/dealer-invitation/')) {
      final fullToken = uri.path.split('/').last; // DEALER-abc123...

      if (fullToken.startsWith('DEALER-')) {
        final token = fullToken.substring(7); // Remove "DEALER-" prefix

        // Check if user is logged in
        if (_isUserLoggedIn()) {
          _navigateToDealerInvitation(token);
        } else {
          // Store token and show login screen
          _storePendingDealerInvitation(token);
          _navigateToLogin();
        }
      }
    }
  }

  @override
  void dispose() {
    _linkSubscription?.cancel();
    super.dispose();
  }
}
```

### Step 3: Invitation Details Screen

```dart
class DealerInvitationScreen extends StatefulWidget {
  final String token;

  const DealerInvitationScreen({required this.token});

  @override
  _DealerInvitationScreenState createState() => _DealerInvitationScreenState();
}

class _DealerInvitationScreenState extends State<DealerInvitationScreen> {
  bool _isLoading = true;
  DealerInvitationDetails? _invitationDetails;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadInvitationDetails();
  }

  Future<void> _loadInvitationDetails() async {
    try {
      // Call public endpoint (no auth required)
      final response = await http.get(
        Uri.parse('$API_BASE_URL/api/Sponsorship/dealer/invitation-details?token=${widget.token}'),
        headers: {'x-dev-arch-version': '1.0'},
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        if (data['success']) {
          setState(() {
            _invitationDetails = DealerInvitationDetails.fromJson(data['data']);
            _isLoading = false;
          });
        } else {
          setState(() {
            _errorMessage = data['message'];
            _isLoading = false;
          });
        }
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'Davetiye bilgileri alınamadı';
        _isLoading = false;
      });
    }
  }

  Future<void> _acceptInvitation() async {
    try {
      final token = await _getAuthToken(); // Get JWT token

      final response = await http.post(
        Uri.parse('$API_BASE_URL/api/Sponsorship/dealer/accept-invitation'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
          'x-dev-arch-version': '1.0',
        },
        body: jsonEncode({
          'invitationToken': widget.token,
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        if (data['success']) {
          // Show success message
          _showSuccessDialog(data['data']);
        } else {
          _showErrorDialog(data['message']);
        }
      }
    } catch (e) {
      _showErrorDialog('Davetiye kabul edilemedi');
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (_errorMessage != null) {
      return Scaffold(
        appBar: AppBar(title: Text('Bayilik Daveti')),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.error_outline, size: 64, color: Colors.red),
              SizedBox(height: 16),
              Text(_errorMessage!, textAlign: TextAlign.center),
            ],
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: Text('Bayilik Daveti')),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(_invitationDetails!.invitationMessage,
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
            SizedBox(height: 24),

            _buildInfoCard('Sponsor', _invitationDetails!.sponsorCompanyName),
            _buildInfoCard('Kod Adedi', '${_invitationDetails!.codeCount} adet'),
            _buildInfoCard('Paket', _invitationDetails!.packageTier),
            _buildInfoCard('Son Geçerlilik',
                '${_invitationDetails!.remainingDays} gün kaldı'),

            Spacer(),

            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _acceptInvitation,
                child: Text('Daveti Kabul Et'),
                style: ElevatedButton.styleFrom(
                  padding: EdgeInsets.symmetric(vertical: 16),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoCard(String label, String value) {
    return Card(
      margin: EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label, style: TextStyle(color: Colors.grey[600])),
            Text(value, style: TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
      ),
    );
  }
}
```

---

## Database Schema

### DealerInvitations Table

```sql
CREATE TABLE public."DealerInvitations" (
    "Id" serial4 NOT NULL,
    "SponsorId" int4 NOT NULL,
    "Email" varchar(255) NULL,
    "Phone" varchar(20) NULL,
    "DealerName" varchar(255) NOT NULL,
    "Status" varchar(50) DEFAULT 'Pending'::character varying NOT NULL,
    "InvitationType" varchar(50) NOT NULL,
    "InvitationToken" varchar(255) NULL,
    "PurchaseId" int4 NOT NULL,
    "CodeCount" int4 NOT NULL,
    "CreatedDealerId" int4 NULL,
    "AcceptedDate" timestamp NULL,
    "AutoCreatedPassword" varchar(255) NULL,

    -- SMS Tracking Fields (NEW)
    "LinkSentDate" timestamp NULL,
    "LinkSentVia" varchar(50) NULL,
    "LinkDelivered" boolean NOT NULL DEFAULT false,

    "CreatedDate" timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "ExpiryDate" timestamp NOT NULL,
    "CancelledDate" timestamp NULL,
    "CancelledByUserId" int4 NULL,
    "Notes" text NULL,

    CONSTRAINT "DealerInvitations_pkey" PRIMARY KEY ("Id"),
    CONSTRAINT "DealerInvitations_InvitationToken_key" UNIQUE ("InvitationToken")
);
```

### Code Transfer Tracking

When codes are transferred to dealer via `AcceptDealerInvitationCommand`:

```sql
-- SponsorshipCodes table updates
UPDATE "SponsorshipCodes"
SET
    "DealerId" = {dealerId},
    "TransferredAt" = NOW(),
    "TransferredByUserId" = {sponsorId}
WHERE "Id" IN (945, 946, 947, ...);
```

---

## Configuration

### appsettings.Development.json

```json
{
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app.dev"
  },
  "DealerInvitation": {
    "DeepLinkBaseUrl": "https://localhost:5001/dealer-invitation/",
    "TokenExpiryDays": 7,
    "SmsTemplate": "🎁 {sponsorName} Bayilik Daveti!\n\nDavet Kodunuz: DEALER-{token}\n\nHemen katılmak için tıklayın:\n{deepLink}\n\nVeya uygulamayı indirin:\n{playStoreLink}"
  }
}
```

### appsettings.Staging.json

```json
{
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app.staging"
  },
  "DealerInvitation": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/dealer-invitation/",
    "TokenExpiryDays": 7,
    "SmsTemplate": "🎁 {sponsorName} Bayilik Daveti!\n\nDavet Kodunuz: DEALER-{token}\n\nHemen katılmak için tıklayın:\n{deepLink}\n\nVeya uygulamayı indirin:\n{playStoreLink}"
  }
}
```

### appsettings.json (Production)

```json
{
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app"
  },
  "DealerInvitation": {
    "DeepLinkBaseUrl": "https://ziraai.com/dealer-invitation/",
    "TokenExpiryDays": 7,
    "SmsTemplate": "🎁 {sponsorName} Bayilik Daveti!\n\nDavet Kodunuz: DEALER-{token}\n\nHemen katılmak için tıklayın:\n{deepLink}\n\nVeya uygulamayı indirin:\n{playStoreLink}"
  }
}
```

---

## Testing Guide

### Manual Testing Steps

#### 1. Create Dealer Invitation

```bash
TOKEN="eyJhbGc..." # Sponsor JWT token

curl -X POST "https://ziraai-api-sit.up.railway.app/api/Sponsorship/dealer/invite-via-sms" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "email": "dealer@test.com",
    "phone": "+905551234567",
    "dealerName": "Test Dealer",
    "purchaseId": 26,
    "codeCount": 5
  }'
```

**Expected Response:**
- `invitationId`: Created invitation ID
- `invitationToken`: 32-character hex token
- `invitationLink`: Full deep link URL
- `smsSent`: true
- `smsDeliveryStatus`: "Sent"

#### 2. Get Invitation Details (Public)

```bash
TOKEN_FROM_SMS="a1b2c3d4e5f67890a1b2c3d4e5f67890"

curl -X GET "https://ziraai-api-sit.up.railway.app/api/Sponsorship/dealer/invitation-details?token=$TOKEN_FROM_SMS" \
  -H "x-dev-arch-version: 1.0"
```

**Expected Response:**
- `sponsorCompanyName`: Sponsor company name
- `codeCount`: Number of codes to transfer
- `packageTier`: Subscription tier (S, M, L, XL)
- `remainingDays`: Days until expiry
- `status`: "Pending"

#### 3. Accept Invitation (Authenticated)

```bash
DEALER_TOKEN="eyJhbGc..." # Dealer JWT token (after login)
INVITATION_TOKEN="a1b2c3d4e5f67890a1b2c3d4e5f67890"

curl -X POST "https://ziraai-api-sit.up.railway.app/api/Sponsorship/dealer/accept-invitation" \
  -H "Authorization: Bearer $DEALER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d "{
    \"invitationToken\": \"$INVITATION_TOKEN\"
  }"
```

**Expected Response:**
- `dealerId`: User ID who accepted
- `transferredCodeCount`: Number of codes transferred
- `transferredCodeIds`: Array of code IDs
- `acceptedAt`: Timestamp of acceptance

#### 4. Verify Codes Transferred

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/Sponsorship/codes?dealerId=158&page=1&pageSize=10" \
  -H "Authorization: Bearer $DEALER_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

**Expected:**
- Codes should have `dealerId` = 158
- `transferredAt` should be populated
- `transferredByUserId` should be sponsor ID

### Automated Testing (Postman)

See `ZiraAI_Dealer_Distribution_Complete_E2E.postman_collection.json` for complete E2E test scenarios.

---

## Error Handling

### Common Error Scenarios

| Error | Cause | Solution |
|-------|-------|----------|
| `Yetersiz kod` | Not enough codes available | Purchase more codes or reduce `codeCount` |
| `Davetiye bulunamadı` | Invalid token | Check SMS for correct token |
| `Bu davetiye size ait değil` | Email mismatch | Login with email from invitation |
| `Davetiyenin süresi dolmuş` | Token expired (>7 days) | Request new invitation from sponsor |
| `Bu davetiye daha önce kabul edilmiş` | Already accepted | Codes already transferred |
| `SMS gönderilemedi` | SMS service failure | Use manual link sharing |

### Error Codes

```typescript
// Frontend error handling
enum DealerInvitationError {
  NOT_FOUND = 'DEALER_INVITATION_NOT_FOUND',
  EXPIRED = 'DEALER_INVITATION_EXPIRED',
  ALREADY_ACCEPTED = 'DEALER_INVITATION_ALREADY_ACCEPTED',
  EMAIL_MISMATCH = 'DEALER_INVITATION_EMAIL_MISMATCH',
  INSUFFICIENT_CODES = 'INSUFFICIENT_CODES',
  SMS_FAILED = 'SMS_DELIVERY_FAILED',
}
```

### Retry Logic

```csharp
// SMS retry logic in InviteDealerViaSmsCommand
if (!sendResult.Success)
{
    // Don't fail the whole operation
    // Just mark SMS as failed and return the link
    invitation.LinkDelivered = false;
    // Sponsor can manually share the link
}
```

---

## Security Considerations

### 1. Email Verification

```csharp
// AcceptDealerInvitationCommand.cs:83-91
if (!string.IsNullOrEmpty(invitation.Email) &&
    !invitation.Email.Equals(request.CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
{
    return new ErrorDataResult<>("Bu davetiye size ait değil");
}
```

**Purpose:** Prevents unauthorized users from accepting invitations meant for others.

### 2. Token Expiry

- Default: 7 days (configurable)
- Auto-expires invitations after expiry date
- Status automatically updated to "Expired"

### 3. One-Time Use

- Invitation can only be accepted once
- Status changes from "Pending" to "Accepted"
- Subsequent attempts return error

### 4. Rate Limiting

**Recommended:** Add rate limiting to public endpoint:

```csharp
[EnableRateLimiting("PublicApi")] // 10 requests per minute per IP
[HttpGet("dealer/invitation-details")]
public async Task<IActionResult> GetDealerInvitationDetails([FromQuery] string token)
```

### 5. Token Generation

```csharp
// InviteDealerViaSmsCommand.cs:94
InvitationToken = Guid.NewGuid().ToString("N") // 32-character hex (no dashes)
```

- Cryptographically secure random token
- No sequential or predictable patterns

---

## Integration with Existing Systems

### Referral System Similarities

| Feature | Referral | Dealer Invitation |
|---------|----------|-------------------|
| SMS Pattern | `REF-{code}` | `DEALER-{token}` |
| Deep Link | `/ref/{code}` | `/dealer-invitation/DEALER-{token}` |
| SMS Reading | ✅ | ✅ |
| Auto-fill | ✅ | ✅ |
| Public Endpoint | ✅ | ✅ |

### Send-Link Redemption Similarities

| Feature | Send-Link | Dealer Invitation |
|---------|-----------|-------------------|
| SMS Service | MessagingServiceFactory | MessagingServiceFactory |
| Link Tracking | `LinkSentDate`, `LinkDelivered` | `LinkSentDate`, `LinkDelivered` |
| Delivery Status | ✅ | ✅ |
| WhatsApp Support | ✅ | ✅ |

---

## Future Enhancements

### Phase 2 Features

1. **WhatsApp Support**
   - Add channel selection in invite-via-sms
   - Update SMS template for WhatsApp formatting

2. **Email Invitations**
   - Add `/dealer/invite-via-email` endpoint
   - HTML email template with deep link

3. **Invitation Analytics**
   - Track link click count
   - Measure acceptance rate
   - Time-to-acceptance metrics

4. **Bulk Invitations**
   - CSV upload support
   - Batch SMS sending
   - Progress tracking

5. **Invitation Expiry Notifications**
   - Remind dealers 1 day before expiry
   - Auto-extend option for sponsors

---

## Support & Troubleshooting

### Debug Checklist

- [ ] Migration applied to database
- [ ] Configuration set in appsettings for environment
- [ ] SMS service configured and working
- [ ] Deep link URL matches mobile app configuration
- [ ] Mobile app has SMS read permission
- [ ] JWT token valid and not expired
- [ ] User email matches invitation email

### Logs to Check

```bash
# Check SMS sending logs
grep "📨 Sponsor.*sending dealer invitation via SMS" /app/logs/

# Check invitation creation
grep "✅ Created invitation.*with token" /app/logs/

# Check acceptance logs
grep "✅ Dealer invitation.*accepted by user" /app/logs/

# Check code transfer logs
grep "📦 Transferring.*codes to dealer" /app/logs/
```

### Common Questions

**Q: Can a dealer accept multiple invitations?**
A: Yes, each invitation is independent. A dealer can accept invitations from multiple sponsors.

**Q: What happens if SMS fails?**
A: The invitation is still created. The sponsor receives the deep link in the API response and can manually share it.

**Q: Can the sponsor cancel an invitation?**
A: Not yet implemented. Planned for Phase 2.

**Q: What if the dealer doesn't have the app installed?**
A: They click the Play Store link in SMS, install the app, and the app reads the SMS to extract the token automatically.

---

## Contact

For questions or issues related to dealer invitation flow:

- **Backend:** Check `Business/Handlers/Sponsorship/Commands/` and `Queries/`
- **Documentation:** `claudedocs/Dealers/`
- **Database:** `DealerInvitations` table
- **Configuration:** `appsettings.*.json` → `DealerInvitation` section

**Version History:**
- v1.0 (2025-01-25): Initial implementation with SMS support
