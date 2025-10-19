-- =====================================================
-- Messaging Features Verification Script
-- Purpose: Verify table structure and seed data
-- Date: 2025-10-19
-- =====================================================

-- 1. Check if table exists
SELECT
    CASE
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = 'public'
            AND table_name = 'MessagingFeatures'
        )
        THEN '✅ MessagingFeatures table exists'
        ELSE '❌ MessagingFeatures table NOT found'
    END AS table_status;

-- 2. Check table structure
SELECT
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'MessagingFeatures'
ORDER BY ordinal_position;

-- 3. Check indexes
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'MessagingFeatures';

-- 4. Check foreign key constraints
SELECT
    tc.constraint_name,
    tc.constraint_type,
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
WHERE tc.table_name = 'MessagingFeatures';

-- 5. Check seed data (count)
SELECT
    COUNT(*) AS total_features,
    COUNT(CASE WHEN "IsEnabled" = true THEN 1 END) AS enabled_features,
    COUNT(CASE WHEN "IsEnabled" = false THEN 1 END) AS disabled_features
FROM "MessagingFeatures";

-- 6. List all features with their configuration
SELECT
    "Id",
    "FeatureName",
    "DisplayName",
    "IsEnabled",
    "RequiredTier",
    "MaxFileSize",
    "MaxDuration",
    "TimeLimit",
    CASE
        WHEN "IsEnabled" = true THEN '✅ Enabled'
        ELSE '❌ Disabled'
    END AS status
FROM "MessagingFeatures"
ORDER BY "Id";

-- 7. Verify tier-based features
SELECT
    "RequiredTier",
    COUNT(*) AS feature_count,
    STRING_AGG("FeatureName", ', ' ORDER BY "FeatureName") AS features
FROM "MessagingFeatures"
GROUP BY "RequiredTier"
ORDER BY
    CASE "RequiredTier"
        WHEN 'None' THEN 1
        WHEN 'S' THEN 2
        WHEN 'M' THEN 3
        WHEN 'L' THEN 4
        WHEN 'XL' THEN 5
        ELSE 6
    END;

-- 8. Check file size limits
SELECT
    "FeatureName",
    "MaxFileSize",
    CASE
        WHEN "MaxFileSize" IS NULL THEN 'N/A'
        ELSE ROUND("MaxFileSize" / 1024.0 / 1024.0, 2)::TEXT || ' MB'
    END AS max_file_size_mb
FROM "MessagingFeatures"
WHERE "MaxFileSize" IS NOT NULL
ORDER BY "MaxFileSize" DESC;

-- 9. Check duration limits
SELECT
    "FeatureName",
    "MaxDuration",
    CASE
        WHEN "MaxDuration" IS NULL THEN 'N/A'
        WHEN "MaxDuration" < 60 THEN "MaxDuration"::TEXT || ' seconds'
        ELSE ROUND("MaxDuration" / 60.0, 1)::TEXT || ' minutes'
    END AS max_duration_formatted
FROM "MessagingFeatures"
WHERE "MaxDuration" IS NOT NULL
ORDER BY "MaxDuration" DESC;

-- 10. Check time limits (edit/delete)
SELECT
    "FeatureName",
    "TimeLimit",
    CASE
        WHEN "TimeLimit" IS NULL THEN 'N/A'
        WHEN "TimeLimit" < 3600 THEN ROUND("TimeLimit" / 60.0)::TEXT || ' minutes'
        WHEN "TimeLimit" < 86400 THEN ROUND("TimeLimit" / 3600.0)::TEXT || ' hours'
        ELSE ROUND("TimeLimit" / 86400.0)::TEXT || ' days'
    END AS time_limit_formatted
FROM "MessagingFeatures"
WHERE "TimeLimit" IS NOT NULL
ORDER BY "TimeLimit" DESC;

-- 11. Summary Report
DO $$
DECLARE
    total_count INT;
    enabled_count INT;
    disabled_count INT;
BEGIN
    SELECT COUNT(*) INTO total_count FROM "MessagingFeatures";
    SELECT COUNT(*) INTO enabled_count FROM "MessagingFeatures" WHERE "IsEnabled" = true;
    SELECT COUNT(*) INTO disabled_count FROM "MessagingFeatures" WHERE "IsEnabled" = false;

    RAISE NOTICE '========================================';
    RAISE NOTICE 'Messaging Features Verification Summary';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Total Features: %', total_count;
    RAISE NOTICE 'Enabled: %', enabled_count;
    RAISE NOTICE 'Disabled: %', disabled_count;
    RAISE NOTICE '========================================';

    IF total_count = 9 THEN
        RAISE NOTICE '✅ All 9 features are present';
    ELSE
        RAISE WARNING '⚠️ Expected 9 features, found %', total_count;
    END IF;
END $$;
