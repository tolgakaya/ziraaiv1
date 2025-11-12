-- ============================================================================
-- Admin Bulk Subscription Assignment - Operation Claims
-- ============================================================================
-- Script: 002_admin_bulk_subscription_operation_claims.sql
-- Purpose: Add operation claims for admin bulk subscription assignment endpoints
-- Date: 2025-01-10
-- Author: Claude Code
--
-- Claims Structure:
-- - 159: QueueBulkSubscriptionAssignmentCommand (POST bulk assignment)
-- - 160: GetBulkSubscriptionAssignmentStatusQuery (GET job status)
-- - 161: GetBulkSubscriptionAssignmentHistoryQuery (GET job history)
-- - 162: GetBulkSubscriptionAssignmentResultQuery (GET result file)
--
-- Assignment: All claims assigned to GroupId 1 (Administrators)
-- ============================================================================

-- ============================================================================
-- 1. INSERT OPERATION CLAIMS
-- ============================================================================
-- IMPORTANT: Uses WHERE NOT EXISTS to prevent duplicates (NO ON CONFLICT)

-- Claim 159: Queue Bulk Subscription Assignment
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    159,
    'QueueBulkSubscriptionAssignmentCommand',
    'Queue Bulk Subscription Assignment',
    'Allows admin to upload Excel file and queue bulk subscription assignments for farmers'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 159
);

-- Claim 160: Get Bulk Subscription Assignment Status
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    160,
    'GetBulkSubscriptionAssignmentStatusQuery',
    'Get Bulk Subscription Assignment Status',
    'Allows admin to check status and progress of bulk subscription assignment job'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 160
);

-- Claim 161: Get Bulk Subscription Assignment History
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    161,
    'GetBulkSubscriptionAssignmentHistoryQuery',
    'Get Bulk Subscription Assignment History',
    'Allows admin to view historical bulk subscription assignment jobs'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 161
);

-- Claim 162: Get Bulk Subscription Assignment Result
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    162,
    'GetBulkSubscriptionAssignmentResultQuery',
    'Get Bulk Subscription Assignment Result',
    'Allows admin to download result file with success/failure details'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 162
);

-- ============================================================================
-- 2. ASSIGN TO ADMINISTRATORS GROUP (GroupId = 1)
-- ============================================================================

-- Assign Claim 159 to Administrators
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 159
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 159
);

-- Assign Claim 160 to Administrators
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 160
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 160
);

-- Assign Claim 161 to Administrators
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 161
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 161
);

-- Assign Claim 162 to Administrators
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 162
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 162
);

-- ============================================================================
-- 3. VERIFICATION QUERIES
-- ============================================================================

-- Verify claims were inserted
SELECT
    "Id",
    "Name",
    "Alias",
    "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 159 AND 162
ORDER BY "Id";

-- Verify group assignments
SELECT
    gc."GroupId",
    g."Name" AS "GroupName",
    gc."ClaimId",
    oc."Name" AS "ClaimName",
    oc."Alias"
FROM "GroupClaims" gc
INNER JOIN "Groups" g ON gc."GroupId" = g."Id"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."ClaimId" BETWEEN 159 AND 162
ORDER BY gc."ClaimId";

-- ============================================================================
-- 4. TEST VERIFICATION (FOR ADMIN USERS)
-- ============================================================================

-- List all admin users who will inherit these claims
SELECT DISTINCT
    u."Id" AS "UserId",
    u."Email",
    u."FirstName",
    u."LastName",
    g."Name" AS "GroupName",
    oc."Id" AS "ClaimId",
    oc."Name" AS "ClaimName"
FROM "Users" u
INNER JOIN "UserGroups" ug ON u."Id" = ug."UserId"
INNER JOIN "Groups" g ON ug."GroupId" = g."Id"
INNER JOIN "GroupClaims" gc ON g."Id" = gc."GroupId"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 1  -- Administrators group
  AND oc."Id" BETWEEN 159 AND 162
ORDER BY u."Email", oc."Id";

-- ============================================================================
-- 5. ROLLBACK SCRIPT (IF NEEDED - DO NOT EXECUTE UNLESS REQUIRED)
-- ============================================================================

/*
-- Remove GroupClaims assignments
DELETE FROM "GroupClaims"
WHERE "ClaimId" BETWEEN 159 AND 162 AND "GroupId" = 1;

-- Remove OperationClaims
DELETE FROM "OperationClaims"
WHERE "Id" BETWEEN 159 AND 162;
*/

-- ============================================================================
-- IMPORTANT REMINDERS:
-- ============================================================================
-- 1. ⚠️ Admin users must LOGOUT and LOGIN after this script to refresh claims cache
-- 2. 📋 Verify the handler names match exactly:
--    - QueueBulkSubscriptionAssignmentCommandHandler → QueueBulkSubscriptionAssignmentCommand
--    - GetBulkSubscriptionAssignmentStatusQueryHandler → GetBulkSubscriptionAssignmentStatusQuery
--    - GetBulkSubscriptionAssignmentHistoryQueryHandler → GetBulkSubscriptionAssignmentHistoryQuery
--    - GetBulkSubscriptionAssignmentResultQueryHandler → GetBulkSubscriptionAssignmentResultQuery
-- 3. 🔒 GroupId 1 = Administrators - confirm this is correct in your database
-- 4. ✅ Claims 159-162 must be unused - verify no conflicts with existing claims
-- ============================================================================

-- End of script
