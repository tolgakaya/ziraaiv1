-- =============================================
-- FIX: Missing GroupClaims for Admin Sponsor View (Claims 133-139)
-- =============================================
-- Issue: Claims 133-139 exist in OperationClaims but NOT in GroupClaims
-- Reason: SQL script added claims but didn't verify GroupClaims assignment
-- Solution: Add missing GroupClaims entries
-- Date: 2025-11-09
-- =============================================

-- Verification BEFORE fix: Check current state
SELECT
    oc."Id",
    oc."Name",
    gc."GroupId" as "AssignedToGroup"
FROM "OperationClaims" oc
LEFT JOIN "GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Id" BETWEEN 133 AND 139
ORDER BY oc."Id";

-- Expected: Claims exist but GroupId is NULL (not assigned)

-- =============================================
-- FIX: Add Missing GroupClaims
-- =============================================

DO $$
BEGIN
    -- Grant Claim 133 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 133) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 133);
        RAISE NOTICE 'Claim 133 (GetSponsorAnalysesAsAdminQuery) granted to Administrators';
    ELSE
        RAISE NOTICE 'Claim 133 already granted';
    END IF;

    -- Grant Claim 134 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 134) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 134);
        RAISE NOTICE 'Claim 134 (GetSponsorAnalysisDetailAsAdminQuery) granted to Administrators';
    ELSE
        RAISE NOTICE 'Claim 134 already granted';
    END IF;

    -- Grant Claim 135 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 135) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 135);
        RAISE NOTICE 'Claim 135 (GetSponsorMessagesAsAdminQuery) granted to Administrators';
    ELSE
        RAISE NOTICE 'Claim 135 already granted';
    END IF;

    -- Grant Claim 136 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 136) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 136);
        RAISE NOTICE 'Claim 136 (GetNonSponsoredAnalysesQuery) granted to Administrators';
    ELSE
        RAISE NOTICE 'Claim 136 already granted';
    END IF;

    -- Grant Claim 137 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 137) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 137);
        RAISE NOTICE 'Claim 137 (GetNonSponsoredFarmerDetailQuery) granted to Administrators';
    ELSE
        RAISE NOTICE 'Claim 137 already granted';
    END IF;

    -- Grant Claim 138 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 138) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 138);
        RAISE NOTICE 'Claim 138 (GetSponsorshipComparisonAnalyticsQuery) granted to Administrators';
    ELSE
        RAISE NOTICE 'Claim 138 already granted';
    END IF;

    -- Grant Claim 139 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 139) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 139);
        RAISE NOTICE 'Claim 139 (SendMessageAsSponsorCommand) granted to Administrators';
    ELSE
        RAISE NOTICE 'Claim 139 already granted';
    END IF;
END $$;

-- =============================================
-- Verification AFTER fix
-- =============================================

SELECT
    oc."Id",
    oc."Name",
    oc."Alias",
    gc."GroupId",
    g."GroupName"
FROM "OperationClaims" oc
LEFT JOIN "GroupClaims" gc ON oc."Id" = gc."ClaimId"
LEFT JOIN "Group" g ON gc."GroupId" = g."Id"
WHERE oc."Id" BETWEEN 133 AND 139
ORDER BY oc."Id";

-- Expected: All 7 claims should show GroupId = 1, GroupName = 'Administrators'

-- =============================================
-- Admin User Verification
-- =============================================

-- Check admin user (UserId = 159) has these claims via group membership
SELECT
    u."UserId",
    u."FullName",
    u."Email",
    g."GroupName",
    oc."Id" as "ClaimId",
    oc."Name" as "ClaimName"
FROM "Users" u
INNER JOIN "UserGroup" ug ON u."UserId" = ug."UserId"
INNER JOIN "Group" g ON ug."GroupId" = g."Id"
INNER JOIN "GroupClaims" gc ON g."Id" = gc."GroupId"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE u."UserId" = 159
  AND oc."Id" BETWEEN 133 AND 139
ORDER BY oc."Id";

-- Expected: 7 rows showing admin user has all claims 133-139

-- =============================================
-- Root Cause Analysis
-- =============================================
--
-- PROBLEM:
-- 1. ADD_ADMIN_SPONSOR_VIEW_CLAIMS.sql successfully created claims 133-139
-- 2. SAME SQL script ALSO added GroupClaims entries
-- 3. BUT on staging database, GroupClaims entries are MISSING
--
-- POSSIBLE REASONS:
-- A) SQL script was partially executed (claims created, but GroupClaims section failed silently)
-- B) Database transaction was rolled back after claims creation but before GroupClaims
-- C) GroupClaims section has SQL syntax error that was ignored
-- D) Permission issue prevented GroupClaims INSERT
--
-- LESSON LEARNED:
-- Always verify BOTH OperationClaims AND GroupClaims after running migration scripts!
--
-- PREVENTION:
-- 1. Add verification SELECT at end of migration scripts
-- 2. Check BOTH tables in same query
-- 3. Add assertion/check that fails if verification doesn't match expected
--
-- =============================================

-- Post-Fix Action Required:
-- 1. Run this SQL script on staging database
-- 2. Verify all 7 claims show GroupId = 1
-- 3. Have admin user logout and login again (to refresh claims cache)
-- 4. Test endpoint: GET /api/admin/sponsorship/sponsors/159/analyses
-- 5. Expected: 200 OK with data (NOT 401 Unauthorized)
