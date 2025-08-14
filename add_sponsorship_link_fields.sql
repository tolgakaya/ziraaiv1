-- Add Sponsorship Link Distribution Fields to SponsorshipCodes table
-- Generated for staging database: ziraai_dev

BEGIN;

-- Add RedemptionLink column
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "RedemptionLink" VARCHAR(500);

-- Add LinkClickDate column
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "LinkClickDate" TIMESTAMP WITHOUT TIME ZONE;

-- Add LinkClickCount column with default value
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "LinkClickCount" INTEGER DEFAULT 0 NOT NULL;

-- Add RecipientPhone column
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "RecipientPhone" VARCHAR(20);

-- Add RecipientName column
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "RecipientName" VARCHAR(100);

-- Add LinkSentDate column
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "LinkSentDate" TIMESTAMP WITHOUT TIME ZONE;

-- Add LinkSentVia column
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "LinkSentVia" VARCHAR(20);

-- Add LinkDelivered column with default value
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "LinkDelivered" BOOLEAN DEFAULT FALSE NOT NULL;

-- Add LastClickIpAddress column
ALTER TABLE "SponsorshipCodes" 
ADD COLUMN IF NOT EXISTS "LastClickIpAddress" VARCHAR(45);

-- Insert migration record if not exists
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20250814160711_AddSponsorshipLinkFields', '9.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20250814160711_AddSponsorshipLinkFields'
);

COMMIT;

-- Verify the changes
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'SponsorshipCodes' 
    AND column_name IN (
        'RedemptionLink', 
        'LinkClickDate', 
        'LinkClickCount', 
        'RecipientPhone',
        'RecipientName',
        'LinkSentDate',
        'LinkSentVia',
        'LinkDelivered',
        'LastClickIpAddress'
    )
ORDER BY ordinal_position;