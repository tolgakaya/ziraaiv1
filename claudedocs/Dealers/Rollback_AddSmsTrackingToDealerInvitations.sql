-- =====================================================
-- Rollback Migration: Remove SMS Tracking Fields from DealerInvitations
-- Date: 2025-01-25
-- Description: Rollback script to remove LinkSentDate, LinkSentVia, and LinkDelivered columns
--              if the migration needs to be reverted
-- =====================================================

-- WARNING: This will permanently delete data in these columns!
-- Make sure to backup data before running this rollback.

-- STEP 1: Drop the index
DROP INDEX IF EXISTS public."IX_DealerInvitations_LinkSentDate";

-- STEP 2: Drop the columns
ALTER TABLE public."DealerInvitations"
DROP COLUMN IF EXISTS "LinkDelivered";

ALTER TABLE public."DealerInvitations"
DROP COLUMN IF EXISTS "LinkSentVia";

ALTER TABLE public."DealerInvitations"
DROP COLUMN IF EXISTS "LinkSentDate";

-- =====================================================
-- Verification Query
-- =====================================================
-- Run this to verify the rollback was successful:
-- SELECT column_name
-- FROM information_schema.columns
-- WHERE table_name = 'DealerInvitations'
-- AND column_name IN ('LinkSentDate', 'LinkSentVia', 'LinkDelivered');
--
-- Expected result: 0 rows (columns should not exist)
