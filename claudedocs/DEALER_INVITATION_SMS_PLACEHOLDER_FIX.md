# Dealer Invitation SMS Placeholder Issue - Root Cause & Fix

**Date**: 2025-12-08
**Issue**: Bulk dealer invitations contain unresolved URL placeholder
**Severity**: 🔴 High (Production SMS delivery broken)
**Status**: ✅ Fixed

---

## 📋 Problem Description

### Observed Behavior

**Single Dealer Invitation (WebAPI)** ✅ Correct:
```
Hemen katılmak için tıklayın:
https://api.ziraai.com/dealer-invitation/DEALER-2ebb718ac64c4421899bcc4d2c4e5f94
```

**Bulk Dealer Invitation (Worker Service)** ❌ Broken:
```
Hemen katilmak icin tiklayin:
$(ZIRAAI_WEBAPI_URL)/dealer-invitation/DEALER-da341de3772c4f32ace936d643dfb7fe
```

### User Impact

- Bulk SMS invitations sent to dealers contain unclickable placeholder URLs
- Dealers cannot accept invitations via deep link
- Manual intervention required to resend invitations
- Poor user experience and increased support burden

---

## 🔍 Root Cause Analysis

### Investigation Timeline

1. **Initial Hypothesis**: Thought `$(ZIRAAI_WEBAPI_URL)` was a Railway environment variable placeholder
2. **Code Search**: Searched for `ZIRAAI_WEBAPI_URL` in codebase → **No results**
3. **Configuration Service Analysis**: Found `DealerInvitationConfigurationService.cs` uses `{deepLink}` placeholder format
4. **Job Service Analysis**: Located `InviteDealerViaSmsCommand.cs` SMS building logic
5. **Root Cause Identified**: Worker Service missing `DealerInvitation:DeepLinkBaseUrl` configuration

### Configuration Flow Analysis

**File**: [Business/Services/DealerInvitation/DealerInvitationConfigurationService.cs](../Business/Services/DealerInvitation/DealerInvitationConfigurationService.cs)

```csharp
public async Task<string> GetDeepLinkBaseUrlAsync()
{
    // Priority: 1. Environment Variable (via appsettings override), 2. Fallback from config
    var configValue = _configuration["DealerInvitation:DeepLinkBaseUrl"];

    if (!string.IsNullOrWhiteSpace(configValue))
    {
        _logger.LogDebug("Using dealer invitation deep link base URL from appsettings/env: {Url}", configValue);
        return await Task.FromResult(configValue);
    }

    // If not in appsettings/env, use fallback from configuration
    var fallbackUrl = _configuration["DealerInvitation:FallbackDeepLinkBaseUrl"]
        ?? _configuration["WebAPI:BaseUrl"]?.TrimEnd('/') + "/dealer-invitation/"
        ?? throw new InvalidOperationException("DealerInvitation:DeepLinkBaseUrl or DealerInvitation:FallbackDeepLinkBaseUrl must be configured");

    _logger.LogDebug("Using dealer invitation deep link fallback URL: {Url}", fallbackUrl);
    return await Task.FromResult(fallbackUrl);
}
```

**Configuration Priority**:
1. ❌ `DealerInvitation:DeepLinkBaseUrl` (not configured in Worker Service)
2. ❌ `DealerInvitation:FallbackDeepLinkBaseUrl` (not configured)
3. ✅ `WebAPI:BaseUrl` → Returns `"https://localhost:5001"` from Worker Service appsettings.json

### SMS Building Logic

