# Referral System Migration Guide

## 📋 Overview
Bu migration, ZiraAI referral (tavsiye) sistemini ekler:
- **4 yeni tablo**: ReferralCodes, ReferralTracking, ReferralRewards, ReferralConfigurations
- **2 tablo değişikliği**: UserSubscriptions (+ReferralCredits), Users (+RegistrationReferralCode)
- **8 varsayılan konfigürasyon**: Sistem ayarları (10 kredi/tavsiye, 30 gün expiry, vb.)

---

## 🚀 Migration Nasıl Çalıştırılır?

### Yöntem 1: Manuel SQL Script (Önerilen - Hızlı)

#### Development Database
```bash
# PostgreSQL'e bağlan
psql -h localhost -p 5432 -U ziraai -d ziraai_dev

# SQL script'i çalıştır
\i 'DataAccess/Migrations/Pg/Manual/20251003_AddReferralSystem.sql'

# Çıkış
\q
```

#### Railway/Production Database
```bash
# Railway PostgreSQL'e bağlan
psql postgres://default:PASSWORD@HOST:PORT/railway

# SQL script'i çalıştır
\i 'DataAccess/Migrations/Pg/Manual/20251003_AddReferralSystem.sql'

# Çıkış
\q
```

#### Alternatif: SQL Dosyasını Pipe ile Çalıştırma
```bash
# Development
psql -h localhost -p 5432 -U ziraai -d ziraai_dev -f DataAccess/Migrations/Pg/Manual/20251003_AddReferralSystem.sql

# Railway
psql "postgresql://default:PASSWORD@HOST:PORT/railway" -f DataAccess/Migrations/Pg/Manual/20251003_AddReferralSystem.sql
```

#### Alternatif: Windows'ta PowerShell
```powershell
# Development
Get-Content "DataAccess\Migrations\Pg\Manual\20251003_AddReferralSystem.sql" | psql -h localhost -p 5432 -U ziraai -d ziraai_dev

# Railway
Get-Content "DataAccess\Migrations\Pg\Manual\20251003_AddReferralSystem.sql" | psql "postgresql://default:PASSWORD@HOST:PORT/railway"
```

---

### Yöntem 2: EF Core Migration (Code-First)

#### 1. Migration Oluştur
```bash
cd "C:\Users\Asus\Documents\Visual Studio 2022\ziraai"

dotnet ef migrations add AddReferralSystem `
  --project DataAccess `
  --startup-project WebAPI `
  --context ProjectDbContext `
  --output-dir Migrations/Pg
```

#### 2. Migration'ı Uygula
```bash
# Development
dotnet ef database update `
  --project DataAccess `
  --startup-project WebAPI `
  --context ProjectDbContext

# Production (connection string override)
dotnet ef database update `
  --project DataAccess `
  --startup-project WebAPI `
  --context ProjectDbContext `
  --connection "Host=railway-host;Port=5432;Database=railway;Username=postgres;Password=xxx"
```

#### 3. SQL Script Oluştur (Opsiyonel)
```bash
# Migration'dan SQL script üret
dotnet ef migrations script `
  --project DataAccess `
  --startup-project WebAPI `
  --context ProjectDbContext `
  --output DataAccess/Migrations/Pg/Manual/AddReferralSystem_Generated.sql
```

---

## ✅ Migration Doğrulama

### 1. Tabloların Oluşturulduğunu Kontrol Et
```sql
-- 4 yeni tablo olmalı
SELECT table_name,
       (SELECT COUNT(*)
        FROM information_schema.columns
        WHERE table_name = t.table_name
          AND table_schema = 'public') as column_count
FROM information_schema.tables t
WHERE table_schema = 'public'
  AND table_name IN ('ReferralCodes', 'ReferralTracking', 'ReferralRewards', 'ReferralConfigurations')
ORDER BY table_name;
```

**Beklenen Çıktı:**
```
table_name               | column_count
-------------------------+-------------
ReferralCodes           | 7
ReferralConfigurations  | 7
ReferralRewards         | 8
ReferralTracking        | 11
```

### 2. Konfigürasyonları Kontrol Et
```sql
-- 8 konfigürasyon olmalı
SELECT "Key", "Value", "DataType"
FROM public."ReferralConfigurations"
ORDER BY "Key";
```

**Beklenen Çıktı:**
```
Key                              | Value                    | DataType
---------------------------------+-------------------------+---------
Referral.CodePrefix              | ZIRA                     | string
Referral.CreditPerReferral       | 10                       | int
Referral.DeepLinkBaseUrl         | https://ziraai.com/ref/  | string
Referral.EnableSMS               | true                     | bool
Referral.EnableWhatsApp          | true                     | bool
Referral.LinkExpiryDays          | 30                       | int
Referral.MaxReferralsPerUser     | 0                        | int
Referral.MinAnalysisForValidation| 1                        | int
```

### 3. Yeni Kolonları Kontrol Et
```sql
-- UserSubscriptions.ReferralCredits
SELECT column_name, data_type, column_default
FROM information_schema.columns
WHERE table_name = 'UserSubscriptions'
  AND column_name = 'ReferralCredits';

