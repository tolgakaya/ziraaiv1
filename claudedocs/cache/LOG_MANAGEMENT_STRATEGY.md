# Log Yönetim ve Temizlik Stratejisi

**Proje**: ZiraAI Platform
**Veritabanı**: PostgreSQL
**Güncelleme**: 2025-12-05
**Durum**: 🔴 PRODUCTION HAZıRLIĞI KRİTİK

---

## 📋 Executive Summary

ZiraAI platformunda **5 farklı log tablosu** bulunmakta ve production'a geçmeden önce **mutlaka log retention policy ve otomatik temizlik mekanizması** kurulmalıdır.

### Kritik Bulgular:
- ❌ Otomatik log temizlik mekanizması YOK
- ❌ Log rotation stratejisi YOK
- ❌ Archive mekanizması YOK
- ✅ Index'ler mevcut (performans için iyi)
- ⚠️ AdminOperationLogs ve SubscriptionUsageLogs süresiz büyüyebilir

---

## 🗂️ Log Tabloları Analizi

### 1. AdminOperationLogs (Audit Trail) 🔴 KRİTİK

**Amaç**: Admin işlemlerinin audit trail kaydı (compliance için kritik)

**Kolonlar**:
- AdminUserId, TargetUserId (FK Users)
- Action, EntityType, EntityId
- IsOnBehalfOf (admin başka kullanıcı adına işlem yapıyor)
- IpAddress, UserAgent, RequestPath
- RequestPayload, ResponseStatus, Duration
- **Timestamp** (retention için kritik)
- BeforeState, AfterState (JSON - değişim takibi)

**Index'ler** (7 adet):
```sql
IX_AdminOperationLogs_Action
IX_AdminOperationLogs_AdminUserId
IX_AdminOperationLogs_AdminUserId_Timestamp (composite)
IX_AdminOperationLogs_IsOnBehalfOf (partial - WHERE IsOnBehalfOf = true)
IX_AdminOperationLogs_TargetUserId (partial - WHERE NOT NULL)
IX_AdminOperationLogs_TargetUserId_Timestamp (composite, partial)
IX_AdminOperationLogs_Timestamp (DESC) ✅ Temizlik için kritik
```

**Retention Önerisi**:
- **Hot Data (1 yıl)**: Production database'de tut
- **Cold Data (2-5 yıl)**: Archive database'e taşı (compliance için)
- **Delete After**: 5 yıl sonra sil (yasal zorunluluk yoksa)

**Büyüme Tahmini**:
- Admin sayısı: ~10
- Günlük işlem: ~500 (user management, on-behalf-of, system changes)
- Aylık: ~15,000 kayıt
- Yıllık: ~180,000 kayıt
- Ortalama row size: ~2KB (BeforeState/AfterState JSON)
- **Yıllık büyüme**: ~360 MB

**GDPR/KVKK Compliance**: ⚠️ DİKKAT
- BeforeState/AfterState'de kişisel veri olabilir
- Kullanıcı silindiğinde ON DELETE CASCADE (✅ otomatik temizleniyor)
- Anonim hale getirme gerekebilir (GDPR "right to be forgotten")

---

### 2. SubscriptionUsageLogs (Billing) 🔴 KRİTİK

**Amaç**: Subscription kullanım takibi (billing ve analytics için)

**Kolonlar**:
- UserId, UserSubscriptionId, PlantAnalysisId (FK)
- **UsageDate** (retention için kritik)
- RequestType, QuotaUsed
- IsSuccess, ErrorMessage
- IpAddress, DeviceInfo

**Retention Önerisi**:
- **Hot Data (3 ay)**: Production database (active billing cycle)
- **Cold Data (7 yıl)**: Archive (vergi kanunu gereği fatura kayıtları 7 yıl saklanmalı)
- **Delete After**: 7 yıl (muhasebe zorunluluğu)

**Büyüme Tahmini**:
- Aktif kullanıcı: ~10,000
- Günlük ortalama analiz: ~5,000
- Aylık: ~150,000 kayıt
- Yıllık: ~1,800,000 kayıt
- Ortalama row size: ~500 bytes
- **Yıllık büyüme**: ~900 MB

**⚠️ BİLLİNG KRİTİK**: Bu loglar kesinlikle silinmemeli, archive edilmeli!

---

### 3. SmsLogs (SMS İşlemleri)

**Amaç**: SMS gönderim logları (debugging ve maliyet takibi)

