-- =====================================================
-- AnalysisMessage Attachments Migration (Phase 2A)
-- Purpose: Add attachment metadata fields
-- Date: 2025-10-19
-- Author: ZiraAI Backend Team
-- =====================================================

-- Add attachment metadata columns
ALTER TABLE "AnalysisMessages"
ADD COLUMN IF NOT EXISTS "AttachmentTypes" TEXT NULL,
ADD COLUMN IF NOT EXISTS "AttachmentSizes" TEXT NULL,
ADD COLUMN IF NOT EXISTS "AttachmentNames" TEXT NULL,
ADD COLUMN IF NOT EXISTS "AttachmentCount" INT DEFAULT 0 NOT NULL;

-- Create indexes for attachment queries
CREATE INDEX IF NOT EXISTS "idx_analysis_messages_has_attachments" ON "AnalysisMessages"("HasAttachments");
CREATE INDEX IF NOT EXISTS "idx_analysis_messages_attachment_count" ON "AnalysisMessages"("AttachmentCount");

-- Add comments
COMMENT ON COLUMN "AnalysisMessages"."AttachmentTypes" IS 'JSON array of MIME types (image/jpeg, application/pdf, etc.)';
COMMENT ON COLUMN "AnalysisMessages"."AttachmentSizes" IS 'JSON array of file sizes in bytes';
COMMENT ON COLUMN "AnalysisMessages"."AttachmentNames" IS 'JSON array of original filenames';
COMMENT ON COLUMN "AnalysisMessages"."AttachmentCount" IS 'Total number of attachments in message';

-- Success message
DO $$
BEGIN
    RAISE NOTICE '✅ AnalysisMessage attachment metadata columns added (Phase 2A)';
END $$;
