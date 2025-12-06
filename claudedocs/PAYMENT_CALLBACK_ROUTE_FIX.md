# Payment Callback Route Fix

**Date:** 2025-11-22
**Issue:** 404 error on payment callback redirect
**Status:** Backend configuration needs update

## Problem

After successful payment with iyzico, the backend redirects to:
```
https://ziraai-web-staging.up.railway.app/sponsor/payment-callback?token=xxx&status=success
```

But this returns **404 Not Found** because the frontend route is:
```
/payment-callback
```

NOT `/sponsor/payment-callback`

## Root Cause

The backend `PaymentController.cs` helper methods (`GetSuccessRedirectUrl` and `GetErrorRedirectUrl`) are constructing the redirect URL incorrectly.

**Current (incorrect) behavior:**
- Base URL: `https://ziraai-web-staging.up.railway.app`
- Appended path: `/sponsor/payment-callback`
- Result: 404 error

**Expected (correct) behavior:**
- Base URL: `https://ziraai-web-staging.up.railway.app`
- Appended path: `/payment-callback`
- Result: Successful redirect to callback page

## Frontend Route Structure

From [src/routes/SponsorRoutes.jsx](../src/routes/SponsorRoutes.jsx):

```javascript
// Root path: /
const SponsorRoutes = {
  path: '/',
  children: [
    // Protected routes
    {
      path: '/',
      element: <ProtectedRoute><DashboardLayout /></ProtectedRoute>,
      children: [
        { path: 'dashboard', element: <SponsorDashboard /> },      // /dashboard
        { path: 'codes', element: <CodeList /> },                  // /codes
        { path: 'payment-callback', element: <PaymentCallbackPage /> }, // /payment-callback
        // ... other routes
      ]
    }
  ]
}
```

**All routes are at root level:**
- `/dashboard`
- `/codes`
- `/payment-callback` ✅ Correct path
- `/admin/users` (admin routes have `/admin` prefix)

**There is NO `/sponsor` base path in the application.**

## Backend Fix Required

Update `PaymentController.cs` helper methods to use correct path:

**File:** `WebAPI/Controllers/PaymentController.cs`

**Method:** `GetSuccessRedirectUrl` (around line 319-328)

```csharp
private string GetSuccessRedirectUrl(string platform, string token)
{
    if (platform == PaymentPlatform.Web)
    {
        var webAppUrl = _configuration["WebAppUrl"];
        // WRONG: return $"{webAppUrl}/sponsor/payment-callback?token={token}&status=success";
        // CORRECT:
        return $"{webAppUrl}/payment-callback?token={token}&status=success";
    }

    // Mobile deep link (correct, no change needed)
    return $"ziraai://payment-callback?token={token}&status=success";
}
```

**Method:** `GetErrorRedirectUrl` (around line 337-347)

```csharp
private string GetErrorRedirectUrl(string platform, string token, string errorMessage)
{
    var encodedError = Uri.EscapeDataString(errorMessage);

    if (platform == PaymentPlatform.Web)
    {
        var webAppUrl = _configuration["WebAppUrl"];
        // WRONG: return $"{webAppUrl}/sponsor/payment-callback?token={token}&status=failed&error={encodedError}";
        // CORRECT:
        return $"{webAppUrl}/payment-callback?token={token}&status=failed&error={encodedError}";
    }

    // Mobile deep link (correct, no change needed)
    return $"ziraai://payment-callback?token={token}&status=failed&error={encodedError}";
}
```

## Changes Summary

**Remove:** `/sponsor` prefix from web redirect URLs
**Keep:** Everything else the same (mobile deep links are correct)

**Before:**
- Success: `{WebAppUrl}/sponsor/payment-callback?token={token}&status=success`
- Error: `{WebAppUrl}/sponsor/payment-callback?token={token}&status=failed&error={message}`

**After:**
- Success: `{WebAppUrl}/payment-callback?token={token}&status=success` ✅
- Error: `{WebAppUrl}/payment-callback?token={token}&status=failed&error={message}` ✅

## Testing After Fix

1. **Initialize payment** from web frontend
   - Frontend sends `platform: "Web"`
   - Backend creates transaction with platform

2. **Complete payment** on iyzico page
   - User enters card details
   - iyzico calls backend callback endpoint

3. **Backend redirect** (FIXED)
   - Backend reads platform from transaction
   - Constructs correct URL: `https://ziraai-web-staging.up.railway.app/payment-callback?token=xxx&status=success`
   - Browser redirects to callback page ✅

4. **Frontend callback page** displays result
   - Shows success message
   - Verifies payment with backend
   - Redirects to purchases page after 3 seconds

## Related Files

- **Frontend Route:** [src/routes/SponsorRoutes.jsx:178](../src/routes/SponsorRoutes.jsx#L178)
- **Frontend Component:** [src/views-sponsor/payments/PaymentCallbackPage.jsx](../src/views-sponsor/payments/PaymentCallbackPage.jsx)
- **Backend Controller:** `WebAPI/Controllers/PaymentController.cs` (lines 319-347)
- **Implementation Summary:** [WEB_PAYMENT_PLATFORM_IMPLEMENTATION_SUMMARY.md](./WEB_PAYMENT_PLATFORM_IMPLEMENTATION_SUMMARY.md)
