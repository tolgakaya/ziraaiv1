-- =====================================================
-- Manual Migration: Create AppInfos Table
-- =====================================================
-- Purpose: Application information for About Us page
-- Single record table - only one active record at a time
-- Date: 2025-11-18
-- Related: AppInfo entity, AppInfoDto, AdminAppInfoDto
-- =====================================================

CREATE TABLE IF NOT EXISTS "AppInfos" (
    "Id" SERIAL PRIMARY KEY,

    -- Company Info
    "CompanyName" VARCHAR(200) NULL,
    "CompanyDescription" VARCHAR(2000) NULL,
    "AppVersion" VARCHAR(50) NULL,

    -- Address
    "Address" VARCHAR(500) NULL,

    -- Contact Info
    "Email" VARCHAR(200) NULL,
    "Phone" VARCHAR(50) NULL,
    "WebsiteUrl" VARCHAR(500) NULL,

    -- Social Media Links
    "FacebookUrl" VARCHAR(500) NULL,
    "InstagramUrl" VARCHAR(500) NULL,
    "YouTubeUrl" VARCHAR(500) NULL,
    "TwitterUrl" VARCHAR(500) NULL,
    "LinkedInUrl" VARCHAR(500) NULL,

    -- Legal Pages URLs
    "TermsOfServiceUrl" VARCHAR(500) NULL,
    "PrivacyPolicyUrl" VARCHAR(500) NULL,
    "CookiePolicyUrl" VARCHAR(500) NULL,

    -- Metadata
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedDate" TIMESTAMP NOT NULL,
    "UpdatedDate" TIMESTAMP NOT NULL,
    "UpdatedByUserId" INTEGER NULL
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_AppInfos_IsActive"
    ON "AppInfos"("IsActive");

-- =====================================================
-- Initial Data (Optional - uncomment and customize)
-- =====================================================

-- INSERT INTO "AppInfos" (
--     "CompanyName",
--     "CompanyDescription",
--     "AppVersion",
--     "Address",
--     "Email",
--     "Phone",
--     "WebsiteUrl",
--     "FacebookUrl",
--     "InstagramUrl",
--     "YouTubeUrl",
--     "TwitterUrl",
--     "LinkedInUrl",
--     "TermsOfServiceUrl",
--     "PrivacyPolicyUrl",
--     "CookiePolicyUrl",
--     "IsActive",
--     "CreatedDate",
--     "UpdatedDate",
--     "UpdatedByUserId"
-- ) VALUES (
--     'ZiraAI',
--     'ZiraAI, yapay zeka destekli bitki analizi hizmeti sunan yenilikçi bir tarım teknolojisi şirketidir.',
--     '1.0.0',
--     'İstanbul, Türkiye',
--     'destek@ziraai.com',
--     '+90 (212) 555 0000',
--     'https://www.ziraai.com',
--     'https://www.facebook.com/ziraai',
--     'https://www.instagram.com/ziraai',
--     'https://www.youtube.com/@ziraai',
--     'https://www.twitter.com/ziraai',
--     'https://www.linkedin.com/company/ziraai',
--     'https://www.ziraai.com/terms',
--     'https://www.ziraai.com/privacy',
--     'https://www.ziraai.com/cookies',
--     true,
--     NOW(),
--     NOW(),
--     1  -- Admin user ID
-- );

-- =====================================================
-- Verification Queries
-- =====================================================

-- Verify table created
SELECT
    table_name,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_name = 'AppInfos'
ORDER BY ordinal_position;

-- Verify indexes created
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'AppInfos';

-- =====================================================
-- Rollback (if needed)
-- =====================================================
-- DROP TABLE IF EXISTS "AppInfos";
