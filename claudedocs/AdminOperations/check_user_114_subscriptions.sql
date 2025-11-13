-- Check User 114's subscription status to debug why currentTier shows "None"
SELECT
    us."Id" as subscription_id,
    st."TierName",
    us."IsActive",
    us."Status",
    us."EndDate",
    us."CreatedDate",
    CASE
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
WHERE us."UserId" = 114
ORDER BY us."Id" ASC;  -- Order by ID to see which would be selected first

-- Expected: Should show why no subscription is being selected (all filtered out?)
