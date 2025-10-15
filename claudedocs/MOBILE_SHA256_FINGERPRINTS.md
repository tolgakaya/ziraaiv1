# Mobile Team: SHA256 Fingerprints for Android App Links

**Date**: 2025-10-14
**Status**: ✅ READY FOR BACKEND INTEGRATION
**Priority**: 🔴 CRITICAL - Required for Deep Link Verification

---

## SHA256 Fingerprints

### Debug/Staging Keystore (`com.ziraai.app.staging`)

**SHA256 Fingerprint**:
```
E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06:94:95:D4:C2:22:78:0E:A9:65:6E:17:0F:1F:AE:0B:67
```

**Keystore Details**:
- **Location**: `C:\Users\Asus\.android\debug.keystore`
- **Alias**: `androiddebugkey`
- **Owner**: C=US, O=Android, CN=Android Debug
- **Valid**: Sep 13, 2025 - Sep 06, 2055 (30 years)
- **Algorithm**: SHA256withRSA (2048-bit RSA key)

**SHA1 Fingerprint** (for reference):
```
52:09:26:A9:6E:00:AC:0D:91:6C:C3:2A:B3:72:D8:B1:EA:28:13:05
```

---

## Backend Integration

### Update `.well-known/assetlinks.json`

Replace the placeholder SHA256 values with the actual fingerprint:

**File**: `/.well-known/assetlinks.json`

```json
[{
  "relation": ["delegate_permission/common.handle_all_urls"],
  "target": {
    "namespace": "android_app",
    "package_name": "com.ziraai.app.staging",
    "sha256_cert_fingerprints": [
      "E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06:94:95:D4:C2:22:78:0E:A9:65:6E:17:0F:1F:AE:0B:67"
    ]
  }
}]
```

### Multiple Package Support (If Needed)

If you need to support multiple builds (dev, staging, production) simultaneously:

```json
[
  {
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
      "namespace": "android_app",
      "package_name": "com.ziraai.app.dev",
      "sha256_cert_fingerprints": [
        "E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06:94:95:D4:C2:22:78:0E:A9:65:6E:17:0F:1F:AE:0B:67"
      ]
    }
  },
  {
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
      "namespace": "android_app",
      "package_name": "com.ziraai.app.staging",
      "sha256_cert_fingerprints": [
        "E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06:94:95:D4:C2:22:78:0E:A9:65:6E:17:0F:1F:AE:0B:67"
      ]
    }
  },
  {
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
      "namespace": "android_app",
      "package_name": "com.ziraai.app",
      "sha256_cert_fingerprints": [
        "PRODUCTION_SHA256_WILL_BE_PROVIDED_SEPARATELY"
      ]
    }
  }
]
```

---

## Verification Steps

### 1. Backend Deployment
After updating assetlinks.json, verify it's accessible:

```bash
curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json

# Expected: Valid JSON with correct SHA256 fingerprint
# NOT: 404 or placeholder values
```

### 2. Android Verification Period
Android needs 2-3 minutes to verify the domain after:
- App installation
- First time opening a deep link
- After backend updates assetlinks.json

**Important**: Be patient! Verification is NOT instant.

### 3. Test Deep Link Flow

**Step 1**: Send test SMS with deep link:
```bash
adb emu sms send 5551234567 "🎁 Test sponsorluğu! Kod: AGRI-2025-TEST123. Tıklayın: https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-TEST123"
```

**Step 2**: Wait 2-3 minutes for Android verification

**Step 3**: Tap the deep link in SMS

**Expected Behavior**:
- ✅ ZiraAI app opens directly (NO browser!)
- ✅ Redemption screen opens with code pre-filled: `AGRI-2025-TEST123`
- ✅ User can tap "Kullan" button to redeem

**NOT Expected**:
- ❌ Browser opens first
- ❌ Shows "Choose app" dialog with 3 ZiraAI options
- ❌ 404 error or connection refused

### 4. Verify in Android System

Check if Android verified the domain:

