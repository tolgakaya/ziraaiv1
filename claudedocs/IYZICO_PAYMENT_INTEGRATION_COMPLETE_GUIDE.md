# iyzico Payment Integration - Complete Implementation Guide

**Date:** 2025-11-22
**Status:** ⚠️ CRITICAL - Multiple critical bugs discovered and fixed
**Source:** Official iyzico Postman Collection Analysis

## 🚨 Critical Issues Found and Fixed

1. **HMAC Signature Format** - Wrong format causing Error 1000
2. **Price Field Types** - Sending strings instead of numeric decimals causing Error 11
3. **Missing Required Fields** - 8+ required fields were missing
4. **FlowData Deserialization** - Case sensitivity issues

**All fixes have been deployed to Railway.**

---

## Table of Contents

1. [Authentication (HMACSHA256)](#authentication-hmacsha256)
2. [Payment Flow Overview](#payment-flow-overview)
3. [Phase 1: Initialize Payment](#phase-1-initialize-payment)
4. [Phase 2: User Completes Payment](#phase-2-user-completes-payment)
5. [Phase 3: Retrieve Payment Result](#phase-3-retrieve-payment-result)
6. [Phase 4: Verify Signature](#phase-4-verify-signature)
7. [Mobile App Implementation](#mobile-app-implementation)
8. [Backend Implementation Status](#backend-implementation-status)
9. [Testing Checklist](#testing-checklist)

---

## Authentication (HMACSHA256)

### ⚠️ CRITICAL FIX - Correct Authorization Header Format

**Previous WRONG Implementation:**
```csharp
// ❌ WRONG - This was causing "Geçersiz imza" (Invalid signature) errors
var authString = $"{apiKey}:{randomKey}:{signature}";
var authBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
return $"IYZWSv2 {authBase64}";
```

**Correct Implementation (from Postman):**
```javascript
// ✅ CORRECT - Format must be key-value pairs with & separator
var authorizationString = "apiKey:" + apiKey
                    + "&randomKey:" + randomKey
                    + "&signature:" + signature;
var base64EncodedAuthorization = CryptoJS.enc.Base64.stringify(
    CryptoJS.enc.Utf8.parse(authorizationString)
);
return "IYZWSv2 " + base64EncodedAuthorization;
```

### Step-by-Step Authentication

1. **Generate Random Key:**
   ```javascript
   var randomVar = "123456789";
   var randomKey = new Date().getTime() + randomVar;
   // Example: 1732246069123456789
   ```

2. **Get URI Path:**
   ```javascript
   // For: https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/initialize/auth/ecom
   // URI Path: /payment/iyzipos/checkoutform/initialize/auth/ecom
   ```

3. **Create Payload:**
   ```javascript
   var payload = _.isEmpty(request.data) ? uri_path : uri_path + request.data;
   // Example: /payment/iyzipos/checkoutform/initialize/auth/ecom{"locale":"tr",...}
   ```

4. **Create Data to Hash:**
   ```javascript
   var dataToEncrypt = randomKey + payload;
   // Example: 1732246069123456789/payment/iyzipos/checkoutform/initialize/auth/ecom{"locale":"tr",...}
   ```

5. **Generate HMAC-SHA256 Signature:**
   ```javascript
   var encryptedData = CryptoJS.HmacSHA256(dataToEncrypt, secretKey);
   var signature = CryptoJS.enc.Base64.stringify(encryptedData);
   ```

6. **Create Authorization String:**
   ```javascript
   // ✅ CRITICAL: Must use & separator and key-value format
   var authorizationString = "apiKey:" + apiKey
                       + "&randomKey:" + randomKey
                       + "&signature:" + signature;
   ```

7. **Base64 Encode:**
   ```javascript
   var base64EncodedAuthorization = CryptoJS.enc.Base64.stringify(
       CryptoJS.enc.Utf8.parse(authorizationString)
   );
   ```

8. **Final Header:**
   ```
   Authorization: IYZWSv2 {base64EncodedAuthorization}
   ```

---

## Payment Flow Overview

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Mobile    │      │   Backend   │      │   iyzico    │      │   Mobile    │
│     App     │      │     API     │      │     API     │      │   WebView   │
└─────────────┘      └─────────────┘      └─────────────┘      └─────────────┘
       │                    │                    │                    │
       │ 1. POST /payments  │                    │                    │
       │    /initialize     │                    │                    │
       ├───────────────────>│                    │                    │
       │                    │ 2. POST /payment/  │                    │
       │                    │    checkoutform/   │                    │
       │                    │    initialize      │                    │
       │                    ├───────────────────>│                    │
       │                    │                    │                    │
       │                    │ 3. Return token    │                    │
       │                    │    & payment URL   │                    │
       │                    │<───────────────────┤                    │
       │ 4. Return          │                    │                    │
       │    paymentPageUrl  │                    │                    │
       │<───────────────────┤                    │                    │
       │                    │                    │                    │
       │ 5. Open WebView    │                    │                    │
       │    with URL        │                    │                    │
       ├────────────────────────────────────────────────────────────>│
       │                    │                    │                    │
       │                    │                    │ 6. User completes  │
       │                    │                    │    payment         │
       │                    │                    │<───────────────────┤
       │                    │                    │                    │
       │ 7. Callback via    │                    │                    │
       │    deep link       │                    │                    │
       │<────────────────────────────────────────────────────────────┤
       │                    │                    │                    │
       │ 8. POST /payments/ │                    │                    │
       │    verify          │                    │                    │
       ├───────────────────>│                    │                    │
       │                    │ 9. POST /payment/  │                    │
       │                    │    checkoutform/   │                    │
       │                    │    auth/detail     │                    │
       │                    ├───────────────────>│                    │
       │                    │                    │                    │
       │                    │ 10. Return payment │                    │
       │                    │     details        │                    │
       │                    │<───────────────────┤                    │
       │                    │                    │                    │
       │ 11. Verify         │                    │                    │
       │     signature &    │                    │                    │
       │     activate codes │                    │                    │
       │<───────────────────┤                    │                    │
```

---

## Phase 1: Initialize Payment

### Backend Endpoint
```
POST /api/v1/payments/initialize
```

### Request Body
```json
{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,
    "quantity": 50
  },
  "currency": "TRY"
}
```

### Backend → iyzico API Call

**Endpoint:**
```
POST https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/initialize/auth/ecom
```

**Headers:**
```
Authorization: IYZWSv2 {base64_encoded_auth_string}
Content-Type: application/json
```

**Required Request Body Fields:**

| Field | Type | Required | Example | Notes |
|-------|------|----------|---------|-------|
| `locale` | string | ✅ | "tr" | Language code |
| `conversationId` | string | ✅ | "SponsorBulkPurchase_134_638993824694005095" | Unique transaction ID |
| `price` | **decimal** | ✅ | 4999.50 | Total basket amount (**MUST be numeric, NOT string**) |
| `paidPrice` | **decimal** | ✅ | 4999.50 | Amount to be paid (**MUST be numeric, NOT string**) |
| `currency` | string | ✅ | "TRY" | Currency code |
| `basketId` | string | ✅ | Same as conversationId | Basket identifier |
| `paymentChannel` | string | ✅ | "MOBILE" | Payment channel |
| `paymentGroup` | string | ✅ | "SUBSCRIPTION" | Payment group type |
| `callbackUrl` | string | ✅ | "ziraai://payment-callback" | Deep link for return |
| `enabledInstallments` | array | ✅ | [1] | Allowed installment options |
| `buyer` | object | ✅ | See below | Buyer information |
| `shippingAddress` | object | ✅ | See below | Shipping address |
| `billingAddress` | object | ✅ | See below | Billing address |
| `basketItems` | array | ✅ | See below | Items in basket |

**Buyer Object (ALL fields required):**
```json
{
  "id": "134",
  "name": "User",
  "surname": "Full Name",
  "email": "user@example.com",
  "gsmNumber": "+905350000000",
  "identityNumber": "11111111111",
  "registrationDate": "2024-01-15 10:30:45",
  "lastLoginDate": "2025-11-22 07:30:00",
  "registrationAddress": "N/A",
  "city": "Istanbul",
  "country": "Turkey",
  "zipCode": "34732",
  "ip": "127.0.0.1"
}
```

**ShippingAddress & BillingAddress Objects:**
```json
{
  "address": "N/A",
  "zipCode": "34742",
  "contactName": "User Full Name",
  "city": "Istanbul",
  "country": "Turkey"
}
```

**BasketItems Array (at least 1 item required):**
```json
[
  {
    "id": "1",
    "price": 4999.50,  // CRITICAL: Must be numeric decimal, NOT string
    "name": "Sponsorship Package",
    "category1": "Subscription",
    "category2": "Service",
    "itemType": "VIRTUAL"
  }
]
```

### iyzico Response (Success)

```json
{
  "status": "success",
  "locale": "tr",
  "systemTime": 1732246069123,
  "conversationId": "SponsorBulkPurchase_134_638993824694005095",
  "token": "c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f",
  "checkoutFormContent": "https://sandbox-merchant.iyzipay.com/checkoutform/auth/...",
  "paymentPageUrl": "https://sandbox-merchant.iyzipay.com/checkoutform/auth/...",
  "signature": "a1b2c3d4e5f6..."
}
```

### Backend Response to Mobile

```json
{
  "success": true,
  "data": {
    "transactionId": 123,
    "paymentToken": "c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f",
    "paymentPageUrl": "https://sandbox-merchant.iyzipay.com/checkoutform/auth/...",
    "callbackUrl": "ziraai://payment-callback?token=c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f",
    "amount": 4999.50,
    "currency": "TRY",
    "expiresAt": "2025-11-22T08:00:00Z",
    "status": "Initialized",
    "conversationId": "SponsorBulkPurchase_134_638993824694005095"
  },
  "message": "Payment initialized successfully"
}
```

---

## Phase 2: User Completes Payment

### Mobile App Actions

1. **Open WebView with `paymentPageUrl`**
   ```dart
   await launch(paymentPageUrl,
     forceSafariVC: true,
     forceWebView: true
   );
   ```

2. **Listen for Deep Link Callback**
   ```dart
   // App receives: ziraai://payment-callback?token=c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f

   void handleDeepLink(Uri uri) {
     if (uri.path == '/payment-callback') {
       String? token = uri.queryParameters['token'];
       if (token != null) {
         verifyPayment(token);
       }
     }
   }
   ```

---

## Phase 3: Retrieve Payment Result

### Backend Endpoint
```
POST /api/v1/payments/verify
```

### Request Body
```json
{
  "token": "c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f"
}
```

### Backend → iyzico API Call

**Endpoint:**
```
POST https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/auth/ecom/detail
```

**Request Body:**
```json
{
  "locale": "tr",
  "conversationId": "SponsorBulkPurchase_134_638993824694005095",
  "token": "c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f"
}
```

### iyzico Response (Success)

```json
{
  "status": "success",
  "locale": "tr",
  "systemTime": 1732246169123,
  "conversationId": "SponsorBulkPurchase_134_638993824694005095",
  "paymentId": "12345678",
  "paymentStatus": "SUCCESS",
  "fraudStatus": 1,
  "merchantCommissionRate": 0,
  "merchantCommissionRateAmount": 0,
  "iyziCommissionRateAmount": 0,
  "iyziCommissionFee": 0,
  "cardType": "CREDIT_CARD",
  "cardAssociation": "MASTER_CARD",
  "cardFamily": "Bonus",
  "binNumber": "552608",
  "lastFourDigits": "0006",
  "basketId": "SponsorBulkPurchase_134_638993824694005095",
  "currency": "TRY",
  "price": "4999.50",
  "paidPrice": "4999.50",
  "installment": 1,
  "token": "c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f",
  "signature": "def456...",
  "itemTransactions": [...]
}
```

---

## Phase 4: Verify Signature

### Response Signature Verification (Critical Security Step)

**Purpose:** Verify that the response actually came from iyzico and hasn't been tampered with.

**Algorithm:**
```javascript
function generateResponseSignature(params) {
  var dataToEncrypt = params.join(":");
  var encryptedData = CryptoJS.HmacSHA256(dataToEncrypt, secretKey);
  var data = CryptoJS.enc.Hex.stringify(encryptedData);  // Note: HEX not Base64!
  return data;
}

// For checkout form detail response:
var expectedSignature = generateResponseSignature([
  jsonData.paymentStatus,      // "SUCCESS"
  jsonData.paymentId,           // "12345678"
  jsonData.currency,            // "TRY"
  jsonData.basketId,            // "SponsorBulkPurchase_..."
  jsonData.conversationId,      // "SponsorBulkPurchase_..."
  formatBigDecimal(jsonData.paidPrice),  // "4999.5" (trailing zeros removed)
  formatBigDecimal(jsonData.price),      // "4999.5" (trailing zeros removed)
  jsonData.token                // "c4b91f9e..."
]);

function formatBigDecimal(bigDecimal) {
  str = bigDecimal.toString();
  if (str.includes('.')) {
    str = str.replace(/(\\.[0-9]*?)0+$/, '$1');  // Remove trailing zeros
    str = str.replace(/\\.$/, '');                // Remove trailing dot
  }
  return str;
}
```

**C# Implementation:**
```csharp
private string GenerateResponseSignature(params string[] values)
{
    var dataToEncrypt = string.Join(":", values);
    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey)))
    {
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToEncrypt));
        // CRITICAL: Use HEX encoding for response signature, NOT Base64!
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

// Verify
var expectedSignature = GenerateResponseSignature(
    response.PaymentStatus,
    response.PaymentId,
    response.Currency,
    response.BasketId,
    response.ConversationId,
    FormatBigDecimal(response.PaidPrice),
    FormatBigDecimal(response.Price),
    response.Token
);

if (expectedSignature != response.Signature)
{
    throw new SecurityException("Invalid signature - possible tampering detected");
}
```

---

## Mobile App Implementation

### 1. Initialize Payment

```dart
Future<PaymentInitializeResponse> initializePayment({
  required String flowType,
  required int subscriptionTierId,
  required int quantity,
}) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/payments/initialize'),
    headers: {
      'Authorization': 'Bearer $accessToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'flowType': flowType,
      'flowData': {
        'subscriptionTierId': subscriptionTierId,
        'quantity': quantity,
      },
      'currency': 'TRY',
    }),
  );

  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    return PaymentInitializeResponse.fromJson(data['data']);
  } else {
    throw Exception('Payment initialization failed');
  }
}
```

### 2. Open Payment WebView

```dart
Future<void> openPaymentPage(String paymentPageUrl) async {
  // Configure deep link handling BEFORE opening WebView
  _configureDeepLinkListener();

  // Open iyzico payment page in WebView
  if (await canLaunch(paymentPageUrl)) {
    await launch(
      paymentPageUrl,
      forceSafariVC: true,
      forceWebView: true,
      enableJavaScript: true,
    );
  } else {
    throw Exception('Could not launch payment page');
  }
}
```

### 3. Handle Deep Link Callback

```dart
void _configureDeepLinkListener() {
  // Using uni_links package
  _sub = uriLinkStream.listen((Uri? uri) {
    if (uri != null && uri.scheme == 'ziraai' && uri.host == 'payment-callback') {
      final token = uri.queryParameters['token'];
      if (token != null) {
        _verifyPayment(token);
      }
    }
  }, onError: (err) {
    // Handle error
  });
}
```

### 4. Verify Payment

```dart
Future<PaymentVerifyResponse> verifyPayment(String token) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/payments/verify'),
    headers: {
      'Authorization': 'Bearer $accessToken',
      'Content-Type': 'application/json',
    },
    body: jsonEncode({
      'token': token,
    }),
  );

  if (response.statusCode == 200) {
    final data = jsonDecode(response.body);
    if (data['success'] == true) {
      // Payment successful - show success screen
      return PaymentVerifyResponse.fromJson(data['data']);
    } else {
      // Payment failed
      throw Exception(data['message']);
    }
  } else {
    throw Exception('Payment verification failed');
  }
}
```

### 5. Complete Flow Example

```dart
class PaymentScreen extends StatefulWidget {
  final int tierId;
  final int quantity;

