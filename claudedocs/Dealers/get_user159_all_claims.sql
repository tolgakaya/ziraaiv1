-- =====================================================
-- Get ALL claims for User 159 (Sponsor)
-- =====================================================
-- Purpose: See the complete list of claims User 159 should have
-- Date: 2025-10-26
-- =====================================================

-- Get all claims from Sponsor group (GroupId=3)
SELECT oc."Name" as "ClaimName", oc."Alias"
FROM public."GroupClaims" gc
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 3
ORDER BY oc."Name";

-- Look for: TransferCodesToDealerCommand in the results
-- If present in DB but User 159 still gets AuthorizationsDenied,
-- then it's a cache/authentication issue
