-- ============================================================================
-- Generic Cleanup Script - Multiple Active Subscriptions
-- ============================================================================
-- Purpose: Fix ALL users with multiple active paid subscriptions
-- Strategy: Keep highest priority subscription active, queue others
-- Priority: CreditCard > BankTransfer > Sponsorship
-- Date: 2025-11-24
-- ============================================================================

BEGIN;

-- ============================================================================
-- STEP 1: Identify Affected Users
-- ============================================================================
SELECT 
    '=== AFFECTED USERS ===' as step,
    "UserId",
    COUNT(*) as active_count,
    STRING_AGG(CONCAT('ID:', "Id", '(', "PaymentMethod", ')'), ', ' ORDER BY "CreatedDate") as subscription_list
FROM "UserSubscriptions"
WHERE "IsActive" = true
  AND "Status" = 'Active'
  AND "EndDate" > NOW()
  AND "IsTrialSubscription" = false
GROUP BY "UserId"
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- ============================================================================
-- STEP 2: Create Cleanup Plan
-- ============================================================================
-- Create temp table with what should be kept vs queued
CREATE TEMP TABLE SubscriptionCleanupPlan AS
WITH RankedSubscriptions AS (
    SELECT 
        "Id",
        "UserId",
        "PaymentMethod",
        "IsSponsoredSubscription",
        "IsTrialSubscription",
        "CreatedDate",
        "EndDate",
        -- Priority: CreditCard (1) > BankTransfer (2) > Sponsorship (3) > Others (4)
        ROW_NUMBER() OVER (
            PARTITION BY "UserId" 
            ORDER BY 
                CASE 
                    WHEN "PaymentMethod" = 'CreditCard' THEN 1
                    WHEN "PaymentMethod" = 'BankTransfer' THEN 2
                    WHEN "IsSponsoredSubscription" = true THEN 3
                    ELSE 4
                END,
                "CreatedDate" DESC  -- Newest first if same priority
        ) as priority_rank,
        LAG("Id") OVER (
            PARTITION BY "UserId" 
            ORDER BY 
                CASE 
                    WHEN "PaymentMethod" = 'CreditCard' THEN 1
                    WHEN "PaymentMethod" = 'BankTransfer' THEN 2
                    WHEN "IsSponsoredSubscription" = true THEN 3
                    ELSE 4
                END,
                "CreatedDate" DESC
        ) as previous_subscription_id
    FROM "UserSubscriptions"
    WHERE "IsActive" = true
      AND "Status" = 'Active'
      AND "EndDate" > NOW()
      AND "IsTrialSubscription" = false
)
SELECT 
    "Id",
    "UserId",
    "PaymentMethod",
    "IsSponsoredSubscription",
    priority_rank,
    CASE 
        WHEN priority_rank = 1 THEN 'KEEP_ACTIVE'
        ELSE 'QUEUE'
    END as action,
    previous_subscription_id as wait_for_subscription_id,
    "CreatedDate",
    "EndDate"
FROM RankedSubscriptions;

-- ============================================================================
-- STEP 3: Review Cleanup Plan
-- ============================================================================
SELECT 
    '=== CLEANUP PLAN ===' as step,
    "UserId",
    "Id" as subscription_id,
    "PaymentMethod",
    action,
    wait_for_subscription_id,
    "EndDate"
FROM SubscriptionCleanupPlan
WHERE action = 'QUEUE'
ORDER BY "UserId", priority_rank;

-- Count affected subscriptions
SELECT 
    '=== SUMMARY ===' as step,
    COUNT(DISTINCT "UserId") as affected_users,
    COUNT(*) FILTER (WHERE action = 'KEEP_ACTIVE') as kept_active,
    COUNT(*) FILTER (WHERE action = 'QUEUE') as will_be_queued
FROM SubscriptionCleanupPlan;

-- ============================================================================
-- STEP 4: Execute Cleanup (COMMENTED OUT FOR SAFETY)
-- ============================================================================
-- IMPORTANT: Review the plan above before uncommenting and running this!