**Kolonlar**:
- SenderUserId (FK Users)
- Action, PhoneNumber
- Message, Status
- **CreatedDate** (retention için kritik)
- Provider, Cost

**Index'ler** (3 adet):
```sql
IX_SmsLogs_Action
IX_SmsLogs_CreatedDate ✅ Temizlik için kritik
IX_SmsLogs_SenderUserId
```

**Retention Önerisi**:
- **Hot Data (30 gün)**: Production database
- **Cold Data (1 yıl)**: Archive (maliyet analizi için)
- **Delete After**: 1 yıl

**Büyüme Tahmini**:
- Günlük SMS: ~100 (OTP, notifications)
- Aylık: ~3,000 kayıt
- Yıllık: ~36,000 kayıt
- Ortalama row size: ~300 bytes
- **Yıllık büyüme**: ~11 MB

**KVKK Compliance**: ⚠️ Telefon numarası kişisel veri (maskeleme gerekebilir)

---

### 4. MobileLogins (Mobil Giriş Logları)

**Amaç**: Mobil uygulama login takibi (security ve analytics)

**Kolonlar**:
- UserId (FK Users)
- ExternalUserId, Provider (Google/Apple)
- DeviceInfo, LoginDate
- IpAddress

**Index'ler** (1 adet):
```sql
IX_MobileLogins_ExternalUserId_Provider (composite)
```

**Retention Önerisi**:
- **Hot Data (90 gün)**: Production database
- **Cold Data (1 yıl)**: Archive (security audit için)
- **Delete After**: 1 yıl

**Büyüme Tahmini**:
- Günlük login: ~5,000
- Aylık: ~150,000 kayıt
- Yıllık: ~1,800,000 kayıt
- Ortalama row size: ~200 bytes
- **Yıllık büyüme**: ~360 MB

**KVKK Compliance**: IP adresi kişisel veri sayılabilir

---

### 5. Logs (Genel Uygulama Logları)

**Amaç**: Genel application logs (debugging)

**Kolonlar**:
- Level (INFO, ERROR, WARN)
- Message, Exception
- **Timestamp** (retention için kritik)

**Retention Önerisi**:
- **Hot Data (7 gün)**: Production database
- **Cold Data (30 gün)**: Archive veya external logging (CloudWatch/Sentry)
- **Delete After**: 30 gün

**Büyüme Tahmini**:
- Günlük log: ~50,000 (high traffic)
- Aylık: ~1,500,000 kayıt
- Yıllık: ~18,000,000 kayıt
- Ortalama row size: ~500 bytes
- **Yıllık büyüme**: ~9 GB (EN YÜKSEK!)

**⚠️ PERFORMANS KRİTİK**: En hızlı büyüyen tablo, external logging'e geçilmeli!

---

## 📊 Toplam Büyüme Tahmini

| Tablo | Yıllık Kayıt | Yıllık Büyüme | Retention | Öncelik |
|-------|-------------|--------------|-----------|---------|
| AdminOperationLogs | 180,000 | 360 MB | 1 yıl hot, 5 yıl cold | 🔴 HIGH |
| SubscriptionUsageLogs | 1,800,000 | 900 MB | 3 ay hot, 7 yıl cold | 🔴 CRITICAL |
| SmsLogs | 36,000 | 11 MB | 30 gün hot, 1 yıl cold | 🟡 MEDIUM |
| MobileLogins | 1,800,000 | 360 MB | 90 gün hot, 1 yıl cold | 🟡 MEDIUM |
| Logs | 18,000,000 | 9 GB | 7 gün hot, 30 gün cold | 🔴 CRITICAL |
| **TOPLAM** | **21,816,000** | **~11 GB/yıl** | - | - |

**Railway Free Tier**: 512 MB database limit
**Railway Pro Plan**: 8 GB database ($20/month)

**⚠️ UYARI**: Log tabloları optimize edilmezse 1 yılda 11 GB büyüme!

---

## 🛠️ Çözüm Stratejileri

### Strateji 1: Otomatik Log Rotation (PostgreSQL) ⭐ ÖNERİLEN

**Avantajlar**:
- Veritabanı içinde tamamen otomatik
- Cron job ile scheduled (günlük/haftalık)
- Transaction güvenli
- Archive tablosu ile yedekleme

**Uygulama**:

#### 1.1. Archive Tabloları Oluştur

