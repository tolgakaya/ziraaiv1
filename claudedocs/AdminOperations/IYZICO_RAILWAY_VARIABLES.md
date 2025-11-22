# iyzico Payment Integration - Railway Environment Variables

## Railway Environment Variables (Copy-Paste Ready)

### 📁 Hazır Dosyalar

Her environment için ayrı dosyalar hazırlandı:

1. **[RAILWAY_IYZICO_STAGING.txt](./RAILWAY_IYZICO_STAGING.txt)** ← Staging için (Sandbox)
2. **[RAILWAY_IYZICO_PRODUCTION.txt](./RAILWAY_IYZICO_PRODUCTION.txt)** ← Production için (Live)

### Staging Environment (Sandbox)

```bash
# Dosya: RAILWAY_IYZICO_STAGING.txt
Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
Iyzico__ApiKey=YOUR_SANDBOX_API_KEY
Iyzico__SecretKey=YOUR_SANDBOX_SECRET_KEY
Iyzico__Currency=TRY
Iyzico__PaymentChannel=MOBILE
Iyzico__PaymentGroup=SUBSCRIPTION
Iyzico__TokenExpirationMinutes=30
Iyzico__Callback__DeepLinkScheme=ziraai://payment-callback
Iyzico__Callback__FallbackUrl=https://ziraai-api-sit.up.railway.app/payment/callback
Iyzico__Timeout__InitializeTimeoutSeconds=30
Iyzico__Timeout__VerifyTimeoutSeconds=30
Iyzico__Timeout__WebhookTimeoutSeconds=15
Iyzico__Retry__MaxRetryAttempts=3
Iyzico__Retry__RetryDelayMilliseconds=1000
Iyzico__Retry__UseExponentialBackoff=true
```

### Production Environment (Live)

```bash
# Dosya: RAILWAY_IYZICO_PRODUCTION.txt
Iyzico__BaseUrl=https://api.iyzipay.com
Iyzico__ApiKey=YOUR_PRODUCTION_API_KEY
Iyzico__SecretKey=YOUR_PRODUCTION_SECRET_KEY
Iyzico__Currency=TRY
Iyzico__PaymentChannel=MOBILE
Iyzico__PaymentGroup=SUBSCRIPTION
Iyzico__TokenExpirationMinutes=30
Iyzico__Callback__DeepLinkScheme=ziraai://payment-callback
Iyzico__Callback__FallbackUrl=https://api.ziraai.com/payment/callback
Iyzico__Timeout__InitializeTimeoutSeconds=30
Iyzico__Timeout__VerifyTimeoutSeconds=30
Iyzico__Timeout__WebhookTimeoutSeconds=15
Iyzico__Retry__MaxRetryAttempts=3
Iyzico__Retry__RetryDelayMilliseconds=1000
Iyzico__Retry__UseExponentialBackoff=true
```

## Variable Mapping

ASP.NET Core automatically maps Railway environment variables to configuration:

| Railway Variable | Maps To Configuration |
|------------------|----------------------|
| `Iyzico__BaseUrl` | `IyzicoOptions.BaseUrl` |
| `Iyzico__ApiKey` | `IyzicoOptions.ApiKey` |
| `Iyzico__SecretKey` | `IyzicoOptions.SecretKey` |
| `Iyzico__Callback__DeepLinkScheme` | `IyzicoOptions.Callback.DeepLinkScheme` |
| `Iyzico__Timeout__InitializeTimeoutSeconds` | `IyzicoOptions.Timeout.InitializeTimeoutSeconds` |
| `Iyzico__Retry__MaxRetryAttempts` | `IyzicoOptions.Retry.MaxRetryAttempts` |

⚠️ **IMPORTANT:** Double underscore `__` is used for nested configuration hierarchy.

## How to Set in Railway

### Option 1: Railway Dashboard (Recommended)

