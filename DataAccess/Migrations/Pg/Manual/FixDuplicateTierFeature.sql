-- Fix duplicate TierFeature entries for Medium tier
-- Run this BEFORE applying the full migration if you already ran the faulty version

-- Delete duplicate entries for Medium tier (keep the ones with ConfigurationJson)
DELETE FROM "TierFeatures" 
WHERE "SubscriptionTierId" = 3 
  AND "FeatureId" = 7 
  AND "ConfigurationJson" IS NULL;

-- Verify only one entry remains
SELECT * FROM "TierFeatures" 
WHERE "SubscriptionTierId" = 3 
  AND "FeatureId" = 7;

-- Expected result: 1 row with ConfigurationJson = '{"percentage": 60}'
