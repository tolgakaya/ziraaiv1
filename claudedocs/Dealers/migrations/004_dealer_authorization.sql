-- =====================================================
-- Dealer Code Distribution - Authorization Setup
-- =====================================================
-- Description: Creates OperationClaims for dealer management endpoints
--              and assigns them to Sponsor and Admin groups
-- Date: 2025-10-26
-- Phase: 7 - Authorization
-- =====================================================

-- =====================================================
-- PART 0: Cleanup - Remove Old Incorrect Claims
-- =====================================================

-- Remove old GroupClaims associations (foreign key constraint)
DELETE FROM public."GroupClaims" 
WHERE "ClaimId" IN (
    SELECT "Id" FROM public."OperationClaims"
    WHERE "Name" IN (
        'TransferCodesToDealer',
        'CreateDealerInvitation', 
        'ReclaimDealerCodes',
        'GetDealerPerformance',
        'GetDealerSummary',
        'GetDealerInvitations',
        'SearchDealerByEmail'
    )
);

-- Remove old incorrect OperationClaims
DELETE FROM public."OperationClaims"
WHERE "Name" IN (
    'TransferCodesToDealer',
    'CreateDealerInvitation',
    'ReclaimDealerCodes', 
    'GetDealerPerformance',
    'GetDealerSummary',
    'GetDealerInvitations',
    'SearchDealerByEmail'
);

-- =====================================================
-- PART 1: Create Operation Claims (Correct Names)
-- =====================================================

-- TransferCodesToDealerCommand
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'TransferCodesToDealerCommand', 'dealer.transfer', 'Transfer sponsorship codes to dealer'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'TransferCodesToDealerCommand');

-- CreateDealerInvitationCommand
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'CreateDealerInvitationCommand', 'dealer.invite', 'Create dealer invitation (Invite/AutoCreate)'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'CreateDealerInvitationCommand');

-- ReclaimDealerCodesCommand
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'ReclaimDealerCodesCommand', 'dealer.reclaim', 'Reclaim unused codes from dealer'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'ReclaimDealerCodesCommand');

-- GetDealerPerformanceQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetDealerPerformanceQuery', 'dealer.analytics', 'View dealer performance analytics'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'GetDealerPerformanceQuery');

-- GetDealerSummaryQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetDealerSummaryQuery', 'dealer.summary', 'View all dealers summary'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'GetDealerSummaryQuery');

-- GetDealerInvitationsQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetDealerInvitationsQuery', 'dealer.invitations', 'List dealer invitations'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'GetDealerInvitationsQuery');

-- SearchDealerByEmailQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'SearchDealerByEmailQuery', 'dealer.search', 'Search dealer by email'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'SearchDealerByEmailQuery');

-- =====================================================
-- PART 2: Assign Claims to Groups
-- =====================================================

-- Get the claim IDs for the newly created claims
-- Then assign them to both Sponsor (GroupId=3) and Admin (GroupId=1) groups

-- Assign to Sponsor group (GroupId = 3)
-- Using INSERT ... WHERE NOT EXISTS for compatibility
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" IN (
    'TransferCodesToDealerCommand',
    'CreateDealerInvitationCommand',
    'ReclaimDealerCodesCommand',
    'GetDealerPerformanceQuery',
    'GetDealerSummaryQuery',
    'GetDealerInvitationsQuery',
    'SearchDealerByEmailQuery'
)
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc 
    WHERE gc."GroupId" = 3 AND gc."ClaimId" = oc."Id"
);

-- Assign to Admin group (GroupId = 1)
-- Using INSERT ... WHERE NOT EXISTS for compatibility
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" IN (
    'TransferCodesToDealerCommand',
    'CreateDealerInvitationCommand',
    'ReclaimDealerCodesCommand',
    'GetDealerPerformanceQuery',
    'GetDealerSummaryQuery',
    'GetDealerInvitationsQuery',
    'SearchDealerByEmailQuery'
)
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc 
    WHERE gc."GroupId" = 1 AND gc."ClaimId" = oc."Id"
);

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
-- 3. WHERE NOT EXISTS clauses ensure idempotent execution (safe to run multiple times)
-- 4. Verification queries help confirm successful setup
-- =====================================================
