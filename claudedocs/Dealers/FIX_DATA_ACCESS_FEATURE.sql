-- ============================================================
-- FIX: Add data_access_percentage feature to TierFeatures
-- ============================================================
-- ONLY RUN THIS IF Query 2 from CHECK_DATA_ACCESS_FEATURE.sql returned EMPTY!
-- ============================================================

-- Step 1: Verify feature exists (should already exist from tier refactoring)
-- If this returns empty, run the INSERT for Features first
SELECT "Id", "FeatureKey" FROM public."Features" WHERE "FeatureKey" = 'data_access_percentage';


-- Step 2: Insert TierFeatures mappings for data_access_percentage
-- NOTE: Adjust FeatureId based on Step 1 result (replace 7 with actual ID)

-- S Tier (ID=2): 30% access
INSERT INTO public."TierFeatures" ("TierId", "FeatureId", "Configuration", "IsEnabled", "CreatedDate")
SELECT
    2,  -- S Tier
    f."Id",
    '{"percentage": 30}',
    true,
    NOW()
FROM public."Features" f
WHERE f."FeatureKey" = 'data_access_percentage'
  AND NOT EXISTS (
    SELECT 1 FROM public."TierFeatures" tf
    WHERE tf."TierId" = 2 AND tf."FeatureId" = f."Id"
  );

-- M Tier (ID=3): 60% access
INSERT INTO public."TierFeatures" ("TierId", "FeatureId", "Configuration", "IsEnabled", "CreatedDate")
SELECT
    3,  -- M Tier
    f."Id",
    '{"percentage": 60}',
    true,
    NOW()
FROM public."Features" f
WHERE f."FeatureKey" = 'data_access_percentage'
  AND NOT EXISTS (
    SELECT 1 FROM public."TierFeatures" tf
    WHERE tf."TierId" = 3 AND tf."FeatureId" = f."Id"
  );

-- L Tier (ID=4): 100% access
INSERT INTO public."TierFeatures" ("TierId", "FeatureId", "Configuration", "IsEnabled", "CreatedDate")
SELECT
    4,  -- L Tier
    f."Id",
    '{"percentage": 100}',
    true,
    NOW()
FROM public."Features" f
WHERE f."FeatureKey" = 'data_access_percentage'
  AND NOT EXISTS (
    SELECT 1 FROM public."TierFeatures" tf
    WHERE tf."TierId" = 4 AND tf."FeatureId" = f."Id"
  );

-- XL Tier (ID=5): 100% access
INSERT INTO public."TierFeatures" ("TierId", "FeatureId", "Configuration", "IsEnabled", "CreatedDate")
SELECT
    5,  -- XL Tier
    f."Id",
    '{"percentage": 100}',
    true,
    NOW()
FROM public."Features" f
WHERE f."FeatureKey" = 'data_access_percentage'
  AND NOT EXISTS (
    SELECT 1 FROM public."TierFeatures" tf
    WHERE tf."TierId" = 5 AND tf."FeatureId" = f."Id"
  );


-- Step 3: Verify inserts
SELECT
    tf."Id",
    st."TierName",
    f."FeatureKey",
    tf."Configuration",
    tf."IsEnabled"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."TierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'data_access_percentage'
ORDER BY st."Id";

-- Expected result:
-- TierName | Configuration
-- S        | {"percentage": 30}
-- M        | {"percentage": 60}
-- L        | {"percentage": 100}
-- XL       | {"percentage": 100}


-- ============================================================
-- ALTERNATIVE: If Features table doesn't have data_access_percentage
-- ============================================================
-- Run this FIRST if Step 1 returned empty:

INSERT INTO public."Features" ("FeatureKey", "DisplayName", "Description", "RequiresConfiguration", "CreatedDate")
VALUES (
    'data_access_percentage',
    'Data Access Percentage',
    'Percentage of analysis data visible to sponsor based on tier',
    true,
    NOW()
)
ON CONFLICT DO NOTHING;

-- Then run Step 2 above


-- ============================================================
-- RESTART SERVICES AFTER FIX
-- ============================================================
-- The TierFeatureService has 15-minute cache
-- Options:
-- 1. Wait 15 minutes for cache to expire
-- 2. Restart WebAPI service on Railway to clear cache immediately
-- 3. If using Redis cache, clear Redis keys: tier_feature_*
-- ============================================================
