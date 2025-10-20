-- =====================================================
-- Messaging Features Seed Data
-- Purpose: Initialize 9 messaging features with default configurations
-- Date: 2025-10-19
-- Author: ZiraAI Backend Team
-- =====================================================

-- Insert messaging features
INSERT INTO "MessagingFeatures" (
    "FeatureName",
    "DisplayName",
    "IsEnabled",
    "RequiredTier",
    "MaxFileSize",
    "MaxDuration",
    "AllowedMimeTypes",
    "TimeLimit",
    "Description"
) VALUES
-- 1. Voice Messages (XL tier only, premium feature)
(
    'VoiceMessages',
    'Voice Messages',
    true,
    'XL',
    5242880,  -- 5MB
    60,       -- 60 seconds
    'audio/m4a,audio/aac,audio/mp3,audio/mpeg',
    NULL,
    'Send voice messages to farmers (premium feature for XL tier sponsors)'
),

-- 2. Image Attachments (L tier and above)
(
    'ImageAttachments',
    'Image Attachments',
    true,
    'L',
    10485760, -- 10MB
    NULL,
    'image/jpeg,image/jpg,image/png,image/webp,image/heic',
    NULL,
    'Send image attachments in messages'
),

-- 3. Video Attachments (XL tier only, large file support)
(
    'VideoAttachments',
    'Video Attachments',
    true,
    'XL',
    52428800, -- 50MB
    60,       -- 60 seconds
    'video/mp4,video/mov,video/avi',
    NULL,
    'Send video attachments (premium feature for XL tier)'
),

-- 4. File Attachments (L tier and above, documents)
(
    'FileAttachments',
    'File Attachments',
    true,
    'L',
    5242880,  -- 5MB
    NULL,
    'application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,text/plain',
    NULL,
    'Send document files (PDF, Word, TXT)'
),

-- 5. Message Edit (L tier and above, 1 hour limit)
(
    'MessageEdit',
    'Edit Messages',
    false,    -- Initially disabled, enable after testing
    'L',
    NULL,
    NULL,
    NULL,
    3600,     -- 1 hour (3600 seconds)
    'Edit sent messages within 1 hour'
),

-- 6. Message Delete (All tiers, 24 hour limit)
(
    'MessageDelete',
    'Delete Messages',
    true,
    'None',
    NULL,
    NULL,
    NULL,
    86400,    -- 24 hours (86400 seconds)
    'Delete sent messages within 24 hours'
),

-- 7. Message Forward (L tier and above)
(
    'MessageForward',
    'Forward Messages',
    false,    -- Initially disabled to prevent spam
    'L',
    NULL,
    NULL,
    NULL,
    NULL,
    'Forward messages to other conversations'
),

-- 8. Typing Indicator (All tiers, basic feature)
(
    'TypingIndicator',
    'Typing Indicator',
    true,
    'None',
    NULL,
    NULL,
    NULL,
    NULL,
    'Show typing indicator in real-time'
),

-- 9. Link Preview (All tiers, initially disabled)
(
    'LinkPreview',
    'Link Previews',
    false,    -- Initially disabled, enable after implementation
    'None',
    NULL,
    NULL,
    NULL,
    NULL,
    'Generate previews for URLs in messages'
)
ON CONFLICT ("FeatureName") DO NOTHING;

-- Verify insertion
DO $$
DECLARE
    feature_count INT;
BEGIN
    SELECT COUNT(*) INTO feature_count FROM "MessagingFeatures";
    RAISE NOTICE 'Total messaging features: %', feature_count;

    IF feature_count >= 9 THEN
        RAISE NOTICE '✅ Seed data inserted successfully';
    ELSE
        RAISE WARNING '⚠️ Expected 9 features, found %', feature_count;
    END IF;
END $$;
