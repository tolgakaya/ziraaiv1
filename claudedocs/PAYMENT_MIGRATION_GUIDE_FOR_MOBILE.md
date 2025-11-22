# 🚨 CRITICAL: Payment Integration Migration Guide for Mobile Team

**Date:** 2025-11-22
**Priority:** HIGH
**Impact:** All payment flows must be updated

---

## ❌ What Was Wrong

The previous backend implementation had **fundamental errors** in iyzico integration:

### 1. Wrong HMAC Signature Format
```
❌ OLD (WRONG):
Authorization string format: "apiKey:randomKey:signature"
Example: "sandbox-ABC123:1732246069:XYZ789=="

✅ NEW (CORRECT):
Authorization string format: "apiKey:VALUE&randomKey:VALUE&signature:VALUE"
Example: "apiKey:sandbox-ABC123&randomKey:1732246069&signature:XYZ789=="
```

### 2. Missing Required Fields
Backend was not sending these required fields:
- `basketId`
- `buyer.gsmNumber`
- `buyer.registrationDate`
- `buyer.lastLoginDate`
- `buyer.zipCode`
- `shippingAddress.zipCode`
- `billingAddress.zipCode`
- `basketItems.category2`

---

## ✅ What Changed - Backend Fixes Applied

### Backend Changes (Already Deployed)

1. ✅ **HMAC signature format fixed** - Now uses correct `apiKey:VALUE&randomKey:VALUE&signature:VALUE` format
2. ✅ **All required fields added** - basketId, gsmNumber, zipCode, dates, etc.
3. ✅ **Price field types corrected** - Changed from string to numeric decimal (CRITICAL FIX)
4. ✅ **Better error handling** - Detailed error logging for debugging

### Status of Changes

| Component | Status | Notes |
|-----------|--------|-------|
| Initialize endpoint | ✅ Fixed | Deployed to Railway |
| HMAC signature | ✅ Fixed | Correct format implemented |
| Required fields | ✅ Fixed | All fields now included |
| Verify endpoint | ⚠️ Pending | Needs implementation |
| Response verification | ⚠️ Pending | Signature check needed |

---

## 📱 What Mobile Team Needs to Do

### NOTHING if you followed the API contract correctly!

**Good news:** If your mobile implementation was calling the backend API endpoints correctly, **you don't need to change anything** in your code. The fixes are all on the backend side.

### ✅ Your Current Implementation Should Work If:

1. You're calling `POST /api/v1/payments/initialize` with correct body:
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

2. You're opening the `paymentPageUrl` from response in WebView

3. You're listening for deep link callback: `ziraai://payment-callback?token=XXX`

### ⚠️ What You MUST Verify

**Test the complete flow:**

```
1. Call initialize endpoint
   ↓
2. Check response structure hasn't changed
   ↓
3. Open paymentPageUrl in WebView
   ↓
4. Complete payment in iyzico page
   ↓
5. Receive callback via deep link
   ↓
6. Call verify endpoint (when ready)
```

---

## 🔍 Expected Response Format

### Initialize Response (No Change)

```json
{
  "success": true,
  "data": {
    "transactionId": 123,
    "paymentToken": "c4b91f9e-8b7a-4c3d-9f2e-1a8b7c6d5e4f",
    "paymentPageUrl": "https://sandbox-merchant.iyzipay.com/checkoutform/auth/...",
    "callbackUrl": "ziraai://payment-callback?token=c4b91f9e...",
    "amount": 4999.50,
    "currency": "TRY",
    "expiresAt": "2025-11-22T08:00:00Z",
    "status": "Initialized",
    "conversationId": "SponsorBulkPurchase_134_638993824694005095"
  },
  "message": "Payment initialized successfully"
}
```

**No changes to response structure!** Your parsing code should work as-is.

---

## ⚠️ Known Issues & Upcoming Changes

### Current Status (as of 2025-11-22)

**Initialize Endpoint:**
- ✅ Fixed HMAC signature
- ✅ Added all required fields
- ⚠️ Still getting Error 11 from iyzico
- 🔄 Under investigation (might be additional validation issues)

