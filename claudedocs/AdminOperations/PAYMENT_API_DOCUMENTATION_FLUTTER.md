# ZiraAI Payment API - Flutter Integration Documentation

**Version:** 1.0
**Last Updated:** 2025-11-21
**Payment Gateway:** iyzico (Pay With iyzico - PWI)
**Base URL:** `https://your-api-domain.com/api/v1/payments`

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Payment Flows](#payment-flows)
3. [Authentication](#authentication)
4. [API Endpoints](#api-endpoints)
5. [Flutter Integration Guide](#flutter-integration-guide)
6. [Code Examples](#code-examples)
7. [Error Handling](#error-handling)
8. [Deep Link Configuration](#deep-link-configuration)
9. [Testing](#testing)
10. [Security Considerations](#security-considerations)

---

## 🎯 Overview

ZiraAI Payment API entegrasyonu, iki farklı ödeme akışını destekler:

### Payment Flow Types

| Flow Type | Description | User Type | Purpose |
|-----------|-------------|-----------|---------|
| `SponsorBulkPurchase` | Toplu kod satın alma | Sponsor | Sponsor, çiftçilere dağıtmak üzere toplu abonelik kodu satın alır |
| `FarmerSubscription` | Bireysel abonelik | Farmer | Çiftçi, kendi kullanımı için aylık abonelik satın alır |

### Payment Process Flow

```
┌─────────────┐
│ Mobile App  │
└──────┬──────┘
       │
       │ 1. Initialize Payment (POST)
       │    FlowType + FlowData
       ▼
┌─────────────┐
│  ZiraAI API │
└──────┬──────┘
       │
       │ 2. Create Transaction
       │    Generate Token
       ▼
┌─────────────┐
│   iyzico    │
└──────┬──────┘
       │
       │ 3. Return Payment URL
       │
       ▼
┌─────────────┐
│ Mobile App  │ Opens WebView
│  (WebView)  │ User enters card info
└──────┬──────┘
       │
       │ 4. User Completes Payment
       │
       ▼
┌─────────────┐
│   iyzico    │ Payment Success/Failure
└──────┬──────┘
       │
       │ 5a. Redirect to Deep Link
       │     ziraai://payment-callback?token=xxx
       │
       ▼
┌─────────────┐
│ Mobile App  │
└──────┬──────┘
       │
       │ 6. Verify Payment (POST)
       │    Send token
       │
       ▼
┌─────────────┐
│  ZiraAI API │
└──────┬──────┘
       │
       │ 7. Verify with iyzico
       │    Process business logic
       │    (Create codes/subscription)
       │
       ▼
┌─────────────┐
│ Mobile App  │ Show Success/Failure
└─────────────┘

       │ 5b. Webhook (Parallel)
       │     iyzico → ZiraAI API
       │     (Background update)
       ▼
```

### Key Concepts

- **Payment Token**: iyzico tarafından üretilen benzersiz ödeme tanımlayıcısı
- **Transaction ID**: ZiraAI veritabanındaki ödeme kaydı ID'si
- **Conversation ID**: iyzico ile tracking için kullanılan ID
- **Deep Link**: Ödeme sonrası uygulamaya dönüş için kullanılan özel URL scheme
- **Webhook**: iyzico'nun arka planda gönderdiği ödeme durumu bildirimleri

---

## 💳 Payment Flows

### 1. Sponsor Bulk Purchase Flow

**Use Case:** Sponsor, çiftçilere dağıtmak üzere toplu abonelik kodu satın alır.

**Flow Sequence:**

1. Sponsor, tier seçer (S, M, L, XL)
2. Miktar belirler (10-10000 arası)
3. `Initialize Payment` çağrısı yapılır
4. iyzico payment page açılır
5. Ödeme tamamlandıktan sonra:
   - `SponsorshipPurchase` kaydı oluşturulur
   - Belirtilen miktarda kod otomatik üretilir
   - Kodlar 30 gün geçerlidir
   - Sponsor kodları çiftçilere dağıtabilir

**FlowData Structure:**
```json
{
  "SubscriptionTierId": 1,
  "Quantity": 100
}
```

**Business Rules:**
- Minimum Quantity: 10 (tier'a göre değişebilir)
- Maximum Quantity: 10000 (tier'a göre değişebilir)
- Price Calculation: `Quantity × Tier.MonthlyPrice`
- Code Validity: 30 days from purchase
- Code Prefix: `AGRI-`

### 2. Farmer Subscription Flow

**Use Case:** Çiftçi, kendi kullanımı için aylık abonelik satın alır.

**Flow Sequence:**

1. Farmer, tier seçer (S, M, L, XL)
2. Süre belirler (1-12 ay arası, default: 1)
3. `Initialize Payment` çağrısı yapılır
4. iyzico payment page açılır
5. Ödeme tamamlandıktan sonra:
   - Mevcut abonelik varsa süre uzatılır
   - Yoksa yeni abonelik oluşturulur
   - Auto-renew devre dışı bırakılır (manuel ödeme)
   - Günlük/aylık kotalar sıfırlanır

**FlowData Structure:**
```json
{
  "SubscriptionTierId": 2,
  "DurationMonths": 1
}
```

**Business Rules:**
- Default Duration: 1 month
- Maximum Duration: 12 months
- Price Calculation: `DurationMonths × Tier.MonthlyPrice`
- Auto-Renew: Disabled for manual purchases
- Subscription Extension: If existing, adds months to EndDate

---

## 🔐 Authentication

Tüm payment endpoint'leri (webhook hariç) JWT authentication gerektirir.

### Required Headers

```http
Authorization: Bearer {access_token}
Content-Type: application/json
x-dev-arch-version: 1.0
```

### Getting Access Token

```dart
// Login endpoint'inden token alınır
final response = await http.post(
  Uri.parse('$baseUrl/api/v1/auth/login'),
  body: jsonEncode({
    'email': email,
    'password': password,
  }),
);

final data = jsonDecode(response.body);
final accessToken = data['data']['accessToken']['token'];
```

### Claims Required

Payment endpoint'leri için şu claim'ler gereklidir:

| Endpoint | Claim | Alias |
|----------|-------|-------|
| Initialize Payment | InitializePayment | payment.initialize |
| Verify Payment | VerifyPayment | payment.verify |
| Get Payment Status | GetPaymentStatus | payment.status |

**Not:** Bu claim'ler Farmer (GroupId=2) ve Sponsor (GroupId=3) rollerine otomatik atanır.

---

## 🔌 API Endpoints

### 1. Initialize Payment

Creates a new payment transaction and returns iyzico payment page URL.

#### Endpoint
```http
POST /api/v1/payments/initialize
```

#### Request Headers
```http
Authorization: Bearer {access_token}
Content-Type: application/json
x-dev-arch-version: 1.0
```

#### Request Body - Sponsor Bulk Purchase
```json
{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,
    "quantity": 100
  },
  "currency": "TRY"
}
```

#### Request Body - Farmer Subscription
```json
{
  "flowType": "FarmerSubscription",
  "flowData": {
    "subscriptionTierId": 2,
    "durationMonths": 1
  },
  "currency": "TRY"
}
```

#### Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `flowType` | string | Yes | Payment flow type: `SponsorBulkPurchase` or `FarmerSubscription` |
| `flowData` | object | Yes | Flow-specific data (see flow data structures) |
| `flowData.subscriptionTierId` | integer | Yes | Subscription tier ID (1-4) |
| `flowData.quantity` | integer | Conditional | Required for SponsorBulkPurchase (10-10000) |
| `flowData.durationMonths` | integer | Conditional | Required for FarmerSubscription (1-12) |
| `currency` | string | No | Currency code (default: TRY) |

#### Success Response (200 OK)
```json
{
  "data": {
    "transactionId": 12345,
    "paymentToken": "f3d8a7b2-4c5e-6f9a-1b2c-3d4e5f6a7b8c",
    "paymentPageUrl": "https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/auth/easypos/detail/f3d8a7b2-4c5e-6f9a-1b2c-3d4e5f6a7b8c",
    "callbackUrl": "ziraai://payment-callback?token=f3d8a7b2-4c5e-6f9a-1b2c-3d4e5f6a7b8c",
    "amount": 5000.00,
    "currency": "TRY",
    "expiresAt": "2025-11-21T15:30:00Z",
    "status": "Pending",
    "conversationId": "CONV-12345-1732197000"
  },
  "success": true,
  "message": "Payment initialized successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `transactionId` | integer | ZiraAI database transaction ID |
| `paymentToken` | string | iyzico payment token (UUID) |
| `paymentPageUrl` | string | iyzico payment page URL (open in WebView) |
| `callbackUrl` | string | Deep link URL for return to app |
| `amount` | decimal | Payment amount |
| `currency` | string | Currency code (TRY) |
| `expiresAt` | string | Payment token expiration (ISO 8601) |
| `status` | string | Payment status (should be "Pending") |
| `conversationId` | string | Tracking conversation ID |

#### Error Responses

**400 Bad Request** - Invalid request data
```json
{
  "success": false,
  "message": "Invalid subscription tier ID"
}
```

**401 Unauthorized** - Missing or invalid token
```json
{
  "success": false,
  "message": "User not authenticated"
}
```

**403 Forbidden** - Missing required claim
```json
{
  "success": false,
  "message": "You are not authorized to access this resource"
}
```

**500 Internal Server Error** - Server error
```json
{
  "success": false,
  "message": "An error occurred while initializing payment"
}
```

---

### 2. Verify Payment

Verifies payment status after user completes payment on iyzico page.

#### Endpoint
```http
POST /api/v1/payments/verify
```

#### Request Headers
```http
Authorization: Bearer {access_token}
Content-Type: application/json
x-dev-arch-version: 1.0
```

#### Request Body
```json
{
  "paymentToken": "f3d8a7b2-4c5e-6f9a-1b2c-3d4e5f6a7b8c"
}
```

#### Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `paymentToken` | string | Yes | Payment token from initialize response or deep link |

#### Success Response (200 OK) - Sponsor Bulk Purchase
```json
{
  "data": {
    "transactionId": 12345,
    "status": "Success",
    "paymentId": "12345678",
    "paymentToken": "f3d8a7b2-4c5e-6f9a-1b2c-3d4e5f6a7b8c",
    "amount": 5000.00,
    "currency": "TRY",
    "paidAmount": 5000.00,
    "completedAt": "2025-11-21T14:25:30Z",
    "errorMessage": null,
    "flowType": "SponsorBulkPurchase",
    "flowResult": {
      "purchaseId": 456,
      "codesGenerated": 100,
      "subscriptionTierName": "Small (S)"
    }
  },
  "success": true,
  "message": "Payment verified successfully"
}
```

#### Success Response (200 OK) - Farmer Subscription
```json
{
  "data": {
    "transactionId": 12346,
    "status": "Success",
    "paymentId": "12345679",
    "paymentToken": "a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
    "amount": 50.00,
    "currency": "TRY",
    "paidAmount": 50.00,
    "completedAt": "2025-11-21T14:30:45Z",
    "errorMessage": null,
    "flowType": "FarmerSubscription",
    "flowResult": {
      "subscriptionId": 789,
      "startDate": "2025-11-21T14:30:45Z",
      "endDate": "2025-12-21T14:30:45Z",
      "subscriptionTierName": "Medium (M)"
    }
  },
  "success": true,
  "message": "Payment verified successfully"
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `transactionId` | integer | ZiraAI database transaction ID |
| `status` | string | Payment status: `Success`, `Failed`, `Pending`, `Cancelled`, `Expired` |
| `paymentId` | string | iyzico payment ID |
| `paymentToken` | string | Payment token |
| `amount` | decimal | Original payment amount |
| `currency` | string | Currency code |
| `paidAmount` | decimal | Actual amount paid |
| `completedAt` | string | Payment completion timestamp (ISO 8601) |
| `errorMessage` | string | Error message if failed (null if success) |
| `flowType` | string | Flow type for client routing |
| `flowResult` | object | Flow-specific result data |

#### FlowResult - SponsorBulkPurchase

| Field | Type | Description |
|-------|------|-------------|
| `purchaseId` | integer | Sponsorship purchase record ID |
| `codesGenerated` | integer | Number of codes generated |
| `subscriptionTierName` | string | Tier name (e.g., "Small (S)") |

#### FlowResult - FarmerSubscription

| Field | Type | Description |
|-------|------|-------------|
| `subscriptionId` | integer | User subscription record ID |
| `startDate` | string | Subscription start date (ISO 8601) |
| `endDate` | string | Subscription end date (ISO 8601) |
| `subscriptionTierName` | string | Tier name (e.g., "Medium (M)") |

#### Error Responses

**400 Bad Request** - Payment failed
```json
{
  "success": false,
  "message": "Payment failed: Insufficient funds"
}
```

**404 Not Found** - Transaction not found
```json
{
  "success": false,
  "message": "Payment transaction not found"
}
```

**500 Internal Server Error**
```json
{
  "success": false,
  "message": "An error occurred while verifying payment"
}
```

---

### 3. Get Payment Status

Queries current status of a payment transaction by token.

#### Endpoint
```http
GET /api/v1/payments/status/{token}
```

#### Request Headers
```http
Authorization: Bearer {access_token}
x-dev-arch-version: 1.0
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `token` | string | Yes | Payment token |

#### Example Request
```http
GET /api/v1/payments/status/f3d8a7b2-4c5e-6f9a-1b2c-3d4e5f6a7b8c
Authorization: Bearer {access_token}
```

#### Success Response (200 OK)
```json
{
  "data": {
    "transactionId": 12345,
    "status": "Success",
    "paymentId": "12345678",
    "paymentToken": "f3d8a7b2-4c5e-6f9a-1b2c-3d4e5f6a7b8c",
    "amount": 5000.00,
    "currency": "TRY",
    "paidAmount": 5000.00,
    "completedAt": "2025-11-21T14:25:30Z",
    "errorMessage": null,
    "flowType": "SponsorBulkPurchase",
    "flowResult": {
      "purchaseId": 456,
      "codesGenerated": 100,
      "subscriptionTierName": "Small (S)"
    }
  },
  "success": true,
  "message": "Payment status retrieved successfully"
}
```

**Note:** Response structure is identical to Verify Payment response.

#### Error Responses

**404 Not Found** - Transaction not found
```json
{
  "success": false,
  "message": "Payment transaction not found"
}
```

---

### 4. Payment Webhook (Backend Only)

**⚠️ This endpoint is NOT called by mobile app.** iyzico calls this endpoint automatically.

#### Endpoint
```http
POST /api/payments/webhook
```

**Note:** This endpoint is public (no authentication) and uses `[AllowAnonymous]` attribute.

iyzico sends webhook notifications when payment status changes. ZiraAI backend processes these automatically.

---

## 📱 Flutter Integration Guide

### Step 1: Add Dependencies

```yaml
# pubspec.yaml
dependencies:
  http: ^1.1.0
  webview_flutter: ^4.4.2
  url_launcher: ^6.2.1
  shared_preferences: ^2.2.2
```

### Step 2: Create Payment Service

```dart
// lib/services/payment_service.dart

import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class PaymentService {
  final String baseUrl;

  PaymentService({required this.baseUrl});

  /// Get stored access token
  Future<String?> _getAccessToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('access_token');
  }

  /// Get common headers with authorization
  Future<Map<String, String>> _getHeaders() async {
    final token = await _getAccessToken();
    return {
      'Content-Type': 'application/json',
      'x-dev-arch-version': '1.0',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  /// Initialize payment for sponsor bulk purchase
  Future<PaymentInitializeResponse> initializeSponsorPayment({
    required int subscriptionTierId,
    required int quantity,
    String currency = 'TRY',
  }) async {
    final url = Uri.parse('$baseUrl/api/v1/payments/initialize');
    final headers = await _getHeaders();

    final body = jsonEncode({
      'flowType': 'SponsorBulkPurchase',
      'flowData': {
        'subscriptionTierId': subscriptionTierId,
        'quantity': quantity,
      },
      'currency': currency,
    });

    final response = await http.post(url, headers: headers, body: body);

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return PaymentInitializeResponse.fromJson(data['data']);
    } else {
      final error = jsonDecode(response.body);
      throw PaymentException(error['message'] ?? 'Payment initialization failed');
    }
  }

  /// Initialize payment for farmer subscription
  Future<PaymentInitializeResponse> initializeFarmerPayment({
    required int subscriptionTierId,
    int durationMonths = 1,
    String currency = 'TRY',
  }) async {
    final url = Uri.parse('$baseUrl/api/v1/payments/initialize');
    final headers = await _getHeaders();

    final body = jsonEncode({
      'flowType': 'FarmerSubscription',
      'flowData': {
        'subscriptionTierId': subscriptionTierId,
        'durationMonths': durationMonths,
      },
      'currency': currency,
    });

    final response = await http.post(url, headers: headers, body: body);

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return PaymentInitializeResponse.fromJson(data['data']);
    } else {
      final error = jsonDecode(response.body);
      throw PaymentException(error['message'] ?? 'Payment initialization failed');
    }
  }

  /// Verify payment after completion
  Future<PaymentVerifyResponse> verifyPayment(String paymentToken) async {
    final url = Uri.parse('$baseUrl/api/v1/payments/verify');
    final headers = await _getHeaders();

    final body = jsonEncode({
      'paymentToken': paymentToken,
    });

    final response = await http.post(url, headers: headers, body: body);

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return PaymentVerifyResponse.fromJson(data['data']);
    } else {
      final error = jsonDecode(response.body);
      throw PaymentException(error['message'] ?? 'Payment verification failed');
    }
  }

  /// Get payment status by token
  Future<PaymentVerifyResponse> getPaymentStatus(String paymentToken) async {
    final url = Uri.parse('$baseUrl/api/v1/payments/status/$paymentToken');
    final headers = await _getHeaders();

    final response = await http.get(url, headers: headers);

    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      return PaymentVerifyResponse.fromJson(data['data']);
    } else {
      final error = jsonDecode(response.body);
      throw PaymentException(error['message'] ?? 'Failed to get payment status');
    }
  }
}

class PaymentException implements Exception {
  final String message;
  PaymentException(this.message);

  @override
  String toString() => message;
}
```

### Step 3: Create Model Classes

```dart
// lib/models/payment_models.dart

class PaymentInitializeResponse {
  final int transactionId;
  final String paymentToken;
  final String paymentPageUrl;
  final String callbackUrl;
  final double amount;
  final String currency;
  final String expiresAt;
  final String status;
  final String conversationId;

  PaymentInitializeResponse({
    required this.transactionId,
    required this.paymentToken,
    required this.paymentPageUrl,
    required this.callbackUrl,
    required this.amount,
    required this.currency,
    required this.expiresAt,
    required this.status,
    required this.conversationId,
  });

  factory PaymentInitializeResponse.fromJson(Map<String, dynamic> json) {
    return PaymentInitializeResponse(
      transactionId: json['transactionId'],
      paymentToken: json['paymentToken'],
      paymentPageUrl: json['paymentPageUrl'],
      callbackUrl: json['callbackUrl'],
      amount: (json['amount'] as num).toDouble(),
      currency: json['currency'],
      expiresAt: json['expiresAt'],
      status: json['status'],
      conversationId: json['conversationId'],
    );
  }
}

class PaymentVerifyResponse {
  final int transactionId;
  final String status;
  final String paymentId;
  final String paymentToken;
  final double amount;
  final String currency;
  final double paidAmount;
  final String completedAt;
  final String? errorMessage;
  final String flowType;
  final dynamic flowResult;

  PaymentVerifyResponse({
    required this.transactionId,
    required this.status,
    required this.paymentId,
    required this.paymentToken,
    required this.amount,
    required this.currency,
    required this.paidAmount,
    required this.completedAt,
    this.errorMessage,
    required this.flowType,
    this.flowResult,
  });

  factory PaymentVerifyResponse.fromJson(Map<String, dynamic> json) {
    return PaymentVerifyResponse(
      transactionId: json['transactionId'],
      status: json['status'],
      paymentId: json['paymentId'],
      paymentToken: json['paymentToken'],
      amount: (json['amount'] as num).toDouble(),
      currency: json['currency'],
      paidAmount: (json['paidAmount'] as num).toDouble(),
      completedAt: json['completedAt'],
      errorMessage: json['errorMessage'],
      flowType: json['flowType'],
      flowResult: json['flowResult'],
    );
  }

  // Helper methods
  bool get isSuccess => status == 'Success';
  bool get isFailed => status == 'Failed';
  bool get isPending => status == 'Pending';

  SponsorBulkPurchaseResult? get sponsorResult {
    if (flowType == 'SponsorBulkPurchase' && flowResult != null) {
      return SponsorBulkPurchaseResult.fromJson(flowResult);
    }
    return null;
  }

  FarmerSubscriptionResult? get farmerResult {
    if (flowType == 'FarmerSubscription' && flowResult != null) {
      return FarmerSubscriptionResult.fromJson(flowResult);
    }
    return null;
  }
}

class SponsorBulkPurchaseResult {
  final int purchaseId;
  final int codesGenerated;
  final String subscriptionTierName;

  SponsorBulkPurchaseResult({
    required this.purchaseId,
    required this.codesGenerated,
    required this.subscriptionTierName,
  });

  factory SponsorBulkPurchaseResult.fromJson(Map<String, dynamic> json) {
    return SponsorBulkPurchaseResult(
      purchaseId: json['purchaseId'],
      codesGenerated: json['codesGenerated'],
      subscriptionTierName: json['subscriptionTierName'],
    );
  }
}

class FarmerSubscriptionResult {
  final int subscriptionId;
  final String startDate;
  final String endDate;
  final String subscriptionTierName;

  FarmerSubscriptionResult({
    required this.subscriptionId,
    required this.startDate,
    required this.endDate,
    required this.subscriptionTierName,
  });

  factory FarmerSubscriptionResult.fromJson(Map<String, dynamic> json) {
    return FarmerSubscriptionResult(
      subscriptionId: json['subscriptionId'],
      startDate: json['startDate'],
      endDate: json['endDate'],
      subscriptionTierName: json['subscriptionTierName'],
    );
  }
}
```

### Step 4: Create Payment WebView Screen

```dart
// lib/screens/payment_webview_screen.dart

import 'package:flutter/material.dart';
import 'package:webview_flutter/webview_flutter.dart';

class PaymentWebViewScreen extends StatefulWidget {
  final String paymentPageUrl;
  final String paymentToken;
  final Function(String token) onPaymentCompleted;
  final VoidCallback onPaymentCancelled;

  const PaymentWebViewScreen({
    Key? key,
    required this.paymentPageUrl,
    required this.paymentToken,
    required this.onPaymentCompleted,
    required this.onPaymentCancelled,
  }) : super(key: key);

  @override
  State<PaymentWebViewScreen> createState() => _PaymentWebViewScreenState();
}

class _PaymentWebViewScreenState extends State<PaymentWebViewScreen> {
  late final WebViewController _controller;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _initWebView();
  }

  void _initWebView() {
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setNavigationDelegate(
        NavigationDelegate(
          onPageStarted: (url) {
            debugPrint('[Payment] Page started loading: $url');

            // Check if deep link callback
            if (url.startsWith('ziraai://payment-callback')) {
              final uri = Uri.parse(url);
              final token = uri.queryParameters['token'];

              if (token != null) {
                debugPrint('[Payment] Payment completed with token: $token');
                widget.onPaymentCompleted(token);
              }
            }
          },
          onPageFinished: (url) {
            setState(() {
              _isLoading = false;
            });
            debugPrint('[Payment] Page finished loading: $url');
          },
          onWebResourceError: (error) {
            debugPrint('[Payment] WebView error: ${error.description}');
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text('Sayfa yüklenirken hata oluştu: ${error.description}'),
                backgroundColor: Colors.red,
              ),
            );
          },
        ),
      )
      ..loadRequest(Uri.parse(widget.paymentPageUrl));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Ödeme'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () {
            _showCancelDialog();
          },
        ),
      ),
      body: Stack(
        children: [
          WebViewWidget(controller: _controller),
          if (_isLoading)
            const Center(
              child: CircularProgressIndicator(),
            ),
        ],
      ),
    );
  }

  void _showCancelDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Ödemeyi İptal Et'),
        content: const Text('Ödeme işlemini iptal etmek istediğinizden emin misiniz?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Hayır'),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(context); // Close dialog
              widget.onPaymentCancelled();
            },
            child: const Text('Evet, İptal Et'),
          ),
        ],
      ),
    );
  }
}
```

### Step 5: Complete Payment Flow Implementation

```dart
// lib/screens/sponsor_payment_screen.dart

