# Railway Environment Variables for Cloudflare R2

## Required Environment Variables

Railway dashboard'unuzda şu environment variable'ları eklemeniz gerekiyor:

### 1. CLOUDFLARE_R2_ACCOUNT_ID
**Nereden bulunur:**
1. Cloudflare Dashboard → R2
2. Sağ üst köşede "Manage R2 API Tokens" butonuna tıklayın
3. Sayfanın üstünde Account ID görünecektir

**Format:** 32 karakterlik alphanumeric string
**Örnek:** `a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6`

---

### 2. CLOUDFLARE_R2_ACCESS_KEY_ID
**Nereden bulunur:**
1. Cloudflare Dashboard → R2 → Manage R2 API Tokens
2. "Create API token" butonuna tıklayın
3. Permissions: "Object Read & Write" seçin
4. Token oluşturduğunuzda **Access Key ID** gösterilecektir

**⚠️ ÖNEMLİ:** Bu bilgi sadece bir kez gösterilir, kaydetmeyi unutmayın!

**Format:** 32 karakterlik alphanumeric string
**Örnek:** `1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p`

---

### 3. CLOUDFLARE_R2_SECRET_ACCESS_KEY
**Nereden bulunur:**
Token oluştururken Access Key ID ile birlikte gösterilir.

**⚠️ ÖNEMLİ:** Bu bilgi sadece bir kez gösterilir, güvenli bir yerde saklayın!

**Format:** 64+ karakterlik alphanumeric string
**Örnek:** `1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0t1u2v3w4x5y6z7a8b9c0d1e2f3g4h5i6j`

---

### 4. CLOUDFLARE_R2_PUBLIC_DOMAIN (Opsiyonel)

**İki seçenek:**

#### Seçenek A: R2 Auto-Generated URL (Önerilen - Hızlı Başlangıç)
Bu değişkeni **TANIMLAYMAYIN**. Kod otomatik olarak şu URL'i kullanacak:
```
https://ziraai-messages-prod.{ACCOUNT_ID}.r2.cloudflarestorage.com
```

**Artıları:**
- Anında çalışır, DNS ayarı gerekmez
- Cloudflare CDN otomatik aktif

**Eksileri:**
- Uzun ve karmaşık URL
- Brand URL yok (ziraai.com yerine cloudflarestorage.com)

#### Seçenek B: Custom Domain (Önerilen - Production için)
Custom domain kullanmak istiyorsanız:

1. **Cloudflare R2 Bucket Settings**
   - Bucket: `ziraai-messages-prod`
   - Settings → Public Access → "Allow Access" (read-only için)
   - Settings → Custom Domains → "Connect Domain"
   - Domain ekleyin: `cdn.ziraai.com` veya `images.ziraai.com`

2. **DNS Ayarları (Cloudflare DNS)**
   Cloudflare otomatik olarak CNAME kaydı ekleyecektir, ancak manuel kontrol:
   - Type: `CNAME`
   - Name: `cdn` (veya `images`)
   - Target: R2 bucket URL
   - Proxy: Enabled (🟠 Cloudflare proxy)

3. **Railway Environment Variable**
   ```bash
   CLOUDFLARE_R2_PUBLIC_DOMAIN=https://cdn.ziraai.com
   ```

**Artıları:**
- Professional, branded URL
- SEO friendly
- Custom cache rules
- Analytics

---

## Railway'de Environment Variables Nasıl Eklenir?

### Staging Environment
1. Railway Dashboard → `ziraai-api-staging` projesine git
2. Settings → Variables sekmesi
3. Şu değişkenleri ekle:
   ```bash
   CLOUDFLARE_R2_ACCOUNT_ID=your-account-id
   CLOUDFLARE_R2_ACCESS_KEY_ID=your-access-key
   CLOUDFLARE_R2_SECRET_ACCESS_KEY=your-secret-key
   # Opsiyonel:
   CLOUDFLARE_R2_PUBLIC_DOMAIN=https://cdn.ziraai.com
   ```
4. Deploy butonuna tıkla veya otomatik deploy'u bekle

### Production Environment
Aynı adımları production projesi için tekrarla.

---

## Bucket Konfigürasyonu

### Public Access Settings
Bucket'ınızda **Public Access** ayarlarını kontrol edin:

1. Cloudflare Dashboard → R2 → `ziraai-messages-prod`
2. Settings → Public Access
3. "Allow Access" seçeneğini aktif edin
4. Sadece **READ** operasyonları için public access
5. WRITE operasyonları API credentials gerektirir (otomatik güvende)

### CORS Ayarları (Opsiyonel)
Eğer browser'dan direct upload yapacaksanız:

