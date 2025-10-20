# CRITICAL: Deep Link Issues and Solutions

**Date**: 2025-10-14
**Priority**: 🔴🔴🔴 CRITICAL - System Not Working
**Status**: Multiple critical issues blocking deep link redemption

---

## Issue 1: Browser Opens Instead of App 🔴

### Current Behavior
```
User taps SMS link → Browser opens → Shows "Code already used"
```

### Root Cause
**Backend missing `.well-known/assetlinks.json` file!**

Test result:
```bash
curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json
# Response: 404 Not Found
```

Without this file:
- Android **CANNOT verify** the domain
- Android **WILL NOT open the app** automatically
- Links **ALWAYS open in browser** first

### Solution

#### Backend Must Serve assetlinks.json

**File Location**: `/.well-known/assetlinks.json`

**File Content**:
```json
[{
  "relation": ["delegate_permission/common.handle_all_urls"],
  "target": {
    "namespace": "android_app",
    "package_name": "com.ziraai.app.staging",
    "sha256_cert_fingerprints": [
      "MOBILE_TEAM_WILL_PROVIDE_THIS_SHA256"
    ]
  }
}]
```

#### Mobile Team Must Provide SHA256

Run this command to get SHA256:
```bash
# For staging/debug keystore
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android | grep SHA256

# Output format:
# SHA256: 14:6D:E9:83:C5:73:06:50:D8:EE:B9:95:2F:34:FC:64:16:A0:83:42:E6:1D:BE:A8:8A:04:96:B2:3F:CF:44:E5
```

#### Backend Implementation (ASP.NET Core)

**Step 1: Create File Structure**
```
Backend/
├── .well-known/
│   └── assetlinks.json    ← Create this
├── Controllers/
└── Program.cs
```

**Step 2: Configure Static Files Middleware**
```csharp
// Program.cs or Startup.cs

// Add this BEFORE app.UseRouting() and app.UseEndpoints()
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), ".well-known")),
    RequestPath = "/.well-known",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/json",
    OnPrepareResponse = ctx =>
    {
        // Disable caching for this file (Android checks periodically)
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    }
});
```

**Step 3: Test**
```bash
curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json
# Expected: JSON content (NOT 404!)
```

#### Android Verification Process

After backend serves the file:
1. Android downloads `/.well-known/assetlinks.json`
2. Verifies package name matches
3. Verifies SHA256 fingerprint matches
4. **This takes 1-2 minutes on first install**
5. After verification, links open app automatically

---

## Issue 2: "Code Already Used" Error 🔴

### Current Behavior
```
User taps link → Browser opens → Backend shows "Code already used"
```

### Root Cause

Backend's `/redeem/{code}` endpoint is **DOING TOO MUCH**:

```csharp
[HttpGet("/redeem/{code}")]
public async Task<IActionResult> HandleDeepLink(string code)
{
    // ❌ WRONG: This endpoint is REDEEMING the code
    var redemption = await RedeemCodeAsync(code);  // <-- THIS USES THE CODE!

    // Then redirects with token
    return Redirect($"ziraai://redemption-success?token={token}");
}
```

**Problem**:
1. User taps link in SMS
2. Browser opens (because assetlinks.json missing)
3. Backend **immediately redeems the code** ❌
4. Backend redirects to `ziraai://...`
5. Android **might** open app (if verification happened)
6. App tries to redeem code again
7. Backend says: "Code already used" (because step 3 already used it!)

### Solution

**GET `/redeem/{code}` should ONLY redirect, NOT redeem!**

```csharp
[HttpGet("/redeem/{code}")]
[AllowAnonymous]
public IActionResult HandleDeepLink(string code)
{
    // ✅ CORRECT: Just validate format and redirect

    // 1. Validate code format (don't check if used/expired yet!)
    if (!IsValidCodeFormat(code))
    {
        return BadRequest("Invalid code format");
    }

    // 2. Redirect to mobile app with code
    // Mobile app will call POST /api/v1/sponsorships/redeem to actually redeem
    var redirectUrl = $"{_config.MobileAppScheme}redeem/{code}";

    return Redirect(redirectUrl);
}

// Separate endpoint for ACTUAL redemption (called by mobile app)
[HttpPost("/api/v1/sponsorships/redeem")]
[Authorize]
public async Task<IActionResult> RedeemCode([FromBody] RedeemRequest request)
{
    // ✅ CORRECT: This endpoint does the actual redemption

    // 1. Check if code exists
    // 2. Check if code is expired
    // 3. Check if code is already used
    // 4. Activate subscription
    // 5. Generate token
    // 6. Return success

    return Ok(new { token, subscription });
}
```

