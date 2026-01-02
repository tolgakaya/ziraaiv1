-- =====================================================
-- Farmer Invitation System - Add Missing Claim
-- =====================================================
-- Purpose: Add GetPendingFarmerInvitationsByPhoneQuery claim (ID 193)
-- Date: 2026-01-02
-- This is a supplementary migration for the farmer endpoint to view pending invitations
-- Previous migration: 004_farmer_invitation_operation_claims.sql (IDs 189-192)
-- =====================================================

-- =====================================================
-- PART 0: Pre-Flight Check
-- =====================================================

-- Check current max claim ID
SELECT
    MAX("Id") as "CurrentMaxClaimId",
    'Expected: 192 or lower. If higher than 192, STOP!' as "Warning"
FROM public."OperationClaims";

-- Check if ID 193 already exists
SELECT
    "Id",
    "Name",
    'CONFLICT DETECTED! ID 193 is already in use!' as "Error"
FROM public."OperationClaims"
WHERE "Id" = 193;
-- Expected: 0 rows returned. If any rows appear, STOP!

-- =====================================================
-- PART 1: Add Missing Operation Claim
-- =====================================================

-- GetPendingFarmerInvitationsByPhoneQuery (ClaimId: 193)
-- Allows farmers to view their own pending invitations
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 193, 'GetPendingFarmerInvitationsByPhoneQuery', 'farmer.invitations.my-invitations', 'Get pending farmer invitations for authenticated farmer by phone'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetPendingFarmerInvitationsByPhoneQuery'
);

-- =====================================================
-- PART 2: Assign Claim to Groups
-- =====================================================

-- Assign to Farmers group (GroupId = 2)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 2, 193
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 2 AND "ClaimId" = 193
);

-- Assign to Administrators group (GroupId = 1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 193
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 1 AND "ClaimId" = 193
);

-- =====================================================
-- PART 3: Verification Queries
-- =====================================================

-- Verify the new claim was created
SELECT "Id", "Name", "Alias", "Description"
FROM public."OperationClaims"
WHERE "Id" = 193;
-- Expected: 1 row

-- Verify group assignments for claim 193
SELECT
    g."GroupName",
    oc."Id" as "ClaimId",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Id" = 193
ORDER BY g."GroupName";
-- Expected: 2 rows (Administrators, Farmers)

-- Verify Farmers group now has both invitation claims
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
-- PART 4: Summary
-- =====================================================

-- NEW CLAIM ADDED:
-- ID 193: GetPendingFarmerInvitationsByPhoneQuery (Farmers + Admins)
--
-- COMPLETE FARMER INVITATION CLAIMS (189-193):
-- 189: CreateFarmerInvitationCommand (Sponsors + Admins)
-- 190: AcceptFarmerInvitationCommand (Farmers + Admins)
-- 191: GetFarmerInvitationsQuery (Sponsors + Admins)
-- 192: GetFarmerInvitationByTokenQuery (Public/AllowAnonymous + Admins)
-- 193: GetPendingFarmerInvitationsByPhoneQuery (Farmers + Admins) ← NEW
