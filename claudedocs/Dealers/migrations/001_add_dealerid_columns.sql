-- =====================================================
-- Migration: Add DealerId columns to SponsorshipCodes and PlantAnalyses
-- Date: 2025-01-26
-- Purpose: Enable dealer code distribution system
-- =====================================================

-- STEP 1: Add DealerId to SponsorshipCodes table
-- =====================================================
ALTER TABLE "SponsorshipCodes" 
ADD "DealerId" INT NULL,
ADD "TransferredAt" TIMESTAMP NULL,
ADD "TransferredByUserId" INT NULL,
ADD "ReclaimedAt" TIMESTAMP NULL,
ADD "ReclaimedByUserId" INT NULL;

-- Add foreign key constraint
ALTER TABLE "SponsorshipCodes"
ADD CONSTRAINT "FK_SponsorshipCodes_Dealer"
FOREIGN KEY ("DealerId") REFERENCES "Users"("Id");

ALTER TABLE "SponsorshipCodes"
ADD CONSTRAINT "FK_SponsorshipCodes_TransferredBy"
FOREIGN KEY ("TransferredByUserId") REFERENCES "Users"("Id");

ALTER TABLE "SponsorshipCodes"
ADD CONSTRAINT "FK_SponsorshipCodes_ReclaimedBy"
FOREIGN KEY ("ReclaimedByUserId") REFERENCES "Users"("Id");

-- STEP 2: Add DealerId to PlantAnalyses table
-- =====================================================
ALTER TABLE "PlantAnalyses" 
ADD "DealerId" INT NULL;

-- Add foreign key constraint
ALTER TABLE "PlantAnalyses"
ADD CONSTRAINT "FK_PlantAnalyses_Dealer"
FOREIGN KEY ("DealerId") REFERENCES "Users"("Id");

-- STEP 3: Add indexes for performance
-- =====================================================
CREATE INDEX "IX_SponsorshipCodes_DealerId" ON "SponsorshipCodes"("DealerId");
CREATE INDEX "IX_SponsorshipCodes_SponsorId_DealerId" ON "SponsorshipCodes"("SponsorId", "DealerId");
CREATE INDEX "IX_PlantAnalyses_DealerId" ON "PlantAnalyses"("DealerId");
CREATE INDEX "IX_PlantAnalyses_SponsorId_DealerId" ON "PlantAnalyses"("SponsorId", "DealerId");

-- =====================================================
-- BACKWARD COMPATIBILITY VERIFICATION
-- =====================================================
-- DealerId is NULLABLE, so existing records work without modification
-- Existing sponsor operations (DealerId = NULL) continue to function normally

-- Verify existing data is not affected:
SELECT 
    COUNT(*) as TotalCodes,
    COUNT("DealerId") as CodesWithDealer,
    COUNT(*) - COUNT("DealerId") as DirectSponsorCodes
FROM "SponsorshipCodes";

SELECT 
    COUNT(*) as TotalAnalyses,
    COUNT("DealerId") as AnalysesWithDealer,
    COUNT(*) - COUNT("DealerId") as DirectSponsorAnalyses
FROM "PlantAnalyses";

-- Expected result: All CodesWithDealer and AnalysesWithDealer should be 0 after migration