**Flow After Fix**:
```
User taps link → Browser (temporarily) → Backend redirects to ziraai://redeem/CODE
                                                ↓
                                       Android opens app with code
                                                ↓
                                       App calls POST /api/v1/sponsorships/redeem
                                                ↓
                                       Backend redeems code (only once!)
                                                ↓
                                       App shows success
```

---

## Issue 3: Multiple ZiraAI Apps Showing 🔴

### Current Behavior
```
User taps link → Android shows:
- ZiraAI (option 1)
- ZiraAI (option 2)
- ZiraAI (option 3)
```

### Root Cause

Multiple builds installed with **same deep link handling**:
- `com.ziraai.app` (production)
- `com.ziraai.app.staging` (staging)
- `com.ziraai.app.dev` (development)

All three are configured to handle:
- `https://ziraai-api-sit.up.railway.app/redeem/*`
- `ziraai://redeem/*`

### Solution

#### Option 1: Different Package Names (BEST)

**Already done in project!** ✅

Check `android/app/build.gradle`:
```gradle
productFlavors {
    dev {
        applicationIdSuffix ".dev"  // com.ziraai.app.dev
    }
    staging {
        applicationIdSuffix ".staging"  // com.ziraai.app.staging
    }
    production {
        // com.ziraai.app
    }
}
```

**Problem**: All builds might be handling same URLs!

#### Option 2: Different Hosts per Flavor

**android/app/src/staging/AndroidManifest.xml**:
```xml
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />

    <!-- ONLY staging URLs -->
    <data android:scheme="https" />
    <data android:host="ziraai-api-sit.up.railway.app" />
    <data android:pathPrefix="/redeem/" />

    <!-- Custom scheme -->
    <data android:scheme="ziraai" />
</intent-filter>
```

**android/app/src/production/AndroidManifest.xml**:
```xml
<intent-filter android:autoVerify="true">
    <!-- ONLY production URLs -->
    <data android:scheme="https" />
    <data android:host="ziraai.com" />
    <data android:pathPrefix="/redeem/" />

    <!-- Custom scheme -->
    <data android:scheme="ziraai" />
</intent-filter>
```

**android/app/src/dev/AndroidManifest.xml**:
```xml
<intent-filter android:autoVerify="true">
    <!-- ONLY localhost URLs -->
    <data android:scheme="https" />
    <data android:host="localhost" />
    <data android:host="10.0.2.2" />
    <data android:pathPrefix="/redeem/" />

    <!-- Custom scheme -->
    <data android:scheme="ziraai" />
</intent-filter>
```

#### Option 3: Uninstall Unused Builds (Quick Fix)

```bash
# Check installed apps
adb shell pm list packages | grep ziraai

# Example output:
# com.ziraai.app
# com.ziraai.app.staging
# com.ziraai.app.dev

# Uninstall the ones you're not using
adb uninstall com.ziraai.app
adb uninstall com.ziraai.app.dev

# Keep only one (e.g., staging)
```

---

## Current Mobile Deep Link Handler

**File**: `lib/core/services/deep_link_service.dart`

```dart
/// Extract sponsorship code from deep link
static String? extractSponsorshipCode(String link) {
  final uri = Uri.parse(link);

  // Handle HTTPS links
  if (uri.scheme == 'https' || uri.scheme == 'http') {
    if (uri.pathSegments.first == 'redeem' && uri.pathSegments.length >= 2) {
      return uri.pathSegments[1];  // Returns CODE
    }
  }

  // Handle custom scheme (ziraai://redeem/CODE)
  if (uri.scheme == 'ziraai') {
    if (uri.pathSegments.first == 'redeem' && uri.pathSegments.length >= 2) {
      return uri.pathSegments[1];  // Returns CODE
    }
  }

  return null;
}
```

**This is correct!** Mobile extracts CODE and calls redemption API separately.

---

## Action Items

### Backend Team (CRITICAL - Must Fix Immediately)

