-- =====================================================
-- Dealer Code Distribution - Authorization Setup
-- =====================================================
-- Description: Creates OperationClaims for dealer management endpoints
--              and assigns them to Sponsor and Admin groups
-- Date: 2025-10-26
-- Phase: 7 - Authorization
-- =====================================================

-- =====================================================
-- PART 1: Create Operation Claims
-- =====================================================

-- Insert dealer management operation claims
-- These will be used by the [SecuredOperation] attribute in handlers

INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
VALUES 
    ('TransferCodesToDealer', 'dealer.transfer', 'Transfer sponsorship codes to dealer'),
    ('CreateDealerInvitation', 'dealer.invite', 'Create dealer invitation (Invite/AutoCreate)'),
    ('ReclaimDealerCodes', 'dealer.reclaim', 'Reclaim unused codes from dealer'),
    ('GetDealerPerformance', 'dealer.analytics', 'View dealer performance analytics'),
    ('GetDealerSummary', 'dealer.summary', 'View all dealers summary'),
    ('GetDealerInvitations', 'dealer.invitations', 'List dealer invitations'),
    ('SearchDealerByEmail', 'dealer.search', 'Search dealer by email')
ON CONFLICT ("Name") DO NOTHING;

-- =====================================================
-- PART 2: Assign Claims to Groups
-- =====================================================

-- Get the claim IDs for the newly created claims
-- Then assign them to both Sponsor (GroupId=3) and Admin (GroupId=1) groups

-- Assign to Sponsor group (GroupId = 3)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, "Id" 
FROM public."OperationClaims" 
WHERE "Name" IN (
    'TransferCodesToDealer',
    'CreateDealerInvitation',
    'ReclaimDealerCodes',
    'GetDealerPerformance',
    'GetDealerSummary',
    'GetDealerInvitations',
    'SearchDealerByEmail'
)
ON CONFLICT ("GroupId", "ClaimId") DO NOTHING;

-- Assign to Admin group (GroupId = 1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, "Id" 
FROM public."OperationClaims" 
WHERE "Name" IN (
    'TransferCodesToDealer',
    'CreateDealerInvitation',
    'ReclaimDealerCodes',
    'GetDealerPerformance',
    'GetDealerSummary',
    'GetDealerInvitations',
    'SearchDealerByEmail'
)
ON CONFLICT ("GroupId", "ClaimId") DO NOTHING;

-- =====================================================
-- PART 3: Verification Queries
-- =====================================================

-- Verify OperationClaims were created
SELECT "Id", "Name", "Alias", "Description"
FROM public."OperationClaims"
WHERE "Name" LIKE '%Dealer%'
ORDER BY "Id";

-- Verify GroupClaims assignments for Sponsor group
SELECT 
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias",
    oc."Description"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 3 -- Sponsor group
  AND oc."Name" LIKE '%Dealer%'
ORDER BY oc."Name";

-- Verify GroupClaims assignments for Admin group
SELECT 
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias",
    oc."Description"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 1 -- Admin group
  AND oc."Name" LIKE '%Dealer%'
ORDER BY oc."Name";

-- =====================================================
-- NOTES:
-- =====================================================
-- 1. These claims should be added via [SecuredOperation] attributes in handlers
-- 2. Controller endpoints already use [Authorize(Roles = "Sponsor,Admin")]
-- 3. ON CONFLICT clauses ensure idempotent execution (safe to run multiple times)
-- 4. Verification queries help confirm successful setup
-- =====================================================
