-- =====================================================
-- Migration: Add BulkDealerInvitationCommand Authorization
-- Date: 2025-01-25
-- Description: Add OperationClaim for bulk dealer invitation endpoint
-- =====================================================

-- =====================================================
-- PART 1: Create Operation Claim
-- =====================================================

-- BulkDealerInvitationCommand
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'BulkDealerInvitationCommand', 'dealer.bulk-invite', 'Create bulk dealer invitations from Excel file'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'BulkDealerInvitationCommand');

-- =====================================================
-- PART 2: Assign Claim to Groups
-- =====================================================

-- Sponsor Group (GroupId = 3)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" = 'BulkDealerInvitationCommand'
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc 
    WHERE gc."GroupId" = 3 AND gc."ClaimId" = oc."Id"
);

-- Admin Group (GroupId = 1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" = 'BulkDealerInvitationCommand'
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc 
    WHERE gc."GroupId" = 1 AND gc."ClaimId" = oc."Id"
);

-- =====================================================
-- PART 3: Verification Queries
-- =====================================================

-- Verify OperationClaim was created
SELECT "Id", "Name", "Alias", "Description"
FROM public."OperationClaims"
WHERE "Name" = 'BulkDealerInvitationCommand';

-- Verify GroupClaims assignment for Sponsor group
SELECT 
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias",
    oc."Description"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 3 
  AND oc."Name" = 'BulkDealerInvitationCommand';

-- Verify GroupClaims assignment for Admin group
SELECT 
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias",
    oc."Description"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 1 
  AND oc."Name" = 'BulkDealerInvitationCommand';

-- Verify all dealer-related claims for Sponsor group
SELECT 
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias"
FROM public."GroupClaims" gc
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 3 
  AND oc."Name" LIKE '%Dealer%'
ORDER BY oc."Name";
