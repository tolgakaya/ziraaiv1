# Invalid Token Error - 2025-11-22

## Current Status: New Error After SSL Fix

### What Changed
1. ✅ **SSL errors are FIXED** - WebView can now load iyzico pages without SSL handshake failures
2. ✅ **User is authenticated** - Valid JWT token with userId 134
3. ❌ **NEW ERROR**: iyzico rejects payment token with "Geçersiz token!"

### Error Screenshot Analysis
The iyzico page shows:
```
"Geçersiz token! Lütfen ortak ödeme sayfası kullanımını kontrol ediniz."
(Invalid token! Please check your common payment page usage.)
```

### Error Logs
```
I/flutter: 💳 WebView: Page finished - https://sandbox-cpp.iyzipay.com/?token=92d8f7b1-2672-49ec-b629-4f07978464aa&lang=tr
I/flutter: 💳 WebView: Page content check - "Geçersiz token! Lütfen ortak ödeme sayfası kullanımını kontrol ediniz."
```

## Root Cause Analysis

### Possible Causes

1. **Backend Payment Token Generation Issue**
   - Backend may not be creating valid iyzico payment tokens
   - Missing required fields in `CheckoutFormInitialize.Create()` request
   - iyzico API keys might be invalid for sandbox environment

2. **Token Expiration**
   - Payment tokens have a short TTL (typically 5-10 minutes)
   - Token might be expiring before user reaches payment page

3. **Backend Configuration**
   - Incorrect iyzico API key/secret key for sandbox
   - Wrong callback URL configuration
   - Missing required buyer/basket data (we know this is an issue from previous analysis)

## What We Know Works

### Mobile Side ✅
- Payment initialization API call succeeds (HTTP 200)
- Backend returns `transactionId`, `paymentToken`, `paymentPageUrl`
- WebView loads the iyzico page successfully
- No SSL errors anymore
- Error detection via JavaScript injection works correctly

### Backend Side ❓
From previous analysis, we know the backend is missing:
- ✅ `buyer` object (required by iyzico)
- ✅ `billingAddress` object (required)
- ✅ `basketItems` array (required)

**This is likely why iyzico rejects the token** - the payment initialization request to iyzico is incomplete.

## Next Steps

### 1. Verify Payment Initialization Request (URGENT)
Need to check backend logs to see what's being sent to iyzico's `CheckoutFormInitialize.Create()`:

**Check Backend:**
```csharp
// In PaymentService.cs or similar
var request = new CreateCheckoutFormInitializeRequest
{
    // CHECK: Are these fields present?
    Buyer = ?,
    BillingAddress = ?,
    BasketItems = ?,
    // ... other fields
};

var response = CheckoutFormInitialize.Create(request, options);
// LOG: response.Status, response.ErrorCode, response.ErrorMessage
```

**Expected Backend Logs:**
```
[PaymentService] Initializing payment for user 134
[PaymentService] iyzico request: { buyer: {...}, billingAddress: {...}, basketItems: [...] }
[PaymentService] iyzico response: { status: "failure", errorCode: "11", errorMessage: "Geçersiz istek" }
```

### 2. Apply Backend Fix from PAYMENT_BACKEND_FIX_REQUIRED.md

The fix documentation already exists in the project:
- File: `claudedocs/PAYMENT_BACKEND_FIX_REQUIRED.md`
- Contains: Complete code examples for adding missing fields
- Status: **NOT YET APPLIED TO BACKEND**

**Required Backend Changes:**
```csharp
// Add to /api/v1/payments/initialize endpoint

Buyer = new Buyer
{
    Id = user.Id.ToString(),
    Name = user.FirstName ?? "User",
    Surname = user.LastName ?? user.Id.ToString(),
    GsmNumber = user.PhoneNumber ?? "+905350000000",
    Email = user.Email ?? "user@example.com",
    IdentityNumber = "11111111111",  // Test for sandbox
    RegistrationAddress = "Istanbul, Turkey",
    City = "Istanbul",
    Country = "Turkey",
    ZipCode = "34000"
},

BillingAddress = new Address
{
    ContactName = sponsorProfile?.CompanyName ?? user.FullName,
    City = "Istanbul",
    Country = "Turkey",
    Description = "Istanbul, Turkey",
    ZipCode = "34000"
},

BasketItems = new List<BasketItem>
{
    new BasketItem
    {
        Id = $"TIER_{subscriptionTierId}_SPONSOR_{quantity}",
        Name = $"{tierName} Tier Sponsorship - {quantity} Codes",
        Category1 = "Subscription",
        Category2 = "Sponsor",
        ItemType = BasketItemType.VIRTUAL.ToString(),
        Price = totalAmount.ToString("F2", CultureInfo.InvariantCulture)
    }
}
```