import 'package:flutter/material.dart';
import '../services/payment_service.dart';
import '../models/payment_models.dart';
import 'payment_webview_screen.dart';

class SponsorPaymentScreen extends StatefulWidget {
  final int subscriptionTierId;
  final int quantity;

  const SponsorPaymentScreen({
    Key? key,
    required this.subscriptionTierId,
    required this.quantity,
  }) : super(key: key);

  @override
  State<SponsorPaymentScreen> createState() => _SponsorPaymentScreenState();
}

class _SponsorPaymentScreenState extends State<SponsorPaymentScreen> {
  final PaymentService _paymentService = PaymentService(
    baseUrl: 'https://your-api-domain.com',
  );

  bool _isProcessing = false;
  String? _currentPaymentToken;

  /// Step 1: Initialize payment
  Future<void> _initializePayment() async {
    setState(() {
      _isProcessing = true;
    });

    try {
      // Call initialize endpoint
      final response = await _paymentService.initializeSponsorPayment(
        subscriptionTierId: widget.subscriptionTierId,
        quantity: widget.quantity,
      );

      debugPrint('[Payment] Initialized: Token=${response.paymentToken}');

      setState(() {
        _currentPaymentToken = response.paymentToken;
        _isProcessing = false;
      });

      // Step 2: Open payment page in WebView
      _openPaymentWebView(response);

    } on PaymentException catch (e) {
      setState(() {
        _isProcessing = false;
      });

      _showErrorDialog('Ödeme Başlatılamadı', e.message);
    } catch (e) {
      setState(() {
        _isProcessing = false;
      });

      _showErrorDialog('Hata', 'Beklenmeyen bir hata oluştu: $e');
    }
  }

