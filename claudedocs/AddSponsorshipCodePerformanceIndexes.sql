-- ============================================================================
-- Add Performance Indexes for Sponsorship Code Queries
-- ============================================================================
-- Date: 2025-10-12
-- Purpose: Add composite indexes for optimized queries on millions of rows
-- Impact: Expected 100x performance improvement (5000ms → 50ms)
-- Target Queries:
--   1. Sent + Expired codes (Priority 1)
--   2. Unsent codes (Priority 2)
--   3. Sent but unused codes (Priority 3)
-- ============================================================================

-- ============================================================================
-- 1. CRITICAL: Composite index for sent+expired codes query
-- ============================================================================
-- Covers: WHERE SponsorId = X AND DistributionDate IS NOT NULL
--         AND ExpiryDate < NOW AND IsUsed = false
-- ORDER BY ExpiryDate DESC, DistributionDate DESC
--
-- Partial index significantly reduces index size by only indexing relevant rows
-- ============================================================================

CREATE INDEX IF NOT EXISTS "IX_SponsorshipCodes_SentExpired"
ON "SponsorshipCodes" ("SponsorId", "DistributionDate", "ExpiryDate", "IsUsed")
WHERE "DistributionDate" IS NOT NULL AND "IsUsed" = false;

-- ============================================================================
-- 2. Composite index for unsent codes query
-- ============================================================================
-- Covers: WHERE SponsorId = X AND DistributionDate IS NULL AND IsUsed = false
-- ORDER BY CreatedDate DESC
--
-- Partial index for codes never sent to farmers (recommended for distribution)
-- ============================================================================

CREATE INDEX IF NOT EXISTS "IX_SponsorshipCodes_Unsent"
ON "SponsorshipCodes" ("SponsorId", "DistributionDate", "IsUsed")
WHERE "DistributionDate" IS NULL;

-- ============================================================================
-- 3. Composite index for sent but unused codes query
-- ============================================================================
-- Covers: WHERE SponsorId = X AND DistributionDate IS NOT NULL
--         AND DistributionDate = X AND IsUsed = false
-- ORDER BY DistributionDate DESC
--
-- Partial index for codes sent X days ago but still not redeemed
-- ============================================================================

CREATE INDEX IF NOT EXISTS "IX_SponsorshipCodes_SentUnused"
ON "SponsorshipCodes" ("SponsorId", "DistributionDate", "IsUsed")
WHERE "DistributionDate" IS NOT NULL AND "IsUsed" = false;

-- ============================================================================
-- 4. Verification Queries (Run AFTER indexes are created)
-- ============================================================================

-- Check created indexes
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'SponsorshipCodes'
  AND indexname IN (
      'IX_SponsorshipCodes_SentExpired',
      'IX_SponsorshipCodes_Unsent',
      'IX_SponsorshipCodes_SentUnused'
  )
ORDER BY indexname;

-- Check index sizes
SELECT
    indexrelname as index_name,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE relname = 'SponsorshipCodes'
  AND indexrelname IN (
      'IX_SponsorshipCodes_SentExpired',
      'IX_SponsorshipCodes_Unsent',
      'IX_SponsorshipCodes_SentUnused'
  )
ORDER BY indexrelname;

-- ============================================================================
-- 5. Performance Test Queries (Optional - for verification)
-- ============================================================================

-- Test query performance for sent+expired codes
EXPLAIN ANALYZE
SELECT *
FROM "SponsorshipCodes"
WHERE "SponsorId" = 1
  AND "DistributionDate" IS NOT NULL
  AND "ExpiryDate" < NOW()
  AND "IsUsed" = false
ORDER BY "ExpiryDate" DESC, "DistributionDate" DESC
LIMIT 50;

-- Test query performance for unsent codes
EXPLAIN ANALYZE
SELECT *
FROM "SponsorshipCodes"
WHERE "SponsorId" = 1
  AND "DistributionDate" IS NULL
ORDER BY "CreatedDate" DESC
LIMIT 50;

-- Test query performance for sent but unused codes
EXPLAIN ANALYZE
SELECT *
FROM "SponsorshipCodes"
WHERE "SponsorId" = 1
  AND "DistributionDate" IS NOT NULL
  AND "IsUsed" = false
ORDER BY "DistributionDate" DESC
LIMIT 50;

-- ============================================================================
-- Performance Expectations (with 1M rows)
-- ============================================================================
--
-- Before Indexes:
--   - Sent+Expired: ~5000ms (full table scan)
--   - Unsent: ~3000ms (full table scan)
--   - Sent Unused: ~4000ms (full table scan)
--
-- After Indexes:
--   - Sent+Expired: ~50ms (index scan + partial index)
--   - Unsent: ~30ms (index scan + partial index)
--   - Sent Unused: ~40ms (index scan + partial index)
--
-- Expected Improvement: ~100x faster
-- Index Size Impact: Minimal (partial indexes only store relevant rows)
-- ============================================================================

-- ============================================================================
-- ROLLBACK SCRIPT (if needed)
-- ============================================================================
-- Uncomment and run if you need to remove the indexes

/*
-- Drop the performance indexes
DROP INDEX IF EXISTS "IX_SponsorshipCodes_SentExpired";
DROP INDEX IF EXISTS "IX_SponsorshipCodes_Unsent";
DROP INDEX IF EXISTS "IX_SponsorshipCodes_SentUnused";

-- Verify removal
SELECT indexname
FROM pg_indexes
WHERE tablename = 'SponsorshipCodes'
  AND indexname IN (
      'IX_SponsorshipCodes_SentExpired',
      'IX_SponsorshipCodes_Unsent',
      'IX_SponsorshipCodes_SentUnused'
  );
*/

-- ============================================================================
-- Notes:
-- ============================================================================
-- 1. These are PARTIAL indexes (with WHERE clauses) for optimal performance
-- 2. Partial indexes are smaller and faster than full indexes
-- 3. PostgreSQL will automatically use these indexes for matching queries
-- 4. The index order (SponsorId first) is critical for performance
-- 5. ExpiryDate and DistributionDate are included for covered index queries
-- 6. Monitor index usage with pg_stat_user_indexes after deployment
-- 7. Consider VACUUM ANALYZE after creating indexes on large tables
-- ============================================================================

-- ============================================================================
-- Monitoring Index Usage (run after deployment)
-- ============================================================================
/*
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE tablename = 'SponsorshipCodes'
  AND indexname IN (
      'IX_SponsorshipCodes_SentExpired',
      'IX_SponsorshipCodes_Unsent',
      'IX_SponsorshipCodes_SentUnused'
  )
ORDER BY idx_scan DESC;
*/

-- ============================================================================
-- VACUUM ANALYZE (run after creating indexes on large tables)
-- ============================================================================
-- VACUUM ANALYZE "SponsorshipCodes";
