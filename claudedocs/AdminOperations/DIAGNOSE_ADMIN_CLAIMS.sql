-- Diagnostic Query for Admin Claims Issue
-- Run this to check if claims are properly configured

-- 1. Check if operation claims exist
SELECT 'Step 1: Operation Claims' as checkpoint;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 100 AND 106
ORDER BY "Id";

-- 2. Check if claims are assigned to Administrators group
SELECT 'Step 2: Group Claims Mapping' as checkpoint;
SELECT gc."GroupId", g."GroupName", gc."ClaimId", oc."Name" as "ClaimName"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."ClaimId" BETWEEN 100 AND 106
ORDER BY gc."ClaimId";

-- 3. Check if user with userId=181 is in Administrators group
SELECT 'Step 3: User Group Membership' as checkpoint;
SELECT u."UserId", u."FullName", u."Email", ug."GroupId", g."GroupName"
FROM "Users" u
LEFT JOIN "UserGroups" ug ON u."UserId" = ug."UserId"
LEFT JOIN "Groups" g ON ug."GroupId" = g."Id"
WHERE u."UserId" = 181;

-- 4. Get all claims for userId=181 (what should be in cache)
SELECT 'Step 4: User Claims (Should be in cache)' as checkpoint;
SELECT DISTINCT oc."Id", oc."Name"
FROM "Users" u
JOIN "UserGroups" ug ON u."UserId" = ug."UserId"
JOIN "GroupClaims" gc ON ug."GroupId" = gc."GroupId"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE u."UserId" = 181
ORDER BY oc."Id";

-- 5. Check specifically for GetSubscriptionDetailsQuery claim
SELECT 'Step 5: Specific Claim Check' as checkpoint;
SELECT COUNT(*) as "HasClaim"
FROM "Users" u
JOIN "UserGroups" ug ON u."UserId" = ug."UserId"
JOIN "GroupClaims" gc ON ug."GroupId" = gc."GroupId"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE u."UserId" = 181
  AND oc."Name" = 'GetSubscriptionDetailsQuery';