1. Go to your Railway project
2. Select service (e.g., `ziraai-api-staging`)
3. Click **Variables** tab
4. Click **RAW Editor** button
5. Paste the variables block above
6. Click **Save**
7. Railway will auto-deploy with new variables

### Option 2: Railway CLI

```bash
# Login and link project
railway login
railway link

# Set variables one by one
railway variables set Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
railway variables set Iyzico__ApiKey=YOUR_SANDBOX_API_KEY
railway variables set Iyzico__SecretKey=YOUR_SANDBOX_SECRET_KEY
# ... (continue with other variables)

# Or use raw editor
railway variables set --raw "$(cat iyzico-staging.env)"
```

### Option 3: Create .env File (NOT committed to git)

Create `iyzico-staging.env` file locally:

```bash
Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
Iyzico__ApiKey=sandbox-xxxxxxxxx
Iyzico__SecretKey=sandbox-yyyyyyyyy
Iyzico__Currency=TRY
Iyzico__PaymentChannel=MOBILE
Iyzico__PaymentGroup=SUBSCRIPTION
Iyzico__TokenExpirationMinutes=30
Iyzico__Callback__DeepLinkScheme=ziraai://payment-callback
Iyzico__Callback__FallbackUrl=https://ziraai-api-sit.up.railway.app/payment/callback
Iyzico__Timeout__InitializeTimeoutSeconds=30
Iyzico__Timeout__VerifyTimeoutSeconds=30
Iyzico__Timeout__WebhookTimeoutSeconds=15
Iyzico__Retry__MaxRetryAttempts=3
Iyzico__Retry__RetryDelayMilliseconds=1000
Iyzico__Retry__UseExponentialBackoff=true
```

Then use Railway RAW Editor to paste the contents.

## Getting iyzico Credentials

### Sandbox (Staging)

1. Go to https://merchant.iyzipay.com/
2. Register a test merchant account
3. Navigate to **Settings** → **API Keys**
4. Select **Sandbox** environment
5. Copy **API Key** and **Secret Key**

### Production

1. Complete merchant verification process on iyzico
2. Get approval from iyzico team
3. Navigate to **Settings** → **API Keys**
4. Select **Production** environment
5. Copy **API Key** and **Secret Key**

## Verification After Deployment

### Check Variables are Set

```bash
# Via Railway CLI
railway variables | grep Iyzico

# Expected output:
# Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
# Iyzico__ApiKey=sandbox-xxx
# Iyzico__SecretKey=sandbox-yyy
# ... (other variables)
```

### Check Application Logs

After deployment, check Railway logs:

```bash
railway logs --tail

# Look for:
# [INFO] IyzicoOptions loaded successfully
# [INFO] BaseUrl: https://sandbox-api.iyzipay.com
# [INFO] Payment channel: MOBILE
```

### Test Configuration Loading (Optional Endpoint)

Add this test endpoint to verify configuration (Development/Staging only):

```csharp
[HttpGet("payment/config-test")]
[Authorize(Roles = "Admin")]
public IActionResult TestIyzicoConfig([FromServices] IOptions<IyzicoOptions> options)
{
    var config = options.Value;
    return Ok(new
    {
        baseUrl = config.BaseUrl,
        currency = config.Currency,
        paymentChannel = config.PaymentChannel,
        hasApiKey = !string.IsNullOrEmpty(config.ApiKey),
        hasSecretKey = !string.IsNullOrEmpty(config.SecretKey),
        callbackScheme = config.Callback.DeepLinkScheme,
        // DON'T expose actual keys!
    });
}
```

## Configuration Priority

ASP.NET Core loads configuration in this order (last wins):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. **Railway Environment Variables** ← Highest priority

This means Railway variables will **override** appsettings.json values.

## Security Notes

### ✅ DO:
- Store credentials ONLY in Railway environment variables
- Use different credentials for staging and production
- Never commit credentials to git
- Rotate API keys periodically
- Use Railway's built-in secrets management

