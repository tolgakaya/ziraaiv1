-- =====================================================
-- Check User 159's Group Membership
-- =====================================================
-- Purpose: Verify that User 159 is in Sponsor group (GroupId=3)
-- Date: 2025-10-26
-- =====================================================

-- Check which groups User 159 belongs to
SELECT
    u."UserId",
    u."FirstName",
    u."LastName",
    g."Id" as "GroupId",
    g."GroupName"
FROM public."Users" u
INNER JOIN public."UserGroups" ug ON u."UserId" = ug."UserId"
INNER JOIN "Groups" g ON ug."GroupId" = g."Id"
WHERE u."UserId" = 159
ORDER BY g."Id";

-- Expected Result:
-- UserId | FirstName | LastName | GroupId | GroupName
-- -------|-----------|----------|---------|----------
-- 159    | User      | 1114     | 3       | Sponsor

-- If GroupId 3 (Sponsor) is missing:
-- User 159 is NOT in Sponsor group, need to add manually
