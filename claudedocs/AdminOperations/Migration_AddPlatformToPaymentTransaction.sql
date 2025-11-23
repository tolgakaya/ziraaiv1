-- Migration: Add Platform field to PaymentTransactions table
-- Date: 2025-11-22
-- Purpose: Support platform-based redirect (iOS/Android deep link vs Web URL)
-- Related: Web payment integration requirement

-- =====================================================
-- Step 1: Add Platform column with default value
-- =====================================================

-- Add Platform column (nullable first to avoid errors on existing data)
ALTER TABLE "PaymentTransactions"
ADD COLUMN "Platform" VARCHAR(20);

-- Update existing records to default platform (iOS for backward compatibility)
UPDATE "PaymentTransactions"
SET "Platform" = 'iOS'
WHERE "Platform" IS NULL;

-- Make Platform column NOT NULL
ALTER TABLE "PaymentTransactions"
ALTER COLUMN "Platform" SET NOT NULL;

-- Set default value for future inserts
ALTER TABLE "PaymentTransactions"
ALTER COLUMN "Platform" SET DEFAULT 'iOS';

-- =====================================================
-- Step 2: Add check constraint for valid platforms
-- =====================================================

ALTER TABLE "PaymentTransactions"
ADD CONSTRAINT "CK_PaymentTransactions_Platform"
CHECK ("Platform" IN ('iOS', 'Android', 'Web'));

-- =====================================================
-- Step 3: Add index for performance (optional but recommended)
-- =====================================================

CREATE INDEX "IX_PaymentTransactions_Platform"
ON "PaymentTransactions" ("Platform");

-- =====================================================
-- Step 4: Verification queries
-- =====================================================

-- Check column exists and has correct type
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'PaymentTransactions'
  AND column_name = 'Platform';

-- Check existing data
SELECT "Platform", COUNT(*) as Count
FROM "PaymentTransactions"
GROUP BY "Platform";

-- Check constraint exists
SELECT constraint_name, check_clause
FROM information_schema.check_constraints
WHERE constraint_name = 'CK_PaymentTransactions_Platform';

-- =====================================================
-- Rollback Script (if needed)
-- =====================================================

-- DROP INDEX "IX_PaymentTransactions_Platform";
-- ALTER TABLE "PaymentTransactions" DROP CONSTRAINT "CK_PaymentTransactions_Platform";
-- ALTER TABLE "PaymentTransactions" DROP COLUMN "Platform";
