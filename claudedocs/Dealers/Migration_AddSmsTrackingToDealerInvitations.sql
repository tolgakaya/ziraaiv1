-- =====================================================
-- Migration: Add SMS Tracking Fields to DealerInvitations
-- Date: 2025-01-25
-- Description: Adds LinkSentDate, LinkSentVia, and LinkDelivered columns
--              to support SMS-based dealer invitation tracking
-- =====================================================

-- STEP 1: Add new columns
ALTER TABLE public."DealerInvitations"
ADD COLUMN "LinkSentDate" timestamp NULL;

ALTER TABLE public."DealerInvitations"
ADD COLUMN "LinkSentVia" varchar(50) NULL;

ALTER TABLE public."DealerInvitations"
ADD COLUMN "LinkDelivered" boolean NOT NULL DEFAULT false;

-- STEP 2: Add comments for documentation
COMMENT ON COLUMN public."DealerInvitations"."LinkSentDate" IS 'When the SMS/email link was sent to the dealer';
COMMENT ON COLUMN public."DealerInvitations"."LinkSentVia" IS 'Communication channel: SMS, WhatsApp, Email, etc.';
COMMENT ON COLUMN public."DealerInvitations"."LinkDelivered" IS 'Whether the message was successfully delivered';

-- STEP 3: Create index for LinkSentDate (optional, for performance on queries filtering by send date)
CREATE INDEX "IX_DealerInvitations_LinkSentDate" ON public."DealerInvitations" USING btree ("LinkSentDate");

-- =====================================================
-- Verification Query
-- =====================================================
-- Run this to verify the migration was successful:
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns
-- WHERE table_name = 'DealerInvitations'
-- AND column_name IN ('LinkSentDate', 'LinkSentVia', 'LinkDelivered')
-- ORDER BY ordinal_position;
