-- ============================================================================
-- Phase 2: Cleanup Verification Script
-- ============================================================================
-- Run this after executing 004_phase2_index_cleanup.sql
-- Verifies all 6 JSONB GIN indexes were dropped successfully
-- ============================================================================

-- ============================================================================
-- PART 1: VERIFY INDEXES ARE DROPPED
-- ============================================================================

\echo '';
\echo '============================================================================';
\echo 'Phase 2 Cleanup Verification';
\echo '============================================================================';
\echo '';

-- Check if any GIN indexes remain on PlantAnalyses
SELECT
    COUNT(*) as remaining_gin_indexes,
    CASE
        WHEN COUNT(*) = 0 THEN '✅ SUCCESS: All 6 JSONB GIN indexes dropped'
        ELSE '⚠️ WARNING: ' || COUNT(*)::text || ' GIN indexes still exist'
    END as status
FROM pg_indexes
WHERE schemaname = 'public'
AND tablename = 'PlantAnalyses'
AND indexname LIKE '%_GIN';

-- List any remaining GIN indexes (should be empty)
\echo '';
\echo 'Remaining GIN indexes (should be empty):';
SELECT indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'public'
AND tablename = 'PlantAnalyses'
AND indexname LIKE '%_GIN'
ORDER BY indexname;

-- ============================================================================
-- PART 2: TABLE SIZE ANALYSIS
-- ============================================================================

\echo '';
\echo '============================================================================';
\echo 'Storage Analysis';
\echo '============================================================================';
\echo '';

SELECT
    'PlantAnalyses' as table_name,
    pg_size_pretty(pg_total_relation_size('"PlantAnalyses"')) as total_size,
    pg_size_pretty(pg_relation_size('"PlantAnalyses"')) as table_only_size,
    pg_size_pretty(pg_total_relation_size('"PlantAnalyses"') - pg_relation_size('"PlantAnalyses"')) as all_indexes_size,
    (SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'PlantAnalyses') as total_indexes
FROM pg_class
WHERE relname = 'PlantAnalyses';

-- ============================================================================
-- PART 3: REMAINING INDEXES ON PlantAnalyses
-- ============================================================================

\echo '';
\echo '============================================================================';
\echo 'Remaining Indexes on PlantAnalyses (Should include Phase 1 indexes)';
\echo '============================================================================';
\echo '';

SELECT
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    idx_scan as times_used,
    CASE
        WHEN indexname LIKE 'IX_%' THEN '✅ Phase 1 index'
        WHEN indexname LIKE 'PK_%' THEN '✅ Primary key'
        WHEN indexname LIKE 'FK_%' THEN '✅ Foreign key'
        WHEN indexname LIKE 'IDX_%' AND indexname NOT LIKE '%_GIN' THEN '✅ Regular index'
        ELSE '⚠️ Other'
    END as index_type
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
AND tablename = 'PlantAnalyses'
ORDER BY pg_relation_size(indexrelid) DESC;

-- ============================================================================
-- PART 4: STATISTICS UPDATE CHECK
-- ============================================================================

\echo '';
\echo '============================================================================';
\echo 'Table Statistics Check';
\echo '============================================================================';
\echo '';

SELECT
    schemaname,
    tablename,
    last_analyze,
    last_autoanalyze,
    n_live_tup as row_count,
    n_dead_tup as dead_rows,
    CASE
        WHEN last_analyze > NOW() - INTERVAL '1 hour' THEN '✅ Recently analyzed'
        WHEN last_autoanalyze > NOW() - INTERVAL '1 day' THEN '🟡 Auto-analyzed recently'
        ELSE '⚠️ Statistics may be stale'
    END as statistics_status
FROM pg_stat_user_tables
WHERE schemaname = 'public'
AND tablename = 'PlantAnalyses';

-- ============================================================================
-- PART 5: WRITE PERFORMANCE TEST (OPTIONAL - RUN MANUALLY)
-- ============================================================================

/*
-- Test INSERT performance after cleanup
-- Compare with baseline timing before cleanup
-- Expected: 25-35% faster

EXPLAIN ANALYZE
INSERT INTO "PlantAnalyses"
    ("UserId", "AnalysisDate", "AnalysisStatus", "CropType",
     "DetailedAnalysisData", "HealthAssessment", "NutrientStatus",
     "PestDisease", "PlantIdentification", "Recommendations",
     "CreatedDate", "IsDeleted")
VALUES
    (1, NOW(), 'completed', 'test_crop',
     '{"test": "data"}'::jsonb, '{"test": "data"}'::jsonb, '{"test": "data"}'::jsonb,
     '{"test": "data"}'::jsonb, '{"test": "data"}'::jsonb, '{"test": "data"}'::jsonb,
     NOW(), false);

-- After running, rollback the test insert:
-- ROLLBACK;
*/

-- ============================================================================
-- SUMMARY
-- ============================================================================

\echo '';
\echo '============================================================================';
\echo 'Verification Summary';
\echo '============================================================================';
\echo '';
\echo 'Expected Results:';
\echo '  ✅ 0 GIN indexes remaining on PlantAnalyses';
\echo '  ✅ Storage reduced by 10-15%';
\echo '  ✅ All Phase 1 indexes (IX_*) still present';
\echo '  ✅ Statistics recently updated';
\echo '';
\echo 'If verification passes:';
\echo '  1. Monitor application for any query issues (should be none)';
\echo '  2. Test INSERT/UPDATE operations (should be faster)';
\echo '  3. Proceed to Phase 3 (code optimization)';
\echo '';
\echo 'If verification fails:';
\echo '  1. Check error logs';
\echo '  2. Consider rollback if critical issues found';
\echo '  3. Investigate which indexes failed to drop';
\echo '';
\echo '============================================================================';
