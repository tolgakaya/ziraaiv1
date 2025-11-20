-- Verify Role Claims Exist and Count Users
-- This script checks if Farmer and Sponsor operation claims exist
-- and counts how many users have each role claim

-- 1. Check if Admin, Farmer, Sponsor claims exist
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Name" IN ('Admin', 'Farmer', 'Sponsor')
ORDER BY "Name";

-- 2. Count users per role claim
SELECT 
    oc."Name" as "RoleName",
    COUNT(DISTINCT uc."UserId") as "UserCount"
FROM "OperationClaims" oc
LEFT JOIN "UserClaims" uc ON oc."Id" = uc."ClaimId"
WHERE oc."Name" IN ('Admin', 'Farmer', 'Sponsor')
GROUP BY oc."Name"
ORDER BY oc."Name";

-- 3. Show all users with their role claims
SELECT 
    u."UserId",
    u."FullName",
    u."Email",
    oc."Name" as "RoleClaim"
FROM "Users" u
INNER JOIN "UserClaims" uc ON u."UserId" = uc."UserId"
INNER JOIN "OperationClaims" oc ON uc."ClaimId" = oc."Id"
WHERE oc."Name" IN ('Admin', 'Farmer', 'Sponsor')
ORDER BY u."UserId", oc."Name";
