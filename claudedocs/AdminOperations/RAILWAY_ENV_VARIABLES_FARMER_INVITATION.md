# Railway Environment Variables - Farmer Invitation System

**Purpose**: Configuration guide for deploying Farmer Invitation System to Railway (Staging & Production)

**Date**: 2026-01-02

---

## Overview

The Farmer Invitation System requires environment-specific configuration for:
- Deep link URLs (different per environment)
- Token expiry settings
- SMS template messages

These settings are configured through Railway environment variables to override `appsettings.json` values.

---

## Environment Variables Structure

Railway uses double underscore (`__`) notation for nested JSON configuration:

```
SectionName__SubSection__Property=value
```

Example:
```
FarmerInvitation__DeepLinkBaseUrl=https://ziraai.com/farmer-invite/
```

---

## Staging Environment Variables

### Required Variables

```bash
# Farmer Invitation Configuration
FARMERINVITATION__DEEPLINKBASEURL=https://ziraai-api-sit.up.railway.app/farmer-invite/
FARMERINVITATION__TOKENEXPIRYDAYS=7
FARMERINVITATION__SMSTEMPLATE=🌱 {sponsorName} tarafından {codeCount} adet sponsorluk kodu gönderildi!\n\nDavetiyeyi görüntülemek için:\n{invitationLink}\n\nDavetiye {expiryDays} gün geçerlidir.
```

### Important Notes for Staging

1. **Deep Link URL**: Points to Railway staging domain
   - Format: `https://ziraai-api-sit.up.railway.app/farmer-invite/`
   - Must match the domain where API is deployed

2. **Token Expiry**: 7 days (standard for testing)
   - Can be adjusted for specific testing scenarios

3. **SMS Template Placeholders**:
   - `{sponsorName}` - Replaced with sponsor's company name
   - `{codeCount}` - Number of codes in invitation
   - `{invitationLink}` - Full deep link with token
   - `{expiryDays}` - Days until invitation expires

---

## Production Environment Variables

### Required Variables

```bash
# Farmer Invitation Configuration
FARMERINVITATION__DEEPLINKBASEURL=https://ziraai.com/farmer-invite/
FARMERINVITATION__TOKENEXPIRYDAYS=7
FARMERINVITATION__SMSTEMPLATE=🌱 {sponsorName} tarafından {codeCount} adet sponsorluk kodu gönderildi!\n\nDavetiyeyi görüntülemek için:\n{invitationLink}\n\nDavetiye {expiryDays} gün geçerlidir.
```

### Important Notes for Production

1. **Deep Link URL**: Points to production domain
   - Format: `https://ziraai.com/farmer-invite/`
   - Must use HTTPS for security

2. **Token Expiry**: 7 days (production standard)
   - Balances user convenience with security
   - Can be adjusted based on business requirements

3. **SMS Template**:
   - Production-ready Turkish message
   - Professional tone
   - Clear call-to-action

---

## Mobile App Package Names

The mobile app deep link handling requires proper package name configuration:

### Development
```bash
MOBILEAPP__PLAYSTOREPACKAGENAME=com.ziraai.app.dev
```

### Staging
```bash
MOBILEAPP__PLAYSTOREPACKAGENAME=com.ziraai.app.staging
```

### Production
```bash
MOBILEAPP__PLAYSTOREPACKAGENAME=com.ziraai.app
```

**Note**: These are already configured in respective `appsettings.json` files but can be overridden via Railway if needed.

---

## Deep Link URL Behavior

### How Deep Links Work

1. **Invitation Created**:
   ```
   Token: a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6
   Deep Link: https://ziraai.com/farmer-invite/a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6
   ```

2. **SMS Sent to Farmer**:
   ```
   🌱 ABC Tarım tarafından 5 adet sponsorluk kodu gönderildi!

   Davetiyeyi görüntülemek için:
   https://ziraai.com/farmer-invite/a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6

   Davetiye 7 gün geçerlidir.
   ```

3. **Mobile App Handles Link**:
   - If app installed: Opens directly to invitation screen
   - If app not installed: Redirects to Play Store, then opens invitation after install

### URL Format Requirements

- **Development**: `http://localhost:5001/farmer-invite/`
- **Staging**: `https://ziraai-api-sit.up.railway.app/farmer-invite/`
- **Production**: `https://ziraai.com/farmer-invite/`

**Important**:
- Always end with trailing slash `/`
- Use HTTPS in staging/production
- Token is appended automatically by the system

---

## Configuration Service Integration

The `FarmerInvitationConfigurationService` reads these values:

