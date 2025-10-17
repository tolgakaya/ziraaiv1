-- =====================================================
-- Verification Script for FarmerSponsorBlock Migration
-- Created: 2025-01-17
-- Purpose: Verify successful migration of FarmerSponsorBlocks table
-- Branch: feature/sponsor-farmer-messaging
-- Database: PostgreSQL
-- =====================================================

-- Check if table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'public'
    AND table_name = 'FarmerSponsorBlocks'
) AS table_exists;

-- Check table structure
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default,
    character_maximum_length
FROM information_schema.columns
WHERE table_name = 'FarmerSponsorBlocks'
ORDER BY ordinal_position;

-- Check indexes
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'FarmerSponsorBlocks'
ORDER BY indexname;

-- Check foreign keys
SELECT
    conname AS constraint_name,
    contype AS constraint_type,
    pg_get_constraintdef(oid) AS constraint_definition
FROM pg_constraint
WHERE conrelid = 'FarmerSponsorBlocks'::regclass
ORDER BY conname;

-- Check table comments
SELECT
    obj_description('FarmerSponsorBlocks'::regclass) AS table_comment;

-- Check column comments
SELECT
    column_name,
    col_description('FarmerSponsorBlocks'::regclass, ordinal_position) AS column_comment
FROM information_schema.columns
WHERE table_name = 'FarmerSponsorBlocks'
ORDER BY ordinal_position;

-- Test insert and delete (cleanup)
DO $$
DECLARE
    test_farmer_id INTEGER;
    test_sponsor_id INTEGER;
    test_id INTEGER;
BEGIN
    -- Get existing user IDs for testing
    SELECT "UserId" INTO test_farmer_id FROM "Users" LIMIT 1;
    SELECT "UserId" INTO test_sponsor_id FROM "Users" OFFSET 1 LIMIT 1;

    IF test_farmer_id IS NOT NULL AND test_sponsor_id IS NOT NULL THEN
        -- Test insert
        INSERT INTO "FarmerSponsorBlocks"
            ("FarmerId", "SponsorId", "IsBlocked", "IsMuted", "CreatedDate", "Reason")
        VALUES
            (test_farmer_id, test_sponsor_id, true, false, NOW(), 'Test migration')
        RETURNING "Id" INTO test_id;

        RAISE NOTICE 'Test insert successful: ID = %', test_id;

        -- Test delete
        DELETE FROM "FarmerSponsorBlocks" WHERE "Id" = test_id;

        RAISE NOTICE 'Test cleanup successful';
    ELSE
        RAISE NOTICE 'No users found for testing - skipping test insert';
    END IF;
END $$;
