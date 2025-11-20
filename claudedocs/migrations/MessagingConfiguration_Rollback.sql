-- =============================================
-- Messaging Configuration Rollback
-- =============================================
-- Description: Removes messaging configuration entries
-- Date: 2025-10-22
-- Purpose: Rollback messaging configuration seed data
-- =============================================

-- Soft delete (set IsActive = false) instead of hard delete to preserve history
UPDATE "Configuration"
SET 
    "IsActive" = false,
    "UpdatedDate" = NOW()
WHERE "Category" = 'Messaging'
AND "Key" IN ('MESSAGING_DAILY_LIMIT_PER_FARMER', 'MESSAGING_ENABLE_RATE_LIMIT');

-- Display rollback results
SELECT 
    "Id",
    "Key",
    "Value",
    "Category",
    "IsActive",
    "UpdatedDate"
FROM "Configuration"
WHERE "Category" = 'Messaging'
ORDER BY "Key";

-- Confirmation message
DO $$ 
BEGIN
    RAISE NOTICE 'Messaging configuration entries have been deactivated (soft delete)';
END $$;
