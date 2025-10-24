-- Migration: Add On-Behalf-Of Columns to PlantAnalyses Table
-- Date: 2025-10-23
-- Purpose: Track when admin creates analysis on behalf of a farmer

-- =============================================================================
-- 1. ADD COLUMNS TO PLANTANALYSES TABLE
-- =============================================================================

-- Add admin tracking column for OBO operations
ALTER TABLE "PlantAnalyses"
ADD COLUMN "CreatedByAdminId" INTEGER NULL;

-- Add flag to indicate OBO operation
ALTER TABLE "PlantAnalyses"
ADD COLUMN "IsOnBehalfOf" BOOLEAN DEFAULT FALSE NOT NULL;

-- =============================================================================
-- 2. ADD FOREIGN KEY CONSTRAINT
-- =============================================================================

-- Foreign key to track which admin created the analysis on behalf of user
ALTER TABLE "PlantAnalyses"
ADD CONSTRAINT "FK_PlantAnalyses_CreatedByAdmin" 
    FOREIGN KEY ("CreatedByAdminId") 
    REFERENCES "Users"("UserId") 
    ON DELETE SET NULL;

-- =============================================================================
-- 3. CREATE INDEXES FOR PERFORMANCE
-- =============================================================================

-- Index for OBO queries (find analyses created by specific admin)
CREATE INDEX "IX_PlantAnalyses_CreatedByAdminId" 
    ON "PlantAnalyses"("CreatedByAdminId")
    WHERE "CreatedByAdminId" IS NOT NULL;

-- Index for OBO flag queries
CREATE INDEX "IX_PlantAnalyses_IsOnBehalfOf" 
    ON "PlantAnalyses"("IsOnBehalfOf")
    WHERE "IsOnBehalfOf" = TRUE;

-- Composite index for common query: OBO analyses by admin and date
CREATE INDEX "IX_PlantAnalyses_CreatedByAdminId_CreatedDate" 
    ON "PlantAnalyses"("CreatedByAdminId", "CreatedDate" DESC)
    WHERE "CreatedByAdminId" IS NOT NULL;

-- Composite index for user's OBO analyses
CREATE INDEX "IX_PlantAnalyses_UserId_IsOnBehalfOf" 
    ON "PlantAnalyses"("UserId", "IsOnBehalfOf", "CreatedDate" DESC)
    WHERE "IsOnBehalfOf" = TRUE;

-- =============================================================================
-- 4. UPDATE EXISTING DATA
-- =============================================================================

-- Set IsOnBehalfOf to FALSE for all existing analyses (they were not OBO)
UPDATE "PlantAnalyses" 
SET "IsOnBehalfOf" = FALSE 
WHERE "IsOnBehalfOf" IS NULL;

-- Verify no existing analyses have CreatedByAdminId set
-- (should be NULL for all existing data)
SELECT COUNT(*) AS analyses_with_admin_id
FROM "PlantAnalyses"
WHERE "CreatedByAdminId" IS NOT NULL;

-- =============================================================================
-- 5. ADD CHECK CONSTRAINT (BUSINESS RULE)
-- =============================================================================

-- Business rule: If IsOnBehalfOf is TRUE, CreatedByAdminId must be set
ALTER TABLE "PlantAnalyses"
ADD CONSTRAINT "CHK_PlantAnalyses_OBO_Consistency"
    CHECK (
        ("IsOnBehalfOf" = FALSE AND "CreatedByAdminId" IS NULL) OR
        ("IsOnBehalfOf" = TRUE AND "CreatedByAdminId" IS NOT NULL)
    );

-- =============================================================================
-- 6. COMMENTS FOR DOCUMENTATION
-- =============================================================================

COMMENT ON COLUMN "PlantAnalyses"."CreatedByAdminId" IS 
'Admin user ID who created this analysis on behalf of the farmer (null if farmer created directly)';

COMMENT ON COLUMN "PlantAnalyses"."IsOnBehalfOf" IS 
'True when analysis was created by admin on behalf of farmer (for customer support scenarios)';

COMMENT ON CONSTRAINT "CHK_PlantAnalyses_OBO_Consistency" ON "PlantAnalyses" IS
'Ensures data consistency: if IsOnBehalfOf is TRUE, CreatedByAdminId must be set';

-- =============================================================================
-- 7. VERIFICATION QUERIES
-- =============================================================================

-- Verify columns were added
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'PlantAnalyses'
    AND column_name IN ('CreatedByAdminId', 'IsOnBehalfOf')
ORDER BY column_name;

-- Verify foreign key
SELECT
    tc.constraint_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_name = 'PlantAnalyses'
    AND kcu.column_name = 'CreatedByAdminId';

-- Verify check constraint
SELECT
    con.conname AS constraint_name,
    pg_get_constraintdef(con.oid) AS constraint_definition
FROM pg_constraint con
JOIN pg_class rel ON rel.oid = con.conrelid
WHERE rel.relname = 'PlantAnalyses'
    AND con.conname = 'CHK_PlantAnalyses_OBO_Consistency';

-- Verify indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'PlantAnalyses' 
    AND (indexname LIKE '%OBO%' OR indexname LIKE '%CreatedByAdmin%')
