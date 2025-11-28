# Staging Deployment Guide - Cloudflare R2

## 🎯 Amaç
Staging ortamında CloudflareR2 storage service'i test etmek ve production'a geçmeden önce doğrulamak.

---

## ✅ Staging Hazırlık (Yapıldı)

- [x] CloudflareR2StorageService implementation
- [x] appsettings.Staging.json CloudflareR2 default olarak ayarlandı
- [x] Bucket oluşturuldu: `ziraai-messages-prod`
- [x] Build başarılı

---

## 📋 Staging Deployment Adımları

### 1. Railway Staging Environment Variables

Railway dashboard'da **staging environment** için şu variables'ları ekleyin:

```bash
# Cloudflare R2 Configuration
CLOUDFLARE_R2_ACCOUNT_ID=your-account-id-here
CLOUDFLARE_R2_ACCESS_KEY_ID=your-access-key-id-here
CLOUDFLARE_R2_SECRET_ACCESS_KEY=your-secret-key-here

# Public Domain (opsiyonel - yoksa otomatik R2 URL kullanılır)
CLOUDFLARE_R2_PUBLIC_DOMAIN=https://pub-xxx.r2.dev/ziraai-messages-prod
```

**Not:** Aynı bucket'ı (`ziraai-messages-prod`) kullanıyoruz, production ile ayrı bucket'a gerek yok.

---

### 2. Cloudflare R2 Bucket Public Access

1. Cloudflare Dashboard → R2 → `ziraai-messages-prod`
2. Settings → Public Access
3. **"Allow Access"** seçeneğini aktif edin
4. Sadece READ operasyonları public, WRITE API credentials ile korunuyor

**R2 Dev URL'i:**
Settings → Public Access aktif olunca otomatik verilen URL:
```
https://pub-xxx.r2.dev/ziraai-messages-prod
```

Bu URL'i `CLOUDFLARE_R2_PUBLIC_DOMAIN` olarak kullanabilirsiniz.

---

### 3. Current Staging Config Check

Şu anki staging konfigürasyonu:

```json
{
  "FileStorage": {
    "Provider": "CloudflareR2",  // ✅ R2 default
    "CloudflareR2": {
      "AccountId": "${CLOUDFLARE_R2_ACCOUNT_ID}",
      "AccessKeyId": "${CLOUDFLARE_R2_ACCESS_KEY_ID}",
      "SecretAccessKey": "${CLOUDFLARE_R2_SECRET_ACCESS_KEY}",
      "BucketName": "ziraai-messages-prod",  // ✅ Correct bucket
      "PublicDomain": "${CLOUDFLARE_R2_PUBLIC_DOMAIN}"  // ✅ Env var
    }
  }
}
```

Her şey hazır! Sadece environment variables eklenmesi gerekiyor.

---

### 4. Deploy

Environment variables ekledikten sonra:

#### Otomatik Deploy
Railway otomatik deploy edecek (GitHub integration aktifse).

#### Manuel Deploy
Railway Dashboard → Deployments → "Deploy Latest"

---

### 5. Deployment Doğrulama

Deploy tamamlandıktan sonra Railway logs'unda şunları kontrol edin:

#### ✅ Başarılı DI Registration
```
[FileStorage DI] Selected provider: CloudflareR2
[CloudflareR2] Initialized - Bucket: ziraai-messages-prod, Domain: https://pub-xxx.r2.dev/ziraai-messages-prod
```

#### ❌ Hata Durumunda
```
Cloudflare R2 Account ID is not configured
```
→ Environment variables Railway'de doğru eklenmemiş.

---

## 🧪 Staging Test Senaryoları

### Test 1: Plant Analysis Upload

**Postman/API Request:**
```http
POST https://ziraai-api-sit.up.railway.app/api/PlantAnalyses/analyze
Authorization: Bearer YOUR_STAGING_TOKEN
Content-Type: application/json

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD...",
  "cropType": "tomato",
  "location": "Ankara",
  "urgencyLevel": "Medium"
}
```

**Beklenen Response:**
```json
{
  "success": true,
  "analysisId": "...",
  "imageInfo": {
    "imageUrl": "https://pub-xxx.r2.dev/ziraai-messages-prod/20251128_150234_abc123_image.jpg",
    "format": "jpg",
    "sizeKb": 245.8
  }
}
```

**Doğrulama Adımları:**
1. Response'daki `imageUrl`'i kopyala
2. Browser'da aç → Resim görünmeli ✅
3. Cloudflare R2 Dashboard → Storage → Dosya listede olmalı ✅
4. Railway logs → Upload başarılı logları ✅

