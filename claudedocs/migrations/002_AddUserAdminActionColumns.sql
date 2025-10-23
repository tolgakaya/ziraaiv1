-- Migration: Add Admin Action Columns to Users Table
-- Date: 2025-10-23
-- Purpose: Track user deactivation and admin actions on user accounts

-- =============================================================================
-- 1. ADD COLUMNS TO USERS TABLE
-- =============================================================================

-- Add IsActive column (default TRUE for existing users)
ALTER TABLE "Users" 
ADD COLUMN "IsActive" BOOLEAN DEFAULT TRUE NOT NULL;

-- Add deactivation tracking columns
ALTER TABLE "Users"
ADD COLUMN "DeactivatedDate" TIMESTAMP NULL,
ADD COLUMN "DeactivatedBy" INTEGER NULL,
ADD COLUMN "DeactivationReason" TEXT NULL;

-- =============================================================================
-- 2. ADD FOREIGN KEY CONSTRAINT
-- =============================================================================

-- Foreign key to track which admin deactivated the user
ALTER TABLE "Users"
ADD CONSTRAINT "FK_Users_DeactivatedBy" 
    FOREIGN KEY ("DeactivatedBy") 
    REFERENCES "Users"("UserId") 
    ON DELETE SET NULL;

-- =============================================================================
-- 3. CREATE INDEXES FOR PERFORMANCE
-- =============================================================================

-- Index for active/inactive user queries
CREATE INDEX "IX_Users_IsActive" 
    ON "Users"("IsActive");

-- Index for deactivation queries (who deactivated, when)
CREATE INDEX "IX_Users_DeactivatedBy_DeactivatedDate" 
    ON "Users"("DeactivatedBy", "DeactivatedDate")
    WHERE "DeactivatedBy" IS NOT NULL;

-- Composite index for common query: active users by registration date
CREATE INDEX "IX_Users_IsActive_RecordDate" 
    ON "Users"("IsActive", "RecordDate" DESC);

-- =============================================================================
-- 4. UPDATE EXISTING DATA
-- =============================================================================

-- Set all existing users as active (safety measure)
UPDATE "Users" 
SET "IsActive" = TRUE 
WHERE "IsActive" IS NULL;

-- =============================================================================
-- 5. COMMENTS FOR DOCUMENTATION
-- =============================================================================

COMMENT ON COLUMN "Users"."IsActive" IS 
'Indicates if user account is active. False means deactivated by admin.';

COMMENT ON COLUMN "Users"."DeactivatedDate" IS 
'Timestamp when user was deactivated by admin (null if active)';

COMMENT ON COLUMN "Users"."DeactivatedBy" IS 
'Admin user ID who deactivated this user (null if active)';

COMMENT ON COLUMN "Users"."DeactivationReason" IS 
'Admin-provided reason for deactivation (for audit and user communication)';

-- =============================================================================
-- 6. VERIFICATION QUERIES
-- =============================================================================

-- Verify columns were added
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Users'
    AND column_name IN ('IsActive', 'DeactivatedDate', 'DeactivatedBy', 'DeactivationReason')
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
    AND tc.table_name = 'Users'
    AND kcu.column_name = 'DeactivatedBy';

-- Verify indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'Users' 
    AND indexname LIKE '%Deactivate%' OR indexname LIKE '%IsActive%'
ORDER BY indexname;

-- Verify all users are active
SELECT 
    COUNT(*) AS total_users,
    SUM(CASE WHEN "IsActive" = TRUE THEN 1 ELSE 0 END) AS active_users,
    SUM(CASE WHEN "IsActive" = FALSE THEN 1 ELSE 0 END) AS inactive_users
FROM "Users";

-- =============================================================================
-- 7. TEST QUERIES (OPTIONAL - FOR VERIFICATION)
-- =============================================================================

-- Test deactivation scenario (DO NOT RUN IN PRODUCTION)
/*
-- Example: Deactivate a test user
UPDATE "Users"
SET "IsActive" = FALSE,
    "DeactivatedDate" = NOW(),
    "DeactivatedBy" = 1,  -- Admin user ID
    "DeactivationReason" = 'Test deactivation'
WHERE "UserId" = 999;  -- Test user ID

-- Query deactivated users
SELECT 
    u."UserId",
    u."FullName",
    u."Email",
    u."IsActive",
    u."DeactivatedDate",
    admin."FullName" AS deactivated_by_admin,
    u."DeactivationReason"
FROM "Users" u
LEFT JOIN "Users" admin ON u."DeactivatedBy" = admin."UserId"
WHERE u."IsActive" = FALSE;

-- Reactivate test user
UPDATE "Users"
SET "IsActive" = TRUE,
    "DeactivatedDate" = NULL,
    "DeactivatedBy" = NULL,
    "DeactivationReason" = NULL
WHERE "UserId" = 999;
*/

-- =============================================================================
-- 8. ROLLBACK SCRIPT (IN CASE OF ISSUES)
-- =============================================================================

-- ROLLBACK INSTRUCTIONS:
-- If you need to rollback this migration, run the following:

/*
-- Drop indexes first
DROP INDEX IF EXISTS "IX_Users_IsActive_RecordDate";
DROP INDEX IF EXISTS "IX_Users_DeactivatedBy_DeactivatedDate";
DROP INDEX IF EXISTS "IX_Users_IsActive";

-- Drop foreign key constraint
ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "FK_Users_DeactivatedBy";

-- Drop columns
ALTER TABLE "Users" DROP COLUMN IF EXISTS "DeactivationReason";
ALTER TABLE "Users" DROP COLUMN IF EXISTS "DeactivatedBy";
ALTER TABLE "Users" DROP COLUMN IF EXISTS "DeactivatedDate";
ALTER TABLE "Users" DROP COLUMN IF EXISTS "IsActive";
*/

-- =============================================================================
-- MIGRATION COMPLETE
-- =============================================================================