```bash
adb shell dumpsys package domain-preferred-apps

# Look for:
# com.ziraai.app.staging
#   Domain verification status: verified
#   ziraai-api-sit.up.railway.app: verified
```

---

## Production Keystore (TODO)

**Status**: 🟡 PENDING

For production builds (`com.ziraai.app`), we need to:

1. **Create production keystore** (if not exists):
   ```bash
   keytool -genkey -v -keystore ziraai-release.keystore -alias ziraai-release -keyalg RSA -keysize 2048 -validity 10000
   ```

2. **Extract SHA256 fingerprint**:
   ```bash
   keytool -list -v -keystore ziraai-release.keystore -alias ziraai-release
   ```

3. **Configure in `android/app/build.gradle`**:
   ```gradle
   signingConfigs {
       release {
           storeFile file('ziraai-release.keystore')
           storePassword 'SECURE_PASSWORD'
           keyAlias 'ziraai-release'
           keyPassword 'SECURE_PASSWORD'
       }
   }
   ```

4. **Provide production SHA256 to backend team**

**Note**: Production keystore should be stored securely and NEVER committed to git!

---

## Common Issues & Solutions

### Issue 1: App Still Opens Browser
**Cause**: Android hasn't verified domain yet
**Solution**: Wait 2-3 minutes, try again. Or clear app data and reinstall.

### Issue 2: "Code Already Used" Error
**Cause**: GET endpoint was redeeming code (backend fixed this)
**Solution**: Backend should only track click, not redeem. Code redeemed by POST endpoint only.

### Issue 3: Multiple ZiraAI Apps Showing
**Cause**: Multiple builds installed (dev, staging, production)
**Solution**: Uninstall unused builds, or implement flavor-specific AndroidManifest with different hosts.

### Issue 4: Deep Link Opens Wrong Screen
**Cause**: Mobile app deep link handler not receiving correct format
**Solution**: Verify backend redirects to `ziraai://redemption-success/{code}` format.

---

## Mobile App Deep Link Handler (Current Implementation)

**File**: `lib/core/services/deep_link_service.dart`

The mobile app already supports all environments and formats:

### Supported Formats
- ✅ `https://ziraai.com/redeem/{CODE}` (Production)
- ✅ `https://ziraai-api-sit.up.railway.app/redeem/{CODE}` (Staging)
- ✅ `https://localhost:5001/redeem/{CODE}` (Development)
- ✅ `ziraai://redeem/{CODE}` (Custom scheme)
- ✅ `ziraai://redemption-success/{CODE}` (Success callback)

### Code Extraction Logic
```dart
/// Extract sponsorship code from deep link
static String? extractSponsorshipCode(String link) {
  final uri = Uri.parse(link);

  // Handle HTTPS links (All environments)
  if (uri.scheme == 'https' || uri.scheme == 'http') {
    final isValidHost = uri.host.contains('ziraai') ||
                       uri.host.contains('localhost') ||
                       uri.host.contains('127.0.0.1');

    if (isValidHost && uri.pathSegments.isNotEmpty) {
      if (uri.pathSegments.first == 'redeem' && uri.pathSegments.length >= 2) {
        return uri.pathSegments[1];  // Returns CODE
      }
    }
  }

  // Handle custom scheme (ziraai://redeem/CODE)
  if (uri.scheme == 'ziraai') {
    if (uri.pathSegments.isNotEmpty) {
      if (uri.pathSegments.first == 'redeem' && uri.pathSegments.length >= 2) {
        return uri.pathSegments[1];  // Returns CODE
      }
    }
  }

  return null;
}
```

**Note**: Mobile app requires NO changes. Already supports all deep link formats.

---

## Testing Checklist

### Backend Team
- [ ] Update assetlinks.json with provided SHA256 fingerprint
- [ ] Deploy to staging: `https://ziraai-api-sit.up.railway.app`
- [ ] Verify file accessible: `curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json`
- [ ] Confirm GET endpoint only redirects, doesn't redeem
- [ ] Confirm POST endpoint handles actual redemption

