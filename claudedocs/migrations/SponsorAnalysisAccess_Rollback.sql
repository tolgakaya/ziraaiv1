-- =============================================
-- Rollback: Drop SponsorAnalysisAccess Table
-- Purpose: Rollback migration for SponsorAnalysisAccess
-- Date: 2025-10-18
-- WARNING: This will delete all sponsor access tracking data
-- =============================================

-- Drop indexes first
DROP INDEX IF EXISTS "IX_SponsorAnalysisAccess_SponsorId_PlantAnalysisId";
DROP INDEX IF EXISTS "IX_SponsorAnalysisAccess_AccessLevel";
DROP INDEX IF EXISTS "IX_SponsorAnalysisAccess_FirstViewedDate";
DROP INDEX IF EXISTS "IX_SponsorAnalysisAccess_FarmerId";
DROP INDEX IF EXISTS "IX_SponsorAnalysisAccess_PlantAnalysisId";
DROP INDEX IF EXISTS "IX_SponsorAnalysisAccess_SponsorId";

-- Drop table (foreign key constraints will be dropped automatically)
DROP TABLE IF EXISTS "SponsorAnalysisAccess";

-- Rollback complete
