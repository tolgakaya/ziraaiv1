# SQL Migration Guide - Sponsorship Queue System

## 📋 Genel Bilgi

**Migration Adı:** AddSponsorshipQueueSystem  
**Tarih:** 2025-10-07  
**SQL Dosyası:** `claudedocs/AddSponsorshipQueueSystem.sql`

---

## 🎯 Bu Migration Ne Yapıyor?

1. **UserSubscriptions Tablosu:**
   - Sponsorluk sıralaması için 4 yeni alan ekliyor
   - Mevcut kayıtları otomatik güncelliyor (QueueStatus, ActivatedDate)

2. **PlantAnalyses Tablosu:**
   - Sponsor attribution tracking için 2 yeni alan ekliyor
   - Logo gösterimi ve erişim kontrolü için gerekli

3. **İndeksler:**
   - Queue sorguları için performans indeksleri
   - Sponsor filtering için composite indeksler

---

## 🚀 Ortamlara Göre Kullanım

### 1️⃣ **Development (Localhost)**

```bash
# PostgreSQL'e bağlan
psql -U postgres -d ziraai_dev

# SQL script'i çalıştır
\i 'C:/Users/Asus/Documents/Visual Studio 2022/ziraai/claudedocs/AddSponsorshipQueueSystem.sql'

# Veya direkt
psql -U postgres -d ziraai_dev -f "claudedocs/AddSponsorshipQueueSystem.sql"
```

**Doğrulama:**
```sql
-- Yeni kolonları kontrol et
\d "UserSubscriptions"
\d "PlantAnalyses"

-- İndeksleri kontrol et
\di *Queue*
\di *Sponsor*
```

---

### 2️⃣ **Staging (Railway)**

**Yöntem 1: Railway Dashboard (Önerilen)**
```bash
1. Railway Dashboard → ziraai-api-sit → PostgreSQL
2. "Data" tab'ına git
3. SQL editor'de script'i yapıştır ve çalıştır
```

**Yöntem 2: Railway CLI**
```bash
# Railway CLI ile bağlan
railway login
railway link

# Local'den SQL çalıştır
railway run psql $DATABASE_URL -f claudedocs/AddSponsorshipQueueSystem.sql

# Veya direkt bağlanıp yapıştır
railway run psql $DATABASE_URL
-- SQL script içeriğini yapıştır
```

**Yöntem 3: pgAdmin / DBeaver (GUI)**
```bash
1. Railway'den connection string al (Settings → Variables → DATABASE_URL)
2. pgAdmin/DBeaver'da connection oluştur
3. Query tool'da SQL script'i çalıştır
```

---

### 3️⃣ **Production**

**⚠️ ÖNEMLİ: Production'da Dikkat Edilecekler**

**Hazırlık:**
1. **Backup al:**
   ```bash
   railway run pg_dump $DATABASE_URL > backup_before_queue_migration_$(date +%Y%m%d).sql
   ```

2. **Downtime planla** (veya):
   - Script sadece ALTER TABLE/CREATE INDEX içeriyor
   - PostgreSQL'de bunlar genellikle non-blocking
   - Ama büyük tablolarda (>1M kayıt) index oluşturma zaman alabilir

**Migration Çalıştırma:**
```bash
# Option 1: Railway CLI
railway run psql $DATABASE_URL -f claudedocs/AddSponsorshipQueueSystem.sql

# Option 2: Railway Dashboard
# SQL editor'de çalıştır (yukarıda anlatıldı)
```

**Doğrulama:**
```sql
-- 1. Kolonları kontrol et
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'UserSubscriptions' 
  AND column_name IN ('QueueStatus', 'QueuedDate', 'ActivatedDate', 'PreviousSponsorshipId');

-- 2. Mevcut kayıtların güncellenmesini kontrol et
SELECT 
    COUNT(*) as total_sponsored,
    COUNT(CASE WHEN "QueueStatus" = 1 THEN 1 END) as active_count,
    COUNT(CASE WHEN "QueueStatus" = 2 THEN 1 END) as expired_count,
    COUNT(CASE WHEN "ActivatedDate" IS NOT NULL THEN 1 END) as has_activated_date
FROM "UserSubscriptions"
WHERE "IsSponsoredSubscription" = true;

-- 3. İndeksleri kontrol et
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename IN ('UserSubscriptions', 'PlantAnalyses')
  AND (indexname LIKE '%Queue%' OR indexname LIKE '%Sponsor%')
ORDER BY tablename, indexname;
```

---

## 🔍 Migration Adımları (Detaylı)

### Step 1: UserSubscriptions - Yeni Kolonlar
```sql
QueueStatus INTEGER NOT NULL DEFAULT 1
QueuedDate TIMESTAMP NULL
ActivatedDate TIMESTAMP NULL
PreviousSponsorshipId INTEGER NULL
```

**Neden Default 1?**
- Mevcut tüm kayıtlar "Active" (1) olarak ayarlanır
- Backward compatible

### Step 2: Mevcut Kayıtları Güncelle
```sql
-- ActivatedDate = StartDate (mevcut sponsorlar için)
-- QueueStatus = EndDate'e göre (Active/Expired)
```

