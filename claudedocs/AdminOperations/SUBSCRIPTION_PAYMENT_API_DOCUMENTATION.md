# Subscription Payment API Documentation

**For Mobile & Frontend Teams**

**Date:** 2025-11-22
**API Version:** v1
**Base URL:** `https://ziraai-api-sit.up.railway.app` (Staging) | `https://ziraai.com` (Production)

---

## Overview

This document describes the subscription payment flow endpoints that replace the previous mock payment implementation. Users (farmers) can now purchase subscriptions using real iyzico payment gateway integration.

### Payment Flow Summary

```
1. User selects subscription tier → Mobile calls Subscribe endpoint (validation)
2. Subscribe returns payment details → Mobile calls Payment Initialize endpoint
3. Payment Initialize returns paymentPageUrl → Mobile opens WebView with iyzico form
4. User completes payment (3D Secure) → iyzico calls backend callback
5. Backend creates subscription → Backend redirects to deep link
6. Mobile receives deep link → Mobile calls Payment Verify endpoint
7. Payment Verify returns subscription details → Mobile shows success screen
```

---

## Endpoint 1: Subscribe (Validation Only)

**Purpose:** Validate subscription eligibility and return payment initialization instructions.

**⚠️ Important:** This endpoint **does NOT create the subscription**. It only validates that the user can subscribe and returns payment details. Actual subscription is created after successful payment.

### Request

```http
POST /api/v1/subscriptions/subscribe HTTP/1.1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "subscriptionTierId": 2,
  "durationMonths": 1,
  "autoRenew": false,
  "startDate": null
}
```

### Request Body Parameters

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `subscriptionTierId` | integer | ✅ Yes | Subscription tier ID to subscribe to | `2` (Premium) |
| `durationMonths` | integer | No | Subscription duration in months (1-12)<br>Defaults to 1 | `1` (monthly)<br>`12` (yearly) |
| `autoRenew` | boolean | No | Enable auto-renewal<br>Defaults to false | `false` |
| `startDate` | string (ISO 8601) | No | Subscription start date<br>Defaults to now | `null` or `"2025-11-22T10:00:00Z"` |

### Response (200 OK - Success)