1. **Add `.well-known/assetlinks.json` file** 🔴
   - Create file structure
   - Configure static files middleware
   - Get SHA256 from mobile team
   - Test: `curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json`

2. **Fix GET `/redeem/{code}` endpoint** 🔴
   - Remove redemption logic
   - Only validate format and redirect
   - Actual redemption stays in POST `/api/v1/sponsorships/redeem`

3. **Fix redirect URL** 🔴
   - Change from `https://localhost:5001/...`
   - To `ziraai://redeem/{code}`

### Mobile Team

1. **Provide SHA256 fingerprint** 🔴
   ```bash
   keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android | grep SHA256
   ```

2. **Check AndroidManifest.xml** 🟡
   - Verify deep link intent filters
   - Consider flavor-specific hosts

3. **Uninstall duplicate builds** 🟢
   - Keep only staging build for testing
   - Remove dev and production from test device

---

## Testing After Fixes

### Test 1: assetlinks.json Available
```bash
curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json
# Expected: Valid JSON with package name and SHA256
# NOT: 404 Not Found
```

### Test 2: Deep Link Redirect
```bash
curl -I https://ziraai-api-sit.up.railway.app/redeem/TEST123
# Expected: 302 Found
# Location: ziraai://redeem/TEST123
# NOT: Shows "Code already used"
```

### Test 3: End-to-End Flow
1. Send SMS with link
2. Wait 2 minutes (Android verification)
3. Tap link
4. **Expected**: App opens directly (no browser!)
5. Code auto-filled in redemption screen
6. User taps "Redeem" button
7. Success screen shows

### Test 4: Only One App Shows
1. Tap link
2. **Expected**: Only ONE "ZiraAI" option
3. **NOT**: 3 different ZiraAI options

---

## Summary

| Issue | Status | Fix Owner | Priority | Fixed Date |
|-------|--------|-----------|----------|------------|
| assetlinks.json missing | ✅ FIXED | Backend | CRITICAL | 2025-10-14 |
| Code redeemed on GET | ✅ FIXED | Backend | CRITICAL | 2025-10-14 |
| Localhost redirect | ✅ FIXED | Backend | CRITICAL | 2025-10-14 |
| Multiple apps show | 🟡 Annoying | Mobile | Medium | Pending |

**✅ All 3 critical backend issues have been FIXED!**

### Fixes Applied

**Issue #1: assetlinks.json** ✅
- Added `.well-known/assetlinks.json` file in WebAPI project
- Configured static file middleware in Startup.cs to serve the file
- Added directory copy to Dockerfile and Dockerfile.staging
- File is now accessible at: `https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json`
- ⚠️ Mobile team needs to provide SHA256 fingerprints to complete Android verification

**Issue #2: Code Redeemed on GET** ✅
- GET `/redeem/{code}` simplified to ONLY redirect (no redemption logic)
- Tracks link click for analytics only
- Validates code format (AGRI-YYYY-XXXXXXXX)
- Redirects to environment-specific deep link: `ziraai://redemption-success/{code}`
- Added POST `/api/v1/redemption/redeem-code` for actual redemption (called by mobile app)
- Mobile app flow: tap link → app opens → user logs in → POST endpoint redeems code

**Issue #3: Localhost Redirect** ✅
- Added `Redemption:DeepLinkBaseUrl` configuration to appsettings
- Development: `ziraai://redemption-success`
- Staging: `ziraai://redemption-success`
- Production: `ziraai://redemption-success`
- No more hardcoded localhost URLs!

**Commits:**
- `d51a735` - Fixed DirectoryNotFoundException for .well-known
- `fcdfd5f` - Removed duplicate /redeem route
- `6cfe25d` - Made redemption URLs environment-aware
- `32ab945` - Simplified GET endpoint to only redirect

---

## Expected Behavior After All Fixes

```
📱 User receives SMS:
   "🎁 Sponsorluk Kodunuz: AGRI-2025-3852DE2A
    Tıklayın: https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-3852DE2A"
        ↓
👆 User taps link
        ↓
✅ ZiraAI app opens directly (NO browser!)
        ↓
✅ Redemption screen opens with code: AGRI-2025-3852DE2A
        ↓
👆 User taps "Kullan" button
        ↓
🎉 Success! Subscription activated
```

**Current behavior**: Browser opens, shows "Code already used", app doesn't open ❌
