# Railway Environment Variable Override Issue

**Date:** 2025-11-22
**Problem:** Backend callback endpoint created but Railway still uses old URL

---

## Problem

Backend logs show callback URL is still using old path:
```json
"callbackUrl": "https://ziraai-api-sit.up.railway.app/payment/callback"
```

Expected (from appsettings.Staging.json):
```json
"callbackUrl": "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
```

## Root Cause

Railway environment variables **override** appsettings.json values.

When we updated `appsettings.Staging.json`:
```json
"Iyzico": {
  "Callback": {
    "FallbackUrl": "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
  }
}
```

Railway deployment ignored this because it has an environment variable:
```
Iyzico__Callback__FallbackUrl=https://ziraai-api-sit.up.railway.app/payment/callback
```

Environment variables have **HIGHER PRIORITY** than appsettings files.

---

## Solution

**Update Railway environment variable to match new endpoint:**

### Railway Dashboard Steps:

1. Go to Railway dashboard: https://railway.app
2. Select ZiraAI project
3. Select staging environment/service
4. Go to **Variables** tab
5. Find: `Iyzico__Callback__FallbackUrl`
6. Update value to: `https://ziraai-api-sit.up.railway.app/api/v1/payments/callback`
7. Save changes
8. Railway will auto-redeploy with new value

### Alternative: Railway CLI

```bash
# Login to Railway
railway login

# Select project
railway link

# Set environment variable
railway variables set Iyzico__Callback__FallbackUrl="https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"

# Redeploy (automatic after variable change)
```

---

## Verification After Fix

### 1. Check Deployment Logs

Look for the new callback URL in logs:
```
"callbackUrl":"https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
```

### 2. Test Payment Flow

1. Mobile: Start payment (select tier, confirm)
2. Fill test card: `5528790000000008`, `12/2030`, `123`
3. Click "Ödemeyi Tamamla"
4. **Expected:** 3D Secure page loads (not "Webpage not available")
5. Enter SMS code: `123456`
6. **Expected:** Backend callback called, mobile app opens

### 3. Check Backend Callback Logs

After clicking Pay button, logs should show:
```
[Payment] Callback received from iyzico. Token: xxx, Status: success
[Payment] Redirecting to mobile app: ziraai://payment-callback?token=xxx&status=success
```

---

## Why This Happened

1. **Initial setup:** Environment variables were set in Railway for secrets
2. **Code change:** We updated appsettings.Staging.json locally
3. **Git push:** Code deployed but environment variables unchanged
4. **ASP.NET Core:** Environment variables override appsettings.json

## Prevention

For future changes to configuration:
1. ✅ Update appsettings.json (for local/reference)
2. ✅ Update Railway environment variables (for deployment)
3. ✅ Verify logs show new values after deployment

---

## Configuration Priority Order (ASP.NET Core)

Highest to Lowest priority:
1. **Environment Variables** ← Railway sets these
2. Command-line arguments
3. appsettings.{Environment}.json
4. appsettings.json
5. User secrets (development only)

This is why Railway environment variable wins over appsettings.Staging.json.

---

## Summary

**Current State:**
- ✅ Callback endpoint exists: `/api/v1/payments/callback`
- ✅ Code committed and pushed
- ❌ Railway environment variable still points to old path

**Action Required:**
- Update Railway environment variable: `Iyzico__Callback__FallbackUrl`
- Change from: `/payment/callback`
- Change to: `/api/v1/payments/callback`

After this change, payment flow will work correctly.
