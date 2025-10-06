# Referral System Testing Guide - Staging Environment

## Overview
Complete testing strategy for referral system with staging backend (Railway) and local Android emulator.

## Environment Configuration

### ⚠️ IMPORTANT: All URLs Must Be Configurable

**NEVER hard-code URLs in code!** All environment-specific URLs must be configured via `appsettings.json` or environment variables.

### Backend URLs by Environment

| Environment | Deep Link Base URL | PlayStore Package | Notes |
|------------|-------------------|-------------------|-------|
| **Development** | `https://localhost:5001/ref/` | `com.ziraai.app.dev` | Local development |
| **Staging** | `https://ziraai-api-sit.up.railway.app/ref/` | `com.ziraai.app.staging` | Railway staging |
| **Production** | `https://ziraai.com/ref/` | `com.ziraai.app` | Production |

### Configuration Files Location

**Development:** `WebAPI/appsettings.Development.json`
```json
{
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app.dev"
  },
  "Referral": {
    "DeepLinkBaseUrl": "https://localhost:5001/ref/"
  },
  "SponsorRequest": {
    "DeepLinkBaseUrl": "https://localhost:5001/sponsor-request/"
  }
}
```

**Staging:** `WebAPI/appsettings.Staging.json`
```json
{
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app.staging"
  },
  "Referral": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/ref/"
  },
  "SponsorRequest": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/sponsor-request/"
  }
}
```

**Production:** Environment Variables (Railway/Docker)
```bash
MobileApp__PlayStorePackageName=com.ziraai.app
Referral__DeepLinkBaseUrl=https://ziraai.com/ref/
Referral__FallbackDeepLinkBaseUrl=https://ziraai.com/ref/
SponsorRequest__DeepLinkBaseUrl=https://ziraai.com/sponsor-request/
```

### Configuration Priority
1. **appsettings.{Environment}.json** - **HIGHEST PRIORITY**
2. **Environment Variables** (Railway/Docker)
3. Database configuration (ReferralConfiguration table)
4. Fallback from `Referral:FallbackDeepLinkBaseUrl`

### Code Implementation
- `ReferralConfigurationService.GetDeepLinkBaseUrlAsync()` - reads from IConfiguration first
- `ReferralLinkService.BuildPlayStoreLinkAsync()` - uses `MobileApp:PlayStorePackageName`
- Both throw `InvalidOperationException` if configuration is missing

## Testing Scenarios

### Scenario 1: Generate Referral Link (Staging)

**Endpoint:** `POST /api/referral/generate`

**Request:**
```json
{
  "deliveryMethod": 1,
  "phoneNumbers": ["05321111121", "05321111122"],
  "customMessage": "ZiraAI'ye katıl!"
}
```

**Expected Response (Staging):**
```json
{
  "data": {
    "referralCode": "ZIRA-K5ZYZX",
    "deepLink": "https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX",
    "playStoreLink": "https://play.google.com/store/apps/details?id=com.ziraai.app&referrer=ZIRA-K5ZYZX",
    "expiresAt": "2025-11-04T09:43:13.3728394+00:00",
    "deliveryStatuses": [...]
  }
}
```

### Scenario 2: Mobile App Deep Link Handling

#### Flutter Deep Link Configuration

**1. Add to `android/app/src/main/AndroidManifest.xml`:**

```xml
<activity android:name=".MainActivity">
    <!-- Existing intent filters... -->

    <!-- Staging Deep Links -->
    <intent-filter android:autoVerify="true">
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />

        <!-- Staging Domain -->
        <data
            android:scheme="https"
            android:host="ziraai-api-sit.up.railway.app"
            android:pathPrefix="/ref" />
    </intent-filter>

    <!-- App Links for Direct Opening -->
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />

        <data android:scheme="ziraai" />
    </intent-filter>
</activity>
```

**2. Flutter Code for Deep Link Handling:**

```dart
// Using uni_links package
import 'package:uni_links/uni_links.dart';

// Initialize deep link listener
Future<void> initDeepLinks() async {
  // Handle initial deep link (app was closed)
  try {
    final initialLink = await getInitialLink();
    if (initialLink != null) {
      handleDeepLink(initialLink);
    }
  } catch (e) {
    print('Error getting initial link: $e');
  }

  // Listen for deep links while app is running
  linkStream.listen((String? link) {
    if (link != null) {
      handleDeepLink(link);
    }
  });
}

void handleDeepLink(String link) {
  // Parse referral code from URL
  // Example: https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX
  final uri = Uri.parse(link);

  if (uri.path.startsWith('/ref/')) {
    final referralCode = uri.pathSegments.last;
    print('Received referral code: $referralCode');

    // Navigate to registration with pre-filled code
    Navigator.pushNamed(
      context,
      '/register',
      arguments: {'referralCode': referralCode}
    );
  }
}
```

### Scenario 3: Testing with Android Emulator

#### Option A: ADB Deep Link Simulation
```bash
# Send deep link to emulator
adb shell am start -W -a android.intent.action.VIEW \
  -d "https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX" \
  com.ziraai.app
```

#### Option B: Browser Testing
1. Open Chrome on emulator
2. Navigate to: `https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX`
3. App should intercept and open automatically
4. If not, check intent filter configuration

#### Option C: SMS/Email Link Testing
1. Send test SMS/Email to emulator
2. Include deep link: `https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX`
3. Click link in message
4. App should open with referral code

### Scenario 4: End-to-End Referral Flow

**Step 1: User A Generates Referral**
- Login as User A (referrer)
- Call `/api/referral/generate` endpoint
- Get referral code: `ZIRA-ABC123`
- Receive deep link: `https://ziraai-api-sit.up.railway.app/ref/ZIRA-ABC123`