### Mobile Team
- [ ] Uninstall all ZiraAI builds from test device
- [ ] Install only staging build: `com.ziraai.app.staging`
- [ ] Send test SMS with deep link
- [ ] Wait 2-3 minutes for Android verification
- [ ] Tap deep link and verify app opens directly
- [ ] Confirm redemption screen opens with pre-filled code
- [ ] Test actual code redemption
- [ ] Verify subscription activated successfully

### QA Team
- [ ] Test with fresh device (never installed ZiraAI before)
- [ ] Test with device that had app previously installed
- [ ] Test immediate tap vs tap after 2-3 minutes
- [ ] Test invalid codes (should show error)
- [ ] Test expired codes (should show error)
- [ ] Test already-used codes (should show error)
- [ ] Test successful redemption flow end-to-end

---

## Expected Complete Flow (After Fixes)

```
📱 Sponsor Company creates sponsorship link
        ↓
📧 Farmer receives SMS:
   "🎁 Chimera Tarım A.Ş. size Medium paketi hediye etti!
    Sponsorluk Kodunuz: AGRI-2025-3852DE2A
    🚀 Hemen kullanmak için tıklayın:
    https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-3852DE2A"
        ↓
👆 Farmer taps deep link
        ↓
🔍 Android checks: /.well-known/assetlinks.json
   → Verifies package name: com.ziraai.app.staging
   → Verifies SHA256: E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06...
   → ✅ Verification passed
        ↓
🌐 Backend GET /redeem/AGRI-2025-3852DE2A
   → Tracks click (analytics)
   → Validates code format
   → Redirects to: ziraai://redemption-success/AGRI-2025-3852DE2A
   → CODE NOT USED YET!
        ↓
📱 Android intercepts custom scheme
   → Opens ZiraAI app
        ↓
🎯 Mobile app deep link handler
   → Extracts code: AGRI-2025-3852DE2A
   → Navigates to redemption screen
   → Pre-fills code in input field
        ↓
👆 Farmer sees:
   "Sponsorluk Kodu: AGRI-2025-3852DE2A"
   [Kullan] button
        ↓
👆 Farmer taps "Kullan" button
        ↓
📡 Mobile app POST /api/v1/redemption/redeem-code
   { "code": "AGRI-2025-3852DE2A" }
        ↓
🔐 Backend validates code
   → Checks if code exists
   → Checks if expired
   → Checks if already used
   → Creates farmer account (if needed)
   → Activates subscription (Medium tier)
   → Generates JWT token
   → CODE USED HERE (only once!)
        ↓
✅ Backend returns success
   { "token": "eyJ...", "subscription": { "tier": "Medium", ... } }
        ↓
💾 Mobile app saves token
   → Stores in secure storage
   → Updates auth state
        ↓
🎉 Success screen shown
   "🎉 Tebrikler! Medium paketiniz aktif edildi!"
   → Navigates to dashboard
   → Shows active subscription
   → Farmer can now use plant analysis
```

---

## Summary

### ✅ What's Ready
- Debug/Staging SHA256 fingerprint extracted
- Mobile app deep link handler supports all formats
- Backend fixes deployed (assetlinks.json, redirect flow)
- Complete testing flow documented

### 🔴 Action Required (Backend Team)
1. Update assetlinks.json with provided SHA256 fingerprint
2. Deploy to staging
3. Verify file is accessible

### 🟡 Action Required (Mobile Team)
1. Test deep link flow after backend deployment
2. Report any issues found
3. Create production keystore for production builds

### 🟢 Future Work
- Production keystore creation and SHA256 extraction
- Flavor-specific AndroidManifest for different environments
- Enhanced analytics and fraud detection testing

---

## Contact

**Questions?**
- Backend Team: Implement assetlinks.json update
- Mobile Team: Test deep link flow after deployment
- QA Team: Execute testing checklist

**Status Updates**: Update this document with test results and any issues found.