```sql
-- AdminOperationLogs Archive
CREATE TABLE "AdminOperationLogs_Archive" (LIKE "AdminOperationLogs" INCLUDING ALL);

-- SubscriptionUsageLogs Archive
CREATE TABLE "SubscriptionUsageLogs_Archive" (LIKE "SubscriptionUsageLogs" INCLUDING ALL);

-- SmsLogs Archive
CREATE TABLE "SmsLogs_Archive" (LIKE "SmsLogs" INCLUDING ALL);

-- MobileLogins Archive
CREATE TABLE "MobileLogins_Archive" (LIKE "MobileLogins" INCLUDING ALL);

-- Logs Archive
CREATE TABLE "Logs_Archive" (LIKE "Logs" INCLUDING ALL);
```

#### 1.2. Temizlik Fonksiyonu (Stored Procedure)

```sql
-- ============================================================================
-- LOG CLEANUP FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION cleanup_old_logs()
RETURNS TABLE(
    table_name text,
    archived_count bigint,
    deleted_count bigint,
    operation_time interval
) AS $$
DECLARE
    start_time timestamp;
    archive_count bigint;
    delete_count bigint;
BEGIN
    -- ========================================================================
    -- 1. AdminOperationLogs: Archive > 1 year, Delete > 5 years
    -- ========================================================================
    start_time := clock_timestamp();

    -- Archive (1 year old → 5 years old)
    INSERT INTO "AdminOperationLogs_Archive"
    SELECT * FROM "AdminOperationLogs"
    WHERE "Timestamp" < NOW() - INTERVAL '1 year'
    AND "Timestamp" >= NOW() - INTERVAL '5 years';

    GET DIAGNOSTICS archive_count = ROW_COUNT;

    -- Delete archived records from main table
    DELETE FROM "AdminOperationLogs"
    WHERE "Timestamp" < NOW() - INTERVAL '1 year'
    AND "Timestamp" >= NOW() - INTERVAL '5 years';

    -- Delete > 5 years from archive (compliance limit)
    DELETE FROM "AdminOperationLogs_Archive"
    WHERE "Timestamp" < NOW() - INTERVAL '5 years';

    GET DIAGNOSTICS delete_count = ROW_COUNT;

    RETURN QUERY SELECT
        'AdminOperationLogs'::text,
        archive_count,
        delete_count,
        clock_timestamp() - start_time;

    -- ========================================================================
    -- 2. SubscriptionUsageLogs: Archive > 3 months, Keep for 7 years
    -- ========================================================================
    start_time := clock_timestamp();

    -- Archive (3 months old → 7 years old)
    INSERT INTO "SubscriptionUsageLogs_Archive"
    SELECT * FROM "SubscriptionUsageLogs"
    WHERE "UsageDate" < NOW() - INTERVAL '3 months'
    AND "UsageDate" >= NOW() - INTERVAL '7 years';

    GET DIAGNOSTICS archive_count = ROW_COUNT;

    -- Delete archived records from main table
    DELETE FROM "SubscriptionUsageLogs"
    WHERE "UsageDate" < NOW() - INTERVAL '3 months'
    AND "UsageDate" >= NOW() - INTERVAL '7 years';

    -- Delete > 7 years from archive (tax law compliance)
    DELETE FROM "SubscriptionUsageLogs_Archive"
    WHERE "UsageDate" < NOW() - INTERVAL '7 years';

    GET DIAGNOSTICS delete_count = ROW_COUNT;

    RETURN QUERY SELECT
        'SubscriptionUsageLogs'::text,
        archive_count,
        delete_count,
        clock_timestamp() - start_time;

    -- ========================================================================
    -- 3. SmsLogs: Archive > 30 days, Delete > 1 year
    -- ========================================================================
    start_time := clock_timestamp();

    -- Archive (30 days old → 1 year old)
    INSERT INTO "SmsLogs_Archive"
    SELECT * FROM "SmsLogs"
    WHERE "CreatedDate" < NOW() - INTERVAL '30 days'
    AND "CreatedDate" >= NOW() - INTERVAL '1 year';

    GET DIAGNOSTICS archive_count = ROW_COUNT;

    -- Delete archived records from main table
    DELETE FROM "SmsLogs"
    WHERE "CreatedDate" < NOW() - INTERVAL '30 days'
    AND "CreatedDate" >= NOW() - INTERVAL '1 year';

    -- Delete > 1 year from archive
    DELETE FROM "SmsLogs_Archive"
    WHERE "CreatedDate" < NOW() - INTERVAL '1 year';

    GET DIAGNOSTICS delete_count = ROW_COUNT;

    RETURN QUERY SELECT
        'SmsLogs'::text,
        archive_count,
        delete_count,
        clock_timestamp() - start_time;

    -- ========================================================================
    -- 4. MobileLogins: Archive > 90 days, Delete > 1 year
    -- ========================================================================
    start_time := clock_timestamp();

    -- Archive (90 days old → 1 year old)
    INSERT INTO "MobileLogins_Archive"
    SELECT * FROM "MobileLogins"
    WHERE "LoginDate" < NOW() - INTERVAL '90 days'
    AND "LoginDate" >= NOW() - INTERVAL '1 year';

    GET DIAGNOSTICS archive_count = ROW_COUNT;

    -- Delete archived records from main table
    DELETE FROM "MobileLogins"
    WHERE "LoginDate" < NOW() - INTERVAL '90 days'
    AND "LoginDate" >= NOW() - INTERVAL '1 year';

    -- Delete > 1 year from archive
    DELETE FROM "MobileLogins_Archive"
    WHERE "LoginDate" < NOW() - INTERVAL '1 year';

    GET DIAGNOSTICS delete_count = ROW_COUNT;

    RETURN QUERY SELECT
        'MobileLogins'::text,
        archive_count,
        delete_count,
        clock_timestamp() - start_time;

    -- ========================================================================
    -- 5. Logs: Archive > 7 days, Delete > 30 days (EN AGRESIF!)
    -- ========================================================================
    start_time := clock_timestamp();

    -- Archive (7 days old → 30 days old)
    INSERT INTO "Logs_Archive"
    SELECT * FROM "Logs"
    WHERE "Timestamp" < NOW() - INTERVAL '7 days'
    AND "Timestamp" >= NOW() - INTERVAL '30 days';

    GET DIAGNOSTICS archive_count = ROW_COUNT;

    -- Delete archived records from main table
    DELETE FROM "Logs"
    WHERE "Timestamp" < NOW() - INTERVAL '7 days'
    AND "Timestamp" >= NOW() - INTERVAL '30 days';

    -- Delete > 30 days from archive
    DELETE FROM "Logs_Archive"
    WHERE "Timestamp" < NOW() - INTERVAL '30 days';

    GET DIAGNOSTICS delete_count = ROW_COUNT;

    RETURN QUERY SELECT
        'Logs'::text,
        archive_count,
        delete_count,
        clock_timestamp() - start_time;

END;
$$ LANGUAGE plpgsql;
```

