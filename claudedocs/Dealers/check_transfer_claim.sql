-- =====================================================
-- Check if TransferCodesToDealerCommand is assigned to Sponsor group
-- =====================================================
-- Purpose: Verify that the TransferCodesToDealerCommand claim
--          exists and is properly assigned to Sponsor group (GroupId=3)
-- Date: 2025-10-26
-- =====================================================

SELECT
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 3 -- Sponsor group
  AND oc."Name" LIKE '%Transfer%'
ORDER BY oc."Name";

-- Expected Result:
-- GroupName | ClaimName                    | ClaimAlias
-- ----------|------------------------------|---------------
-- Sponsor   | TransferCodesToDealerCommand | dealer.transfer

-- If empty result:
-- The claim is not assigned to Sponsor group, need to run migration 004_dealer_authorization.sql
