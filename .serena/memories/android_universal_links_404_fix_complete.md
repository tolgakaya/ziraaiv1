# Android Universal Links - assetlinks.json 404 Fix Session

**Date**: 2025-10-14
**Status**: 🟡 DEPLOYED - Awaiting Verification
**Branch**: `feature/sponsorship-sms-deep-linking`
**Context**: Final fix for Android Universal Links - assetlinks.json was returning 404 on Railway staging

---

## Problem Summary

SMS-based sponsorship code redemption links were opening browser instead of app directly. The root cause was assetlinks.json returning 404 from Railway staging environment.

**Expected Flow:**
```
SMS: https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-ABC123
  ↓
Android OS checks: /.well-known/assetlinks.json
  ↓
Verifies package + SHA256
  ↓
Opens app directly (NO browser!)
```

**Actual Flow (BROKEN):**
```
SMS link → 404 on assetlinks.json → Browser opens → User confusion
```

---

## Root Causes Identified and Fixed

### Fix #1: WebAPI.csproj - Include .well-known in Build ✅
**Commit**: f188319

**Problem**: `.well-known` directory not included in build output

**Solution**: Added to WebAPI.csproj:
```xml
<ItemGroup>
    <!-- Copy .well-known directory for Android Universal Links -->
    <Content Include=".well-known\**\*">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
        <CopyToPublishDirectory>Always</CopyToPublishDirectory>
    </Content>
</ItemGroup>
```

**Result**: Still 404 (not enough!)

---

### Fix #2: Dockerfile.webapi - Copy .well-known to Container ✅
**Commit**: 17052b3

**Problem**: Docker build didn't include `.well-known` directory even after .csproj fix

**Root Cause**: Railway build logs showed NO COPY command for `.well-known`

**Solution**: Added to Dockerfile.webapi line 42:
```dockerfile
# Final stage
FROM base AS final
ARG TARGET_ENVIRONMENT
WORKDIR /app
COPY --from=publish /app/publish .

# Copy .well-known directory for Android Universal Links (assetlinks.json)
COPY --from=build /src/WebAPI/.well-known /app/.well-known

# Create config directory for backup files
RUN mkdir -p /app/config
```

**Result**: Still 404 (files in container, but not served!)

---

### Fix #3: Startup.cs - Correct Path in Docker ✅
**Commit**: 4079099 (FINAL FIX)

**Problem**: Startup.cs used `env.ContentRootPath` which pointed to wrong location in Docker container

**Investigation**:
- Dockerfile copies to: `/app/.well-known` ✅
- Runtime tries to read from: `env.ContentRootPath + "/.well-known"` ❌
- In Railway container: `ContentRootPath != /app`
- StaticFileOptions couldn't find files → 404

**Solution**: Smart path detection with Docker/local fallback:
```csharp
// Serve .well-known directory for Android Universal Links (assetlinks.json)
// Docker: /app/.well-known | Local: ContentRootPath/.well-known
var wellKnownPath = Directory.Exists("/app/.well-known")
    ? "/app/.well-known"                                  // Docker/Railway
    : Path.Combine(env.ContentRootPath, ".well-known");   // Local dev

if (!Directory.Exists(wellKnownPath))
{
    Directory.CreateDirectory(wellKnownPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wellKnownPath),
    RequestPath = "/.well-known",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/json"
});
```

**Result**: SHOULD WORK NOW! (Deployed, awaiting verification)

---

## Technical Details

### File: WebAPI/.well-known/assetlinks.json
**Purpose**: Android domain verification for Universal Links

**Content**:
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
},
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
    "package_name": "com.ziraai.app",
    "sha256_cert_fingerprints": [
      "PRODUCTION_SHA256_WILL_BE_PROVIDED_LATER"
    ]
  }
}]
```

### SHA256 Fingerprint Source
- **Keystore**: `C:\Users\Asus\.android\debug.keystore`
- **Fingerprint**: `E2:9C:97:6A:A8:82:5A:02:44:71:A1:97:B1:B6:AA:06:94:95:D4:C2:22:78:0E:A9:65:6E:17:0F:1F:AE:0B:67`
- **For**: Development and Staging builds
- **Packages**: `com.ziraai.app.dev`, `com.ziraai.app.staging`

---

## Verification Steps (NEXT SESSION)

### 1. Verify assetlinks.json Accessible
```bash
curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json
```

**Expected Response**: Valid JSON with SHA256 fingerprints ✅

**NOT Expected**: 
```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404}
```

### 2. Test Android Universal Links Flow

**Send Test SMS**:
```
🎁 Test sponsorluğu! 
Kod: AGRI-2025-TEST123
Tıklayın: https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-TEST123
```

**Expected Behavior**:
1. Tap link in SMS
2. Android OS checks assetlinks.json
3. Verifies package: `com.ziraai.app.staging`
4. Verifies SHA256 matches debug keystore
5. Opens ZiraAI app DIRECTLY (NO browser!)
6. Redemption screen opens with code pre-filled
7. User taps "Kullan" button
8. POST /api/v1/redemption/redeem-code called
9. Subscription activated ✅

**NOT Expected**:
- ❌ Browser opens first
- ❌ "Choose app" dialog
- ❌ HTML fallback page shown

### 3. Check Railway Deployment Status

Railway auto-deploy is active. Check:
- Latest commit: 4079099
- Build logs for successful deployment
- No errors in startup logs

### 4. Android Domain Verification Check

On Android device with staging app installed:
```bash
adb shell dumpsys package domain-preferred-apps | grep -A 10 ziraai

