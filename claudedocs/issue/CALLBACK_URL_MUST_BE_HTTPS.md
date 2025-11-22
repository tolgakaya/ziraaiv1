# iyzico Callback URL Must Be HTTPS (Not Deep Link)

**Date:** 2025-11-22
**Issue:** 3D Secure redirect failing because callback URL is a deep link
**Status:** ⚠️ BACKEND FIX REQUIRED

---

## Problem

After user fills card details and clicks "Pay", iyzico should:
1. Show 3D Secure authentication page
2. After 3D completion, redirect to backend callback URL
3. Backend processes payment result
4. Backend redirects to mobile deep link

**Current behavior (WRONG ❌):**
- iyzico tries to redirect to: `ziraai://payment-callback?token=xxx`
- Browser cannot handle deep link scheme in redirect
- WebView shows "Webpage not available" error
- 3D Secure never happens

## Root Cause

Backend is sending **deep link** as callback URL to iyzico:

```csharp
// WRONG - Current backend code
CallbackUrl = "ziraai://payment-callback?token={paymentToken}"
```

iyzico redirects browser to this URL after 3D Secure. But browsers cannot redirect to custom URL schemes directly from web pages (security restriction).

## Solution

Backend must send **HTTPS URL** as callback to iyzico, not deep link.

### Step 1: Create Callback Endpoint (Backend)

```csharp
// Add new endpoint: PaymentController.cs

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

### Step 2: Update Payment Initialization (Backend)

```csharp
// Change callback URL to HTTPS endpoint
var request = new CreateCheckoutFormInitializeRequest
{
    // ... other fields ...

    // CORRECT - Use HTTPS callback URL
    CallbackUrl = $"{_configuration["BaseUrl"]}/api/v1/payments/callback",

    // NOT this:
    // CallbackUrl = "ziraai://payment-callback"  // ❌ WRONG
};
```

### Step 3: Environment Variable

Update `.env` or configuration:

```bash
# Current (WRONG)
Iyzico__Callback__DeepLinkScheme="ziraai://payment-callback"

# Should be (CORRECT)
Iyzico__Callback__Url="https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
```

Then use in code:
```csharp
CallbackUrl = _configuration["Iyzico:Callback:Url"]
```

---

## Complete Flow (Correct)

```
┌─────────────┐
│ Mobile App  │ 1. User fills card info
│  (WebView)  │    and clicks "Pay"
└──────┬──────┘
       │
       │ 2. iyzico shows 3D Secure
       ↓
┌──────────────┐
│   iyzico     │ 3. User enters SMS code
│  3D Secure   │    (123456 in sandbox)
└──────┬───────┘
       │
       │ 4. POST to callback URL (HTTPS)
       │    https://ziraai-api-sit.up.railway.app/api/v1/payments/callback
       ↓
┌──────────────────┐
│  Backend         │ 5. Process payment result
│  /payments/      │    Update database
│  callback        │
└──────┬───────────┘
       │
       │ 6. Redirect to deep link
       │    ziraai://payment-callback?token=xxx&status=success
       ↓
┌─────────────┐
│ Mobile App  │ 7. Deep link opens app
│             │    Verify payment
│             │    Show success screen
└─────────────┘
```

---

## Why HTTPS Callback is Required

### Technical Reason
Browsers (including WebView) **cannot directly redirect** to custom URL schemes (`ziraai://`) from web pages due to security restrictions.

The redirect chain must be:
```
iyzico → HTTPS backend → Deep link
```

NOT:
```
iyzico → Deep link ❌ (Browser blocks this)
```

### iyzico Documentation
From iyzico API docs:
> "callbackUrl: The URL where iyzico will send the payment result via HTTP POST.
> **This must be an HTTPS URL accessible from the internet.**"

Custom URL schemes are NOT valid callback URLs.

---

## Testing After Fix

### 1. Backend Callback Endpoint Test

Test the endpoint manually:
```bash
curl -X POST https://ziraai-api-sit.up.railway.app/api/v1/payments/callback \
  -d "token=test-token-123" \
  -d "status=success"
```

Expected response: HTTP 302 redirect to `ziraai://payment-callback?token=test-token-123&status=success`

### 2. Complete Payment Flow Test

1. Mobile: Start payment (tier selection, confirm order)
2. iyzico: Fill card: `5528790000000008`, expiry: `12/2030`, CVV: `123`
3. iyzico: Click "Ödemeyi Tamamla"
4. **CHECK:** 3D Secure page should load (SMS code entry)
5. iyzico: Enter SMS code: `123456`
6. **CHECK:** Page redirects to backend callback
7. **CHECK:** Backend logs show "Callback received"
8. **CHECK:** Backend redirects to deep link
9. **CHECK:** Mobile app opens and shows success

### 3. Expected Logs

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

## Alternative: Use iyzico's payWithIyzicoPageUrl

If you want to avoid server callback complexity, you can use iyzico's PWI (Pay With Iyzico) flow which handles redirects better with deep links. But the callback URL approach is more standard and reliable.

---

## Files to Modify

### Backend
1. **`WebAPI/Controllers/PaymentController.cs`**
   - Add `PaymentCallback` endpoint
   - Handle iyzico POST callback
   - Redirect to deep link after processing

2. **`Business/Services/PaymentService.cs`**
   - Update `CallbackUrl` in iyzico request
   - Add `RetrievePaymentResult(token)` method
   - Add `UpdateTransactionFromCallback(result)` method

3. **`appsettings.json` or Environment Variables**
   - Update `Iyzico:Callback:Url` to HTTPS endpoint

### Mobile (No changes needed)
Mobile code is already correct - it's listening for `ziraai://payment-callback` deep links.

---

## Summary

**Current (WRONG):**
```
CallbackUrl = "ziraai://payment-callback"
→ Browser cannot redirect to custom scheme
→ 3D Secure redirect fails
```

**Fixed (CORRECT):**
```
CallbackUrl = "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
→ iyzico sends POST to backend
→ Backend processes result
→ Backend redirects to "ziraai://payment-callback"
→ Mobile app opens and completes flow
```

---

## Priority

**🔴 CRITICAL** - Payment flow completely broken without this fix. No payments can be completed.

**Action:** Backend team must implement callback endpoint and update callback URL ASAP.
