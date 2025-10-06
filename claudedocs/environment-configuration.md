# Environment Configuration Guide

## ⚠️ CRITICAL: Never Hard-Code URLs or Environment-Specific Values

All environment-specific configurations must be externalized to configuration files or environment variables.

## Configuration Files

### Development: `WebAPI/appsettings.Development.json`
- Used when `ASPNETCORE_ENVIRONMENT=Development`
- Local development settings
- Debug features enabled

### Staging: `WebAPI/appsettings.Staging.json`
- Used when `ASPNETCORE_ENVIRONMENT=Staging`
- Railway deployment settings
- Production-like environment for testing

### Production: `WebAPI/appsettings.json` + Environment Variables
- `appsettings.json` is in `.gitignore` and contains defaults
- Sensitive values must be set via environment variables
- Railway/Docker environment variables override appsettings

## Referral System Configuration

### Required Configuration Keys

#### Mobile App Settings
```json
{
  "MobileApp": {
    "PlayStorePackageName": "com.ziraai.app"  // Changes per environment
  }
}
```

**Environment Values:**
- Development: `com.ziraai.app.dev`
- Staging: `com.ziraai.app.staging`
- Production: `com.ziraai.app`

#### Referral Deep Links
```json
{
  "Referral": {
    "DeepLinkBaseUrl": "https://ziraai.com/ref/",
    "FallbackDeepLinkBaseUrl": "https://ziraai.com/ref/"  // Used if database config fails
  }
}
```

**Environment Values:**
- Development: `https://localhost:5001/ref/`
- Staging: `https://ziraai-api-sit.up.railway.app/ref/`
- Production: `https://ziraai.com/ref/`

#### Sponsor Request Deep Links
```json
{
  "SponsorRequest": {
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/"
  }
}
```

**Environment Values:**
- Development: `https://localhost:5001/sponsor-request/`
- Staging: `https://ziraai-api-sit.up.railway.app/sponsor-request/`
- Production: `https://ziraai.com/sponsor-request/`

## Configuration Priority Order

1. **Environment Variables** (Railway/Docker) - HIGHEST PRIORITY
   ```bash
   MobileApp__PlayStorePackageName=com.ziraai.app
   Referral__DeepLinkBaseUrl=https://ziraai.com/ref/
   ```

2. **appsettings.{Environment}.json**
   - `appsettings.Development.json`
   - `appsettings.Staging.json`

3. **appsettings.json** (base configuration)

4. **Database Configuration** (ReferralConfiguration table)

5. **Fallback values** (`Referral:FallbackDeepLinkBaseUrl`)

## Code Implementation

### Reading Configuration

**ReferralConfigurationService.cs:**
```csharp
public async Task<string> GetDeepLinkBaseUrlAsync()
{
    // Priority: 1. appsettings.json (environment-specific), 2. Database, 3. Fallback from config
    var configValue = _configuration["Referral:DeepLinkBaseUrl"];

    if (!string.IsNullOrWhiteSpace(configValue))
    {
        _logger.LogDebug("Using deep link base URL from appsettings: {Url}", configValue);
        return await Task.FromResult(configValue);
    }

    // If not in appsettings, try database with fallback from configuration
    var fallbackUrl = _configuration["Referral:FallbackDeepLinkBaseUrl"]
        ?? throw new InvalidOperationException("Referral:DeepLinkBaseUrl or Referral:FallbackDeepLinkBaseUrl must be configured");

    return await GetCachedStringValueAsync(
        ReferralConfigurationKeys.DeepLinkBaseUrl,
        fallbackUrl);
}
```

**ReferralLinkService.cs:**
```csharp
public async Task<string> BuildPlayStoreLinkAsync(string referralCode)
{
    var packageName = _configuration["MobileApp:PlayStorePackageName"]
        ?? throw new InvalidOperationException("MobileApp:PlayStorePackageName must be configured");

    var playStoreLink = $"https://play.google.com/store/apps/details?id={packageName}&referrer={referralCode}";
    return await Task.FromResult(playStoreLink);
}
```

## Railway Environment Variables Setup

### Staging Environment

```bash
# Required
ASPNETCORE_ENVIRONMENT=Staging
MobileApp__PlayStorePackageName=com.ziraai.app.staging
Referral__DeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/ref/
Referral__FallbackDeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/ref/
SponsorRequest__DeepLinkBaseUrl=https://ziraai-api-sit.up.railway.app/sponsor-request/

# Database
ConnectionStrings__DArchPgContext=Host=xxx;Port=5432;Database=railway;Username=postgres;Password=xxx

# Other services
UseRedis=true
CacheOptions__Host=xxx
CacheOptions__Port=38265
CacheOptions__Password=xxx
CacheOptions__Ssl=true
```

### Production Environment

```bash
# Required
ASPNETCORE_ENVIRONMENT=Production
MobileApp__PlayStorePackageName=com.ziraai.app
Referral__DeepLinkBaseUrl=https://ziraai.com/ref/
Referral__FallbackDeepLinkBaseUrl=https://ziraai.com/ref/
SponsorRequest__DeepLinkBaseUrl=https://ziraai.com/sponsor-request/

# Database, cache, etc.
# ... (same pattern as staging with production values)
```

## Testing Configuration

### Verify Current Configuration

```bash
# Check which environment is active
curl https://your-api-url/api/health | jq '.environment'

# Test referral link generation
curl -X POST https://your-api-url/api/referral/generate \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"deliveryMethod": 1, "phoneNumbers": ["05321111121"]}' \
  | jq '.data.deepLink'
```

**Expected Responses:**
- Development: `https://localhost:5001/ref/ZIRA-XXXXXX`
- Staging: `https://ziraai-api-sit.up.railway.app/ref/ZIRA-XXXXXX`
- Production: `https://ziraai.com/ref/ZIRA-XXXXXX`

### Common Issues

#### Issue: Wrong URL in Response
**Symptom:** Getting production URL in staging environment

**Solution:**
1. Check `ASPNETCORE_ENVIRONMENT` is set correctly
2. Verify `appsettings.Staging.json` exists and contains correct values
3. Check Railway environment variables

#### Issue: InvalidOperationException
**Symptom:** `MobileApp:PlayStorePackageName must be configured`

**Solution:**
Add missing configuration to appropriate `appsettings.{Environment}.json` or environment variables

## Checklist for Adding New Environment-Specific Config

- [ ] Add to `appsettings.Development.json`
- [ ] Add to `appsettings.Staging.json`
- [ ] Document in this guide
- [ ] Add Railway environment variables (if needed for production)
- [ ] Update `CLAUDE.md` if it's a critical configuration
- [ ] Test in all environments

## Related Documentation

- [Referral Testing Guide](./referral-testing-guide.md) - End-to-end testing with environment configs
- [CLAUDE.md](../CLAUDE.md) - Project setup and configuration reference
- [Railway Deployment Guide](./railway-deployment-best-practices.md) - Production deployment
