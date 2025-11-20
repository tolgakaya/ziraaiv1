-- =====================================================
-- FIX: Add Query class names (not Handler class names)
-- SecuredOperation uses ReflectedType.Name which returns the PARENT class
-- =====================================================

-- These already exist in database (from our previous insert):
-- GetPackageDistributionStatisticsQueryHandler
-- GetCodeAnalysisStatisticsQueryHandler  
-- GetSponsorMessagingAnalyticsQueryHandler
-- GetSponsorImpactAnalyticsQueryHandler
-- GetSponsorTemporalAnalyticsQueryHandler
-- GetSponsorROIAnalyticsQueryHandler

-- But SecuredOperation actually checks for the QUERY class names (parent class):

INSERT INTO "OperationClaims" ("Name", "Alias", "Description")
VALUES 
    ('GetPackageDistributionStatisticsQuery', 'Package Distribution Query', 'View package distribution analytics'),
    ('GetCodeAnalysisStatisticsQuery', 'Code Analysis Query', 'View code analysis statistics'),
    ('GetSponsorMessagingAnalyticsQuery', 'Messaging Analytics Query', 'View messaging analytics'),
    ('GetSponsorImpactAnalyticsQuery', 'Impact Analytics Query', 'View impact analytics'),
    ('GetSponsorTemporalAnalyticsQuery', 'Temporal Analytics Query', 'View temporal analytics'),
    ('GetSponsorROIAnalyticsQuery', 'ROI Analytics Query', 'View ROI analytics');

-- Assign to Sponsor role
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 
    g."Id" as "GroupId",
    oc."Id" as "ClaimId"
FROM "Groups" g
CROSS JOIN "OperationClaims" oc
WHERE g."GroupName" = 'Sponsor'
  AND oc."Name" IN (
    'GetPackageDistributionStatisticsQuery',
    'GetCodeAnalysisStatisticsQuery',
    'GetSponsorMessagingAnalyticsQuery',
    'GetSponsorImpactAnalyticsQuery',
    'GetSponsorTemporalAnalyticsQuery',
    'GetSponsorROIAnalyticsQuery'
  );

-- Assign to Admin role
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 
    g."Id" as "GroupId",
    oc."Id" as "ClaimId"
FROM "Groups" g
CROSS JOIN "OperationClaims" oc
WHERE g."GroupName" = 'Admin'
  AND oc."Name" IN (
    'GetPackageDistributionStatisticsQuery',
    'GetCodeAnalysisStatisticsQuery',
    'GetSponsorMessagingAnalyticsQuery',
    'GetSponsorImpactAnalyticsQuery',
    'GetSponsorTemporalAnalyticsQuery',
    'GetSponsorROIAnalyticsQuery'
  );

-- Verification
SELECT oc."Name" 
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."GroupName" = 'Sponsor' 
  AND (oc."Name" LIKE '%Statistics%' OR oc."Name" LIKE '%Analytics%')
ORDER BY oc."Name";
