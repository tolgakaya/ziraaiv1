# Mobile Payment URL Loading Issue - Fix Required

**Date:** 2025-11-22
**Issue:** Mobile app loading callback URL instead of payment page URL
**Error:** `net::ERR_HTTP_RESPONSE_CODE_FAILURE`
**Status:** ⚠️ MOBILE FIX REQUIRED

---

## Problem Summary

The mobile app is loading the **callback URL** in the WebView instead of the **payment page URL**, causing a "Webpage not available" error.

### Current Behavior (WRONG ❌)

```
Mobile App WebView loads:
https://ziraai-api-sit.up.railway.app/payment/callback

Result: net::ERR_HTTP_RESPONSE_CODE_FAILURE
```

### Expected Behavior (CORRECT ✅)

```
Mobile App WebView should load:
https://sandbox-cpp.iyzipay.com?token=fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419&lang=tr

Result: iyzico payment form displayed
```

---

## Root Cause

The mobile app is incorrectly extracting or using the URL from the backend payment initialization response.

### Backend Response Structure

When mobile app calls `POST /api/v1/payments/initialize`, the backend returns:

```json
{
  "success": true,
  "message": "Payment initialized successfully",
  "data": {
    "transactionId": 12,
    "paymentToken": "fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419",
    "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419&lang=tr",
    "expiresAt": "2025-11-22T08:48:33Z"
  }
}
```

### What Each Field Means

| Field | Purpose | Who Uses It |
|-------|---------|-------------|
| `transactionId` | Backend transaction reference | Mobile app (for verify call) |
| `paymentToken` | iyzico payment token | Mobile app (included in paymentPageUrl) |
| `paymentPageUrl` | **iyzico payment form URL** | **Mobile app MUST load this in WebView** |
| `expiresAt` | Token expiration time | Mobile app (optional, for UI) |

---

## The Fix (Mobile Side)

### Current Code (WRONG ❌)

The mobile app is likely doing something like this:

```dart
// WRONG - Don't do this
final response = await paymentService.initializePayment(...);
final callbackUrl = response.data['callbackUrl']; // ❌ This field doesn't exist in response
webView.loadUrl(callbackUrl); // ❌ Loading wrong URL
```

OR:

```dart
// WRONG - Hardcoded URL
final baseUrl = 'https://ziraai-api-sit.up.railway.app';
final callbackUrl = '$baseUrl/payment/callback'; // ❌ Wrong URL
webView.loadUrl(callbackUrl); // ❌ Loading callback instead of payment page
```

### Correct Code (RIGHT ✅)

```dart
// CORRECT - Use paymentPageUrl from backend response
final response = await paymentService.initializePayment(
  tierId: tierId,
  quantity: quantity,
);

if (response.success) {
  final paymentPageUrl = response.data['paymentPageUrl']; // ✅ Correct field

  // Log for debugging
  print('💳 Loading payment page: $paymentPageUrl');

  // Load iyzico payment page in WebView
  webViewController.loadRequest(Uri.parse(paymentPageUrl)); // ✅ Correct URL
} else {
  // Handle error
  showError(response.message);
}
```

---

## Step-by-Step Fix Guide

### 1. Find Payment Initialization Response Handler

**Location:** Likely in one of these files:
- `lib/features/payment/services/payment_service.dart`
- `lib/features/payment/presentation/screens/payment_webview_screen.dart`
- `lib/features/sponsor/presentation/screens/sponsor_purchase_screen.dart`

**Look for:** Code that handles the response from `/api/v1/payments/initialize` endpoint

### 2. Verify Response Model

Check the payment response model:

```dart
class PaymentInitializationResponse {
  final int transactionId;
  final String paymentToken;
  final String paymentPageUrl; // ✅ This field must exist
  final DateTime expiresAt;

  PaymentInitializationResponse.fromJson(Map<String, dynamic> json)
    : transactionId = json['transactionId'],
      paymentToken = json['paymentToken'],
      paymentPageUrl = json['paymentPageUrl'], // ✅ Parse this field
      expiresAt = DateTime.parse(json['expiresAt']);
}
```

