# Payment Integration Issue - Executive Summary

**Date:** 2025-11-22
**Priority:** 🔴 CRITICAL
**Impact:** Payment flow completely blocked - no payments can be completed
**Team Affected:** Backend (Mobile code is correct)

---

## Problem Statement

Users can fill all payment form fields in the iyzico WebView, but when they click "Ödemeyi Tamamla" (Complete Payment), the 3D Secure authentication page does not load. Instead, an error appears: "Webpage not available".

### User Experience
1. ✅ Select subscription tier and quantity
2. ✅ Payment WebView opens successfully
3. ✅ iyzico payment form loads correctly
4. ✅ User fills card number, expiry, CVV, name
5. ✅ User clicks "Ödemeyi Tamamla" button
6. ❌ **3D Secure page should load but doesn't**
7. ❌ Error: "Webpage not available" for callback URL
8. ❌ Payment cannot be completed

---

## Root Cause

Backend sends a **deep link** as the callback URL to iyzico:

```json
{
  "callbackUrl": "ziraai://payment-callback?token=fe7ab927-81a0-4bf9-ad4a-a9fc6ddbf419"
}
```

### Why This Fails

1. After user clicks Pay button, iyzico needs to:
   - Show 3D Secure authentication page
   - After 3D Secure completes, redirect to callback URL

2. iyzico tries to redirect the **browser** to: `ziraai://payment-callback`

3. Browsers (including WebView) **cannot redirect to custom URL schemes** from web pages due to security restrictions

4. Result: "Webpage not available" error, 3D Secure never loads

### Technical Evidence

From mobile logs at 12:17:37:
```
I/flutter: 💳 WebView: Page started - https://ziraai-api-sit.up.railway.app/payment/callback
I/flutter: 💳 WebView: Page finished - https://ziraai-api-sit.up.railway.app/payment/callback
I/flutter: 💳 WebView: Page content check - "Webpage not available..."
```

From iyzico documentation:
> "callbackUrl: The URL where iyzico will send the payment result via HTTP POST.
> **This must be an HTTPS URL accessible from the internet.**"

Custom URL schemes are NOT valid callback URLs.

---

## Solution

Backend must implement an **HTTPS callback endpoint** instead of using a deep link.

### Required Flow

```
User clicks Pay
    ↓
iyzico shows 3D Secure
    ↓
User enters SMS code (123456)
    ↓
iyzico sends POST to: https://ziraai-api-sit.up.railway.app/api/v1/payments/callback
    ↓
Backend receives callback
Backend verifies payment with iyzico
Backend updates transaction in database
    ↓
Backend returns HTTP 302 Redirect to: ziraai://payment-callback?token=xxx&status=success
    ↓
Mobile app opens via deep link
Mobile verifies payment
Mobile shows success screen
```

### Implementation Required

#### 1. Create Callback Endpoint

**File:** `WebAPI/Controllers/PaymentController.cs`

```csharp
[HttpPost("callback")]
[AllowAnonymous]  // iyzico calls this, not authenticated mobile app
public async Task<IActionResult> PaymentCallback([FromForm] IyzicoCallbackRequest request)
{
    _logger.LogInformation("[Payment] Callback received from iyzico. Token: {Token}, Status: {Status}",
        request.Token, request.Status);

    try
    {
        // 1. Verify payment with iyzico
        var paymentResult = await _paymentService.RetrievePaymentResult(request.Token);

        // 2. Update transaction in database
        await _paymentService.UpdateTransactionFromCallback(paymentResult);

        // 3. Redirect to mobile deep link
        var deepLinkUrl = $"ziraai://payment-callback?token={request.Token}&status={paymentResult.Status}";

        _logger.LogInformation("[Payment] Redirecting to mobile app: {DeepLink}", deepLinkUrl);

        return Redirect(deepLinkUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Payment] Callback processing failed for token: {Token}", request.Token);

        // Redirect to mobile with error
        var errorDeepLink = $"ziraai://payment-callback?token={request.Token}&status=failed&error={Uri.EscapeDataString(ex.Message)}";
        return Redirect(errorDeepLink);
    }
}

public class IyzicoCallbackRequest
{
    public string Token { get; set; }
    public string Status { get; set; }
    public string PaymentId { get; set; }
    public string ConversationId { get; set; }
}
```

