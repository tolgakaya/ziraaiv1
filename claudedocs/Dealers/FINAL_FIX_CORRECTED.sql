-- ============================================================
-- FINAL FIX - Corrected UPDATE and INSERT Statements
-- ============================================================
-- Previous UPDATE didn't work - using simpler, direct approach
-- ============================================================

-- ============================================================
-- PART 1: Show current state BEFORE fix
-- ============================================================
SELECT
    st."Id" as "TierId",
    st."TierName",
    tf."Id" as "TierFeatureId",
    tf."ConfigurationJson",
    tf."ConfigurationJson"::jsonb->>'percentage' as "Percentage"
FROM public."SubscriptionTiers" st
LEFT JOIN public."TierFeatures" tf ON st."Id" = tf."SubscriptionTierId"
LEFT JOIN public."Features" f ON tf."FeatureId" = f."Id" AND f."FeatureKey" = 'data_access_percentage'
WHERE st."Id" IN (1, 2, 3, 4, 5)
ORDER BY st."Id";

-- Current state (your result):
-- 1 | S     | NULL | NULL               | NULL
-- 2 | M     | 14   | {"percentage": 30} | 30
-- 3 | L     | 17   | {"percentage": 60} | 60
-- 4 | XL    | 22   | {"percentage": 100}| 100
-- 5 | Trial | 30   | {"percentage": 100}| 100


-- ============================================================
-- PART 2: FIX - Direct UPDATE by TierFeature ID
-- ============================================================

-- Fix M tier (TierFeature ID = 14): 30% → 60%
UPDATE public."TierFeatures"
SET "ConfigurationJson" = '{"percentage": 60}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
WHERE "Id" = 14;

-- Fix L tier (TierFeature ID = 17): 60% → 100%
UPDATE public."TierFeatures"
SET "ConfigurationJson" = '{"percentage": 100}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
WHERE "Id" = 17;

-- Fix Trial tier (TierFeature ID = 30): 100% → 0%
UPDATE public."TierFeatures"
SET "ConfigurationJson" = '{"percentage": 0}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
WHERE "Id" = 30;


-- ============================================================
-- PART 3: ADD S tier mapping
-- ============================================================

-- Get feature ID first
DO $$
DECLARE
    v_feature_id INT;
BEGIN
    SELECT "Id" INTO v_feature_id
    FROM public."Features"
    WHERE "FeatureKey" = 'data_access_percentage';

    -- Insert S tier (SubscriptionTierId = 1)
    INSERT INTO public."TierFeatures" (
        "SubscriptionTierId",
        "FeatureId",
        "IsEnabled",
        "ConfigurationJson",
        "CreatedDate",
        "CreatedByUserId"
    )
    VALUES (
        1,  -- S Tier
        v_feature_id,
        true,
        '{"percentage": 30}',
        NOW(),
        1
    )
    ON CONFLICT DO NOTHING;  -- In case it exists

    RAISE NOTICE 'S tier mapping added successfully';
END $$;


-- ============================================================
-- PART 4: VERIFY - Should show corrected values
-- ============================================================
SELECT
    st."Id" as "TierId",
    st."TierName",
    tf."Id" as "TierFeatureId",
    tf."ConfigurationJson",
    tf."ConfigurationJson"::jsonb->>'percentage' as "Percentage",
    tf."ModifiedDate"
FROM public."SubscriptionTiers" st
LEFT JOIN public."TierFeatures" tf ON st."Id" = tf."SubscriptionTierId"
LEFT JOIN public."Features" f ON tf."FeatureId" = f."Id" AND f."FeatureKey" = 'data_access_percentage'
WHERE st."Id" IN (1, 2, 3, 4, 5)
ORDER BY st."Id";

-- Expected result AFTER fix:
-- 1 | S     | NEW  | {"percentage": 30}  | 30  | NOW()
-- 2 | M     | 14   | {"percentage": 60}  | 60  | NOW()
-- 3 | L     | 17   | {"percentage": 100} | 100 | NOW()
-- 4 | XL    | 22   | {"percentage": 100} | 100 | (unchanged)
-- 5 | Trial | 30   | {"percentage": 0}   | 0   | NOW()


-- ============================================================
-- PART 5: Verify User 159's MAX access percentage will be 100
-- ============================================================
SELECT
    sp."Id" as "PurchaseId",
    st."TierName",
    tf."ConfigurationJson"::jsonb->>'percentage' as "AccessPercentage"
FROM public."SponsorshipPurchases" sp
INNER JOIN public."SubscriptionTiers" st ON sp."SubscriptionTierId" = st."Id"
LEFT JOIN public."TierFeatures" tf ON st."Id" = tf."SubscriptionTierId"
LEFT JOIN public."Features" f ON tf."FeatureId" = f."Id" AND f."FeatureKey" = 'data_access_percentage'
WHERE sp."SponsorId" = 159
ORDER BY (tf."ConfigurationJson"::jsonb->>'percentage')::int DESC;

-- Expected to show Purchase #26 with L tier = 100% at top


-- ============================================================
-- EXECUTION INSTRUCTIONS
-- ============================================================
-- 1. Run PART 1 to see current state (you already did this)
-- 2. Run PART 2 - Three UPDATE statements (update by TierFeature ID)
-- 3. Run PART 3 - DO block to add S tier
-- 4. Run PART 4 - Verify all tiers have correct percentages
-- 5. Run PART 5 - Verify User 159 will get 100%
-- 6. Restart WebAPI service on Railway
-- 7. Test endpoint
-- ============================================================