  /// Step 2: Open payment page in WebView
  void _openPaymentWebView(PaymentInitializeResponse response) {
    Navigator.push(
      context,
      MaterialPageRoute(
        fullscreenDialog: true,
        builder: (context) => PaymentWebViewScreen(
          paymentPageUrl: response.paymentPageUrl,
          paymentToken: response.paymentToken,
          onPaymentCompleted: _handlePaymentCompleted,
          onPaymentCancelled: _handlePaymentCancelled,
        ),
      ),
    );
  }

  /// Step 3: Handle payment completion from deep link
  Future<void> _handlePaymentCompleted(String token) async {
    // Close WebView
    Navigator.pop(context);

    setState(() {
      _isProcessing = true;
    });

    try {
      // Step 4: Verify payment with backend
      final verifyResponse = await _paymentService.verifyPayment(token);

      setState(() {
        _isProcessing = false;
      });

      if (verifyResponse.isSuccess) {
        // Payment successful
        _showSuccessDialog(verifyResponse);
      } else if (verifyResponse.isFailed) {
        // Payment failed
        _showFailureDialog(verifyResponse);
      } else {
        // Payment pending
        _showPendingDialog(verifyResponse);
      }

    } on PaymentException catch (e) {
      setState(() {
        _isProcessing = false;
      });

      _showErrorDialog('Doğrulama Hatası', e.message);
    }
  }

