# Referral System - Environment-Based Deep Link Implementation

## Problem
Deep link URLs in referral responses were hardcoded to production (`https://ziraai.com/ref/`) regardless of environment, making staging/local testing impossible with mobile emulators.

## Solution Implemented

### 1. Updated ReferralConfigurationService
**File:** `Business/Services/Referral/ReferralConfigurationService.cs`

**Changes:**
- Added `IConfiguration` dependency injection
- Modified `GetDeepLinkBaseUrlAsync()` with priority system:
  1. **appsettings.json** (environment-specific) - HIGHEST PRIORITY
  2. Database configuration (ReferralConfiguration table)
  3. Default fallback (`https://ziraai.com/ref/`)

```csharp
public async Task<string> GetDeepLinkBaseUrlAsync()
{
    // Priority: 1. appsettings.json, 2. Database, 3. Default
    var configValue = _configuration["Referral:DeepLinkBaseUrl"];

    if (!string.IsNullOrWhiteSpace(configValue))
    {
        _logger.LogDebug("Using deep link base URL from appsettings: {Url}", configValue);
        return await Task.FromResult(configValue);
    }

    return await GetCachedStringValueAsync(
        ReferralConfigurationKeys.DeepLinkBaseUrl,
        "https://ziraai.com/ref/");
}
```

### 2. Environment-Specific Configuration

**appsettings.Development.json:**
```json
{
  "Referral": {
    "DeepLinkBaseUrl": "https://localhost:5001/ref/"
  }
}
```

**appsettings.Staging.json:**
```json
{
  "Referral": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/ref/"
  },
  "SponsorRequest": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/sponsor-request/"
  }
}
```

**appsettings.json (Production):**
```json
{
  "Referral": {
    "DeepLinkBaseUrl": "https://ziraai.com/ref/"
  },
  "SponsorRequest": {
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/"
  }
}
```

### 3. Expected Behavior

**Staging Response:**
```json
{
  "data": {
    "referralCode": "ZIRA-K5ZYZX",
    "deepLink": "https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX",
    "playStoreLink": "...",
    "expiresAt": "2025-11-04T09:43:13Z"
  }
}
```

**Development Response:**
```json
{
  "data": {
    "referralCode": "ZIRA-K5ZYZX",
    "deepLink": "https://localhost:5001/ref/ZIRA-K5ZYZX",
    "playStoreLink": "...",
    "expiresAt": "2025-11-04T09:43:13Z"
  }
}
```

## Mobile App Integration

### Flutter Deep Link Setup (AndroidManifest.xml)

```xml
<!-- Staging Environment -->
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    
    <data
        android:scheme="https"
        android:host="ziraai-api-sit.up.railway.app"
        android:pathPrefix="/ref" />
</intent-filter>

<!-- Production Environment -->
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    
    <data
        android:scheme="https"
        android:host="ziraai.com"
        android:pathPrefix="/ref" />
</intent-filter>
```

## Testing Strategy

### 1. Test Deep Link Generation
```bash
# Staging
curl -X POST https://ziraai-api-sit.up.railway.app/api/referral/generate \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"deliveryMethod": 1, "phoneNumbers": ["05321111121"]}'
```

### 2. Test with Android Emulator
```bash
# Send deep link to emulator
adb shell am start -W -a android.intent.action.VIEW \
  -d "https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX" \
  com.ziraai.app
```

### 3. Verify Code Handling
```dart
void handleDeepLink(String link) {
  final uri = Uri.parse(link);
  if (uri.path.startsWith('/ref/')) {
    final referralCode = uri.pathSegments.last;
    // Navigate to registration with code
  }
}
```

## Deployment Checklist

- [x] ReferralConfigurationService updated
- [x] Development config added
- [x] Staging config added
- [x] Production config added
- [ ] Build and test staging
- [ ] Update mobile app intent filters
- [ ] Test end-to-end flow
- [ ] Deploy to production

## Files Modified
1. `Business/Services/Referral/ReferralConfigurationService.cs`
2. `WebAPI/appsettings.Development.json`
3. `WebAPI/appsettings.Staging.json`
4. `WebAPI/appsettings.json`

## Documentation Created
- `claudedocs/referral-testing-guide.md` - Complete testing guide with mobile integration examples
