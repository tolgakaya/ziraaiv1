-- =====================================================
-- User Avatar Fields Migration
-- Purpose: Add avatar support to Users table
-- Date: 2025-10-19
-- Author: ZiraAI Backend Team
-- =====================================================

-- Add avatar columns to Users table
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "AvatarUrl" VARCHAR(500) NULL,
ADD COLUMN IF NOT EXISTS "AvatarThumbnailUrl" VARCHAR(500) NULL,
ADD COLUMN IF NOT EXISTS "AvatarUpdatedDate" TIMESTAMP NULL;

-- Create index for avatar queries
CREATE INDEX IF NOT EXISTS "idx_users_avatar_updated" ON "Users"("AvatarUpdatedDate");

-- Add comments for documentation
COMMENT ON COLUMN "Users"."AvatarUrl" IS 'User profile avatar URL (full size, 512x512)';
COMMENT ON COLUMN "Users"."AvatarThumbnailUrl" IS 'User profile avatar thumbnail URL (optimized, 128x128)';
COMMENT ON COLUMN "Users"."AvatarUpdatedDate" IS 'Timestamp when avatar was last updated';

-- Success message
DO $$
BEGIN
    RAISE NOTICE '✅ User avatar columns added successfully';
END $$;
