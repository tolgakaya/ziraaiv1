-- ============================================================================
-- Migration: AddMessagingStatusIndexes
-- Date: 2025-10-21
-- Purpose: Add performance indexes for messaging status queries
-- ============================================================================

-- Create composite index for messaging status queries
-- This index optimizes the GetMessagingStatusForAnalysesAsync query
-- which groups messages by PlantAnalysisId and filters by IsDeleted
CREATE INDEX IF NOT EXISTS "IX_AnalysisMessages_PlantAnalysisId_IsDeleted_SentDate"
ON "AnalysisMessages" ("PlantAnalysisId", "IsDeleted", "SentDate" DESC)
INCLUDE ("FromUserId", "ToUserId", "IsRead", "Message");

-- Verification query
-- This should show the new index
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'AnalysisMessages'
ORDER BY indexname;
