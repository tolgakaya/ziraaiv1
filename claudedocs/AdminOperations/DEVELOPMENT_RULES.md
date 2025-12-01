# ZiraAI Geliştirme Kuralları ve Süreç Rehberi

**Branch:** feature/production-readiness
**Environment:** Railway Staging
**Tarih:** 30 Kasım 2025

---

## 🔴 KRİTİK KURALLAR

### 1. Branch Yönetimi
- ✅ **SADECE** `feature/production-readiness` branch'inde çalış
- ✅ Tüm commit'ler bu branch'e push edilecek
- ✅ Railway Staging otomatik deploy edecek
- ❌ Asla başka branch'e commit atma
- ❌ Asla main/master'a direkt push yapma

**Doğrulama:**
```bash
git branch  # feature/production-readiness'de olduğundan emin ol
```

### 2. Build ve Test Süreci
- ✅ Her anlamlı aşamadan sonra build al
- ✅ Build hataları varsa düzelt
- ✅ Dependency hatalarına DİKKAT et
- ❌ Build almadan commit atma

**Build Komutu:**
```bash
dotnet build
# Hata kontrolü - exit code 0 olmalı
echo $?
```

### 3. Database Migration
- ✅ Migration'lar **SADECE SQL script** olarak
- ✅ Script'i `claudedocs/AdminOperations/migrations/` klasörüne kaydet
- ✅ Manuel olarak Railway Staging PostgreSQL'e uygula
- ❌ EF Core migration komutları kullanma (production risk)

**Migration Template:**
```sql
-- Migration: [FeatureName]
-- Date: YYYY-MM-DD
-- Author: Claude
-- Branch: feature/production-readiness

-- Apply:
ALTER TABLE "TableName" ADD COLUMN "NewColumn" TYPE;

-- Rollback:
ALTER TABLE "TableName" DROP COLUMN "NewColumn";
```

### 4. Dokümantasyon Kuralı
- ✅ **TÜM** dokümanlar `claudedocs/AdminOperations/` içinde
- ✅ Her endpoint için API dokümanı oluştur
- ✅ Her aşamada geliştirme planını güncelle
- ❌ Dışarıda (root, vb.) doküman oluşturma

**Klasör Yapısı:**
```
claudedocs/AdminOperations/
├── DEVELOPMENT_PLAN.md           # Ana plan (güncel tut)
├── DEVELOPMENT_RULES.md          # Bu dosya
├── API_DOCUMENTATION.md          # Endpoint dokümanları
├── operation_claims.csv          # Mevcut claim'ler
├── migrations/                   # SQL migration scriptleri
└── completed/                    # Tamamlanan işler arşivi
```

### 5. SecuredOperation Kullanımı
- ✅ `SECUREDOPERATION_GUIDE.md` dosyasını OKU
- ✅ `SponsorAnalytics` endpoint yapısını örnek al
- ✅ `OperationClaims` ve `GroupClaims` ilişkisine dikkat et
- ✅ `operation_claims.csv` dosyasındaki claim'leri kontrol et
- ❌ Yeni claim oluştururken SQL script unutma

