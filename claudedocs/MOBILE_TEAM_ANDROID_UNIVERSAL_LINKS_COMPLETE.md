# Mobile Team: Android Universal Links Implementation Status

**Date**: 2025-10-14
**Status**: ✅ READY - No Mobile Changes Required!
**Priority**: 🟢 INFORMATION ONLY

---

## ✅ Summary: Mobile App Already Ready!

**Good News**: Mobile app code already supports Android Universal Links correctly. **NO CHANGES NEEDED!**

Your current deep link handler in `lib/core/services/deep_link_service.dart` already supports:
- ✅ `https://ziraai-api-sit.up.railway.app/redeem/{CODE}` (Staging)
- ✅ `https://ziraai.com/redeem/{CODE}` (Production)
- ✅ `https://localhost:5001/redeem/{CODE}` (Development)
- ✅ `ziraai://redeem/{CODE}` (Custom scheme fallback)

---

## 📋 What Backend Changed (FYI Only)

### Old Flow (WRONG - Was Breaking)
```
SMS Link (HTTPS) → Backend GET /redeem/{code} → Redirect to ziraai://redemption-success/{code}
                                                          ↓
                                                 Custom scheme opens app
```

**Problem**: Android Universal Links don't work with redirect to custom scheme!

### New Flow (CORRECT - Now Fixed)
```
SMS Link (HTTPS) → Android intercepts HTTPS URL → App opens directly with HTTPS URL!
                         ↓ (if app not installed)
                   Backend returns HTML fallback page
```

**Key Change**: Backend NO longer redirects. Android handles HTTPS URL directly!

---

## 🎯 Expected Flow After Backend Deployment

```
📱 Farmer receives SMS:
   "🎁 Sponsorluk! Kod: AGRI-2025-ABC123
    Tıklayın: https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-ABC123"
        ↓
👆 Farmer taps HTTPS link
        ↓
🔍 Android OS checks assetlinks.json:
   → Package: com.ziraai.app.staging ✅
   → SHA256: E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06... ✅
   → Domain: ziraai-api-sit.up.railway.app ✅
        ↓
📱 ZiraAI app opens DIRECTLY (NO browser!)
        ↓
🎯 Your deep link handler receives:
   URL: "https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-ABC123"
        ↓
💡 Your existing code extracts: "AGRI-2025-ABC123"
   (Already works - no changes needed!)
        ↓
✅ Redemption screen opens with pre-filled code
        ↓
👆 User taps "Kullan" button
        ↓
📡 Your app calls POST /api/v1/redemption/redeem-code
   { "code": "AGRI-2025-ABC123" }
        ↓
🎉 Success! Subscription activated
```

---

## 🛠️ Your Current Code (Already Correct!)

```dart
/// Extract sponsorship code from deep link
static String? extractSponsorshipCode(String link) {
  final uri = Uri.parse(link);

  // Handle HTTPS links (All environments) ← THIS HANDLES IT!
  if (uri.scheme == 'https' || uri.scheme == 'http') {
    final isValidHost = uri.host.contains('ziraai') ||
                       uri.host.contains('localhost') ||
                       uri.host.contains('127.0.0.1');

    if (isValidHost && uri.pathSegments.isNotEmpty) {
      if (uri.pathSegments.first == 'redeem' && uri.pathSegments.length >= 2) {
        return uri.pathSegments[1];  // ✅ Returns CODE from HTTPS URL
      }
    }
  }

  // Custom scheme fallback (still supported)
  if (uri.scheme == 'ziraai') {
    if (uri.pathSegments.isNotEmpty) {
      if (uri.pathSegments.first == 'redeem' && uri.pathSegments.length >= 2) {
        return uri.pathSegments[1];
      }
    }
  }

  return null;
}
```

**Why This Works:**
- Your code checks for `https` scheme ✅
- Validates host contains `ziraai` ✅
- Extracts code from path `/redeem/{CODE}` ✅
- **Perfect for Android Universal Links!** ✅

---

## ⚙️ AndroidManifest Configuration Check

**IMPORTANT**: Verify your `AndroidManifest.xml` has correct intent filters:

### For Staging Build (`com.ziraai.app.staging`)

