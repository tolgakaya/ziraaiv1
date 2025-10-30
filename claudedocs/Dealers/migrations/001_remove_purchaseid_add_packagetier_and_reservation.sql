-- ========================================================================
-- Migration: Remove PurchaseId Requirement, Add PackageTier and Reservation
-- ========================================================================
-- Date: 2025-10-30
-- Description: Implement intelligent code selection by removing purchaseId 
--              requirement and adding packageTier filter with code reservation
-- Related: BACKEND_CHANGE_REQUEST_DEALER_INVITATION_PURCHASEID_REMOVAL.md
-- ========================================================================

-- ========================================================================
-- PART 1: DealerInvitations Table Changes
-- ========================================================================

-- Step 1: Make PurchaseId nullable (allow dealer invitations without specific purchase)
ALTER TABLE "DealerInvitations"
ALTER COLUMN "PurchaseId" DROP NOT NULL;

COMMENT ON COLUMN "DealerInvitations"."PurchaseId" IS 
'[DEPRECATED] Purchase ID - will be removed in future. Use PackageTier instead for filtering.';

-- Step 2: Add PackageTier column for tier-based filtering (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'DealerInvitations' AND column_name = 'PackageTier'
    ) THEN
        ALTER TABLE "DealerInvitations"
        ADD COLUMN "PackageTier" VARCHAR(10) NULL;
        
        RAISE NOTICE 'Column PackageTier added to DealerInvitations';
    ELSE
        RAISE NOTICE 'Column PackageTier already exists in DealerInvitations, skipping';
    END IF;
END $$;

COMMENT ON COLUMN "DealerInvitations"."PackageTier" IS 
'Optional tier filter for code selection: S, M, L, XL. If null, codes from any tier can be selected automatically.';
-- ========================================================================
-- PART 2: SponsorshipCodes Table Changes (Reservation System)
-- ========================================================================

-- Step 3: Add code reservation fields to prevent double-allocation (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SponsorshipCodes' AND column_name = 'ReservedForInvitationId'
    ) THEN
        ALTER TABLE "SponsorshipCodes"
        ADD COLUMN "ReservedForInvitationId" INT4 NULL;
        
        RAISE NOTICE 'Column ReservedForInvitationId added to SponsorshipCodes';
    ELSE
        RAISE NOTICE 'Column ReservedForInvitationId already exists in SponsorshipCodes, skipping';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'SponsorshipCodes' AND column_name = 'ReservedAt'
    ) THEN
        ALTER TABLE "SponsorshipCodes"
        ADD COLUMN "ReservedAt" TIMESTAMP NULL;
        
        RAISE NOTICE 'Column ReservedAt added to SponsorshipCodes';
    ELSE
        RAISE NOTICE 'Column ReservedAt already exists in SponsorshipCodes, skipping';
    END IF;
END $$;

COMMENT ON COLUMN "SponsorshipCodes"."ReservedForInvitationId" IS 
'Invitation ID for which this code is reserved. Prevents double-allocation during pending invitations.';

COMMENT ON COLUMN "SponsorshipCodes"."ReservedAt" IS 
'Timestamp when the code was reserved for an invitation. Reservation expires with invitation.';
-- Step 4: Migrate existing PurchaseId data to PackageTier (tier name, not ID)
-- Maps existing purchase records to their tier names for backward compatibility
UPDATE "DealerInvitations" di
SET "PackageTier" = (
    SELECT st."TierName"
    FROM "SponsorshipPurchases" sp
    INNER JOIN "SubscriptionTiers" st ON sp."SubscriptionTierId" = st."Id"
    WHERE sp."Id" = di."PurchaseId"
    LIMIT 1
)
WHERE di."PurchaseId" IS NOT NULL
  AND di."PackageTier" IS NULL;

-- ========================================================================
-- PART 3: Add Foreign Key Constraint for Code Reservation
-- ========================================================================

-- Step 5: Add foreign key constraint to ensure referential integrity
-- Use DO block to make it idempotent (safe to run multiple times)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_SponsorshipCodes_ReservedForInvitation'
    ) THEN
        ALTER TABLE "SponsorshipCodes"
        ADD CONSTRAINT "FK_SponsorshipCodes_ReservedForInvitation"
        FOREIGN KEY ("ReservedForInvitationId")
        REFERENCES "DealerInvitations"("Id")
        ON DELETE SET NULL;
        
        RAISE NOTICE 'Foreign key constraint FK_SponsorshipCodes_ReservedForInvitation created successfully';
    ELSE
        RAISE NOTICE 'Foreign key constraint FK_SponsorshipCodes_ReservedForInvitation already exists, skipping';
    END IF;
END $$;  -- Clear reservation if invitation is deleted

-- ========================================================================
-- PART 4: Create Performance Indexes
-- ========================================================================

-- Step 6: Index for intelligent code selection (multi-column for optimal performance)
-- Used by: GetCodesToTransferAsync in all dealer invitation commands
-- Query pattern: WHERE SponsorId AND !IsUsed AND DealerId IS NULL AND ReservedForInvitationId IS NULL AND ExpiryDate > NOW
--                ORDER BY ExpiryDate, CreatedDate
CREATE INDEX IF NOT EXISTS "IX_SponsorshipCodes_IntelligentSelection"
ON "SponsorshipCodes" ("SponsorId", "IsUsed", "DealerId", "ReservedForInvitationId", "ExpiryDate", "CreatedDate")
WHERE "IsUsed" = false AND "DealerId" IS NULL AND "ReservedForInvitationId" IS NULL;

