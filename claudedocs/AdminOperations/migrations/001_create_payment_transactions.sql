-- =====================================================================
-- Migration: 001_create_payment_transactions.sql
-- Purpose: Create PaymentTransactions table for iyzico payment integration
-- Author: Claude Code
-- Date: 2025-11-21
-- Environment: Staging (PostgreSQL)
-- =====================================================================

-- Create PaymentTransactions table
CREATE TABLE IF NOT EXISTS "PaymentTransactions" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "FlowType" VARCHAR(50) NOT NULL,  -- 'SponsorBulkPurchase' or 'FarmerSubscription'
    "FlowDataJson" TEXT NOT NULL,     -- JSON with flow-specific data
    "SponsorshipPurchaseId" INTEGER NULL,  -- For sponsor flow
    "UserSubscriptionId" INTEGER NULL,     -- For farmer flow (future)
    "IyzicoToken" VARCHAR(255) NOT NULL UNIQUE,  -- Token from iyzico initialize response
    "IyzicoPaymentId" VARCHAR(255) NULL,         -- Payment ID from iyzico verify response
    "ConversationId" VARCHAR(100) NOT NULL UNIQUE,  -- Unique conversation ID for tracking
    "Amount" NUMERIC(18, 2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'TRY',
    "Status" VARCHAR(50) NOT NULL,  -- 'Initialized', 'Pending', 'Success', 'Failed', 'Expired'
    "InitializedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CompletedAt" TIMESTAMP NULL,
    "ExpiresAt" TIMESTAMP NOT NULL,  -- Token expiration (usually 30 minutes from initialization)
    "InitializeResponse" TEXT NULL,  -- Full JSON response from iyzico initialize
    "VerifyResponse" TEXT NULL,      -- Full JSON response from iyzico verify
    "ErrorMessage" TEXT NULL,        -- Error details if payment failed
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedDate" TIMESTAMP NULL,

    -- Indexes for performance
    CONSTRAINT "IX_PaymentTransactions_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id"),
    CONSTRAINT "IX_PaymentTransactions_SponsorshipPurchaseId" FOREIGN KEY ("SponsorshipPurchaseId") REFERENCES "SponsorshipPurchases"("Id")
);

-- Create indexes for frequently queried columns
CREATE INDEX IF NOT EXISTS "IDX_PaymentTransactions_IyzicoToken" ON "PaymentTransactions"("IyzicoToken");
CREATE INDEX IF NOT EXISTS "IDX_PaymentTransactions_ConversationId" ON "PaymentTransactions"("ConversationId");
CREATE INDEX IF NOT EXISTS "IDX_PaymentTransactions_Status" ON "PaymentTransactions"("Status");
CREATE INDEX IF NOT EXISTS "IDX_PaymentTransactions_UserId" ON "PaymentTransactions"("UserId");
CREATE INDEX IF NOT EXISTS "IDX_PaymentTransactions_CreatedDate" ON "PaymentTransactions"("CreatedDate");
CREATE INDEX IF NOT EXISTS "IDX_PaymentTransactions_FlowType" ON "PaymentTransactions"("FlowType");

-- Add comments for documentation
COMMENT ON TABLE "PaymentTransactions" IS 'Stores all payment transactions for both sponsor bulk purchases and farmer subscriptions';
COMMENT ON COLUMN "PaymentTransactions"."FlowType" IS 'Type of payment flow: SponsorBulkPurchase or FarmerSubscription';
COMMENT ON COLUMN "PaymentTransactions"."FlowDataJson" IS 'JSON containing flow-specific data (tier, quantity, etc.)';
COMMENT ON COLUMN "PaymentTransactions"."IyzicoToken" IS 'Unique token from iyzico initialize response, used to complete payment';
COMMENT ON COLUMN "PaymentTransactions"."ConversationId" IS 'Unique identifier for tracking payment across systems';
COMMENT ON COLUMN "PaymentTransactions"."Status" IS 'Payment status: Initialized, Pending, Success, Failed, Expired';
COMMENT ON COLUMN "PaymentTransactions"."ExpiresAt" IS 'Token expiration time (typically 30 minutes from initialization)';

-- Verification query
SELECT
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'PaymentTransactions'
ORDER BY ordinal_position;

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Migration 001_create_payment_transactions.sql completed successfully';
END $$;
