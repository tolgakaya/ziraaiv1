-- =====================================================================
-- Rollback: rollback_002.sql
-- Purpose: Rollback 002_alter_sponsorship_purchases.sql
-- Author: Claude Code
-- Date: 2025-11-21
-- Environment: Staging (PostgreSQL)
-- WARNING: This will remove PaymentTransactionId from SponsorshipPurchases!
-- =====================================================================

-- Drop the index first
DROP INDEX IF EXISTS "IDX_SponsorshipPurchases_PaymentTransactionId";

-- Drop the foreign key constraint
ALTER TABLE "SponsorshipPurchases"
DROP CONSTRAINT IF EXISTS "FK_SponsorshipPurchases_PaymentTransactions";

-- Remove the column
ALTER TABLE "SponsorshipPurchases"
DROP COLUMN IF EXISTS "PaymentTransactionId";

-- Verification query
SELECT EXISTS (
    SELECT FROM information_schema.columns
    WHERE table_name = 'SponsorshipPurchases'
        AND column_name = 'PaymentTransactionId'
) AS column_exists;

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Rollback 002 completed successfully';
    RAISE NOTICE 'PaymentTransactionId column removed from SponsorshipPurchases table';
END $$;
