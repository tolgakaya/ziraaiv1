-- Migration: Add AdminOperationLogs Table
-- Date: 2025-10-23
-- Purpose: Create audit trail table for all admin operations including on-behalf-of actions

-- =============================================================================
-- 1. CREATE TABLE: AdminOperationLogs
-- =============================================================================

CREATE TABLE "AdminOperationLogs" (
    "Id" SERIAL PRIMARY KEY,
    
    -- Actor Information
    "AdminUserId" INTEGER NOT NULL,
    "TargetUserId" INTEGER,  -- User affected by the action (for OBO operations)
    
    -- Action Details
    "Action" VARCHAR(100) NOT NULL,  -- e.g., "DeactivateUser", "CreateAnalysis", "AssignSubscription"
    "EntityType" VARCHAR(50),        -- e.g., "User", "PlantAnalysis", "Subscription"
    "EntityId" INTEGER,               -- ID of the affected entity
    "IsOnBehalfOf" BOOLEAN DEFAULT FALSE,  -- True if admin acted on behalf of another user
    
    -- Request Context
    "IpAddress" VARCHAR(45),          -- IPv4 or IPv6
    "UserAgent" TEXT,                 -- Browser/client info
    "RequestPath" VARCHAR(500),       -- API endpoint called
    "RequestPayload" TEXT,            -- JSON request body (for complex operations)
    
    -- Response Information
    "ResponseStatus" INTEGER,         -- HTTP status code (200, 400, 500, etc.)
    "Duration" INTEGER,               -- Request processing time in milliseconds
    
    -- Audit Information
    "Timestamp" TIMESTAMP NOT NULL DEFAULT NOW(),
    "Reason" TEXT,                    -- Admin's reason for the action (especially for OBO)
    "BeforeState" TEXT,               -- JSON snapshot before change (for critical operations)
    "AfterState" TEXT,                -- JSON snapshot after change (for critical operations)
    
    -- Foreign Keys
    CONSTRAINT "FK_AdminOperationLogs_AdminUser" 
        FOREIGN KEY ("AdminUserId") REFERENCES "Users"("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_AdminOperationLogs_TargetUser" 
        FOREIGN KEY ("TargetUserId") REFERENCES "Users"("UserId") ON DELETE SET NULL
);

-- =============================================================================
-- 2. CREATE INDEXES FOR PERFORMANCE
-- =============================================================================

-- Index for queries by admin user
CREATE INDEX "IX_AdminOperationLogs_AdminUserId" 
    ON "AdminOperationLogs"("AdminUserId");

-- Index for queries by target user (affected user)
CREATE INDEX "IX_AdminOperationLogs_TargetUserId" 
    ON "AdminOperationLogs"("TargetUserId") 
    WHERE "TargetUserId" IS NOT NULL;

-- Index for time-based queries (most common)
CREATE INDEX "IX_AdminOperationLogs_Timestamp" 
    ON "AdminOperationLogs"("Timestamp" DESC);

-- Index for action-based queries
CREATE INDEX "IX_AdminOperationLogs_Action" 
    ON "AdminOperationLogs"("Action");

-- Index for on-behalf-of queries
CREATE INDEX "IX_AdminOperationLogs_IsOnBehalfOf" 
    ON "AdminOperationLogs"("IsOnBehalfOf") 
    WHERE "IsOnBehalfOf" = TRUE;

-- Composite index for common query pattern: admin + time range
CREATE INDEX "IX_AdminOperationLogs_AdminUserId_Timestamp" 
    ON "AdminOperationLogs"("AdminUserId", "Timestamp" DESC);

-- Composite index for target user + time range
CREATE INDEX "IX_AdminOperationLogs_TargetUserId_Timestamp" 
    ON "AdminOperationLogs"("TargetUserId", "Timestamp" DESC)
    WHERE "TargetUserId" IS NOT NULL;

-- =============================================================================
-- 3. COMMENTS FOR DOCUMENTATION
-- =============================================================================

COMMENT ON TABLE "AdminOperationLogs" IS 
'Audit trail for all admin operations including user management, on-behalf-of actions, and system changes';

COMMENT ON COLUMN "AdminOperationLogs"."AdminUserId" IS 
'ID of the admin user who performed the action';

COMMENT ON COLUMN "AdminOperationLogs"."TargetUserId" IS 
'ID of the user affected by the action (null for system-wide operations)';

COMMENT ON COLUMN "AdminOperationLogs"."IsOnBehalfOf" IS 
'True when admin is acting on behalf of another user (farmer/sponsor)';

COMMENT ON COLUMN "AdminOperationLogs"."BeforeState" IS 
'JSON snapshot of entity state before the change (for critical operations only)';

COMMENT ON COLUMN "AdminOperationLogs"."AfterState" IS 
'JSON snapshot of entity state after the change (for critical operations only)';

-- =============================================================================
-- 4. VERIFICATION QUERIES
-- =============================================================================

-- Verify table creation
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = 'AdminOperationLogs'
) AS table_exists;

-- Verify indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'AdminOperationLogs' 
ORDER BY indexname;

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
    AND tc.table_name = 'AdminOperationLogs';

-- =============================================================================
-- 5. ROLLBACK SCRIPT (IN CASE OF ISSUES)
-- =============================================================================

-- ROLLBACK INSTRUCTIONS:
-- If you need to rollback this migration, run the following:

/*
-- Drop indexes first
DROP INDEX IF EXISTS "IX_AdminOperationLogs_TargetUserId_Timestamp";
DROP INDEX IF EXISTS "IX_AdminOperationLogs_AdminUserId_Timestamp";
DROP INDEX IF EXISTS "IX_AdminOperationLogs_IsOnBehalfOf";
DROP INDEX IF EXISTS "IX_AdminOperationLogs_Action";
DROP INDEX IF EXISTS "IX_AdminOperationLogs_Timestamp";
DROP INDEX IF EXISTS "IX_AdminOperationLogs_TargetUserId";
DROP INDEX IF EXISTS "IX_AdminOperationLogs_AdminUserId";

-- Drop table (foreign keys will be automatically dropped)
DROP TABLE IF EXISTS "AdminOperationLogs";
*/

-- =============================================================================
-- MIGRATION COMPLETE
-- =============================================================================
