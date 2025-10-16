# ZiraAI Mobile Integration Guide

**Version**: 1.0
**Last Updated**: 2025-10-04
**Target Platform**: Flutter (iOS & Android)
**API Version**: v1
**Backend Branch**: `feature/referrer-tier-system` → `staging`

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [Architecture Overview](#architecture-overview)
3. [Authentication Integration](#authentication-integration)
4. [Referral System Integration](#referral-system-integration)
5. [Deep Linking Setup](#deep-linking-setup)
6. [API Reference](#api-reference)
7. [Flutter Code Examples](#flutter-code-examples)
8. [Error Handling](#error-handling)
9. [Testing Guide](#testing-guide)
10. [Best Practices](#best-practices)
11. [Troubleshooting](#troubleshooting)

---

## Executive Summary

### What's New in This Integration

This guide covers the complete integration of **two major features** implemented in the backend:

#### 1. Phone-Based Authentication (OTP)
- ✅ **2-Step OTP Registration**: SMS-based phone number registration
- ✅ **2-Step OTP Login**: Passwordless login with SMS OTP
- ✅ **Referral Code Support**: Optional referral code during registration
- ✅ **Backwards Compatible**: Email authentication still works

#### 2. Referral System
- ✅ **Referral Link Generation**: Users can invite friends via SMS/WhatsApp
- ✅ **Deep Linking**: Play Store → App → Registration with referral code
- ✅ **Automatic Rewards**: 10 credits per successful referral (configurable)
- ✅ **Real-time Data**: Credits and stats update immediately
- ✅ **Credits Priority**: Referral credits used before subscription quota

### Key Benefits for Mobile App

1. **Simplified Onboarding**: Phone number + OTP is faster than email + password
2. **Viral Growth**: Built-in referral system with automatic rewards
3. **Better UX**: Passwordless authentication reduces friction
4. **Unlimited Credits**: Users earn credits by referring friends
5. **Real-time Feedback**: All data updates instantly (no cache delays)

### Timeline Impact

- **Minimum Integration Time**: 2-3 days
- **Recommended Integration Time**: 1 week (with testing)
- **Critical Path**: Deep linking setup (requires Google Play configuration)

---

## Architecture Overview

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                 Flutter Mobile App                      │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌──────────────────────────────┐ │
│  │ Auth Service    │  │ Referral Service             │ │
│  │ - Email Auth    │  │ - Generate Links             │ │
│  │ - Phone OTP     │  │ - Track Stats                │ │
│  │ - Token Mgmt    │  │ - Deep Link Handler          │ │
│  └─────────────────┘  └──────────────────────────────┘ │
└────────────┬────────────────────────┬───────────────────┘
             │                        │
             │ REST API (JWT Bearer)  │
             ▼                        ▼
┌─────────────────────────────────────────────────────────┐
│              ZiraAI Backend API (.NET 9.0)              │
├─────────────────────────────────────────────────────────┤
│  ┌───────────────────────┐  ┌────────────────────────┐ │
│  │ AuthController        │  │ ReferralController     │ │
│  │ /register (email)     │  │ /generate              │ │
│  │ /register-phone       │  │ /stats                 │ │
│  │ /verify-phone-register│  │ /codes                 │ │
│  │ /login (email)        │  │ /credits               │ │
│  │ /login-phone          │  │ /rewards               │ │
│  │ /verify-phone-otp     │  │ /track-click (public)  │ │
│  └───────────────────────┘  │ /validate (public)     │ │
│                              └────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
             │                        │
             ▼                        ▼
┌─────────────────────────────────────────────────────────┐
│              PostgreSQL Database                        │
│  - Users                    - ReferralCodes             │
│  - UserSubscriptions        - ReferralTracking          │
│  - MobileLogins (OTP)       - ReferralRewards           │
└─────────────────────────────────────────────────────────┘

External Services:
┌──────────────────┐  ┌──────────────────┐
│   SMS Service    │  │  Play Store      │
│   (OTP Delivery) │  │  (Deep Linking)  │
└──────────────────┘  └──────────────────┘
```

### Data Flow: Complete User Journey

```
┌─────────────────────────────────────────────────────────┐
│              NEW USER ACQUISITION FLOW                  │
└─────────────────────────────────────────────────────────┘

1️⃣ REFERRER GENERATES LINK
   App → POST /api/v1/referral/generate
       → Backend creates code: ZIRA-ABC123
       → SMS/WhatsApp sent to recipient

2️⃣ REFEREE CLICKS LINK
   SMS → Play Store → App Install → Deep Link Captured
       → App extracts: ZIRA-ABC123

3️⃣ REFEREE REGISTERS (2-Step OTP)
   Step A: App → POST /api/v1/auth/register-phone
           Body: { phone, fullName, referralCode: "ZIRA-ABC123" }
           → Backend generates OTP: 123456
           → SMS sent to phone

   Step B: App → POST /api/v1/auth/verify-phone-register
           Body: { phone, code: 123456, fullName, referralCode }
           → Backend creates user + links referral
           → Returns JWT token

4️⃣ REFEREE COMPLETES FIRST ANALYSIS
   App → POST /api/v1/plantanalyses/analyze
       → Backend validates referral
       → Adds 10 credits to referrer

5️⃣ REFERRER SEES REWARDS
   App → GET /api/v1/referral/stats
       → Returns: successfulReferrals: 1, totalCreditsEarned: 10
```

---

## Authentication Integration

### Overview: Two Authentication Methods

ZiraAI supports **two independent authentication methods**:

| Method | Steps | Input | Output | Use Case |
|--------|-------|-------|--------|----------|
| **Email** | 1-step | Email + Password | JWT Token | Traditional users |
| **Phone OTP** | 2-step | Phone + OTP Code | JWT Token | Quick onboarding |

Both methods:
- ✅ Generate the same JWT token format
- ✅ Create trial subscriptions automatically
- ✅ Support referral codes (optional)
- ✅ Can be used interchangeably after registration

### Implementation Strategy

```dart
// Recommended: Support BOTH methods in your app
class AuthService {
  // Email authentication (existing)
  Future<AuthResponse> loginWithEmail(String email, String password);
  Future<AuthResponse> registerWithEmail(String email, String password, String fullName);

  // Phone authentication (NEW)
  Future<OtpResponse> requestPhoneOtp(String phone);
  Future<AuthResponse> verifyPhoneOtp(String phone, String code);
  Future<OtpResponse> requestRegistrationOtp(String phone, String fullName, {String? referralCode});
  Future<AuthResponse> verifyRegistrationOtp(String phone, String code, String fullName, {String? referralCode});
}
```

---

### Email Authentication (Existing - Preserved)

#### Login Flow

**Endpoint**: `POST /api/v1/auth/login`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "expiration": "2025-10-04T12:00:00Z",
    "claims": [
      { "id": 1, "name": "GetPlantAnalyses" }
    ]
  }
}
```

**Flutter Example:**
```dart
class EmailAuthService {
  final Dio _dio;
  static const String baseUrl = 'https://api.ziraai.com';

  Future<AuthToken> loginWithEmail(String email, String password) async {
    try {
      final response = await _dio.post(
        '$baseUrl/api/v1/auth/login',
        data: {
          'email': email,
          'password': password,
        },
      );

      if (response.data['success']) {
        return AuthToken.fromJson(response.data['data']);
      } else {
        throw AuthException(response.data['message']);
      }
    } catch (e) {
      throw AuthException('Login failed: $e');
    }
  }
}

class AuthToken {
  final String token;
  final String refreshToken;
  final DateTime expiration;
  final List<Claim> claims;

  AuthToken({
    required this.token,
    required this.refreshToken,
    required this.expiration,
    required this.claims,
  });

  factory AuthToken.fromJson(Map<String, dynamic> json) {
    return AuthToken(
      token: json['token'],
      refreshToken: json['refreshToken'],
      expiration: DateTime.parse(json['expiration']),
      claims: (json['claims'] as List)
          .map((c) => Claim.fromJson(c))
          .toList(),
    );
  }
}
```

#### Registration Flow

**Endpoint**: `POST /api/v1/auth/register`

**Request:**
```json
{
  "email": "newuser@example.com",
  "password": "SecurePassword123!",
  "fullName": "Ahmet Yılmaz",
  "mobilePhones": "05321234567",
  "referralCode": "ZIRA-ABC123"
}
```

**Notes:**
- `mobilePhones`: Optional
- `referralCode`: Optional
- Password requirements: Min 6 characters (configurable)

---

### Phone OTP Authentication (NEW)

#### Phone Login Flow (2-Step)

##### Step 1: Request OTP

**Endpoint**: `POST /api/v1/auth/login-phone`

**Request:**
```json
{
  "mobilePhone": "05321234567"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "OTP sent successfully",
  "data": {
    "message": "SAAT 10:30 TALEP ETTIGINIZ 24 SAAT GECERLI PAROLANIZ : 123456",
    "status": "Ok"
  }
}
```

**Error Response (400):**
```json
{
  "success": false,
  "message": "UserNotFound"
}
```

**Flutter Example:**
```dart
class PhoneAuthService {
  final Dio _dio;
  static const String baseUrl = 'https://api.ziraai.com';

  Future<void> requestLoginOtp(String phone) async {
    try {
      final response = await _dio.post(
        '$baseUrl/api/v1/auth/login-phone',
        data: {'mobilePhone': phone},
      );

      if (!response.data['success']) {
        throw AuthException(response.data['message']);
      }

      // In development, OTP is 123456 (fixed)
      // In production, user receives SMS
    } catch (e) {
      throw AuthException('Failed to send OTP: $e');
    }
  }
}
```

##### Step 2: Verify OTP & Get Token

**Endpoint**: `POST /api/v1/auth/verify-phone-otp`

**Request:**
```json
{
  "mobilePhone": "05321234567",
  "code": 123456
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "expiration": "2025-10-04T12:00:00Z",
    "externalUserId": "05321234567",
    "provider": "Phone",
    "claims": [
      { "id": 1, "name": "GetPlantAnalyses" }
    ]
  }
}
```

**Error Response (400):**
```json
{
  "success": false,
  "message": "Invalid or expired OTP code"
}
```

**Flutter Example:**
```dart
Future<AuthToken> verifyLoginOtp(String phone, String code) async {
  try {
    final response = await _dio.post(
      '$baseUrl/api/v1/auth/verify-phone-otp',
      data: {
        'mobilePhone': phone,
        'code': int.parse(code),
      },
    );

    if (response.data['success']) {
      return AuthToken.fromJson(response.data['data']);
    } else {
      throw AuthException(response.data['message']);
    }
  } catch (e) {
    throw AuthException('OTP verification failed: $e');
  }
}
```

**Important Notes:**
- **OTP Expiry**: 100 seconds (1 minute 40 seconds)
- **OTP Format**: 6-digit integer (123456)
- **One-Time Use**: OTP cannot be reused after verification
- **Development Mode**: Fixed OTP is `123456` for testing

---

#### Phone Registration Flow (2-Step with Referral)

##### Step 1: Request Registration OTP

**Endpoint**: `POST /api/v1/auth/register-phone`

**Request (WITHOUT Referral Code):**
```json
{
  "mobilePhone": "05321234567",
  "fullName": "Ahmet Yılmaz"
}
```

**Request (WITH Referral Code):**
```json
{
  "mobilePhone": "05321234567",
  "fullName": "Ahmet Yılmaz",
  "referralCode": "ZIRA-ABC123"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "OTP sent to 05321234567. Code: 123456 (dev mode)"
}
```

**Error Response (400):**
```json
{
  "success": false,
  "message": "Phone number is already registered"
}
```

**Flutter Example:**
```dart
Future<void> requestRegistrationOtp({
  required String phone,
  required String fullName,
  String? referralCode,
}) async {
  try {
    final data = {
      'mobilePhone': phone,
      'fullName': fullName,
    };

    if (referralCode != null && referralCode.isNotEmpty) {
      data['referralCode'] = referralCode;
    }

    final response = await _dio.post(
      '$baseUrl/api/v1/auth/register-phone',
      data: data,
    );

    if (!response.data['success']) {
      throw AuthException(response.data['message']);
    }

    // OTP sent via SMS (or shown in dev mode)
  } catch (e) {
    throw AuthException('Failed to send registration OTP: $e');
  }
}
```

##### Step 2: Verify OTP & Complete Registration

**Endpoint**: `POST /api/v1/auth/verify-phone-register`

**Request:**
```json
{
  "mobilePhone": "05321234567",
  "code": 123456,
  "fullName": "Ahmet Yılmaz",
  "referralCode": "ZIRA-ABC123"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "expiration": "2025-10-04T12:00:00Z",
    "claims": [
      { "id": 1, "name": "GetPlantAnalyses" }
    ]
  }
}
```

**Error Responses:**

Phone already registered (400):
```json
{
  "success": false,
  "message": "Phone number is already registered"
}
```

Invalid OTP (400):
```json
{
  "success": false,
  "message": "Invalid or expired OTP code"
}
```

OTP expired (400):
```json
{
  "success": false,
  "message": "OTP code has expired"
}
```

**Flutter Example:**
```dart
Future<AuthToken> verifyRegistrationOtp({
  required String phone,
  required String code,
  required String fullName,
  String? referralCode,
}) async {
  try {
    final data = {
      'mobilePhone': phone,
      'code': int.parse(code),
      'fullName': fullName,
    };

    if (referralCode != null && referralCode.isNotEmpty) {
      data['referralCode'] = referralCode;
    }

    final response = await _dio.post(
      '$baseUrl/api/v1/auth/verify-phone-register',
      data: data,
    );

    if (response.data['success']) {
      return AuthToken.fromJson(response.data['data']);
    } else {
      throw AuthException(response.data['message']);
    }
  } catch (e) {
    throw AuthException('Registration verification failed: $e');
  }
}
```

**What Happens After Registration:**
1. ✅ User created with email: `{phone}@phone.ziraai.com`
2. ✅ "Farmer" role assigned automatically
3. ✅ 30-day trial subscription created
4. ✅ Referral code linked (if provided)
5. ✅ JWT token generated
6. ✅ User is logged in immediately

**Important Notes:**
- **OTP Expiry**: 5 minutes (300 seconds) - longer than login OTP
- **Referral Code**: Optional - registration succeeds even if code is invalid
- **No Password**: Phone users have no password (passwordless auth)
- **Trial Subscription**: Automatically created with 30-day validity

---

### Token Management (Same for Both Methods)

#### Token Storage

```dart
class TokenManager {
  static const String _tokenKey = 'auth_token';
  static const String _refreshTokenKey = 'refresh_token';
  static const String _expirationKey = 'token_expiration';

  Future<void> saveToken(AuthToken token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, token.token);
    await prefs.setString(_refreshTokenKey, token.refreshToken);
    await prefs.setString(_expirationKey, token.expiration.toIso8601String());
  }

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();

    // Check if token expired
    final expirationStr = prefs.getString(_expirationKey);
    if (expirationStr != null) {
      final expiration = DateTime.parse(expirationStr);
      if (DateTime.now().isAfter(expiration)) {
        // Token expired, refresh it
        return await refreshToken();
      }
    }

    return prefs.getString(_tokenKey);
  }

  Future<String?> refreshToken() async {
    // Implement token refresh logic
    // POST /api/v1/auth/refresh-token
    throw UnimplementedError('Token refresh not yet implemented');
  }

  Future<void> clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_refreshTokenKey);
    await prefs.remove(_expirationKey);
  }
}
```

#### HTTP Interceptor (Auto Token Attachment)

```dart
class AuthInterceptor extends Interceptor {
  final TokenManager _tokenManager;

  AuthInterceptor(this._tokenManager);

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Add token to all requests
    final token = await _tokenManager.getToken();
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioError err, ErrorInterceptorHandler handler) {
    if (err.response?.statusCode == 401) {
      // Token expired or invalid
      _tokenManager.clearToken();
      // Navigate to login screen
      // NavigationService.instance.pushNamedAndRemoveUntil('/login');
    }
    handler.next(err);
  }
}

// Usage
final dio = Dio(BaseOptions(baseUrl: 'https://api.ziraai.com'));
dio.interceptors.add(AuthInterceptor(tokenManager));
```

---

## Referral System Integration

### Overview

The referral system enables users to earn credits by inviting friends. Complete flow:

```
1. GENERATE → User creates referral link
2. SEND → Link sent via SMS/WhatsApp
3. CLICK → Friend clicks link (tracked)
4. REGISTER → Friend signs up with code
5. ANALYZE → Friend completes first analysis
6. REWARD → User receives 10 credits
```

### Feature Checklist for Mobile App

- [ ] **Referral Link Generation UI**: Button to generate and send links
- [ ] **Deep Link Handling**: Capture referral code from Play Store
- [ ] **Registration Form Update**: Add optional referral code field
- [ ] **Referral Stats Dashboard**: Show earned credits and successful referrals
- [ ] **Credit Usage Display**: Show referral credits in usage breakdown
- [ ] **Share Sheet Integration**: Native share for SMS/WhatsApp/Email

---

### 1. Generate Referral Link

**Endpoint**: `POST /api/v1/referral/generate`

**Auth**: Required (JWT Bearer)

**Request:**
```json
{
  "deliveryMethod": 3,
  "phoneNumbers": ["05321234567", "05339876543"],
  "customMessage": "ZiraAI'yi dene ve bitkilerini analiz et!"
}
```

**Delivery Methods:**
- `1` = SMS only
- `2` = WhatsApp only
- `3` = Both SMS + WhatsApp (Hybrid)

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Referral links generated and sent successfully",
  "data": {
    "referralCode": "ZIRA-ABC123",
    "deepLink": "ziraai://referral?code=ZIRA-ABC123",
    "playStoreLink": "https://play.google.com/store/apps/details?id=com.ziraai&referrer=ZIRA-ABC123",
    "expiresAt": "2025-11-03T10:30:00",
    "deliveryStatuses": [
      {
        "phoneNumber": "05321234567",
        "method": "SMS",
        "status": "Sent",
        "errorMessage": null
      },
      {
        "phoneNumber": "05321234567",
        "method": "WhatsApp",
        "status": "Sent",
        "errorMessage": null
      }
    ]
  }
}
```

**Flutter Example:**
```dart
class ReferralService {
  final Dio _dio;

  Future<ReferralLinkResponse> generateReferralLink({
    required List<String> phoneNumbers,
    DeliveryMethod method = DeliveryMethod.both,
    String? customMessage,
  }) async {
    final token = await TokenManager().getToken();

    final response = await _dio.post(
      '/api/v1/referral/generate',
      options: Options(headers: {'Authorization': 'Bearer $token'}),
      data: {
        'deliveryMethod': method.value,
        'phoneNumbers': phoneNumbers,
        if (customMessage != null) 'customMessage': customMessage,
      },
    );

    if (response.data['success']) {
      return ReferralLinkResponse.fromJson(response.data['data']);
    } else {
      throw ReferralException(response.data['message']);
    }
  }
}

enum DeliveryMethod {
  sms(1),
  whatsApp(2),
  both(3);

  final int value;
  const DeliveryMethod(this.value);
}

class ReferralLinkResponse {
  final String referralCode;
  final String deepLink;
  final String playStoreLink;
  final DateTime expiresAt;
  final List<DeliveryStatus> deliveryStatuses;

  ReferralLinkResponse({
    required this.referralCode,
    required this.deepLink,
    required this.playStoreLink,
    required this.expiresAt,
    required this.deliveryStatuses,
  });

  factory ReferralLinkResponse.fromJson(Map<String, dynamic> json) {
    return ReferralLinkResponse(
      referralCode: json['referralCode'],
      deepLink: json['deepLink'],
      playStoreLink: json['playStoreLink'],
      expiresAt: DateTime.parse(json['expiresAt']),
      deliveryStatuses: (json['deliveryStatuses'] as List)
          .map((d) => DeliveryStatus.fromJson(d))
          .toList(),
    );
  }
}
```

**UI Example:**
```dart
class ReferralGenerateScreen extends StatelessWidget {
  final ReferralService _referralService = ReferralService();

  Future<void> _generateAndShare() async {
    try {
      // Generate link (no phone numbers = just create code)
      final result = await _referralService.generateReferralLink(
        phoneNumbers: [],
        method: DeliveryMethod.both,
      );

      // Share using native share sheet
      await Share.share(
        'Merhaba! ZiraAI ile bitkilerini analiz et.\n\n'
        'Kayıt olurken bu kodu kullan: ${result.referralCode}\n\n'
        'İndir: ${result.playStoreLink}',
        subject: 'ZiraAI Davet Linki',
      );
    } catch (e) {
      // Show error
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Hata: $e')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Arkadaşını Davet Et')),
      body: Center(
        child: ElevatedButton.icon(
          onPressed: _generateAndShare,
          icon: Icon(Icons.share),
          label: Text('Davet Linki Oluştur'),
        ),
      ),
    );
  }
}
```

---

### 2. Get Referral Statistics

**Endpoint**: `GET /api/v1/referral/stats`

**Auth**: Required (JWT Bearer)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalReferrals": 15,
    "successfulReferrals": 8,
    "pendingReferrals": 3,
    "totalCreditsEarned": 80,
    "referralBreakdown": {
      "clicked": 15,
      "registered": 11,
      "validated": 9,
      "rewarded": 8
    }
  }
}
```

**Flutter Example:**
```dart
class ReferralStats {
  final int totalReferrals;
  final int successfulReferrals;
  final int pendingReferrals;
  final int totalCreditsEarned;
  final ReferralBreakdown breakdown;

  ReferralStats({
    required this.totalReferrals,
    required this.successfulReferrals,
    required this.pendingReferrals,
    required this.totalCreditsEarned,
    required this.breakdown,
  });

  factory ReferralStats.fromJson(Map<String, dynamic> json) {
    return ReferralStats(
      totalReferrals: json['totalReferrals'],
      successfulReferrals: json['successfulReferrals'],
      pendingReferrals: json['pendingReferrals'],
      totalCreditsEarned: json['totalCreditsEarned'],
      breakdown: ReferralBreakdown.fromJson(json['referralBreakdown']),
    );
  }
}

class ReferralBreakdown {
  final int clicked;
  final int registered;
  final int validated;
  final int rewarded;

  ReferralBreakdown({
    required this.clicked,
    required this.registered,
    required this.validated,
    required this.rewarded,
  });

  factory ReferralBreakdown.fromJson(Map<String, dynamic> json) {
    return ReferralBreakdown(
      clicked: json['clicked'],
      registered: json['registered'],
      validated: json['validated'],
      rewarded: json['rewarded'],
    );
  }
}

// Service method
Future<ReferralStats> getReferralStats() async {
  final token = await TokenManager().getToken();

  final response = await _dio.get(
    '/api/v1/referral/stats',
    options: Options(headers: {'Authorization': 'Bearer $token'}),
  );

  if (response.data['success']) {
    return ReferralStats.fromJson(response.data['data']);
  } else {
    throw ReferralException(response.data['message']);
  }
}
```

**UI Example (Stats Dashboard):**
```dart
class ReferralStatsWidget extends StatelessWidget {
  final ReferralStats stats;

  const ReferralStatsWidget({required this.stats});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Referans İstatistikleri',
                style: Theme.of(context).textTheme.headline6),
            SizedBox(height: 16),
            _buildStatRow('Toplam Referanslar', stats.totalReferrals),
            _buildStatRow('Başarılı Referanslar', stats.successfulReferrals),
            _buildStatRow('Bekleyen Referanslar', stats.pendingReferrals),
            _buildStatRow('Kazanılan Kredi', stats.totalCreditsEarned,
                          highlight: true),
            Divider(height: 24),
            Text('Detaylı Dökümüm',
                style: Theme.of(context).textTheme.subtitle2),
            SizedBox(height: 8),
            _buildBreakdownRow('Tıklanan', stats.breakdown.clicked),
            _buildBreakdownRow('Kayıt Olan', stats.breakdown.registered),
            _buildBreakdownRow('Doğrulanan', stats.breakdown.validated),
            _buildBreakdownRow('Ödüllendirilen', stats.breakdown.rewarded),
          ],
        ),
      ),
    );
  }

  Widget _buildStatRow(String label, int value, {bool highlight = false}) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label),
          Text(
            value.toString(),
            style: TextStyle(
              fontWeight: highlight ? FontWeight.bold : FontWeight.normal,
              fontSize: highlight ? 18 : 16,
              color: highlight ? Colors.green : null,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBreakdownRow(String label, int value) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontSize: 14)),
          Text(value.toString(), style: TextStyle(fontSize: 14)),
        ],
      ),
    );
  }
}
```

---

### 3. Get Credit Breakdown

**Endpoint**: `GET /api/v1/referral/credits`

**Auth**: Required (JWT Bearer)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalEarned": 80,
    "totalUsed": 25,
    "currentBalance": 55
  }
}
```

**Flutter Example:**
```dart
class CreditBreakdown {
  final int totalEarned;
  final int totalUsed;
  final int currentBalance;

  CreditBreakdown({
    required this.totalEarned,
    required this.totalUsed,
    required this.currentBalance,
  });

  factory CreditBreakdown.fromJson(Map<String, dynamic> json) {
    return CreditBreakdown(
      totalEarned: json['totalEarned'],
      totalUsed: json['totalUsed'],
      currentBalance: json['currentBalance'],
    );
  }
}

Future<CreditBreakdown> getCreditBreakdown() async {
  final token = await TokenManager().getToken();

  final response = await _dio.get(
    '/api/v1/referral/credits',
    options: Options(headers: {'Authorization': 'Bearer $token'}),
  );

  if (response.data['success']) {
    return CreditBreakdown.fromJson(response.data['data']);
  } else {
    throw ReferralException(response.data['message']);
  }
}
```

---

### 4. Validate Referral Code (Public Endpoint)

**Endpoint**: `POST /api/v1/referral/validate`

**Auth**: NOT required (public endpoint)

**Purpose**: Check if a referral code is valid before using it during registration

**Request:**
```json
{
  "code": "ZIRA-ABC123"
}
```

**Response (200 OK - Valid):**
```json
{
  "success": true,
  "message": "Referral code is valid and active"
}
```

**Response (400 Bad Request - Invalid):**
```json
{
  "success": false,
  "message": "Referral code has expired"
}
```

**Flutter Example:**
```dart
Future<bool> validateReferralCode(String code) async {
  try {
    final response = await _dio.post(
      '/api/v1/referral/validate',
      data: {'code': code},
    );

    return response.data['success'];
  } catch (e) {
    return false;
  }
}

// Usage in registration form
class RegistrationForm extends StatefulWidget {
  @override
  _RegistrationFormState createState() => _RegistrationFormState();
}

class _RegistrationFormState extends State<RegistrationForm> {
  final TextEditingController _referralCodeController = TextEditingController();
  bool _isValidatingCode = false;
  bool? _isCodeValid;

  Future<void> _validateCode() async {
    if (_referralCodeController.text.isEmpty) return;

    setState(() => _isValidatingCode = true);

    final isValid = await ReferralService().validateReferralCode(
      _referralCodeController.text.trim(),
    );

    setState(() {
      _isValidatingCode = false;
      _isCodeValid = isValid;
    });
  }

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: _referralCodeController,
      decoration: InputDecoration(
        labelText: 'Referans Kodu (Opsiyonel)',
        hintText: 'ZIRA-ABC123',
        suffixIcon: _isValidatingCode
            ? CircularProgressIndicator()
            : _isCodeValid != null
                ? Icon(_isCodeValid! ? Icons.check_circle : Icons.error,
                      color: _isCodeValid! ? Colors.green : Colors.red)
                : IconButton(
                    icon: Icon(Icons.check),
                    onPressed: _validateCode,
                  ),
      ),
      onChanged: (_) => setState(() => _isCodeValid = null),
      textCapitalization: TextCapitalization.characters,
    );
  }
}
```

---

## Deep Linking Setup

### Overview

Deep linking enables the app to capture referral codes from Play Store installs:

```
User clicks link → Play Store → Install → App Launch → Referral code captured
```

### Implementation Steps

#### 1. Configure Android Manifest

**File**: `android/app/src/main/AndroidManifest.xml`

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.ziraai">

    <application>
        <activity
            android:name=".MainActivity"
            android:launchMode="singleTop">

            <!-- Deep Link Intent Filter -->
            <intent-filter android:autoVerify="true">
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />

                <!-- App Links (HTTPS) -->
                <data
                    android:scheme="https"
                    android:host="ziraai.com"
                    android:pathPrefix="/ref" />

                <!-- Custom Scheme -->
                <data android:scheme="ziraai" />
            </intent-filter>

            <!-- Install Referrer (Play Store) -->
            <receiver
                android:name="com.google.android.gms.analytics.CampaignTrackingReceiver"
                android:exported="true">
                <intent-filter>
                    <action android:name="com.android.vending.INSTALL_REFERRER" />
                </intent-filter>
            </receiver>
        </activity>
    </application>
</manifest>
```

#### 2. Add Dependencies

**File**: `pubspec.yaml`

```yaml
dependencies:
  flutter:
    sdk: flutter

  # Deep linking
  uni_links: ^0.5.1
  android_play_install_referrer: ^0.3.0

  # State management & DI
  provider: ^6.0.5
  get_it: ^7.2.0

  # HTTP & Storage
  dio: ^5.0.0
  shared_preferences: ^2.0.15

  # Share functionality
  share_plus: ^6.0.0
```

#### 3. Implement Deep Link Handler

**File**: `lib/services/deep_link_service.dart`

```dart
import 'dart:async';
import 'package:uni_links/uni_links.dart';
import 'package:android_play_install_referrer/android_play_install_referrer.dart';

class DeepLinkService {
  StreamSubscription? _linkSubscription;
  String? _capturedReferralCode;

  String? get referralCode => _capturedReferralCode;

  Future<void> initialize() async {
    // Check initial link (app was closed and opened via link)
    try {
      final initialLink = await getInitialUri();
      if (initialLink != null) {
        _handleDeepLink(initialLink);
      }
    } catch (e) {
      print('Error getting initial link: $e');
    }

    // Listen for links while app is running
    _linkSubscription = uriLinkStream.listen(
      (Uri? uri) {
        if (uri != null) {
          _handleDeepLink(uri);
        }
      },
      onError: (err) {
        print('Error listening to deep links: $err');
      },
    );

    // Get Play Store install referrer (Android only)
    await _getPlayStoreReferrer();
  }

  void _handleDeepLink(Uri uri) {
    print('Deep link received: $uri');

    // Extract referral code from URI
    // Format 1: ziraai://referral?code=ZIRA-ABC123
    // Format 2: https://ziraai.com/ref/ZIRA-ABC123

    if (uri.scheme == 'ziraai' && uri.host == 'referral') {
      // Custom scheme
      _capturedReferralCode = uri.queryParameters['code'];
    } else if (uri.host == 'ziraai.com' && uri.path.startsWith('/ref/')) {
      // HTTPS scheme
      _capturedReferralCode = uri.path.replaceFirst('/ref/', '');
    }

    if (_capturedReferralCode != null) {
      print('Referral code captured: $_capturedReferralCode');
      // Notify listeners (e.g., navigation to registration)
      _onReferralCodeCaptured(_capturedReferralCode!);
    }
  }

  Future<void> _getPlayStoreReferrer() async {
    try {
      final referrer = await AndroidPlayInstallReferrer.installReferrer;

      if (referrer != null && referrer.isNotEmpty) {
        print('Play Store referrer: $referrer');

        // Extract referral code from Play Store parameter
        // Format: referrer=ZIRA-ABC123
        final regex = RegExp(r'ZIRA-[A-Z0-9]{6}');
        final match = regex.firstMatch(referrer);

        if (match != null) {
          _capturedReferralCode = match.group(0);
          print('Referral code from Play Store: $_capturedReferralCode');
          _onReferralCodeCaptured(_capturedReferralCode!);
        }
      }
    } catch (e) {
      print('Error getting Play Store referrer: $e');
    }
  }

  void _onReferralCodeCaptured(String code) {
    // Save to local storage
    SharedPreferences.getInstance().then((prefs) {
      prefs.setString('pending_referral_code', code);
    });

    // Notify app (e.g., using EventBus or Provider)
    // EventBus.instance.fire(ReferralCodeCapturedEvent(code));
  }

  void dispose() {
    _linkSubscription?.cancel();
  }
}
```

#### 4. Initialize at App Startup

**File**: `lib/main.dart`

```dart
void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize deep link service
  final deepLinkService = DeepLinkService();
  await deepLinkService.initialize();

  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ZiraAI',
      home: SplashScreen(),
    );
  }
}
```

#### 5. Use Referral Code in Registration

**File**: `lib/screens/registration_screen.dart`

```dart
class RegistrationScreen extends StatefulWidget {
  @override
  _RegistrationScreenState createState() => _RegistrationScreenState();
}

class _RegistrationScreenState extends State<RegistrationScreen> {
  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _referralCodeController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadPendingReferralCode();
  }

  Future<void> _loadPendingReferralCode() async {
    final prefs = await SharedPreferences.getInstance();
    final pendingCode = prefs.getString('pending_referral_code');

    if (pendingCode != null) {
      setState(() {
        _referralCodeController.text = pendingCode;
      });

      // Show banner
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Referans kodu otomatik eklendi: $pendingCode'),
          backgroundColor: Colors.green,
          duration: Duration(seconds: 3),
        ),
      );

      // Clear pending code
      await prefs.remove('pending_referral_code');
    }
  }

  Future<void> _register() async {
    // Step 1: Request OTP
    await AuthService().requestRegistrationOtp(
      phone: _phoneController.text,
      fullName: _nameController.text,
      referralCode: _referralCodeController.text.isNotEmpty
          ? _referralCodeController.text
          : null,
    );

    // Navigate to OTP verification screen
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => OtpVerificationScreen(
          phone: _phoneController.text,
          fullName: _nameController.text,
          referralCode: _referralCodeController.text.isNotEmpty
              ? _referralCodeController.text
              : null,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Kayıt Ol')),
      body: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          children: [
            TextField(
              controller: _phoneController,
              decoration: InputDecoration(labelText: 'Telefon Numarası'),
              keyboardType: TextInputType.phone,
            ),
            TextField(
              controller: _nameController,
              decoration: InputDecoration(labelText: 'Ad Soyad'),
            ),
            TextField(
              controller: _referralCodeController,
              decoration: InputDecoration(
                labelText: 'Referans Kodu (Opsiyonel)',
                suffixIcon: Icon(Icons.card_giftcard),
              ),
              textCapitalization: TextCapitalization.characters,
            ),
            SizedBox(height: 24),
            ElevatedButton(
              onPressed: _register,
              child: Text('Kayıt Ol'),
            ),
          ],
        ),
      ),
    );
  }
}
```

---

## API Reference

### Base Configuration

```dart
class ApiConfig {
  // Development
  static const String devBaseUrl = 'https://localhost:5001';

  // Staging
  static const String stagingBaseUrl = 'https://ziraai-staging.up.railway.app';

  // Production
  static const String prodBaseUrl = 'https://api.ziraai.com';

  // Current environment
  static const String baseUrl = stagingBaseUrl; // Change based on build flavor

  // API Version
  static const String apiVersion = 'v1';

  // Endpoints
  static const String authBase = '/api/$apiVersion/auth';
  static const String referralBase = '/api/$apiVersion/referral';
  static const String plantAnalysisBase = '/api/$apiVersion/plantanalyses';
  static const String subscriptionBase = '/api/$apiVersion/subscription';
}
```

### Error Response Format

All endpoints return errors in the same format:

```json
{
  "success": false,
  "message": "Error message in English or Turkish",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

**Common Error Codes:**

| HTTP Status | Message | Meaning |
|------------|---------|---------|
| 400 | Invalid or expired OTP code | OTP is wrong or expired |
| 400 | Phone number is already registered | Duplicate phone number |
| 400 | Referral code has expired | Referral link expired (30 days) |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User lacks permission |
| 404 | UserNotFound | User doesn't exist |
| 429 | Too Many Requests | Rate limit exceeded (future) |
| 500 | Internal Server Error | Server-side error |

**Flutter Error Handling:**
```dart
class ApiException implements Exception {
  final int statusCode;
  final String message;
  final List<String>? errors;

  ApiException({
    required this.statusCode,
    required this.message,
    this.errors,
  });

  factory ApiException.fromResponse(Response response) {
    final data = response.data;
    return ApiException(
      statusCode: response.statusCode ?? 500,
      message: data['message'] ?? 'Unknown error',
      errors: data['errors'] != null
          ? List<String>.from(data['errors'])
          : null,
    );
  }

  @override
  String toString() {
    if (errors != null && errors!.isNotEmpty) {
      return '$message: ${errors!.join(", ")}';
    }
    return message;
  }
}

// Usage in Dio interceptor
dio.interceptors.add(
  InterceptorsWrapper(
    onError: (DioError e, ErrorInterceptorHandler handler) {
      if (e.response != null) {
        handler.reject(
          DioError(
            requestOptions: e.requestOptions,
            error: ApiException.fromResponse(e.response!),
          ),
        );
      } else {
        handler.next(e);
      }
    },
  ),
);
```

---

## Flutter Code Examples

### Complete Authentication Service

```dart
// File: lib/services/auth_service.dart

import 'package:dio/dio.dart';
import '../models/auth_token.dart';
import '../models/api_response.dart';
import 'token_manager.dart';
import 'api_config.dart';

class AuthService {
  final Dio _dio;
  final TokenManager _tokenManager;

  AuthService({Dio? dio, TokenManager? tokenManager})
      : _dio = dio ?? Dio(BaseOptions(baseUrl: ApiConfig.baseUrl)),
        _tokenManager = tokenManager ?? TokenManager();

  // ========================================
  // EMAIL AUTHENTICATION
  // ========================================

  Future<AuthToken> loginWithEmail(String email, String password) async {
    try {
      final response = await _dio.post(
        '${ApiConfig.authBase}/login',
        data: {
          'email': email,
          'password': password,
        },
      );

      if (response.data['success']) {
        final token = AuthToken.fromJson(response.data['data']);
        await _tokenManager.saveToken(token);
        return token;
      } else {
        throw AuthException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  Future<AuthToken> registerWithEmail({
    required String email,
    required String password,
    required String fullName,
    String? mobilePhone,
    String? referralCode,
  }) async {
    try {
      final data = {
        'email': email,
        'password': password,
        'fullName': fullName,
      };

      if (mobilePhone != null) data['mobilePhones'] = mobilePhone;
      if (referralCode != null) data['referralCode'] = referralCode;

      final response = await _dio.post(
        '${ApiConfig.authBase}/register',
        data: data,
      );

      if (response.data['success']) {
        // After registration, login automatically
        return await loginWithEmail(email, password);
      } else {
        throw AuthException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  // ========================================
  // PHONE OTP AUTHENTICATION
  // ========================================

  Future<void> requestLoginOtp(String phone) async {
    try {
      final response = await _dio.post(
        '${ApiConfig.authBase}/login-phone',
        data: {'mobilePhone': phone},
      );

      if (!response.data['success']) {
        throw AuthException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  Future<AuthToken> verifyLoginOtp(String phone, String code) async {
    try {
      final response = await _dio.post(
        '${ApiConfig.authBase}/verify-phone-otp',
        data: {
          'mobilePhone': phone,
          'code': int.parse(code),
        },
      );

      if (response.data['success']) {
        final token = AuthToken.fromJson(response.data['data']);
        await _tokenManager.saveToken(token);
        return token;
      } else {
        throw AuthException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  Future<void> requestRegistrationOtp({
    required String phone,
    required String fullName,
    String? referralCode,
  }) async {
    try {
      final data = {
        'mobilePhone': phone,
        'fullName': fullName,
      };

      if (referralCode != null && referralCode.isNotEmpty) {
        data['referralCode'] = referralCode;
      }

      final response = await _dio.post(
        '${ApiConfig.authBase}/register-phone',
        data: data,
      );

      if (!response.data['success']) {
        throw AuthException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  Future<AuthToken> verifyRegistrationOtp({
    required String phone,
    required String code,
    required String fullName,
    String? referralCode,
  }) async {
    try {
      final data = {
        'mobilePhone': phone,
        'code': int.parse(code),
        'fullName': fullName,
      };

      if (referralCode != null && referralCode.isNotEmpty) {
        data['referralCode'] = referralCode;
      }

      final response = await _dio.post(
        '${ApiConfig.authBase}/verify-phone-register',
        data: data,
      );

      if (response.data['success']) {
        final token = AuthToken.fromJson(response.data['data']);
        await _tokenManager.saveToken(token);
        return token;
      } else {
        throw AuthException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  // ========================================
  // TOKEN MANAGEMENT
  // ========================================

  Future<bool> isAuthenticated() async {
    final token = await _tokenManager.getToken();
    return token != null;
  }

  Future<void> logout() async {
    await _tokenManager.clearToken();
  }

  // ========================================
  // ERROR HANDLING
  // ========================================

  AuthException _handleDioError(DioError e) {
    if (e.response != null) {
      final message = e.response!.data['message'] ?? 'Unknown error';
      return AuthException(message);
    } else if (e.type == DioErrorType.connectTimeout ||
               e.type == DioErrorType.receiveTimeout) {
      return AuthException('Connection timeout');
    } else if (e.type == DioErrorType.other) {
      return AuthException('No internet connection');
    } else {
      return AuthException('Network error: ${e.message}');
    }
  }
}

class AuthException implements Exception {
  final String message;
  AuthException(this.message);

  @override
  String toString() => message;
}
```

### Complete Referral Service

```dart
// File: lib/services/referral_service.dart

import 'package:dio/dio.dart';
import '../models/referral_models.dart';
import 'token_manager.dart';
import 'api_config.dart';

class ReferralService {
  final Dio _dio;
  final TokenManager _tokenManager;

  ReferralService({Dio? dio, TokenManager? tokenManager})
      : _dio = dio ?? Dio(BaseOptions(baseUrl: ApiConfig.baseUrl)),
        _tokenManager = tokenManager ?? TokenManager();

  // ========================================
  // REFERRAL LINK GENERATION
  // ========================================

  Future<ReferralLinkResponse> generateReferralLink({
    required List<String> phoneNumbers,
    DeliveryMethod method = DeliveryMethod.both,
    String? customMessage,
  }) async {
    try {
      final token = await _tokenManager.getToken();

      final response = await _dio.post(
        '${ApiConfig.referralBase}/generate',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
        data: {
          'deliveryMethod': method.value,
          'phoneNumbers': phoneNumbers,
          if (customMessage != null) 'customMessage': customMessage,
        },
      );

      if (response.data['success']) {
        return ReferralLinkResponse.fromJson(response.data['data']);
      } else {
        throw ReferralException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  // ========================================
  // REFERRAL STATISTICS
  // ========================================

  Future<ReferralStats> getReferralStats() async {
    try {
      final token = await _tokenManager.getToken();

      final response = await _dio.get(
        '${ApiConfig.referralBase}/stats',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );

      if (response.data['success']) {
        return ReferralStats.fromJson(response.data['data']);
      } else {
        throw ReferralException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  Future<List<ReferralCode>> getUserReferralCodes() async {
    try {
      final token = await _tokenManager.getToken();

      final response = await _dio.get(
        '${ApiConfig.referralBase}/codes',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );

      if (response.data['success']) {
        final codes = response.data['data'] as List;
        return codes.map((c) => ReferralCode.fromJson(c)).toList();
      } else {
        throw ReferralException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  Future<CreditBreakdown> getCreditBreakdown() async {
    try {
      final token = await _tokenManager.getToken();

      final response = await _dio.get(
        '${ApiConfig.referralBase}/credits',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );

      if (response.data['success']) {
        return CreditBreakdown.fromJson(response.data['data']);
      } else {
        throw ReferralException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  Future<List<ReferralReward>> getReferralRewards() async {
    try {
      final token = await _tokenManager.getToken();

      final response = await _dio.get(
        '${ApiConfig.referralBase}/rewards',
        options: Options(headers: {'Authorization': 'Bearer $token'}),
      );

      if (response.data['success']) {
        final rewards = response.data['data'] as List;
        return rewards.map((r) => ReferralReward.fromJson(r)).toList();
      } else {
        throw ReferralException(response.data['message']);
      }
    } on DioError catch (e) {
      throw _handleDioError(e);
    }
  }

  // ========================================
  // PUBLIC ENDPOINTS (No Auth Required)
  // ========================================

  Future<bool> validateReferralCode(String code) async {
    try {
      final response = await _dio.post(
        '${ApiConfig.referralBase}/validate',
        data: {'code': code},
      );

      return response.data['success'];
    } catch (e) {
      return false;
    }
  }

  Future<void> trackReferralClick(String code, String deviceId) async {
    try {
      await _dio.post(
        '${ApiConfig.referralBase}/track-click',
        data: {
          'code': code,
          'deviceId': deviceId,
        },
      );
    } catch (e) {
      // Silent fail - click tracking is non-critical
      print('Failed to track referral click: $e');
    }
  }

  // ========================================
  // ERROR HANDLING
  // ========================================

  ReferralException _handleDioError(DioError e) {
    if (e.response != null) {
      final message = e.response!.data['message'] ?? 'Unknown error';
      return ReferralException(message);
    } else {
      return ReferralException('Network error: ${e.message}');
    }
  }
}

class ReferralException implements Exception {
  final String message;
  ReferralException(this.message);

  @override
  String toString() => message;
}

enum DeliveryMethod {
  sms(1),
  whatsApp(2),
  both(3);

  final int value;
  const DeliveryMethod(this.value);
}
```

---

## Error Handling

### Centralized Error Handler

```dart
// File: lib/utils/error_handler.dart

import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../services/referral_service.dart';

class ErrorHandler {
  static void handle(BuildContext context, dynamic error) {
    String message;
    Color backgroundColor;

    if (error is AuthException) {
      message = _getAuthErrorMessage(error.message);
      backgroundColor = Colors.red;
    } else if (error is ReferralException) {
      message = _getReferralErrorMessage(error.message);
      backgroundColor = Colors.orange;
    } else {
      message = 'Beklenmeyen bir hata oluştu';
      backgroundColor = Colors.red;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: backgroundColor,
        duration: Duration(seconds: 4),
        action: SnackBarAction(
          label: 'Tamam',
          textColor: Colors.white,
          onPressed: () {},
        ),
      ),
    );
  }

  static String _getAuthErrorMessage(String error) {
    // Map English errors to Turkish user-friendly messages
    final errorMap = {
      'UserNotFound': 'Kullanıcı bulunamadı',
      'Invalid or expired OTP code': 'Geçersiz veya süresi dolmuş kod',
      'Phone number is already registered': 'Bu telefon numarası zaten kayıtlı',
      'OTP code has expired': 'Kod süresi doldu. Lütfen yeni kod isteyin',
      'Invalid credentials': 'E-posta veya şifre hatalı',
      'Connection timeout': 'Bağlantı zaman aşımına uğradı',
      'No internet connection': 'İnternet bağlantısı yok',
    };

    return errorMap[error] ?? error;
  }

  static String _getReferralErrorMessage(String error) {
    final errorMap = {
      'Referral code has expired': 'Referans kodu süresi dolmuş',
      'Referral code not found': 'Referans kodu bulunamadı',
      'Cannot refer yourself': 'Kendi referans kodunuzu kullanamazsınız',
      'Invalid referral code': 'Geçersiz referans kodu',
    };

    return errorMap[error] ?? error;
  }
}

// Usage
try {
  await authService.loginWithEmail(email, password);
} catch (e) {
  ErrorHandler.handle(context, e);
}
```

---

## Testing Guide

### Unit Tests

```dart
// File: test/services/auth_service_test.dart

import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:dio/dio.dart';
import 'package:ziraai/services/auth_service.dart';

class MockDio extends Mock implements Dio {}
class MockTokenManager extends Mock implements TokenManager {}

void main() {
  group('AuthService', () {
    late MockDio mockDio;
    late MockTokenManager mockTokenManager;
    late AuthService authService;

    setUp(() {
      mockDio = MockDio();
      mockTokenManager = MockTokenManager();
      authService = AuthService(
        dio: mockDio,
        tokenManager: mockTokenManager,
      );
    });

    test('loginWithEmail - success', () async {
      // Arrange
      final mockResponse = Response(
        requestOptions: RequestOptions(path: ''),
        data: {
          'success': true,
          'data': {
            'token': 'test_token',
            'refreshToken': 'test_refresh',
            'expiration': '2025-10-04T12:00:00Z',
            'claims': [],
          },
        },
        statusCode: 200,
      );

      when(mockDio.post(any, data: anyNamed('data')))
          .thenAnswer((_) async => mockResponse);

      // Act
      final result = await authService.loginWithEmail(
        'test@example.com',
        'password123',
      );

      // Assert
      expect(result.token, 'test_token');
      verify(mockTokenManager.saveToken(any)).called(1);
    });

    test('requestRegistrationOtp - success with referral', () async {
      // Arrange
      final mockResponse = Response(
        requestOptions: RequestOptions(path: ''),
        data: {
          'success': true,
          'message': 'OTP sent',
        },
        statusCode: 200,
      );

      when(mockDio.post(any, data: anyNamed('data')))
          .thenAnswer((_) async => mockResponse);

      // Act
      await authService.requestRegistrationOtp(
        phone: '05321234567',
        fullName: 'Test User',
        referralCode: 'ZIRA-ABC123',
      );

      // Assert
      verify(mockDio.post(
        '/api/v1/auth/register-phone',
        data: {
          'mobilePhone': '05321234567',
          'fullName': 'Test User',
          'referralCode': 'ZIRA-ABC123',
        },
      )).called(1);
    });

    test('verifyLoginOtp - invalid OTP throws exception', () async {
      // Arrange
      final mockResponse = Response(
        requestOptions: RequestOptions(path: ''),
        data: {
          'success': false,
          'message': 'Invalid or expired OTP code',
        },
        statusCode: 400,
      );

      when(mockDio.post(any, data: anyNamed('data')))
          .thenAnswer((_) async => mockResponse);

      // Act & Assert
      expect(
        () => authService.verifyLoginOtp('05321234567', '999999'),
        throwsA(isA<AuthException>()),
      );
    });
  });
}
```

### Integration Tests (E2E)

```dart
// File: integration_test/auth_flow_test.dart

import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:ziraai/main.dart' as app;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('Authentication E2E Tests', () {
    testWidgets('Complete phone registration flow with referral',
        (WidgetTester tester) async {
      // Start app
      app.main();
      await tester.pumpAndSettle();

      // Navigate to registration
      await tester.tap(find.text('Kayıt Ol'));
      await tester.pumpAndSettle();

      // Choose phone registration
      await tester.tap(find.text('Telefon ile Kayıt Ol'));
      await tester.pumpAndSettle();

      // Enter phone number
      await tester.enterText(
        find.byKey(Key('phone_input')),
        '05321234567',
      );

      // Enter full name
      await tester.enterText(
        find.byKey(Key('fullname_input')),
        'Test User',
      );

      // Enter referral code
      await tester.enterText(
        find.byKey(Key('referral_input')),
        'ZIRA-TEST01',
      );

      // Request OTP
      await tester.tap(find.text('Kod Gönder'));
      await tester.pumpAndSettle();

      // Verify OTP screen shown
      expect(find.text('Doğrulama Kodu'), findsOneWidget);

      // Enter OTP (in dev mode, it's always 123456)
      await tester.enterText(
        find.byKey(Key('otp_input')),
        '123456',
      );

      // Verify OTP
      await tester.tap(find.text('Doğrula'));
      await tester.pumpAndSettle(Duration(seconds: 3));

      // Verify navigated to home screen
      expect(find.text('Ana Sayfa'), findsOneWidget);
    });
  });
}
```

---

## Best Practices

### 1. Security

✅ **DO:**
- Store JWT tokens in secure storage (flutter_secure_storage)
- Always use HTTPS in production
- Implement certificate pinning for critical endpoints
- Clear tokens on logout
- Validate referral codes before submission

❌ **DON'T:**
- Store tokens in SharedPreferences (use secure storage)
- Log sensitive data (tokens, OTP codes)
- Hardcode API keys in code
- Trust client-side validation only

**Example:**
```dart
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureTokenManager {
  final storage = FlutterSecureStorage();

  Future<void> saveToken(String token) async {
    await storage.write(key: 'auth_token', value: token);
  }

  Future<String?> getToken() async {
    return await storage.read(key: 'auth_token');
  }

  Future<void> deleteToken() async {
    await storage.delete(key: 'auth_token');
  }
}
```

### 2. Error Handling

✅ **DO:**
- Show user-friendly error messages in Turkish
- Log technical errors for debugging
- Handle network timeouts gracefully
- Provide retry mechanisms for failed requests

❌ **DON'T:**
- Show raw API error messages to users
- Crash the app on network errors
- Ignore errors silently

### 3. State Management

✅ **DO:**
- Use Provider/Riverpod for global auth state
- Invalidate cache on logout
- Refresh stats after referral actions

**Example (Provider):**
```dart
class AuthProvider extends ChangeNotifier {
  AuthToken? _token;
  bool _isLoading = false;

  bool get isAuthenticated => _token != null;
  bool get isLoading => _isLoading;

  Future<void> login(String email, String password) async {
    _isLoading = true;
    notifyListeners();

    try {
      _token = await AuthService().loginWithEmail(email, password);
      notifyListeners();
    } catch (e) {
      _isLoading = false;
      notifyListeners();
      rethrow;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    await TokenManager().clearToken();
    _token = null;
    notifyListeners();
  }
}
```

### 4. Performance

✅ **DO:**
- Cache referral stats locally (10-second TTL)
- Use pagination for reward lists (future)
- Optimize image loading
- Debounce search/validation inputs

**Example (Cache):**
```dart
class CachedReferralService {
  final ReferralService _service;
  ReferralStats? _cachedStats;
  DateTime? _lastFetch;
  static const cacheDuration = Duration(seconds: 10);

  Future<ReferralStats> getReferralStats() async {
    if (_cachedStats != null &&
        _lastFetch != null &&
        DateTime.now().difference(_lastFetch!) < cacheDuration) {
      return _cachedStats!;
    }

    _cachedStats = await _service.getReferralStats();
    _lastFetch = DateTime.now();
    return _cachedStats!;
  }
}
```

### 5. User Experience

✅ **DO:**
- Show loading indicators during API calls
- Provide OTP auto-fill (SMS Code Autofill)
- Validate phone numbers before submission
- Show referral code as uppercase
- Pre-fill referral code from deep link

**Example (OTP Auto-fill):**
```dart
import 'package:sms_autofill/sms_autofill.dart';

class OtpScreen extends StatefulWidget {
  @override
  _OtpScreenState createState() => _OtpScreenState();
}

class _OtpScreenState extends State<OtpScreen> with CodeAutoFill {
  String? _code;

  @override
  void initState() {
    super.initState();
    listenForCode();  // Auto-fill OTP from SMS
  }

  @override
  void codeUpdated() {
    setState(() {
      _code = code;
    });
    // Auto-verify when code captured
    if (_code != null && _code!.length == 6) {
      _verifyOtp(_code!);
    }
  }

  @override
  Widget build(BuildContext context) {
    return PinFieldAutoFill(
      codeLength: 6,
      onCodeSubmitted: (code) => _verifyOtp(code),
      onCodeChanged: (code) {
        if (code!.length == 6) {
          _verifyOtp(code);
        }
      },
    );
  }

  @override
  void dispose() {
    cancel();  // Stop listening
    super.dispose();
  }
}
```

---

## Troubleshooting

### Common Issues

#### 1. Deep Link Not Working

**Problem**: App doesn't capture referral code from Play Store

**Solutions:**
- ✅ Verify AndroidManifest.xml intent filters
- ✅ Test with `adb shell am start` command:
  ```bash
  adb shell am start -a android.intent.action.VIEW \
    -d "ziraai://referral?code=ZIRA-ABC123" \
    com.ziraai
  ```
- ✅ Check Play Store install referrer:
  ```dart
  final referrer = await AndroidPlayInstallReferrer.installReferrer;
  print('Referrer: $referrer');
  ```
- ✅ Enable debug logging in DeepLinkService

---

#### 2. OTP Not Received (Development)

**Problem**: User doesn't receive OTP SMS

**Solutions:**
- ✅ In development, OTP is fixed: `123456`
- ✅ Check API response: `message` contains OTP in dev mode
- ✅ Verify SMS service configuration in backend
- ✅ Check console logs for OTP code
- ✅ In production, verify phone number format (Turkish: `05XX`)

---

#### 3. Token Expired Error (401)

**Problem**: Authenticated requests fail with 401

**Solutions:**
- ✅ Implement token refresh logic
- ✅ Check token expiration before requests
- ✅ Clear token and redirect to login on 401
- ✅ Use Dio interceptor to auto-refresh:
  ```dart
  dio.interceptors.add(
    QueuedInterceptorsWrapper(
      onError: (e, handler) async {
        if (e.response?.statusCode == 401) {
          // Refresh token
          final newToken = await refreshToken();
          // Retry request
          e.requestOptions.headers['Authorization'] = 'Bearer $newToken';
          final response = await dio.fetch(e.requestOptions);
          return handler.resolve(response);
        }
        handler.next(e);
      },
    ),
  );
  ```

---

#### 4. Referral Stats Return Empty

**Problem**: GET /api/v1/referral/stats returns empty data

**Root Causes:**
- User has no referrals yet (normal)
- Wrong user authenticated (check JWT token)
- Database data mismatch

**Debugging:**
1. Verify user ID in JWT token:
   ```dart
   final token = await TokenManager().getToken();
   final parts = token!.split('.');
   final payload = json.decode(
     utf8.decode(base64.decode(base64.normalize(parts[1])))
   );
   print('User ID: ${payload['nameid']}');
   ```

2. Check if user generated any referral codes:
   ```dart
   final codes = await ReferralService().getUserReferralCodes();
   print('Referral codes: ${codes.length}');
   ```

3. Verify backend logs for user ID

---

#### 5. App Crashes on Referral Code Input

**Problem**: App crashes when entering referral code

**Solutions:**
- ✅ Handle null values in referral code
- ✅ Validate code format (ZIRA-XXXXXX)
- ✅ Use try-catch around validation:
  ```dart
  try {
    final isValid = await ReferralService().validateReferralCode(code);
  } catch (e) {
    print('Validation error: $e');
    // Show error to user
  }
  ```

---

### Debug Mode Tools

```dart
class DebugTools {
  static void printAuthToken(String token) {
    final parts = token.split('.');
    final payload = json.decode(
      utf8.decode(base64.decode(base64.normalize(parts[1])))
    );

    print('=== JWT Token Debug ===');
    print('User ID: ${payload['nameid']}');
    print('Email: ${payload['email']}');
    print('Expiration: ${payload['exp']}');
    print('Roles: ${payload['role']}');
    print('Claims: ${payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/ClaimSet']}');
  }

  static void printApiRequest(RequestOptions options) {
    print('=== API Request ===');
    print('URL: ${options.baseUrl}${options.path}');
    print('Method: ${options.method}');
    print('Headers: ${options.headers}');
    print('Data: ${options.data}');
  }

  static void printApiResponse(Response response) {
    print('=== API Response ===');
    print('Status: ${response.statusCode}');
    print('Data: ${response.data}');
  }
}

// Usage
dio.interceptors.add(
  InterceptorsWrapper(
    onRequest: (options, handler) {
      DebugTools.printApiRequest(options);
      handler.next(options);
    },
    onResponse: (response, handler) {
      DebugTools.printApiResponse(response);
      handler.next(response);
    },
  ),
);
```

---

## Conclusion

This integration guide covers all aspects of integrating ZiraAI's phone authentication and referral system into your Flutter mobile app.

### Key Takeaways

1. **Phone Authentication**: 2-step OTP flow for quick onboarding
2. **Referral System**: Built-in viral growth with automatic rewards
3. **Deep Linking**: Seamless referral code capture from Play Store
4. **Real-time Data**: All stats and credits update immediately
5. **Credits Priority**: Referral credits used before subscription quota

### Next Steps

1. ✅ Implement authentication screens (login + registration)
2. ✅ Set up deep linking (AndroidManifest.xml + DeepLinkService)
3. ✅ Create referral dashboard (stats + generate links)
4. ✅ Test complete flow end-to-end
5. ✅ Deploy to staging for QA testing

### Support

For questions or issues:
- Check this guide's Troubleshooting section
- Review backend API documentation
- Test with Postman collection first
- Contact backend team with debug logs

**Happy coding! 🚀**

---

**Document Version**: 1.0
**Last Updated**: 2025-10-04
**Maintained By**: ZiraAI Backend Team