**Verify Endpoint:**
- ❌ Not yet implemented
- 🔄 Will be implemented next
- 📋 Documented in complete guide

### What to Expect Next

1. **Initialize endpoint final fix** - Once Error 11 is resolved
2. **Verify endpoint implementation** - For post-payment verification
3. **Complete flow testing** - End-to-end with real payment

---

## 🧪 Testing Checklist for Mobile Team

### Phase 1: Initialize Payment (Test Now)

- [ ] Call initialize endpoint
- [ ] Verify response structure matches expected format
- [ ] Check all required fields are present in response
- [ ] Verify `paymentPageUrl` is valid URL
- [ ] Verify `callbackUrl` contains correct deep link

### Phase 2: WebView Payment (Test Now)

- [ ] Open `paymentPageUrl` in WebView
- [ ] WebView loads iyzico payment page
- [ ] Can enter card details
- [ ] Can submit payment
- [ ] Receives callback (even if payment fails)

### Phase 3: Callback Handling (Test Now)

- [ ] Deep link listener activated
- [ ] Receives `ziraai://payment-callback?token=XXX`
- [ ] Token extracted from URL
- [ ] Ready to call verify endpoint

### Phase 4: Verify Payment (Wait for Implementation)

- [ ] Verify endpoint implemented on backend
- [ ] Call verify endpoint with token
- [ ] Receive payment status
- [ ] Handle success/failure appropriately

---

## 🐛 Error Scenarios to Test

### Scenario 1: Initialize Fails
```json
{
  "success": false,
  "message": "Geçersiz tier ID",
  "data": null
}
```
**Expected:** Show error message to user

### Scenario 2: User Cancels Payment
```
Deep link callback receives token
→ Call verify endpoint
→ Response: status = "CANCELLED"
```
**Expected:** Return to previous screen with "Payment cancelled" message

### Scenario 3: Payment Fails
```
Deep link callback receives token
→ Call verify endpoint
→ Response: status = "FAILURE"
```
**Expected:** Show error, allow retry

### Scenario 4: Network Error
```
Initialize call fails with network error
```
**Expected:** Show network error, allow retry

---

## 📞 Communication

### When to Contact Backend Team

1. **Response structure changed** - If fields are missing or renamed
2. **New errors appear** - If you get errors you can't handle
3. **Deep link not working** - If callback URL format is wrong
4. **Verify endpoint ready** - When you're ready to integrate verification

### What Information to Provide

When reporting issues, please include:

1. Request body you sent
2. Response you received
3. Error messages from logs
4. Device/OS information
5. Network conditions (wifi/cellular)

---

## 📚 Reference Documents

- **Complete Implementation Guide:** `IYZICO_PAYMENT_INTEGRATION_COMPLETE_GUIDE.md`
- **API Documentation:** Previous docs (PAYMENT_API_DOCUMENTATION_FLUTTER.md)
- **Postman Collection:** `iyzico Collection.postman_collection.json`

---

## ✅ Quick Action Items

### For Mobile Team (NOW)

1. ✅ Read this document
2. ✅ Test initialize endpoint with latest backend
3. ✅ Verify WebView opens correctly
4. ✅ Verify deep link callback works
5. ⏳ Wait for verify endpoint implementation
6. ⏳ Test complete flow when verify is ready

### For Backend Team (IN PROGRESS)

1. ✅ Fixed HMAC signature format
2. ✅ Added all required fields
3. 🔄 Debug Error 11 from iyzico
4. ⏳ Implement verify endpoint
5. ⏳ Implement signature verification
6. ⏳ End-to-end testing

---

## 🎯 Bottom Line

**Mobile team:** Your code probably doesn't need changes if you followed the API contract. Just test the flow with the new backend deployment and report any issues.

**Backend team:** We fixed critical bugs but still have work to do (verify endpoint, Error 11 debugging).

**Timeline:**
- Initialize endpoint: Fixed, testing in progress
- Verify endpoint: Planned, not yet started
- Complete flow: TBD based on testing results

---

**Questions?** Ask in the team channel with `@backend` or `@mobile` tags.
