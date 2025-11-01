# Mobile Integration Guide: SMS-Based Dealer Invitation Flow

**Target Audience:** Mobile Development Team (Flutter)
**Version:** 1.0
**Date:** 2025-01-25
**Status:** Ready for Implementation

---

## 📋 Table of Contents

1. [Overview & Context](#overview--context)
2. [Complete Flow Diagram](#complete-flow-diagram)
3. [SMS Format & Content](#sms-format--content)
4. [API Endpoints](#api-endpoints)
5. [Deep Link Configuration](#deep-link-configuration)
6. [SMS Reading Implementation](#sms-reading-implementation)
7. [UI/UX Implementation](#uiux-implementation)
8. [Similarities with Existing Flows](#similarities-with-existing-flows)
9. [Error Handling](#error-handling)
10. [Testing Checklist](#testing-checklist)

---

## 🎯 Overview & Context

### What is this feature?

Dealer invitation system allows **sponsors** to invite **dealers** via SMS, enabling dealers to receive sponsorship codes that they can later distribute to farmers. This is similar to the referral and send-link systems you've already implemented.

### User Roles

1. **Sponsor (Sponsor):** Company/organization that purchases codes and invites dealers
2. **Dealer (Bayi):** Distributor who receives codes from sponsor and distributes to farmers
3. **Farmer (Çiftçi):** End user who redeems codes (not involved in this flow)

### Key Similarities with Existing Flows

| Feature | Referral System | Send-Link System | **Dealer Invitation** |
|---------|----------------|------------------|---------------------|
| **SMS Format** | `REF-{code}` | `AGRI-2025-{code}` | `DEALER-{token}` |
| **Deep Link** | `/ref/{code}` | `/redeem?code={code}` | `/dealer-invitation/DEALER-{token}` |
| **SMS Reading** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Auto-fill** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Public Endpoint** | ✅ `/ref/{code}` | ✅ `/redeem?code={code}` | ✅ `/dealer/invitation-details?token={token}` |
| **Auth Required** | Register/Login | Register/Login | Register/Login |
| **Auto Action** | Apply referral | Redeem code | Transfer codes |

### What Makes This Different?

- **Token-based:** Uses 32-character tokens (not 8-character codes)
- **Pre-login Details:** Can view invitation details BEFORE logging in
- **Email Verification:** Must login with email that matches invitation
- **Role Assignment:** Auto-assigns "Sponsor" role to dealer
- **Code Transfer:** Automatically transfers multiple codes (not single redemption)

---

## 🔄 Complete Flow Diagram

### Scenario A: App Already Installed

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1: Sponsor sends invitation via API                   │
│ POST /dealer/invite-via-sms                                 │
│ → Creates invitation in database                            │
│ → Sends SMS to dealer's phone                               │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 2: Dealer receives SMS                                 │
│ 📱 SMS Content:                                             │
│    🎁 ABC Tarım A.Ş. Bayilik Daveti!                        │
│    Davet Kodunuz: DEALER-a1b2c3d4e5f67890...                │
│    Hemen katılmak için tıklayın:                            │
│    https://ziraai.com/dealer-invitation/DEALER-a1b2...      │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 3: Dealer clicks deep link                             │
│ → Android: App opens via intent filter                      │
│ → iOS: App opens via universal links                        │
│ → App receives: /dealer-invitation/DEALER-a1b2c3d4...       │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 4: App checks authentication                           │
│ ├─ Logged in? → Go to Step 5                                │
│ └─ Not logged in? → Store token → Show Login/Register       │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 5: Get invitation details (NO AUTH REQUIRED)           │
│ GET /dealer/invitation-details?token={token}                │
│ → Returns: Sponsor name, code count, tier, expiry           │
│ → Show invitation details screen                            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 6: Dealer accepts invitation                           │
│ POST /dealer/accept-invitation                              │
│ → Validates email match                                     │
│ → Assigns Sponsor role                                      │
│ → Transfers codes to dealer                                 │
│ → Shows success screen                                      │
└─────────────────────────────────────────────────────────────┘
```

### Scenario B: App Not Installed

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1-2: Same as Scenario A                                │
│ Sponsor sends → Dealer receives SMS                         │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 3: Dealer clicks Play Store link in SMS                │
│ → Opens Play Store                                           │
│ → Dealer installs app                                        │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 4: App first launch                                    │
│ → Requests SMS read permission                              │
│ → Reads recent SMS messages                                 │
│ → Extracts DEALER-{token} using regex                       │
│ → Stores token for later use                                │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 5: User registers/logs in                              │
│ → Must use email from invitation                            │
│ → After login, proceed to Step 6                            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ STEP 6-7: Same as Scenario A                                │
│ Get details → Accept invitation                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 📱 SMS Format & Content

### Full SMS Text

```
🎁 {sponsorName} Bayilik Daveti!

Davet Kodunuz: DEALER-{token}

Hemen katılmak için tıklayın:
{deepLink}

Veya uygulamayı indirin:
{playStoreLink}
```

### Real Example (Staging)

```
🎁 ABC Tarım A.Ş. Bayilik Daveti!

Davet Kodunuz: DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890

Hemen katılmak için tıklayın:
https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app.staging
```

### Real Example (Production)

```
🎁 ABC Tarım A.Ş. Bayilik Daveti!

Davet Kodunuz: DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890

Hemen katılmak için tıklayın:
https://ziraai.com/dealer-invitation/DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

### Token Format Details

| Component | Description | Example |
|-----------|-------------|---------|
| **Prefix** | Always "DEALER-" | `DEALER-` |
| **Token** | 32-character hexadecimal (lowercase) | `a1b2c3d4e5f67890a1b2c3d4e5f67890` |
| **Full Token** | Prefix + Token | `DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890` |
| **Valid Characters** | `a-f`, `0-9` (no uppercase, no special chars) | ✅ `a1b2c3` ❌ `A1B2C3` ❌ `g1h2i3` |
| **Length** | Fixed 32 characters (without prefix) | Length must be exactly 32 |

### ⚠️ CRITICAL: Token Extraction

**What to extract:** Only the **32-character hex part**, NOT the "DEALER-" prefix

```dart
// ❌ WRONG - Including prefix
String token = "DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890";

// ✅ CORRECT - Only the hex part
String token = "a1b2c3d4e5f67890a1b2c3d4e5f67890";
```

**Why?** The API expects only the token, not the prefix.

### Comparison with Existing Systems

| System | SMS Pattern | Token Length | Prefix in API? |
|--------|-------------|--------------|----------------|
| **Referral** | `REF-ABC12345` | 8 chars | ❌ No (send `ABC12345`) |
| **Send-Link** | `AGRI-2025-X3K9L2M8` | 8 chars | ❌ No (send `X3K9L2M8`) |
| **Dealer Invitation** | `DEALER-a1b2c3d4...` | 32 chars | ❌ No (send `a1b2c3d4...`) |

---

## 🔌 API Endpoints

### Endpoint 1: Get Invitation Details (Public - NO AUTH)

**Purpose:** Show invitation details BEFORE user logs in

#### Request

```http
GET /api/Sponsorship/dealer/invitation-details?token={token}
Host: {baseUrl}
x-dev-arch-version: 1.0
```

**⚠️ NO Authorization header required**

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `token` | string | ✅ Yes | 32-character invitation token (without "DEALER-" prefix) | `a1b2c3d4e5f67890a1b2c3d4e5f67890` |

#### Success Response (200 OK)

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

**Token Not Found (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Davetiye bulunamadı"
}
```

**Invitation Expired (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Davetiyenin süresi dolmuş. Lütfen sponsor ile iletişime geçin"
}
```

**Already Accepted (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Bu davetiye daha önce kabul edilmiş"
}
```

**Invitation Rejected (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Bu davetiye reddedilmiş"
}
```

#### Flutter Implementation Example

```dart
class DealerInvitationService {
  static const String baseUrl = 'https://ziraai-api-sit.up.railway.app';

  /// Get invitation details (NO AUTH REQUIRED)
  Future<DealerInvitationDetails?> getInvitationDetails(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/api/Sponsorship/dealer/invitation-details?token=$token'),
        headers: {
          'x-dev-arch-version': '1.0',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        if (data['success'] == true && data['data'] != null) {
          return DealerInvitationDetails.fromJson(data['data']);
        } else {
          // Handle error message
          throw Exception(data['message'] ?? 'Davetiye alınamadı');
        }
      } else {
        throw Exception('HTTP ${response.statusCode}');
      }
    } catch (e) {
      print('Error fetching invitation details: $e');
      return null;
    }
  }
}

// Model class
class DealerInvitationDetails {
  final int invitationId;
  final String sponsorCompanyName;
  final int codeCount;
  final String packageTier;
  final DateTime expiresAt;
  final int remainingDays;
  final String status;
  final String invitationMessage;
  final String dealerEmail;
  final String dealerPhone;
  final DateTime createdAt;

  DealerInvitationDetails({
    required this.invitationId,
    required this.sponsorCompanyName,
    required this.codeCount,
    required this.packageTier,
    required this.expiresAt,
    required this.remainingDays,
    required this.status,
    required this.invitationMessage,
    required this.dealerEmail,
    required this.dealerPhone,
    required this.createdAt,
  });

  factory DealerInvitationDetails.fromJson(Map<String, dynamic> json) {
    return DealerInvitationDetails(
      invitationId: json['invitationId'],
      sponsorCompanyName: json['sponsorCompanyName'],
      codeCount: json['codeCount'],
      packageTier: json['packageTier'],
      expiresAt: DateTime.parse(json['expiresAt']),
      remainingDays: json['remainingDays'],
      status: json['status'],
      invitationMessage: json['invitationMessage'],
      dealerEmail: json['dealerEmail'],
      dealerPhone: json['dealerPhone'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}
```

---

### Endpoint 2: Accept Invitation (AUTH REQUIRED)

**Purpose:** Accept invitation and transfer codes to dealer

#### Request

```http
POST /api/Sponsorship/dealer/accept-invitation
Host: {baseUrl}
Authorization: Bearer {jwt_token}
Content-Type: application/json
x-dev-arch-version: 1.0

{
  "invitationToken": "a1b2c3d4e5f67890a1b2c3d4e5f67890"
}
```

**⚠️ Authorization header REQUIRED**

#### Request Body

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `invitationToken` | string | ✅ Yes | 32-character token (without "DEALER-" prefix) | `a1b2c3d4e5f67890a1b2c3d4e5f67890` |

#### Success Response (200 OK)

```json
{
  "data": {
    "invitationId": 15,
    "dealerId": 158,
    "transferredCodeCount": 10,
    "transferredCodeIds": [945, 946, 947, 948, 949, 950, 951, 952, 953, 954],
    "acceptedAt": "2025-01-25T11:00:00Z",
    "message": "✅ Tebrikler! 10 adet kod hesabınıza transfer edildi. Artık bu kodları çiftçilere dağıtabilirsiniz."
  },
  "success": true,
  "message": "Bayilik daveti başarıyla kabul edildi"
}
```

#### Response Fields Explanation

| Field | Type | Description |
|-------|------|-------------|
| `invitationId` | int | ID of the accepted invitation |
| `dealerId` | int | Your user ID (now a dealer) |
| `transferredCodeCount` | int | Number of codes transferred to you |
| `transferredCodeIds` | int[] | Array of code IDs you received |
| `acceptedAt` | datetime | Timestamp when invitation was accepted |
| `message` | string | User-friendly success message (Turkish) |

#### Error Responses

**Email Mismatch - Security Check (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Bu davetiye size ait değil"
}
```
**⚠️ IMPORTANT:** User must login with the EXACT email from the invitation.

**Invitation Not Found/Already Used (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Davetiye bulunamadı veya daha önce kabul edilmiş/reddedilmiş"
}
```

**Invitation Expired (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Davetiye süresi dolmuş. Lütfen sponsor ile iletişime geçin"
}
```

**Insufficient Codes (200 OK with success=false)**
```json
{
  "data": null,
  "success": false,
  "message": "Yetersiz kod. İstenen: 10, Mevcut: 5"
}
```

**Unauthorized (401)**
```json
{
  "message": "Unauthorized"
}
```

#### Flutter Implementation Example

```dart
class DealerInvitationService {
  static const String baseUrl = 'https://ziraai-api-sit.up.railway.app';

  /// Accept invitation (AUTH REQUIRED)
  Future<DealerInvitationAcceptResponse?> acceptInvitation(
    String token,
    String jwtToken,
  ) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/api/Sponsorship/dealer/accept-invitation'),
        headers: {
          'Authorization': 'Bearer $jwtToken',
          'Content-Type': 'application/json',
          'x-dev-arch-version': '1.0',
        },
        body: jsonEncode({
          'invitationToken': token,
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        if (data['success'] == true && data['data'] != null) {
          return DealerInvitationAcceptResponse.fromJson(data['data']);
        } else {
          // Handle error message
          throw Exception(data['message'] ?? 'Davetiye kabul edilemedi');
        }
      } else if (response.statusCode == 401) {
        throw Exception('Oturum süreniz dolmuş. Lütfen tekrar giriş yapın');
      } else {
        throw Exception('HTTP ${response.statusCode}');
      }
    } catch (e) {
      print('Error accepting invitation: $e');
      rethrow;
    }
  }
}

// Model class
class DealerInvitationAcceptResponse {
  final int invitationId;
  final int dealerId;
  final int transferredCodeCount;
  final List<int> transferredCodeIds;
  final DateTime acceptedAt;
  final String message;

  DealerInvitationAcceptResponse({
    required this.invitationId,
    required this.dealerId,
    required this.transferredCodeCount,
    required this.transferredCodeIds,
    required this.acceptedAt,
    required this.message,
  });

  factory DealerInvitationAcceptResponse.fromJson(Map<String, dynamic> json) {
    return DealerInvitationAcceptResponse(
      invitationId: json['invitationId'],
      dealerId: json['dealerId'],
      transferredCodeCount: json['transferredCodeCount'],
      transferredCodeIds: List<int>.from(json['transferredCodeIds']),
      acceptedAt: DateTime.parse(json['acceptedAt']),
      message: json['message'],
    );
  }
}
```

---

## 🔗 Deep Link Configuration

### URL Structure

| Environment | Base URL | Full Deep Link Example |
|-------------|----------|------------------------|
| **Development** | `https://localhost:5001` | `https://localhost:5001/dealer-invitation/DEALER-a1b2c3d4...` |
| **Staging** | `https://ziraai-api-sit.up.railway.app` | `https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-a1b2c3d4...` |
| **Production** | `https://ziraai.com` | `https://ziraai.com/dealer-invitation/DEALER-a1b2c3d4...` |

### Android Configuration (AndroidManifest.xml)

```xml
<activity
    android:name=".MainActivity"
    android:exported="true"
    android:launchMode="singleTop">

    <!-- Existing intent filters... -->

    <!-- Dealer Invitation Deep Links -->
    <intent-filter android:autoVerify="true">
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />

        <!-- Staging Environment -->
        <data
            android:scheme="https"
            android:host="ziraai-api-sit.up.railway.app"
            android:pathPrefix="/dealer-invitation/" />

        <!-- Production Environment -->
        <data
            android:scheme="https"
            android:host="ziraai.com"
            android:pathPrefix="/dealer-invitation/" />

        <!-- App-specific scheme (fallback) -->
        <data
            android:scheme="ziraai"
            android:host="dealer-invitation" />
    </intent-filter>
</activity>
```

### iOS Configuration

#### 1. Associated Domains (Entitlements)

```xml
<!-- ios/Runner/Runner.entitlements -->
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.developer.associated-domains</key>
    <array>
        <!-- Staging -->
        <string>applinks:ziraai-api-sit.up.railway.app</string>
        <!-- Production -->
        <string>applinks:ziraai.com</string>
    </array>
</dict>
</plist>
```

#### 2. Apple App Site Association (Server-side)

**File:** `https://ziraai.com/.well-known/apple-app-site-association`

```json
{
  "applinks": {
    "apps": [],
    "details": [
      {
        "appID": "TEAM_ID.com.ziraai.app",
        "paths": [
          "/dealer-invitation/*",
          "/ref/*",
          "/redeem"
        ]
      }
    ]
  }
}
```

### Flutter Deep Link Handling

```dart
import 'package:uni_links/uni_links.dart';
import 'dart:async';

class DeepLinkHandler {
  StreamSubscription? _linkSubscription;

  // Initialize deep link listener
  void initDeepLinkListener(BuildContext context) {
    // Handle links when app is already running
    _linkSubscription = linkStream.listen(
      (String? link) {
        if (link != null) {
          _handleDeepLink(context, link);
        }
      },
      onError: (err) {
        print('Deep link error: $err');
      },
    );

    // Handle initial link when app is opened from terminated state
    _handleInitialLink(context);
  }

  // Handle initial link
  Future<void> _handleInitialLink(BuildContext context) async {
    try {
      final initialLink = await getInitialLink();
      if (initialLink != null) {
        _handleDeepLink(context, initialLink);
      }
    } catch (e) {
      print('Error handling initial link: $e');
    }
  }

  // Parse and handle deep link
  void _handleDeepLink(BuildContext context, String link) {
    print('📎 Deep link received: $link');

    try {
      final uri = Uri.parse(link);

      // Check if it's a dealer invitation link
      if (uri.path.startsWith('/dealer-invitation/')) {
        final fullToken = uri.pathSegments.last; // DEALER-a1b2c3d4...

        if (fullToken.startsWith('DEALER-')) {
          final token = fullToken.substring(7); // Remove "DEALER-" prefix
          print('✅ Extracted dealer token: $token');

          _handleDealerInvitation(context, token);
        } else {
          print('❌ Invalid token format: $fullToken');
        }
      }
      // Check for other deep links (referral, redemption, etc.)
      else if (uri.path.startsWith('/ref/')) {
        // Handle referral
        _handleReferral(context, uri);
      }
      else if (uri.path.startsWith('/redeem')) {
        // Handle code redemption
        _handleRedemption(context, uri);
      }
    } catch (e) {
      print('❌ Error parsing deep link: $e');
    }
  }

  // Handle dealer invitation flow
  void _handleDealerInvitation(BuildContext context, String token) async {
    // Check if user is logged in
    final authService = Provider.of<AuthService>(context, listen: false);
    final isLoggedIn = await authService.isLoggedIn();

    if (isLoggedIn) {
      // User is logged in - go directly to invitation screen
      Navigator.of(context).push(
        MaterialPageRoute(
          builder: (context) => DealerInvitationScreen(token: token),
        ),
      );
    } else {
      // User not logged in - store token and show login
      await _storePendingInvitation(token);

      Navigator.of(context).pushAndRemoveUntil(
        MaterialPageRoute(
          builder: (context) => LoginScreen(
            redirectTo: 'dealer-invitation',
            message: 'Bayilik davetini görüntülemek için lütfen giriş yapın',
          ),
        ),
        (route) => false,
      );
    }
  }

  // Store pending invitation token
  Future<void> _storePendingInvitation(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('pending_dealer_invitation', token);
    await prefs.setInt('pending_invitation_timestamp', DateTime.now().millisecondsSinceEpoch);
  }

  // Check for pending invitation after login
  static Future<String?> getPendingInvitation() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('pending_dealer_invitation');
    final timestamp = prefs.getInt('pending_invitation_timestamp');

    if (token != null && timestamp != null) {
      // Check if stored within last 7 days
      final storedDate = DateTime.fromMillisecondsSinceEpoch(timestamp);
      final daysSince = DateTime.now().difference(storedDate).inDays;

      if (daysSince <= 7) {
        // Clear stored token
        await prefs.remove('pending_dealer_invitation');
        await prefs.remove('pending_invitation_timestamp');
        return token;
      }
    }

    return null;
  }

  void dispose() {
    _linkSubscription?.cancel();
  }
}
```

### Testing Deep Links

**Android (ADB):**
```bash
# Test staging deep link
adb shell am start -W -a android.intent.action.VIEW \
  -d "https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890" \
  com.ziraai.app.staging

# Test custom scheme
adb shell am start -W -a android.intent.action.VIEW \
  -d "ziraai://dealer-invitation/DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890" \
  com.ziraai.app.staging
```

**iOS (Simulator):**
```bash
xcrun simctl openurl booted \
  "https://ziraai.com/dealer-invitation/DEALER-a1b2c3d4e5f67890a1b2c3d4e5f67890"
```

---

## 📖 SMS Reading Implementation

### Android Permissions

#### 1. Add Permissions to AndroidManifest.xml

```xml
<!-- SMS Reading Permissions -->
<uses-permission android:name="android.permission.RECEIVE_SMS" />
<uses-permission android:name="android.permission.READ_SMS" />
```

#### 2. Request Runtime Permissions

```dart
import 'package:permission_handler/permission_handler.dart';

class SmsPermissionHandler {
  /// Request SMS read permission
  static Future<bool> requestSmsPermission() async {
    final status = await Permission.sms.request();

    if (status.isGranted) {
      print('✅ SMS permission granted');
      return true;
    } else if (status.isDenied) {
      print('❌ SMS permission denied');
      return false;
    } else if (status.isPermanentlyDenied) {
      print('⚠️ SMS permission permanently denied - open settings');
      await openAppSettings();
      return false;
    }

    return false;
  }

  /// Check if SMS permission is already granted
  static Future<bool> hasSmsPermission() async {
    return await Permission.sms.isGranted;
  }
}
```

### SMS Listening and Token Extraction

```dart
import 'package:telephony/telephony.dart';

class SmsReader {
  final Telephony telephony = Telephony.instance;

  // Regex to extract dealer invitation token
  static final dealerTokenRegex = RegExp(r'DEALER-([a-f0-9]{32})', caseSensitive: false);

  /// Start listening for incoming SMS
  Future<void> startSmsListener() async {
    final hasPermission = await SmsPermissionHandler.hasSmsPermission();

    if (!hasPermission) {
      print('❌ No SMS permission - cannot listen');
      return;
    }

    // Listen for incoming messages (foreground only)
    telephony.listenIncomingSms(
      onNewMessage: _onSmsReceived,
      listenInBackground: false, // Set true if you want background listening
    );

    print('✅ SMS listener started');
  }

  /// Handle received SMS
  void _onSmsReceived(SmsMessage message) {
    print('📩 SMS received from: ${message.address}');
    print('📩 SMS body: ${message.body}');

    final body = message.body ?? '';

    // Try to extract dealer invitation token
    final dealerToken = extractDealerToken(body);
    if (dealerToken != null) {
      print('✅ Dealer invitation token found: $dealerToken');
      _handleDealerInvitationToken(dealerToken);
      return;
    }

    // Try to extract referral code
    final referralCode = extractReferralCode(body);
    if (referralCode != null) {
      print('✅ Referral code found: $referralCode');
      _handleReferralCode(referralCode);
      return;
    }

    // Try to extract sponsorship code
    final sponsorshipCode = extractSponsorshipCode(body);
    if (sponsorshipCode != null) {
      print('✅ Sponsorship code found: $sponsorshipCode');
      _handleSponsorshipCode(sponsorshipCode);
      return;
    }
  }

  /// Extract dealer invitation token from SMS
  static String? extractDealerToken(String smsBody) {
    final match = dealerTokenRegex.firstMatch(smsBody);
    if (match != null && match.groupCount >= 1) {
      // Return only the 32-character hex part (without "DEALER-" prefix)
      return match.group(1)!.toLowerCase();
    }
    return null;
  }

  /// Extract referral code from SMS (existing)
  static String? extractReferralCode(String smsBody) {
    final refRegex = RegExp(r'REF-([A-Z0-9]{8})', caseSensitive: false);
    final match = refRegex.firstMatch(smsBody);
    return match?.group(1);
  }

  /// Extract sponsorship code from SMS (existing)
  static String? extractSponsorshipCode(String smsBody) {
    final codeRegex = RegExp(r'AGRI-\d{4}-([A-Z0-9]{8})', caseSensitive: false);
    final match = codeRegex.firstMatch(smsBody);
    return match?.group(1);
  }

  /// Handle dealer invitation token
  void _handleDealerInvitationToken(String token) async {
    // Store token for later use
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('auto_detected_dealer_token', token);
    await prefs.setInt('auto_detected_dealer_timestamp', DateTime.now().millisecondsSinceEpoch);

    // Show notification or navigate
    // (Implementation depends on your app architecture)
  }

  /// Read recent SMS messages (for app first launch)
  Future<List<String>> readRecentSmsMessages({int maxMessages = 20}) async {
    final hasPermission = await SmsPermissionHandler.hasSmsPermission();

    if (!hasPermission) {
      print('❌ No SMS permission - cannot read messages');
      return [];
    }

    try {
      final messages = await telephony.getInboxSms(
        columns: [SmsColumn.ADDRESS, SmsColumn.BODY, SmsColumn.DATE],
        sortOrder: [OrderBy(SmsColumn.DATE, sort: Sort.DESC)],
      );

      return messages
          .take(maxMessages)
          .map((msg) => msg.body ?? '')
          .toList();
    } catch (e) {
      print('❌ Error reading SMS messages: $e');
      return [];
    }
  }

  /// Scan recent messages for dealer invitation token (first launch)
  Future<String?> scanForDealerInvitation() async {
    final recentMessages = await readRecentSmsMessages(maxMessages: 50);

    for (final body in recentMessages) {
      final token = extractDealerToken(body);
      if (token != null) {
        print('✅ Found dealer invitation in recent SMS: $token');
        return token;
      }
    }

    print('ℹ️ No dealer invitation found in recent SMS');
    return null;
  }
}
```

### First Launch Flow (App Not Previously Installed)

```dart
class FirstLaunchHandler {
  /// Check and handle first launch scenario
  static Future<void> handleFirstLaunch(BuildContext context) async {
    final prefs = await SharedPreferences.getInstance();
    final isFirstLaunch = prefs.getBool('is_first_launch') ?? true;

    if (!isFirstLaunch) {
      return; // Not first launch
    }

    print('🎉 First app launch detected');

    // Request SMS permission
    final hasPermission = await SmsPermissionHandler.requestSmsPermission();

    if (hasPermission) {
      // Scan recent SMS for dealer invitation
      final smsReader = SmsReader();
      final dealerToken = await smsReader.scanForDealerInvitation();

      if (dealerToken != null) {
        // Store token for after login
        await prefs.setString('pending_dealer_invitation', dealerToken);
        await prefs.setInt('pending_invitation_timestamp', DateTime.now().millisecondsSinceEpoch);

        // Show notification to user
        _showDealerInvitationFoundDialog(context);
      }
    }

    // Mark first launch as complete
    await prefs.setBool('is_first_launch', false);
  }

  static void _showDealerInvitationFoundDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('🎁 Bayilik Daveti Bulundu'),
        content: Text(
          'SMS mesajlarınızda bir bayilik daveti tespit ettik. '
          'Giriş yaptıktan sonra davetiyenizi görüntüleyebilirsiniz.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: Text('Tamam'),
          ),
        ],
      ),
    );
  }
}
```

### Add to Main App Initialization

```dart
class MyApp extends StatefulWidget {
  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  late DeepLinkHandler _deepLinkHandler;
  late SmsReader _smsReader;

  @override
  void initState() {
    super.initState();

    // Initialize deep link handler
    _deepLinkHandler = DeepLinkHandler();

    // Initialize SMS reader
    _smsReader = SmsReader();

    // Delayed initialization after first frame
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _initializeApp();
    });
  }

  Future<void> _initializeApp() async {
    // Handle first launch (scan SMS)
    await FirstLaunchHandler.handleFirstLaunch(context);

    // Start deep link listener
    _deepLinkHandler.initDeepLinkListener(context);

    // Start SMS listener
    await _smsReader.startSmsListener();
  }

  @override
  void dispose() {
    _deepLinkHandler.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      // Your app configuration...
    );
  }
}
```

---

## 🎨 UI/UX Implementation

### Screen 1: Invitation Details (Pre-Login)

```dart
class DealerInvitationScreen extends StatefulWidget {
  final String token;

  const DealerInvitationScreen({required this.token});

  @override
  _DealerInvitationScreenState createState() => _DealerInvitationScreenState();
}

class _DealerInvitationScreenState extends State<DealerInvitationScreen> {
  bool _isLoading = true;
  DealerInvitationDetails? _invitation;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadInvitationDetails();
  }

  Future<void> _loadInvitationDetails() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final service = DealerInvitationService();
      final invitation = await service.getInvitationDetails(widget.token);

      setState(() {
        _invitation = invitation;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: Text('Bayilik Daveti')),
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (_errorMessage != null) {
      return Scaffold(
        appBar: AppBar(title: Text('Bayilik Daveti')),
        body: Center(
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.error_outline, size: 64, color: Colors.red),
                SizedBox(height: 16),
                Text(
                  _errorMessage!,
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 16),
                ),
                SizedBox(height: 24),
                ElevatedButton(
                  onPressed: () => Navigator.of(context).pop(),
                  child: Text('Geri Dön'),
                ),
              ],
            ),
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: Text('Bayilik Daveti'),
        backgroundColor: Colors.green,
      ),
      body: SingleChildScrollView(
        child: Padding(
          padding: EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.green.shade200),
                ),
                child: Row(
                  children: [
                    Icon(Icons.card_giftcard, size: 48, color: Colors.green),
                    SizedBox(width: 16),
                    Expanded(
                      child: Text(
                        _invitation!.invitationMessage,
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: Colors.green.shade900,
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              SizedBox(height: 24),

              // Details Cards
              _buildInfoCard(
                icon: Icons.business,
                title: 'Sponsor',
                value: _invitation!.sponsorCompanyName,
                color: Colors.blue,
              ),
              _buildInfoCard(
                icon: Icons.confirmation_number,
                title: 'Kod Adedi',
                value: '${_invitation!.codeCount} adet',
                color: Colors.orange,
              ),
              _buildInfoCard(
                icon: Icons.workspace_premium,
                title: 'Paket',
                value: _invitation!.packageTier,
                color: Colors.purple,
              ),
              _buildInfoCard(
                icon: Icons.access_time,
                title: 'Kalan Süre',
                value: '${_invitation!.remainingDays} gün',
                color: _invitation!.remainingDays < 2 ? Colors.red : Colors.green,
              ),

              SizedBox(height: 24),

              // Info Box
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.blue.shade50,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  children: [
                    Icon(Icons.info_outline, color: Colors.blue),
                    SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        'Daveti kabul etmek için ${_invitation!.dealerEmail} adresi ile giriş yapmanız gerekmektedir.',
                        style: TextStyle(fontSize: 14, color: Colors.blue.shade900),
                      ),
                    ),
                  ],
                ),
              ),

              SizedBox(height: 32),

              // Accept Button
              SizedBox(
                width: double.infinity,
                height: 56,
                child: ElevatedButton(
                  onPressed: _handleAccept,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.green,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  child: Text(
                    'Daveti Kabul Et',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                ),
              ),

              SizedBox(height: 16),

              // Reject Button
              SizedBox(
                width: double.infinity,
                height: 56,
                child: OutlinedButton(
                  onPressed: _handleReject,
                  style: OutlinedButton.styleFrom(
                    foregroundColor: Colors.red,
                    side: BorderSide(color: Colors.red),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                  ),
                  child: Text(
                    'Reddet',
                    style: TextStyle(fontSize: 16),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInfoCard({
    required IconData icon,
    required String title,
    required String value,
    required Color color,
  }) {
    return Card(
      margin: EdgeInsets.only(bottom: 12),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Row(
          children: [
            Container(
              padding: EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: color.withOpacity(0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, color: color, size: 24),
            ),
            SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: TextStyle(
                      fontSize: 12,
                      color: Colors.grey[600],
                    ),
                  ),
                  SizedBox(height: 4),
                  Text(
                    value,
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _handleAccept() async {
    // Check if user is logged in
    final authService = Provider.of<AuthService>(context, listen: false);
    final isLoggedIn = await authService.isLoggedIn();

    if (!isLoggedIn) {
      // Store token and redirect to login
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString('pending_dealer_invitation', widget.token);

      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (context) => LoginScreen(
            redirectTo: 'dealer-invitation',
            message: 'Daveti kabul etmek için lütfen ${_invitation!.dealerEmail} adresi ile giriş yapın',
          ),
        ),
      );
      return;
    }

    // User is logged in - accept invitation
    await _acceptInvitation();
  }

  Future<void> _acceptInvitation() async {
    // Show loading
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => Center(child: CircularProgressIndicator()),
    );

    try {
      final authService = Provider.of<AuthService>(context, listen: false);
      final jwtToken = await authService.getToken();

      final service = DealerInvitationService();
      final response = await service.acceptInvitation(widget.token, jwtToken!);

      // Close loading
      Navigator.of(context).pop();

      // Show success dialog
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => AlertDialog(
          title: Row(
            children: [
              Icon(Icons.check_circle, color: Colors.green, size: 32),
              SizedBox(width: 12),
              Text('Tebrikler!'),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(response!.message),
              SizedBox(height: 16),
              Text(
                '${response.transferredCodeCount} adet kod hesabınıza aktarıldı.',
                style: TextStyle(fontWeight: FontWeight.bold),
              ),
            ],
          ),
          actions: [
            ElevatedButton(
              onPressed: () {
                // Navigate to codes screen
                Navigator.of(context).pop(); // Close dialog
                Navigator.of(context).pop(); // Close invitation screen
                // Navigate to my codes screen
                Navigator.of(context).push(
                  MaterialPageRoute(builder: (context) => MyCodesScreen()),
                );
              },
              child: Text('Kodlarımı Görüntüle'),
            ),
          ],
        ),
      );
    } catch (e) {
      // Close loading
      Navigator.of(context).pop();

      // Show error
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Hata'),
          content: Text(e.toString()),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: Text('Tamam'),
            ),
          ],
        ),
      );
    }
  }

  Future<void> _handleReject() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Daveti Reddet?'),
        content: Text('Bu daveti reddetmek istediğinizden emin misiniz?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text('İptal'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: Text('Reddet'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      // TODO: Implement reject API call (if backend supports it)
      Navigator.of(context).pop();
    }
  }
}
```

---

## 🔄 Similarities with Existing Flows

### Comparison Table

| Aspect | **Referral System** | **Send-Link System** | **Dealer Invitation** |
|--------|---------------------|----------------------|----------------------|
| **Purpose** | Invite new users to app | Send code to farmer | Invite dealer to receive codes |
| **Sender** | Any user | Sponsor/Dealer | Sponsor only |
| **Receiver** | New potential user | Farmer | Dealer |
| **SMS Pattern** | `REF-ABC12345` | `AGRI-2025-X3K9L2M8` | `DEALER-a1b2c3d4...` (32 chars) |
| **Token Length** | 8 characters | 8 characters | 32 characters |
| **SMS Reading** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Deep Link** | `/ref/{code}` | `/redeem?code={code}` | `/dealer-invitation/DEALER-{token}` |
| **Public Endpoint** | ✅ Yes | ✅ Yes (with limitations) | ✅ Yes |
| **Auth Required for Action** | Register/Login | Register/Login | Register/Login |
| **Email Verification** | ❌ No | ❌ No | ✅ **Yes (CRITICAL)** |
| **Action** | Apply referral bonus | Redeem single code | Transfer multiple codes |
| **Role Assignment** | ❌ No | ❌ No | ✅ Yes (Sponsor role) |
| **One-Time Use** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Expiry** | No expiry | Code expiry | 7-day invitation expiry |

### Key Implementation Patterns to Reuse

#### 1. SMS Reading Pattern (SAME AS REFERRAL)

```dart
// ✅ REUSE THIS PATTERN
class SmsReader {
  // Referral code extraction (EXISTING)
  static String? extractReferralCode(String smsBody) {
    final refRegex = RegExp(r'REF-([A-Z0-9]{8})');
    final match = refRegex.firstMatch(smsBody);
    return match?.group(1); // Returns 8-char code
  }

  // Dealer invitation extraction (NEW - SAME PATTERN)
  static String? extractDealerToken(String smsBody) {
    final dealerRegex = RegExp(r'DEALER-([a-f0-9]{32})');
    final match = dealerRegex.firstMatch(smsBody);
    return match?.group(1); // Returns 32-char token
  }
}
```

#### 2. Deep Link Handling Pattern (SAME AS REFERRAL)

```dart
// ✅ REUSE THIS PATTERN
void _handleDeepLink(BuildContext context, String link) {
  final uri = Uri.parse(link);

  // Referral link (EXISTING)
  if (uri.path.startsWith('/ref/')) {
    final code = uri.pathSegments.last;
    _handleReferral(context, code);
  }

  // Dealer invitation link (NEW - SAME PATTERN)
  else if (uri.path.startsWith('/dealer-invitation/')) {
    final fullToken = uri.pathSegments.last; // DEALER-abc123...
    if (fullToken.startsWith('DEALER-')) {
      final token = fullToken.substring(7); // Remove prefix
      _handleDealerInvitation(context, token);
    }
  }
}
```

#### 3. Token Storage Pattern (SAME AS SEND-LINK)

```dart
// ✅ REUSE THIS PATTERN
class PendingActionStorage {
  // Store pending action for after login
  static Future<void> storePendingAction(String type, String value) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('pending_action_type', type);
    await prefs.setString('pending_action_value', value);
    await prefs.setInt('pending_action_timestamp', DateTime.now().millisecondsSinceEpoch);
  }

  // Retrieve and clear pending action
  static Future<Map<String, String>?> getPendingAction() async {
    final prefs = await SharedPreferences.getInstance();
    final type = prefs.getString('pending_action_type');
    final value = prefs.getString('pending_action_value');

    if (type != null && value != null) {
      // Clear stored action
      await prefs.remove('pending_action_type');
      await prefs.remove('pending_action_value');
      await prefs.remove('pending_action_timestamp');

      return {'type': type, 'value': value};
    }

    return null;
  }
}

// Usage for dealer invitation
await PendingActionStorage.storePendingAction('dealer_invitation', token);

// After login
final pending = await PendingActionStorage.getPendingAction();
if (pending?['type'] == 'dealer_invitation') {
  final token = pending!['value'];
  // Navigate to invitation screen
}
```

### ⚠️ Critical Differences to Note

#### 1. Email Verification (UNIQUE TO DEALER INVITATION)

```dart
// ❌ NOT IN REFERRAL/SEND-LINK
// ✅ REQUIRED FOR DEALER INVITATION

// User MUST login with the exact email from invitation
// Backend validates: invitation.Email == currentUser.Email
```

**Implementation:**
```dart
// After login, check email match
final userEmail = authService.getCurrentUserEmail();
final invitationEmail = invitation.dealerEmail;

if (userEmail.toLowerCase() != invitationEmail.toLowerCase()) {
  showDialog(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('Email Uyuşmuyor'),
      content: Text(
        'Bu davetiye $invitationEmail adresine gönderilmiştir. '
        'Lütfen doğru email ile giriş yapın.',
      ),
      actions: [
        TextButton(
          onPressed: () {
            // Logout and show login screen
            authService.logout();
            Navigator.of(context).pop();
          },
          child: Text('Farklı Hesap ile Giriş Yap'),
        ),
      ],
    ),
  );
  return;
}
```

#### 2. Token Length (32 vs 8 characters)

| System | Token Length | Example | Regex |
|--------|--------------|---------|-------|
| Referral | 8 chars | `ABC12345` | `[A-Z0-9]{8}` |
| Send-Link | 8 chars | `X3K9L2M8` | `[A-Z0-9]{8}` |
| **Dealer** | **32 chars** | `a1b2c3d4e5f67890...` | `[a-f0-9]{32}` |

```dart
// ⚠️ DIFFERENT VALIDATION
bool isValidReferralCode(String code) => code.length == 8;
bool isValidDealerToken(String token) => token.length == 32;
```

#### 3. Role Assignment (UNIQUE TO DEALER INVITATION)

```dart
// ❌ NOT IN REFERRAL/SEND-LINK
// ✅ AUTOMATIC IN DEALER INVITATION

// Backend automatically assigns "Sponsor" role to dealer
// No mobile action required - just be aware
```

**What this means for mobile:**
- After accepting invitation, user becomes a Sponsor
- UI should check for Sponsor role and show dealer features
- Dealer can now distribute codes to farmers

```dart
// Check if user has Sponsor role
final hasProducerRole = authService.hasRole('Sponsor');

if (hasSponsorRole) {
  // Show dealer-specific features
  // - My Codes screen
  // - Send Code to Farmer
  // - Dealer Analytics
}
```

---

## ❌ Error Handling

### Error Handling Matrix

| Error | HTTP Status | Response | Mobile Action |
|-------|-------------|----------|---------------|
| **Token not found** | 200 OK | `success: false` | Show "Davetiye bulunamadı" error |
| **Expired invitation** | 200 OK | `success: false` | Show "Davetiye süresi dolmuş" error |
| **Already accepted** | 200 OK | `success: false` | Show "Davetiye daha önce kabul edilmiş" error |
| **Email mismatch** | 200 OK | `success: false` | Show "Bu davetiye size ait değil" + Logout option |
| **Insufficient codes** | 200 OK | `success: false` | Show "Yetersiz kod" error (sponsor issue) |
| **Unauthorized** | 401 | Empty/Error | Redirect to login |
| **Network error** | N/A | Exception | Show "Bağlantı hatası" + Retry option |

### Error Message Translations

| Backend Message (Turkish) | User-Friendly Display | Action |
|---------------------------|----------------------|--------|
| `Davetiye bulunamadı` | "Davetiye bulunamadı veya geçersiz" | Go back |
| `Davetiyenin süresi dolmuş` | "Bu davetiyenin süresi dolmuş. Lütfen sponsor ile iletişime geçin" | Go back + Contact sponsor |
| `Bu davetiye daha önce kabul edilmiş` | "Bu davetiye zaten kabul edilmiş" | Go to My Codes |
| `Bu davetiye size ait değil` | "Bu davetiye $email adresine gönderilmiş. Lütfen doğru hesap ile giriş yapın" | Logout + Login |
| `Yetersiz kod` | "Sponsor'da yeterli kod kalmamış. Lütfen sponsor ile iletişime geçin" | Go back |
| `Unauthorized` | "Oturum süreniz dolmuş. Lütfen tekrar giriş yapın" | Logout + Login |

### Error Handling Implementation

```dart
class ErrorHandler {
  /// Handle API error response
  static String getUserFriendlyMessage(String backendMessage) {
    if (backendMessage.contains('bulunamadı')) {
      return 'Davetiye bulunamadı veya geçersiz';
    } else if (backendMessage.contains('süresi dolmuş')) {
      return 'Bu davetiyenin süresi dolmuş.\nLütfen sponsor ile iletişime geçin.';
    } else if (backendMessage.contains('kabul edilmiş')) {
      return 'Bu davetiye zaten kabul edilmiş';
    } else if (backendMessage.contains('size ait değil')) {
      return 'Bu davetiye başka bir email adresine gönderilmiş.\nLütfen doğru hesap ile giriş yapın.';
    } else if (backendMessage.contains('Yetersiz kod')) {
      return 'Sponsor\'da yeterli kod kalmamış.\nLütfen sponsor ile iletişime geçin.';
    } else if (backendMessage.contains('reddedilmiş')) {
      return 'Bu davetiye reddedilmiş';
    } else {
      return backendMessage;
    }
  }

  /// Show error dialog
  static void showErrorDialog(
    BuildContext context,
    String message, {
    VoidCallback? onRetry,
    VoidCallback? onContactSponsor,
  }) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Row(
          children: [
            Icon(Icons.error_outline, color: Colors.red),
            SizedBox(width: 12),
            Text('Hata'),
          ],
        ),
        content: Text(getUserFriendlyMessage(message)),
        actions: [
          if (onContactSponsor != null)
            TextButton(
              onPressed: onContactSponsor,
              child: Text('Sponsor ile İletişim'),
            ),
          if (onRetry != null)
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
                onRetry();
              },
              child: Text('Tekrar Dene'),
            ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: Text('Tamam'),
          ),
        ],
      ),
    );
  }
}
```

### Retry Logic for Network Errors

```dart
class ApiHelper {
  /// Make API call with automatic retry
  static Future<http.Response> makeRequest(
    Future<http.Response> Function() request, {
    int maxRetries = 3,
    Duration retryDelay = const Duration(seconds: 2),
  }) async {
    int attempts = 0;

    while (attempts < maxRetries) {
      try {
        final response = await request();
        return response;
      } catch (e) {
        attempts++;

        if (attempts >= maxRetries) {
          rethrow;
        }

        print('⚠️ Request failed (attempt $attempts/$maxRetries). Retrying...');
        await Future.delayed(retryDelay);
      }
    }

    throw Exception('Max retries reached');
  }
}

// Usage
final response = await ApiHelper.makeRequest(
  () => http.get(Uri.parse('$baseUrl/api/Sponsorship/dealer/invitation-details?token=$token')),
);
```

---

## ✅ Testing Checklist

### Pre-Release Testing

#### 1. SMS Reading Tests

- [ ] SMS permission requested on first launch
- [ ] Recent SMS scanned for dealer tokens
- [ ] Dealer token extracted correctly (32 chars, no "DEALER-" prefix)
- [ ] Token stored in SharedPreferences
- [ ] Incoming SMS intercepted and parsed
- [ ] Multiple token formats handled (referral, send-link, dealer)

#### 2. Deep Link Tests

**Android:**
- [ ] Deep link opens app from SMS click
- [ ] Deep link opens app from browser
- [ ] Custom scheme works (`ziraai://dealer-invitation/...`)
- [ ] HTTPS universal link works
- [ ] Token extracted correctly from deep link
- [ ] App handles deep link when not logged in (stores token)
- [ ] App handles deep link when logged in (shows invitation)

**iOS:**
- [ ] Universal link opens app from SMS click
- [ ] Universal link opens app from Safari
- [ ] Associated domains configured correctly
- [ ] Token extracted correctly from deep link

#### 3. API Integration Tests

- [ ] Get invitation details (public endpoint, no auth)
- [ ] Accept invitation (authenticated endpoint)
- [ ] Error handling for expired invitations
- [ ] Error handling for email mismatch
- [ ] Error handling for already accepted invitations
- [ ] Error handling for network errors
- [ ] Token validation (32 chars, hex only)

#### 4. Authentication Flow Tests

- [ ] Not logged in: Stores token → Shows login → Redirects to invitation
- [ ] Already logged in: Shows invitation directly
- [ ] Email matches invitation: Acceptance succeeds
- [ ] Email doesn't match: Shows error + logout option
- [ ] Token persists across app restarts
- [ ] Token cleared after successful acceptance

#### 5. UI/UX Tests

- [ ] Invitation details screen shows correctly
- [ ] Sponsor name displayed
- [ ] Code count displayed
- [ ] Package tier displayed
- [ ] Remaining days displayed (with color coding)
- [ ] Email requirement message shown
- [ ] Accept button works
- [ ] Reject button works (if implemented)
- [ ] Success dialog shows after acceptance
- [ ] Navigate to My Codes after acceptance

#### 6. Edge Cases

- [ ] Invalid token (too short, too long, invalid chars)
- [ ] Expired token (>7 days old)
- [ ] Already accepted invitation
- [ ] Network offline/timeout
- [ ] App killed during API call
- [ ] Multiple invitations (should handle each separately)
- [ ] SMS permission denied (fallback: manual token entry)

### Test Scenarios

#### Scenario 1: Happy Path (App Installed)

```
1. Sponsor sends invitation via API
2. Dealer receives SMS on phone with app installed
3. Dealer clicks deep link in SMS
4. App opens automatically
5. Dealer already logged in with correct email
6. Invitation details screen shows
7. Dealer clicks "Daveti Kabul Et"
8. Success! Codes transferred
9. Navigates to My Codes screen
```

**Expected:** ✅ All steps successful, codes visible in My Codes

#### Scenario 2: App Not Installed

```
1. Sponsor sends invitation via API
2. Dealer receives SMS on phone (no app installed)
3. Dealer clicks Play Store link in SMS
4. Dealer installs app
5. App first launch: Requests SMS permission
6. App scans recent SMS and finds dealer token
7. Token stored in SharedPreferences
8. Dealer registers with email from invitation
9. After login, app shows invitation details automatically
10. Dealer accepts invitation
```

**Expected:** ✅ Token extracted from SMS, invitation shown after login

#### Scenario 3: Email Mismatch

```
1. Dealer clicks deep link
2. Dealer logs in with different email
3. Tries to accept invitation
4. Backend returns "Bu davetiye size ait değil"
5. App shows error dialog with logout option
6. Dealer logs out and logs in with correct email
7. Accepts invitation successfully
```

**Expected:** ✅ Error shown, user can logout and retry

#### Scenario 4: Expired Invitation

```
1. Dealer receives SMS but waits >7 days
2. Dealer clicks deep link
3. App calls /invitation-details
4. Backend returns "Davetiyenin süresi dolmuş"
5. App shows expiry error message
```

**Expected:** ✅ Clear expiry message shown

### Debug Logging

Add these logs for easier troubleshooting:

```dart
class DebugLogger {
  static const bool isDebugMode = true; // Set false for production

  static void log(String tag, String message) {
    if (isDebugMode) {
      print('[$tag] $message');
    }
  }

  static void logDealerInvitation(String step, Map<String, dynamic> data) {
    if (isDebugMode) {
      print('🎁 [DEALER_INVITATION] $step');
      data.forEach((key, value) {
        print('   $key: $value');
      });
    }
  }
}

// Usage
DebugLogger.logDealerInvitation('SMS Received', {
  'smsBody': smsBody,
  'extractedToken': token,
});

DebugLogger.logDealerInvitation('Deep Link Opened', {
  'link': link,
  'extractedToken': token,
  'isLoggedIn': isLoggedIn,
});

DebugLogger.logDealerInvitation('API Call', {
  'endpoint': 'invitation-details',
  'token': token.substring(0, 8) + '...',
  'timestamp': DateTime.now(),
});
```

---

## 📞 Support & Questions

### Common Questions

**Q: What if the dealer doesn't receive SMS?**
A: Backend returns the deep link in API response. Sponsor can manually share the link via WhatsApp or other channels.

**Q: Can dealer accept multiple invitations?**
A: Yes! Each invitation is independent. Dealer can accept invitations from multiple sponsors.

**Q: What if SMS permission is denied?**
A: Deep link will still work. User can click link in SMS to open app directly.

**Q: How long is the invitation valid?**
A: 7 days by default (configurable by backend). After 7 days, invitation automatically expires.

**Q: What happens if codes run out?**
A: Backend checks available codes before acceptance. If insufficient, returns "Yetersiz kod" error.

**Q: Can invitation be revoked by sponsor?**
A: Not yet implemented. Planned for future (status would change to "Cancelled").

### Mobile Team Contact

For questions or issues with mobile integration:
- **Backend API:** Check API documentation in `SMS_BASED_DEALER_INVITATION_FLOW.md`
- **Postman Examples:** Import `SMS_Based_Dealer_Invitation_Postman_Examples.json` for testing
- **Database:** Migration scripts in `claudedocs/Dealers/Migration_*.sql`

---

## 📚 Additional Resources

### Documentation Files

1. **SMS_BASED_DEALER_INVITATION_FLOW.md** - Complete technical documentation (95KB)
2. **DealerInvite.md** - Dealer distribution methods overview
3. **Migration_AddSmsTrackingToDealerInvitations.sql** - Database migration
4. **SMS_Based_Dealer_Invitation_Postman_Examples.json** - API test collection

### Related Flows to Reference

1. **Referral System:**
   - SMS pattern: `REF-{code}`
   - Deep link: `/ref/{code}`
   - Similar token extraction and storage patterns

2. **Send-Link System:**
   - SMS pattern: `AGRI-2025-{code}`
   - Deep link: `/redeem?code={code}`
   - Similar SMS sending and delivery tracking

### API Base URLs

| Environment | Base URL | Package Name |
|-------------|----------|--------------|
| **Development** | `https://localhost:5001` | `com.ziraai.app.dev` |
| **Staging** | `https://ziraai-api-sit.up.railway.app` | `com.ziraai.app.staging` |
| **Production** | `https://ziraai.com` | `com.ziraai.app` |

---

**Version:** 1.0
**Last Updated:** 2025-01-25
**Status:** ✅ Ready for Implementation

🎉 Happy coding! If you have any questions, please reach out to the backend team.
