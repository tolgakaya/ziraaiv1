-- Verify which subscription should be selected as active for User 165
-- This matches the logic we implemented in GetFarmerJourneyQuery.cs (Lines 182-188)

SELECT
    us."Id" as subscription_id,
    st."TierName",
    us."IsActive",
    us."Status",
    us."EndDate",
    us."CreatedDate",
    CASE
        -- These are the exact filters from our code
        WHEN us."IsActive" = true
         AND us."Status" = 'Active'
         AND us."EndDate" > NOW()
        THEN '✅ SHOULD BE SELECTED'
        ELSE '❌ Filtered out'
    END as selection_status,
    CASE
        WHEN us."IsActive" = false THEN 'IsActive = false'
        WHEN us."Status" != 'Active' THEN 'Status != Active'
        WHEN us."EndDate" <= NOW() THEN 'EndDate expired'
        ELSE 'Passes all filters'
    END as reason
FROM "UserSubscriptions" us
JOIN "SubscriptionTiers" st ON us."SubscriptionTierId" = st."Id"
WHERE us."UserId" = 165
ORDER BY
    us."EndDate" DESC,  -- Primary sort: furthest end date
    us."CreatedDate" DESC;  -- Secondary sort: most recent

-- Expected Result: L tier (ID: 154) should be at top with ✅ status