```json
{
  "success": true,
  "message": "Subscription validated. Please proceed to payment initialization.",
  "data": {
    "subscriptionTierId": 2,
    "tierName": "S",
    "tierDisplayName": "Küçük",
    "amount": 199.99,
    "currency": "TRY",
    "durationMonths": 1,
    "nextStep": "Initialize payment via POST /api/v1/payments/initialize",
    "paymentInitializeUrl": "/api/v1/payments/initialize",
    "paymentFlowType": "FarmerSubscription"
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `subscriptionTierId` | integer | Subscription tier ID |
| `tierName` | string | Tier internal name (`"Trial"`, `"S"`, `"M"`, `"L"`, `"XL"`) |
| `tierDisplayName` | string | Tier display name in Turkish |
| `amount` | decimal | Total amount to be paid in TRY |
| `currency` | string | Currency code (always `"TRY"`) |
| `durationMonths` | integer | Subscription duration in months |
| `nextStep` | string | Instruction for mobile app |
| `paymentInitializeUrl` | string | Next endpoint to call |
| `paymentFlowType` | string | Flow type for payment initialization (always `"FarmerSubscription"`) |

### Error Responses

**400 Bad Request - Invalid Tier:**
```json
{
  "success": false,
  "message": "Invalid subscription tier"
}
```

**400 Bad Request - Already Subscribed:**
```json
{
  "success": false,
  "message": "You already have an active subscription. Please cancel it first."
}
```

**Note:** Users with **trial subscriptions** can upgrade to paid subscriptions. The trial will be automatically cancelled during payment processing.

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

### Mobile Implementation Example (Flutter)

```dart
Future<SubscribeResponseDto> validateSubscription({
  required int subscriptionTierId,
  int? durationMonths,
  bool autoRenew = false,
}) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/subscriptions/subscribe'),
    headers: {
      'Authorization': 'Bearer $jwtToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'subscriptionTierId': subscriptionTierId,
      'durationMonths': durationMonths ?? 1,
      'autoRenew': autoRenew,
      'startDate': null,
    }),
  );

  if (response.statusCode == 200) {
    final jsonData = jsonDecode(response.body);
    return SubscribeResponseDto.fromJson(jsonData['data']);
  } else {
    throw Exception('Failed to validate subscription');
  }
}
```

---

## Endpoint 2: Initialize Payment

**Purpose:** Initialize iyzico payment and get payment page URL.

**⚠️ Important:** This endpoint creates a pending payment transaction and returns the iyzico payment form URL. User must complete payment in WebView.

### Request

```http
POST /api/v1/payments/initialize HTTP/1.1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "flowType": "FarmerSubscription",
  "flowData": {
    "subscriptionTierId": 2,
    "durationMonths": 1
  },
  "currency": "TRY"
}
```

### Request Body Parameters

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `flowType` | string | ✅ Yes | Payment flow type<br>**Must be:** `"FarmerSubscription"` | `"FarmerSubscription"` |
| `flowData` | object | ✅ Yes | Subscription purchase data | See below |
| `flowData.subscriptionTierId` | integer | ✅ Yes | Subscription tier ID | `2` |
| `flowData.durationMonths` | integer | No | Subscription duration (1-12)<br>Defaults to 1 | `1` or `12` |
| `currency` | string | No | Currency code<br>Defaults to `"TRY"` | `"TRY"` |

### Response (200 OK - Success)

```json
{
  "success": true,
  "message": "Payment initialized successfully",
  "data": {
    "paymentToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=a1b2c3d4-e5f6-7890-abcd-ef1234567890&lang=tr",
    "callbackUrl": "ziraai://payment-callback",
    "amount": 199.99,
    "currency": "TRY",
    "expiresAt": "2025-11-22T11:30:00Z"
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `paymentToken` | string | Unique payment token (UUID) - save this for verification |
| `paymentPageUrl` | string | iyzico payment form URL - open in WebView |
| `callbackUrl` | string | Deep link for mobile app callback |
| `amount` | decimal | Payment amount |
| `currency` | string | Currency code |
| `expiresAt` | string (ISO 8601) | Payment expiration time (30 minutes) |

### Error Responses

**400 Bad Request - Invalid Tier:**
```json
{
  "success": false,
  "message": "Subscription tier not found"
}
```

**500 Internal Server Error - iyzico API Error:**
```json
{
  "success": false,
  "message": "Failed to initialize payment. Please try again."
}
```

### Mobile Implementation Example (Flutter)

```dart
Future<PaymentInitializeResponseDto> initializePayment({
  required int subscriptionTierId,
  int? durationMonths,
}) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/payments/initialize'),
    headers: {
      'Authorization': 'Bearer $jwtToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'flowType': 'FarmerSubscription',
      'flowData': {
        'subscriptionTierId': subscriptionTierId,
        'durationMonths': durationMonths ?? 1,
      },
      'currency': 'TRY',
    }),
  );

  if (response.statusCode == 200) {
    final jsonData = jsonDecode(response.body);
    return PaymentInitializeResponseDto.fromJson(jsonData['data']);
  } else {
    throw Exception('Failed to initialize payment');
  }
}
```

---

## Endpoint 3: WebView Integration

**Purpose:** Display iyzico payment form and handle 3D Secure authentication.

### WebView Configuration

```dart
import 'package:webview_flutter/webview_flutter.dart';

class PaymentWebView extends StatefulWidget {
  final String paymentPageUrl;
  final String paymentToken;

  const PaymentWebView({
    required this.paymentPageUrl,
    required this.paymentToken,
  });

  @override
  _PaymentWebViewState createState() => _PaymentWebViewState();
}

class _PaymentWebViewState extends State<PaymentWebView> {
  late final WebViewController _controller;

  @override
  void initState() {
    super.initState();

    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setNavigationDelegate(
        NavigationDelegate(
          onNavigationRequest: (NavigationRequest request) {
            // Detect deep link callback
            if (request.url.startsWith('ziraai://payment-callback')) {
              _handlePaymentCallback(request.url);
              return NavigationDecision.prevent;
            }
            return NavigationDecision.navigate;
          },
          onPageFinished: (String url) {
            print('Payment page loaded: $url');
          },
        ),
      )
      ..loadRequest(Uri.parse(widget.paymentPageUrl));
  }

  void _handlePaymentCallback(String deepLinkUrl) {
    // Parse deep link parameters
    final uri = Uri.parse(deepLinkUrl);
    final token = uri.queryParameters['token'];
    final status = uri.queryParameters['status'];

    print('Payment callback: token=$token, status=$status');

    // Close WebView and verify payment
    Navigator.of(context).pop();
    _verifyPayment(token!);
  }

  Future<void> _verifyPayment(String paymentToken) async {
    // Call verify endpoint (see Endpoint 4)
    final result = await paymentService.verifyPayment(paymentToken);

    if (result.status == 'Success') {
      // Show success screen
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => SubscriptionSuccessScreen(
            subscription: result.subscription,
          ),
        ),
      );
    } else {
      // Show error screen
      _showError('Payment failed: ${result.errorMessage}');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Ödeme')),
      body: WebViewWidget(controller: _controller),
    );
  }
}
```

### Test Card Details (Sandbox)

| Field | Value |
|-------|-------|
| Card Number | `5528790000000008` |
| Expiry Date | `12/2030` |
| CVV | `123` |
| Cardholder Name | `Test User` |
| 3D Secure Code | `123456` |

---

## Endpoint 4: Verify Payment

**Purpose:** Verify payment status and retrieve subscription details after payment completion.

### Request

```http
POST /api/v1/payments/verify HTTP/1.1
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "paymentToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### Request Body Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `paymentToken` | string | ✅ Yes | Payment token from initialize response or deep link |

### Response (200 OK - Success)

```json
{
  "success": true,
  "message": "Payment verified successfully",
  "data": {
    "transactionId": 25,
    "status": "Success",
    "paymentId": "27659973",
    "paymentToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "amount": 199.99,
    "currency": "TRY",
    "paidAmount": 199.99,
    "completedAt": "2025-11-22T10:45:00Z",
    "errorMessage": null,
    "flowType": "FarmerSubscription",
    "flowResult": {
      "subscriptionId": 156,
      "subscriptionTierName": "Küçük",
      "startDate": "2025-11-22T10:45:00Z",
      "endDate": "2025-12-22T10:45:00Z"
    }
  }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `transactionId` | integer | Payment transaction ID |
| `status` | string | Payment status: `"Success"`, `"Failed"`, `"Pending"` |
| `paymentId` | string | iyzico payment ID |
| `paymentToken` | string | Payment token |
| `amount` | decimal | Payment amount |
| `currency` | string | Currency code |
| `paidAmount` | decimal | Amount actually paid (same as amount) |
| `completedAt` | string (ISO 8601) | Payment completion timestamp |
| `errorMessage` | string or null | Error message if payment failed |
| `flowType` | string | Payment flow type (`"FarmerSubscription"`) |
| `flowResult` | object | Subscription details (see below) |

### FlowResult Object (FarmerSubscription)

| Field | Type | Description |
|-------|------|-------------|
| `subscriptionId` | integer | Created subscription ID |
| `subscriptionTierName` | string | Subscription tier display name |
| `startDate` | string (ISO 8601) | Subscription start date |
| `endDate` | string (ISO 8601) | Subscription end date |

### Response (200 OK - Failed Payment)

```json
{
  "success": true,
  "message": "Payment verified successfully",
  "data": {
    "transactionId": 26,
    "status": "Failed",
    "paymentId": null,
    "paymentToken": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
    "amount": 199.99,
    "currency": "TRY",
    "paidAmount": 0.00,
    "completedAt": null,
    "errorMessage": "Payment declined by issuer",
    "flowType": "FarmerSubscription",
    "flowResult": null
  }
}
```

### Error Responses

**404 Not Found - Invalid Token:**
```json
{
  "success": false,
  "message": "Payment transaction not found"
}
```

### Mobile Implementation Example (Flutter)

```dart
Future<PaymentVerifyResponseDto> verifyPayment(String paymentToken) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/payments/verify'),
    headers: {
      'Authorization': 'Bearer $jwtToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'paymentToken': paymentToken,
    }),
  );

  if (response.statusCode == 200) {
    final jsonData = jsonDecode(response.body);
    return PaymentVerifyResponseDto.fromJson(jsonData['data']);
  } else {
    throw Exception('Failed to verify payment');
  }
}
```

---

## Complete Flow Example (Flutter)

```dart
class SubscriptionPurchaseFlow {
  final PaymentService paymentService;
  final SubscriptionService subscriptionService;

  SubscriptionPurchaseFlow({
    required this.paymentService,
    required this.subscriptionService,
  });

  Future<void> purchaseSubscription({
    required BuildContext context,
    required int subscriptionTierId,
    int? durationMonths,
    bool autoRenew = false,
  }) async {
    try {
      // Step 1: Validate subscription
      print('Step 1: Validating subscription...');
      final validateResponse = await subscriptionService.validateSubscription(
        subscriptionTierId: subscriptionTierId,
        durationMonths: durationMonths,
        autoRenew: autoRenew,
      );

      // Show confirmation dialog
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (context) => ConfirmationDialog(
          title: 'Confirm Subscription',
          message: 'Subscribe to ${validateResponse.tierDisplayName} '
              'for ${validateResponse.amount} ${validateResponse.currency}?',
        ),
      );

      if (confirmed != true) return;

      // Step 2: Initialize payment
      print('Step 2: Initializing payment...');
      final initResponse = await paymentService.initializePayment(
        subscriptionTierId: subscriptionTierId,
        durationMonths: durationMonths,
      );

      // Step 3: Open payment WebView
      print('Step 3: Opening payment WebView...');
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (context) => PaymentWebView(
            paymentPageUrl: initResponse.paymentPageUrl,
            paymentToken: initResponse.paymentToken,
          ),
        ),
      );

      // WebView handles payment completion and calls verify
      // Flow continues in PaymentWebView._verifyPayment()
    } catch (e) {
      print('Subscription purchase error: $e');
      _showError(context, 'Failed to purchase subscription: $e');
    }
  }

  void _showError(BuildContext context, String message) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Error'),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: Text('OK'),
          ),
        ],
      ),
    );
  }
}
```

---

## Trial Subscription Upgrade

When a user with an **active trial subscription** purchases a paid subscription:

1. **Subscribe endpoint** detects trial subscription and allows upgrade
2. **Payment flow** proceeds normally
3. **Payment callback** automatically:
   - Cancels trial subscription (Status: `"Upgraded"`)
   - Creates new paid subscription
   - User now has paid subscription only

**Mobile app does NOT need special handling** - backend manages trial upgrade automatically.

---

## Subscription Status After Payment

### UserSubscription Record Fields

| Field | Value After Payment |
|-------|---------------------|
| `userId` | Current user ID |
| `subscriptionTierId` | Selected tier ID |
| `startDate` | Payment completion time |
| `endDate` | startDate + duration months |
| `isActive` | `true` |
| `autoRenew` | `false` (manual purchases) |
| `paymentMethod` | `"CreditCard"` |
| `paymentReference` | iyzico payment ID |
| `paymentTransactionId` | Payment transaction ID |
| `paidAmount` | Payment amount |
| `currency` | `"TRY"` |
| `lastPaymentDate` | Payment completion time |
| `status` | `"Active"` |
| `isTrialSubscription` | `false` |
| `isSponsoredSubscription` | `false` |

### Cache Invalidation

After subscription creation, the backend automatically invalidates:
```
Cache Key: UserSubscription:{userId}
```

Mobile app should refetch user subscription data after successful payment to get latest status.

---

## Error Handling

### Common Error Scenarios

| Scenario | HTTP Status | Error Message | Action |
|----------|-------------|---------------|--------|
| Invalid tier ID | 400 | "Invalid subscription tier" | Show error, go back |
| Already subscribed | 400 | "You already have an active subscription..." | Show message, suggest cancellation |
| Payment initialization failed | 500 | "Failed to initialize payment. Please try again." | Retry or contact support |
| Payment declined | 200 (verify) | Status: "Failed", errorMessage: "..." | Show error message |
| Payment expired | 404 | "Payment transaction not found" | Restart flow |
| Network error | N/A | Connection timeout | Retry with exponential backoff |

### Recommended Error UI

```dart
void handlePaymentError(String errorType, String message) {
  switch (errorType) {
    case 'already_subscribed':
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Already Subscribed'),
          content: Text('You already have an active subscription. '
              'Please cancel it first before purchasing a new one.'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pushNamed(context, '/subscriptions'),
              child: Text('View Subscriptions'),
            ),
          ],
        ),
      );
      break;

    case 'payment_declined':
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Payment Declined'),
          content: Text(message),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('Try Again'),
            ),
          ],
        ),
      );
      break;

    default:
      showSnackBar('Error: $message');
  }
}
```

---

## Testing Checklist

### Staging Environment Testing

- [ ] **Happy Path - Monthly Subscription**
  - Select tier S (Small)
  - Duration: 1 month
  - Complete payment with test card
  - Verify subscription created
  - Check subscription end date = now + 1 month

- [ ] **Yearly Subscription**
  - Select tier M (Medium)
  - Duration: 12 months
  - Verify amount = yearly price (not monthly × 12)
  - Complete payment
  - Check subscription end date = now + 12 months

- [ ] **Trial to Paid Upgrade**
  - User has active trial subscription
  - Purchase tier S
  - Verify trial cancelled (Status: "Upgraded")
  - Verify new paid subscription created
  - Check user has only 1 active subscription

- [ ] **Already Subscribed Error**
  - User has active paid subscription
  - Try to purchase another subscription
  - Verify error: "You already have an active subscription..."

- [ ] **Payment Failure**
  - Start payment flow
  - Decline payment on iyzico page
  - Verify no subscription created
  - Verify transaction status = "Failed"

- [ ] **Payment Timeout**
  - Start payment flow
  - Wait 30+ minutes without completing
  - Try to verify payment
  - Verify error or expired status

### Production Monitoring

After production deployment, monitor:
- **Railway logs** for payment processing errors
- **Payment transaction** records in database
- **User subscription** creation rate
- **Failed payment** reasons
- **Cache invalidation** logs

---

## Support & Troubleshooting

### Backend Logs

Key log messages to search for:

```
[Subscription] User {userId} initiated subscription purchase
[iyzico] Payment initialized. Token: {token}
[iyzico] Callback received from iyzico. Token: {token}, Status: {status}
[iyzico] Upgrading from trial subscription. TrialSubId: {id}
[iyzico] Created paid subscription after trial upgrade. SubscriptionId: {id}
[SubscriptionCache] 🗑️ Invalidated cache for user {userId}
```

### Common Issues

**Issue:** WebView shows "Webpage not available"
- **Cause:** Callback URL misconfigured
- **Solution:** Check Railway environment variable `Iyzico__Callback__FallbackUrl`

**Issue:** Payment success but no subscription created
- **Cause:** ProcessFarmerSubscriptionAsync error
- **Solution:** Check backend logs for exception details

**Issue:** User sees "Already subscribed" but has trial
- **Cause:** Trial detection logic issue
- **Solution:** Check `IsTrialSubscription` field value

---

## Contact

**Backend Team:** For API issues, check Railway logs
**Mobile Team:** For integration questions, refer to this documentation
**QA Team:** Use testing checklist above

---

**Document Version:** 1.0
**Last Updated:** 2025-11-22
**Status:** ✅ Ready for Integration
