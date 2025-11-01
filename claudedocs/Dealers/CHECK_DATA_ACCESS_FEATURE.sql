-- ============================================================
-- CRITICAL BUG INVESTIGATION: accessPercentage returning 0
-- ============================================================
-- Endpoint: GET /api/v1/sponsorship/analyses
-- Issue: All analyses showing tierName="Unknown", accessPercentage=0
-- Suspected cause: Missing data_access_percentage feature in TierFeatures table
-- ============================================================

-- Query 1: Check if data_access_percentage feature exists
SELECT
    f."Id" as "FeatureId",
    f."FeatureKey",
    f."DisplayName",
    f."RequiresConfiguration"
FROM public."Features" f
WHERE f."FeatureKey" = 'data_access_percentage';

-- Expected: 1 row with FeatureId (probably 7)


-- Query 2: Check TierFeatures mapping for data_access_percentage
SELECT
    tf."Id",
    st."TierName",
    st."Id" as "TierId",
    f."FeatureKey",
    tf."Configuration"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."TierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'data_access_percentage'
ORDER BY st."Id";

-- Expected: 4 rows (S=30%, M=60%, L=100%, XL=100%)
-- If EMPTY: This is the root cause!


-- Query 3: Check sponsor's purchases (User 159)
SELECT
    sp."Id" as "PurchaseId",
    sp."SponsorId",
    st."TierName",
    st."Id" as "TierId",
    sp."PackageSize",
    sp."PaymentStatus"
FROM public."SponsorshipPurchases" sp
INNER JOIN public."SubscriptionTiers" st ON sp."SubscriptionTierId" = st."Id"
WHERE sp."SponsorId" = 159
ORDER BY sp."Id" DESC;

-- Expected: Should show Purchase ID 26 with tier L (ID=4)


-- Query 4: Check ALL features mapped to tier L (ID=4)
SELECT
    f."FeatureKey",
    f."DisplayName",
    tf."Configuration"
FROM public."TierFeatures" tf
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE tf."TierId" = 4  -- L tier
ORDER BY f."FeatureKey";

-- Expected: Should include data_access_percentage with {"percentage": 100}


-- Query 5: Check if messaging feature exists (for comparison)
SELECT
    tf."Id",
    st."TierName",
    f."FeatureKey",
    tf."Configuration"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."TierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'messaging'
ORDER BY st."Id";

-- Expected: L and XL tiers should have messaging feature


-- ============================================================
-- RESULTS INTERPRETATION:
-- ============================================================
-- If Query 2 returns EMPTY:
--   ROOT CAUSE: data_access_percentage feature NOT mapped in TierFeatures
--   FIX: Run INSERT statements below to add mappings
--
-- If Query 2 returns rows:
--   Check Configuration column - should be valid JSON with "percentage" field
--   If Configuration is wrong: Update with correct JSON
-- ============================================================
