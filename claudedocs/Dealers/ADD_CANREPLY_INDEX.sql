-- ============================================================================
-- Performance Index for canReply Check
-- ============================================================================
-- Purpose: Optimize HasSponsorMessagedAnalysisAsync query performance
-- Date: 2025-10-27
-- ============================================================================

-- Create composite partial index for sponsor message checks
-- This index covers the exact query pattern used by HasSponsorMessagedAnalysisAsync
-- Partial index only includes non-deleted messages (smaller, faster)

CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_PlantAnalysisId_FromUserId_IsDeleted"
ON "AnalysisMessages" ("PlantAnalysisId", "FromUserId", "IsDeleted")
WHERE "IsDeleted" = false;

-- ============================================================================
-- Verification Query
-- ============================================================================

-- Check if index was created successfully
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'AnalysisMessages'
  AND indexname = 'IX_AnalysisMessages_PlantAnalysisId_FromUserId_IsDeleted';

-- ============================================================================
-- Performance Test Query
-- ============================================================================

-- Test the query that uses this index
EXPLAIN ANALYZE
SELECT EXISTS(
    SELECT 1
    FROM "AnalysisMessages"
    WHERE "PlantAnalysisId" = 74
      AND "FromUserId" = 159
      AND "IsDeleted" = false
);

-- Expected: Index Scan using IX_AnalysisMessages_PlantAnalysisId_FromUserId_IsDeleted
-- Execution time: <5ms
