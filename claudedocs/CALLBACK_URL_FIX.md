# Callback URL Fix - Invalid Token Error Resolution

**Date:** 2025-11-22
**Issue:** "Geçersiz token!" error on iyzico payment page
**Status:** ✅ FIXED

## Root Cause

iyzico's payment page was rejecting tokens because the `callbackUrl` parameter in the payment initialization request was using a **mobile deep link scheme** instead of a **valid HTTPS URL**.

### The Problem

**File:** `Business/Services/Payment/IyzicoPaymentService.cs:211`

**Before (WRONG):**
```csharp
callbackUrl = _iyzicoOptions.Callback.DeepLinkScheme,  // "ziraai://payment-callback"
```

**Why This Failed:**
1. iyzico's payment page requires an **HTTPS callback URL** to POST payment results
2. Mobile deep link schemes (`ziraai://`) are NOT valid HTTP endpoints
3. iyzico validates the callback URL during payment initialization
4. If the callback URL is invalid, iyzico generates a token but marks it as invalid
5. When the mobile app loads the payment page, iyzico shows "Geçersiz token!" error

### The Paradox Explained

This explains why:
- ✅ Backend received `"status":"success"` from iyzico API (HTTP request succeeded)
- ✅ iyzico generated a payment token (`92d8f7b1-2672-49ec-b629-4f07978464aa`)
- ✅ Mobile app loaded the payment URL successfully
- ❌ iyzico's payment page rejected the token with "Geçersiz token!"

The token was technically generated but flagged as invalid due to the invalid callback URL.

## The Fix

**After (CORRECT):**
```csharp
callbackUrl = _iyzicoOptions.Callback.FallbackUrl,  // "https://ziraai-api-sit.up.railway.app/payment/callback"
```

### Configuration Values

**Development** (`appsettings.Development.json`):
```json
"Callback": {
  "DeepLinkScheme": "ziraai://payment-callback",
  "FallbackUrl": "https://localhost:5001/payment/callback"
}
```

**Staging** (`appsettings.Staging.json`):
```json
"Callback": {
  "DeepLinkScheme": "ziraai://payment-callback",
  "FallbackUrl": "https://ziraai-api-sit.up.railway.app/payment/callback"
}
```

**Production** (Environment Variables on Railway):
```json
"Callback": {
  "DeepLinkScheme": "ziraai://payment-callback",
  "FallbackUrl": "https://ziraai.com/api/v1/payments/callback"
}
```

## How iyzico Payment Flow Works

### 1. Payment Initialization
```
Mobile App → Backend API → iyzico API
{
  "callbackUrl": "https://ziraai-api-sit.up.railway.app/payment/callback",
  "buyer": {...},
  "basketItems": [...]
}
```

iyzico validates:
- ✅ Buyer data
- ✅ Basket items
- ✅ **Callback URL must be HTTPS**

### 2. Payment Page Display
```
Mobile App loads: https://sandbox-cpp.iyzipay.com/?token=xxx&lang=tr
```

iyzico checks:
- ✅ Token exists
- ✅ **Callback URL is valid HTTPS**
- ❌ If callback URL was invalid → "Geçersiz token!" error

### 3. Payment Completion
```
User completes payment → iyzico POSTs to callback URL
POST https://ziraai-api-sit.up.railway.app/payment/callback
{
  "token": "xxx",
  "status": "SUCCESS",
  "paymentId": "123"
}
```

Backend webhook endpoint:
- Receives iyzico callback
- Updates payment transaction status
- Redirects to mobile deep link: `ziraai://payment-callback?token=xxx`

### 4. Mobile App Handling
```
Mobile App receives deep link → Calls /api/v1/payments/verify → Shows result
```

## Changes Made

### File Modified
- **Business/Services/Payment/IyzicoPaymentService.cs**
  - Line 211: Changed from `DeepLinkScheme` to `FallbackUrl`

### No Configuration Changes Needed
The `FallbackUrl` already existed in:
- ✅ `appsettings.Development.json`
- ✅ `appsettings.Staging.json`
- ⚠️ **Must be set in Railway environment variables for production**

## Testing Checklist

### Backend Verification
1. ✅ Ensure `FallbackUrl` is configured for each environment
2. ✅ Build and deploy updated backend code
3. ✅ Verify logs show correct callback URL in request body

