-- =====================================================
-- AnalysisMessage Voice Messages (Phase 2B)
-- Purpose: Add voice message support fields
-- Date: 2025-10-19
-- =====================================================

ALTER TABLE "AnalysisMessages"
ADD COLUMN IF NOT EXISTS "VoiceMessageUrl" VARCHAR(500) NULL,
ADD COLUMN IF NOT EXISTS "VoiceMessageDuration" INT NULL,
ADD COLUMN IF NOT EXISTS "VoiceMessageWaveform" TEXT NULL;

CREATE INDEX IF NOT EXISTS "idx_analysis_messages_voice" ON "AnalysisMessages"("VoiceMessageUrl") WHERE "VoiceMessageUrl" IS NOT NULL;

COMMENT ON COLUMN "AnalysisMessages"."VoiceMessageUrl" IS 'URL to voice message audio file';
COMMENT ON COLUMN "AnalysisMessages"."VoiceMessageDuration" IS 'Voice message duration in seconds';
COMMENT ON COLUMN "AnalysisMessages"."VoiceMessageWaveform" IS 'JSON array of waveform visualization data';

DO $$
BEGIN
    RAISE NOTICE '✅ Voice message columns added (Phase 2B)';
END $$;
