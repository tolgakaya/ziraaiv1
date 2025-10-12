-- ============================================================================
-- Update ValidityDays Default Value from 365 to 30 Days
-- ============================================================================
-- Date: 2025-10-12
-- Purpose: Change default code expiry period from 365 days to 30 days
-- Impact: Only affects new SponsorshipPurchase records created after this change
-- Existing records remain unchanged
-- ============================================================================

BEGIN;

-- ============================================================================
-- 1. Update SponsorshipPurchases table default value
-- ============================================================================

COMMENT ON COLUMN "SponsorshipPurchases"."ValidityDays" IS
'Number of days after purchase when generated codes expire (can be redeemed). Default changed from 365 to 30 days on 2025-10-12.';

-- Change default value for ValidityDays column
ALTER TABLE "SponsorshipPurchases"
ALTER COLUMN "ValidityDays" SET DEFAULT 30;

-- Verify the change
SELECT
    column_name,
    column_default,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'SponsorshipPurchases'
  AND column_name = 'ValidityDays';

-- ============================================================================
-- 2. Add SubscriptionTiers columns (bonus from migration)
-- ============================================================================
-- These columns were added by the migration, add them if they don't exist

DO $$
BEGIN
    -- Add MinPurchaseQuantity if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'SubscriptionTiers'
        AND column_name = 'MinPurchaseQuantity'
    ) THEN
        ALTER TABLE "SubscriptionTiers"
        ADD COLUMN "MinPurchaseQuantity" INTEGER NOT NULL DEFAULT 0;

        COMMENT ON COLUMN "SubscriptionTiers"."MinPurchaseQuantity" IS
        'Minimum number of codes that can be purchased in a single transaction';
    END IF;

    -- Add MaxPurchaseQuantity if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'SubscriptionTiers'
        AND column_name = 'MaxPurchaseQuantity'
    ) THEN
        ALTER TABLE "SubscriptionTiers"
        ADD COLUMN "MaxPurchaseQuantity" INTEGER NOT NULL DEFAULT 0;

        COMMENT ON COLUMN "SubscriptionTiers"."MaxPurchaseQuantity" IS
        'Maximum number of codes that can be purchased in a single transaction';
    END IF;

    -- Add RecommendedQuantity if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'SubscriptionTiers'
        AND column_name = 'RecommendedQuantity'
    ) THEN
        ALTER TABLE "SubscriptionTiers"
        ADD COLUMN "RecommendedQuantity" INTEGER NOT NULL DEFAULT 0;

        COMMENT ON COLUMN "SubscriptionTiers"."RecommendedQuantity" IS
        'Recommended purchase quantity for optimal pricing and usage';
    END IF;
END $$;

-- ============================================================================
-- 3. Update seed data for SubscriptionTiers (if columns were just added)
-- ============================================================================

UPDATE "SubscriptionTiers"
SET
    "MinPurchaseQuantity" = 10,
    "MaxPurchaseQuantity" = 10000,
    "RecommendedQuantity" = 100
WHERE "MinPurchaseQuantity" = 0
   OR "MaxPurchaseQuantity" = 0
   OR "RecommendedQuantity" = 0;

-- ============================================================================
-- 4. Verification Queries
-- ============================================================================

-- Check SponsorshipPurchases default value
SELECT
    'SponsorshipPurchases.ValidityDays' as "Column",
    column_default as "Default Value"
FROM information_schema.columns
WHERE table_name = 'SponsorshipPurchases'
  AND column_name = 'ValidityDays';

-- Check SubscriptionTiers new columns
SELECT
    "Id",
    "TierName",
    "DisplayName",
    "MinPurchaseQuantity",
    "MaxPurchaseQuantity",
    "RecommendedQuantity"
FROM "SubscriptionTiers"
ORDER BY "Id";

-- Check existing SponsorshipPurchases (optional - for information only)
SELECT
    "Id",
    "SponsorId",
    "ValidityDays",
    "PurchaseDate",
    "Status"
FROM "SponsorshipPurchases"
ORDER BY "PurchaseDate" DESC
LIMIT 10;

COMMIT;

-- ============================================================================
-- ROLLBACK SCRIPT (if needed)
-- ============================================================================
-- Uncomment and run if you need to revert changes

/*
BEGIN;

-- Revert ValidityDays default to 365
ALTER TABLE "SponsorshipPurchases"
ALTER COLUMN "ValidityDays" SET DEFAULT 365;

-- Remove SubscriptionTiers columns (optional - only if you want to completely revert)
ALTER TABLE "SubscriptionTiers" DROP COLUMN IF EXISTS "MinPurchaseQuantity";
ALTER TABLE "SubscriptionTiers" DROP COLUMN IF EXISTS "MaxPurchaseQuantity";
ALTER TABLE "SubscriptionTiers" DROP COLUMN IF EXISTS "RecommendedQuantity";

COMMIT;
*/

-- ============================================================================
-- Notes:
-- ============================================================================
-- 1. This script only changes the DEFAULT value for new records
-- 2. Existing SponsorshipPurchases records are NOT affected
-- 3. Existing unredeemed SponsorshipCodes keep their current ExpiryDate
-- 4. Only NEW purchases will use 30-day default (unless sponsor specifies otherwise)
-- 5. Sponsors can still customize ValidityDays when purchasing codes
-- ============================================================================