  void _handlePaymentCancelled() {
    Navigator.pop(context); // Close WebView
    Navigator.pop(context); // Go back to previous screen
  }

  void _showSuccessDialog(PaymentVerifyResponse response) {
    final result = response.sponsorResult!;

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        title: const Text('✅ Ödeme Başarılı'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Ödeme Tutarı: ${response.paidAmount} ${response.currency}'),
            const SizedBox(height: 8),
            Text('Abonelik Tipi: ${result.subscriptionTierName}'),
            Text('Üretilen Kod Sayısı: ${result.codesGenerated}'),
            const SizedBox(height: 8),
            const Text(
              'Kodlarınız başarıyla oluşturuldu. Sponsorluk kodları sayfasından kodlarınızı görüntüleyebilirsiniz.',
              style: TextStyle(fontSize: 12, color: Colors.grey),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context); // Close dialog
              Navigator.pop(context); // Go back
              // Navigate to sponsorship codes page
            },
            child: const Text('Kodlarımı Görüntüle'),
          ),
        ],
      ),
    );
  }

  void _showFailureDialog(PaymentVerifyResponse response) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('❌ Ödeme Başarısız'),
        content: Text(
          response.errorMessage ?? 'Ödeme işleminiz başarısız oldu.',
        ),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context); // Close dialog
            },
            child: const Text('Tekrar Dene'),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(context); // Close dialog
              Navigator.pop(context); // Go back
            },
            child: const Text('İptal'),
          ),
        ],
      ),
    );
  }

  void _showPendingDialog(PaymentVerifyResponse response) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('⏳ Ödeme Bekleniyor'),
        content: const Text(
          'Ödemeniz henüz tamamlanmadı. Lütfen bir süre sonra tekrar kontrol edin.',
        ),
        actions: [
          TextButton(
            onPressed: () async {
              Navigator.pop(context);
              // Retry verification
              await _retryVerification();
            },
            child: const Text('Tekrar Kontrol Et'),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              Navigator.pop(context);
            },
            child: const Text('Kapat'),
          ),
        ],
      ),
    );
  }

  Future<void> _retryVerification() async {
    if (_currentPaymentToken == null) return;

    setState(() {
      _isProcessing = true;
    });

    try {
      final verifyResponse = await _paymentService.getPaymentStatus(
        _currentPaymentToken!,
      );

      setState(() {
        _isProcessing = false;
      });

      if (verifyResponse.isSuccess) {
        _showSuccessDialog(verifyResponse);
      } else if (verifyResponse.isFailed) {
        _showFailureDialog(verifyResponse);
      } else {
        _showPendingDialog(verifyResponse);
      }
    } catch (e) {
      setState(() {
        _isProcessing = false;
      });
      _showErrorDialog('Hata', 'Durum sorgulanamadı: $e');
    }
  }

  void _showErrorDialog(String title, String message) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Tamam'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Sponsor Ödeme'),
      ),
      body: _isProcessing
          ? const Center(child: CircularProgressIndicator())
          : Center(
              child: ElevatedButton(
                onPressed: _initializePayment,
                child: const Text('Ödemeyi Başlat'),
              ),
            ),
    );
  }
}
```

---

## 🔗 Deep Link Configuration

### Android Configuration

```xml
<!-- android/app/src/main/AndroidManifest.xml -->