#### 1.3. Scheduled Cron Job (pg_cron Extension)

```sql
-- pg_cron extension'ını etkinleştir (Superuser gerekli)
CREATE EXTENSION IF NOT EXISTS pg_cron;

-- Her gün sabah 02:00'da çalıştır (düşük trafik saati)
SELECT cron.schedule(
    'log-cleanup-daily',           -- job name
    '0 2 * * *',                    -- cron expression (02:00 daily)
    'SELECT * FROM cleanup_old_logs();'
);

-- Job'ları listele
SELECT * FROM cron.job;

-- Job'u manuel çalıştır (test için)
SELECT cron.run_job('log-cleanup-daily');

-- Job'u sil
-- SELECT cron.unschedule('log-cleanup-daily');
```

#### 1.4. Manuel Çalıştırma (Cron Yoksa)

```sql
-- Manuel çalıştır ve sonuçları gör
SELECT * FROM cleanup_old_logs();

-- Sonuç:
-- table_name              | archived_count | deleted_count | operation_time
-- -----------------------|----------------|---------------|---------------
-- AdminOperationLogs     | 50000          | 10000         | 00:00:15
-- SubscriptionUsageLogs  | 120000         | 5000          | 00:00:30
-- SmsLogs                | 3000           | 500           | 00:00:02
-- MobileLogins           | 80000          | 20000         | 00:00:10
-- Logs                   | 500000         | 200000        | 00:01:30
```

---

### Strateji 2: Hangfire Background Job (.NET) ⭐ KOLAY

**Avantajlar**:
- Zaten Hangfire kullanılıyor (mevcutta var)
- .NET kodundan kontrol
- Retry mekanizması
- Dashboard ile izleme

**Uygulama**:

