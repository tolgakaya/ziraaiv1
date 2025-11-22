# Railway Environment Variables - Quick Setup Guide

## 📋 İyzico Payment Integration

### Dosyalar

| Dosya | Kullanım | Environment |
|-------|----------|-------------|
| [RAILWAY_IYZICO_STAGING.txt](./RAILWAY_IYZICO_STAGING.txt) | Staging için copy-paste | Sandbox |
| [RAILWAY_IYZICO_PRODUCTION.txt](./RAILWAY_IYZICO_PRODUCTION.txt) | Production için copy-paste | Live |
| [IYZICO_RAILWAY_VARIABLES.md](./IYZICO_RAILWAY_VARIABLES.md) | Detaylı dokümantasyon | Her iki ortam |

### 🚀 Hızlı Kurulum (Staging)

1. **iyzico Sandbox Credentials Alın**
   - https://merchant.iyzipay.com/ → Register
   - Settings → API Keys → **Sandbox**
   - API Key ve Secret Key'i kopyalayın

2. **Railway'e Yapıştırın**
   - Railway Dashboard → `ziraai-api-staging` service
   - **Variables** → **RAW Editor** butonuna tıklayın
   - [RAILWAY_IYZICO_STAGING.txt](./RAILWAY_IYZICO_STAGING.txt) dosyasını açın
   - İçeriğini kopyalayın
   - RAW Editor'a yapıştırın
   - `YOUR_SANDBOX_API_KEY` → Gerçek API key ile değiştirin
   - `YOUR_SANDBOX_SECRET_KEY` → Gerçek secret key ile değiştirin
   - **Save** butonuna basın (otomatik deploy olur)

3. **Doğrulayın**
   ```bash
   railway logs --tail

   # Aranacak log:
   # [INFO] IyzicoOptions loaded successfully
   # [INFO] BaseUrl: https://sandbox-api.iyzipay.com
   ```

### 📦 Toplam Variable Sayısı

- **15 adet** iyzico environment variable
- **3 mandatory** (BaseUrl, ApiKey, SecretKey)
- **12 optional** (defaults var ama override edilebilir)

### 🔑 Mandatory Variables

```bash
Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
Iyzico__ApiKey=YOUR_SANDBOX_API_KEY
Iyzico__SecretKey=YOUR_SANDBOX_SECRET_KEY
```

### ⚙️ Optional Variables (Defaults Var)

```bash
# Payment Settings
Iyzico__Currency=TRY
Iyzico__PaymentChannel=MOBILE
Iyzico__PaymentGroup=SUBSCRIPTION
Iyzico__TokenExpirationMinutes=30

# Callback
Iyzico__Callback__DeepLinkScheme=ziraai://payment-callback
Iyzico__Callback__FallbackUrl=https://ziraai-api-sit.up.railway.app/payment/callback

# Timeouts
Iyzico__Timeout__InitializeTimeoutSeconds=30
Iyzico__Timeout__VerifyTimeoutSeconds=30
Iyzico__Timeout__WebhookTimeoutSeconds=15

# Retries
Iyzico__Retry__MaxRetryAttempts=3
Iyzico__Retry__RetryDelayMilliseconds=1000
Iyzico__Retry__UseExponentialBackoff=true
```

### 🔄 Environment Farkları

| Setting | Staging (Sandbox) | Production (Live) |
|---------|-------------------|-------------------|
| BaseUrl | `https://sandbox-api.iyzipay.com` | `https://api.iyzipay.com` |
| ApiKey | Sandbox API Key | Production API Key |
| SecretKey | Sandbox Secret | Production Secret |
| FallbackUrl | `https://ziraai-api-sit.up.railway.app/payment/callback` | `https://api.ziraai.com/payment/callback` |
| DeepLinkScheme | `ziraai://payment-callback` | `ziraai://payment-callback` |

### ⚠️ Önemli Notlar

1. **Double underscore kullanımı:** `Iyzico__Callback__DeepLinkScheme`
   - ASP.NET Core nested configuration için `__` kullanır
   - Single underscore `_` ÇALIŞMAZ!

2. **appsettings.json override edilir:**
   - Railway variables > appsettings.Staging.json > appsettings.json
   - appsettings.Staging.json'da empty strings var (Railway override için)

3. **Production credentials:**
   - ⚠️ STAGING'DE KAPSAMLI TEST YAPIN!
   - iyzico merchant verification gerekli
   - Gerçek para ile işlem yapılacak

### 📊 Configuration Loading Priority

```
1. Railway Environment Variables    ← En yüksek (her zaman kazanır)
2. appsettings.{Environment}.json   ← Railway yoksa bu
3. appsettings.json                 ← En düşük (fallback)
```

### ✅ Test Checklist

Staging'e deploy ettikten sonra:

- [ ] Railway logs kontrol edildi
- [ ] `IyzicoOptions loaded successfully` log'u görüldü
- [ ] BaseUrl doğru (sandbox for staging)
- [ ] Payment initialize endpoint çalışıyor (Phase 6'da yapılacak)
- [ ] iyzico API connectivity var

### 🚫 GÜVENLİK

**✅ YAPILMASI GEREKENLER:**
- Credentials sadece Railway'de
- Her environment için farklı keys
- API key rotation (periyodik)
- Git'e asla credential commit etmeyin

**❌ YAPILMAMASI GEREKENLER:**
- appsettings.json'a gerçek credentials
- Production credentials staging'de kullanmayın
- Credentials'ı chat/email ile paylaşmayın
- Log'larda credential expose etmeyin

### 📚 İlgili Dokümantasyon

- [Implementation Plan](./SPONSOR_PAYMENT_IMPLEMENTATION_PLAN.md) - Ana plan
- [iyzico Integration Analysis](../iyzico-payment-integration-UPDATED.md) - Analiz
- [Database Migrations](./migrations/README.md) - SQL migrations
- [Environment Variables Complete Reference](../ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md) - Tüm env vars

### 🆘 Troubleshooting

**Problem:** Variables yüklenmedi
**Çözüm:** Variable isimleri case-sensitive, `__` kullanımı kontrol et

**Problem:** Authentication failed
**Çözüm:** API Key ve Secret Key doğru mu? BaseUrl environment'a uygun mu?

**Problem:** appsettings değerleri kullanılıyor
**Çözüm:** Railway variables deploy SONRASI aktif olur, redeploy gerekebilir

### 📞 Destek

- iyzico Support: https://support.iyzico.com/
- iyzico Docs: https://docs.iyzico.com/
- iyzico Status: https://status.iyzico.com/