#### 2. Update Payment Initialization

**File:** `Business/Services/PaymentService.cs`

```csharp
// BEFORE (WRONG)
var request = new CreateCheckoutFormInitializeRequest
{
    CallbackUrl = "ziraai://payment-callback"  // ❌ Browser cannot handle this
};

// AFTER (CORRECT)
var request = new CreateCheckoutFormInitializeRequest
{
    CallbackUrl = $"{_configuration["BaseUrl"]}/api/v1/payments/callback"  // ✅ HTTPS endpoint
};
```

#### 3. Environment Variable

Update configuration:

```bash
# Add to environment variables or appsettings.json
BaseUrl=https://ziraai-api-sit.up.railway.app

# Or specifically:
Iyzico__Callback__Url=https://ziraai-api-sit.up.railway.app/api/v1/payments/callback
```

---

## Testing After Fix

### 1. Test Callback Endpoint

```bash
curl -X POST https://ziraai-api-sit.up.railway.app/api/v1/payments/callback \
  -d "token=test-token-123" \
  -d "status=success"
```

Expected: HTTP 302 redirect to `ziraai://payment-callback?token=test-token-123&status=success`

### 2. Complete Payment Flow

1. Mobile: Start payment (select tier, quantity, confirm order)
2. WebView: Fill test card details
   - Card: `5528790000000008`
   - Expiry: `12/2030`
   - CVV: `123`
   - Name: `Test User`
3. Click "Ödemeyi Tamamla"
4. **✅ CHECK:** 3D Secure page should load (SMS code entry)
5. Enter SMS code: `123456` (sandbox test code)
6. **✅ CHECK:** iyzico sends POST to backend callback
7. **✅ CHECK:** Backend logs show "Callback received"
8. **✅ CHECK:** Backend redirects to deep link
9. **✅ CHECK:** Mobile app opens and shows success screen

### Expected Logs

**Backend:**
```
[Payment] Payment initialized. Token: abc123
[iyzico] Callback URL: https://ziraai-api-sit.up.railway.app/api/v1/payments/callback
[Payment] Callback received from iyzico. Token: abc123, Status: success
[Payment] Redirecting to mobile app: ziraai://payment-callback?token=abc123&status=success
```

**Mobile:**
```
💳 Payment: Initializing sponsor payment...
💳 WebView: Page started - https://sandbox-cpp.iyzipay.com/?token=abc123
💳 WebView: Page finished - iyzico payment form loaded
💳 WebView: Navigation request - https://ziraai-api-sit.up.railway.app/api/v1/payments/callback
💳 WebView: Navigation request - ziraai://payment-callback?token=abc123&status=success
💳 WebView: Deep link callback detected
✅ Payment: Verification successful
```

---

## Files to Modify

### Backend Changes Required

1. **`WebAPI/Controllers/PaymentController.cs`**
   - Add `PaymentCallback` POST endpoint
   - Handle iyzico callback
   - Redirect to deep link after processing

2. **`Business/Services/PaymentService.cs`**
   - Update `CallbackUrl` in iyzico request initialization
   - Add `RetrievePaymentResult(token)` method
   - Add `UpdateTransactionFromCallback(result)` method

3. **`appsettings.json` or Environment Variables**
   - Add `BaseUrl` configuration
   - Or add `Iyzico:Callback:Url` configuration

### Mobile Changes Required

**None.** Mobile code is already correct and handles deep links properly.

---

## Timeline Estimate

- **Backend implementation:** 1-2 hours
- **Testing and validation:** 30 minutes
- **Total:** 2-3 hours

---

## Additional Documentation

For complete technical details, see:
- [CALLBACK_URL_MUST_BE_HTTPS.md](./CALLBACK_URL_MUST_BE_HTTPS.md) - Full implementation guide
- [PAYMENT_WORKING_STATUS.md](./PAYMENT_WORKING_STATUS.md) - Current payment flow status
- [SSL_ERROR_RESOLUTION.md](./SSL_ERROR_RESOLUTION.md) - Previous SSL issue (resolved)

---

## Questions?

Contact mobile team for:
- Deep link configuration details
- Mobile app callback handling
- Testing coordination

For iyzico API details:
- Refer to iyzico sandbox documentation
- Test credentials and sandbox SMS codes
- Callback URL requirements
