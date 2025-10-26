
-- Step 1: Count total claims (should match log count)
SELECT COUNT(DISTINCT oc."Name") as "TotalClaimsCount"
FROM public."Users" u
INNER JOIN public."UserGroups" ug ON u."UserId" = ug."UserId"
INNER JOIN public."GroupClaims" gc ON ug."GroupId" = gc."GroupId"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE u."UserId" = 159;

SORGU SONUCU: 24

-- Step 2: List all claims (alphabetically sorted)
SELECT DISTINCT oc."Name" as "ClaimName"
FROM public."Users" u
INNER JOIN public."UserGroups" ug ON u."UserId" = ug."UserId"
INNER JOIN public."GroupClaims" gc ON ug."GroupId" = gc."GroupId"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE u."UserId" = 159
UNION
SELECT oc."Name" as "ClaimName"
FROM public."Users" u
INNER JOIN public."UserClaims" uc ON u."UserId" = uc."UserId"
INNER JOIN public."OperationClaims" oc ON uc."ClaimId" = oc."Id"
WHERE u."UserId" = 159
ORDER BY "ClaimName";


SORGU SONUCU

CreateDealerInvitationCommand
GetCodeAnalysisStatisticsQuery
GetCodeAnalysisStatisticsQueryHandler
GetDealerInvitationsQuery
GetDealerPerformanceQuery
GetDealerSummaryQuery
GetLinkStatisticsQuery
GetPackageDistributionStatisticsQuery
GetPackageDistributionStatisticsQueryHandler
GetSponsorImpactAnalyticsQuery
GetSponsorImpactAnalyticsQueryHandler
GetSponsorMessagingAnalyticsQuery
GetSponsorMessagingAnalyticsQueryHandler
GetSponsorROIAnalyticsQuery
GetSponsorROIAnalyticsQueryHandler
GetSponsorTemporalAnalyticsQuery
GetSponsorTemporalAnalyticsQueryHandler
PlantAnalysisCreate
PlantAnalysisView
ReclaimDealerCodesCommand
SearchDealerByEmailQuery
SendSponsorshipLinkCommand
SubscriptionView
TransferCodesToDealerCommand
