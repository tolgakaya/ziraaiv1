-- ================================================================
-- Migration: Add Farmer Journey Analytics Endpoint
-- Date: 2025-11-12
-- Feature: Sponsor Advanced Analytics - Farmer Journey
-- ================================================================
--
-- This migration adds the GetFarmerJourneyQuery operation claim
-- and assigns it to the Administrators group (GroupId = 1) and Sponsors group (GroupId = 3)
--
-- Endpoint: GET /api/sponsorship/farmer-journey?farmerId={id}
-- Purpose: Complete lifecycle analytics from code redemption through ongoing engagement
-- ================================================================

-- Step 1: Insert OperationClaim for GetFarmerJourneyQuery
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES (
    165,
    'GetFarmerJourneyQuery',
    'sponsorship.analytics.farmer-journey',
    'View complete farmer journey analytics including timeline, behavioral patterns, and AI-driven recommendations'
);

-- Step 2: Assign to Administrators Group (GroupId = 1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES (
    1, -- Administrators group
    165 -- GetFarmerJourneyQuery
);

-- Step 3: Assign to Sponsors Group (GroupId = 3)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES (
    3, -- Sponsors group
    165 -- GetFarmerJourneyQuery
);

-- ================================================================
-- Verification Queries (Run after migration to verify)
-- ================================================================

-- Verify OperationClaim was created
-- SELECT * FROM "OperationClaims" WHERE "Id" = 165;

-- Verify GroupClaim was assigned to Administrators and Sponsors
-- SELECT gc.*, oc."Name", g."Name" as "GroupName"
-- FROM "GroupClaims" gc
-- JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
-- JOIN "Groups" g ON gc."GroupId" = g."Id"
-- WHERE oc."Id" = 165;

-- ================================================================
-- Rollback (if needed)
-- ================================================================

-- DELETE FROM "GroupClaims" WHERE "ClaimId" = 165;
-- DELETE FROM "OperationClaims" WHERE "Id" = 165;