```csharp
// Business/Services/LogCleanupService.cs

public class LogCleanupService
{
    private readonly IAdminOperationLogRepository _adminLogRepo;
    private readonly ISubscriptionUsageLogRepository _usageLogRepo;
    private readonly ISmsLogRepository _smsLogRepo;
    // ... diğer repository'ler

    public async Task<LogCleanupResult> CleanupOldLogsAsync()
    {
        var result = new LogCleanupResult();

        // 1. AdminOperationLogs: 1 yıl öncesini archive et
        var adminLogsToArchive = await _adminLogRepo.GetListAsync(
            log => log.Timestamp < DateTime.Now.AddYears(-1));

        // Archive table'a kopyala (veya file export)
        // await _adminLogArchiveRepo.BulkInsertAsync(adminLogsToArchive);

        // Main table'dan sil
        // await _adminLogRepo.BulkDeleteAsync(adminLogsToArchive);

        result.AdminLogsArchived = adminLogsToArchive.Count();

        // 2. SubscriptionUsageLogs: 3 ay öncesini archive et
        var usageLogsToArchive = await _usageLogRepo.GetListAsync(
            log => log.UsageDate < DateTime.Now.AddMonths(-3));

        result.UsageLogsArchived = usageLogsToArchive.Count();

        // 3. Logs: 7 gün öncesini sil (aggressive)
        var oldLogs = await _logRepo.GetListAsync(
            log => log.Timestamp < DateTime.Now.AddDays(-7));

        foreach (var log in oldLogs)
        {
            _logRepo.Delete(log);
        }
        await _logRepo.SaveChangesAsync();

        result.GeneralLogsDeleted = oldLogs.Count();

        return result;
    }
}

// Startup.cs veya Program.cs
public void ConfigureHangfire(IServiceProvider services)
{
    // Her gün sabah 02:00'da çalıştır
    RecurringJob.AddOrUpdate<LogCleanupService>(
        "log-cleanup",
        service => service.CleanupOldLogsAsync(),
        Cron.Daily(2) // 02:00
    );
}
```

---

### Strateji 3: External Logging Service (Logs tablosu için) ⭐ ÖNERİLEN

**Neden Gerekli**:
- `Logs` tablosu en hızlı büyüyen tablo (~9 GB/yıl)
- Production database'i gereksiz yere şişiriyor
- PostgreSQL log storage için optimize edilmemiş

**Alternatifler**:

#### 3.1. Sentry (Error Tracking)
```bash
# Install
dotnet add package Sentry.AspNetCore

# appsettings.json
"Sentry": {
  "Dsn": "https://examplePublicKey@o0.ingest.sentry.io/0",
  "Environment": "production",
  "TracesSampleRate": 0.1
}

# Program.cs
builder.WebHost.UseSentry();
```

**Maliyet**: Free tier (5,000 events/month), Pro $26/month (50K events)

#### 3.2. AWS CloudWatch Logs
```bash
# Install
dotnet add package AWS.Logger.AspNetCore

# appsettings.json
"AWS": {
  "Region": "eu-central-1",
  "CloudWatch": {
    "LogGroup": "ziraai-production",
    "LogStreamNameSuffix": "webapi"
  }
}
```

**Maliyet**: $0.50/GB ingestion, $0.03/GB storage (first 5 GB free)

#### 3.3. Seq (Self-Hosted)
```bash
# Docker
docker run -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq

# Install
dotnet add package Seq.Extensions.Logging

# appsettings.json
"Seq": {
  "ServerUrl": "http://localhost:5341",
  "ApiKey": "your-api-key"
}
```

**Maliyet**: Free (self-hosted), Developer $195/year (SaaS)

---

## 🚀 Önerilen Implementation Planı

### Phase 1: Acil Durum (1 gün) 🔴

**Hedef**: Mevcut log'ları temizle, disk alanı aç

