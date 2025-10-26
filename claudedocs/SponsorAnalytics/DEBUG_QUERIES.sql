-- =====================================================
-- Debug Queries for Operation Claims Investigation
-- =====================================================

-- 1. Sponsor role'ün toplam kaç claim'i var?
SELECT COUNT(*) as "SponsorClaimCount"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
WHERE g."GroupName" = 'Sponsor';

sonuç: 20

-- 2. Sponsor role'e ait son 20 claim'i göster
SELECT oc."Name"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."GroupName" = 'Sponsor'
ORDER BY gc."ClaimId" DESC
LIMIT 20;


GetSponsorROIAnalyticsQuery
GetSponsorTemporalAnalyticsQuery
GetSponsorImpactAnalyticsQuery
GetSponsorMessagingAnalyticsQuery
GetCodeAnalysisStatisticsQuery
GetPackageDistributionStatisticsQuery
GetSponsorROIAnalyticsQueryHandler
GetSponsorTemporalAnalyticsQueryHandler
GetSponsorImpactAnalyticsQueryHandler
GetSponsorMessagingAnalyticsQueryHandler
GetCodeAnalysisStatisticsQueryHandler
GetPackageDistributionStatisticsQueryHandler
GetSponsorTemporalAnalyticsQuery
GetSponsorROIAnalyticsQuery
GetSponsorMessagingAnalyticsQuery
GetSponsorImpactAnalyticsQuery
GetPackageDistributionStatisticsQuery
GetCodeAnalysisStatisticsQuery
SendSponsorshipLinkCommand
GetLinkStatisticsQuery


-- 3. Analytics claim'leri Sponsor role'e atanmış mı?
SELECT oc."Id", oc."Name", oc."Alias"
FROM "OperationClaims" oc
WHERE oc."Name" IN (
    'GetPackageDistributionStatisticsQuery',
    'GetCodeAnalysisStatisticsQuery',
    'GetSponsorMessagingAnalyticsQuery',
    'GetSponsorImpactAnalyticsQuery',
    'GetSponsorTemporalAnalyticsQuery',
    'GetSponsorROIAnalyticsQuery'
)
ORDER BY oc."Id";


71	GetCodeAnalysisStatisticsQuery	
72	GetPackageDistributionStatisticsQuery	
116	GetSponsorImpactAnalyticsQuery	
117	GetSponsorMessagingAnalyticsQuery	
118	GetSponsorROIAnalyticsQuery	
119	GetSponsorTemporalAnalyticsQuery	
126	GetPackageDistributionStatisticsQuery	Package Distribution Query
127	GetCodeAnalysisStatisticsQuery	Code Analysis Query
128	GetSponsorMessagingAnalyticsQuery	Messaging Analytics Query
129	GetSponsorImpactAnalyticsQuery	Impact Analytics Query
130	GetSponsorTemporalAnalyticsQuery	Temporal Analytics Query
131	GetSponsorROIAnalyticsQuery	ROI Analytics Query


-- 4. Bu claim'lerden hangileri Sponsor role'e atanmış?
SELECT oc."Name"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."GroupName" = 'Sponsor'
  AND oc."Id" BETWEEN 126 AND 131
ORDER BY oc."Id";


GetPackageDistributionStatisticsQuery
GetCodeAnalysisStatisticsQuery
GetSponsorMessagingAnalyticsQuery
GetSponsorImpactAnalyticsQuery
GetSponsorTemporalAnalyticsQuery
GetSponsorROIAnalyticsQuery

-- 5. Groups tablosunda Sponsor var mı ve ID'si nedir?
SELECT "Id", "GroupName" 
FROM "Groups" 
WHERE "GroupName" = 'Sponsor';

3	Sponsor