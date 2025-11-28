-- =============================================
-- Migration: Add Multi-Image URL Fields
-- Feature: Multi-Image Plant Analysis
-- Date: 2025-11-27
-- Description: Adds 4 new URL columns to PlantAnalyses table
--              to support comprehensive multi-image analysis
-- =============================================

-- Phase: Database Schema Update
-- Tables Affected: PlantAnalyses

BEGIN;

-- Add 4 new URL columns for multi-image analysis
ALTER TABLE public."PlantAnalyses"
    ADD COLUMN "LeafTopUrl" TEXT NULL,
    ADD COLUMN "LeafBottomUrl" TEXT NULL,
    ADD COLUMN "PlantOverviewUrl" TEXT NULL,
    ADD COLUMN "RootUrl" TEXT NULL;

-- Add column comments for documentation
COMMENT ON COLUMN public."PlantAnalyses"."LeafTopUrl" IS 'URL for top view of leaf image (optional, for detailed leaf analysis)';
COMMENT ON COLUMN public."PlantAnalyses"."LeafBottomUrl" IS 'URL for bottom view of leaf image (optional, for stomata and underside inspection)';
COMMENT ON COLUMN public."PlantAnalyses"."PlantOverviewUrl" IS 'URL for full plant overview image (optional, for structural and growth assessment)';
COMMENT ON COLUMN public."PlantAnalyses"."RootUrl" IS 'URL for root system image (optional, for root health analysis)';

-- Create indexes for performance (if needed for queries)
-- Note: Text URLs are unlikely to be queried directly, but adding for consistency
CREATE INDEX "IDX_PlantAnalyses_LeafTopUrl" ON public."PlantAnalyses" ("LeafTopUrl") WHERE "LeafTopUrl" IS NOT NULL;
CREATE INDEX "IDX_PlantAnalyses_LeafBottomUrl" ON public."PlantAnalyses" ("LeafBottomUrl") WHERE "LeafBottomUrl" IS NOT NULL;
CREATE INDEX "IDX_PlantAnalyses_PlantOverviewUrl" ON public."PlantAnalyses" ("PlantOverviewUrl") WHERE "PlantOverviewUrl" IS NOT NULL;
CREATE INDEX "IDX_PlantAnalyses_RootUrl" ON public."PlantAnalyses" ("RootUrl") WHERE "RootUrl" IS NOT NULL;

COMMIT;

-- Verification Query
-- SELECT "Id", "AnalysisId", "ImageUrl", "LeafTopUrl", "LeafBottomUrl", "PlantOverviewUrl", "RootUrl"
-- FROM public."PlantAnalyses"
-- WHERE "LeafTopUrl" IS NOT NULL OR "LeafBottomUrl" IS NOT NULL OR "PlantOverviewUrl" IS NOT NULL OR "RootUrl" IS NOT NULL
-- LIMIT 10;

-- =============================================
-- Rollback Script (if needed)
-- =============================================
/*
BEGIN;

DROP INDEX IF EXISTS public."IDX_PlantAnalyses_RootUrl";
DROP INDEX IF EXISTS public."IDX_PlantAnalyses_PlantOverviewUrl";
DROP INDEX IF EXISTS public."IDX_PlantAnalyses_LeafBottomUrl";
DROP INDEX IF EXISTS public."IDX_PlantAnalyses_LeafTopUrl";

ALTER TABLE public."PlantAnalyses"
    DROP COLUMN IF EXISTS "RootUrl",
    DROP COLUMN IF EXISTS "PlantOverviewUrl",
    DROP COLUMN IF EXISTS "LeafBottomUrl",
    DROP COLUMN IF EXISTS "LeafTopUrl";

COMMIT;
*/
