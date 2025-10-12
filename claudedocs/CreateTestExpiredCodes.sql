-- ============================================================================
-- Create Test Data: Sent and Expired Sponsorship Codes
-- ============================================================================
-- Purpose: Generate test data for testing sent+expired codes filter
-- Use Case: Test the new onlySentExpired=true endpoint
-- ============================================================================

-- ============================================================================
-- Option 1: Update Existing Codes to be Sent and Expired (RECOMMENDED)
-- ============================================================================
-- This updates existing unsent/unused codes to appear as sent and expired

-- Step 1: Find some unused codes to convert
SELECT
    "Id",
    "Code",
    "SponsorId",
    "ExpiryDate",
    "DistributionDate",
    "IsUsed"
FROM "SponsorshipCodes"
WHERE "IsUsed" = false
  AND "DistributionDate" IS NULL
LIMIT 10;

-- Step 2: Update 5-10 codes to be sent and expired
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NOW() - INTERVAL '35 days',  -- Sent 35 days ago
    "ExpiryDate" = NOW() - INTERVAL '5 days',         -- Expired 5 days ago
    "DistributedTo" = 'Test Farmer - farmer@test.com',
    "DistributionChannel" = 'SMS'
WHERE "Id" IN (
    SELECT "Id"
    FROM "SponsorshipCodes"
    WHERE "IsUsed" = false
      AND "DistributionDate" IS NULL
    LIMIT 5  -- Change this number as needed
)
RETURNING "Id", "Code", "ExpiryDate", "DistributionDate";

-- ============================================================================
-- Option 2: Update Specific Codes by ID
-- ============================================================================
-- If you know the specific code IDs you want to update

-- First, find the codes you want to update
SELECT
    "Id",
    "Code",
    "SponsorId",
    "ExpiryDate",
    "DistributionDate",
    "IsUsed"
FROM "SponsorshipCodes"
WHERE "SponsorId" = 1  -- Replace with your sponsor ID
  AND "IsUsed" = false
LIMIT 10;

-- Then update specific codes
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NOW() - INTERVAL '30 days',  -- Sent 30 days ago
    "ExpiryDate" = NOW() - INTERVAL '1 day',          -- Expired yesterday
    "DistributedTo" = 'Test Farmer - test@example.com',
    "DistributionChannel" = 'WhatsApp'
WHERE "Id" IN (101, 102, 103, 104, 105)  -- Replace with actual IDs
RETURNING "Id", "Code", "ExpiryDate", "DistributionDate";

-- ============================================================================
-- Option 3: Create Various Test Scenarios
-- ============================================================================

-- Scenario A: Codes sent 10 days ago, expired 5 days ago
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NOW() - INTERVAL '10 days',
    "ExpiryDate" = NOW() - INTERVAL '5 days',
    "DistributedTo" = 'Scenario A - farmer1@test.com',
    "DistributionChannel" = 'SMS'
WHERE "Id" IN (
    SELECT "Id"
    FROM "SponsorshipCodes"
    WHERE "IsUsed" = false AND "DistributionDate" IS NULL
    LIMIT 3
)
RETURNING "Id", "Code", "ExpiryDate", "DistributionDate";

-- Scenario B: Codes sent 45 days ago, expired 15 days ago
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NOW() - INTERVAL '45 days',
    "ExpiryDate" = NOW() - INTERVAL '15 days',
    "DistributedTo" = 'Scenario B - farmer2@test.com',
    "DistributionChannel" = 'WhatsApp'
WHERE "Id" IN (
    SELECT "Id"
    FROM "SponsorshipCodes"
    WHERE "IsUsed" = false AND "DistributionDate" IS NULL
    LIMIT 3
)
RETURNING "Id", "Code", "ExpiryDate", "DistributionDate";

-- Scenario C: Codes sent 7 days ago, expired today (just expired)
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NOW() - INTERVAL '7 days',
    "ExpiryDate" = NOW() - INTERVAL '1 hour',  -- Expired 1 hour ago
    "DistributedTo" = 'Scenario C - farmer3@test.com',
    "DistributionChannel" = 'Email'
