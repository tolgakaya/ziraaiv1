-- =============================================
-- Migration: Add SponsorAnalysisAccess Table
-- Purpose: Track sponsor access to plant analyses for messaging validation
-- Date: 2025-10-18
-- =============================================

-- Create SponsorAnalysisAccess table
CREATE TABLE "SponsorAnalysisAccess" (
    "Id" SERIAL PRIMARY KEY,

    -- Access Information
    "SponsorId" INTEGER NOT NULL,
    "PlantAnalysisId" INTEGER NOT NULL,
    "FarmerId" INTEGER NOT NULL,

    -- Access Level Based on Sponsor Tier
    "AccessLevel" VARCHAR(50) NOT NULL,
    "AccessPercentage" INTEGER NOT NULL DEFAULT 30,

    -- Tracking
    "FirstViewedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "LastViewedDate" TIMESTAMP NULL,
    "ViewCount" INTEGER NOT NULL DEFAULT 0,
    "DownloadedDate" TIMESTAMP NULL,
    "HasDownloaded" BOOLEAN NOT NULL DEFAULT FALSE,

    -- Data Access Details
    "AccessedFields" TEXT NULL,
    "RestrictedFields" TEXT NULL,
    "CanViewHealthScore" BOOLEAN NOT NULL DEFAULT FALSE,
    "CanViewDiseases" BOOLEAN NOT NULL DEFAULT FALSE,
    "CanViewPests" BOOLEAN NOT NULL DEFAULT FALSE,
    "CanViewNutrients" BOOLEAN NOT NULL DEFAULT FALSE,
    "CanViewRecommendations" BOOLEAN NOT NULL DEFAULT FALSE,
    "CanViewFarmerContact" BOOLEAN NOT NULL DEFAULT FALSE,
    "CanViewLocation" BOOLEAN NOT NULL DEFAULT FALSE,
    "CanViewImages" BOOLEAN NOT NULL DEFAULT FALSE,

    -- Interaction
    "HasContactedFarmer" BOOLEAN NOT NULL DEFAULT FALSE,
    "ContactDate" TIMESTAMP NULL,
    "ContactMethod" VARCHAR(50) NULL,
    "Notes" VARCHAR(1000) NULL,

    -- Sponsorship Context
    "SponsorshipCodeId" INTEGER NULL,
    "SponsorshipPurchaseId" INTEGER NULL,

    -- Audit
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMP NULL,
    "IpAddress" VARCHAR(50) NULL,
    "UserAgent" VARCHAR(500) NULL,

    -- Foreign Keys
    CONSTRAINT "FK_SponsorAnalysisAccess_Sponsor"
        FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId") ON DELETE RESTRICT,

    CONSTRAINT "FK_SponsorAnalysisAccess_PlantAnalysis"
        FOREIGN KEY ("PlantAnalysisId") REFERENCES "PlantAnalyses"("Id") ON DELETE CASCADE,

    CONSTRAINT "FK_SponsorAnalysisAccess_Farmer"
        FOREIGN KEY ("FarmerId") REFERENCES "Users"("UserId") ON DELETE RESTRICT,

    CONSTRAINT "FK_SponsorAnalysisAccess_SponsorshipCode"
        FOREIGN KEY ("SponsorshipCodeId") REFERENCES "SponsorshipCodes"("Id") ON DELETE RESTRICT,

    CONSTRAINT "FK_SponsorAnalysisAccess_SponsorshipPurchase"
        FOREIGN KEY ("SponsorshipPurchaseId") REFERENCES "SponsorshipPurchases"("Id") ON DELETE RESTRICT
);

-- Create indexes for performance
CREATE INDEX "IX_SponsorAnalysisAccess_SponsorId"
    ON "SponsorAnalysisAccess"("SponsorId");

CREATE INDEX "IX_SponsorAnalysisAccess_PlantAnalysisId"
    ON "SponsorAnalysisAccess"("PlantAnalysisId");

CREATE INDEX "IX_SponsorAnalysisAccess_FarmerId"
    ON "SponsorAnalysisAccess"("FarmerId");

CREATE INDEX "IX_SponsorAnalysisAccess_FirstViewedDate"
    ON "SponsorAnalysisAccess"("FirstViewedDate");

CREATE INDEX "IX_SponsorAnalysisAccess_AccessLevel"
    ON "SponsorAnalysisAccess"("AccessLevel");

-- Create unique constraint: one access record per sponsor-analysis pair
CREATE UNIQUE INDEX "IX_SponsorAnalysisAccess_SponsorId_PlantAnalysisId"
    ON "SponsorAnalysisAccess"("SponsorId", "PlantAnalysisId");

-- Add comments for documentation
COMMENT ON TABLE "SponsorAnalysisAccess" IS 'Tracks sponsor access to plant analyses for tier-based data filtering and messaging validation';
COMMENT ON COLUMN "SponsorAnalysisAccess"."AccessLevel" IS 'Access level based on sponsor tier: Basic30, Extended60, Full100';
COMMENT ON COLUMN "SponsorAnalysisAccess"."AccessPercentage" IS 'Percentage of data accessible: 30, 60, or 100';
COMMENT ON COLUMN "SponsorAnalysisAccess"."ViewCount" IS 'Number of times sponsor has viewed this analysis';
COMMENT ON COLUMN "SponsorAnalysisAccess"."AccessedFields" IS 'JSON array of field names sponsor can access';
COMMENT ON COLUMN "SponsorAnalysisAccess"."RestrictedFields" IS 'JSON array of field names sponsor cannot access';

-- Migration complete
