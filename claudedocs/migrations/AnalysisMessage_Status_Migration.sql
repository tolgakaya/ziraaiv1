-- =====================================================
-- AnalysisMessage Status Fields Migration (Phase 1B)
-- Purpose: Add message status tracking (Sent/Delivered/Read)
-- Date: 2025-10-19
-- Author: ZiraAI Backend Team
-- =====================================================

-- Add message status columns to AnalysisMessages table
ALTER TABLE "AnalysisMessages"
ADD COLUMN IF NOT EXISTS "MessageStatus" VARCHAR(20) DEFAULT 'Sent' NULL,
ADD COLUMN IF NOT EXISTS "DeliveredDate" TIMESTAMP NULL;

-- Update existing messages to have default status
UPDATE "AnalysisMessages"
SET "MessageStatus" = CASE
    WHEN "IsRead" = true THEN 'Read'
    ELSE 'Sent'
END
WHERE "MessageStatus" IS NULL;

-- Create index for status queries
CREATE INDEX IF NOT EXISTS "idx_analysis_messages_status" ON "AnalysisMessages"("MessageStatus");
CREATE INDEX IF NOT EXISTS "idx_analysis_messages_delivered_date" ON "AnalysisMessages"("DeliveredDate");

-- Add comments for documentation
COMMENT ON COLUMN "AnalysisMessages"."MessageStatus" IS 'Message delivery status: Sent, Delivered, Read';
COMMENT ON COLUMN "AnalysisMessages"."DeliveredDate" IS 'Timestamp when message was delivered to recipient';

-- Success message
DO $$
BEGIN
    RAISE NOTICE '✅ AnalysisMessage status columns added successfully (Phase 1B)';
END $$;