**File**: `android/app/src/staging/AndroidManifest.xml` (or main if using single manifest)

```xml
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />

    <!-- HTTPS URLs for Universal Links -->
    <data android:scheme="https" />
    <data android:host="ziraai-api-sit.up.railway.app" />
    <data android:pathPrefix="/redeem/" />
</intent-filter>

<!-- Custom scheme fallback -->
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />

    <data android:scheme="ziraai" />
    <data android:host="redemption-success" />
</intent-filter>
```

### For Production Build (`com.ziraai.app`)

```xml
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />

    <data android:scheme="https" />
    <data android:host="ziraai.com" />
    <data android:pathPrefix="/redeem/" />
</intent-filter>
```

**Key Points:**
- ✅ `android:autoVerify="true"` enables Android App Links
- ✅ `https` scheme for Universal Links
- ✅ Correct host for each environment
- ✅ `/redeem/` path prefix

---

## 🧪 Testing Instructions for Mobile Team

### 1. Pre-Test Checklist
- [ ] Uninstall ALL ZiraAI builds from test device
- [ ] Install ONLY staging build: `com.ziraai.app.staging`
- [ ] Ensure backend deployed to Railway with latest changes

### 2. Verify Backend Ready
```bash
# Check assetlinks.json is accessible
curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json

# Expected output:
# [{
#   "relation": ["delegate_permission/common.handle_all_urls"],
#   "target": {
#     "namespace": "android_app",
#     "package_name": "com.ziraai.app.staging",
#     "sha256_cert_fingerprints": [
#       "E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06:94:95:D4:C2:22:78:0E:A9:65:6E:17:0F:1F:AE:0B:67"
#     ]
#   }
# }, ...]
```

### 3. Test Deep Link Flow

**Step 1**: Send test SMS (use sponsor dashboard or manual ADB)
```bash
adb emu sms send 5551234567 "🎁 Test sponsorluğu! Kod: AGRI-2025-TEST123. Tıklayın: https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-TEST123"
```

**Step 2**: **WAIT 2-3 MINUTES** (Android domain verification takes time!)

**Step 3**: Tap the link in SMS

**Expected Result:**
- ✅ App opens IMMEDIATELY (NO browser!)
- ✅ Redemption screen shows
- ✅ Code pre-filled: `AGRI-2025-TEST123`
- ✅ User can tap "Kullan" to redeem

**NOT Expected:**
- ❌ Browser opens first
- ❌ Shows "Choose app" dialog
- ❌ Error page or 404

### 4. Verify Android System Recognized Domain

```bash
adb shell dumpsys package domain-preferred-apps | grep -A 10 ziraai

# Expected output should show:
# Package: com.ziraai.app.staging
#   Domain verification status:
#     ziraai-api-sit.up.railway.app: verified
```

### 5. Test Edge Cases

**Test A: Invalid Code Format**
```
Link: https://ziraai-api-sit.up.railway.app/redeem/INVALID-CODE
Expected: App opens, shows error "Invalid code format"
```

**Test B: App Not Installed**
```
1. Uninstall app
2. Tap link
Expected: Browser opens showing:
  - "📱 ZiraAI Uygulamasını Aç" button
  - "📥 Uygulamayı İndir" button (Play Store)
  - JavaScript tries custom scheme as fallback
```

**Test C: Multiple Taps (Code Reuse Prevention)**
```
1. Tap link → redeem successfully
2. Tap same link again
Expected: Shows "Code already used" error
```

---

## 🐛 Common Issues & Fixes

### Issue 1: Browser Still Opens
**Symptoms**: Link opens Chrome instead of app

**Causes & Solutions:**

1. **Android hasn't verified domain yet**
   - Solution: Wait 2-3 minutes after first install
   - Or: Clear app data and reinstall

2. **assetlinks.json not accessible**
   - Check: `curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json`
   - Expected: Valid JSON (not 404!)

3. **SHA256 mismatch**
   - Check: Your keystore SHA256 matches assetlinks.json
   - Verify: `keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android`

4. **AndroidManifest missing `autoVerify`**
   - Fix: Add `android:autoVerify="true"` to intent-filter
   - Rebuild app

