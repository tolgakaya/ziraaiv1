-- Debug Analysis 59 Tier and Messaging Feature
-- Purpose: Verify why messaging is failing for L tier analysis

-- Analysis 59'un bilgilerini görelim
SELECT 
    pa."Id" as "AnalysisId",
    pa."ActiveSponsorshipId",
    us."SubscriptionTierId",
    st."TierName",
    tf."IsEnabled" as "MessagingEnabled",
    f."FeatureKey",
    f."DisplayName"
FROM "PlantAnalyses" pa
JOIN "UserSubscriptions" us ON us."Id" = pa."ActiveSponsorshipId"
JOIN "SubscriptionTiers" st ON st."Id" = us."SubscriptionTierId"
LEFT JOIN "TierFeatures" tf ON tf."SubscriptionTierId" = us."SubscriptionTierId" 
    AND tf."FeatureId" = 1  -- messaging feature
LEFT JOIN "Features" f ON f."Id" = 1
WHERE pa."Id" = 59;

-- Expected Result:
-- AnalysisId | ActiveSponsorshipId | SubscriptionTierId | TierName | MessagingEnabled | FeatureKey | DisplayName
-- 59         | XXX                 | 4                  | L        | true             | messaging  | Messaging