```sql
-- ============================================================================
-- ACİL: Eski Logları Manuel Temizle (PRODUCTION ÖNCESİ)
-- ============================================================================

-- 1. Backup al (önemli!)
-- pg_dump -t "AdminOperationLogs" -t "SubscriptionUsageLogs" ziraai_db > logs_backup.sql

-- 2. Mevcut log sayılarını kontrol et
SELECT
    'AdminOperationLogs' as table_name,
    COUNT(*) as total_records,
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '1 year' THEN 1 END) as old_records,
    pg_size_pretty(pg_total_relation_size('"AdminOperationLogs"')) as table_size
FROM "AdminOperationLogs"
UNION ALL
SELECT
    'SubscriptionUsageLogs',
    COUNT(*),
    COUNT(CASE WHEN "UsageDate" < NOW() - INTERVAL '3 months' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"SubscriptionUsageLogs"'))
FROM "SubscriptionUsageLogs"
UNION ALL
SELECT
    'SmsLogs',
    COUNT(*),
    COUNT(CASE WHEN "CreatedDate" < NOW() - INTERVAL '30 days' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"SmsLogs"'))
FROM "SmsLogs"
UNION ALL
SELECT
    'MobileLogins',
    COUNT(*),
    COUNT(CASE WHEN "LoginDate" < NOW() - INTERVAL '90 days' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"MobileLogins"'))
FROM "MobileLogins"
UNION ALL
SELECT
    'Logs',
    COUNT(*),
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '7 days' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"Logs"'))
FROM "Logs";

-- 3. Eski logları sil (DIKKATLI!)
-- ⚠️ Production'da çalıştırmadan önce staging'de test et!

-- AdminOperationLogs: > 2 yıl önce
DELETE FROM "AdminOperationLogs"
WHERE "Timestamp" < NOW() - INTERVAL '2 years';

-- SubscriptionUsageLogs: > 2 yıl önce (billing records için dikkatli!)
DELETE FROM "SubscriptionUsageLogs"
WHERE "UsageDate" < NOW() - INTERVAL '2 years';

-- SmsLogs: > 6 ay önce
DELETE FROM "SmsLogs"
WHERE "CreatedDate" < NOW() - INTERVAL '6 months';

-- MobileLogins: > 6 ay önce
DELETE FROM "MobileLogins"
WHERE "LoginDate" < NOW() - INTERVAL '6 months';

-- Logs: > 30 gün önce (aggressive)
DELETE FROM "Logs"
WHERE "Timestamp" < NOW() - INTERVAL '30 days';

-- 4. VACUUM (disk alanını geri kazanmak için)
VACUUM FULL "AdminOperationLogs";
VACUUM FULL "SubscriptionUsageLogs";
VACUUM FULL "SmsLogs";
VACUUM FULL "MobileLogins";
VACUUM FULL "Logs";

-- 5. ANALYZE (statistics güncelle)
ANALYZE "AdminOperationLogs";
ANALYZE "SubscriptionUsageLogs";
ANALYZE "SmsLogs";
ANALYZE "MobileLogins";
ANALYZE "Logs";

-- 6. Sonuç kontrolü
SELECT
    tablename,
    pg_size_pretty(pg_total_relation_size('"' || tablename || '"')) as new_size,
    n_live_tup as remaining_records
FROM pg_stat_user_tables
WHERE tablename IN (
    'AdminOperationLogs',
    'SubscriptionUsageLogs',
    'SmsLogs',
    'MobileLogins',
    'Logs'
);
```

### Phase 2: Otomatik Temizlik (1 hafta) 🟡

**Hedef**: pg_cron ile otomatik log rotation

1. ✅ Archive tabloları oluştur
2. ✅ cleanup_old_logs() fonksiyonu deploy et
3. ✅ pg_cron schedule ayarla
4. ✅ Test et (staging)
5. ✅ Production'a deploy et
6. ✅ İlk çalıştırmayı izle

**Migration Script**: `005_log_cleanup_automation.sql`

### Phase 3: External Logging (2 hafta) 🟢

**Hedef**: `Logs` tablosunu external service'e taşı

1. ✅ Sentry/CloudWatch/Seq seç
2. ✅ .NET logger configuration
3. ✅ Staging'de test et
4. ✅ Mevcut `Logs` tablosunu temizle
5. ✅ Production'a deploy et
6. ✅ `Logs` tablosunu DROP et (6 ay sonra)

---

## 📝 DBeaver Manuel Temizlik Scriptleri

### 1. Günlük Log Özet Raporu

```sql
-- ============================================================================
-- GÜNLÜK: Log Tabloları Boyut ve Kayıt Sayısı
-- ============================================================================

SELECT
    NOW()::date as report_date,
    tablename,
    n_live_tup as total_records,
    pg_size_pretty(pg_total_relation_size('"' || tablename || '"')) as total_size,
    pg_size_pretty(pg_relation_size('"' || tablename || '"')) as table_size,
    pg_size_pretty(pg_indexes_size('"' || tablename || '"')) as indexes_size,
    n_dead_tup as dead_rows,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup, 0), 2) as dead_percent,
    last_vacuum,
    last_autovacuum
FROM pg_stat_user_tables
WHERE tablename IN (
    'AdminOperationLogs',
    'SubscriptionUsageLogs',
    'SmsLogs',
    'MobileLogins',
    'Logs'
)
ORDER BY pg_total_relation_size('"' || tablename || '"') DESC;
```

### 2. Eski Kayıt Analizi

