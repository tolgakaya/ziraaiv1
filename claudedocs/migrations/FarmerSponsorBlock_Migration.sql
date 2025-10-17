-- =====================================================
-- FarmerSponsorBlock Table Migration
-- Created: 2025-01-17
-- Purpose: Farmer-initiated sponsor blocking for messaging
-- Branch: feature/sponsor-farmer-messaging
-- Database: PostgreSQL
-- =====================================================

-- Create FarmerSponsorBlocks table
CREATE TABLE "FarmerSponsorBlocks" (
    "Id" SERIAL PRIMARY KEY,
    "FarmerId" INTEGER NOT NULL,
    "SponsorId" INTEGER NOT NULL,
    "IsBlocked" BOOLEAN NOT NULL DEFAULT false,
    "IsMuted" BOOLEAN NOT NULL DEFAULT false,
    "CreatedDate" TIMESTAMP NOT NULL,
    "Reason" VARCHAR(500) NULL,

    -- Foreign key constraints
    CONSTRAINT "FK_FarmerSponsorBlocks_Users_FarmerId"
        FOREIGN KEY ("FarmerId")
        REFERENCES "Users" ("UserId")
        ON DELETE RESTRICT,

    CONSTRAINT "FK_FarmerSponsorBlocks_Users_SponsorId"
        FOREIGN KEY ("SponsorId")
        REFERENCES "Users" ("UserId")
        ON DELETE RESTRICT
);

-- Create unique index on (FarmerId, SponsorId) - prevents duplicate blocks
CREATE UNIQUE INDEX "IX_FarmerSponsorBlocks_FarmerId_SponsorId"
    ON "FarmerSponsorBlocks" ("FarmerId", "SponsorId");

-- Create index on FarmerId for farmer's blocked list queries
CREATE INDEX "IX_FarmerSponsorBlocks_FarmerId"
    ON "FarmerSponsorBlocks" ("FarmerId");

-- Create index on SponsorId for sponsor's block status checks
CREATE INDEX "IX_FarmerSponsorBlocks_SponsorId"
    ON "FarmerSponsorBlocks" ("SponsorId");

-- Add comment to table
COMMENT ON TABLE "FarmerSponsorBlocks" IS 'Farmer-initiated blocking of sponsors for messaging system. Allows farmers to prevent unwanted messages from specific sponsors.';

-- Add comments to columns
COMMENT ON COLUMN "FarmerSponsorBlocks"."Id" IS 'Primary key';
COMMENT ON COLUMN "FarmerSponsorBlocks"."FarmerId" IS 'Farmer user ID who is blocking';
COMMENT ON COLUMN "FarmerSponsorBlocks"."SponsorId" IS 'Sponsor user ID being blocked';
COMMENT ON COLUMN "FarmerSponsorBlocks"."IsBlocked" IS 'Is the sponsor blocked (cannot send messages)';
COMMENT ON COLUMN "FarmerSponsorBlocks"."IsMuted" IS 'Is the sponsor muted (can send but farmer doesnt get notifications)';
COMMENT ON COLUMN "FarmerSponsorBlocks"."CreatedDate" IS 'When the block/mute was created';
COMMENT ON COLUMN "FarmerSponsorBlocks"."Reason" IS 'Optional reason for blocking (e.g., Spam, Inappropriate, No longer needed)';
