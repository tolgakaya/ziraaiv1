-- ============================================================
-- CRITICAL: Check if TierFeatures table is COMPLETELY EMPTY
-- ============================================================
-- This will tell us if we need to run the FULL migration
-- or just the data_access_percentage fix
-- ============================================================

-- Query 1: Count total rows in TierFeatures
SELECT COUNT(*) as "TotalTierFeatures"
FROM public."TierFeatures";

SONUÇ: 18
-- If result = 0: Table is COMPLETELY EMPTY → Need FULL migration
-- If result > 0: Some features exist → Only need data_access_percentage fix


-- Query 2: Show ALL mapped features (if any exist)
SELECT
    tf."Id",
    st."TierName" as "Tier",
    f."FeatureKey" as "Feature",
    tf."ConfigurationJson" as "Config",
    tf."IsEnabled" as "Enabled"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
ORDER BY st."Id", f."FeatureKey";

-- This shows what features are currently configured
14	M	data_access_percentage	{"percentage": 30}	true
15	L	advanced_analytics		true
17	L	data_access_percentage	{"percentage": 60}	true
16	L	sponsor_visibility	{"showLogo": true, "showProfile": false}	true
19	XL	advanced_analytics		true
20	XL	api_access		true
22	XL	data_access_percentage	{"percentage": 100}	true
18	XL	messaging		true
23	XL	priority_support	{"responseTimeHours": 12}	true
21	XL	sponsor_visibility	{"showLogo": true, "showProfile": true}	true
27	Trial	advanced_analytics		true
28	Trial	api_access		true
30	Trial	data_access_percentage	{"percentage": 100}	true
24	Trial	messaging		true
31	Trial	priority_support	{"responseTimeHours": 6}	true
26	Trial	smart_links		true
29	Trial	sponsor_visibility	{"showLogo": true, "showProfile": true}	true
25	Trial	voice_messages		true

-- Query 3: Show which features exist but are NOT mapped
SELECT
    f."FeatureKey",
    f."DisplayName",
    COUNT(tf."Id") as "MappingCount"
FROM public."Features" f
LEFT JOIN public."TierFeatures" tf ON f."Id" = tf."FeatureId"
GROUP BY f."FeatureKey", f."DisplayName"
HAVING COUNT(tf."Id") = 0
ORDER BY f."FeatureKey";

-- This shows unmapped features
SONUÇ BOŞ GELDİ

-- ============================================================
-- DECISION TREE
-- ============================================================

-- SCENARIO A: Query 1 returns 0 (Table completely empty)
--   → Need to run FULL migration
--   → File: claudedocs/TIER_FEATURE_MIGRATION_GUIDE.md
--   → Or: DataAccess/Migrations/Pg/Manual/AddTierFeatureManagementSystem.sql
--   → This will create ALL feature mappings (messaging, smart_links, etc.)

-- SCENARIO B: Query 1 returns > 0 (Some features exist)
--   → Only data_access_percentage is missing
--   → Run: CORRECT_CHECK_AND_FIX.sql (PART 2)
--   → This adds only the missing feature

-- ============================================================

-- Query 4: Show expected vs actual feature counts
SELECT
    'Expected' as "Source",
    'messaging' as "Feature",
    2 as "ExpectedMappings"
UNION ALL
SELECT 'Expected', 'smart_links', 1
UNION ALL
SELECT 'Expected', 'sponsor_visibility', 3
UNION ALL
SELECT 'Expected', 'data_access_percentage', 4
UNION ALL
SELECT 'Expected', 'voice_messages', 1
UNION ALL
SELECT 'Expected', 'advanced_analytics', 3
UNION ALL
SELECT 'Expected', 'api_access', 2
UNION ALL
SELECT 'Expected', 'priority_support', 2
UNION ALL
SELECT
    'Actual' as "Source",
    f."FeatureKey" as "Feature",
    COUNT(tf."Id") as "ActualMappings"
FROM public."Features" f
LEFT JOIN public."TierFeatures" tf ON f."Id" = tf."FeatureId"
WHERE f."FeatureKey" IN (
    'messaging',
    'smart_links',
    'sponsor_visibility',
    'data_access_percentage',
    'voice_messages',
    'advanced_analytics',
    'api_access',
    'priority_support'
)
GROUP BY f."FeatureKey"
ORDER BY "Feature", "Source";

-- This shows side-by-side comparison
Actual	advanced_analytics	3
Expected	advanced_analytics	3
Actual	api_access	2
Expected	api_access	2
Actual	data_access_percentage	4
Expected	data_access_percentage	4
Actual	messaging	2
Expected	messaging	2
Actual	priority_support	2
Expected	priority_support	2
Actual	smart_links	1
Expected	smart_links	1
Actual	sponsor_visibility	3
Expected	sponsor_visibility	3
Actual	voice_messages	1
Expected	voice_messages	1
-- ============================================================
-- PLEASE RUN ALL 4 QUERIES AND REPORT RESULTS
-- ============================================================