```json
{
  "allowed_origins": ["https://ziraai.com", "https://app.ziraai.com"],
  "allowed_methods": ["GET", "PUT", "POST"],
  "allowed_headers": ["*"],
  "max_age_seconds": 3600
}
```

**Not:** Şu anki implementasyon backend'den upload yapıyor, CORS gerekmez.

---

## Güvenlik Kontrol Listesi

- [ ] API Token sadece **R2 Read & Write** yetkisine sahip (Admin değil!)
- [ ] Secret Access Key güvenli yerde saklandı (1Password, Railway Secrets)
- [ ] Public Access sadece **Read** için aktif
- [ ] Token rotation planı var (her 90 günde bir yenile)
- [ ] Railway Variables **şifrelendi** (otomatik)
- [ ] `.env` dosyası `.gitignore`'da (kod deposuna commit edilmedi)

---

## Test Etme

Environment variables'ı ekledikten sonra:

### 1. Railway Logs Kontrolü
Deploy sonrası logs'u kontrol edin:
```bash
[FileStorage DI] Selected provider: CloudflareR2
[CloudflareR2] Initialized - Bucket: ziraai-messages-prod, Domain: https://...
```

### 2. Test Upload
Postman veya API test:
```bash
POST /api/PlantAnalyses/analyze
{
  "image": "data:image/jpeg;base64,/9j/4AAQ...",
  "cropType": "tomato"
}
```

Response'da `imageUrl` kontrol edin:
```json
{
  "imageUrl": "https://cdn.ziraai.com/20251128_143022_abc123_image.jpg"
}
```

### 3. URL Erişilebilirlik
Browser'da dönen URL'i aç, resim görünmeli.

### 4. Cloudflare Dashboard
R2 → `ziraai-messages-prod` → Storage → Yeni dosya görünmeli

---

## Sorun Giderme

### "Account ID is not configured" Hatası
**Çözüm:** Railway'de `CLOUDFLARE_R2_ACCOUNT_ID` environment variable'ı doğru ayarlandığından emin olun.

### "Unauthorized" Hatası
**Çözüm:**
1. Access Key ID ve Secret doğru mu?
2. API Token R2 Read & Write yetkisine sahip mi?
3. Token expire olmamış mı?

### Dosyalar upload oluyor ama erişilemiyor
**Çözüm:**
1. Bucket Public Access "Allow" mu?
2. PublicDomain environment variable doğru mu?
3. Custom domain kullanıyorsanız DNS propagate oldu mu? (15-30 dakika)

### Custom Domain çalışmıyor
**Çözüm:**
1. DNS CNAME doğru eklendi mi? (`dig cdn.ziraai.com` ile kontrol)
2. Cloudflare proxy enabled mi? (🟠 orange cloud)
3. SSL/TLS certificate otomatik mı? (Cloudflare otomatik halleder)

---

## Maliyet Takibi

Environment variables eklendikten sonra:

1. **Cloudflare R2 Dashboard**
   - Overview → Usage sekmesi
   - Storage (GB)
   - Requests (Class A: PUT, Class B: GET)
   - **Egress: $0** (Cloudflare'in artısı!)

2. **Beklenen Maliyet (1K upload/ay)**
   - Storage: 0.25 GB × $0.015 = $0.00375
   - Class A (PUT): 1K × $4.50/million = $0.0045
   - Class B (GET): 10K × $0.36/million = $0.0036
   - **Toplam: ~$0.01/ay**

3. **Alert Kurulum**
   - Cloudflare → Notifications
   - R2 Usage Alerts ekle
   - Threshold: Monthly cost > $1

---

## Deployment Checklist

### Staging'e Deploy Öncesi
- [x] Code implementation complete
- [x] Configuration files updated
- [x] Build successful
- [ ] **Cloudflare R2 Account ID alındı**
- [ ] **API Token oluşturuldu (Access Key ID + Secret)**
- [ ] **Railway environment variables eklendi**
- [ ] **Bucket public access enabled**
- [ ] Deploy ve logs kontrolü

### Production'a Deploy Öncesi
- [ ] Staging'de 24 saat test edildi
- [ ] Custom domain DNS ayarları yapıldı (opsiyonel)
- [ ] Production API token oluşturuldu (staging'den farklı)
- [ ] Production Railway variables eklendi
- [ ] Cost alerts kuruldu
- [ ] Monitoring dashboard hazır

---

**Son Güncelleme:** 2025-11-28
**Bucket Adı:** `ziraai-messages-prod`
**Kod Dalı:** `feature/production-storage-service`
