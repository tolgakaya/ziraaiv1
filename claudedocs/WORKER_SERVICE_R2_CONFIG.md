# Worker Service - Cloudflare R2 Configuration

## ⚠️ CRITICAL: Worker Service Eksik Environment Variables

Worker servisinde CloudflareR2 provider seçilmiş ama bucket name ve credentials yok.

## 📋 Railway Worker Service Environment Variables

Railway dashboard'da **PlantAnalysisWorkerService** için şu variables'ları ekleyin:

```bash
# File Storage Provider Selection
FileStorage__Provider=CloudflareR2

# Cloudflare R2 Credentials
FileStorage__CloudflareR2__AccountId=YOUR_CLOUDFLARE_ACCOUNT_ID
FileStorage__CloudflareR2__AccessKeyId=YOUR_R2_ACCESS_KEY_ID
FileStorage__CloudflareR2__SecretAccessKey=YOUR_R2_SECRET_ACCESS_KEY
FileStorage__CloudflareR2__BucketName=ziraai-messages-prod
FileStorage__CloudflareR2__PublicDomain=https://1lik.net
```

## 🔍 Current Error in Logs

```
[Worker FileStorage DI] Selected provider: CloudflareR2
System.InvalidOperationException: Cloudflare R2 Bucket Name is not configured
  at CloudflareR2StorageService.ValidateConfiguration(...)
  at CloudflareR2StorageService..ctor(IConfiguration configuration, ILogger`1 logger)
```

**Root Cause:** Worker service appsettings.json'da CloudflareR2 default olarak seçilmiş ama Railway'de environment variables tanımlı değil.

## ✅ Çözüm Adımları

### 1. Railway Dashboard'da Worker Service'i Bul

1. Railway Dashboard → Projects → ZiraAI
2. **PlantAnalysisWorkerService** deployment'ını seç
3. Variables sekmesine git

### 2. Environment Variables Ekle

Yukarıdaki 6 environment variable'ı ekle:

- `FileStorage__Provider`
- `FileStorage__CloudflareR2__AccountId`
- `FileStorage__CloudflareR2__AccessKeyId`
- `FileStorage__CloudflareR2__SecretAccessKey`
- `FileStorage__CloudflareR2__BucketName`
- `FileStorage__CloudflareR2__PublicDomain`

**ÖNEMLİ:** Double underscore (`__`) kullanmayı unutmayın!

### 3. Redeploy Worker Service

Environment variables ekledikten sonra:
1. Railway otomatik redeploy yapacak
2. Veya manuel "Deploy Latest" butonuna basın

### 4. Logs Kontrolü

Deploy sonrası worker logs'unda şunları kontrol edin:

#### ✅ Başarılı Initialization
```
[Worker FileStorage DI] Selected provider: CloudflareR2
[CloudflareR2] Configuration validated - AccountId: YOUR_ACCOUNT_ID, Bucket: ziraai-messages-prod
[CloudflareR2] Initialized - Bucket: ziraai-messages-prod, Domain: https://1lik.net
```

#### ❌ Hala Hata Varsa
```
Cloudflare R2 Bucket Name is not configured
```
→ Environment variables doğru formatta değil veya deploy edilmemiş.

---

## 🎯 Neden Gerekli?

Worker Service şu işlevler için R2'yi kullanıyor:
1. **Async Plant Analysis:** RabbitMQ'dan gelen analysis request'lerde image upload
2. **Multi-Image Analysis:** Birden fazla image'ın upload edilmesi
3. **Background Jobs:** Hangfire üzerinden çalışan image işleme görevleri

WebAPI'de environment variables olsa bile, **Worker Service ayrı bir deployment** olduğu için kendi environment variables'larına ihtiyaç var.

---

## 📝 Değerleri Nereden Alacaksınız?

WebAPI'nin Railway environment variables'larındaki aynı değerleri kullanın:

1. Railway → WebAPI deployment → Variables sekmesi
2. `FileStorage__CloudflareR2__*` ile başlayan tüm değerleri kopyala
3. Worker Service deployment'ına aynı değerleri yapıştır

---

**Hazırlayan:** Claude Code
**Tarih:** 2025-11-28
**Issue:** Worker service bucket name missing
**Status:** ⏳ Pending Railway configuration