### 3. Verify iyzico API Configuration

**Check Backend Configuration:**
```csharp
// appsettings.json or environment variables
{
  "Iyzico": {
    "ApiKey": "sandbox-xxx",  // Should start with "sandbox-"
    "SecretKey": "sandbox-xxx",
    "BaseUrl": "https://sandbox-api.iyzipay.com"  // NOT production URL
  }
}
```

**Test iyzico Credentials:**
```csharp
// In PaymentService initialization
var options = new Options
{
    ApiKey = "your-sandbox-api-key",
    SecretKey = "your-sandbox-secret-key",
    BaseUrl = "https://sandbox-api.iyzipay.com"
};

// Test with a simple request
var locale = Locale.Retrieve(options);
// Should return success without error
```

## Verification Steps

After backend applies the fix:

1. **Clear App Data** (token may be cached)
   ```bash
   adb -s emulator-5554 shell pm clear com.ziraai.app.staging
   ```

2. **Restart Flutter App**
   ```bash
   flutter run -d emulator-5554 --flavor staging
   ```

3. **Login Again** (credentials cleared)

4. **Navigate to Sponsorship Purchase**
   - Dashboard → Sponsor button → Select tier → Enter quantity → Confirm order

5. **Check Logs for Success Pattern**
   ```
   💳 Payment: Initializing sponsor payment...
   💳 Payment: API Success - received paymentToken and paymentPageUrl
   💳 WebView: Page started - https://sandbox-cpp.iyzipay.com/...
   💳 WebView: Page finished - https://sandbox-cpp.iyzipay.com/...
   [NO "Geçersiz token" message]
   [Payment form should be visible and fillable]
   ```

6. **Test Payment Flow**
   - Fill card: 5528790000000008
   - Expiry: 12/2030
   - CVV: 123
   - Name: Test User
   - Complete 3D Secure: SMS code 123456
   - Check for success callback or error

## Summary

| Component | Status | Action Required |
|-----------|--------|-----------------|
| **SSL Connection** | ✅ Fixed | None - emulator cache cleared |
| **Mobile Code** | ✅ Working | None - code is correct |
| **User Authentication** | ✅ Working | None - valid JWT token |
| **iyzico Token** | ❌ Invalid | **Backend fix required** |
| **Backend Payment Init** | ❌ Incomplete | **Add buyer/billing/basket data** |

## Critical Path

```
User → Mobile initiates payment → Backend creates iyzico request
                                    ↓
                              [MISSING buyer/billing/basket]
                                    ↓
                              iyzico rejects request
                                    ↓
                              Returns invalid token
                                    ↓
                              Mobile shows "Geçersiz token" error
```

**Fix:** Add missing fields to backend's iyzico request (see PAYMENT_BACKEND_FIX_REQUIRED.md)

## Files Involved

- **Backend** (NEEDS FIX):
  - `Business/Services/PaymentService.cs` - Add buyer/billing/basket data
  - `appsettings.json` - Verify iyzico sandbox credentials

- **Mobile** (WORKING):
  - `lib/features/payment/services/payment_service.dart` - API calls working ✅
  - `lib/features/payment/presentation/screens/payment_webview_screen.dart` - Error detection working ✅

- **Documentation**:
  - `claudedocs/PAYMENT_BACKEND_FIX_REQUIRED.md` - Complete fix guide
  - `claudedocs/SSL_ERROR_RESOLUTION.md` - Previous SSL issue (resolved)
  - `claudedocs/INVALID_TOKEN_ERROR.md` - This document

## Backend Developer Action Items

- [ ] Review `PAYMENT_BACKEND_FIX_REQUIRED.md`
- [ ] Add `Buyer` object to payment initialization
- [ ] Add `BillingAddress` object to payment initialization
- [ ] Add `BasketItems` array to payment initialization
- [ ] Verify iyzico sandbox API credentials
- [ ] Test payment initialization and check iyzico response
- [ ] Deploy to staging environment
- [ ] Notify mobile team for testing
