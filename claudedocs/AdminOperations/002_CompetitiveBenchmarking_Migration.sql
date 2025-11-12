-- ================================================================
-- Migration: Add Competitive Benchmarking Analytics Endpoint
-- Date: 2025-11-12
-- Feature: Sponsor Advanced Analytics - Competitive Benchmarking
-- ================================================================
--
-- This migration adds the GetCompetitiveBenchmarkingQuery operation claim
-- and assigns it to the Administrators group (GroupId = 1) for admin access.
--
-- Sponsors can also access this endpoint through the Sponsor role check in the controller.
-- ================================================================

-- Step 1: Insert OperationClaim for GetCompetitiveBenchmarkingQuery
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedDate", "UpdatedDate")
VALUES (
    164,
    'GetCompetitiveBenchmarkingQuery',
    'sponsorship.analytics.competitive-benchmarking',
    'View competitive benchmarking analytics comparing sponsor performance with industry averages, percentile rankings, and gap analysis',
    NOW(),
    NOW()
);

-- Step 2: Assign to Administrators Group (GroupId = 1)
INSERT INTO "GroupClaims" ("Id", "GroupId", "ClaimId", "CreatedDate", "UpdatedDate")
VALUES (
    (SELECT COALESCE(MAX("Id"), 0) + 1 FROM "GroupClaims"),
    1, -- Administrators group
    164, -- GetCompetitiveBenchmarkingQuery
    NOW(),
    NOW()
);

-- ================================================================
-- Verification Queries (Run after migration to verify)
-- ================================================================

-- Verify OperationClaim was created
-- SELECT * FROM "OperationClaims" WHERE "Id" = 164;

-- Verify GroupClaim was assigned to Administrators
-- SELECT gc.*, oc."Name", g."Name" as "GroupName"
-- FROM "GroupClaims" gc
-- JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
-- JOIN "Groups" g ON gc."GroupId" = g."Id"
-- WHERE oc."Id" = 164;

-- ================================================================
-- Rollback (if needed)
-- ================================================================

-- DELETE FROM "GroupClaims" WHERE "ClaimId" = 164;
-- DELETE FROM "OperationClaims" WHERE "Id" = 164;
