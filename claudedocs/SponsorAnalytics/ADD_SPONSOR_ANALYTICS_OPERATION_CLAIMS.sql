-- =====================================================
-- Add Sponsor Analytics Operation Claims
-- Date: 2025-01-25
-- Purpose: Fix "AuthorizationsDenied" errors on new sponsor analytics endpoints
-- =====================================================

-- STEP 1: Insert new operation claims for analytics handlers
-- =====================================================

-- Insert new operation claims (if they don't exist, manually check first)
-- Run this SELECT first to check if they already exist:
-- SELECT "Name" FROM "OperationClaims" WHERE "Name" LIKE '%Analytics%';

INSERT INTO "OperationClaims" ("Name", "Alias", "Description")
VALUES 
    ('GetPackageDistributionStatisticsQueryHandler', 'Package Distribution Statistics', 'View package distribution analytics for sponsors'),
    ('GetCodeAnalysisStatisticsQueryHandler', 'Code Analysis Statistics', 'View code-level analysis statistics for sponsors'),
    ('GetSponsorMessagingAnalyticsQueryHandler', 'Messaging Analytics', 'View messaging analytics for sponsors'),
    ('GetSponsorImpactAnalyticsQueryHandler', 'Impact Analytics', 'View impact analytics for sponsors'),
    ('GetSponsorTemporalAnalyticsQueryHandler', 'Temporal Analytics', 'View temporal analytics for sponsors'),
    ('GetSponsorROIAnalyticsQueryHandler', 'ROI Analytics', 'View ROI analytics for sponsors'); -- Skip if already exists

-- =====================================================
-- STEP 2: Assign operation claims to Sponsor role
-- =====================================================

INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 
    g."Id" as "GroupId",
    oc."Id" as "ClaimId"
FROM "Groups" g
CROSS JOIN "OperationClaims" oc
WHERE g."GroupName" = 'Sponsor'
  AND oc."Name" IN (
    'GetPackageDistributionStatisticsQueryHandler',
    'GetCodeAnalysisStatisticsQueryHandler',
    'GetSponsorMessagingAnalyticsQueryHandler',
    'GetSponsorImpactAnalyticsQueryHandler',
    'GetSponsorTemporalAnalyticsQueryHandler',
    'GetSponsorROIAnalyticsQueryHandler'
  ) -- Skip if already assigned

-- =====================================================
-- STEP 3: Also assign to Admin role (for testing)
-- =====================================================

INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 
    g."Id" as "GroupId",
    oc."Id" as "ClaimId"
FROM "Groups" g
CROSS JOIN "OperationClaims" oc
WHERE g."GroupName" = 'Admin'
  AND oc."Name" IN (
    'GetPackageDistributionStatisticsQueryHandler',
    'GetCodeAnalysisStatisticsQueryHandler',
    'GetSponsorMessagingAnalyticsQueryHandler',
    'GetSponsorImpactAnalyticsQueryHandler',
    'GetSponsorTemporalAnalyticsQueryHandler',
    'GetSponsorROIAnalyticsQueryHandler'
  )

-- =====================================================
-- VERIFICATION QUERIES (Run these after applying the script)
-- =====================================================

-- 1. Check if operation claims were created
SELECT "Id", "Name", "Alias", "Description" 
FROM "OperationClaims" 
WHERE "Name" IN (
    'GetPackageDistributionStatisticsQueryHandler',
    'GetCodeAnalysisStatisticsQueryHandler',
    'GetSponsorMessagingAnalyticsQueryHandler',
    'GetSponsorImpactAnalyticsQueryHandler',
    'GetSponsorTemporalAnalyticsQueryHandler',
    'GetSponsorROIAnalyticsQueryHandler'
)
ORDER BY "Name";

-- 2. Check if claims were assigned to Sponsor role
SELECT 
    g."GroupName",
    oc."Name" as "OperationClaimName",
    oc."Alias" as "ClaimAlias"
FROM "GroupClaims" ocg
JOIN "Groups" g ON ocg."GroupId" = g."Id"
JOIN "OperationClaims" oc ON ocg."OperationClaimId" = oc."Id"
WHERE g."GroupName" IN ('Sponsor', 'Admin')
  AND oc."Name" LIKE '%Analytics%'
ORDER BY g."GroupName", oc."Name";

-- 3. Check total operation claims count for Sponsor role
SELECT 
    g."GroupName",
    COUNT(*) as "TotalOperationClaims"
FROM "GroupClaims" ocg
JOIN "Groups" g ON ocg."GroupId" = g."Id"
WHERE g."GroupName" = 'Sponsor'
GROUP BY g."GroupName";

-- =====================================================
-- ROLLBACK SCRIPT (In case you need to undo changes)
-- =====================================================

/*
-- Delete operation claim assignments
DELETE FROM "OperationClaimGroups"
WHERE "OperationClaimId" IN (
    SELECT "Id" FROM "OperationClaims"
    WHERE "Name" IN (
        'GetPackageDistributionStatisticsQueryHandler',
        'GetCodeAnalysisStatisticsQueryHandler',
        'GetSponsorMessagingAnalyticsQueryHandler',
        'GetSponsorImpactAnalyticsQueryHandler',
        'GetSponsorTemporalAnalyticsQueryHandler',
        'GetSponsorROIAnalyticsQueryHandler'
    )
);

-- Delete operation claims
DELETE FROM "OperationClaims"
WHERE "Name" IN (
    'GetPackageDistributionStatisticsQueryHandler',
    'GetCodeAnalysisStatisticsQueryHandler',
    'GetSponsorMessagingAnalyticsQueryHandler',
    'GetSponsorImpactAnalyticsQueryHandler',
    'GetSponsorTemporalAnalyticsQueryHandler',
    'GetSponsorROIAnalyticsQueryHandler'
);
*/

-- =====================================================
-- POST-DEPLOYMENT STEPS
-- =====================================================

/*
1. Run this SQL script on staging database
2. RESTART the API application to clear operation claims cache
3. Test user must RE-AUTHENTICATE to get fresh JWT token with updated claims
4. Test all 7 sponsor analytics endpoints

Test command example:
TOKEN="<new_token_after_restart_and_reauth>"
curl -s -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/package-statistics" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0" | jq .
*/