<manifest>
  <application>
    <activity
      android:name=".MainActivity"
      android:launchMode="singleTop">

      <!-- Deep Link Intent Filter -->
      <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />

        <!-- Payment callback deep link -->
        <data
          android:scheme="ziraai"
          android:host="payment-callback" />
      </intent-filter>

    </activity>
  </application>
</manifest>
```

### iOS Configuration

```xml
<!-- ios/Runner/Info.plist -->

<dict>
  <!-- ... other configurations ... -->

  <!-- URL Types for Deep Links -->
  <key>CFBundleURLTypes</key>
  <array>
    <dict>
      <key>CFBundleTypeRole</key>
      <string>Editor</string>
      <key>CFBundleURLName</key>
      <string>com.ziraai.app</string>
      <key>CFBundleURLSchemes</key>
      <array>
        <string>ziraai</string>
      </array>
    </dict>
  </array>
</dict>
```

### Deep Link Handling in Flutter

```dart
// lib/main.dart

import 'package:uni_links/uni_links.dart';
import 'dart:async';

class MyApp extends StatefulWidget {
  @override
  State<MyApp> createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  StreamSubscription? _sub;

  @override
  void initState() {
    super.initState();
    _initDeepLinkListener();
  }

  void _initDeepLinkListener() async {
    // Handle deep link when app is already running
    _sub = linkStream.listen((String? link) {
      if (link != null) {
        _handleDeepLink(link);
      }
    }, onError: (err) {
      debugPrint('[DeepLink] Error: $err');
    });

    // Handle deep link when app is opened from terminated state
    try {
      final initialLink = await getInitialLink();
      if (initialLink != null) {
        _handleDeepLink(initialLink);
      }
    } catch (e) {
      debugPrint('[DeepLink] Failed to get initial link: $e');
    }
  }