**Etki:**
- Tüm mevcut sponsored subscriptions için `ActivatedDate` set edilir
- `QueueStatus` doğru değere ayarlanır

### Step 3-4: Constraints ve İndeksler
```sql
FK_UserSubscriptions_PreviousSponsorship
IX_UserSubscriptions_QueueStatus
IX_UserSubscriptions_Queue_Lookup
IX_UserSubscriptions_Sponsored_Active
```

**Performans:**
- Index oluşturma büyük tablolarda zaman alabilir
- CONCURRENT kullanılmadı (downtime gerekir)

### Step 5-7: PlantAnalysis Sponsor Attribution
```sql
ActiveSponsorshipId INTEGER NULL
SponsorCompanyId INTEGER NULL
+ Foreign Keys + Indexes
```

**Etki:**
- Mevcut analizler NULL kalır (normal)
- Yeni analizler sponsor bilgisi yakalayacak

---

## 📊 Beklenen Süre (Tahmini)

| Tablo Boyutu | Süre (yaklaşık) |
|--------------|----------------|
| < 10K kayıt  | 1-2 saniye     |
| 10K - 100K   | 5-10 saniye    |
| 100K - 1M    | 30-60 saniye   |
| > 1M         | 2-5 dakika     |

**Not:** İndeks oluşturma en uzun süren işlem

---

## ⚠️ Sorun Giderme

### Hata: "column already exists"
```sql
-- Kolon zaten varsa skip et (script tekrar çalıştırıldığında)
-- Hata mesajına bakılmaksızın devam edilebilir
```

### Hata: "relation already exists" (index)
```sql
-- Index zaten varsa skip et
DROP INDEX IF EXISTS index_name;
-- Sonra tekrar CREATE
```

### Hata: Foreign Key Violation
```sql
-- PreviousSponsorshipId invalid bir ID içeriyor
-- Kontrol et:
SELECT * FROM "UserSubscriptions" 
WHERE "PreviousSponsorshipId" IS NOT NULL 
  AND "PreviousSponsorshipId" NOT IN (SELECT "Id" FROM "UserSubscriptions");
```

### Timeout Hatası (Büyük Tablolarda)
```bash
# Timeout süresini artır
psql -U postgres -d database_name -c "SET statement_timeout = '10min';"
# Sonra script'i çalıştır
```

---

## 🔄 Rollback (Geri Alma)

Eğer migration sonrası sorun çıkarsa:

```sql
-- Script sonundaki ROLLBACK bölümünü çalıştır
-- Veya:

-- PlantAnalyses değişikliklerini geri al
ALTER TABLE "PlantAnalyses" DROP CONSTRAINT IF EXISTS "FK_PlantAnalyses_SponsorCompany";
ALTER TABLE "PlantAnalyses" DROP CONSTRAINT IF EXISTS "FK_PlantAnalyses_ActiveSponsorship";
DROP INDEX IF EXISTS "IX_PlantAnalyses_ActiveSponsorship";
DROP INDEX IF EXISTS "IX_PlantAnalyses_SponsorCompany";
DROP INDEX IF EXISTS "IX_PlantAnalyses_UserSponsor";
ALTER TABLE "PlantAnalyses" DROP COLUMN IF EXISTS "SponsorCompanyId";
ALTER TABLE "PlantAnalyses" DROP COLUMN IF EXISTS "ActiveSponsorshipId";

-- UserSubscriptions değişikliklerini geri al
ALTER TABLE "UserSubscriptions" DROP CONSTRAINT IF EXISTS "FK_UserSubscriptions_PreviousSponsorship";
DROP INDEX IF EXISTS "IX_UserSubscriptions_Sponsored_Active";
DROP INDEX IF EXISTS "IX_UserSubscriptions_Queue_Lookup";
DROP INDEX IF EXISTS "IX_UserSubscriptions_QueueStatus";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "PreviousSponsorshipId";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "ActivatedDate";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "QueuedDate";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "QueueStatus";
```

---

## ✅ Post-Migration Checklist

- [ ] Backup alındı mı?
- [ ] SQL script çalıştırıldı mı?
- [ ] Doğrulama sorguları çalışıyor mu?
- [ ] Uygulama çalışıyor mu? (`dotnet run`)
- [ ] Test senaryoları:
  - [ ] Trial user sponsorship redeem (immediate activation)
  - [ ] Active sponsor → new sponsor redeem (queue)
  - [ ] PlantAnalysis creation (sponsor attribution)
- [ ] Log'larda hata var mı?
- [ ] Railway/Production'da API response doğru mu?

---

## 📞 Yardım

**Migration hakkında sorular:**
- SQL script: `claudedocs/AddSponsorshipQueueSystem.sql`
- Tasarım: `claudedocs/SPONSORSHIP_QUEUE_SYSTEM_DESIGN.md`
- Özet: `claudedocs/SPONSORSHIP_QUEUE_IMPLEMENTATION_SUMMARY.md`

**Railway Docs:**
- https://docs.railway.app/databases/postgresql

**PostgreSQL Docs:**
- ALTER TABLE: https://www.postgresql.org/docs/current/sql-altertable.html
- CREATE INDEX: https://www.postgresql.org/docs/current/sql-createindex.html
