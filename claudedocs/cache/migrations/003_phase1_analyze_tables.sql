-- ============================================================================
-- Phase 1: Table Statistics Update (DBeaver Compatible)
-- ============================================================================
-- Run this after creating all indexes to update query planner statistics
-- This helps PostgreSQL choose optimal query plans with the new indexes
-- ============================================================================

-- Update statistics for PlantAnalyses
ANALYZE "PlantAnalyses";

-- Update statistics for UserSubscriptions
ANALYZE "UserSubscriptions";

-- Update statistics for AnalysisMessages
ANALYZE "AnalysisMessages";

-- Update statistics for SponsorshipCodes
ANALYZE "SponsorshipCodes";

-- Update statistics for ReferralCodes
ANALYZE "ReferralCodes";

-- ============================================================================
-- VERIFICATION: Check if analyze completed successfully
-- ============================================================================

SELECT
    schemaname,
    tablename,
    last_analyze,
    last_autoanalyze,
    n_live_tup as row_count,
    n_dead_tup as dead_rows
FROM pg_stat_user_tables
WHERE schemaname = 'public'
AND tablename IN ('PlantAnalyses', 'UserSubscriptions', 'AnalysisMessages', 'SponsorshipCodes', 'ReferralCodes')
ORDER BY tablename;

-- ============================================================================
-- SUCCESS MESSAGE
-- ============================================================================
SELECT '✅ Phase 1 Table Analysis Completed Successfully' as status,
       '5 tables analyzed: PlantAnalyses, UserSubscriptions, AnalysisMessages, SponsorshipCodes, ReferralCodes' as details;
