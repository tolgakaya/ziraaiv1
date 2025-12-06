# PostgreSQL Performans İzleme ve Değerlendirme Kılavuzu

**Proje**: ZiraAI Platform
**Veritabanı**: PostgreSQL
**Araç**: DBeaver
**Güncelleme**: 2025-12-05
**Periyot**: Haftalık/Aylık

---

## 📋 İçindekiler

1. [Haftalık Performans Kontrol Listesi](#haftalık-performans-kontrol-listesi)
2. [Aylık Performans Kontrol Listesi](#aylık-performans-kontrol-listesi)
3. [Index Performans Analizi](#index-performans-analizi)
4. [Sorgu Performans Analizi](#sorgu-performans-analizi)
5. [Tablo Boyut ve Şişme Analizi](#tablo-boyut-ve-şişme-analizi)
6. [Bağlantı ve Kaynak Kullanımı](#bağlantı-ve-kaynak-kullanımı)
7. [Cache Hit Ratio Analizi](#cache-hit-ratio-analizi)
8. [Yavaş Sorgu Analizi](#yavaş-sorgu-analizi)
9. [Maintenance İşlemleri](#maintenance-i̇şlemleri)
10. [Alarm ve Uyarı Eşikleri](#alarm-ve-uyarı-eşikleri)

---

## Haftalık Performans Kontrol Listesi

### 1️⃣ Hızlı Sağlık Kontrolü (5 dakika)

```sql
-- ============================================================================
-- Haftalık Hızlı Performans Özeti
-- ============================================================================

-- 1. Genel Veritabanı Durumu
SELECT
    'Database Health Check' as check_type,
    pg_database_size(current_database()) / (1024*1024*1024.0) as size_gb,
    (SELECT count(*) FROM pg_stat_activity WHERE state = 'active') as active_connections,
    (SELECT count(*) FROM pg_stat_activity WHERE state = 'idle') as idle_connections,
    (SELECT count(*) FROM pg_stat_activity WHERE state = 'idle in transaction') as idle_in_transaction,
    NOW() as check_time;

-- 2. En Büyük 10 Tablo
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) as table_size,
    pg_size_pretty(pg_indexes_size(schemaname||'.'||tablename)) as indexes_size,
    n_live_tup as row_count,
    n_dead_tup as dead_rows,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) as dead_row_percent
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC
LIMIT 10;

-- 3. Cache Hit Ratio (Kritik: >95% olmalı)
SELECT
    'Cache Hit Ratio' as metric,
    ROUND(100.0 * sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0), 2) as cache_hit_percentage,
    CASE
        WHEN ROUND(100.0 * sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0), 2) >= 95 THEN '✅ EXCELLENT'
        WHEN ROUND(100.0 * sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0), 2) >= 90 THEN '🟡 GOOD'
        ELSE '🔴 POOR - Increase shared_buffers'
    END as status
FROM pg_statio_user_tables
WHERE schemaname = 'public';

-- 4. Index Hit Ratio (Kritik: >95% olmalı)
SELECT
    'Index Hit Ratio' as metric,
    ROUND(100.0 * sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0), 2) as index_hit_percentage,
    CASE
        WHEN ROUND(100.0 * sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0), 2) >= 95 THEN '✅ EXCELLENT'
        WHEN ROUND(100.0 * sum(idx_blks_hit) / NULLIF(sum(idx_blks_hit) + sum(idx_blks_read), 0), 2) >= 90 THEN '🟡 GOOD'
        ELSE '🔴 POOR - Increase shared_buffers'
    END as status
FROM pg_statio_user_indexes
WHERE schemaname = 'public';

-- 5. Vacuum İhtiyacı Olan Tablolar (dead_row > %10)
SELECT
    schemaname,
    tablename,
    n_live_tup as live_rows,
    n_dead_tup as dead_rows,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) as dead_percent,
    last_vacuum,
    last_autovacuum,
    CASE
        WHEN ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) > 20 THEN '🔴 URGENT - Manual VACUUM needed'
        WHEN ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) > 10 THEN '🟡 WARNING - Schedule VACUUM'
        ELSE '✅ OK'
    END as vacuum_status
FROM pg_stat_user_tables
WHERE schemaname = 'public'
AND n_dead_tup > 0
ORDER BY n_dead_tup DESC
LIMIT 10;
```

### 2️⃣ Kritik Tablolar İzleme

```sql
-- ============================================================================
-- Kritik ZiraAI Tabloları Haftalık İzleme
-- ============================================================================

-- PlantAnalyses, UserSubscriptions, AnalysisMessages, SponsorshipCodes, ReferralCodes

SELECT
    tablename,
    pg_size_pretty(pg_total_relation_size('"' || tablename || '"')) as total_size,
    n_live_tup as row_count,
    n_dead_tup as dead_rows,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup, 0), 2) as dead_percent,
    seq_scan as sequential_scans,
    idx_scan as index_scans,
    ROUND(100.0 * idx_scan / NULLIF(seq_scan + idx_scan, 0), 2) as index_usage_percent,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables
WHERE schemaname = 'public'
AND tablename IN (
    'PlantAnalyses',
    'UserSubscriptions',
    'AnalysisMessages',
    'SponsorshipCodes',
    'ReferralCodes',
    'Users',
    'Configurations',
    'SubscriptionTiers'
)
ORDER BY pg_total_relation_size('"' || tablename || '"') DESC;
```

---

## Aylık Performans Kontrol Listesi

### 1️⃣ Detaylı Index Performans Analizi (15 dakika)

```sql
-- ============================================================================
-- AYLIK: Index Kullanım ve Performans Analizi
-- ============================================================================

-- 1. Kullanılmayan Indexler (idx_scan = 0)
SELECT
    schemaname,
    tablename,
    indexrelname as index_name,
    idx_scan as times_used,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    pg_relation_size(indexrelid) as size_bytes,
    CASE
        WHEN idx_scan = 0 THEN '🔴 NEVER USED - Consider dropping'
        ELSE '✅ OK'
    END as recommendation
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND idx_scan = 0
ORDER BY pg_relation_size(indexrelid) DESC;

-- 2. Düşük Kullanımlı Indexler (idx_scan < 100)
SELECT
    schemaname,
    tablename,
    indexrelname as index_name,
    idx_scan as times_used,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    CASE
        WHEN idx_scan = 0 THEN '🔴 NEVER USED'
        WHEN idx_scan < 10 THEN '🟡 RARELY USED (< 10 times)'
        WHEN idx_scan < 100 THEN '🟠 LOW USAGE (< 100 times)'
        ELSE '🟢 ACTIVE'
    END as usage_status
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND idx_scan < 100
ORDER BY idx_scan ASC, pg_relation_size(indexrelid) DESC;

-- 3. Index vs Sequential Scan Oranı (Tablo Bazında)
SELECT
    tablename,
    seq_scan as sequential_scans,
    idx_scan as index_scans,
    ROUND(100.0 * idx_scan / NULLIF(seq_scan + idx_scan, 0), 2) as index_usage_percent,
    n_live_tup as row_count,
    CASE
        WHEN n_live_tup > 1000 AND ROUND(100.0 * idx_scan / NULLIF(seq_scan + idx_scan, 0), 2) < 50 THEN '🔴 LOW INDEX USAGE - Missing indexes?'
        WHEN n_live_tup > 1000 AND ROUND(100.0 * idx_scan / NULLIF(seq_scan + idx_scan, 0), 2) < 80 THEN '🟡 MODERATE INDEX USAGE'
        WHEN n_live_tup > 1000 THEN '✅ GOOD INDEX USAGE'
        ELSE '⚪ Small table - OK'
    END as status
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY n_live_tup DESC;

-- 4. Phase 1 Index Performans Takibi (13 index)
SELECT
    tablename,
    indexrelname as index_name,
    idx_scan as times_used,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    CASE
        WHEN idx_scan > 1000 THEN '🟢 HIGH USAGE - Excellent'
        WHEN idx_scan > 100 THEN '✅ GOOD USAGE'
        WHEN idx_scan > 10 THEN '🟡 MODERATE USAGE'
        WHEN idx_scan > 0 THEN '🟠 LOW USAGE'
        ELSE '🔴 NOT USED YET'
    END as performance_status
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND indexrelname IN (
    'IX_PlantAnalyses_UserId_AnalysisDate',
    'IX_PlantAnalyses_SponsorCompanyId_AnalysisDate',
    'IX_PlantAnalyses_AnalysisStatus_AnalysisDate',
    'IX_UserSubscriptions_UserId_Active_EndDate',
    'IX_UserSubscriptions_UserId',
    'IX_UserSubscriptions_SubscriptionTierId',
    'IX_PlantAnalyses_SponsorCompanyId',
    'IX_UserSubscriptions_SponsorId',
    'IX_AnalysisMessages_FromUserId_SentDate',
    'IX_AnalysisMessages_ToUserId_IsRead_SentDate',
    'IX_SponsorshipCodes_SponsorId_IsUsed_ExpiryDate',
    'IX_SponsorshipCodes_Code_Active_Expiry',
    'IX_ReferralCodes_Code_IsActive'
)
ORDER BY idx_scan DESC;

-- 5. Index Boyut Analizi (En büyük 20 index)
SELECT
    schemaname,
    tablename,
    indexrelname as index_name,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    pg_relation_size(indexrelid) as size_bytes,
    idx_scan as times_used,
    ROUND(pg_relation_size(indexrelid)::numeric / NULLIF(idx_scan, 0), 2) as bytes_per_scan,
    CASE
        WHEN idx_scan = 0 AND pg_relation_size(indexrelid) > 1024*1024 THEN '🔴 LARGE UNUSED - Drop candidate'
        WHEN idx_scan < 100 AND pg_relation_size(indexrelid) > 1024*1024 THEN '🟡 LARGE LOW-USAGE'
        ELSE '✅ OK'
    END as recommendation
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY pg_relation_size(indexrelid) DESC
LIMIT 20;

-- 6. Total Index Storage Kullanımı
SELECT
    schemaname,
    tablename,
    COUNT(*) as index_count,
    pg_size_pretty(SUM(pg_relation_size(indexrelid))) as total_index_size,
    pg_size_pretty(pg_relation_size('"' || tablename || '"')) as table_size,
    ROUND(100.0 * SUM(pg_relation_size(indexrelid)) /
          NULLIF(pg_relation_size('"' || tablename || '"'), 0), 2) as index_to_table_ratio,
    CASE
        WHEN COUNT(*) > 20 THEN '🔴 TOO MANY INDEXES (' || COUNT(*)::text || ')'
        WHEN COUNT(*) > 10 THEN '🟡 HIGH INDEX COUNT (' || COUNT(*)::text || ')'
        ELSE '✅ OK (' || COUNT(*)::text || ' indexes)'
    END as status
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
GROUP BY schemaname, tablename
ORDER BY SUM(pg_relation_size(indexrelid)) DESC;
```

### 2️⃣ Tablo Şişme (Bloat) Analizi

```sql
-- ============================================================================
-- AYLIK: Tablo ve Index Şişme Analizi
-- ============================================================================

-- 1. Tablo Şişme Tahmini
SELECT
    schemaname,
    tablename,
    n_live_tup as live_rows,
    n_dead_tup as dead_rows,
    ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) as dead_percent,
    pg_size_pretty(pg_total_relation_size('"' || tablename || '"')) as total_size,
    last_vacuum,
    last_autovacuum,
    CASE
        WHEN n_dead_tup > 10000 AND ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) > 20
            THEN '🔴 CRITICAL - Immediate VACUUM FULL needed'
        WHEN n_dead_tup > 5000 AND ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) > 15
            THEN '🟡 WARNING - Schedule VACUUM'
        WHEN ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2) > 10
            THEN '🟠 MODERATE - Monitor'
        ELSE '✅ HEALTHY'
    END as bloat_status
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY n_dead_tup DESC;

-- 2. Tablo Büyüme Trendi (Son Vacuum'dan Beri)
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size('"' || tablename || '"')) as current_size,
    n_tup_ins as inserts_since_analyze,
    n_tup_upd as updates_since_analyze,
    n_tup_del as deletes_since_analyze,
    n_tup_ins + n_tup_upd + n_tup_del as total_changes,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    CASE
        WHEN (n_tup_ins + n_tup_upd + n_tup_del) > 50000 THEN '🔴 HIGH ACTIVITY - ANALYZE needed'
        WHEN (n_tup_ins + n_tup_upd + n_tup_del) > 10000 THEN '🟡 MODERATE ACTIVITY'
        ELSE '✅ LOW ACTIVITY'
    END as activity_status
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY (n_tup_ins + n_tup_upd + n_tup_del) DESC
LIMIT 20;
```

### 3️⃣ Sorgu Performans İstatistikleri

```sql
-- ============================================================================
-- AYLIK: Sorgu Performans İstatistikleri
-- ============================================================================

-- NOT: pg_stat_statements extension'ı aktif olmalı
-- Extension kontrolü:
SELECT * FROM pg_extension WHERE extname = 'pg_stat_statements';

-- Extension yoksa aktive et (Superuser gerekli):
-- CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- 1. En Yavaş 20 Sorgu (Ortalama Çalışma Zamanı)
SELECT
    ROUND(mean_exec_time::numeric, 2) as avg_time_ms,
    ROUND(total_exec_time::numeric, 2) as total_time_ms,
    calls,
    ROUND((100.0 * total_exec_time / SUM(total_exec_time) OVER ())::numeric, 2) as percent_of_total,
    LEFT(query, 100) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY mean_exec_time DESC
LIMIT 20;

-- 2. En Çok Çağrılan 20 Sorgu
SELECT
    calls,
    ROUND(mean_exec_time::numeric, 2) as avg_time_ms,
    ROUND(total_exec_time::numeric, 2) as total_time_ms,
    ROUND((100.0 * total_exec_time / SUM(total_exec_time) OVER ())::numeric, 2) as percent_of_total,
    LEFT(query, 100) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY calls DESC
LIMIT 20;

-- 3. En Çok Zaman Harcayan 20 Sorgu (Total Time)
SELECT
    ROUND(total_exec_time::numeric, 2) as total_time_ms,
    calls,
    ROUND(mean_exec_time::numeric, 2) as avg_time_ms,
    ROUND((100.0 * total_exec_time / SUM(total_exec_time) OVER ())::numeric, 2) as percent_of_total,
    LEFT(query, 100) as query_preview
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat_statements%'
ORDER BY total_exec_time DESC
LIMIT 20;

-- 4. Sorgu İstatistiklerini Sıfırla (Aylık başlangıçta)
-- SELECT pg_stat_statements_reset();
```

---

## Index Performans Analizi

### Duplicate ve Redundant Index Tespiti

```sql
-- ============================================================================
-- Index Tekrarı ve Fazlalık Analizi
-- ============================================================================

-- 1. Aynı Kolonlarda Duplicate Indexler
SELECT
    a.tablename,
    a.indexname as index1,
    b.indexname as index2,
    a.indexdef as index1_definition,
    b.indexdef as index2_definition,
    pg_size_pretty(pg_relation_size(a.indexrelid)) as index1_size,
    pg_size_pretty(pg_relation_size(b.indexrelid)) as index2_size
FROM pg_stat_user_indexes a
JOIN pg_stat_user_indexes b
    ON a.tablename = b.tablename
    AND a.indexrelname < b.indexrelname
WHERE a.schemaname = 'public'
AND a.indexrelname != b.indexrelname
-- Kolon listesini karşılaştır
AND (
    SELECT array_agg(attname ORDER BY attnum)
    FROM pg_attribute
    WHERE attrelid = a.indexrelid
) = (
    SELECT array_agg(attname ORDER BY attnum)
    FROM pg_attribute
    WHERE attrelid = b.indexrelid
);

-- 2. Prefix Index Redundancy
-- (Composite index varsa, tek kolon index gereksizdir)
SELECT
    t1.tablename,
    t1.indexrelname as single_column_index,
    t2.indexrelname as composite_index,
    pg_size_pretty(pg_relation_size(t1.indexrelid)) as single_index_size,
    t1.idx_scan as single_index_usage,
    t2.idx_scan as composite_index_usage,
    CASE
        WHEN t1.idx_scan < t2.idx_scan * 0.1 THEN '🔴 DROP single column index (redundant)'
        WHEN t1.idx_scan < t2.idx_scan THEN '🟡 Consider dropping single column index'
        ELSE '✅ Both indexes useful'
    END as recommendation
FROM pg_stat_user_indexes t1
JOIN pg_stat_user_indexes t2 ON t1.tablename = t2.tablename
WHERE t1.schemaname = 'public'
AND t2.schemaname = 'public'
AND t1.indexrelname != t2.indexrelname
-- Sadece tek kolon ve composite index'leri karşılaştır
AND (SELECT count(*) FROM pg_index WHERE indexrelid = t1.indexrelid AND indnatts = 1) > 0
AND (SELECT count(*) FROM pg_index WHERE indexrelid = t2.indexrelid AND indnatts > 1) > 0;
```

---

## Sorgu Performans Analizi

### Yavaş Sorgu ve Long-Running Transaction İzleme

```sql
-- ============================================================================
-- Anlık Aktif Sorgu ve Transaction İzleme
-- ============================================================================

-- 1. Şu An Çalışan Tüm Sorgular (>100ms)
SELECT
    pid,
    NOW() - query_start as duration,
    state,
    wait_event_type,
    wait_event,
    usename as username,
    datname as database,
    client_addr,
    LEFT(query, 100) as query_preview,
    CASE
        WHEN NOW() - query_start > INTERVAL '5 minutes' THEN '🔴 CRITICAL - Very slow query'
        WHEN NOW() - query_start > INTERVAL '1 minute' THEN '🟡 WARNING - Slow query'
        WHEN NOW() - query_start > INTERVAL '10 seconds' THEN '🟠 MODERATE'
        ELSE '✅ OK'
    END as status
FROM pg_stat_activity
WHERE state != 'idle'
AND query NOT LIKE '%pg_stat_activity%'
AND NOW() - query_start > INTERVAL '100 milliseconds'
ORDER BY query_start ASC;

-- 2. Uzun Süre Açık Kalan Transaction'lar
SELECT
    pid,
    NOW() - xact_start as transaction_duration,
    NOW() - query_start as query_duration,
    state,
    usename,
    LEFT(query, 100) as query_preview,
    CASE
        WHEN NOW() - xact_start > INTERVAL '10 minutes' THEN '🔴 CRITICAL - Kill this transaction'
        WHEN NOW() - xact_start > INTERVAL '5 minutes' THEN '🟡 WARNING'
        ELSE '🟠 MONITOR'
    END as status
FROM pg_stat_activity
WHERE xact_start IS NOT NULL
AND NOW() - xact_start > INTERVAL '1 minute'
ORDER BY xact_start ASC;

-- 3. Idle in Transaction (Potansiyel Lock Problemi)
SELECT
    pid,
    NOW() - state_change as idle_duration,
    usename,
    datname,
    client_addr,
    LEFT(query, 100) as last_query,
    CASE
        WHEN NOW() - state_change > INTERVAL '5 minutes' THEN '🔴 CRITICAL - Connection leak?'
        WHEN NOW() - state_change > INTERVAL '1 minute' THEN '🟡 WARNING'
        ELSE '🟠 MONITOR'
    END as status
FROM pg_stat_activity
WHERE state = 'idle in transaction'
ORDER BY state_change ASC;

-- 4. Lock'lar ve Bekleyen Sorgular
SELECT
    blocked_locks.pid AS blocked_pid,
    blocked_activity.usename AS blocked_user,
    blocking_locks.pid AS blocking_pid,
    blocking_activity.usename AS blocking_user,
    blocked_activity.query AS blocked_statement,
    blocking_activity.query AS blocking_statement,
    NOW() - blocked_activity.query_start AS blocked_duration
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks
    ON blocking_locks.locktype = blocked_locks.locktype
    AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
    AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
    AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
    AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
    AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
    AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
    AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
    AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
    AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
    AND blocking_locks.pid != blocked_locks.pid
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;

-- 5. Sorguyu Öldür (Gerekirse)
-- SELECT pg_cancel_backend(PID); -- Yumuşak: Sorguyu iptal et
-- SELECT pg_terminate_backend(PID); -- Sert: Connection'ı kes
```

---

## Tablo Boyut ve Şişme Analizi

### Detaylı Tablo Büyüme Takibi

```sql
-- ============================================================================
-- Tablo Boyut Büyüme Analizi
-- ============================================================================

-- 1. Tablo Boyut Özeti (Tüm Tablolar)
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size('"' || tablename || '"')) as total_size,
    pg_size_pretty(pg_relation_size('"' || tablename || '"')) as table_only_size,
    pg_size_pretty(pg_indexes_size('"' || tablename || '"')) as indexes_size,
    pg_size_pretty(pg_total_relation_size('"' || tablename || '"') -
                   pg_relation_size('"' || tablename || '"')) as external_size,
    n_live_tup as row_count,
    CASE
        WHEN n_live_tup > 0 THEN pg_relation_size('"' || tablename || '"') / n_live_tup
        ELSE 0
    END as avg_row_size_bytes,
    ROUND(100.0 * pg_indexes_size('"' || tablename || '"') /
          NULLIF(pg_total_relation_size('"' || tablename || '"'), 0), 2) as index_percent
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size('"' || tablename || '"') DESC;

-- 2. Kritik Tablolar Büyüme Trendi (Manuel Kayıt Tut)
-- Bu sorguyu aylık çalıştır ve sonuçları Excel'e kaydet
SELECT
    NOW()::date as measurement_date,
    tablename,
    pg_total_relation_size('"' || tablename || '"') / (1024*1024) as total_size_mb,
    pg_relation_size('"' || tablename || '"') / (1024*1024) as table_size_mb,
    pg_indexes_size('"' || tablename || '"') / (1024*1024) as indexes_size_mb,
    n_live_tup as row_count
FROM pg_stat_user_tables
WHERE schemaname = 'public'
AND tablename IN (
    'PlantAnalyses',
    'UserSubscriptions',
    'AnalysisMessages',
    'SponsorshipCodes',
    'ReferralCodes',
    'Users',
    'AdminOperationLogs'
)
ORDER BY tablename;

-- 3. Son 7 Günlük Insert/Update/Delete Aktivitesi
SELECT
    tablename,
    n_tup_ins as inserts,
    n_tup_upd as updates,
    n_tup_del as deletes,
    n_tup_ins + n_tup_upd + n_tup_del as total_changes,
    n_live_tup as current_rows,
    ROUND(100.0 * (n_tup_ins + n_tup_upd + n_tup_del) / NULLIF(n_live_tup, 0), 2) as churn_rate,
    last_vacuum,
    last_autovacuum
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY (n_tup_ins + n_tup_upd + n_tup_del) DESC;
```

---

## Bağlantı ve Kaynak Kullanımı

```sql
-- ============================================================================
-- Bağlantı Havuzu ve Kaynak İzleme
-- ============================================================================

-- 1. Aktif Bağlantı Özeti
SELECT
    state,
    COUNT(*) as connection_count,
    MAX(NOW() - query_start) as max_duration,
    AVG(NOW() - query_start) as avg_duration
FROM pg_stat_activity
WHERE datname = current_database()
GROUP BY state
ORDER BY connection_count DESC;

-- 2. Kullanıcı Bazında Bağlantı Sayısı
SELECT
    usename,
    COUNT(*) as connection_count,
    MAX(NOW() - backend_start) as longest_connection,
    COUNT(CASE WHEN state = 'active' THEN 1 END) as active,
    COUNT(CASE WHEN state = 'idle' THEN 1 END) as idle,
    COUNT(CASE WHEN state = 'idle in transaction' THEN 1 END) as idle_in_transaction
FROM pg_stat_activity
WHERE datname = current_database()
GROUP BY usename
ORDER BY connection_count DESC;

-- 3. Client IP Bazında Bağlantılar
SELECT
    client_addr,
    COUNT(*) as connection_count,
    MAX(NOW() - backend_start) as longest_connection,
    array_agg(DISTINCT state) as states,
    array_agg(DISTINCT usename) as users
FROM pg_stat_activity
WHERE datname = current_database()
AND client_addr IS NOT NULL
GROUP BY client_addr
ORDER BY connection_count DESC;

-- 4. Database Limitleri ve Kullanım
SELECT
    setting as max_connections,
    (SELECT COUNT(*) FROM pg_stat_activity) as current_connections,
    ROUND(100.0 * (SELECT COUNT(*) FROM pg_stat_activity)::numeric / setting::numeric, 2) as usage_percent,
    CASE
        WHEN ROUND(100.0 * (SELECT COUNT(*) FROM pg_stat_activity)::numeric / setting::numeric, 2) > 80
            THEN '🔴 CRITICAL - Near connection limit'
        WHEN ROUND(100.0 * (SELECT COUNT(*) FROM pg_stat_activity)::numeric / setting::numeric, 2) > 60
            THEN '🟡 WARNING - High connection usage'
        ELSE '✅ OK'
    END as status
FROM pg_settings
WHERE name = 'max_connections';
```

---

## Cache Hit Ratio Analizi

```sql
-- ============================================================================
-- Cache Performans Analizi
-- ============================================================================

-- 1. Tablo Bazında Cache Hit Ratio
SELECT
    schemaname,
    tablename,
    heap_blks_read as disk_reads,
    heap_blks_hit as cache_hits,
    heap_blks_read + heap_blks_hit as total_reads,
    CASE
        WHEN (heap_blks_read + heap_blks_hit) > 0
        THEN ROUND(100.0 * heap_blks_hit / (heap_blks_read + heap_blks_hit), 2)
        ELSE 0
    END as cache_hit_ratio,
    CASE
        WHEN (heap_blks_read + heap_blks_hit) > 0 AND
             ROUND(100.0 * heap_blks_hit / (heap_blks_read + heap_blks_hit), 2) < 90
            THEN '🔴 LOW CACHE HIT - Table too large or infrequent access'
        WHEN (heap_blks_read + heap_blks_hit) > 0 AND
             ROUND(100.0 * heap_blks_hit / (heap_blks_read + heap_blks_hit), 2) < 95
            THEN '🟡 MODERATE'
        WHEN (heap_blks_read + heap_blks_hit) > 0
            THEN '✅ EXCELLENT'
        ELSE '⚪ No data'
    END as status
FROM pg_statio_user_tables
WHERE schemaname = 'public'
AND (heap_blks_read + heap_blks_hit) > 0
ORDER BY (heap_blks_read + heap_blks_hit) DESC
LIMIT 20;

-- 2. Index Bazında Cache Hit Ratio
SELECT
    schemaname,
    tablename,
    indexrelname as index_name,
    idx_blks_read as disk_reads,
    idx_blks_hit as cache_hits,
    idx_blks_read + idx_blks_hit as total_reads,
    CASE
        WHEN (idx_blks_read + idx_blks_hit) > 0
        THEN ROUND(100.0 * idx_blks_hit / (idx_blks_read + idx_blks_hit), 2)
        ELSE 0
    END as cache_hit_ratio,
    CASE
        WHEN (idx_blks_read + idx_blks_hit) > 0 AND
             ROUND(100.0 * idx_blks_hit / (idx_blks_read + idx_blks_hit), 2) < 90
            THEN '🔴 LOW CACHE HIT'
        WHEN (idx_blks_read + idx_blks_hit) > 0 AND
             ROUND(100.0 * idx_blks_hit / (idx_blks_read + idx_blks_hit), 2) < 95
            THEN '🟡 MODERATE'
        WHEN (idx_blks_read + idx_blks_hit) > 0
            THEN '✅ EXCELLENT'
        ELSE '⚪ No data'
    END as status
FROM pg_statio_user_indexes
WHERE schemaname = 'public'
AND (idx_blks_read + idx_blks_hit) > 0
ORDER BY (idx_blks_read + idx_blks_hit) DESC
LIMIT 20;

-- 3. Global Cache İstatistikleri
SELECT
    'Shared Buffers' as metric,
    pg_size_pretty(current_setting('shared_buffers')::bigint *
                   (SELECT setting FROM pg_settings WHERE name = 'block_size')::bigint) as value
UNION ALL
SELECT
    'Effective Cache Size',
    pg_size_pretty(current_setting('effective_cache_size')::bigint *
                   (SELECT setting FROM pg_settings WHERE name = 'block_size')::bigint)
UNION ALL
SELECT
    'Work Mem',
    pg_size_pretty(current_setting('work_mem')::bigint)
UNION ALL
SELECT
    'Maintenance Work Mem',
    pg_size_pretty(current_setting('maintenance_work_mem')::bigint);
```

---

## Maintenance İşlemleri

### Düzenli Bakım Scriptleri

```sql
-- ============================================================================
-- MANUEL: Maintenance İşlemleri (Haftalık/Aylık)
-- ============================================================================

-- 1. VACUUM ve ANALYZE (Haftalık - Düşük Trafik Saatlerinde)
-- Kritik tablolar için manuel VACUUM

-- PlantAnalyses
VACUUM (VERBOSE, ANALYZE) "PlantAnalyses";

-- UserSubscriptions
VACUUM (VERBOSE, ANALYZE) "UserSubscriptions";

-- AnalysisMessages
VACUUM (VERBOSE, ANALYZE) "AnalysisMessages";

-- SponsorshipCodes
VACUUM (VERBOSE, ANALYZE) "SponsorshipCodes";

-- ReferralCodes
VACUUM (VERBOSE, ANALYZE) "ReferralCodes";

-- Users
VACUUM (VERBOSE, ANALYZE) "Users";

-- 2. REINDEX (Aylık - Maintenance Penceresinde)
-- Index fragmentasyon düzeltme

-- Tablo bazında reindex (CONCURRENTLY ile)
REINDEX TABLE CONCURRENTLY "PlantAnalyses";
REINDEX TABLE CONCURRENTLY "UserSubscriptions";
REINDEX TABLE CONCURRENTLY "AnalysisMessages";

-- 3. VACUUM FULL (3 Ayda Bir - Downtime Gerektirir)
-- Sadece yüksek bloat olan tablolar için
-- ⚠️ UYARI: VACUUM FULL tabloyu kilitler, downtime gerektirir

-- Önce backup al
-- pg_dump -t "PlantAnalyses" ziraai_db > plant_analyses_backup.sql

-- VACUUM FULL "PlantAnalyses";

-- 4. Statistics Reset (Aylık Başlangıçta)
-- SELECT pg_stat_reset(); -- Tüm istatistikleri sıfırla
-- SELECT pg_stat_reset_shared('bgwriter'); -- Sadece bgwriter istatistiklerini sıfırla

-- 5. Index Bloat Düzeltme
-- Bloat > %30 olan indexler için REINDEX

-- Bloat analizi (pg_repack extension ile daha iyi)
SELECT
    schemaname,
    tablename,
    indexrelname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    idx_scan,
    CASE
        WHEN idx_scan < 100 THEN '🟡 Consider REINDEX'
        ELSE '✅ OK'
    END as recommendation
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND pg_relation_size(indexrelid) > 10485760 -- >10MB
ORDER BY pg_relation_size(indexrelid) DESC;
```

---

## Alarm ve Uyarı Eşikleri

### Performans Metrik Eşikleri

```sql
-- ============================================================================
-- Alarm Eşikleri - Haftalık Kontrol
-- ============================================================================

-- 1. Cache Hit Ratio Kontrolü
SELECT
    'Cache Hit Ratio' as metric,
    ROUND(100.0 * sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0), 2) as current_value,
    '95%' as warning_threshold,
    '90%' as critical_threshold,
    CASE
        WHEN ROUND(100.0 * sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0), 2) < 90
            THEN '🔴 CRITICAL'
        WHEN ROUND(100.0 * sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0), 2) < 95
            THEN '🟡 WARNING'
        ELSE '✅ OK'
    END as status
FROM pg_statio_user_tables
WHERE schemaname = 'public';

-- 2. Dead Row Percentage Kontrolü
SELECT
    'Dead Row Percentage' as metric,
    MAX(ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2)) as max_dead_percent,
    '10%' as warning_threshold,
    '20%' as critical_threshold,
    CASE
        WHEN MAX(ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2)) > 20
            THEN '🔴 CRITICAL - VACUUM needed'
        WHEN MAX(ROUND(100.0 * n_dead_tup / NULLIF(n_live_tup + n_dead_tup, 0), 2)) > 10
            THEN '🟡 WARNING - Schedule VACUUM'
        ELSE '✅ OK'
    END as status
FROM pg_stat_user_tables
WHERE schemaname = 'public';

-- 3. Bağlantı Kullanım Oranı
SELECT
    'Connection Usage' as metric,
    ROUND(100.0 * (SELECT COUNT(*) FROM pg_stat_activity)::numeric /
          (SELECT setting::numeric FROM pg_settings WHERE name = 'max_connections'), 2) as usage_percent,
    '60%' as warning_threshold,
    '80%' as critical_threshold,
    CASE
        WHEN ROUND(100.0 * (SELECT COUNT(*) FROM pg_stat_activity)::numeric /
                   (SELECT setting::numeric FROM pg_settings WHERE name = 'max_connections'), 2) > 80
            THEN '🔴 CRITICAL'
        WHEN ROUND(100.0 * (SELECT COUNT(*) FROM pg_stat_activity)::numeric /
                   (SELECT setting::numeric FROM pg_settings WHERE name = 'max_connections'), 2) > 60
            THEN '🟡 WARNING'
        ELSE '✅ OK'
    END as status;

-- 4. Database Boyut Kontrolü
SELECT
    'Database Size' as metric,
    pg_size_pretty(pg_database_size(current_database())) as current_size,
    '20 GB' as warning_threshold,
    '30 GB' as critical_threshold,
    CASE
        WHEN pg_database_size(current_database()) > 30 * 1024^3
            THEN '🔴 CRITICAL'
        WHEN pg_database_size(current_database()) > 20 * 1024^3
            THEN '🟡 WARNING'
        ELSE '✅ OK'
    END as status;

-- 5. Long Running Queries
SELECT
    'Long Running Queries' as metric,
    COUNT(*) as query_count,
    '5' as warning_threshold,
    '10' as critical_threshold,
    CASE
        WHEN COUNT(*) > 10 THEN '🔴 CRITICAL'
        WHEN COUNT(*) > 5 THEN '🟡 WARNING'
        ELSE '✅ OK'
    END as status
FROM pg_stat_activity
WHERE state = 'active'
AND NOW() - query_start > INTERVAL '5 seconds'
AND query NOT LIKE '%pg_stat_activity%';

-- 6. Index Usage Ratio
SELECT
    'Index Usage Ratio' as metric,
    ROUND(100.0 * SUM(idx_scan) / NULLIF(SUM(seq_scan + idx_scan), 0), 2) as index_usage_percent,
    '80%' as warning_threshold,
    '70%' as critical_threshold,
    CASE
        WHEN ROUND(100.0 * SUM(idx_scan) / NULLIF(SUM(seq_scan + idx_scan), 0), 2) < 70
            THEN '🔴 CRITICAL - Missing indexes'
        WHEN ROUND(100.0 * SUM(idx_scan) / NULLIF(SUM(seq_scan + idx_scan), 0), 2) < 80
            THEN '🟡 WARNING'
        ELSE '✅ OK'
    END as status
FROM pg_stat_user_tables
WHERE schemaname = 'public'
AND n_live_tup > 1000;
```

---

## DBeaver Kullanım İpuçları

### 1. Script Kaydetme
- `File > Save As` ile scriptleri kaydet
- Klasör yapısı: `ZiraAI_DB_Scripts/Weekly/` ve `Monthly/`

### 2. SQL Scheduler (Enterprise Edition)
- DBeaver Enterprise varsa SQL Task Scheduler kullan
- Haftalık/Aylık scriptleri otomatik çalıştır

### 3. Result Export
- Sonuçları Excel'e export et: `Right Click > Export Data > Excel`
- Tarih bazlı dosyalar: `performance_metrics_2025_12_05.xlsx`

### 4. Query History
- Tüm çalıştırdığın sorguları sakla: `SQL Editor > SQL History`
- Trend analizi için geçmiş sonuçları sakla

### 5. Visual Explain
- Yavaş sorgular için EXPLAIN ANALYZE kullan:
```sql
EXPLAIN ANALYZE
SELECT * FROM "PlantAnalyses"
WHERE "UserId" = 123 AND "AnalysisDate" >= '2025-01-01';
```

---

## Performans Takip Excel Template

### Aylık Excel Şablonu

| Tarih | PlantAnalyses (GB) | UserSubscriptions (MB) | Cache Hit % | Dead Row % | Avg Query Time (ms) | Active Connections |
|-------|-------------------|------------------------|-------------|------------|--------------------|--------------------|
| 2025-12-05 | | | | | | |
| 2026-01-05 | | | | | | |

---

## Acil Durum Prosedürleri

### Performans Sorunu Troubleshooting

```sql
-- ============================================================================
-- ACİL: Performans Sorunu Çözüm Adımları
-- ============================================================================

-- 1. Şu an çalışan yavaş sorguları bul
SELECT pid, NOW() - query_start as duration, query
FROM pg_stat_activity
WHERE state = 'active'
AND NOW() - query_start > INTERVAL '30 seconds'
ORDER BY query_start ASC;

-- 2. Lock'lanmış sorguları bul
SELECT * FROM pg_locks WHERE NOT granted;

-- 3. Bloklayan query'leri öldür (Dikkatli!)
-- SELECT pg_terminate_backend(PID);

-- 4. Acil VACUUM (Çok dead row varsa)
VACUUM (VERBOSE) "PlantAnalyses";

-- 5. Statistics refresh (Query planner için)
ANALYZE "PlantAnalyses";

-- 6. Connection reset (Connection pool sıkıntısı varsa)
-- Railway/Heroku: Restart database
```

---

## Özet Checklist

### ✅ Haftalık (Her Pazartesi Sabahı - 10 dakika)
- [ ] Hızlı sağlık kontrolü sorgusu çalıştır
- [ ] Cache hit ratio kontrol (>95% olmalı)
- [ ] Dead row percentage kontrol (<10% olmalı)
- [ ] Kritik tablolar boyut kontrol
- [ ] Alarm eşikleri kontrol

### ✅ Aylık (Her Ayın 1'i - 30 dakika)
- [ ] Detaylı index kullanım analizi
- [ ] Kullanılmayan indexleri tespit et
- [ ] Tablo şişme (bloat) analizi
- [ ] Sorgu performans istatistikleri export
- [ ] Excel'e performans metriklerini kaydet
- [ ] Maintenance işlemlerini planla (VACUUM/REINDEX)

### ✅ 3 Ayda Bir (Derin Analiz - 2 saat)
- [ ] pg_stat_statements detaylı analiz
- [ ] Duplicate/Redundant index temizliği
- [ ] VACUUM FULL (downtime ile)
- [ ] Partitioning değerlendirmesi
- [ ] Archive stratejisi gözden geçir

---

## İletişim ve Destek

**Sorular veya sorunlar için:**
- PostgreSQL Docs: https://www.postgresql.org/docs/
- DBeaver Community: https://dbeaver.io/
- ZiraAI DevOps Team: [Email/Slack Channel]

---

**Son Güncelleme**: 2025-12-05
**Versiyon**: 1.0
**Hazırlayan**: Backend Performance Team