```sql
-- ============================================================================
-- Tablolarda Ne Kadar Eski Kayıt Var?
-- ============================================================================

-- AdminOperationLogs
SELECT
    'AdminOperationLogs' as table_name,
    MIN("Timestamp") as oldest_record,
    MAX("Timestamp") as newest_record,
    COUNT(*) as total_records,
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '1 year' THEN 1 END) as older_than_1y,
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '2 years' THEN 1 END) as older_than_2y,
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '5 years' THEN 1 END) as older_than_5y,
    pg_size_pretty(pg_total_relation_size('"AdminOperationLogs"')) as total_size
FROM "AdminOperationLogs"

UNION ALL

-- SubscriptionUsageLogs
SELECT
    'SubscriptionUsageLogs',
    MIN("UsageDate"),
    MAX("UsageDate"),
    COUNT(*),
    COUNT(CASE WHEN "UsageDate" < NOW() - INTERVAL '1 year' THEN 1 END),
    COUNT(CASE WHEN "UsageDate" < NOW() - INTERVAL '2 years' THEN 1 END),
    COUNT(CASE WHEN "UsageDate" < NOW() - INTERVAL '5 years' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"SubscriptionUsageLogs"'))
FROM "SubscriptionUsageLogs"

UNION ALL

-- SmsLogs
SELECT
    'SmsLogs',
    MIN("CreatedDate"),
    MAX("CreatedDate"),
    COUNT(*),
    COUNT(CASE WHEN "CreatedDate" < NOW() - INTERVAL '1 year' THEN 1 END),
    COUNT(CASE WHEN "CreatedDate" < NOW() - INTERVAL '2 years' THEN 1 END),
    COUNT(CASE WHEN "CreatedDate" < NOW() - INTERVAL '5 years' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"SmsLogs"'))
FROM "SmsLogs"

UNION ALL

-- MobileLogins
SELECT
    'MobileLogins',
    MIN("LoginDate"),
    MAX("LoginDate"),
    COUNT(*),
    COUNT(CASE WHEN "LoginDate" < NOW() - INTERVAL '1 year' THEN 1 END),
    COUNT(CASE WHEN "LoginDate" < NOW() - INTERVAL '2 years' THEN 1 END),
    COUNT(CASE WHEN "LoginDate" < NOW() - INTERVAL '5 years' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"MobileLogins"'))
FROM "MobileLogins"

UNION ALL

-- Logs
SELECT
    'Logs',
    MIN("Timestamp"),
    MAX("Timestamp"),
    COUNT(*),
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '1 year' THEN 1 END),
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '2 years' THEN 1 END),
    COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '5 years' THEN 1 END),
    pg_size_pretty(pg_total_relation_size('"Logs"'))
FROM "Logs";
```

### 3. Disk Alanı Tasarrufu Hesaplama

```sql
-- ============================================================================
-- Ne Kadar Disk Alanı Kazanabiliriz?
-- ============================================================================

WITH log_analysis AS (
    SELECT
        'AdminOperationLogs' as table_name,
        COUNT(*) as total_records,
        COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '1 year' THEN 1 END) as deletable_records,
        pg_total_relation_size('"AdminOperationLogs"') as current_size
    FROM "AdminOperationLogs"

    UNION ALL

    SELECT
        'SubscriptionUsageLogs',
        COUNT(*),
        COUNT(CASE WHEN "UsageDate" < NOW() - INTERVAL '3 months' THEN 1 END),
        pg_total_relation_size('"SubscriptionUsageLogs"')
    FROM "SubscriptionUsageLogs"

    UNION ALL

    SELECT
        'SmsLogs',
        COUNT(*),
        COUNT(CASE WHEN "CreatedDate" < NOW() - INTERVAL '30 days' THEN 1 END),
        pg_total_relation_size('"SmsLogs"')
    FROM "SmsLogs"

    UNION ALL

    SELECT
        'MobileLogins',
        COUNT(*),
        COUNT(CASE WHEN "LoginDate" < NOW() - INTERVAL '90 days' THEN 1 END),
        pg_total_relation_size('"MobileLogins"')
    FROM "MobileLogins"

    UNION ALL

    SELECT
        'Logs',
        COUNT(*),
        COUNT(CASE WHEN "Timestamp" < NOW() - INTERVAL '7 days' THEN 1 END),
        pg_total_relation_size('"Logs"')
    FROM "Logs"
)
SELECT
    table_name,
    total_records,
    deletable_records,
    ROUND(100.0 * deletable_records / NULLIF(total_records, 0), 2) as deletable_percent,
    pg_size_pretty(current_size) as current_size,
    pg_size_pretty(current_size * deletable_records / NULLIF(total_records, 0)) as estimated_savings,
    CASE
        WHEN deletable_records > total_records * 0.5 THEN '🔴 URGENT - >50% old data'
        WHEN deletable_records > total_records * 0.3 THEN '🟡 WARNING - >30% old data'
        ELSE '✅ OK'
    END as cleanup_priority
FROM log_analysis
ORDER BY (current_size * deletable_records / NULLIF(total_records, 0)) DESC;
```

