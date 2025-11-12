-- =====================================================
-- Manual Migration: Create BulkSubscriptionAssignmentJobs Table
-- =====================================================
-- Purpose: Admin bulk subscription assignment to farmers via Excel upload
-- Date: 2025-11-10
-- Related: BulkSubscriptionAssignmentJob entity
-- =====================================================

CREATE TABLE IF NOT EXISTS "BulkSubscriptionAssignmentJobs" (
    "Id" SERIAL PRIMARY KEY,

    -- Admin Information
    "AdminId" INTEGER NOT NULL,

    -- Configuration - Optional Defaults
    "DefaultTierId" INTEGER NULL,
    "DefaultDurationDays" INTEGER NULL,
    "SendNotification" BOOLEAN NOT NULL,
    "NotificationMethod" VARCHAR(50) NOT NULL DEFAULT 'Email',
    "AutoActivate" BOOLEAN NOT NULL DEFAULT true,

    -- Progress Tracking
    "TotalFarmers" INTEGER NOT NULL,
    "ProcessedFarmers" INTEGER NOT NULL DEFAULT 0,
    "SuccessfulAssignments" INTEGER NOT NULL DEFAULT 0,
    "FailedAssignments" INTEGER NOT NULL DEFAULT 0,

    -- Status: Pending, Processing, Completed, PartialSuccess, Failed
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',

    -- Timestamps
    "CreatedDate" TIMESTAMP NOT NULL,
    "StartedDate" TIMESTAMP NULL,
    "CompletedDate" TIMESTAMP NULL,

    -- File Information
    "OriginalFileName" VARCHAR(500) NOT NULL,
    "FileSize" INTEGER NOT NULL,

    -- Results
    "ResultFileUrl" VARCHAR(1000) NULL,
    "ErrorSummary" TEXT NULL,

    -- Statistics
    "NewSubscriptionsCreated" INTEGER NOT NULL DEFAULT 0,
    "ExistingSubscriptionsUpdated" INTEGER NOT NULL DEFAULT 0,
    "TotalNotificationsSent" INTEGER NOT NULL DEFAULT 0
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS "IX_BulkSubscriptionAssignmentJobs_AdminId"
    ON "BulkSubscriptionAssignmentJobs"("AdminId");

CREATE INDEX IF NOT EXISTS "IX_BulkSubscriptionAssignmentJobs_Status"
    ON "BulkSubscriptionAssignmentJobs"("Status");

CREATE INDEX IF NOT EXISTS "IX_BulkSubscriptionAssignmentJobs_CreatedDate"
    ON "BulkSubscriptionAssignmentJobs"("CreatedDate");

CREATE INDEX IF NOT EXISTS "IX_BulkSubscriptionAssignmentJobs_AdminId_CreatedDate"
    ON "BulkSubscriptionAssignmentJobs"("AdminId", "CreatedDate");

-- =====================================================
-- Verification Queries
-- =====================================================

-- Verify table created
SELECT
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'BulkSubscriptionAssignmentJobs'
ORDER BY ordinal_position;

-- Verify indexes created
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'BulkSubscriptionAssignmentJobs';

-- Test insert (rollback after test)
BEGIN;
INSERT INTO "BulkSubscriptionAssignmentJobs" (
    "AdminId",
    "SendNotification",
    "NotificationMethod",
    "AutoActivate",
    "TotalFarmers",
    "CreatedDate",
    "OriginalFileName",
    "FileSize"
) VALUES (
    1,
    true,
    'Email',
    true,
    0,
    NOW(),
    'test.xlsx',
    1024
);
SELECT * FROM "BulkSubscriptionAssignmentJobs" WHERE "AdminId" = 1;
ROLLBACK;