```csharp
public class FarmerInvitationConfigurationService : IFarmerInvitationConfigurationService
{
    private readonly IConfiguration _configuration;

    public async Task<string> GetDeepLinkBaseUrlAsync()
    {
        return _configuration["FarmerInvitation:DeepLinkBaseUrl"]
            ?? "https://localhost:5001/farmer-invite/";
    }

    public async Task<int> GetTokenExpiryDaysAsync()
    {
        var days = _configuration["FarmerInvitation:TokenExpiryDays"];
        return int.TryParse(days, out var result) ? result : 7;
    }

    public async Task<string> GetSmsTemplateAsync()
    {
        return _configuration["FarmerInvitation:SmsTemplate"]
            ?? "🌱 {sponsorName} tarafından {codeCount} adet sponsorluk kodu gönderildi!...";
    }
}
```

### Priority Order

1. **Railway Environment Variables** (highest priority)
2. **appsettings.Staging.json** (for staging deployments)
3. **appsettings.json** (fallback/default values)

---

## Deployment Checklist

### Before Deploying to Staging

- [ ] Set `FARMERINVITATION__DEEPLINKBASEURL` to staging domain
- [ ] Verify `FARMERINVITATION__TOKENEXPIRYDAYS=7`
- [ ] Test SMS template formatting
- [ ] Verify mobile app package name matches staging
- [ ] Test deep link opens mobile app correctly

### Before Deploying to Production

- [ ] Set `FARMERINVITATION__DEEPLINKBASEURL` to production domain
- [ ] Verify `FARMERINVITATION__TOKENEXPIRYDAYS=7`
- [ ] Review SMS template for production tone
- [ ] Verify mobile app package name matches production
- [ ] Test deep link with production domain
- [ ] Verify HTTPS certificate is valid
- [ ] Test SMS delivery with real phone numbers

---

## Testing Deep Links

### Manual Testing Steps

1. **Create Invitation via API**:
   ```bash
   POST /api/Sponsorship/farmer/invite
   {
     "phone": "05551234567",
     "farmerName": "Test Farmer",
     "codeCount": 5,
     "packageTier": "M"
   }
   ```

2. **Verify SMS Contains Correct URL**:
   - Check SMS logs for sent message
   - Verify deep link format
   - Verify token is valid GUID

3. **Test Deep Link**:
   - Click link on mobile device
   - Verify app opens (if installed)
   - Verify redirect to Play Store (if not installed)
   - After install, verify invitation details load

4. **Test Invitation Acceptance**:
   ```bash
   POST /api/Sponsorship/farmer/accept-invitation
   {
     "invitationToken": "a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6"
   }
   ```

---

## SMS Template Placeholders Reference

| Placeholder | Description | Example Value |
|------------|-------------|---------------|
| `{sponsorName}` | Sponsor's company name | "ABC Tarım" |
| `{codeCount}` | Number of codes | "5" |
| `{invitationLink}` | Full deep link URL | "https://ziraai.com/farmer-invite/abc123..." |
| `{expiryDays}` | Days until expiry | "7" |

### Template Formatting Tips

- Keep total message under 160 characters if possible (single SMS)
- Use emojis sparingly (🌱 for farming context)
- Include clear call-to-action
- Mention expiry to create urgency
- Use Turkish language for Turkish farmers

---

## Troubleshooting

### Deep Link Not Working

**Problem**: Deep link doesn't open mobile app

**Solutions**:
1. Verify `assetlinks.json` is configured in mobile app
2. Check package name matches environment
3. Verify deep link URL format (trailing slash)
4. Test with different browsers/messaging apps

### SMS Not Sending

**Problem**: Farmer doesn't receive SMS

**Solutions**:
1. Check SMS logging in database
2. Verify SMS provider credentials (Netgsm)
3. Check phone number format (normalized correctly)
4. Review SMS provider quota/balance

### Token Expired

**Problem**: Invitation shows as expired

**Solutions**:
1. Verify `TokenExpiryDays` is set to 7
2. Check invitation creation date in database
3. Consider increasing expiry for specific use cases
4. Implement resend functionality (Phase 3)

---

## Related Documentation

- [FARMER_INVITATION_DEVELOPMENT_PLAN.md](FARMER_INVITATION_DEVELOPMENT_PLAN.md) - Overall development plan
- [FARMER_SPONSORSHIP_INVITATION_DESIGN.md](FARMER_SPONSORSHIP_INVITATION_DESIGN.md) - System design
- [Environment Configuration Guide](../environment-configuration.md) - General environment setup

---

## Railway Deployment Commands

### View Current Variables (Staging)
```bash
railway variables --environment staging
```

### Set Variable (Staging)
```bash
railway variables set FARMERINVITATION__DEEPLINKBASEURL=https://ziraai-api-sit.up.railway.app/farmer-invite/ --environment staging
```

### Set Variable (Production)
```bash
railway variables set FARMERINVITATION__DEEPLINKBASEURL=https://ziraai.com/farmer-invite/ --environment production
```

---

**Last Updated**: 2026-01-02
**Updated By**: Claude
**Status**: Ready for deployment