  @override
  _PaymentScreenState createState() => _PaymentScreenState();
}

class _PaymentScreenState extends State<PaymentScreen> {
  StreamSubscription? _deepLinkSub;
  bool _isProcessing = false;

  @override
  void initState() {
    super.initState();
    _initializePayment();
  }

  Future<void> _initializePayment() async {
    try {
      setState(() => _isProcessing = true);

      // Step 1: Initialize payment
      final initResponse = await PaymentService().initializePayment(
        flowType: 'SponsorBulkPurchase',
        subscriptionTierId: widget.tierId,
        quantity: widget.quantity,
      );

      // Step 2: Configure deep link listener
      _configureDeepLinkListener();

      // Step 3: Open payment page
      await _openPaymentPage(initResponse.paymentPageUrl);

    } catch (e) {
      _showError(e.toString());
    } finally {
      setState(() => _isProcessing = false);
    }
  }

  void _configureDeepLinkListener() {
    _deepLinkSub = uriLinkStream.listen((Uri? uri) {
      if (uri?.scheme == 'ziraai' && uri?.host == 'payment-callback') {
        final token = uri?.queryParameters['token'];
        if (token != null) {
          _verifyPayment(token);
        }
      }
    });
  }

  Future<void> _verifyPayment(String token) async {
    try {
      final verifyResponse = await PaymentService().verifyPayment(token);

      if (verifyResponse.status == 'SUCCESS') {
        _showSuccessScreen(verifyResponse);
      } else {
        _showError('Payment failed: ${verifyResponse.status}');
      }
    } catch (e) {
      _showError('Verification failed: $e');
    }
  }

