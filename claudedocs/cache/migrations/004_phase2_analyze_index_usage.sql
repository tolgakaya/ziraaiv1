-- ============================================================================
-- Phase 2: Index Usage Analysis Script
-- ============================================================================
-- Run this on staging database to identify unused indexes
-- This helps determine which indexes can be safely dropped
-- ============================================================================

-- ============================================================================
-- PART 1: FIND UNUSED INDEXES (idx_scan = 0)
-- ============================================================================

SELECT
    schemaname,
    relname as tablename,
    indexrelname as indexname,
    idx_scan as times_used,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    pg_relation_size(indexrelid) as size_bytes
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND idx_scan = 0  -- Never used since last stats reset
AND indexrelname LIKE '%_GIN'  -- Focus on JSONB GIN indexes
ORDER BY pg_relation_size(indexrelid) DESC;

-- ============================================================================
-- PART 2: FIND LOW-USAGE INDEXES (idx_scan < 100)
-- ============================================================================

SELECT
    schemaname,
    relname as tablename,
    indexrelname as indexname,
    idx_scan as times_used,
    idx_tup_read as tuples_read,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    CASE
        WHEN idx_scan = 0 THEN '🔴 NEVER USED - Consider dropping'
        WHEN idx_scan < 10 THEN '🟡 RARELY USED - Consider dropping'
        WHEN idx_scan < 100 THEN '🟠 LOW USAGE - Monitor'
        ELSE '🟢 ACTIVE'
    END as usage_status
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND indexrelname LIKE '%_GIN'
ORDER BY idx_scan ASC, pg_relation_size(indexrelid) DESC;

-- ============================================================================
-- PART 3: ALL JSONB GIN INDEXES ON PlantAnalyses
-- ============================================================================

SELECT
    indexrelname as indexname,
    idx_scan as times_used,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND relname = 'PlantAnalyses'
AND indexrelname LIKE '%_GIN'
ORDER BY idx_scan ASC;

-- ============================================================================
-- PART 4: TOTAL STORAGE USED BY UNUSED INDEXES
-- ============================================================================

SELECT
    COUNT(*) as unused_index_count,
    pg_size_pretty(SUM(pg_relation_size(indexrelid))) as total_wasted_storage,
    SUM(pg_relation_size(indexrelid)) as wasted_bytes
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND idx_scan = 0
AND indexrelname LIKE '%_GIN';

-- ============================================================================
-- PART 5: SINGLE-COLUMN INDEXES THAT MIGHT BE REDUNDANT
-- ============================================================================

SELECT
    schemaname,
    relname as tablename,
    indexrelname as indexname,
    idx_scan as times_used,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND relname = 'PlantAnalyses'
AND indexrelname IN (
    'IDX_PlantAnalyses_CropType',
    'IDX_PlantAnalyses_Location',
    'IDX_PlantAnalyses_FarmerId'
)
ORDER BY idx_scan ASC;

-- ============================================================================
-- PART 6: WHEN WERE STATS LAST RESET?
-- ============================================================================

SELECT
    stats_reset as last_reset,
    NOW() - stats_reset as stats_age
FROM pg_stat_database
WHERE datname = current_database();

-- ============================================================================
-- INSTRUCTIONS FOR USER
-- ============================================================================

SELECT '
📊 INDEX USAGE ANALYSIS COMPLETE

Next Steps:
1. Review "NEVER USED" indexes (idx_scan = 0)
2. Check if JSONB fields are queried in code (grep codebase)
3. Create drop list for Phase 2 cleanup script
4. Estimate storage savings from cleanup

Notes:
- Stats age shows how long usage data has been collected
- Older stats = more reliable usage patterns
- If stats were recently reset, wait longer before dropping indexes
- Always test on staging before production

Expected Results:
- 30-35 unused JSONB GIN indexes
- 15-20% storage reduction potential
- 25-35% write performance improvement
' as instructions;