---

### Test 2: Multi-Image Upload

```http
POST https://ziraai-api-sit.up.railway.app/api/PlantAnalyses/analyze-multi-image
```

**Body:**
```json
{
  "leafTopImage": "data:image/jpeg;base64,...",
  "leafBottomImage": "data:image/jpeg;base64,...",
  "plantOverviewImage": "data:image/jpeg;base64,...",
  "cropType": "tomato"
}
```

**Doğrulama:**
- 3 farklı URL dönmeli
- Her 3 URL browser'da erişilebilir olmalı
- R2 bucket'ta 3 dosya görünmeli

---

### Test 3: Delete Operation

**API Request:**
```http
DELETE https://ziraai-api-sit.up.railway.app/api/PlantAnalyses/{analysisId}
```

**Doğrulama:**
1. Analysis silindiğinde image de silinmeli
2. R2 Dashboard → Storage → Dosya kaybolmalı
3. Image URL artık 404 dönmeli

---

### Test 4: Error Scenarios

#### A. Invalid Credentials Test
1. Railway'de `CLOUDFLARE_R2_SECRET_ACCESS_KEY` yanlış değer verin
2. Redeploy
3. Upload dene → 500 error almalısınız
4. Logs'da: "Unauthorized" veya "Invalid credentials"

#### B. Network Timeout Test
Railway logs'unda timeout olup olmadığını kontrol edin (normal durumda olmamalı).

---

## 📊 Monitoring (Staging)

### Railway Logs
Deploy sonrası şunları izleyin:

```bash
# Başarılı upload
[CloudflareR2] Uploading file - Key: 20251128_150234_abc/image.jpg, Size: 245.8 KB
[CloudflareR2] Upload successful - URL: https://pub-xxx.r2.dev/...

# Başarılı delete
[CloudflareR2] Deleting file - Key: 20251128_150234_abc/image.jpg
[CloudflareR2] Delete successful
```

### Cloudflare R2 Dashboard
1. R2 → `ziraai-messages-prod`
2. Overview → Storage sekmesi
3. Request count ve storage usage kontrol edin

**Beklenen (ilk testler):**
- Storage: 0.001 GB (birkaç test resmi)
- Requests: <100 (upload + download testleri)
- Cost: $0.00

---

## 🚨 Rollback Plan (Staging)

Eğer R2 ile problem yaşarsanız:

### Quick Rollback (2 dakika)

Railway'de environment variable değiştirin:
```bash
# Eski ayar
FileStorage__Provider=CloudflareR2

# Yeni ayar (FreeImageHost'a dön)
FileStorage__Provider=FreeImageHost
```

Redeploy → FreeImageHost aktif olur.

**Logs'da kontrol:**
```
[FileStorage DI] Selected provider: FreeImageHost
```

---

## ✅ Staging Success Criteria

Staging'i başarılı saymak için:

- [ ] Railway environment variables eklendi
- [ ] Deploy başarılı
- [ ] Logs'da CloudflareR2 initialization başarılı
- [ ] Test 1: Single image upload çalışıyor
- [ ] Test 2: Multi-image upload çalışıyor
- [ ] Test 3: Delete operation çalışıyor
- [ ] Browser'dan image URL'lere erişiliyor
- [ ] R2 Dashboard'da dosyalar görünüyor
- [ ] 24 saat boyunca hata yok
- [ ] Cost $0.01'in altında

**Tüm kriterler sağlandığında → Production'a geçilebilir!**

---

## 📝 Staging Test Sonuçları (Manuel Doldurulacak)

| Test | Tarih | Sonuç | Notlar |
|------|-------|-------|--------|
| Deploy | 2025-11-__ | ⏳ | - |
| Single Upload | 2025-11-__ | ⏳ | - |
| Multi Upload | 2025-11-__ | ⏳ | - |
| Delete | 2025-11-__ | ⏳ | - |
| 24h Stability | 2025-11-__ | ⏳ | - |
| Cost Check | 2025-11-__ | ⏳ | - |

**Tüm testler ✅ olunca production'a geç.**

---

## 🔗 Useful Links

- **Railway Staging:** https://railway.app/project/ziraai-staging
- **Cloudflare R2 Dashboard:** https://dash.cloudflare.com/ → R2
- **API Staging Base URL:** https://ziraai-api-sit.up.railway.app
- **Postman Collection:** ZiraAI_Complete_API_Collection_v6.1.json

---

**Hazırlayan:** Claude Code
**Tarih:** 2025-11-28
**Branch:** feature/production-storage-service
**Status:** ✅ Ready for staging deployment
