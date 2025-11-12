-- =====================================================
-- Farmer Segmentation Analytics - OperationClaim & GroupClaims
-- =====================================================
-- Purpose: Add GetFarmerSegmentationQuery claim and assign to Admin/Sponsor groups
-- Date: 2025-11-12
-- Branch: feature/sponsor-advanced-analytics
-- Related: GetFarmerSegmentationQuery.cs, SponsorshipController.cs:914
-- =====================================================

-- =====================================================
-- PART 0: Pre-Flight Analysis
-- =====================================================

-- Check if claim already exists
SELECT
    oc."Id",
    oc."Name",
    oc."Alias",
    oc."Description",
    CASE
        WHEN gc1."ClaimId" IS NOT NULL THEN '✅ Admin Assigned'
        ELSE '❌ Admin NOT Assigned'
    END as "AdminStatus",
    CASE
        WHEN gc2."ClaimId" IS NOT NULL THEN '✅ Sponsor Assigned'
        ELSE '❌ Sponsor NOT Assigned'
    END as "SponsorStatus"
FROM public."OperationClaims" oc
LEFT JOIN public."GroupClaims" gc1 ON oc."Id" = gc1."ClaimId" AND gc1."GroupId" = 1  -- Administrators
LEFT JOIN public."GroupClaims" gc2 ON oc."Id" = gc2."ClaimId" AND gc2."GroupId" = 3  -- Sponsor
WHERE oc."Name" = 'GetFarmerSegmentationQuery';

-- =====================================================
-- PART 1: Create OperationClaim (if not exists)
-- =====================================================
-- Next available Id: 163 (based on operation_claims.csv)

INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    163,
    'GetFarmerSegmentationQuery',
    'sponsorship.analytics.farmer-segmentation',
    'View farmer behavioral segmentation analytics (Heavy Users, Regular Users, At-Risk, Dormant)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetFarmerSegmentationQuery'
);

-- =====================================================
-- PART 2: Assign to Administrators Group (GroupId = 1)
-- =====================================================

-- Get the ClaimId (will be used in next statement)
DO $$
DECLARE
    claim_id INT;
BEGIN
    -- Find the claim ID
    SELECT "Id" INTO claim_id
    FROM public."OperationClaims"
    WHERE "Name" = 'GetFarmerSegmentationQuery';

    -- Assign to Administrators group (GroupId = 1)
    IF claim_id IS NOT NULL THEN
        INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
        SELECT 1, claim_id
        WHERE NOT EXISTS (
            SELECT 1 FROM public."GroupClaims"
            WHERE "GroupId" = 1 AND "ClaimId" = claim_id
        );

        RAISE NOTICE '✅ Assigned GetFarmerSegmentationQuery (ID: %) to Administrators group', claim_id;
    ELSE
        RAISE NOTICE '❌ GetFarmerSegmentationQuery claim not found';
    END IF;
END $$;

-- =====================================================
-- PART 3: Assign to Sponsor Group (GroupId = 3)
-- =====================================================

-- Sponsors also need this claim since endpoint allows Sponsor role
DO $$
DECLARE
    claim_id INT;
BEGIN
    -- Find the claim ID
    SELECT "Id" INTO claim_id
    FROM public."OperationClaims"
    WHERE "Name" = 'GetFarmerSegmentationQuery';

    -- Assign to Sponsor group (GroupId = 3)
    IF claim_id IS NOT NULL THEN
        INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
        SELECT 3, claim_id
        WHERE NOT EXISTS (
            SELECT 1 FROM public."GroupClaims"
            WHERE "GroupId" = 3 AND "ClaimId" = claim_id
        );

        RAISE NOTICE '✅ Assigned GetFarmerSegmentationQuery (ID: %) to Sponsor group', claim_id;
    ELSE
        RAISE NOTICE '❌ GetFarmerSegmentationQuery claim not found';
    END IF;
END $$;

-- =====================================================
-- PART 4: Post-Flight Verification
-- =====================================================

-- Verify assignments
SELECT
    oc."Id",
    oc."Name",
    oc."Alias",
    g."GroupName",
    CASE
        WHEN gc."ClaimId" IS NOT NULL THEN '✅ Assigned'
        ELSE '❌ NOT Assigned'
    END as "Status"
FROM public."OperationClaims" oc
CROSS JOIN public."Group" g
LEFT JOIN public."GroupClaims" gc ON oc."Id" = gc."ClaimId" AND g."Id" = gc."GroupId"
WHERE oc."Name" = 'GetFarmerSegmentationQuery'
  AND g."GroupName" IN ('Administrators', 'Sponsor')
ORDER BY g."GroupName";

-- =====================================================
-- PART 5: Summary Report
-- =====================================================

-- Count all sponsor analytics claims assigned to groups
SELECT
    g."GroupName",
    COUNT(DISTINCT gc."ClaimId") as "TotalAnalyticsClaims",
    STRING_AGG(oc."Alias", ', ' ORDER BY oc."Alias") as "ClaimAliases"
FROM public."GroupClaims" gc
JOIN public."Group" g ON gc."GroupId" = g."Id"
JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Alias" LIKE 'sponsorship.analytics%'
  AND g."GroupName" IN ('Administrators', 'Sponsor')
GROUP BY g."GroupName"
ORDER BY g."GroupName";

-- =====================================================
-- EXECUTION NOTES
-- =====================================================
--
-- 1. Run this script on the database (staging first, then production)
-- 2. After execution, admin and sponsor users must logout/login to refresh claims
-- 3. Verify endpoint works:
--    GET /api/v1/sponsorship/farmer-segmentation
--    Authorization: Bearer {token}
-- 4. Expected response: 200 OK with segmentation data
--
-- =====================================================
-- ROLLBACK (if needed)
-- =====================================================
--
-- To remove the claim and assignments:
--
-- DELETE FROM public."GroupClaims"
-- WHERE "ClaimId" IN (
--     SELECT "Id" FROM public."OperationClaims"
--     WHERE "Name" = 'GetFarmerSegmentationQuery'
-- );
--
-- DELETE FROM public."OperationClaims"
-- WHERE "Name" = 'GetFarmerSegmentationQuery';
--
-- =====================================================