ORDER BY indexname;

-- Verify existing data
SELECT 
    COUNT(*) AS total_analyses,
    SUM(CASE WHEN "IsOnBehalfOf" = TRUE THEN 1 ELSE 0 END) AS obo_analyses,
    SUM(CASE WHEN "IsOnBehalfOf" = FALSE THEN 1 ELSE 0 END) AS direct_analyses,
    SUM(CASE WHEN "CreatedByAdminId" IS NOT NULL THEN 1 ELSE 0 END) AS analyses_with_admin
FROM "PlantAnalyses";

-- =============================================================================
-- 8. TEST QUERIES (OPTIONAL - FOR VERIFICATION)
-- =============================================================================

-- Test OBO scenario (DO NOT RUN IN PRODUCTION)
/*
-- Example: Create test OBO analysis
INSERT INTO "PlantAnalyses" (
    "UserId",           -- Farmer ID
    "CropType",
    "ImageUrl",
    "Status",
    "CreatedDate",
    "CreatedByAdminId", -- Admin who created it
    "IsOnBehalfOf"
) VALUES (
    456,                -- Farmer user ID
    'Domates',
    'https://example.com/test.jpg',
    'Processing',
    NOW(),
    1,                  -- Admin user ID
    TRUE
);

-- Query OBO analyses
SELECT 
    pa."Id",
    pa."UserId",
    farmer."FullName" AS farmer_name,
    pa."CropType",
    pa."Status",
    pa."CreatedDate",
    pa."IsOnBehalfOf",
    pa."CreatedByAdminId",
    admin."FullName" AS created_by_admin
FROM "PlantAnalyses" pa
JOIN "Users" farmer ON pa."UserId" = farmer."UserId"
LEFT JOIN "Users" admin ON pa."CreatedByAdminId" = admin."UserId"
WHERE pa."IsOnBehalfOf" = TRUE
ORDER BY pa."CreatedDate" DESC;

-- Test check constraint violation (should fail)
INSERT INTO "PlantAnalyses" (
    "UserId",
    "CropType",
    "ImageUrl",
    "Status",
    "CreatedDate",
    "IsOnBehalfOf"      -- TRUE but no CreatedByAdminId
) VALUES (
    456,
    'Test',
    'https://example.com/test.jpg',
    'Processing',
    NOW(),
    TRUE                -- Should fail - no admin ID
);
*/

-- =============================================================================
-- 9. USEFUL ADMIN QUERIES
-- =============================================================================

-- Query: Find all OBO analyses by admin
/*
SELECT 
    admin."FullName" AS admin_name,
    COUNT(pa."Id") AS total_obo_analyses,
    COUNT(DISTINCT pa."UserId") AS unique_farmers_helped,
    MIN(pa."CreatedDate") AS first_obo,
    MAX(pa."CreatedDate") AS last_obo
FROM "PlantAnalyses" pa
JOIN "Users" admin ON pa."CreatedByAdminId" = admin."UserId"
WHERE pa."IsOnBehalfOf" = TRUE
GROUP BY admin."UserId", admin."FullName"
ORDER BY total_obo_analyses DESC;
*/

-- Query: Find farmers who received most admin help
/*
SELECT 
    farmer."UserId",
    farmer."FullName" AS farmer_name,
    COUNT(pa."Id") AS obo_analyses_count,
    STRING_AGG(DISTINCT admin."FullName", ', ') AS helped_by_admins
FROM "PlantAnalyses" pa
JOIN "Users" farmer ON pa."UserId" = farmer."UserId"
LEFT JOIN "Users" admin ON pa."CreatedByAdminId" = admin."UserId"
WHERE pa."IsOnBehalfOf" = TRUE
GROUP BY farmer."UserId", farmer."FullName"
ORDER BY obo_analyses_count DESC;
*/

-- =============================================================================
-- 10. ROLLBACK SCRIPT (IN CASE OF ISSUES)
-- =============================================================================

-- ROLLBACK INSTRUCTIONS:
-- If you need to rollback this migration, run the following:

/*
-- Drop indexes first
DROP INDEX IF EXISTS "IX_PlantAnalyses_UserId_IsOnBehalfOf";
DROP INDEX IF EXISTS "IX_PlantAnalyses_CreatedByAdminId_CreatedDate";
DROP INDEX IF EXISTS "IX_PlantAnalyses_IsOnBehalfOf";
DROP INDEX IF EXISTS "IX_PlantAnalyses_CreatedByAdminId";

-- Drop check constraint
ALTER TABLE "PlantAnalyses" DROP CONSTRAINT IF EXISTS "CHK_PlantAnalyses_OBO_Consistency";

-- Drop foreign key constraint
ALTER TABLE "PlantAnalyses" DROP CONSTRAINT IF EXISTS "FK_PlantAnalyses_CreatedByAdmin";

-- Drop columns
ALTER TABLE "PlantAnalyses" DROP COLUMN IF EXISTS "IsOnBehalfOf";
ALTER TABLE "PlantAnalyses" DROP COLUMN IF EXISTS "CreatedByAdminId";
*/

-- =============================================================================
-- MIGRATION COMPLETE
-- =============================================================================
