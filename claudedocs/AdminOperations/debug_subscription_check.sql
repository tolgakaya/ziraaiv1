-- Check User 165's subscriptions to debug currentTier issue
-- Timeline shows L tier created at 2025-11-07T15:35:09.534 (newer)
-- But currentTier returns "Trial" created at 2025-11-07T13:38:03.767 (older)

SELECT
    us."Id" as subscription_id,
    st."TierName",
    st."DisplayName",
    us."IsActive",
    us."StartDate",
    us."EndDate",
    us."CreatedDate",
    us."Status",
    us."QueueStatus",
    CASE
        WHEN us."EndDate" >= NOW() THEN 'Future/Valid'
        ELSE 'Expired'
    END as end_date_status,
    CASE
        WHEN us."IsActive" = true AND us."EndDate" >= NOW() THEN 'YES - Should be selected'
        WHEN us."IsActive" = false THEN 'NO - IsActive=false'
        WHEN us."EndDate" < NOW() THEN 'NO - EndDate expired'
        ELSE 'NO - Other reason'
    END as should_be_selected
FROM "UserSubscriptions" us
JOIN "SubscriptionTiers" st ON us."SubscriptionTierId" = st."Id"
WHERE us."UserId" = 165
ORDER BY us."CreatedDate" DESC;

-- Expected: L tier should have IsActive=true AND EndDate >= NOW()
-- If L tier has IsActive=false or EndDate < NOW(), that explains why Trial is selected
