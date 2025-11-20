-- =============================================
-- Messaging Configuration Seed Data
-- =============================================
-- Description: Adds messaging-related configuration entries to Configuration table
-- Date: 2025-10-22
-- Purpose: Enable configurable message rate limiting for sponsor-farmer messaging
-- =============================================

-- Check if Configuration table exists
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Configuration') THEN
        RAISE EXCEPTION 'Configuration table does not exist. Please ensure the table is created first.';
    END IF;
END $$;

-- Insert Messaging configuration entries
-- Daily message limit per farmer for sponsors (L/XL tier)
INSERT INTO "Configuration" 
("Key", "Value", "Description", "Category", "ValueType", "IsActive", "CreatedDate")
SELECT 
    'MESSAGING_DAILY_LIMIT_PER_FARMER',
    '10',
    'Daily message limit per farmer for sponsors (L/XL tier). Controls how many messages a sponsor can send to a single farmer per day.',
    'Messaging',
    'int',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Configuration" WHERE "Key" = 'MESSAGING_DAILY_LIMIT_PER_FARMER'
);

-- Enable/disable rate limiting feature
INSERT INTO "Configuration" 
("Key", "Value", "Description", "Category", "ValueType", "IsActive", "CreatedDate")
SELECT 
    'MESSAGING_ENABLE_RATE_LIMIT',
    'true',
    'Enable or disable rate limiting for sponsor messages. When disabled, no daily limit will be enforced.',
    'Messaging',
    'bool',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Configuration" WHERE "Key" = 'MESSAGING_ENABLE_RATE_LIMIT'
);

-- Verify insertion
DO $$ 
DECLARE
    v_count int;
BEGIN
    SELECT COUNT(*) INTO v_count 
    FROM "Configuration" 
    WHERE "Category" = 'Messaging' AND "IsActive" = true;
    
    IF v_count < 2 THEN
        RAISE WARNING 'Expected 2 Messaging configuration entries, but found only %', v_count;
    ELSE
        RAISE NOTICE 'Successfully inserted % Messaging configuration entries', v_count;
    END IF;
END $$;

-- Display inserted configurations
SELECT 
    "Id",
    "Key",
    "Value",
    "Description",
    "Category",
    "ValueType",
    "IsActive",
    "CreatedDate"
FROM "Configuration"
WHERE "Category" = 'Messaging'
ORDER BY "Key";