### Issue 2: Wrong Package Name Dialog
**Symptoms**: Android shows "Open with:" dialog with multiple ZiraAI options

**Cause**: Multiple builds installed (dev, staging, production)

**Solution**: Uninstall unused builds
```bash
adb uninstall com.ziraai.app.dev
adb uninstall com.ziraai.app  # Keep only staging for testing
```

### Issue 3: Code Extraction Fails
**Symptoms**: App opens but code not pre-filled

**Debug Steps:**
1. Add debug log in `extractSponsorshipCode`:
   ```dart
   print('Deep link received: $link');
   final code = extractSponsorshipCode(link);
   print('Extracted code: $code');
   ```

2. Check logs for received URL format

3. Verify URL matches expected patterns:
   - ✅ `https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-ABC123`
   - ❌ NOT: `ziraai://redemption-success/AGRI-2025-ABC123` (old format)

---

## 📊 Testing Checklist

### Backend Verification
- [ ] assetlinks.json accessible at `/.well-known/assetlinks.json`
- [ ] Contains correct SHA256 fingerprint
- [ ] Contains correct package name: `com.ziraai.app.staging`
- [ ] GET `/redeem/{code}` returns HTML (not redirect)
- [ ] POST `/api/v1/redemption/redeem-code` works for redemption

### Mobile App Verification
- [ ] AndroidManifest has correct intent filters with `autoVerify="true"`
- [ ] Deep link handler supports HTTPS URLs
- [ ] Code extraction logic works for `/redeem/{CODE}` path
- [ ] Redemption screen opens with pre-filled code
- [ ] POST redemption endpoint called correctly

### End-to-End Testing
- [ ] Fresh install → wait 2-3 min → tap link → app opens directly
- [ ] Code pre-filled in redemption screen
- [ ] "Kullan" button triggers POST endpoint
- [ ] Success screen shows after redemption
- [ ] Subscription activated correctly
- [ ] Re-tapping used link shows error

### Edge Cases
- [ ] Invalid code format shows error
- [ ] Expired code shows error
- [ ] Already-used code shows error
- [ ] App not installed → fallback page works
- [ ] Browser manual tap → custom scheme button works

---

## 🎓 Technical Background (Optional Reading)

### Why Android Universal Links?

**Old Approach (Deep Links Only):**
```
SMS link → Browser opens → Asks "Open with?" → User selects app
```
- ❌ Bad UX (extra steps)
- ❌ User confusion
- ❌ Lower conversion

**New Approach (Universal Links):**
```
SMS link → App opens directly (0 clicks!)
```
- ✅ Seamless UX
- ✅ Higher conversion
- ✅ Professional experience

### How Android Verifies Domain

1. **App declares intent filters** with `autoVerify="true"` in AndroidManifest
2. **Android OS downloads** `https://domain/.well-known/assetlinks.json`
3. **Verifies package name** matches app
4. **Verifies SHA256 fingerprint** matches signing certificate
5. **Marks domain as verified** for this app
6. **Future HTTPS links** to this domain open app automatically

### Why It Takes 2-3 Minutes

Android performs verification:
- On first app install
- When assetlinks.json changes
- Periodically in background

This isn't instant - **be patient during testing!**

---

## 📞 Contact & Support

### If You Encounter Issues

**For Backend Problems** (assetlinks.json, API endpoints):
- Contact: Backend Team
- Check: Railway deployment logs

**For Mobile Problems** (app not opening, code extraction):
- Debug: Add logs to deep link handler
- Check: AndroidManifest intent filters
- Verify: Package name matches environment

**For QA Testing**:
- Follow testing checklist above
- Test on fresh device first
- Report specific error messages

---

## ✅ Final Confirmation

**Mobile Team Action Items:**
1. ✅ **No code changes needed** - Your implementation is already correct!
2. 🧪 **Test after backend deployment** - Verify end-to-end flow
3. 📊 **Report test results** - Update this doc with findings
4. 🔮 **Create production keystore** - For future production builds (not urgent)

**You're all set!** Backend has been fixed to work with your existing code. Just test after deployment! 🚀