  @override
  void dispose() {
    _deepLinkSub?.cancel();
    super.dispose();
  }
}
```

---

## Backend Implementation Status

### ✅ Completed

1. Payment initialization endpoint
2. HMAC signature generation (FIXED)
3. Request body formatting (FIXED - added all required fields)
4. Database transaction tracking

### ⚠️ Needs Review/Fix

1. **Verify endpoint** - Needs implementation
2. **Signature verification** - Must use HEX encoding (not Base64)
3. **Post-payment business logic** - Activate sponsorship codes
4. **Error handling** - Map iyzico error codes to user-friendly messages

### 🔴 Critical Issues Found

1. **Wrong HMAC format** - Was using `apiKey:randomKey:signature` instead of `apiKey:VALUE&randomKey:VALUE&signature:VALUE`
2. **Missing required fields** - basketId, gsmNumber, zipCode, registrationDate, lastLoginDate, category2
3. **Response signature verification** - Must use HEX encoding (different from request signature which uses Base64)

---

## Testing Checklist

### Unit Tests

- [ ] HMAC signature generation (verify against Postman examples)
- [ ] Response signature verification
- [ ] BigDecimal formatting for signature
- [ ] Request body serialization

### Integration Tests

- [ ] Initialize payment (happy path)
- [ ] Initialize payment (invalid tier)
- [ ] Initialize payment (insufficient data)
- [ ] Verify payment (success)
- [ ] Verify payment (failure)
- [ ] Verify payment (invalid signature)

### End-to-End Tests

- [ ] Complete payment flow (success)
- [ ] User cancels payment
- [ ] Payment timeout
- [ ] Network error during payment
- [ ] Deep link handling
- [ ] Sponsorship code activation after successful payment

### Security Tests

- [ ] Signature tampering detection
- [ ] Replay attack prevention
- [ ] Token expiration
- [ ] Invalid token handling

---

## Common Error Codes

| Code | Message | Meaning | Solution |
|------|---------|---------|----------|
| 1000 | Geçersiz imza | Invalid signature | Check HMAC algorithm and format |
| 11 | Geçersiz istek | Invalid request | Missing or invalid required fields |
| 5 | Transaction not approved | Payment declined | User should try different card |
| 10012 | Invalid card number | Invalid card | Check card number format |
| 10051 | Card not active | Card not activated | Use different card |

---

## Next Steps

1. ✅ Fix HMAC signature format (COMPLETED)
2. ✅ Add missing required fields (COMPLETED)
3. 🔄 Implement verify endpoint
4. 🔄 Implement response signature verification
5. 🔄 Test complete flow end-to-end
6. 🔄 Add comprehensive error handling
7. 🔄 Mobile app integration testing

---

## References

- iyzico Official Postman Collection: `claudedocs/iyzico Collection.postman_collection.json`
- Backend Service: `Business/Services/Payment/IyzicoPaymentService.cs`
- Payment Controller: `WebAPI/Controllers/PaymentController.cs`
