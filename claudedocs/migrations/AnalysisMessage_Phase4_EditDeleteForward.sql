-- =====================================================
-- AnalysisMessage Edit/Delete/Forward (Phase 4)
-- Purpose: Add message editing and forwarding support
-- Date: 2025-10-19
-- =====================================================

ALTER TABLE "AnalysisMessages"
ADD COLUMN IF NOT EXISTS "IsEdited" BOOLEAN DEFAULT false NOT NULL,
ADD COLUMN IF NOT EXISTS "EditedDate" TIMESTAMP NULL,
ADD COLUMN IF NOT EXISTS "OriginalMessage" TEXT NULL,
ADD COLUMN IF NOT EXISTS "ForwardedFromMessageId" INT NULL,
ADD COLUMN IF NOT EXISTS "IsForwarded" BOOLEAN DEFAULT false NOT NULL;

CREATE INDEX IF NOT EXISTS "idx_analysis_messages_edited" ON "AnalysisMessages"("IsEdited") WHERE "IsEdited" = true;
CREATE INDEX IF NOT EXISTS "idx_analysis_messages_forwarded" ON "AnalysisMessages"("ForwardedFromMessageId") WHERE "ForwardedFromMessageId" IS NOT NULL;

COMMENT ON COLUMN "AnalysisMessages"."IsEdited" IS 'Message has been edited';
COMMENT ON COLUMN "AnalysisMessages"."EditedDate" IS 'When message was last edited';
COMMENT ON COLUMN "AnalysisMessages"."OriginalMessage" IS 'Original message content before first edit';
COMMENT ON COLUMN "AnalysisMessages"."ForwardedFromMessageId" IS 'ID of original message if forwarded';
COMMENT ON COLUMN "AnalysisMessages"."IsForwarded" IS 'Message is a forwarded copy';

DO $$
BEGIN
    RAISE NOTICE '✅ Edit/Delete/Forward columns added (Phase 4)';
END $$;
