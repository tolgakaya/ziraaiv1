-- ============================================================
-- CRITICAL FINDING: S Tier is MISSING!
-- ============================================================
-- Analysis of your results shows a critical issue
-- ============================================================

-- YOUR RESULTS SHOW:
-- M tier:     data_access_percentage = 30%  ✅
-- L tier:     data_access_percentage = 60%  ✅
-- XL tier:    data_access_percentage = 100% ✅
-- Trial tier: data_access_percentage = 100% ✅
-- S tier:     MISSING! ❌

-- EXPECTED CONFIGURATION:
-- S tier (ID=2):  30%   ← MISSING!
-- M tier (ID=3):  60%   ✅ (but shows 30% which is wrong!)
-- L tier (ID=4):  100%  ✅ (but shows 60% which is wrong!)
-- XL tier (ID=5): 100%  ✅

-- ============================================================
-- ISSUES IDENTIFIED:
-- ============================================================
-- 1. S Tier (ID=2) completely missing
-- 2. M Tier has WRONG percentage (30% instead of 60%)
-- 3. L Tier has WRONG percentage (60% instead of 100%)
-- 4. Trial tier shouldn't have 100% access (should be minimal)

-- ============================================================
-- VERIFICATION QUERY: Check exact tier IDs and percentages
-- ============================================================

SELECT
    st."Id" as "TierId",
    st."TierName",
    tf."Id" as "TierFeatureId",
    tf."ConfigurationJson",
    tf."ConfigurationJson"::jsonb->>'percentage' as "Percentage"
FROM public."SubscriptionTiers" st
LEFT JOIN public."TierFeatures" tf ON st."Id" = tf."SubscriptionTierId"
LEFT JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'data_access_percentage' OR f."FeatureKey" IS NULL
ORDER BY st."Id";

-- This will show EACH tier and whether it has data_access_percentage
1	S	NULL	NULL	 NULL
2	M	14	{"percentage": 30}	30
3	L	17	{"percentage": 60}	60
4	XL	22	{"percentage": 100}	100
5	Trial	30	{"percentage": 100}	100

-- ============================================================
-- EXPECTED RESULT:
-- ============================================================
-- TierId | TierName | TierFeatureId | ConfigurationJson      | Percentage
-- -------+----------+---------------+------------------------+-----------
-- 1      | Trial    | NULL or X     | NULL or {"percentage": 0}  | 0
-- 2      | S        | NULL          | NULL (MISSING!)        | NULL
-- 3      | M        | 14            | {"percentage": 60}     | 60
-- 4      | L        | 17            | {"percentage": 100}    | 100
-- 5      | XL       | 22            | {"percentage": 100}    | 100

-- ============================================================
-- ROOT CAUSE ANALYSIS:
-- ============================================================

-- Why did the first check return EMPTY?
-- → Because we were checking for User 159's tier
-- → User 159 has Purchase with tier L (ID=4)
-- → L tier HAS data_access_percentage (60%)
-- → So it should have worked!

-- Why is it returning 0 now?
-- → Need to check SponsorDataAccessService logic
-- → Maybe it's looking for tier L but finding wrong percentage?
-- → Or cache issue?

-- ============================================================
-- NEXT STEP: Check User 159's Purchase Tier
-- ============================================================

SELECT
    sp."Id" as "PurchaseId",
    sp."SponsorId",
    sp."SubscriptionTierId",
    st."TierName",
    sp."PaymentStatus",
    tf."ConfigurationJson" as "TierDataAccess"
FROM public."SponsorshipPurchases" sp
INNER JOIN public."SubscriptionTiers" st ON sp."SubscriptionTierId" = st."Id"
LEFT JOIN public."TierFeatures" tf ON st."Id" = tf."SubscriptionTierId"
LEFT JOIN public."Features" f ON tf."FeatureId" = f."Id" AND f."FeatureKey" = 'data_access_percentage'
WHERE sp."SponsorId" = 159
ORDER BY sp."Id" DESC;

-- This shows what tier User 159's purchase has and its data access config
27	159	2	M	Completed	{"percentage": 30}
26	159	3	L	Completed	{"percentage": 60}
26	159	3	L	Completed	{"showLogo": true, "showProfile": false}
26	159	3	L	Completed	
25	159	2	M	Completed	{"percentage": 30}
24	159	2	M	Completed	{"percentage": 30}
23	159	2	M	Completed	{"percentage": 30}
22	159	2	M	Completed	{"percentage": 30}
21	159	2	M	Completed	{"percentage": 30}
20	159	3	L	Completed	{"percentage": 60}
20	159	3	L	Completed	{"showLogo": true, "showProfile": false}
20	159	3	L	Completed	
19	159	2	M	Completed	{"percentage": 30}

-- ============================================================
-- FIX REQUIRED:
-- ============================================================

-- 1. Add MISSING S tier mapping
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
    f."Id",
    true,
    '{"percentage": 30}',
    NOW(),
    1
FROM public."Features" f
WHERE f."FeatureKey" = 'data_access_percentage'
  AND NOT EXISTS (
    SELECT 1 FROM public."TierFeatures" tf
    WHERE tf."SubscriptionTierId" = 2 AND tf."FeatureId" = f."Id"
  );

-- 2. Fix WRONG percentages for M and L tiers
UPDATE public."TierFeatures" tf
SET "ConfigurationJson" = '{"percentage": 60}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
FROM public."Features" f
WHERE tf."FeatureId" = f."Id"
  AND f."FeatureKey" = 'data_access_percentage'
  AND tf."SubscriptionTierId" = 3;  -- M tier

UPDATE public."TierFeatures" tf
SET "ConfigurationJson" = '{"percentage": 100}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
FROM public."Features" f
WHERE tf."FeatureId" = f."Id"
  AND f."FeatureKey" = 'data_access_percentage'
  AND tf."SubscriptionTierId" = 4;  -- L tier

-- 3. Fix Trial tier (should have minimal or 0 access)
UPDATE public."TierFeatures" tf
SET "ConfigurationJson" = '{"percentage": 0}',
    "ModifiedDate" = NOW(),
    "ModifiedByUserId" = 1
FROM public."Features" f
WHERE tf."FeatureId" = f."Id"
  AND f."FeatureKey" = 'data_access_percentage'
  AND tf."SubscriptionTierId" = 1;  -- Trial tier

-- ============================================================
-- VERIFICATION AFTER FIX:
-- ============================================================

SELECT
    st."TierName",
    tf."ConfigurationJson",
    tf."ConfigurationJson"::jsonb->>'percentage' as "Percentage"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'data_access_percentage'
ORDER BY st."Id";

-- Expected after fix:
-- Trial | {"percentage": 0}   | 0
-- S     | {"percentage": 30}  | 30
-- M     | {"percentage": 60}  | 60
-- L     | {"percentage": 100} | 100
-- XL    | {"percentage": 100} | 100

-- ============================================================
-- PLEASE RUN THE VERIFICATION QUERY AND USER 159 CHECK
-- ============================================================

M	{"percentage": 30}	30
L	{"percentage": 60}	60
XL	{"percentage": 100}	100
Trial	{"percentage": 100}	100