-- =====================================================
-- Messaging Features Rollback Script
-- Purpose: Safely remove MessagingFeatures table and data
-- Date: 2025-10-19
-- CAUTION: This will delete all feature configuration data!
-- =====================================================

-- Backup data before rollback (optional)
-- CREATE TABLE "MessagingFeatures_Backup" AS SELECT * FROM "MessagingFeatures";

-- Drop foreign key constraints first
ALTER TABLE IF EXISTS "MessagingFeatures"
    DROP CONSTRAINT IF EXISTS "FK_MessagingFeatures_CreatedBy";

ALTER TABLE IF EXISTS "MessagingFeatures"
    DROP CONSTRAINT IF EXISTS "FK_MessagingFeatures_UpdatedBy";

-- Drop indexes
DROP INDEX IF EXISTS "idx_messaging_features_name";
DROP INDEX IF EXISTS "idx_messaging_features_enabled";
DROP INDEX IF EXISTS "idx_messaging_features_tier";

-- Drop table
DROP TABLE IF EXISTS "MessagingFeatures" CASCADE;

-- Verification
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public'
        AND table_name = 'MessagingFeatures'
    ) THEN
        RAISE NOTICE '✅ MessagingFeatures table removed successfully';
    ELSE
        RAISE WARNING '❌ MessagingFeatures table still exists';
    END IF;
END $$;
