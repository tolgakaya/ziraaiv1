-- ============================================================
-- CORRECTED SQL - Fixed Column Names and JOINs
-- ============================================================
-- Issue found: Previous SQL had wrong column names
-- - ConfigurationJson (not Configuration)
-- - SubscriptionTierId (not TierId) in JOIN
-- ============================================================

-- ============================================================
-- PART 1: VERIFICATION QUERIES (Run these first)
-- ============================================================

-- Query 1: Check if data_access_percentage feature exists
SELECT
    f."Id" as "FeatureId",
    f."FeatureKey",
    f."DisplayName",
    f."RequiresConfiguration"
FROM public."Features" f
WHERE f."FeatureKey" = 'data_access_percentage';

-- Expected: 1 row with FeatureId = 7
-- Result you got: ✅ 7, data_access_percentage, Data Access Percentage, true


-- Query 2: Check TierFeatures mapping for data_access_percentage
-- CORRECTED VERSION with right column names and JOIN
SELECT
    tf."Id",
    st."TierName",
    st."Id" as "TierId",
    f."FeatureKey",
    tf."ConfigurationJson",  -- CORRECTED: was "Configuration"
    tf."IsEnabled"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"  -- CORRECTED: was tf."TierId"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'data_access_percentage'
ORDER BY st."Id";

-- Expected: 4 rows (S=30%, M=60%, L=100%, XL=100%)
-- Your result: EMPTY ❌ - This confirms the root cause!


-- Query 3: Check ALL TierFeatures (to see if table has ANY data)
SELECT
    tf."Id",
    st."TierName",
    f."FeatureKey",
    tf."ConfigurationJson",
    tf."IsEnabled"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
ORDER BY st."Id", f."FeatureKey";

-- This will show if ANY features are mapped to tiers
-- If empty: NO tier features configured AT ALL


-- Query 4: Check what Features exist (for reference)
SELECT
    "Id",
    "FeatureKey",
    "DisplayName",
    "RequiresConfiguration"
FROM public."Features"
ORDER BY "FeatureKey";

-- This shows which features are defined in the system


-- ============================================================
-- PART 2: FIX - INSERT MISSING TierFeatures
-- ============================================================
-- ONLY RUN THIS AFTER CONFIRMING Query 2 returned EMPTY!
-- ============================================================

-- Get current user ID for audit (replace 1 with actual admin user ID if known)
-- For now using 1 as system user
DO $$
DECLARE
    v_feature_id INT;
    v_admin_user_id INT := 1; -- System/Admin user for audit
BEGIN
    -- Get the feature ID
    SELECT "Id" INTO v_feature_id
    FROM public."Features"
    WHERE "FeatureKey" = 'data_access_percentage';

    IF v_feature_id IS NULL THEN
        RAISE EXCEPTION 'Feature data_access_percentage not found!';
    END IF;

    -- Insert S Tier (ID=2): 30% access
    INSERT INTO public."TierFeatures" (
        "SubscriptionTierId",
        "FeatureId",
        "IsEnabled",
        "ConfigurationJson",
        "CreatedDate",
        "CreatedByUserId"
    )
    SELECT
        2,  -- S Tier
        v_feature_id,
        true,
        '{"percentage": 30}',
        NOW(),
        v_admin_user_id
    WHERE NOT EXISTS (
        SELECT 1 FROM public."TierFeatures"
        WHERE "SubscriptionTierId" = 2 AND "FeatureId" = v_feature_id
    );

    -- Insert M Tier (ID=3): 60% access
    INSERT INTO public."TierFeatures" (
        "SubscriptionTierId",
        "FeatureId",
        "IsEnabled",
        "ConfigurationJson",
        "CreatedDate",
        "CreatedByUserId"
    )
    SELECT
        3,  -- M Tier
        v_feature_id,
        true,
        '{"percentage": 60}',
        NOW(),
        v_admin_user_id
    WHERE NOT EXISTS (
        SELECT 1 FROM public."TierFeatures"
        WHERE "SubscriptionTierId" = 3 AND "FeatureId" = v_feature_id
    );

    -- Insert L Tier (ID=4): 100% access
    INSERT INTO public."TierFeatures" (
        "SubscriptionTierId",
        "FeatureId",
        "IsEnabled",
        "ConfigurationJson",
        "CreatedDate",
        "CreatedByUserId"
    )
    SELECT
        4,  -- L Tier
        v_feature_id,
        true,
        '{"percentage": 100}',
        NOW(),
        v_admin_user_id
    WHERE NOT EXISTS (
        SELECT 1 FROM public."TierFeatures"
        WHERE "SubscriptionTierId" = 4 AND "FeatureId" = v_feature_id
    );

    -- Insert XL Tier (ID=5): 100% access
    INSERT INTO public."TierFeatures" (
        "SubscriptionTierId",
        "FeatureId",
        "IsEnabled",
        "ConfigurationJson",
        "CreatedDate",
        "CreatedByUserId"
    )
    SELECT
        5,  -- XL Tier
        v_feature_id,
        true,
        '{"percentage": 100}',
        NOW(),
        v_admin_user_id
    WHERE NOT EXISTS (
        SELECT 1 FROM public."TierFeatures"
        WHERE "SubscriptionTierId" = 5 AND "FeatureId" = v_feature_id
    );

    RAISE NOTICE 'TierFeatures inserted successfully for data_access_percentage';
END $$;


-- ============================================================
-- PART 3: VERIFICATION AFTER FIX
-- ============================================================

-- Verify inserts - should now return 4 rows
SELECT
    tf."Id",
    st."TierName",
    st."Id" as "TierId",
    f."FeatureKey",
    tf."ConfigurationJson",
    tf."IsEnabled",
    tf."CreatedDate"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'data_access_percentage'
ORDER BY st."Id";

-- Expected result after fix:
-- TierName | ConfigurationJson
-- S        | {"percentage": 30}
-- M        | {"percentage": 60}
-- L        | {"percentage": 100}
-- XL       | {"percentage": 100}


-- ============================================================
-- EXECUTION SUMMARY
-- ============================================================
-- 1. Run PART 1 (Verification) - You already confirmed Query 2 is EMPTY ✅
-- 2. Run PART 2 (Fix) - This will insert 4 rows
-- 3. Run PART 3 (Verification) - Should show 4 rows
-- 4. Restart WebAPI service on Railway
-- 5. Test endpoint: GET /api/v1/sponsorship/analyses
-- ============================================================
