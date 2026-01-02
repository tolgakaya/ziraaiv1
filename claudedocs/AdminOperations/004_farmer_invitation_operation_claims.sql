-- =====================================================
-- Farmer Invitation System Operation Claims Migration
-- =====================================================
-- Purpose: Add operation claims for farmer invitation endpoints
-- Date: 2026-01-02
-- Handlers: 2 Commands + 3 Queries = 5 total
-- Target Groups:
--   - Sponsors (GroupId = 3) - Can create invitations and view their own
--   - Farmers (GroupId = 2) - Can accept invitations and view pending invitations
--   - Administrators (GroupId = 1) - Full access to all invitations
-- Claim IDs: 189-193 (verified no conflicts with existing claims)
-- Previous max ID: 188 (from operation_claims.csv: ProcessPaymentWebhook)
-- =====================================================

-- =====================================================
-- PART 0: Pre-Flight Checks
-- =====================================================

-- Check current max claim ID to verify no conflicts
SELECT
    MAX("Id") as "CurrentMaxClaimId",
    'Expected: 188 or lower. If higher than 188, STOP and adjust claim IDs!' as "Warning"
FROM public."OperationClaims";

-- Check if any of our target IDs (189-193) already exist
SELECT
    "Id",
    "Name",
    'CONFLICT DETECTED! This ID is already in use!' as "Error"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 189 AND 193;
-- Expected: 0 rows returned. If any rows appear, STOP and adjust claim IDs!

-- =====================================================
-- PART 1: FarmerInvitation Commands (2 handlers)
-- =====================================================

-- CreateFarmerInvitationCommand (ClaimId: 189)
-- Allows sponsors to create farmer invitations with SMS delivery
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 189, 'CreateFarmerInvitationCommand', 'sponsor.farmer-invitations.create', 'Create farmer invitation with code reservation and SMS delivery'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'CreateFarmerInvitationCommand'
);

-- AcceptFarmerInvitationCommand (ClaimId: 190)
-- Allows farmers to accept invitations (phone verification required)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 190, 'AcceptFarmerInvitationCommand', 'farmer.invitations.accept', 'Accept farmer invitation with phone verification and code assignment'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'AcceptFarmerInvitationCommand'
);

-- =====================================================
-- PART 2: FarmerInvitation Queries (2 handlers)
-- =====================================================

-- GetFarmerInvitationsQuery (ClaimId: 191)
-- Allows sponsors to view their own invitations, admins to view all
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 191, 'GetFarmerInvitationsQuery', 'sponsor.farmer-invitations.list', 'Get list of farmer invitations for a sponsor with status filtering'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetFarmerInvitationsQuery'
);

-- GetFarmerInvitationByTokenQuery (ClaimId: 192)
-- Public endpoint (AllowAnonymous) for unregistered users to view invitation details
-- No claim assignment needed as this is a public endpoint
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 192, 'GetFarmerInvitationByTokenQuery', 'public.farmer-invitations.detail', 'Get farmer invitation details by token (public endpoint for unregistered users)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetFarmerInvitationByTokenQuery'
);

-- GetPendingFarmerInvitationsByPhoneQuery (ClaimId: 193)
-- Allows farmers to view their own pending invitations
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 193, 'GetPendingFarmerInvitationsByPhoneQuery', 'farmer.invitations.my-invitations', 'Get pending farmer invitations for authenticated farmer by phone'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetPendingFarmerInvitationsByPhoneQuery'
);

-- =====================================================
-- PART 3: Assign Claims to Groups
-- =====================================================

-- Assign CreateFarmerInvitationCommand (189) to Sponsors group (GroupId = 3)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, 189
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 3 AND "ClaimId" = 189
);

-- Assign AcceptFarmerInvitationCommand (190) to Farmers group (GroupId = 2)
-- Note: Farmers automatically get their group claims through GroupId = 2
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 2, 190
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 2 AND "ClaimId" = 190
);

-- Assign GetFarmerInvitationsQuery (191) to Sponsors group (GroupId = 3)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, 191
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 3 AND "ClaimId" = 191
);

-- Assign GetPendingFarmerInvitationsByPhoneQuery (193) to Farmers group (GroupId = 2)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 2, 193
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 2 AND "ClaimId" = 193
);

-- GetFarmerInvitationByTokenQuery (192) is AllowAnonymous - No group assignment needed
-- This is a public endpoint accessible without authentication

-- Assign all claims (189-193) to Administrators group (GroupId = 1) for full access
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Id" BETWEEN 189 AND 193
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc
    WHERE gc."GroupId" = 1 AND gc."ClaimId" = oc."Id"
);

-- =====================================================
-- PART 4: Verification Queries
-- =====================================================

-- Verify all 5 OperationClaims were created
SELECT "Id", "Name", "Alias", "Description"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 189 AND 193
ORDER BY "Id";
-- Expected: 5 rows

-- Verify group assignments
SELECT
    g."GroupName",
    oc."Id" as "ClaimId",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Id" BETWEEN 189 AND 193
ORDER BY oc."Id", g."GroupName";
-- Expected:
-- ClaimId 189: Administrators, Sponsors
-- ClaimId 190: Administrators, Farmers
-- ClaimId 191: Administrators, Sponsors
-- ClaimId 192: Administrators (public endpoint, group assignment is informational)
-- ClaimId 193: Administrators, Farmers

-- Verify Sponsors group has invitation creation capability
SELECT
    oc."Id" as "ClaimId",
    oc."Name" as "ClaimName"
FROM public."GroupClaims" gc
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 3  -- Sponsors group
  AND oc."Id" IN (189, 191)  -- Create and List claims
ORDER BY oc."Id";
-- Expected: 2 rows (189, 191)

-- Verify Farmers group has invitation acceptance capability
SELECT
    oc."Id" as "ClaimId",
    oc."Name" as "ClaimName"
FROM public."GroupClaims" gc
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 2  -- Farmers group
  AND oc."Id" IN (190, 193)  -- Accept and MyInvitations claims
ORDER BY oc."Id";
-- Expected: 2 rows (190, 193)

-- =====================================================
-- PART 5: Claim ID Summary
-- =====================================================

-- FarmerInvitation Commands (2 claims): 189-190
-- 189: CreateFarmerInvitationCommand (Sponsors + Admins)
-- 190: AcceptFarmerInvitationCommand (Farmers + Admins)

-- FarmerInvitation Queries (3 claims): 191-193
-- 191: GetFarmerInvitationsQuery (Sponsors + Admins)
-- 192: GetFarmerInvitationByTokenQuery (Public/AllowAnonymous + Admins for tracking)
-- 193: GetPendingFarmerInvitationsByPhoneQuery (Farmers + Admins)

-- Total: 5 operation claims (189-193)
-- Group assignments:
--   - Administrators (GroupId = 1): All 5 claims
--   - Sponsors (GroupId = 3): 2 claims (189, 191)
--   - Farmers (GroupId = 2): 2 claims (190, 193)
