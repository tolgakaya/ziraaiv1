-- ============================================================================
-- Cleanup Script for UserId=189
-- ============================================================================
-- Purpose: Fix multiple active subscriptions issue for UserId=189
-- Strategy: Keep CreditCard subscription active, queue sponsorship subscriptions
-- Date: 2025-11-24
-- ============================================================================

-- IMPORTANT: Run this in a transaction so we can rollback if needed
BEGIN;

-- ============================================================================
-- STEP 1: Current State Analysis
-- ============================================================================
SELECT 
    '=== BEFORE CLEANUP ===' as step,
    Id,
    UserId,
    PaymentMethod,
    IsSponsoredSubscription,
    QueueStatus,
    IsActive,
    Status,
    PreviousSponsorshipId,
    EndDate,
    CreatedDate
FROM "UserSubscriptions"
WHERE "UserId" = 189
ORDER BY "CreatedDate";

-- ============================================================================
-- STEP 2: Update Sponsorship Subscriptions to Pending
-- ============================================================================
-- Queue subscriptions: ID 188, 189, 190
-- Keep CreditCard subscription (ID 187) active

UPDATE "UserSubscriptions"
SET 
    "QueueStatus" = 0,  -- Pending
    "IsActive" = false,
    "Status" = 'Pending',
    "UpdatedDate" = NOW(),
    "SponsorshipNotes" = CONCAT(
        COALESCE("SponsorshipNotes", ''), 
        ' | Queued on ', NOW()::text, 
        ' (data cleanup - was incorrectly activated)'
    )
WHERE "UserId" = 189
  AND "Id" IN (188, 189, 190)
  AND "IsSponsoredSubscription" = true;

-- ============================================================================
-- STEP 3: Set Up Queue Chain
-- ============================================================================
-- Chain: 187 (CreditCard) → 188 → 189 → 190

-- ID 188 waits for ID 187 (CreditCard)
UPDATE "UserSubscriptions"
SET "PreviousSponsorshipId" = 187,
    "UpdatedDate" = NOW()
WHERE "Id" = 188;

-- ID 189 waits for ID 188
UPDATE "UserSubscriptions"
SET "PreviousSponsorshipId" = 188,
    "UpdatedDate" = NOW()
WHERE "Id" = 189;

-- ID 190 waits for ID 189
UPDATE "UserSubscriptions"
SET "PreviousSponsorshipId" = 189,
    "UpdatedDate" = NOW()
WHERE "Id" = 190;

-- ============================================================================
-- STEP 4: Verify Cleanup
-- ============================================================================
SELECT 
    '=== AFTER CLEANUP ===' as step,
    Id,
    PaymentMethod,
    IsSponsoredSubscription,
    QueueStatus,
    IsActive,
    Status,
    PreviousSponsorshipId,
    CASE 
        WHEN "Id" = 187 THEN '✅ Active CreditCard'
        WHEN "Id" = 188 THEN '⏳ Pending → Waits for 187'
        WHEN "Id" = 189 THEN '⏳ Pending → Waits for 188'
        WHEN "Id" = 190 THEN '⏳ Pending → Waits for 189'
    END as expected_state,
    EndDate
FROM "UserSubscriptions"
WHERE "UserId" = 189
ORDER BY "CreatedDate";

-- ============================================================================
-- STEP 5: Validation Checks
-- ============================================================================

-- Check 1: Only ONE active subscription
SELECT 
    '=== VALIDATION: Active Count ===' as check_name,
    COUNT(*) as active_count,
    CASE WHEN COUNT(*) = 1 THEN '✅ PASS' ELSE '❌ FAIL' END as result
FROM "UserSubscriptions"
WHERE "UserId" = 189
  AND "IsActive" = true
  AND "Status" = 'Active';

-- Check 2: Three pending sponsorships
SELECT 
    '=== VALIDATION: Pending Count ===' as check_name,
    COUNT(*) as pending_count,
    CASE WHEN COUNT(*) = 3 THEN '✅ PASS' ELSE '❌ FAIL' END as result
FROM "UserSubscriptions"
WHERE "UserId" = 189
  AND "QueueStatus" = 0  -- Pending
  AND "IsSponsoredSubscription" = true;

-- Check 3: Queue chain is correct
WITH QueueChain AS (
    SELECT 
        "Id",
        "PreviousSponsorshipId",
        CASE 
            WHEN "Id" = 187 AND "PreviousSponsorshipId" IS NULL THEN true
            WHEN "Id" = 188 AND "PreviousSponsorshipId" = 187 THEN true
            WHEN "Id" = 189 AND "PreviousSponsorshipId" = 188 THEN true
            WHEN "Id" = 190 AND "PreviousSponsorshipId" = 189 THEN true
            ELSE false
        END as chain_correct
    FROM "UserSubscriptions"
    WHERE "UserId" = 189
      AND "Id" IN (187, 188, 189, 190)
)
SELECT 
    '=== VALIDATION: Queue Chain ===' as check_name,
    COUNT(*) FILTER (WHERE chain_correct = true) as correct_count,
    CASE WHEN COUNT(*) FILTER (WHERE chain_correct = true) = 4 
         THEN '✅ PASS' 
         ELSE '❌ FAIL' 
    END as result
FROM QueueChain;

-- ============================================================================
-- STEP 6: Final Decision
-- ============================================================================
-- Review the output above. If all validation checks PASS:
-- COMMIT;

-- If any check FAILS:
-- ROLLBACK;

-- For now, let's rollback to be safe (remove comment to commit)
ROLLBACK;

-- To actually apply changes, replace ROLLBACK with:
-- COMMIT;

-- ============================================================================
-- NOTES
-- ============================================================================
-- After running this script with COMMIT:
-- 1. User 189 will have 1 active CreditCard subscription
-- 2. When CreditCard expires, subscription 188 will activate automatically
-- 3. When 188 expires, 189 will activate
-- 4. When 189 expires, 190 will activate
-- 5. This is the correct queue behavior
-- ============================================================================
