-- ============================================================================
-- Phase 1: Index Verification Script
-- ============================================================================
-- Run this to verify all 14 indexes were created successfully
-- ============================================================================

SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
AND indexname IN (
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
ORDER BY tablename, indexname;

-- ============================================================================
-- INDEX SIZE CHECK
-- ============================================================================

SELECT
    relname as tablename,
    indexrelname as indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    idx_scan as times_used,
    idx_tup_read as tuples_read
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
ORDER BY pg_relation_size(indexrelid) DESC;

-- ============================================================================
-- SUMMARY
-- ============================================================================

SELECT
    COUNT(*) as indexes_created,
    CASE
        WHEN COUNT(*) = 13 THEN '✅ All 13 new indexes created successfully'
        ELSE '⚠️ Expected 13 indexes, found ' || COUNT(*)::text
    END as status
FROM pg_indexes
WHERE schemaname = 'public'
AND indexname IN (
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
);