/*
UPDATE "UserSubscriptions" us
SET 
    "QueueStatus" = 0,  -- Pending
    "IsActive" = false,
    "Status" = 'Pending',
    "PreviousSponsorshipId" = cp.wait_for_subscription_id,
    "UpdatedDate" = NOW(),
    "SponsorshipNotes" = CONCAT(
        COALESCE(us."SponsorshipNotes", ''), 
        ' | Queued on ', NOW()::text, 
        ' (auto-cleanup - was incorrectly activated)'
    )
FROM SubscriptionCleanupPlan cp
WHERE us."Id" = cp."Id"
  AND cp.action = 'QUEUE';
*/

-- ============================================================================
-- STEP 5: Verification Query
-- ============================================================================
-- Run this AFTER uncommenting and executing Step 4

/*
-- Check: No users should have multiple active paid subscriptions
SELECT 
    '=== POST-CLEANUP VALIDATION ===' as check_name,
    "UserId",
    COUNT(*) as active_count,
    CASE WHEN COUNT(*) = 1 THEN '✅ PASS' ELSE '❌ FAIL' END as result
FROM "UserSubscriptions"
WHERE "IsActive" = true
  AND "Status" = 'Active'
  AND "EndDate" > NOW()
  AND "IsTrialSubscription" = false
GROUP BY "UserId"
HAVING COUNT(*) > 1;

-- If above query returns no rows, cleanup was successful!
*/

-- ============================================================================
-- STEP 6: Final Decision
-- ============================================================================
-- Review all output above. 

-- Option 1: If cleanup plan looks good and you want to proceed:
-- 1. Uncomment the UPDATE statement in Step 4
-- 2. Uncomment the validation query in Step 5
-- 3. Replace ROLLBACK below with COMMIT
-- 4. Run the entire script

-- Option 2: If you want to test first or review more:
-- Keep ROLLBACK (nothing will be changed)

ROLLBACK;

-- To actually apply changes:
-- COMMIT;

-- ============================================================================
-- STEP 7: Manual Review for Complex Cases
-- ============================================================================
-- For users with unusual combinations, you may want to review manually:

/*
-- Users with mixed CreditCard + Sponsorship
SELECT 
    "UserId",
    "Id",
    "PaymentMethod",
    "IsSponsoredSubscription",
    "QueueStatus",
    "IsActive",
    "EndDate"
FROM "UserSubscriptions"
WHERE "UserId" IN (
    SELECT DISTINCT "UserId" 
    FROM SubscriptionCleanupPlan 
    WHERE action = 'QUEUE'
)
  AND "IsActive" = true
ORDER BY "UserId", "CreatedDate";
*/

-- ============================================================================
-- NOTES & RECOMMENDATIONS
-- ============================================================================
-- 1. Take a database backup BEFORE running this with COMMIT
-- 2. Run this script first with ROLLBACK to review the plan
-- 3. Review the cleanup plan carefully for each affected user
-- 4. Consider running cleanup for one user at a time initially
-- 5. Monitor logs after cleanup to ensure queue activation works correctly
-- 6. Users will NOT lose access - their highest priority subscription remains active
-- 7. Queued subscriptions will activate automatically when current expires
-- ============================================================================

-- ============================================================================
-- ROLLBACK PROCEDURE (if needed)
-- ============================================================================
-- If cleanup causes issues, you can restore subscriptions:

/*
-- Emergency rollback - restore all subscriptions that were queued
UPDATE "UserSubscriptions"
SET 
    "QueueStatus" = 1,  -- Active
    "IsActive" = true,
    "Status" = 'Active',
    "PreviousSponsorshipId" = NULL,
    "UpdatedDate" = NOW()
WHERE "SponsorshipNotes" LIKE '%auto-cleanup - was incorrectly activated%'
  AND "QueueStatus" = 0
  AND "UpdatedDate" > NOW() - INTERVAL '1 hour';  -- Only recent changes
*/

-- ============================================================================
