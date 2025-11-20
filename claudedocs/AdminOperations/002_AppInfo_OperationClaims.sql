-- =====================================================
-- App Info - Operation Claims
-- =====================================================
-- Purpose: Add operation claims for App Info (About Us) endpoints
-- Date: 2025-11-18
-- Branch: feature/landing-page-planning
-- Related: UpdateAppInfoCommand, GetAppInfoQuery, GetAppInfoAsAdminQuery
-- =====================================================
--
-- GroupId Reference:
--   1 = Administrators
--   2 = Farmers
--   3 = Sponsors
--
-- Claim IDs: 180-182 (after Ticketing System claims 168-179)
--
-- Endpoints:
--   GET /api/v1/appinfo - Farmer/Sponsor view (Claim 180)
--   GET /api/admin/appinfo - Admin view with metadata (Claim 181)
--   PUT /api/admin/appinfo - Admin update (Claim 182)
-- =====================================================

-- =====================================================
-- PART 0: Pre-Flight Analysis
-- =====================================================

-- Check next available claim ID
SELECT MAX("Id") + 1 as "NextAvailableClaimId"
FROM public."OperationClaims";

-- =====================================================
-- PART 1: Create Operation Claims
-- =====================================================

-- Claim 180: GetAppInfoQuery (Farmer/Sponsor view)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 180, 'GetAppInfoQuery', 'appinfo.get', 'View app info (About Us page)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetAppInfoQuery'
);

-- Claim 181: GetAppInfoAsAdminQuery (Admin view with metadata)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 181, 'GetAppInfoAsAdminQuery', 'appinfo.admin.get', 'View app info with admin metadata'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetAppInfoAsAdminQuery'
);

-- Claim 182: UpdateAppInfoCommand (Admin update)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 182, 'UpdateAppInfoCommand', 'appinfo.admin.update', 'Update app info (About Us page)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'UpdateAppInfoCommand'
);

-- =====================================================
-- PART 2: Assign Claims to Groups
-- =====================================================

-- GetAppInfoQuery (180) -> Farmers (2) and Sponsors (3)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 2, 180
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 2 AND "ClaimId" = 180
);

INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, 180
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 3 AND "ClaimId" = 180
);

-- GetAppInfoAsAdminQuery (181) -> Administrators (1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 181
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 1 AND "ClaimId" = 181
);

-- UpdateAppInfoCommand (182) -> Administrators (1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 182
WHERE NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 1 AND "ClaimId" = 182
);

-- =====================================================
-- PART 3: Verification
-- =====================================================

-- Verify claims created
SELECT
    oc."Id",
    oc."Name",
    oc."Alias",
    oc."Description"
FROM public."OperationClaims" oc
WHERE oc."Id" IN (180, 181, 182)
ORDER BY oc."Id";

-- Verify group assignments
SELECT
    gc."GroupId",
    g."GroupName",
    gc."ClaimId",
    oc."Name" as "ClaimName",
    oc."Alias"
FROM public."GroupClaims" gc
JOIN public."Group" g ON gc."GroupId" = g."Id"
JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Id" IN (180, 181, 182)
ORDER BY gc."ClaimId", gc."GroupId";

-- =====================================================
-- PART 4: Summary Report
-- =====================================================

-- App Info claims summary
SELECT
    'AppInfo Claims' as "Feature",
    COUNT(DISTINCT oc."Id") as "TotalClaims",
    COUNT(DISTINCT gc."GroupId") as "GroupsAssigned"
FROM public."OperationClaims" oc
LEFT JOIN public."GroupClaims" gc ON oc."Id" = gc."ClaimId"
WHERE oc."Alias" LIKE 'appinfo%';

-- =====================================================
-- EXECUTION NOTES
-- =====================================================
--
-- 1. Run this script on the database (staging first, then production)
-- 2. After execution, users must logout/login to refresh claims
-- 3. Verify endpoints work:
--    - GET /api/v1/appinfo (Farmer/Sponsor token)
--    - GET /api/admin/appinfo (Admin token)
--    - PUT /api/admin/appinfo (Admin token)
--
-- =====================================================
-- ROLLBACK (if needed)
-- =====================================================
--
-- To remove the claims and assignments:
--
-- DELETE FROM public."GroupClaims" WHERE "ClaimId" IN (180, 181, 182);
-- DELETE FROM public."OperationClaims" WHERE "Id" IN (180, 181, 182);
--
-- =====================================================