### 3. Update WebView URL Loading

**Before (WRONG):**
```dart
// If you see any of these patterns, they are WRONG:
webView.loadUrl(callbackUrl);
webView.loadUrl('$baseUrl/payment/callback');
webView.loadUrl(response.data['callbackUrl']);
```

**After (CORRECT):**
```dart
// Use paymentPageUrl from backend response
final paymentPageUrl = response.data['paymentPageUrl'];
webViewController.loadRequest(Uri.parse(paymentPageUrl));
```

### 4. Add Debugging Logs

Add these logs to verify the fix:

```dart
print('💳 Payment initialized successfully');
print('💳 Transaction ID: ${response.data['transactionId']}');
print('💳 Payment Token: ${response.data['paymentToken']}');
print('💳 Payment Page URL: ${response.data['paymentPageUrl']}');

// This should print:
// 💳 Payment Page URL: https://sandbox-cpp.iyzipay.com?token=xxx&lang=tr
// NOT: https://ziraai-api-sit.up.railway.app/payment/callback
```

---

## Testing Checklist

### 1. Before Fix - Verify Problem
- [ ] Run payment flow
- [ ] Check mobile logs for URL being loaded
- [ ] Should see: `https://ziraai-api-sit.up.railway.app/payment/callback` ❌
- [ ] Should get error: `net::ERR_HTTP_RESPONSE_CODE_FAILURE` ❌

### 2. After Fix - Verify Solution
- [ ] Apply mobile code changes
- [ ] Run payment flow again
- [ ] Check mobile logs for URL being loaded
- [ ] Should see: `https://sandbox-cpp.iyzipay.com?token=xxx&lang=tr` ✅
- [ ] iyzico payment form should load successfully ✅
- [ ] Should NOT see "Geçersiz token!" error ✅

### 3. Complete Payment Flow Test
- [ ] Select sponsor tier (e.g., S tier)
- [ ] Enter quantity (e.g., 50 codes)
- [ ] Click "Confirm Order"
- [ ] Verify payment page loads in WebView
- [ ] Fill test card details:
  - Card: `5528790000000008`
  - Expiry: `12/2030`
  - CVV: `123`
  - Name: `Test User`
- [ ] Complete 3D Secure with SMS code: `123456`
- [ ] Verify payment success callback
- [ ] Check backend logs for callback received
- [ ] Verify mobile app receives success status

---

## Backend Response Example (For Reference)

This is what the backend is currently returning (verified in logs):

```json
{
  "success": true,
  "message": "Payment initialized successfully",
  "data": {
    "transactionId": 12,
    "paymentToken": "fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419",
    "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419&lang=tr",
    "expiresAt": "2025-11-22T08:48:33.000Z"
  }
}
```

**Key Point:** Backend is sending the CORRECT data. Mobile app just needs to use `paymentPageUrl` field.

---

## Understanding the Payment Flow

### Complete iyzico Payment Flow

```
┌─────────────────┐
│  Mobile App     │
│  (User selects  │
│   sponsor tier) │
└────────┬────────┘
         │
         │ 1. POST /api/v1/payments/initialize
         │    { tierId: 1, quantity: 50 }
         ↓
┌─────────────────────────┐
│  Backend API            │
│  - Creates transaction  │
│  - Calls iyzico API     │
│  - Gets payment token   │
└────────┬────────────────┘
         │
         │ 2. Response with paymentPageUrl
         │    {
         │      "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=xxx"
         │    }
         ↓
┌─────────────────┐
│  Mobile App     │
│  WebView loads  │ ✅ USE THIS URL (paymentPageUrl)
│  iyzico page    │ ❌ NOT callback URL
└────────┬────────┘
         │
         │ 3. User fills card info
         │    (on iyzico page)
         ↓
┌─────────────────┐
│  iyzico Page    │
│  - Validates    │
│  - Processes 3DS│
│  - Completes    │
└────────┬────────┘
         │
         │ 4. POST to callback URL
         │    (iyzico → backend webhook)
         ↓
┌─────────────────────────┐
│  Backend Webhook        │
│  /payment/callback      │ ⚠️ This is for iyzico to call
│  - Receives result      │    NOT for mobile to load
│  - Updates transaction  │
│  - Redirects to app     │
└────────┬────────────────┘
         │
         │ 5. Deep link redirect
         │    ziraai://payment-callback?token=xxx
         ↓
┌─────────────────┐
│  Mobile App     │
│  - Receives     │
│  - Calls verify │
│  - Shows result │
└─────────────────┘
```