  void _handleDeepLink(String link) {
    debugPrint('[DeepLink] Received: $link');

    final uri = Uri.parse(link);

    // Payment callback: ziraai://payment-callback?token=xxx
    if (uri.host == 'payment-callback') {
      final token = uri.queryParameters['token'];
      if (token != null) {
        // Navigate to payment verification
        // This will be handled by PaymentWebViewScreen
        debugPrint('[DeepLink] Payment token: $token');
      }
    }
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      // Your app configuration
    );
  }
}
```

---

## ⚠️ Error Handling

### Common Error Scenarios

#### 1. Invalid Subscription Tier

```json
{
  "success": false,
  "message": "Subscription tier not found"
}
```

**Handling:**
```dart
try {
  await _paymentService.initializeSponsorPayment(...);
} on PaymentException catch (e) {
  if (e.message.contains('tier not found')) {
    // Refresh tier list
    await _loadSubscriptionTiers();
    _showErrorDialog('Geçersiz Abonelik Tipi', 'Lütfen geçerli bir abonelik tipi seçin.');
  }
}
```

#### 2. Insufficient Balance (Payment Failed)

```json
{
  "data": {
    "status": "Failed",
    "errorMessage": "Insufficient funds"
  }
}
```

**Handling:**
```dart
if (verifyResponse.isFailed) {
  final errorMsg = verifyResponse.errorMessage ?? 'Ödeme başarısız';
  _showErrorDialog('Ödeme Başarısız', errorMsg);
}
```

#### 3. Token Expired

```json
{
  "success": false,
  "message": "Payment token has expired"
}
```

**Handling:**
```dart
try {
  await _paymentService.verifyPayment(token);
} on PaymentException catch (e) {
  if (e.message.contains('expired')) {
    _showErrorDialog(
      'Token Süresi Doldu',
      'Ödeme token\'ının süresi dolmuş. Lütfen ödemeyi yeniden başlatın.',
    );
    // Restart payment flow
  }
}
```

#### 4. Network Errors

```dart
Future<T> _withRetry<T>(Future<T> Function() operation, {int maxRetries = 3}) async {
  int attempt = 0;

  while (attempt < maxRetries) {
    try {
      return await operation();
    } on SocketException {
      attempt++;
      if (attempt >= maxRetries) {
        throw PaymentException('İnternet bağlantısı yok. Lütfen bağlantınızı kontrol edin.');
      }
      await Future.delayed(Duration(seconds: 2 * attempt));
    } on TimeoutException {
      attempt++;
      if (attempt >= maxRetries) {
        throw PaymentException('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      await Future.delayed(Duration(seconds: 2 * attempt));
    }
  }

  throw PaymentException('Beklenmeyen hata');
}

// Usage
final response = await _withRetry(() => _paymentService.initializeSponsorPayment(...));
```

#### 5. User Not Authenticated (401)

```dart
if (response.statusCode == 401) {
  // Token expired, redirect to login
  await _clearSession();
  Navigator.pushReplacementNamed(context, '/login');
  return;
}
```

#### 6. Insufficient Permissions (403)

```json
{
  "success": false,
  "message": "You are not authorized to access this resource"
}
```

**Handling:**
```dart
if (response.statusCode == 403) {
  _showErrorDialog(
    'Yetkiniz Yok',
    'Bu işlem için gerekli yetkiye sahip değilsiniz. Lütfen hesap türünüzü kontrol edin.',
  );
  return;
}
```

### Error Response Structure

All error responses follow this structure:

```json
{
  "success": false,
  "message": "Error description"
}
```

### HTTP Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Process response data |
| 400 | Bad Request | Show error message to user |
| 401 | Unauthorized | Redirect to login |
| 403 | Forbidden | Show permission error |
| 404 | Not Found | Show not found error |
| 500 | Server Error | Show generic error, retry |

---

## 🧪 Testing

### Test Environment

**Sandbox API Base URL:**
```
https://sandbox-api.iyzipay.com
```

**Test Credit Cards:**

| Card Number | CVV | Expiry | 3D Secure | Result |
|-------------|-----|--------|-----------|--------|
| 5528790000000008 | 123 | 12/2030 | Yes | Success |
| 5406670000000009 | 123 | 12/2030 | Yes | Failure |

### Test Scenarios

#### 1. Successful Sponsor Payment

```dart
// Test: Sponsor purchases 100 codes of tier S
await _paymentService.initializeSponsorPayment(
  subscriptionTierId: 1, // S tier
  quantity: 100,
);

// Expected result:
// - Payment initialized
// - WebView opens
// - Use test card: 5528790000000008
// - 3D Secure: 123456
// - Payment success
// - 100 codes generated
```

#### 2. Successful Farmer Payment

```dart
// Test: Farmer purchases 1 month of tier M
await _paymentService.initializeFarmerPayment(
  subscriptionTierId: 2, // M tier
  durationMonths: 1,
);

// Expected result:
// - Payment initialized
// - WebView opens
// - Use test card: 5528790000000008
// - 3D Secure: 123456
// - Payment success
// - Subscription created/extended
```

#### 3. Failed Payment

```dart
// Test: Use failing test card
// Use card: 5406670000000009
// Expected result:
// - Payment fails
// - Error message shown
// - No codes/subscription created
```

#### 4. Payment Cancellation

```dart
// Test: User cancels during payment
// 1. Initialize payment
// 2. Open WebView
// 3. User presses back/close button
// 4. Confirm cancellation
// Expected result:
// - WebView closes
// - User returns to previous screen
// - Transaction remains in Pending state
```

#### 5. Token Expiration

```dart
// Test: Token expires before verification
// 1. Initialize payment
// 2. Wait > 30 minutes (token expiry time)
// 3. Try to verify
// Expected result:
// - "Token expired" error
// - User prompted to restart payment
```

### Testing Checklist

- [ ] Initialize sponsor payment (valid tier, valid quantity)
- [ ] Initialize sponsor payment (invalid tier)
- [ ] Initialize sponsor payment (invalid quantity)
- [ ] Initialize farmer payment (valid tier, 1 month)
- [ ] Initialize farmer payment (valid tier, 12 months)
- [ ] Complete payment with success (test card 5528790000000008)
- [ ] Complete payment with failure (test card 5406670000000009)
- [ ] Cancel payment during WebView
- [ ] Handle deep link callback correctly
- [ ] Verify payment after success
- [ ] Verify payment after failure
- [ ] Get payment status for pending transaction
- [ ] Get payment status for completed transaction
- [ ] Handle network errors gracefully
- [ ] Handle token expiration
- [ ] Handle 401 (unauthorized)
- [ ] Handle 403 (forbidden)
- [ ] Verify sponsor codes are generated
- [ ] Verify farmer subscription is created/extended

---

## 🔒 Security Considerations

### 1. Token Storage

**DO NOT** store payment tokens in SharedPreferences or persistent storage.

```dart
// ❌ WRONG
final prefs = await SharedPreferences.getInstance();
await prefs.setString('payment_token', token); // NEVER DO THIS

// ✅ CORRECT
// Keep token only in memory during payment flow
String? _currentPaymentToken; // Class-level variable
```

### 2. Deep Link Validation

Always validate deep link tokens before processing:

```dart
void _handleDeepLink(String link) {
  final uri = Uri.parse(link);
  final token = uri.queryParameters['token'];

  // Validate token format (UUID)
  if (token == null || !_isValidUUID(token)) {
    debugPrint('[Security] Invalid token format');
    return;
  }

  // Verify token matches current payment session
  if (token != _currentPaymentToken) {
    debugPrint('[Security] Token mismatch');
    return;
  }

  // Proceed with verification
  _verifyPayment(token);
}

bool _isValidUUID(String token) {
  final uuidPattern = RegExp(
    r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
    caseSensitive: false,
  );
  return uuidPattern.hasMatch(token);
}
```

### 3. WebView Security

```dart
WebViewController()
  ..setJavaScriptMode(JavaScriptMode.unrestricted) // Required for payment
  ..setNavigationDelegate(
    NavigationDelegate(
      onNavigationRequest: (request) {
        // Only allow iyzico domains and deep links
        final uri = Uri.parse(request.url);

        if (uri.scheme == 'ziraai') {
          return NavigationDecision.navigate; // Allow deep link
        }

        if (uri.host.endsWith('iyzipay.com') || uri.host.endsWith('iyzico.com')) {
          return NavigationDecision.navigate; // Allow iyzico domains
        }

        // Block all other domains
        debugPrint('[Security] Blocked navigation to: ${request.url}');
        return NavigationDecision.prevent;
      },
    ),
  );
```

### 4. HTTPS Only

```dart
class PaymentService {
  PaymentService({required String baseUrl}) {
    // Validate HTTPS
    if (!baseUrl.startsWith('https://')) {
      throw ArgumentError('Payment API must use HTTPS');
    }
    this.baseUrl = baseUrl;
  }
}
```

### 5. Sensitive Data Logging

```dart
// ❌ WRONG - Logs sensitive data
debugPrint('Payment response: ${jsonEncode(response)}');

// ✅ CORRECT - Sanitize logs
debugPrint('Payment initialized: TransactionId=${response.transactionId}');
```

### 6. Certificate Pinning (Production)

For production, consider implementing certificate pinning:

```dart
// Using dio package with certificate pinning
final dio = Dio();
(dio.httpClientAdapter as DefaultHttpClientAdapter).onHttpClientCreate =
  (client) {
    client.badCertificateCallback =
      (X509Certificate cert, String host, int port) {
        // Implement certificate pinning logic
        return _validateCertificate(cert, host);
      };
    return client;
  };
```

---

## 📊 Payment Status Reference

### Status Values

| Status | Description | Final | Next Action |
|--------|-------------|-------|-------------|
| `Pending` | Payment initialized, awaiting completion | No | Wait for user to complete payment |
| `Success` | Payment completed successfully | Yes | Show success, create codes/subscription |
| `Failed` | Payment failed (card declined, insufficient funds) | Yes | Show error, allow retry |
| `Cancelled` | User cancelled payment | Yes | Return to previous screen |
| `Expired` | Payment token expired (>30 min) | Yes | Restart payment flow |

### Status Flow Diagram

```
┌─────────┐
│ Pending │ ──────────────┐
└────┬────┘               │
     │                    │
     │ User completes     │ User cancels
     │ payment           │ or timeout
     │                    │
     ▼                    ▼
┌─────────┐          ┌──────────┐
│ Success │          │ Failed / │
└─────────┘          │Cancelled │
                     │ /Expired │
                     └──────────┘
```

---

## 🎨 UI/UX Recommendations

### 1. Loading States

Show clear loading indicators during:
- Payment initialization
- WebView loading
- Payment verification

```dart
if (_isProcessing) {
  return const Center(
    child: Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        CircularProgressIndicator(),
        SizedBox(height: 16),
        Text('Ödeme işlemi yapılıyor...'),
      ],
    ),
  );
}
```

### 2. Error Messages

Use user-friendly error messages:

```dart
final userFriendlyErrors = {
  'Insufficient funds': 'Kartınızda yeterli bakiye bulunmuyor.',
  'Card expired': 'Kartınızın süresi dolmuş.',
  'Invalid card': 'Geçersiz kart bilgisi.',
  'Transaction declined': 'İşlem bankanız tarafından reddedildi.',
  'Token expired': 'Ödeme süresi doldu. Lütfen yeniden deneyin.',
};