**Kontrol Listesi:**
- [ ] Handler'da `[SecuredOperation]` attribute ekledim
- [ ] Doğru claim name kullandım (csv'de var mı?)
- [ ] Group'a claim ataması için SQL script yazdım
- [ ] API dokümantasyonuna authorization bilgisi ekledim

### 6. Geriye Uyumluluk (Backward Compatibility)
- ✅ Yeni geliştirme mevcut feature'ları bozmamalı
- ✅ Örnek: Bayi ID eklerken sponsor yetenekleri korunmalı
- ✅ Her değişiklik sonrası ilgili feature'ı test et
- ❌ Breaking change yapma (production'da çalışan şeyler bozulmasın)

**Test Checklist:**
```markdown
Değişiklik: [FeatureName]
Etkilenen Feature'lar:
- [ ] Feature 1: Test edildi, çalışıyor ✅
- [ ] Feature 2: Test edildi, çalışıyor ✅
- [ ] Feature 3: Test edildi, çalışıyor ✅
```

### 7. Backend Odaklı Geliştirme
- ✅ Sadece backend/API geliştirme yap
- ✅ UI geliştirme yapma (mobile/frontend ekibi yapacak)
- ✅ Her endpoint için amaç ve kullanım senaryosu açıkla
- ✅ Request/Response yapısını detaylı dokümante et

**API Doküman Template:**
```markdown
## Endpoint: [Name]

**Amaç:** [Ne için kullanılacak]
**Kullanıcı:** Admin | Farmer | Sponsor
**Version:** v1 | v2 | none

### Request
- Method: GET | POST | PUT | DELETE
- URL: /api/v1/endpoint
- Headers: Authorization, Content-Type
- Body: {...}

### Response
- Success (200): {...}
- Error (400/401/403/500): {...}

### Kullanım Senaryosu
1. [Adım 1]
2. [Adım 2]
```

### 8. API Versiyonlama
- ✅ Farmer endpoints: `/api/v1/` kullan
- ✅ Admin endpoints: `/api/` kullan (versiyon yok)
- ✅ Mevcut pattern'i takip et

**Kontrol:**
```csharp
// Farmer endpoint
[Route("api/v1/farmers")]  // ✅ Versiyonlu

// Admin endpoint
[Route("api/admin/sponsors")]  // ✅ Versiyonsuz
```

### 9. Configuration Yönetimi
- ✅ Railway environment variables kullan
- ✅ `appsettings.Staging.json` dosyasında placeholder kullan
- ✅ Storage Service config implementation'ını örnek al
- ❌ Hardcoded value kullanma

**Config Pattern:**
```json
// appsettings.Staging.json
{
  "FeatureName": {
    "Setting1": "${FEATURE_SETTING1}",
    "Setting2": "${FEATURE_SETTING2:default_value}"
  }
}
```

**Railway Environment Variables:**
```
FEATURE_SETTING1=value1
FEATURE_SETTING2=value2
```

### 10. Geliştirme Planı Takibi
- ✅ `DEVELOPMENT_PLAN.md` dosyasını her aşamada güncelle
- ✅ Session kaybında bu plan üzerinden devam et
- ✅ Compact/summary durumlarında plan kritik
- ❌ Plan güncel değilse context kaybolur

**Plan Yapısı:**
```markdown
# Geliştirme Planı

## Durum: [In Progress | Completed | Blocked]

### Tamamlanan İşler
- [x] İş 1
- [x] İş 2

### Devam Eden İşler
- [ ] İş 3 (50% - detay)

### Bekleyen İşler
- [ ] İş 4
- [ ] İş 5

### Blocker'lar
- Issue 1: Açıklama

### Sonraki Adımlar
1. Adım 1
2. Adım 2
```

---

## 🔧 Railway Staging Workflow

### 1. Kod Geliştirme
```bash
# 1. Branch kontrolü
git branch  # feature/production-readiness'de olmalı

# 2. Kod yaz/değiştir
# ...

# 3. Build al
dotnet build

# 4. Build başarılı mı kontrol et
echo $?  # 0 olmalı

# 5. Commit
git add .
git commit -m "feat: [feature description]"

# 6. Push (Railway otomatik deploy eder)
git push origin feature/production-readiness
```

### 2. Railway Deployment Takibi
```bash
# Railway logs izle
railway logs --tail 100

# Service status kontrol
railway status

# Environment variables kontrol
railway variables
```

### 3. Test (Railway Staging)
```bash
# API endpoint test
curl -X POST https://ziraai-api-staging.up.railway.app/api/endpoint \
  -H "Authorization: Bearer $STAGING_JWT" \
  -H "Content-Type: application/json" \
  -d '{...}'

# Database kontrol
railway connect postgres
# SQL sorguları çalıştır
```

---

## 📋 Her Endpoint Geliştirme Checklist

### Pre-Development
- [ ] `DEVELOPMENT_PLAN.md` güncelle (yeni iş ekle)
- [ ] Mevcut kodu incele (benzer endpoint var mı?)
- [ ] Claim'leri kontrol et (`operation_claims.csv`)
- [ ] API versiyonunu belirle (v1 mi, versiyonsuz mu?)

### Development
- [ ] Entity oluştur/güncelle
- [ ] DTO oluştur
- [ ] Command/Query handler yaz
- [ ] SecuredOperation ekle (gerekiyorsa)
- [ ] Controller endpoint ekle
- [ ] Validation ekle (FluentValidation)

### Testing
- [ ] Build al (`dotnet build`)
- [ ] Build başarılı (exit code 0)
- [ ] Migration gerekiyorsa SQL script yaz
- [ ] Railway Staging'e push et
- [ ] Endpoint'i Postman/curl ile test et
- [ ] Response yapısını doğrula

### Documentation
- [ ] API dokümanı yaz (`API_DOCUMENTATION.md`)
- [ ] Claim SQL script yaz (gerekiyorsa)
- [ ] Migration script ekle (gerekiyorsa)
- [ ] `DEVELOPMENT_PLAN.md` güncelle (tamamlandı olarak işaretle)

### Post-Development
- [ ] Geriye uyumluluk test et (etkilenen feature'lar)
- [ ] Railway logs kontrol et (hata var mı?)
- [ ] Performance kontrolü (yavaş mı?)

---

## 🚨 Sık Yapılan Hatalar ve Çözümleri

### Hata 1: SecuredOperation claim hatası
**Belirti:** 403 Forbidden, "User doesn't have required claim"
**Çözüm:**
1. `operation_claims.csv` kontrol et - claim var mı?
2. SQL script ile claim'i ekle
3. SQL script ile group'a claim ata
4. Test user'ı doğru group'ta mı kontrol et

### Hata 2: Dependency injection hatası
**Belirti:** Build hatası, "Service not registered"
**Çözüm:**
1. `Business/Startup.cs` kontrol et
2. `WebAPI/Startup.cs` kontrol et
3. Service registration ekle
4. Build tekrar al

### Hata 3: Migration hatası
**Belirti:** "Column does not exist"
**Çözüm:**
1. SQL migration script yaz
2. Railway Staging PostgreSQL'e bağlan
3. Script'i manuel çalıştır
4. Kontrol et: `SELECT * FROM table LIMIT 1;`

### Hata 4: Geriye uyumsuzluk
**Belirti:** Eski feature çalışmıyor
**Çözüm:**
1. Değişikliği geri al veya düzelt
2. Nullable field kullan (yeni eklenen için)
3. Default value belirle
4. Test et

---

## 📊 Geliştirme Metrikleri

Her geliştirme sonrası kaydet:

```markdown
## Metrikler

**Endpoint:** [name]
**Tarih:** [date]
**Süre:** [X hours]
**LOC:** [Lines of Code]
**Files Changed:** [X]

**Sorunlar:**
- Sorun 1: Çözüm
- Sorun 2: Çözüm

**Öğrenilenler:**
- [Key learning 1]
- [Key learning 2]
```

---

## 🎯 Başarı Kriterleri

Her geliştirme aşaması için:

- [ ] Build başarılı (exit code 0)
- [ ] Railway Staging deploy başarılı
- [ ] Endpoint çalışıyor (200/201 response)
- [ ] Geriye uyumlu (eski feature'lar çalışıyor)
- [ ] Doküman tamamlandı
- [ ] Migration script hazır (gerekiyorsa)
- [ ] Claim script hazır (gerekiyorsa)

---

**Son Güncelleme:** 30 Kasım 2025
**Branch:** feature/production-readiness
**Session:** Active
