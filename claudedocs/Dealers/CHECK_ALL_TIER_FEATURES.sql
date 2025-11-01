-- ============================================================
-- CHECK ALL TIER FEATURES - Comprehensive Verification
-- ============================================================
-- This will show which features are missing from TierFeatures table
-- Run this to see the full picture of what needs to be fixed
-- ============================================================

-- Query 1: List ALL features and their mapping count
SELECT
    f."Id" as "FeatureId",
    f."FeatureKey",
    f."DisplayName",
    f."RequiresConfiguration",
    COUNT(tf."Id") as "TierMappingCount",
    STRING_AGG(st."TierName", ', ' ORDER BY st."Id") as "MappedToTiers"
FROM public."Features" f
LEFT JOIN public."TierFeatures" tf ON f."Id" = tf."FeatureId" AND tf."IsEnabled" = true
LEFT JOIN public."SubscriptionTiers" st ON tf."TierId" = st."Id"
GROUP BY f."Id", f."FeatureKey", f."DisplayName", f."RequiresConfiguration"
ORDER BY f."FeatureKey";

-- Expected Result (from TIER_FEATURE_MIGRATION_GUIDE.md):
-- FeatureKey              | TierMappingCount | MappedToTiers
-- ------------------------+------------------+------------------
-- advanced_analytics      | 3                | M, L, XL
-- api_access              | 2                | L, XL
-- data_access_percentage  | 4                | S, M, L, XL  ⚠️ CURRENTLY MISSING
-- messaging               | 2                | L, XL
-- priority_support        | 2                | L, XL
-- smart_links             | 1                | XL
-- sponsor_visibility      | 3                | M, L, XL
-- voice_messages          | 1                | XL


-- Query 2: Detailed view of EACH tier's features
SELECT
    st."Id" as "TierId",
    st."TierName",
    f."FeatureKey",
    tf."Configuration",
    tf."IsEnabled"
FROM public."SubscriptionTiers" st
LEFT JOIN public."TierFeatures" tf ON st."Id" = tf."TierId"
LEFT JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE st."TierName" IN ('S', 'M', 'L', 'XL')
ORDER BY st."Id", f."FeatureKey";

-- Expected counts per tier:
-- Trial (1): 0 features
-- S (2): 1 feature (data_access_percentage: 30%)
-- M (3): 3 features (advanced_analytics, sponsor_visibility, data_access_percentage: 60%)
-- L (4): 6 features (+ messaging, api_access, full visibility, data_access: 100%, priority_support: 12h)
-- XL (5): 8 features (all features including voice_messages, smart_links, priority_support: 6h)


-- Query 3: Find MISSING features (features with 0 mappings)
SELECT
    f."FeatureKey",
    f."DisplayName",
    f."Description"
FROM public."Features" f
LEFT JOIN public."TierFeatures" tf ON f."Id" = tf."FeatureId"
WHERE tf."Id" IS NULL
ORDER BY f."FeatureKey";

-- If this returns ANY rows: Those features are NOT mapped to ANY tier!
-- Expected: Should be EMPTY if migration was run correctly


-- Query 4: Check specific critical features
SELECT
    f."FeatureKey",
    st."TierName",
    tf."Configuration",
    tf."IsEnabled",
    tf."CreatedDate"
FROM public."Features" f
CROSS JOIN public."SubscriptionTiers" st
LEFT JOIN public."TierFeatures" tf ON f."Id" = tf."FeatureId" AND st."Id" = tf."TierId"
WHERE f."FeatureKey" IN (
    'data_access_percentage',  -- CRITICAL: Returns analysis field visibility
    'messaging',               -- CRITICAL: Enables sponsor-farmer chat
    'sponsor_visibility',      -- IMPORTANT: Shows sponsor logo/profile
    'smart_links'              -- XL tier premium feature
)
AND st."TierName" IN ('S', 'M', 'L', 'XL')
ORDER BY f."FeatureKey", st."Id";

-- Expected results:
-- data_access_percentage + S:  {"percentage": 30}
-- data_access_percentage + M:  {"percentage": 60}
-- data_access_percentage + L:  {"percentage": 100}
-- data_access_percentage + XL: {"percentage": 100}
-- messaging + L:   {} (no config needed)
-- messaging + XL:  {} (no config needed)
-- sponsor_visibility + M:  {"showLogo": true, "showProfile": false}
-- sponsor_visibility + L:  {"showLogo": true, "showProfile": true}
-- sponsor_visibility + XL: {"showLogo": true, "showProfile": true}
-- smart_links + XL: {} (no config needed)


-- ============================================================
-- INTERPRETATION GUIDE
-- ============================================================

-- If Query 1 shows TierMappingCount = 0 for any feature:
--   → Feature exists but NOT mapped to any tier
--   → Services using this feature will return FALSE
--   → FIX: Run migration SQL to add mappings

-- If Query 3 returns ANY rows:
--   → Those features are completely unmapped
--   → CRITICAL if feature is used in code
--   → FIX: Run full migration from TIER_FEATURE_MIGRATION_GUIDE.md

-- If Query 4 shows NULL for Configuration where expected:
--   → Feature mapped but missing configuration
--   → FIX: Update TierFeatures.Configuration with correct JSON

-- ============================================================
-- NEXT STEPS BASED ON RESULTS
-- ============================================================

-- If data_access_percentage missing:
--   1. Run FIX_DATA_ACCESS_FEATURE.sql
--   2. Restart WebAPI service
--   3. Test /api/v1/sponsorship/analyses

-- If multiple features missing:
--   1. Run full migration: AddTierFeatureManagementSystem.sql
--   2. Restart WebAPI service
--   3. Test all affected endpoints

-- If Configuration is wrong:
--   1. Use UPDATE statements to fix JSON
--   2. Clear cache or restart service
--   3. Verify with HasFeatureAccessAsync tests

-- ============================================================