### Key URLs and Their Purposes

| URL | Purpose | Used By |
|-----|---------|---------|
| `https://sandbox-cpp.iyzipay.com?token=xxx` | Payment form page | **Mobile WebView** ← Load this! |
| `https://ziraai-api-sit.up.railway.app/payment/callback` | Webhook endpoint | **iyzico server** (POST callback) |
| `ziraai://payment-callback?token=xxx` | Deep link | **Mobile app** (after payment) |

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Using Callback URL in WebView
```dart
// WRONG - Don't load callback URL
final url = 'https://ziraai-api-sit.up.railway.app/payment/callback';
webView.loadUrl(url); // ❌ This is a webhook, not a payment page
```

### ❌ Mistake 2: Hardcoding Payment Page URL
```dart
// WRONG - Don't hardcode iyzico URL
final url = 'https://sandbox-cpp.iyzipay.com?token=${token}';
webView.loadUrl(url); // ❌ Token might be wrong, use backend response
```

### ❌ Mistake 3: Ignoring Backend Response
```dart
// WRONG - Don't ignore paymentPageUrl field
final response = await initializePayment();
// ❌ Not using response.data['paymentPageUrl']
```

### ✅ Correct Approach
```dart
// CORRECT - Use paymentPageUrl from backend
final response = await paymentService.initializePayment(tierId, quantity);
final url = response.data['paymentPageUrl']; // ✅ Backend provides correct URL
webViewController.loadRequest(Uri.parse(url)); // ✅ Load it
```

---

## Expected Log Output After Fix

### Mobile Logs (After Fix)
```
💳 Payment: Initializing sponsor payment...
💳 Payment: Tier ID: 1, Quantity: 50
💳 Payment: API call to /api/v1/payments/initialize
💳 Payment: Response received successfully
💳 Payment: Transaction ID: 12
💳 Payment: Payment Token: fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419
💳 Payment: Payment Page URL: https://sandbox-cpp.iyzipay.com?token=fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419&lang=tr
💳 WebView: Loading URL: https://sandbox-cpp.iyzipay.com?token=fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419&lang=tr
💳 WebView: Page started loading
💳 WebView: Page finished loading
💳 WebView: Payment form displayed successfully
```

### Backend Logs (Already Correct ✅)
```
[iyzico] Payment initialized successfully. TransactionId: 12, Token: fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419
[Payment] Payment initialized successfully. UserId: 189, TransactionId: 12
```

---

## Related Documentation

- [Callback URL Fix](./CALLBACK_URL_FIX.md) - Backend callback URL fix (already applied ✅)
- [Invalid Token Error](./INVALID_TOKEN_ERROR.md) - Previous error analysis (resolved ✅)
- Backend log: `claudedocs/application.log` - Shows correct backend response

---

## Summary for Mobile Team

### What's Wrong
Mobile app is loading `https://ziraai-api-sit.up.railway.app/payment/callback` in WebView, which is a backend webhook endpoint, not the payment page.

### What to Fix
Extract `paymentPageUrl` from backend API response and load that URL in WebView instead.

### Backend Response Field
```json
{
  "data": {
    "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=xxx&lang=tr"
  }
}
```

### Code Change
```dart
// Use this field ↑
final url = response.data['paymentPageUrl'];
webViewController.loadRequest(Uri.parse(url));
```

### Expected Result
iyzico payment form should load successfully in WebView, allowing user to enter card details and complete payment.

---

## Questions?

If you need clarification on:
- Backend API response structure
- Payment flow sequence
- URL purposes and differences
- Testing steps

Contact backend team or refer to `CALLBACK_URL_FIX.md` for the complete payment flow explanation.
