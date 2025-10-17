-- =====================================================
-- Sponsorship Queue System Migration
-- Date: 2025-10-07
-- Description: Adds queue system for sponsorships and sponsor attribution tracking
-- =====================================================

-- =====================================================
-- STEP 1: Add UserSubscription Queue Fields
-- =====================================================

-- Add QueueStatus column (enum: 0=Pending, 1=Active, 2=Expired, 3=Cancelled)
ALTER TABLE "UserSubscriptions" 
ADD COLUMN "QueueStatus" INTEGER NOT NULL DEFAULT 1;

-- Add QueuedDate (when sponsorship code was redeemed if queued)
ALTER TABLE "UserSubscriptions" 
ADD COLUMN "QueuedDate" TIMESTAMP NULL;

-- Add ActivatedDate (when subscription actually became active)
ALTER TABLE "UserSubscriptions" 
ADD COLUMN "ActivatedDate" TIMESTAMP NULL;

-- Add PreviousSponsorshipId (FK to sponsorship this is waiting for)
ALTER TABLE "UserSubscriptions" 
ADD COLUMN "PreviousSponsorshipId" INTEGER NULL;

-- =====================================================
-- STEP 2: Set ActivatedDate for Existing Records
-- =====================================================

-- For existing active/sponsored subscriptions, set ActivatedDate = StartDate
UPDATE "UserSubscriptions" 
SET "ActivatedDate" = "StartDate"
WHERE "IsSponsoredSubscription" = true 
  AND "ActivatedDate" IS NULL;

-- Set QueueStatus based on current status
UPDATE "UserSubscriptions" 
SET "QueueStatus" = CASE 
    WHEN "EndDate" < NOW() THEN 2  -- Expired
    WHEN "Status" = 'Cancelled' THEN 3  -- Cancelled
    ELSE 1  -- Active (default for existing records)
END
WHERE "IsSponsoredSubscription" = true;

-- =====================================================
-- STEP 3: Add Foreign Key Constraint
-- =====================================================

ALTER TABLE "UserSubscriptions"
ADD CONSTRAINT "FK_UserSubscriptions_PreviousSponsorship"
FOREIGN KEY ("PreviousSponsorshipId") 
REFERENCES "UserSubscriptions"("Id")
ON DELETE SET NULL;

-- =====================================================
-- STEP 4: Add Indexes for Queue Queries
-- =====================================================

-- Index for queue status queries
CREATE INDEX "IX_UserSubscriptions_QueueStatus" 
ON "UserSubscriptions"("QueueStatus");

-- Composite index for queue lookup (most common query)
CREATE INDEX "IX_UserSubscriptions_Queue_Lookup" 
ON "UserSubscriptions"("QueueStatus", "PreviousSponsorshipId")
WHERE "PreviousSponsorshipId" IS NOT NULL;

-- Index for sponsored subscription queries
CREATE INDEX "IX_UserSubscriptions_Sponsored_Active" 
ON "UserSubscriptions"("UserId", "IsSponsoredSubscription", "QueueStatus", "IsActive")
WHERE "IsSponsoredSubscription" = true;

-- =====================================================
-- STEP 5: Add PlantAnalysis Sponsor Attribution Fields
-- =====================================================

-- Add ActiveSponsorshipId (FK to UserSubscription that was active during analysis)
ALTER TABLE "PlantAnalyses"
ADD COLUMN "ActiveSponsorshipId" INTEGER NULL;

-- Add SponsorCompanyId (denormalized sponsor company ID for performance)
ALTER TABLE "PlantAnalyses"
ADD COLUMN "SponsorCompanyId" INTEGER NULL;

-- =====================================================
-- STEP 6: Add Foreign Key Constraints for PlantAnalysis
-- =====================================================

ALTER TABLE "PlantAnalyses"
ADD CONSTRAINT "FK_PlantAnalyses_ActiveSponsorship"
FOREIGN KEY ("ActiveSponsorshipId")
REFERENCES "UserSubscriptions"("Id")
ON DELETE SET NULL;

ALTER TABLE "PlantAnalyses"
ADD CONSTRAINT "FK_PlantAnalyses_SponsorCompany"
FOREIGN KEY ("SponsorCompanyId")
REFERENCES "Users"("UserId")
ON DELETE SET NULL;

