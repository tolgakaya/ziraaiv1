-- Count total claims for Sponsor group
SELECT COUNT(*) as "TotalClaims"
FROM public."GroupClaims" gc
WHERE gc."GroupId" = 3;

-- List all claim names
SELECT oc."Name"
FROM public."GroupClaims" gc
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 3
ORDER BY oc."Name";