String getUserFriendlyError(String error) {
  for (var entry in userFriendlyErrors.entries) {
    if (error.contains(entry.key)) {
      return entry.value;
    }
  }
  return error; // Return original if no match
}
```

### 3. Success Feedback

Provide clear success confirmation:

```dart
void _showSuccessDialog(PaymentVerifyResponse response) {
  showDialog(
    context: context,
    barrierDismissible: false,
    builder: (context) => AlertDialog(
      icon: const Icon(Icons.check_circle, color: Colors.green, size: 64),
      title: const Text('Ödeme Başarılı!'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text('${response.paidAmount} ${response.currency} ödemeniz başarıyla alındı.'),
          const SizedBox(height: 16),
          if (response.sponsorResult != null)
            Text('${response.sponsorResult!.codesGenerated} adet kod oluşturuldu.'),
          if (response.farmerResult != null)
            Text('Aboneliğiniz ${response.farmerResult!.endDate} tarihine kadar geçerlidir.'),
        ],
      ),
      actions: [
        ElevatedButton(
          onPressed: () {
            Navigator.pop(context);
            // Navigate to relevant screen
          },
          child: const Text('Devam Et'),
        ),
      ],
    ),
  );
}
```

### 4. Payment Summary

Show payment summary before initializing:

```dart
Widget _buildPaymentSummary() {
  return Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Ödeme Özeti', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          const Divider(),
          _buildSummaryRow('Abonelik Tipi', tierName),
          _buildSummaryRow('Miktar', '$quantity adet'),
          _buildSummaryRow('Birim Fiyat', '$unitPrice TRY'),
          const Divider(),
          _buildSummaryRow('Toplam', '$totalAmount TRY', isTotal: true),
        ],
      ),
    ),
  );
}
```

### 5. Network Status Indicator

Monitor network connectivity:

```dart
import 'package:connectivity_plus/connectivity_plus.dart';