### ❌ DON'T:
- Don't hardcode credentials in appsettings.json
- Don't share credentials via chat/email
- Don't use production credentials in staging
- Don't expose credentials in logs

## Troubleshooting

### Issue: Variables not loaded

**Symptom:** Application fails with "iyzico configuration missing"

**Solution:**
1. Check variable names are exactly correct (case-sensitive)
2. Ensure double underscores `__` are used (not single `_`)
3. Verify Railway service redeployed after setting variables
4. Check Railway logs for configuration errors

### Issue: Still using appsettings values

**Symptom:** Application uses hardcoded values from appsettings.json

**Solution:**
1. Railway variables must be set BEFORE deployment
2. Ensure ASPNETCORE_ENVIRONMENT is set correctly
3. Clear Railway cache and redeploy
4. Verify variables in Railway dashboard

### Issue: Authentication fails with iyzico

**Symptom:** API calls return 401 Unauthorized

**Solution:**
1. Verify ApiKey and SecretKey are correct
2. Check BaseUrl matches credential environment (sandbox vs production)
3. Ensure no extra spaces in variable values
4. Test credentials directly with iyzico API

## Next Steps

After setting these variables:

1. ✅ Deploy to Railway staging
2. ✅ Check deployment logs for configuration loading
3. ✅ Test payment initialize endpoint (will be created in Phase 6)
4. ✅ Verify iyzico API connectivity
5. ✅ Test full payment flow with sandbox test cards

## Related Files

- [IyzicoOptions.cs](../../Core/Configuration/IyzicoOptions.cs) - Configuration model
- [appsettings.Development.json](../../WebAPI/appsettings.Development.json) - Local dev config
- [appsettings.Staging.json](../../WebAPI/appsettings.Staging.json) - Staging template (overridden by Railway)
- [Implementation Plan](./SPONSOR_PAYMENT_IMPLEMENTATION_PLAN.md) - Full integration roadmap

## Add to Environment Variables Reference

Bu değişkenleri mevcut dokümana da eklemelisiniz:
- [ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md](../ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md)

Eklenecek section:

```markdown
## 21. Payment Gateway (iyzico) Configuration

### Iyzico__BaseUrl
- **Description:** iyzico API base URL
- **Type:** String (URL)
- **Required:** Yes
- **Values:**
  - `https://sandbox-api.iyzipay.com` (Staging)
  - `https://api.iyzipay.com` (Production)

### Iyzico__ApiKey
- **Description:** iyzico merchant API key
- **Type:** String (Secret)
- **Required:** Yes
- **Source:** iyzico merchant dashboard

### Iyzico__SecretKey
- **Description:** iyzico merchant secret key for HMACSHA256 authentication
- **Type:** String (Secret)
- **Required:** Yes
- **Source:** iyzico merchant dashboard

### Iyzico__Currency
- **Description:** Default payment currency
- **Type:** String (ISO 4217)
- **Required:** No
- **Default:** `TRY`
- **Values:** `TRY`, `USD`, `EUR`, `GBP`

### Iyzico__PaymentChannel
- **Description:** Payment channel for mobile apps
- **Type:** String
- **Required:** No
- **Default:** `MOBILE`
- **Values:** `MOBILE`, `MOBILE_IOS`, `MOBILE_ANDROID`

### Iyzico__Callback__DeepLinkScheme
- **Description:** Deep link scheme for payment callback
- **Type:** String (URL Scheme)
- **Required:** Yes
- **Default:** `ziraai://payment-callback`

### Iyzico__Callback__FallbackUrl
- **Description:** Fallback URL if deep link fails
- **Type:** String (URL)
- **Required:** No
- **Environment-specific:**
  - Dev: `https://localhost:5001/payment/callback`
  - Staging: `https://ziraai-api-sit.up.railway.app/payment/callback`
  - Production: `https://api.ziraai.com/payment/callback`
```
