-- =============================================
-- Migration: Add Quantity Limits to SubscriptionTiers
-- Date: 2025-10-11
-- Description: Add MinPurchaseQuantity, MaxPurchaseQuantity, and RecommendedQuantity fields
-- =============================================

-- Step 1: Add new columns
ALTER TABLE "SubscriptionTiers"
ADD COLUMN "MinPurchaseQuantity" integer NOT NULL DEFAULT 10,
ADD COLUMN "MaxPurchaseQuantity" integer NOT NULL DEFAULT 10000,
ADD COLUMN "RecommendedQuantity" integer NOT NULL DEFAULT 100;

-- Step 2: Update existing tiers with appropriate limits
-- Trial tier - Not for sponsorship purchases, set minimal limits
UPDATE "SubscriptionTiers"
SET
    "MinPurchaseQuantity" = 1,
    "MaxPurchaseQuantity" = 10,
    "RecommendedQuantity" = 5
WHERE "TierName" = 'Trial';

-- S (Small) tier - For small sponsors
UPDATE "SubscriptionTiers"
SET
    "MinPurchaseQuantity" = 10,
    "MaxPurchaseQuantity" = 100,
    "RecommendedQuantity" = 50
WHERE "TierName" = 'S';

-- M (Medium) tier - For medium sponsors
UPDATE "SubscriptionTiers"
SET
    "MinPurchaseQuantity" = 20,
    "MaxPurchaseQuantity" = 500,
    "RecommendedQuantity" = 100
WHERE "TierName" = 'M';

-- L (Large) tier - For large sponsors
UPDATE "SubscriptionTiers"
SET
    "MinPurchaseQuantity" = 50,
    "MaxPurchaseQuantity" = 2000,
    "RecommendedQuantity" = 500
WHERE "TierName" = 'L';

-- XL (Extra Large) tier - For enterprise sponsors
UPDATE "SubscriptionTiers"
SET
    "MinPurchaseQuantity" = 100,
    "MaxPurchaseQuantity" = 10000,
    "RecommendedQuantity" = 1000
WHERE "TierName" = 'XL';

-- Step 3: Add check constraints to ensure data integrity
ALTER TABLE "SubscriptionTiers"
ADD CONSTRAINT "CK_SubscriptionTiers_MinQuantity_Positive"
CHECK ("MinPurchaseQuantity" > 0);

ALTER TABLE "SubscriptionTiers"
ADD CONSTRAINT "CK_SubscriptionTiers_MaxQuantity_GreaterThanMin"
CHECK ("MaxPurchaseQuantity" >= "MinPurchaseQuantity");

ALTER TABLE "SubscriptionTiers"
ADD CONSTRAINT "CK_SubscriptionTiers_RecommendedQuantity_InRange"
CHECK ("RecommendedQuantity" >= "MinPurchaseQuantity" AND "RecommendedQuantity" <= "MaxPurchaseQuantity");

-- Step 4: Create index for better query performance
CREATE INDEX "IX_SubscriptionTiers_Quantities"
ON "SubscriptionTiers" ("MinPurchaseQuantity", "MaxPurchaseQuantity");

-- Step 5: Verify the changes
SELECT
    "Id",
    "TierName",
    "DisplayName",
    "MinPurchaseQuantity",
    "MaxPurchaseQuantity",
    "RecommendedQuantity",
    "MonthlyPrice"
FROM "SubscriptionTiers"
ORDER BY "DisplayOrder";

-- =============================================
-- Rollback Script (if needed)
-- =============================================
/*
-- Drop constraints
ALTER TABLE "SubscriptionTiers" DROP CONSTRAINT IF EXISTS "CK_SubscriptionTiers_MinQuantity_Positive";
ALTER TABLE "SubscriptionTiers" DROP CONSTRAINT IF EXISTS "CK_SubscriptionTiers_MaxQuantity_GreaterThanMin";
ALTER TABLE "SubscriptionTiers" DROP CONSTRAINT IF EXISTS "CK_SubscriptionTiers_RecommendedQuantity_InRange";

-- Drop index
DROP INDEX IF EXISTS "IX_SubscriptionTiers_Quantities";

-- Drop columns
ALTER TABLE "SubscriptionTiers"
DROP COLUMN IF EXISTS "MinPurchaseQuantity",
DROP COLUMN IF EXISTS "MaxPurchaseQuantity",
DROP COLUMN IF EXISTS "RecommendedQuantity";
*/

-- =============================================
-- Summary of Changes
-- =============================================
-- | Tier  | Min | Max   | Recommended | Monthly Price |
-- |-------|-----|-------|-------------|---------------|
-- | Trial | 1   | 10    | 5           | 0 TRY         |
-- | S     | 10  | 100   | 50          | 99.99 TRY     |
-- | M     | 20  | 500   | 100         | 299.99 TRY    |
-- | L     | 50  | 2000  | 500         | 599.99 TRY    |
-- | XL    | 100 | 10000 | 1000        | 1499.99 TRY   |
-- =============================================
