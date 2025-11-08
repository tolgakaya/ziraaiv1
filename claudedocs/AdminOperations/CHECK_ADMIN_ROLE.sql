-- Check if userId=181 has Admin role in JWT token
-- This checks the authorization setup for admin endpoints

-- 1. Check user's groups (GroupClaims-based authorization)
SELECT 'Step 1: User Groups' as checkpoint;
SELECT
    u."UserId",
    u."FullName",
    u."Email",
    g."Id" as "GroupId",
    g."GroupName"
FROM "Users" u
LEFT JOIN "UserGroups" ug ON u."UserId" = ug."UserId"
LEFT JOIN "Groups" g ON ug."GroupId" = g."Id"
WHERE u."UserId" = 181;

-- 2. Check if "Admin" exists as a ROLE (for [Authorize(Roles = "Admin")])
SELECT 'Step 2: Admin Role Check' as checkpoint;
SELECT *
FROM "Groups"
WHERE "GroupName" = 'Admin' OR "GroupName" = 'Administrators';

-- 3. Expected: User should be in "Administrators" group (GroupId = 1)
-- Problem: Controller uses [Authorize(Roles = "Admin")]
-- Solution: Either:
--   A) Change group name from "Administrators" to "Admin"
--   B) Change controller attribute to [Authorize(Roles = "Administrators")]
--   C) Add user to a group named "Admin"

-- 4. Check what roles are in JWT token for this user
-- JWT roles come from Groups table via UserGroups
-- If user is in "Administrators" group, JWT will have "Administrators" role
-- But controller expects "Admin" role