COMMENT ON INDEX "IX_SponsorshipCodes_IntelligentSelection" IS 
'Optimizes intelligent code selection queries with FIFO ordering. Partial index for available codes only.';
-- Step 7: Index for tier-based filtering
-- Used when PackageTier is specified in requests
CREATE INDEX IF NOT EXISTS "IX_SponsorshipCodes_TierSelection"
ON "SponsorshipCodes" ("SubscriptionTierId", "IsUsed", "DealerId")
WHERE "IsUsed" = false AND "DealerId" IS NULL;

COMMENT ON INDEX "IX_SponsorshipCodes_TierSelection" IS 
'Optimizes tier-based code filtering. Partial index for available codes only.';
-- Step 8: Index for reservation lookup
-- Used by: AcceptDealerInvitationCommand to find reserved codes
CREATE INDEX IF NOT EXISTS "IX_SponsorshipCodes_Reservation"
ON "SponsorshipCodes" ("ReservedForInvitationId")
WHERE "ReservedForInvitationId" IS NOT NULL;

COMMENT ON INDEX "IX_SponsorshipCodes_Reservation" IS 
'Optimizes reservation lookup during invitation acceptance. Partial index for reserved codes only.';
-- ========================================================================
-- PART 5: Verification Queries
-- ========================================================================

-- Verify PurchaseId is nullable
SELECT column_name, is_nullable, data_type
FROM information_schema.columns
WHERE table_name = 'DealerInvitations'
  AND column_name = 'PurchaseId';
-- Expected: is_nullable = 'YES'

-- Verify PackageTier column exists
SELECT column_name, is_nullable, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'DealerInvitations'
  AND column_name = 'PackageTier';
-- Expected: 1 row, data_type = 'character varying', character_maximum_length = 10

-- Verify reservation columns exist
SELECT column_name, is_nullable, data_type
FROM information_schema.columns
WHERE table_name = 'SponsorshipCodes'
  AND column_name IN ('ReservedForInvitationId', 'ReservedAt');
-- Expected: 2 rows

-- Verify indexes created
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'SponsorshipCodes'
  AND indexname LIKE 'IX_SponsorshipCodes_%';
-- Expected: Multiple indexes including new ones

-- Verify foreign key constraint
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_name = 'SponsorshipCodes'
  AND tc.constraint_name = 'FK_SponsorshipCodes_ReservedForInvitation';
-- Expected: 1 row

-- ========================================================================
-- PART 6: Test Queries (Performance Check)
-- ========================================================================

-- Test intelligent code selection query (should use IX_SponsorshipCodes_IntelligentSelection)
EXPLAIN ANALYZE
SELECT *
FROM "SponsorshipCodes"
WHERE "SponsorId" = 159  -- Replace with test sponsor ID
  AND "IsUsed" = false
  AND "DealerId" IS NULL
  AND "ReservedForInvitationId" IS NULL
  AND "ExpiryDate" > NOW()
ORDER BY "ExpiryDate" ASC, "CreatedDate" ASC
LIMIT 10;
-- Expected: Index Scan using IX_SponsorshipCodes_IntelligentSelection
-- Expected: Execution time < 10ms

-- Test tier-based selection query
EXPLAIN ANALYZE
SELECT sc.*
FROM "SponsorshipCodes" sc
INNER JOIN "SubscriptionTiers" st ON sc."SubscriptionTierId" = st."Id"
WHERE sc."SponsorId" = 159
  AND sc."IsUsed" = false
  AND sc."DealerId" IS NULL
  AND sc."ReservedForInvitationId" IS NULL
  AND sc."ExpiryDate" > NOW()
  AND st."TierId" = 'M'
ORDER BY sc."ExpiryDate" ASC, sc."CreatedDate" ASC
LIMIT 10;
-- Expected: Uses indexes for optimal performance
-- Expected: Execution time < 15ms

-- ========================================================================
-- ROLLBACK SCRIPT (If Needed)
-- ========================================================================

/*
-- ROLLBACK: Undo all changes

-- Remove indexes
DROP INDEX IF EXISTS "IX_SponsorshipCodes_TierSelection";
DROP INDEX IF EXISTS "IX_SponsorshipCodes_IntelligentSelection";
DROP INDEX IF EXISTS "IX_SponsorshipCodes_ReservedForInvitationId";

-- Remove foreign key
ALTER TABLE "SponsorshipCodes"
DROP CONSTRAINT IF EXISTS "FK_SponsorshipCodes_ReservedForInvitation";

-- Remove columns from SponsorshipCodes
ALTER TABLE "SponsorshipCodes"
DROP COLUMN IF EXISTS "ReservedForInvitationId",
DROP COLUMN IF EXISTS "ReservedAt";

-- Remove PackageTier from DealerInvitations
ALTER TABLE "DealerInvitations"
DROP COLUMN IF EXISTS "PackageTier";

-- Restore PurchaseId NOT NULL constraint
ALTER TABLE "DealerInvitations"
ALTER COLUMN "PurchaseId" SET NOT NULL;

*/

-- ========================================================================
-- Migration Complete
-- ========================================================================

SELECT 'Migration 001_remove_purchaseid_add_packagetier_and_reservation.sql completed successfully!' AS status;