---

## ⚠️ GDPR/KVKK Compliance

### Kişisel Veri İçeren Log Kolonları

| Tablo | Kolon | Veri Tipi | Risk |
|-------|-------|-----------|------|
| AdminOperationLogs | IpAddress | IP | 🟡 Orta |
| AdminOperationLogs | BeforeState/AfterState | JSON (user data) | 🔴 Yüksek |
| SubscriptionUsageLogs | IpAddress | IP | 🟡 Orta |
| SubscriptionUsageLogs | DeviceInfo | Device | 🟢 Düşük |
| SmsLogs | PhoneNumber | Telefon | 🔴 Yüksek |
| MobileLogins | IpAddress | IP | 🟡 Orta |
| MobileLogins | DeviceInfo | Device | 🟢 Düşük |

### Kullanıcı Silme Durumunda (GDPR "Right to be Forgotten")

```sql
-- User silme durumunda log'ları anonim hale getir
CREATE OR REPLACE FUNCTION anonymize_user_logs()
RETURNS TRIGGER AS $$
BEGIN
    -- AdminOperationLogs
    UPDATE "AdminOperationLogs"
    SET "BeforeState" = NULL,
        "AfterState" = NULL,
        "IpAddress" = '0.0.0.0'
    WHERE "TargetUserId" = OLD."UserId";

    -- SubscriptionUsageLogs (cascade ile zaten siliniyor ama emin olmak için)
    UPDATE "SubscriptionUsageLogs"
    SET "IpAddress" = '0.0.0.0',
        "DeviceInfo" = 'ANONYMIZED'
    WHERE "UserId" = OLD."UserId";

    -- SmsLogs
    UPDATE "SmsLogs"
    SET "PhoneNumber" = '***MASKED***'
    WHERE "SenderUserId" = OLD."UserId";

    -- MobileLogins
    UPDATE "MobileLogins"
    SET "IpAddress" = '0.0.0.0',
        "DeviceInfo" = 'ANONYMIZED'
    WHERE "UserId" = OLD."UserId";

    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

-- Trigger oluştur
CREATE TRIGGER user_deletion_anonymize_logs
BEFORE DELETE ON "Users"
FOR EACH ROW
EXECUTE FUNCTION anonymize_user_logs();
```

---

## 📋 Production Checklist

### Deployment Öncesi

- [ ] **Mevcut log boyutlarını ölç** (DBeaver script çalıştır)
- [ ] **Eski logları backup al** (pg_dump)
- [ ] **Phase 1 temizlik scriptini çalıştır** (staging'de test et)
- [ ] **Archive tabloları oluştur**
- [ ] **cleanup_old_logs() fonksiyonunu deploy et**
- [ ] **pg_cron schedule ayarla** (veya Hangfire job)
- [ ] **GDPR compliance trigger'ları ekle**
- [ ] **External logging seç ve configure et** (Sentry/CloudWatch)

### Deployment Sonrası

- [ ] **İlk cleanup job'u manuel çalıştır**
- [ ] **Log boyutlarını tekrar ölç** (disk alanı kazancını doğrula)
- [ ] **Haftalık monitoring ayarla** (log boyut trendi)
- [ ] **Alarm kuralları oluştur** (log tablosu > 5 GB uyarı)
- [ ] **6 ay sonra `Logs` tablosunu DROP et** (external logging'e geçildiyse)

---

## 📞 Acil Durum İletişimi

**Log tablosu çok büyüdü, production yavaşladı:**

1. ✅ Acil durum temizlik scriptini çalıştır (yukarıda)
2. ✅ VACUUM FULL çalıştır (downtime gerektirir)
3. ✅ Railway/Heroku database upgrade et (geçici çözüm)
4. ✅ DevOps team'i bilgilendir

---

**Son Güncelleme**: 2025-12-05
**Versiyon**: 1.0
**Hazırlayan**: Backend Performance Team
**Onay**: Production Readiness Review