**Step 2: User B Receives Link (Emulator)**
```bash
# Simulate receiving link on emulator
adb shell am start -W -a android.intent.action.VIEW \
  -d "https://ziraai-api-sit.up.railway.app/ref/ZIRA-ABC123" \
  com.ziraai.app
```

**Step 3: User B Registers**
- App opens with pre-filled code: `ZIRA-ABC123`
- User B completes registration
- Backend validates referral code
- Creates referral relationship

**Step 4: User B Completes Analysis**
- User B performs plant analysis (meets `MinAnalysisForValidation`)
- Backend validates referral completion
- Credits awarded to User A

**Step 5: Verify Credits**
```http
GET /api/referral/user/{userId}/stats
```

**Expected Response:**
```json
{
  "totalReferrals": 1,
  "validatedReferrals": 1,
  "pendingReferrals": 0,
  "totalCreditsEarned": 10,
  "activeCodes": [...]
}
```

## Testing Checklist

### Backend Configuration
- [x] Environment-based deep link URLs configured
- [x] `ReferralConfigurationService` updated with IConfiguration priority
- [x] Staging: `https://ziraai-api-sit.up.railway.app/ref/`
- [x] Development: `https://localhost:5001/ref/`
- [x] Production: `https://ziraai.com/ref/`

### Mobile App Setup
- [ ] Add intent filters for staging domain
- [ ] Implement uni_links package for deep link handling
- [ ] Parse referral code from URL path
- [ ] Auto-fill registration form with code
- [ ] Handle app-closed vs app-running scenarios

### API Testing
- [ ] Generate referral link (verify correct domain)
- [ ] Validate referral code format
- [ ] Check expiry date calculation
- [ ] Test SMS/WhatsApp delivery (if enabled)

### Deep Link Testing
- [ ] ADB command deep link simulation
- [ ] Browser redirect to app
- [ ] SMS/Email link handling
- [ ] App launch with referral code

### Flow Testing
- [ ] User A generates code
- [ ] User B receives and opens link
- [ ] User B registers with code
- [ ] User B completes minimum analysis
- [ ] User A receives credits
- [ ] Verify in database and API

### Edge Cases
- [ ] Expired referral code handling
- [ ] Invalid/malformed code handling
- [ ] Maximum referrals per user limit
- [ ] Deep link when app not installed (fallback)
- [ ] Multiple referral codes for same user

## Debugging Tips

### Check Deep Link URL in Response
```bash
curl -X POST https://ziraai-api-sit.up.railway.app/api/referral/generate \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryMethod": 1,
    "phoneNumbers": ["05321111121"]
  }' | jq '.data.deepLink'
```

### Verify App Intent Filters
```bash
# Check if app handles the domain
adb shell dumpsys package com.ziraai.app | grep -A 5 "android.intent.action.VIEW"
```

### Monitor Deep Link Activity
```bash
# Watch for deep link activity in logcat
adb logcat | grep -i "intent\|deeplink\|referral"
```

### Test Environment Variable
```bash
# Verify staging environment is active
curl https://ziraai-api-sit.up.railway.app/api/health | jq '.environment'
```

## Common Issues & Solutions

### Issue 1: Deep Link Returns Production URL
**Cause:** appsettings.Staging.json not loaded or ASPNETCORE_ENVIRONMENT not set

**Solution:**
```bash
# Set environment variable in Railway
ASPNETCORE_ENVIRONMENT=Staging
```

### Issue 2: App Doesn't Open from Link
**Cause:** Intent filter domain mismatch or missing autoVerify

**Solution:**
- Verify `android:host` matches exactly: `ziraai-api-sit.up.railway.app`
- Add `android:autoVerify="true"` to intent filter
- Rebuild and reinstall app

### Issue 3: Referral Code Not Extracted
**Cause:** Path parsing logic incorrect

**Solution:**
```dart
// Correct parsing
final uri = Uri.parse(link);
final referralCode = uri.pathSegments.last; // Gets "ZIRA-ABC123"
```

### Issue 4: Staging Link Opens Browser Instead of App
**Cause:** App Links not verified for staging domain

**Solution:**
1. Add `.well-known/assetlinks.json` on staging server
2. Or use custom scheme: `ziraai://ref/ZIRA-ABC123`

## Quick Test Commands

```bash
# 1. Generate referral (get code from response)
curl -X POST https://ziraai-api-sit.up.railway.app/api/referral/generate \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"deliveryMethod": 1, "phoneNumbers": ["05321111121"]}' \
  | jq '.data'

# 2. Open deep link on emulator
adb shell am start -W -a android.intent.action.VIEW \
  -d "https://ziraai-api-sit.up.railway.app/ref/ZIRA-XXXXXX" \
  com.ziraai.app

# 3. Verify code on backend
curl -X POST https://ziraai-api-sit.up.railway.app/api/referral/validate \
  -H "Content-Type: application/json" \
  -d '{"code": "ZIRA-XXXXXX"}'

# 4. Check user stats
curl https://ziraai-api-sit.up.railway.app/api/referral/user/{userId}/stats \
  -H "Authorization: Bearer $TOKEN"
```

## Next Steps

1. **Build and Deploy Staging:**
   ```bash
   dotnet build --configuration Release
   # Deploy to Railway
   ```

2. **Configure Mobile App:**
   - Update AndroidManifest.xml with staging domain
   - Implement deep link handler
   - Test with ADB commands

3. **Production Deployment:**
   - Change ASPNETCORE_ENVIRONMENT to Production
   - Verify deep links use `https://ziraai.com/ref/`
   - Upload app to Play Store with production domain

## Support

For issues or questions:
- Check logs: `https://ziraai-api-sit.up.railway.app/api/logs`
- Review configuration: `/api/referral/config`
- Database check: ReferralConfiguration table