# Expected output:
# Package: com.ziraai.app.staging
#   Domain verification status:
#     ziraai-api-sit.up.railway.app: verified
```

**Note**: First install takes 2-3 minutes for domain verification!

---

## Files Modified (3 commits)

### Commit f188319: WebAPI.csproj
- Added Content Include for `.well-known\**\*`
- CopyToOutputDirectory: Always
- CopyToPublishDirectory: Always

### Commit 17052b3: Dockerfile.webapi
- Added line 42: `COPY --from=build /src/WebAPI/.well-known /app/.well-known`
- Ensures directory included in Docker image

### Commit 4079099: Startup.cs (FINAL FIX)
- Line 342-344: Smart path detection
- Checks `/app/.well-known` first (Docker)
- Falls back to `ContentRootPath/.well-known` (local)

---

## Related Documentation

**Created During This Session**:
- `claudedocs/MOBILE_TEAM_ANDROID_UNIVERSAL_LINKS_COMPLETE.md` - Mobile team guide
- `claudedocs/MOBILE_SHA256_FINGERPRINTS.md` - User-provided fingerprints
- `claudedocs/redeem.png` - Screenshot showing HTML fallback (browser opening)

**Previous Sessions**:
- `claudedocs/DEEP_LINK_BACKEND_FIXES_COMPLETE.md` - Earlier fixes (GET/POST separation)
- `claudedocs/CRITICAL_DEEP_LINK_ISSUES_AND_SOLUTIONS.md` - Original issue list

---

## Environment Configuration

### Staging (Railway)
```json
{
  "ApiBaseUrl": "https://ziraai-api-sit.up.railway.app",
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app.staging"
  },
  "Redemption": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/redeem",
    "FallbackDeepLinkBaseUrl": "ziraai://redemption-success"
  }
}
```

### Development (Local)
```json
{
  "ApiBaseUrl": "https://localhost:5001",
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app.dev"
  },
  "Redemption": {
    "DeepLinkBaseUrl": "https://localhost:5001/redeem",
    "FallbackDeepLinkBaseUrl": "ziraai://redemption-success"
  }
}
```

---

## Why This Fix Should Work

**Three-Layer Fix**:
1. ✅ Files included in build (WebAPI.csproj)
2. ✅ Files copied to Docker image (Dockerfile.webapi)
3. ✅ Files served from correct path (Startup.cs)

**Previous Attempts Failed Because**:
- Fix #1 only: Files built but not in Docker image
- Fix #1 + #2: Files in Docker but runtime looked in wrong path
- Fix #1 + #2 + #3: **Complete chain from source to serving!**

---

## Next Session Action Items

### IMMEDIATE (First 5 Minutes)
1. Check Railway deployment status for commit 4079099
2. Test: `curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json`
3. If ✅: Mark as RESOLVED and proceed to end-to-end testing
4. If ❌: Debug with Railway logs and container inspection

### IF VERIFIED (Android Testing)
1. Uninstall all ZiraAI apps from test device
2. Install ONLY staging build: `com.ziraai.app.staging`
3. Wait 2-3 minutes for Android domain verification
4. Send test SMS with redemption link
5. Tap link and verify app opens directly
6. Test full redemption flow
7. Document results

### IF STILL 404 (Emergency Debugging)
1. Check Railway build logs for COPY command execution
2. SSH into Railway container (if possible)
3. Verify file exists: `ls -la /app/.well-known/assetlinks.json`
4. Check Startup.cs logs for which path was selected
5. Verify StaticFileOptions middleware registered correctly

### AFTER SUCCESS
1. Update mobile team documentation
2. Merge to master/main
3. Create production keystore and extract SHA256
4. Update assetlinks.json for production package
5. Test production flow before release

---

## Key Lessons Learned

1. **Docker Path != Local Path**: ContentRootPath unreliable in containers
2. **Three-Layer Problem**: Build → Docker → Runtime all need correct configuration
3. **Build Logs Critical**: Railway logs revealed missing COPY command
4. **Smart Fallback**: Support both Docker and local dev with path detection
5. **Auto-Deploy**: User has auto-deploy from feature branch to staging

---

## Status at Session End

- ✅ All 3 fixes committed and pushed
- ⏳ Railway deploying commit 4079099
- 📋 Waiting for verification (5-10 minutes)
- 🎯 Expected: assetlinks.json accessible → Android Universal Links working
- 📱 Mobile team: NO code changes needed!

**Resume Next Session With:**
"Test assetlinks.json accessibility on Railway staging after deployment of commit 4079099"