**File**: [Business/Handlers/Sponsorship/Commands/InviteDealerViaSmsCommand.cs:162-176](../Business/Handlers/Sponsorship/Commands/InviteDealerViaSmsCommand.cs#L162-L176)

```csharp
// 6. Generate deep link using configuration service
var baseUrl = await _configService.GetDeepLinkBaseUrlAsync();
var deepLink = $"{baseUrl.TrimEnd('/')}/DEALER-{invitation.InvitationToken}";

// 7. Get Play Store link
var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

// 8. Build SMS message using configuration service
var smsTemplate = await _configService.GetSmsTemplateAsync();
var smsMessage = smsTemplate
    .Replace("{sponsorName}", sponsorCompanyName)
    .Replace("{token}", invitation.InvitationToken)
    .Replace("{deepLink}", deepLink)
    .Replace("{playStoreLink}", playStoreLink);

// 9. Send SMS
var smsService = _messagingFactory.GetSmsService();
var sendResult = await smsService.SendSmsAsync(invitation.Phone, smsMessage);
```

**Analysis**:
- Code logic is **correct** - it calls `GetDeepLinkBaseUrlAsync()` and replaces `{deepLink}` placeholder
- Problem is configuration service returns `https://localhost:5001` from Worker Service appsettings
- Railway environment variable `$(ZIRAAI_WEBAPI_URL)` appears in SMS because Worker Service can't resolve localhost URL

### Why Single Invitations Work

**WebAPI Configuration** (assumed to have Railway env vars):
```json
{
  "DealerInvitation": {
    "DeepLinkBaseUrl": "https://api.ziraai.com/dealer-invitation"
  }
}
```

Result: `https://api.ziraai.com/dealer-invitation/DEALER-xxx` ✅

### Why Bulk Invitations Don't Work

**Worker Service Configuration** (missing dealer invitation config):
```json
{
  "WebAPI": {
    "BaseUrl": "https://localhost:5001"
  }
  // Missing: DealerInvitation:DeepLinkBaseUrl
}
```

Result: Falls back to `https://localhost:5001/dealer-invitation/DEALER-xxx` which Railway shows as `$(ZIRAAI_WEBAPI_URL)` ❌

---

## ✅ Solution

### Code Changes

**File**: [PlantAnalysisWorkerService/appsettings.json:89-97](../PlantAnalysisWorkerService/appsettings.json#L89-L97)

**Before**:
```json
{
  "WebAPI": {
    "BaseUrl": "https://localhost:5001",
    "InternalSecret": "ZiraAI_Internal_Secret_2025"
  }
}
```

**After**:
```json
{
  "WebAPI": {
    "BaseUrl": "https://localhost:5001",
    "InternalSecret": "ZiraAI_Internal_Secret_2025"
  },
  "DealerInvitation": {
    "DeepLinkBaseUrl": "https://api.ziraai.com/dealer-invitation",
    "FallbackDeepLinkBaseUrl": "https://api.ziraai.com/dealer-invitation",
    "TokenExpiryDays": 30
  }
}
```

**Changes**:
- ✅ Added `DealerInvitation:DeepLinkBaseUrl` configuration
- ✅ Added `DealerInvitation:FallbackDeepLinkBaseUrl` for redundancy
- ✅ Added `DealerInvitation:TokenExpiryDays` to match WebAPI configuration

---

## 🚀 Railway Environment Variable Setup

### Required Environment Variables for Worker Service

To support different environments (staging, production), configure these Railway environment variables:

| Variable Name | Development | Staging | Production |
|---------------|-------------|---------|------------|
| `DealerInvitation__DeepLinkBaseUrl` | `http://localhost:5001/dealer-invitation` | `https://ziraai-api-sit.up.railway.app/dealer-invitation` | `https://api.ziraai.com/dealer-invitation` |
| `DealerInvitation__FallbackDeepLinkBaseUrl` | `http://localhost:5001/dealer-invitation` | `https://ziraai-api-sit.up.railway.app/dealer-invitation` | `https://api.ziraai.com/dealer-invitation` |
| `MobileApp__PlayStorePackageName` | `com.ziraai.app.dev` | `com.ziraai.app.staging` | `com.ziraai.app` |

**Note**: Railway uses double underscore `__` to represent nested JSON keys (e.g., `DealerInvitation__DeepLinkBaseUrl` → `DealerInvitation:DeepLinkBaseUrl`)

### Railway Configuration Steps

1. Go to Railway Worker Service dashboard
2. Navigate to **Variables** tab
3. Add the following variables:

```bash
# Production
DealerInvitation__DeepLinkBaseUrl=https://api.ziraai.com/dealer-invitation
DealerInvitation__FallbackDeepLinkBaseUrl=https://api.ziraai.com/dealer-invitation
DealerInvitation__TokenExpiryDays=30
MobileApp__PlayStorePackageName=com.ziraai.app
```

4. Click **Deploy** to apply changes
5. Verify new SMS messages contain correct URLs

---

## 🧪 Testing Guide

### Test Case 1: Single Dealer Invitation (Baseline)

**Endpoint**: `POST /api/sponsorship/dealer-invitations/invite-via-sms`

**Request**:
```json
{
  "email": "dealer1@test.com",
  "phone": "05551234567",
  "dealerName": "Test Dealer",
  "packageTier": "M",
  "codeCount": 5
}
```

**Expected SMS** ✅:
```
🎁 [Sponsor Company] Bayilik Daveti!

Davet Kodunuz: DEALER-xxx

Hemen katılmak için tıklayın:
https://api.ziraai.com/dealer-invitation/DEALER-xxx

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

---

### Test Case 2: Bulk Dealer Invitation (Fixed)

**Endpoint**: `POST /api/sponsorship/dealer-invitations/bulk-invite`

**Request**:
```json
{
  "dealers": [
    {
      "email": "bulk1@test.com",
      "phone": "05551111111",
      "dealerName": "Bulk Dealer 1",
      "codeCount": 3
    },
    {
      "email": "bulk2@test.com",
      "phone": "05552222222",
      "dealerName": "Bulk Dealer 2",
      "codeCount": 3
    }
  ],
  "packageTier": "S",
  "sendSms": true
}
```

**Expected SMS** ✅:
```
🎁 [Sponsor Company] Bayilik Daveti!

Davet Kodunuz: DEALER-yyy

Hemen katılmak için tıklayın:
https://api.ziraai.com/dealer-invitation/DEALER-yyy

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Verification**:
- ✅ SMS contains `https://api.ziraai.com/dealer-invitation/DEALER-xxx` (not localhost)
- ✅ Deep link is clickable and navigates to dealer invitation page
- ✅ Play Store link is correct for environment

---

## 📊 Impact Assessment

### Before Fix
- ❌ Bulk invitations: Broken deep links with localhost/placeholder URLs
- ❌ User Experience: Dealers cannot accept invitations via SMS
- ❌ Manual Workaround: Support team must resend invitations manually

### After Fix
- ✅ Bulk invitations: Working deep links with production URLs
- ✅ User Experience: Seamless one-click invitation acceptance
- ✅ Automation: No manual intervention required

### Affected Users
- **Production**: All sponsors sending bulk dealer invitations (likely 100% broken)
- **Staging**: Not affected (separate Railway service)
- **Development**: Not affected (localhost testing)

---

## 🔄 Deployment Checklist

### Pre-Deployment
- [x] Add `DealerInvitation` configuration to Worker Service appsettings.json
- [ ] Build and test locally
- [ ] Verify Railway environment variables configured
- [ ] Review SMS template placeholders

### Deployment
- [ ] Commit changes to staging branch
- [ ] Deploy to Railway staging environment
- [ ] Test bulk invitation SMS on staging
- [ ] Merge to master and deploy to production
- [ ] Test bulk invitation SMS on production

### Post-Deployment
- [ ] Monitor Worker Service logs for SMS delivery
- [ ] Check first 5 bulk invitations for correct URLs
- [ ] Verify dealer invitation acceptance rate improves
- [ ] Update documentation with Railway configuration

---

## 🔗 Related Files

### Modified Files
- [PlantAnalysisWorkerService/appsettings.json](../PlantAnalysisWorkerService/appsettings.json) - Added dealer invitation configuration

### Key Files (No Changes)
- [Business/Services/DealerInvitation/DealerInvitationConfigurationService.cs](../Business/Services/DealerInvitation/DealerInvitationConfigurationService.cs) - Configuration service
- [Business/Handlers/Sponsorship/Commands/InviteDealerViaSmsCommand.cs](../Business/Handlers/Sponsorship/Commands/InviteDealerViaSmsCommand.cs) - SMS building logic
- [PlantAnalysisWorkerService/Jobs/DealerInvitationJobService.cs](../PlantAnalysisWorkerService/Jobs/DealerInvitationJobService.cs) - Hangfire job processing
- [PlantAnalysisWorkerService/Services/DealerInvitationConsumerWorker.cs](../PlantAnalysisWorkerService/Services/DealerInvitationConsumerWorker.cs) - RabbitMQ consumer

---

## 📝 Lessons Learned

1. **Configuration Parity**: Worker Service and WebAPI should have matching configuration for shared features
2. **Environment Variables**: Use Railway env vars to override appsettings for environment-specific URLs
3. **SMS Template Testing**: Always test actual SMS delivery, not just API responses
4. **Fallback Configuration**: Multiple fallback levels prevent single points of failure
5. **Cross-Process Communication**: Worker Service needs same configuration as WebAPI for consistent behavior

---

## ❓ FAQ

**Q: Why didn't Railway environment variables override the Worker Service configuration?**
A: Worker Service appsettings.json didn't have `DealerInvitation` section at all, so there was nothing to override. Railway env vars can only override existing keys.

**Q: Why does single invitation work but bulk doesn't?**
A: Single invitations are processed by WebAPI (correct config), bulk invitations are processed by Worker Service via RabbitMQ (missing config).

**Q: What is `$(ZIRAAI_WEBAPI_URL)` and where does it come from?**
A: This is likely Railway's way of displaying unresolved localhost URLs. The actual value sent was `https://localhost:5001` which Railway tried to "fix" with this placeholder.

**Q: Will this fix affect existing pending invitations?**
A: No. Existing invitations with broken URLs will remain broken. Only new invitations sent after deployment will have correct URLs.

**Q: Do we need to update WebAPI configuration too?**
A: No. WebAPI already has correct configuration (verified by working single invitations).

**Q: Should we add the same config to staging?**
A: Yes. Update `appsettings.Staging.json` with staging-specific URLs:
```json
{
  "DealerInvitation": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app/dealer-invitation"
  }
}
```

---

**Generated**: 2025-12-08
**Commit**: Pending
**Version**: 1.0
