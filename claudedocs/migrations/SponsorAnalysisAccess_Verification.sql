-- =============================================
-- Verification: Check SponsorAnalysisAccess Table
-- Purpose: Verify migration was applied successfully
-- Date: 2025-10-18
-- =============================================

-- 1. Check if table exists
SELECT
    CASE
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = 'public'
            AND table_name = 'SponsorAnalysisAccess'
        ) THEN '✅ Table exists'
        ELSE '❌ Table NOT found'
    END AS table_status;

-- 2. Check table structure
SELECT
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'SponsorAnalysisAccess'
ORDER BY ordinal_position;

-- 3. Check indexes
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'SponsorAnalysisAccess'
ORDER BY indexname;

-- 4. Check foreign key constraints
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_name = 'SponsorAnalysisAccess';

-- 5. Check record count (should be 0 for new table)
SELECT COUNT(*) as record_count
FROM "SponsorAnalysisAccess";

-- 6. Test insert and delete (to verify constraints work)
DO $$
DECLARE
    test_sponsor_id INTEGER;
    test_farmer_id INTEGER;
    test_analysis_id INTEGER;
BEGIN
    -- Get a real sponsor user ID
    SELECT "UserId" INTO test_sponsor_id
    FROM "Users" u
    INNER JOIN "UserGroups" ug ON u."UserId" = ug."UserId"
    INNER JOIN "Groups" g ON ug."GroupId" = g."Id"
    WHERE g."GroupName" = 'Sponsor'
    LIMIT 1;

    -- Get a real farmer user ID
    SELECT "UserId" INTO test_farmer_id
    FROM "Users" u
    INNER JOIN "UserGroups" ug ON u."UserId" = ug."UserId"
    INNER JOIN "Groups" g ON ug."GroupId" = g."Id"
    WHERE g."GroupName" = 'Farmer'
    LIMIT 1;

    -- Get a real analysis ID
    SELECT "Id" INTO test_analysis_id
    FROM "PlantAnalyses"
    LIMIT 1;

    IF test_sponsor_id IS NOT NULL AND test_farmer_id IS NOT NULL AND test_analysis_id IS NOT NULL THEN
        -- Try to insert a test record
        INSERT INTO "SponsorAnalysisAccess" (
            "SponsorId",
            "PlantAnalysisId",
            "FarmerId",
            "AccessLevel",
            "AccessPercentage",
            "FirstViewedDate",
            "ViewCount",
            "HasDownloaded",
            "CanViewHealthScore",
            "CanViewImages"
        ) VALUES (
            test_sponsor_id,
            test_analysis_id,
            test_farmer_id,
            'Extended60',
            60,
            NOW(),
            1,
            FALSE,
            TRUE,
            TRUE
        );

        RAISE NOTICE '✅ Test insert successful';

        -- Clean up test record
        DELETE FROM "SponsorAnalysisAccess"
        WHERE "SponsorId" = test_sponsor_id
          AND "PlantAnalysisId" = test_analysis_id;

        RAISE NOTICE '✅ Test delete successful';
    ELSE
        RAISE NOTICE '⚠️ Could not find test data (sponsor, farmer, or analysis)';
    END IF;
END $$;

-- Verification complete
SELECT '✅ Migration verification complete' AS status;
