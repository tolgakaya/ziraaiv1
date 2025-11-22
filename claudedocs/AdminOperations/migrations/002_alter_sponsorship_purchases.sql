-- =====================================================================
-- Migration: 002_alter_sponsorship_purchases.sql
-- Purpose: Add PaymentTransactionId to SponsorshipPurchases table
-- Author: Claude Code
-- Date: 2025-11-21
-- Environment: Staging (PostgreSQL)
-- =====================================================================

-- Add PaymentTransactionId column to SponsorshipPurchases
ALTER TABLE "SponsorshipPurchases"
ADD COLUMN IF NOT EXISTS "PaymentTransactionId" INTEGER NULL;

-- Add foreign key constraint
ALTER TABLE "SponsorshipPurchases"
ADD CONSTRAINT "FK_SponsorshipPurchases_PaymentTransactions"
FOREIGN KEY ("PaymentTransactionId")
REFERENCES "PaymentTransactions"("Id")
ON DELETE SET NULL;  -- If payment transaction is deleted, set to NULL

-- Create index for foreign key
CREATE INDEX IF NOT EXISTS "IDX_SponsorshipPurchases_PaymentTransactionId"
ON "SponsorshipPurchases"("PaymentTransactionId");

-- Add comment for documentation
COMMENT ON COLUMN "SponsorshipPurchases"."PaymentTransactionId" IS 'Foreign key to PaymentTransactions table for payment tracking';

-- Verification query
SELECT
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'SponsorshipPurchases'
    AND column_name = 'PaymentTransactionId';

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Migration 002_alter_sponsorship_purchases.sql completed successfully';
    RAISE NOTICE 'PaymentTransactionId column added to SponsorshipPurchases table';
END $$;
