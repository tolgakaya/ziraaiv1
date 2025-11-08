-- =====================================================
-- Add GetAllSponsorsQuery Operation Claim
-- =====================================================
-- Handler: GetAllSponsorsQueryHandler
-- Claim Name: GetAllSponsorsQuery (Handler suffix removed)
-- Purpose: Query all users with Sponsor role (GroupId = 3)
-- Endpoint: GET /api/admin/sponsorship/sponsors

-- =====================================================
-- PART 1: Create Operation Claim
-- =====================================================

-- Insert GetAllSponsorsQuery claim with ID 107
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 107, 'GetAllSponsorsQuery', 'Get All Sponsors', 'Query all users with Sponsor role (GroupId = 3)'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Name" = 'GetAllSponsorsQuery'
);

-- =====================================================
-- PART 2: Assign Claim to Administrators Group
-- =====================================================

-- Assign to Administrators group (GroupId = 1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 107
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 107
);

-- =====================================================
-- PART 3: Verification Queries
-- =====================================================

-- Verify OperationClaim was created
SELECT 'VERIFICATION: OperationClaim Created' as info;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" = 107;

-- Verify GroupClaim assignment to Administrators
SELECT 'VERIFICATION: Assigned to Administrators Group' as info;
SELECT
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias",
    oc."Description"
FROM "GroupClaims" gc
INNER JOIN "Groups" g ON gc."GroupId" = g."Id"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."ClaimId" = 107;

-- Verify all admin sponsorship claims (100-107)
SELECT 'VERIFICATION: All Admin Sponsorship Claims' as info;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 100 AND 107
ORDER BY "Id";

-- =====================================================
-- DEPLOYMENT CHECKLIST
-- =====================================================
-- [ ] Run this SQL script on database
-- [ ] Verify claim created (ID 107)
-- [ ] Verify claim assigned to Administrators group
-- [ ] Push code changes to repository
-- [ ] Deploy to staging/production
-- [ ] Admin user MUST logout/login (cache refresh)
-- [ ] Test endpoint: GET /api/admin/sponsorship/sponsors
-- [ ] Verify 200 OK response (not 401)

-- =====================================================
-- EXPECTED RESULTS
-- =====================================================
-- OperationClaims table:
--   Id: 107
--   Name: GetAllSponsorsQuery
--   Alias: Get All Sponsors
--   Description: Query all users with Sponsor role (GroupId = 3)
--
-- GroupClaims table:
--   GroupId: 1 (Administrators)
--   ClaimId: 107
