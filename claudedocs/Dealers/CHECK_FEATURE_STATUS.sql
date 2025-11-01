-- ============================================================
-- CHECK FEATURE STATUS - Why accessPercentage returns 0
-- ============================================================

-- Check if data_access_percentage feature exists and is active
SELECT
    "Id",
    "FeatureKey",
    "FeatureName",
    "Description",
    "IsActive",
    "IsDeprecated",
    "DefaultConfigJson"
FROM public."Features"
WHERE "FeatureKey" = 'data_access_percentage';

-- Expected: IsActive = true, IsDeprecated = false
-- If IsActive = false OR IsDeprecated = true → Feature is ignored!

-- ============================================================
-- Check TierFeatures for User 159's tiers
-- ============================================================

-- First, find User 159's purchase tiers
SELECT DISTINCT
    sp."Id" as "PurchaseId",
    sp."SponsorId",
    sp."SubscriptionTierId",
    st."TierName",
    sp."PaymentStatus"
FROM public."SponsorshipPurchases" sp
INNER JOIN public."SubscriptionTiers" st ON sp."SubscriptionTierId" = st."Id"
WHERE sp."SponsorId" = 159
  AND sp."PaymentStatus" = 'Completed'
ORDER BY sp."Id" DESC;

-- Then check TierFeatures for those tiers
SELECT
    tf."Id" as "TierFeatureId",
    st."Id" as "TierId",
    st."TierName",
    f."FeatureKey",
    tf."IsEnabled",
    tf."ConfigurationJson",
    tf."ConfigurationJson"::jsonb->>'percentage' as "Percentage",
    tf."EffectiveDate",
    tf."ExpiryDate"
FROM public."TierFeatures" tf
INNER JOIN public."SubscriptionTiers" st ON tf."SubscriptionTierId" = st."Id"
INNER JOIN public."Features" f ON tf."FeatureId" = f."Id"
WHERE f."FeatureKey" = 'data_access_percentage'
  AND st."Id" IN (
    SELECT DISTINCT sp."SubscriptionTierId"
    FROM public."SponsorshipPurchases" sp
    WHERE sp."SponsorId" = 159
      AND sp."PaymentStatus" = 'Completed'
  )
ORDER BY st."Id";

-- ============================================================
-- EXPECTED RESULTS:
-- ============================================================
-- Feature: IsActive = true, IsDeprecated = false
-- TierFeatures: Should show M and L tiers with their percentages
-- If Feature.IsActive = false → Feature is completely ignored!
-- If TierFeature.IsEnabled = false → That tier doesn't have access
-- ============================================================