StreamSubscription<ConnectivityResult>? _connectivitySubscription;

@override
void initState() {
  super.initState();
  _connectivitySubscription = Connectivity().onConnectivityChanged.listen((result) {
    if (result == ConnectivityResult.none) {
      _showNetworkError();
    }
  });
}

void _showNetworkError() {
  ScaffoldMessenger.of(context).showSnackBar(
    const SnackBar(
      content: Text('İnternet bağlantısı yok'),
      backgroundColor: Colors.red,
      duration: Duration(seconds: 5),
    ),
  );
}
```

---

## 📝 Changelog

### Version 1.0 (2025-11-21)
- Initial API documentation
- Sponsor bulk purchase flow
- Farmer subscription flow
- Flutter integration guide
- Error handling patterns
- Security recommendations

---

## 📧 Support

**Backend Team:**
- API Issues: backend-team@ziraai.com
- Integration Support: dev-support@ziraai.com

**Documentation:**
- API Docs: https://api.ziraai.com/swagger
- Postman Collection: `ZiraAI_Payment_Collection.json`

---

## ✅ Implementation Checklist

### Backend Prerequisites
- [x] PaymentController implemented
- [x] Operation claims created (185-188)
- [x] Claims assigned to Farmer and Sponsor roles
- [x] iyzico configuration added to appsettings
- [x] Database migrations applied
- [ ] Test in sandbox environment
- [ ] Production iyzico credentials configured

### Flutter Implementation
- [ ] Add payment dependencies to pubspec.yaml
- [ ] Create PaymentService class
- [ ] Create payment model classes
- [ ] Implement PaymentWebViewScreen
- [ ] Configure Android deep links
- [ ] Configure iOS deep links
- [ ] Implement deep link handler
- [ ] Create sponsor payment flow UI
- [ ] Create farmer payment flow UI
- [ ] Implement error handling
- [ ] Add loading states
- [ ] Add success/failure dialogs
- [ ] Test with sandbox cards
- [ ] Security review
- [ ] Production testing

---

**Document End**
