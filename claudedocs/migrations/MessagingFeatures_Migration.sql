-- =====================================================
-- Messaging Features Table Migration
-- Purpose: Admin-controlled feature flags for messaging system
-- Date: 2025-10-19
-- Author: ZiraAI Backend Team
-- =====================================================

-- Create MessagingFeatures table
CREATE TABLE IF NOT EXISTS "MessagingFeatures" (
    "Id" SERIAL PRIMARY KEY,
    "FeatureName" VARCHAR(100) UNIQUE NOT NULL,
    "DisplayName" VARCHAR(200),
    "IsEnabled" BOOLEAN DEFAULT true NOT NULL,
    "RequiredTier" VARCHAR(20) DEFAULT 'None' NOT NULL,
    "MaxFileSize" BIGINT NULL,
    "MaxDuration" INT NULL,
    "AllowedMimeTypes" VARCHAR(1000) NULL,
    "TimeLimit" INT NULL,
    "Description" VARCHAR(500) NULL,
    "ConfigurationJson" TEXT NULL,
    "CreatedDate" TIMESTAMP DEFAULT NOW() NOT NULL,
    "UpdatedDate" TIMESTAMP NULL,
    "CreatedByUserId" INT NULL,
    "UpdatedByUserId" INT NULL,
    CONSTRAINT "FK_MessagingFeatures_CreatedBy" FOREIGN KEY ("CreatedByUserId")
        REFERENCES "Users"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_MessagingFeatures_UpdatedBy" FOREIGN KEY ("UpdatedByUserId")
        REFERENCES "Users"("Id") ON DELETE SET NULL
);

-- Create indexes for performance
CREATE INDEX "idx_messaging_features_name" ON "MessagingFeatures"("FeatureName");
CREATE INDEX "idx_messaging_features_enabled" ON "MessagingFeatures"("IsEnabled");
CREATE INDEX "idx_messaging_features_tier" ON "MessagingFeatures"("RequiredTier");

-- Add comments for documentation
COMMENT ON TABLE "MessagingFeatures" IS 'Feature flags for messaging system with tier-based access control';
COMMENT ON COLUMN "MessagingFeatures"."FeatureName" IS 'Unique identifier (e.g., VoiceMessages, ImageAttachments)';
COMMENT ON COLUMN "MessagingFeatures"."IsEnabled" IS 'Admin toggle - global on/off switch';
COMMENT ON COLUMN "MessagingFeatures"."RequiredTier" IS 'Minimum subscription tier (None, S, M, L, XL)';
COMMENT ON COLUMN "MessagingFeatures"."MaxFileSize" IS 'Maximum file size in bytes for attachments';
COMMENT ON COLUMN "MessagingFeatures"."MaxDuration" IS 'Maximum duration in seconds for voice/video';
COMMENT ON COLUMN "MessagingFeatures"."AllowedMimeTypes" IS 'Comma-separated allowed MIME types';
COMMENT ON COLUMN "MessagingFeatures"."TimeLimit" IS 'Time limit in seconds for actions (edit/delete)';

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'MessagingFeatures table created successfully';
END $$;
