-- =====================================================
-- Bulk Farmer Invitation Operation Claims Migration
-- =====================================================
-- Purpose: Add operation claims for BULK farmer invitation endpoints
-- Date: 2026-01-03
-- Handlers: 2 Commands (BulkCreateFarmerInvitationsCommand, AdminBulkCreateFarmerInvitationsCommand)
-- Target Groups:
--   - Sponsors (GroupId = 3) - Can create bulk invitations
--   - Administrators (GroupId = 1) - Can create bulk invitations on behalf of sponsors
-- Claim IDs: 194-195
-- Previous max ID: 193 (from 005_add_farmer_my_invitations_claim.sql)
-- =====================================================

-- =====================================================
-- PART 0: Pre-Flight Checks
-- =====================================================

-- Check current max claim ID to verify no conflicts
SELECT
    MAX("Id") as "CurrentMaxClaimId",
    'Expected: 193 or lower. If higher than 193, STOP and adjust claim IDs!' as "Warning"
FROM public."OperationClaims";

-- Check if any of our target IDs (194-195) already exist
SELECT
    "Id",
    "Name",
    'CONFLICT DETECTED! This ID is already in use!' as "Error"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 194 AND 195;
-- Expected: 0 rows returned. If any rows appear, STOP and adjust claim IDs!

-- =====================================================
-- PART 1: Bulk FarmerInvitation Commands (2 handlers)
-- =====================================================

-- BulkCreateFarmerInvitationsCommand (ClaimId: 194)
-- Allows sponsors to create bulk farmer invitations (Excel upload support)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 194, 'BulkCreateFarmerInvitationsCommand', 'sponsor.farmer-invitations.bulk-create', 'Create bulk farmer invitations with code reservation and SMS/WhatsApp delivery (Excel upload)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'BulkCreateFarmerInvitationsCommand'
);

-- AdminBulkCreateFarmerInvitationsCommand (ClaimId: 195)
-- Allows admins to create bulk farmer invitations on behalf of sponsors
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 195, 'AdminBulkCreateFarmerInvitationsCommand', 'admin.farmer-invitations.bulk-create-on-behalf', 'Admin: Create bulk farmer invitations on behalf of sponsor with audit logging'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'AdminBulkCreateFarmerInvitationsCommand'
);

-- =====================================================
-- PART 2: Assign Claims to Groups
-- =====================================================

-- Assign BulkCreateFarmerInvitationsCommand (194) to Sponsors group (GroupId = 3)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, 194
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 3 AND "ClaimId" = 194
);

-- Assign BulkCreateFarmerInvitationsCommand (194) to Administrators group (GroupId = 1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 194
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 1 AND "ClaimId" = 194
);

-- Assign AdminBulkCreateFarmerInvitationsCommand (195) to Administrators group (GroupId = 1) ONLY
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 195
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 1 AND "ClaimId" = 195
);

-- =====================================================
-- PART 3: Verification Queries
-- =====================================================

-- Verify all operation claims were created
SELECT
    "Id",
    "Name",
    "Alias",
    "Description"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 194 AND 195
ORDER BY "Id";
-- Expected: 2 rows (IDs 194-195)

-- Verify group assignments
SELECT
    gc."GroupId",
    g."Name" as "GroupName",
    gc."ClaimId",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias"
FROM public."GroupClaims" gc
INNER JOIN public."Groups" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."ClaimId" BETWEEN 194 AND 195
ORDER BY gc."ClaimId", gc."GroupId";
-- Expected: 4 rows
--   194 → Sponsors (GroupId = 3)
--   194 → Administrators (GroupId = 1)
--   195 → Administrators (GroupId = 1)

-- Verify complete farmer invitation claim chain (189-195)
SELECT
    "Id",
    "Name",
    "Alias",
    "Description"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 189 AND 195
ORDER BY "Id";
-- Expected: 7 rows (complete farmer invitation system)

-- =====================================================
-- PART 4: Summary
-- =====================================================

SELECT
    COUNT(*) FILTER (WHERE "Id" BETWEEN 189 AND 195) as "TotalFarmerInvitationClaims",
    COUNT(*) FILTER (WHERE "Id" BETWEEN 189 AND 193) as "IndividualInvitationClaims",
    COUNT(*) FILTER (WHERE "Id" BETWEEN 194 AND 195) as "BulkInvitationClaims",
    MAX("Id") as "NewMaxClaimId"
FROM public."OperationClaims";
-- Expected: 7 total (5 individual + 2 bulk), NewMaxClaimId = 195

-- =====================================================
-- ROLLBACK INSTRUCTIONS (if needed)
-- =====================================================
/*
-- ROLLBACK: Remove group claim assignments
DELETE FROM public."GroupClaims" WHERE "ClaimId" IN (194, 195);

-- ROLLBACK: Remove operation claims
DELETE FROM public."OperationClaims" WHERE "Id" IN (194, 195);

-- VERIFICATION: Confirm rollback
SELECT COUNT(*) FROM public."OperationClaims" WHERE "Id" BETWEEN 194 AND 195;
-- Expected: 0 rows
*/
