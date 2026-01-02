-- =====================================================
-- Farmer Invitation System - ROLLBACK Script
-- =====================================================
-- Purpose: Rollback farmer invitation functionality if needed
-- Date: 2026-01-02
-- Related: feature/remove-sms-listener-logic
-- WARNING: This will delete all farmer invitation data!
-- =====================================================

-- =====================================================
-- STEP 1: DROP FOREIGN KEY CONSTRAINTS
-- =====================================================

-- Drop FK from SponsorshipCodes to FarmerInvitation
ALTER TABLE "SponsorshipCodes"
    DROP CONSTRAINT IF EXISTS "FK_SponsorshipCode_FarmerInvitation";

-- =====================================================
-- STEP 2: DROP INDEXES FROM SPONSORSHIPCODES
-- =====================================================

DROP INDEX IF EXISTS "IX_SponsorshipCode_FarmerInvitationId";
DROP INDEX IF EXISTS "IX_SponsorshipCode_ReservedForFarmerInvitationId";

-- =====================================================
-- STEP 3: REMOVE COLUMNS FROM SPONSORSHIPCODES
-- =====================================================

ALTER TABLE "SponsorshipCodes"
    DROP COLUMN IF EXISTS "FarmerInvitationId",
    DROP COLUMN IF EXISTS "ReservedForFarmerInvitationId",
    DROP COLUMN IF EXISTS "ReservedForFarmerAt";

-- =====================================================
-- STEP 4: DROP FARMER INVITATION TABLE
-- =====================================================

-- This will cascade and remove all associated data
DROP TABLE IF EXISTS "FarmerInvitation" CASCADE;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Verify FarmerInvitation table removed
SELECT
    table_name,
    table_type
FROM information_schema.tables
WHERE table_name = 'FarmerInvitation';
-- Should return 0 rows

-- Verify columns removed from SponsorshipCodes
SELECT
    column_name,
    data_type
FROM information_schema.columns
WHERE table_name = 'SponsorshipCodes'
    AND column_name IN ('FarmerInvitationId', 'ReservedForFarmerInvitationId', 'ReservedForFarmerAt');
-- Should return 0 rows

-- Verify indexes removed
SELECT
    indexname,
    tablename
FROM pg_indexes
WHERE indexname LIKE '%FarmerInvitation%';
-- Should return 0 rows

-- =====================================================
-- SUCCESS MESSAGE
-- =====================================================

DO $$
BEGIN
    RAISE NOTICE '✅ Farmer Invitation System rollback completed successfully!';
    RAISE NOTICE 'Table dropped: FarmerInvitation';
    RAISE NOTICE 'Columns removed from SponsorshipCodes: 3 columns';
    RAISE NOTICE 'Indexes removed: 7 indexes';
    RAISE NOTICE 'Foreign keys removed: All constraints';
END $$;