-- Users.RegistrationReferralCode
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Users'
  AND column_name = 'RegistrationReferralCode';
```

### 4. Foreign Key'leri Kontrol Et
```sql
-- Tüm referral foreign key'lerini listele
SELECT
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_name LIKE 'Referral%'
ORDER BY tc.table_name;
```

---

## 🔄 Rollback (Geri Alma)

**⚠️ UYARI: Bu işlem tüm referral verilerini siler!**

### Rollback Script'i Çalıştır
```bash
# Development
psql -h localhost -p 5432 -U ziraai -d ziraai_dev -f DataAccess/Migrations/Pg/Manual/ROLLBACK_20251003_AddReferralSystem.sql

# Railway
psql "postgresql://default:PASSWORD@HOST:PORT/railway" -f DataAccess/Migrations/Pg/Manual/ROLLBACK_20251003_AddReferralSystem.sql
```

### EF Core Rollback
```bash
# Son migration'ı geri al
dotnet ef database update PreviousMigrationName `
  --project DataAccess `
  --startup-project WebAPI `
  --context ProjectDbContext

# Migration dosyasını sil
dotnet ef migrations remove `
  --project DataAccess `
  --startup-project WebAPI `
  --context ProjectDbContext
```

---

## 📊 Migration Özeti

| Özellik | Sayı | Detay |
|---------|------|-------|
| **Yeni Tablolar** | 4 | ReferralCodes, ReferralTracking, ReferralRewards, ReferralConfigurations |
| **Değişen Tablolar** | 2 | UserSubscriptions (+ReferralCredits), Users (+RegistrationReferralCode) |
| **Toplam Index** | 17 | Performance optimization için |
| **Default Config** | 8 | Sistem ayarları |
| **Foreign Keys** | 7 | Referential integrity |

---

## 🔧 Troubleshooting

### Problem: "permission denied for schema public"
```sql
-- Schema yetkilerini kontrol et
GRANT ALL ON SCHEMA public TO ziraai;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO ziraai;
```

### Problem: "relation already exists"
```sql
-- Mevcut tabloları kontrol et
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name LIKE 'Referral%';

-- Gerekirse manuel drop
DROP TABLE IF EXISTS public."ReferralRewards" CASCADE;
DROP TABLE IF EXISTS public."ReferralTracking" CASCADE;
DROP TABLE IF EXISTS public."ReferralCodes" CASCADE;
DROP TABLE IF EXISTS public."ReferralConfigurations" CASCADE;
```

### Problem: "column already exists"
```sql
-- Kolonları kontrol et
SELECT column_name
FROM information_schema.columns
WHERE table_name IN ('Users', 'UserSubscriptions')
  AND column_name IN ('RegistrationReferralCode', 'ReferralCredits');

-- Gerekirse manuel drop
ALTER TABLE public."Users" DROP COLUMN IF EXISTS "RegistrationReferralCode";
ALTER TABLE public."UserSubscriptions" DROP COLUMN IF EXISTS "ReferralCredits";
```

---

## 🎯 Sonraki Adımlar

Migration başarılı olduktan sonra:

1. ✅ **Entity Classes** oluştur (`Entities/Concrete/`)
2. ✅ **EF Configurations** ekle (`DataAccess/Concrete/Configurations/`)
3. ✅ **Repository Interfaces & Implementations** (`DataAccess/`)
4. ✅ **Business Services** (`Business/Services/Referral/`)
5. ✅ **API Controllers** (`WebAPI/Controllers/`)
6. ✅ **Integration** (RegisterUser, PlantAnalysis, SubscriptionUsage)

---

## 📞 Destek

Migration ile ilgili sorunlar için:
- Database logs: `SELECT * FROM public."Logs" WHERE "Level" = 'Error' ORDER BY "TimeStamp" DESC LIMIT 10;`
- PostgreSQL logs: `psql -c "SHOW log_directory;"`
- Entity Framework logs: `WebAPI/logs/dev/YYYYMMDD.txt`

---

**Migration Hazırlayan**: ZiraAI Development Team
**Tarih**: 2025-10-03
**Branch**: feature/referrer-tier-system
**Durum**: ✅ Ready for Deployment