-- =====================================================
-- STEP 7: Add Indexes for PlantAnalysis Sponsor Queries
-- =====================================================

-- Composite index for sponsor filtering (user + sponsor company)
CREATE INDEX "IX_PlantAnalyses_UserSponsor" 
ON "PlantAnalyses"("UserId", "SponsorCompanyId")
WHERE "SponsorCompanyId" IS NOT NULL;

-- Index for sponsor company analysis lookup
CREATE INDEX "IX_PlantAnalyses_SponsorCompany" 
ON "PlantAnalyses"("SponsorCompanyId")
WHERE "SponsorCompanyId" IS NOT NULL;

-- Index for active sponsorship lookup
CREATE INDEX "IX_PlantAnalyses_ActiveSponsorship" 
ON "PlantAnalyses"("ActiveSponsorshipId")
WHERE "ActiveSponsorshipId" IS NOT NULL;

-- =====================================================
-- STEP 8: Add Comments for Documentation
-- =====================================================

COMMENT ON COLUMN "UserSubscriptions"."QueueStatus" IS 'Queue status: 0=Pending, 1=Active, 2=Expired, 3=Cancelled';
COMMENT ON COLUMN "UserSubscriptions"."QueuedDate" IS 'When sponsorship code was redeemed (if queued)';
COMMENT ON COLUMN "UserSubscriptions"."ActivatedDate" IS 'When subscription actually became active';
COMMENT ON COLUMN "UserSubscriptions"."PreviousSponsorshipId" IS 'FK to sponsorship this is waiting for (queue system)';

COMMENT ON COLUMN "PlantAnalyses"."ActiveSponsorshipId" IS 'FK to UserSubscription that was active during analysis (immutable)';
COMMENT ON COLUMN "PlantAnalyses"."SponsorCompanyId" IS 'Denormalized sponsor company ID for fast logo/access queries';

-- =====================================================
-- VERIFICATION QUERIES (Optional - for testing)
-- =====================================================

-- Verify UserSubscriptions columns
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'UserSubscriptions' 
  AND column_name IN ('QueueStatus', 'QueuedDate', 'ActivatedDate', 'PreviousSponsorshipId')
ORDER BY column_name;

-- Verify PlantAnalyses columns
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'PlantAnalyses' 
  AND column_name IN ('ActiveSponsorshipId', 'SponsorCompanyId')
ORDER BY column_name;

-- Verify indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename IN ('UserSubscriptions', 'PlantAnalyses')
  AND indexname LIKE '%Queue%' OR indexname LIKE '%Sponsor%'
ORDER BY tablename, indexname;

-- =====================================================
-- ROLLBACK SCRIPT (If needed)
-- =====================================================

/*
-- Drop PlantAnalyses constraints and columns
ALTER TABLE "PlantAnalyses" DROP CONSTRAINT IF EXISTS "FK_PlantAnalyses_SponsorCompany";
ALTER TABLE "PlantAnalyses" DROP CONSTRAINT IF EXISTS "FK_PlantAnalyses_ActiveSponsorship";
DROP INDEX IF EXISTS "IX_PlantAnalyses_ActiveSponsorship";
DROP INDEX IF EXISTS "IX_PlantAnalyses_SponsorCompany";
DROP INDEX IF EXISTS "IX_PlantAnalyses_UserSponsor";
ALTER TABLE "PlantAnalyses" DROP COLUMN IF EXISTS "SponsorCompanyId";
ALTER TABLE "PlantAnalyses" DROP COLUMN IF EXISTS "ActiveSponsorshipId";

-- Drop UserSubscriptions constraints and columns
ALTER TABLE "UserSubscriptions" DROP CONSTRAINT IF EXISTS "FK_UserSubscriptions_PreviousSponsorship";
DROP INDEX IF EXISTS "IX_UserSubscriptions_Sponsored_Active";
DROP INDEX IF EXISTS "IX_UserSubscriptions_Queue_Lookup";
DROP INDEX IF EXISTS "IX_UserSubscriptions_QueueStatus";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "PreviousSponsorshipId";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "ActivatedDate";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "QueuedDate";
ALTER TABLE "UserSubscriptions" DROP COLUMN IF EXISTS "QueueStatus";
*/

-- =====================================================
-- END OF MIGRATION
-- =====================================================
