-- =====================================================================
-- Rollback: rollback_001.sql
-- Purpose: Rollback 001_create_payment_transactions.sql
-- Author: Claude Code
-- Date: 2025-11-21
-- Environment: Staging (PostgreSQL)
-- WARNING: This will delete ALL data in PaymentTransactions table!
-- =====================================================================

-- Drop indexes first
DROP INDEX IF EXISTS "IDX_PaymentTransactions_FlowType";
DROP INDEX IF EXISTS "IDX_PaymentTransactions_CreatedDate";
DROP INDEX IF EXISTS "IDX_PaymentTransactions_UserId";
DROP INDEX IF EXISTS "IDX_PaymentTransactions_Status";
DROP INDEX IF EXISTS "IDX_PaymentTransactions_ConversationId";
DROP INDEX IF EXISTS "IDX_PaymentTransactions_IyzicoToken";

-- Drop the table (foreign key constraints will be dropped automatically)
DROP TABLE IF EXISTS "PaymentTransactions" CASCADE;

-- Verification query
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_name = 'PaymentTransactions'
) AS table_exists;

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Rollback 001 completed successfully';
    RAISE NOTICE 'PaymentTransactions table and all related indexes have been removed';
END $$;