WHERE "Id" IN (
    SELECT "Id"
    FROM "SponsorshipCodes"
    WHERE "IsUsed" = false AND "DistributionDate" IS NULL
    LIMIT 2
)
RETURNING "Id", "Code", "ExpiryDate", "DistributionDate";

-- ============================================================================
-- Option 4: Update by Sponsor ID (All codes for a sponsor)
-- ============================================================================

-- Update all unsent codes for a specific sponsor to be sent and expired
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NOW() - INTERVAL '20 days',
    "ExpiryDate" = NOW() - INTERVAL '3 days',
    "DistributedTo" = 'Bulk Test - farmers@test.com',
    "DistributionChannel" = 'Bulk SMS'
WHERE "SponsorId" = 1  -- Replace with your sponsor ID
  AND "IsUsed" = false
  AND "DistributionDate" IS NULL
  AND "Id" IN (
      SELECT "Id" FROM "SponsorshipCodes"
      WHERE "SponsorId" = 1 AND "IsUsed" = false AND "DistributionDate" IS NULL
      LIMIT 10  -- Limit to 10 codes
  )
RETURNING "Id", "Code", "SponsorId", "ExpiryDate", "DistributionDate";

-- ============================================================================
-- Verification Queries
-- ============================================================================

-- Check sent and expired codes
SELECT
    "Id",
    "Code",
    "SponsorId",
    "DistributionDate",
    "ExpiryDate",
    "IsUsed",
    "DistributedTo",
    "DistributionChannel",
    NOW() - "DistributionDate" as days_since_sent,
    NOW() - "ExpiryDate" as days_since_expired
FROM "SponsorshipCodes"
WHERE "DistributionDate" IS NOT NULL
  AND "ExpiryDate" < NOW()
  AND "IsUsed" = false
ORDER BY "ExpiryDate" DESC
LIMIT 20;

-- Count by scenario
SELECT
    CASE
        WHEN "DistributionDate" IS NULL THEN 'Unsent'
        WHEN "ExpiryDate" < NOW() AND "IsUsed" = false THEN 'Sent & Expired'
        WHEN "ExpiryDate" >= NOW() AND "IsUsed" = false THEN 'Sent & Active'
        WHEN "IsUsed" = true THEN 'Used'
    END as status,
    COUNT(*) as count
FROM "SponsorshipCodes"
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- Rollback Script (UNDO Changes)
-- ============================================================================
-- Use this to revert the test data changes

/*
-- Revert specific codes back to unsent
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NULL,
    "ExpiryDate" = NOW() + INTERVAL '30 days',  -- Reset to 30 days from now
    "DistributedTo" = NULL,
    "DistributionChannel" = NULL
WHERE "Id" IN (101, 102, 103, 104, 105)  -- Replace with the IDs you changed
RETURNING "Id", "Code", "ExpiryDate", "DistributionDate";

-- Or revert all test codes (if you added a marker)
UPDATE "SponsorshipCodes"
SET
    "DistributionDate" = NULL,
    "ExpiryDate" = NOW() + INTERVAL '30 days',
    "DistributedTo" = NULL,
    "DistributionChannel" = NULL
WHERE "DistributedTo" LIKE '%test%' OR "DistributedTo" LIKE '%Test%'
RETURNING "Id", "Code", "ExpiryDate", "DistributionDate";
*/

-- ============================================================================
-- Test API Endpoint
-- ============================================================================
-- After creating test data, test the endpoint:
-- GET /api/v1/sponsorship/codes?onlySentExpired=true&page=1&pageSize=50
--
-- Expected result: Should return the codes you just created with:
-- - DistributionDate IS NOT NULL
-- - ExpiryDate < NOW()
-- - IsUsed = false
-- ============================================================================

-- ============================================================================
-- Notes:
-- ============================================================================
-- 1. Use Option 1 for quick testing (updates random codes)
-- 2. Use Option 2 for specific code testing (update by ID)
-- 3. Use Option 3 for multiple test scenarios
-- 4. Use Option 4 for bulk testing with a sponsor
-- 5. Always verify with the verification queries
-- 6. Use rollback script to clean up test data
-- 7. Test dates are relative (NOW() - INTERVAL) so they work anytime
-- ============================================================================