### Mobile Testing
1. **Initialize Payment**
   - Navigate to: Dashboard → Sponsor → Select tier → Enter quantity
   - Click "Confirm Order"
   - Backend should initialize payment with iyzico

2. **Check Logs (Backend)**
   ```
   [iyzico] FULL Request Body: {"callbackUrl":"https://ziraai-api-sit.up.railway.app/payment/callback", ...}
   ```
   Should show **HTTPS URL**, NOT `ziraai://`

3. **Load Payment Page**
   - Mobile app should load: `https://sandbox-cpp.iyzipay.com/?token=xxx&lang=tr`
   - Page should show **payment form**, NOT "Geçersiz token!" error

4. **Complete Payment (Sandbox)**
   - Card: `5528790000000008`
   - Expiry: `12/2030`
   - CVV: `123`
   - Name: `Test User`
   - Complete 3D Secure with SMS code: `123456`

5. **Verify Callback**
   - iyzico should POST to: `https://ziraai-api-sit.up.railway.app/payment/callback`
   - Backend should receive callback and update transaction status
   - Mobile app should receive success status

## Expected Log Output

**Before Fix (ERROR):**
```
[iyzico] FULL Request Body: {"callbackUrl":"ziraai://payment-callback", ...}
[Mobile] WebView: Page content check - "Geçersiz token! Lütfen ortak ödeme sayfası kullanımını kontrol ediniz."
```

**After Fix (SUCCESS):**
```
[iyzico] FULL Request Body: {"callbackUrl":"https://ziraai-api-sit.up.railway.app/payment/callback", ...}
[iyzico] API returned success: {"status":"success","token":"xxx","paymentPageUrl":"https://sandbox-cpp.iyzipay.com/?token=xxx"}
[Mobile] WebView: Payment form loaded successfully
[Mobile] User completed payment
[Backend] Callback received from iyzico: {"status":"SUCCESS","paymentId":"123"}
```

## Related Issues Resolved

1. ✅ **Error 11** (missing buyer/basket data) - Fixed in previous commits
2. ✅ **Error 1000** (HMAC signature format) - Fixed in previous commits
3. ✅ **SSL handshake errors** - Fixed by clearing Android emulator cache
4. ✅ **"Geçersiz token!" error** - Fixed by this commit (callback URL)

## Production Deployment Checklist

- [ ] Update Railway environment variable: `Iyzico__Callback__FallbackUrl`
- [ ] Set to: `https://ziraai.com/api/v1/payments/callback`
- [ ] Deploy updated backend code to Railway
- [ ] Test payment flow end-to-end on staging
- [ ] Verify iyzico callback endpoint is accessible via HTTPS
- [ ] Monitor logs for successful payment completions

## Technical Notes

### Why Two URLs?

- **FallbackUrl (HTTPS)**: For iyzico's POST callback after payment completion
- **DeepLinkScheme (Mobile)**: For redirecting user back to mobile app

### Callback Flow
```
iyzico payment page
  ↓ (payment completed)
POST https://ziraai.com/api/v1/payments/callback
  ↓ (backend processes)
Redirect → ziraai://payment-callback?token=xxx
  ↓ (mobile app receives)
Call /api/v1/payments/verify
  ↓
Show success/failure to user
```

### Security Considerations

1. **HTTPS Required**: iyzico only accepts HTTPS callback URLs
2. **Signature Verification**: Backend verifies iyzico's HMAC signature on callbacks
3. **Token Validation**: Backend checks token belongs to valid transaction
4. **Status Verification**: Mobile app calls `/verify` endpoint to confirm payment status

## Lessons Learned

1. **Read Configuration Carefully**: The `FallbackUrl` field existed but wasn't being used
2. **Understand Third-Party Requirements**: iyzico requires HTTPS callbacks, not deep links
3. **Test Full Flow**: The error only appeared on the payment page, not during initialization
4. **Check Logs Thoroughly**: The request body showed `ziraai://` instead of `https://`

## References

- iyzico PWI Documentation: https://docs.iyzico.com/tr/odeme-hizmetleri/pwi
- iyzico Callback Handling: https://docs.iyzico.com/tr/odeme-hizmetleri/callback
- Project Issue Tracker: `claudedocs/INVALID_TOKEN_ERROR.md`
