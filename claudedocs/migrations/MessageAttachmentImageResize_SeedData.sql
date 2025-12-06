-- =============================================
-- Message Attachment Image Resize Configuration Seed Data
-- =============================================
-- Description: Adds image resize configuration for message attachments
-- Date: 2025-12-04
-- Purpose: Enable automatic image optimization for message attachments (1 MB target)
-- =============================================

-- Check if Configuration table exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Configuration') THEN
        RAISE EXCEPTION 'Configuration table does not exist. Please ensure the table is created first.';
    END IF;
END $$;

-- Insert Message Attachment Image Resize configuration entries

-- 1. Enable/disable automatic image resize for message attachments
INSERT INTO "Configuration"
("Key", "Value", "Description", "Category", "ValueType", "IsActive", "CreatedDate")
SELECT
    'MESSAGING_ATTACHMENT_IMAGE_ENABLE_RESIZE',
    'true',
    'Enable or disable automatic image resizing for message attachments. When enabled, images will be optimized to target size.',
    'Messaging',
    'bool',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Configuration" WHERE "Key" = 'MESSAGING_ATTACHMENT_IMAGE_ENABLE_RESIZE'
);

-- 2. Maximum image size in MB (target size for optimization)
INSERT INTO "Configuration"
("Key", "Value", "Description", "Category", "ValueType", "IsActive", "CreatedDate")
SELECT
    'MESSAGING_ATTACHMENT_IMAGE_MAX_SIZE_MB',
    '1.0',
    'Target maximum size in MB for message attachment images. Images will be compressed to meet this target (default: 1 MB).',
    'Messaging',
    'decimal',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Configuration" WHERE "Key" = 'MESSAGING_ATTACHMENT_IMAGE_MAX_SIZE_MB'
);

-- 3. Maximum image width
INSERT INTO "Configuration"
("Key", "Value", "Description", "Category", "ValueType", "IsActive", "CreatedDate")
SELECT
    'MESSAGING_ATTACHMENT_IMAGE_MAX_WIDTH',
    '1920',
    'Maximum width in pixels for message attachment images. Images exceeding this will be resized proportionally.',
    'Messaging',
    'int',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Configuration" WHERE "Key" = 'MESSAGING_ATTACHMENT_IMAGE_MAX_WIDTH'
);

-- 4. Maximum image height
INSERT INTO "Configuration"
("Key", "Value", "Description", "Category", "ValueType", "IsActive", "CreatedDate")
SELECT
    'MESSAGING_ATTACHMENT_IMAGE_MAX_HEIGHT',
    '1080',
    'Maximum height in pixels for message attachment images. Images exceeding this will be resized proportionally.',
    'Messaging',
    'int',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Configuration" WHERE "Key" = 'MESSAGING_ATTACHMENT_IMAGE_MAX_HEIGHT'
);

-- Verify insertion
DO $$
DECLARE
    v_count int;
    v_total_messaging_count int;
BEGIN
    -- Count new entries
    SELECT COUNT(*) INTO v_count
    FROM "Configuration"
    WHERE "Key" IN (
        'MESSAGING_ATTACHMENT_IMAGE_ENABLE_RESIZE',
        'MESSAGING_ATTACHMENT_IMAGE_MAX_SIZE_MB',
        'MESSAGING_ATTACHMENT_IMAGE_MAX_WIDTH',
        'MESSAGING_ATTACHMENT_IMAGE_MAX_HEIGHT'
    );

    -- Count all messaging entries
    SELECT COUNT(*) INTO v_total_messaging_count
    FROM "Configuration"
    WHERE "Category" = 'Messaging' AND "IsActive" = true;

    IF v_count < 4 THEN
        RAISE WARNING 'Expected 4 message attachment image resize configuration entries, but found only %', v_count;
    ELSE
        RAISE NOTICE '✅ Successfully inserted % message attachment image resize configuration entries', v_count;
    END IF;

    RAISE NOTICE 'Total active Messaging configuration entries: %', v_total_messaging_count;
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
WHERE "Key" IN (
    'MESSAGING_ATTACHMENT_IMAGE_ENABLE_RESIZE',
    'MESSAGING_ATTACHMENT_IMAGE_MAX_SIZE_MB',
    'MESSAGING_ATTACHMENT_IMAGE_MAX_WIDTH',
    'MESSAGING_ATTACHMENT_IMAGE_MAX_HEIGHT'
)
ORDER BY "Key";

-- Display all Messaging category configurations for reference
RAISE NOTICE 'All Messaging configurations:';
SELECT
    "Key",
    "Value",
    "ValueType",
    "IsActive"
FROM "Configuration"
WHERE "Category" = 'Messaging'
ORDER BY "Key";
