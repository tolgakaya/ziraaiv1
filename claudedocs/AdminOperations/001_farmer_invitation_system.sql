-- =====================================================
-- Farmer Invitation System - Database Migration
-- =====================================================
-- Purpose: Add farmer invitation functionality (token-based code distribution)
-- Date: 2026-01-02
-- Related: feature/remove-sms-listener-logic
-- =====================================================

-- =====================================================
-- PART 1: CREATE FARMER INVITATION TABLE
-- =====================================================

CREATE TABLE "FarmerInvitation" (
    "Id" SERIAL PRIMARY KEY,

    -- Sponsor Information
    "SponsorId" INTEGER NOT NULL,

    -- Farmer Information (at least phone is required)
    "Phone" VARCHAR(20) NOT NULL,
    "FarmerName" VARCHAR(200),
    "Email" VARCHAR(200),

    -- Invitation Details
    "InvitationToken" VARCHAR(100) NOT NULL UNIQUE,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Accepted, Expired, Cancelled
    "InvitationType" VARCHAR(50) NOT NULL DEFAULT 'Invite',

    -- Code Information
    "CodeCount" INTEGER NOT NULL CHECK ("CodeCount" > 0),
    "PackageTier" VARCHAR(10), -- Optional: S, M, L, XL

    -- Acceptance Tracking
    "AcceptedByUserId" INTEGER,
    "AcceptedDate" TIMESTAMP,

    -- SMS Tracking
    "LinkSentDate" TIMESTAMP,
    "LinkSentVia" VARCHAR(50), -- SMS, WhatsApp, Email
    "LinkDelivered" BOOLEAN NOT NULL DEFAULT FALSE,

    -- Lifecycle
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "ExpiryDate" TIMESTAMP NOT NULL,
    "CancelledDate" TIMESTAMP,
    "Notes" TEXT,

    -- Foreign Keys
    CONSTRAINT "FK_FarmerInvitation_Sponsor"
        FOREIGN KEY ("SponsorId")
        REFERENCES "Users"("UserId")
        ON DELETE CASCADE,

    CONSTRAINT "FK_FarmerInvitation_AcceptedBy"
        FOREIGN KEY ("AcceptedByUserId")
        REFERENCES "Users"("UserId")
        ON DELETE SET NULL
);

-- =====================================================
-- PART 2: CREATE INDEXES FOR FARMER INVITATION
-- =====================================================

-- Index for sponsor queries (most common)
CREATE INDEX "IX_FarmerInvitation_SponsorId"
    ON "FarmerInvitation"("SponsorId");

-- Index for token lookup (mobile app deep link)
CREATE INDEX "IX_FarmerInvitation_Token"
    ON "FarmerInvitation"("InvitationToken");

-- Index for phone-based queries
CREATE INDEX "IX_FarmerInvitation_Phone"
    ON "FarmerInvitation"("Phone");

-- Index for status filtering
CREATE INDEX "IX_FarmerInvitation_Status"
    ON "FarmerInvitation"("Status");

-- Composite index for sponsor + status queries (performance optimization)
CREATE INDEX "IX_FarmerInvitation_SponsorId_Status"
    ON "FarmerInvitation"("SponsorId", "Status");

-- =====================================================
-- PART 3: ALTER SPONSORSHIPCODES TABLE
-- =====================================================

-- Add farmer invitation tracking fields (nullable for backward compatibility)
ALTER TABLE "SponsorshipCodes"
    ADD COLUMN "FarmerInvitationId" INTEGER NULL,
    ADD COLUMN "ReservedForFarmerInvitationId" INTEGER NULL,
    ADD COLUMN "ReservedForFarmerAt" TIMESTAMP NULL;

-- Add foreign key constraint
ALTER TABLE "SponsorshipCodes"
    ADD CONSTRAINT "FK_SponsorshipCode_FarmerInvitation"
    FOREIGN KEY ("FarmerInvitationId")
    REFERENCES "FarmerInvitation"("Id")
    ON DELETE SET NULL;

-- =====================================================
-- PART 4: CREATE INDEXES FOR SPONSORSHIPCODES
-- =====================================================

-- Index for farmer invitation queries
CREATE INDEX "IX_SponsorshipCode_FarmerInvitationId"
    ON "SponsorshipCodes"("FarmerInvitationId")
    WHERE "FarmerInvitationId" IS NOT NULL;

-- Index for reserved codes queries
CREATE INDEX "IX_SponsorshipCode_ReservedForFarmerInvitationId"
    ON "SponsorshipCodes"("ReservedForFarmerInvitationId")
    WHERE "ReservedForFarmerInvitationId" IS NOT NULL;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Verify FarmerInvitation table created
SELECT
    table_name,
    table_type
FROM information_schema.tables
WHERE table_name = 'FarmerInvitation';

-- Verify indexes created
SELECT
    indexname,
    tablename,
    indexdef
FROM pg_indexes
WHERE tablename IN ('FarmerInvitation', 'SponsorshipCodes')
    AND indexname LIKE '%Farmer%'
ORDER BY tablename, indexname;

-- Verify new columns added to SponsorshipCodes
SELECT
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'SponsorshipCodes'
    AND column_name IN ('FarmerInvitationId', 'ReservedForFarmerInvitationId', 'ReservedForFarmerAt')
ORDER BY column_name;

-- Verify foreign keys
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
    AND (tc.table_name = 'FarmerInvitation' OR
         (tc.table_name = 'SponsorshipCodes' AND kcu.column_name LIKE '%Farmer%'))
ORDER BY tc.table_name, tc.constraint_name;

-- =====================================================
-- SUCCESS MESSAGE
-- =====================================================

DO $$
BEGIN
    RAISE NOTICE '✅ Farmer Invitation System migration completed successfully!';
    RAISE NOTICE 'Tables created: FarmerInvitation';
    RAISE NOTICE 'Columns added to SponsorshipCodes: FarmerInvitationId, ReservedForFarmerInvitationId, ReservedForFarmerAt';
    RAISE NOTICE 'Indexes created: 7 indexes total';
    RAISE NOTICE 'Foreign keys: 3 constraints added';
END $$;
