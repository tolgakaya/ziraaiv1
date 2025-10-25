-- =====================================================
-- Assign OLD (middleware-created) Claims to Sponsor Role
-- =====================================================
-- Problem: Duplicate claim'ler var. Middleware ID 71,72,116-119 yaratmış.
-- Biz ID 126-131 ekledik. Sadece yeni olanlar Sponsor role'e atanmış.
-- Eski olanları da atamamız lazım.
-- =====================================================

-- Insert OLD claims to Sponsor role
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES
    (3, 71),  -- GetCodeAnalysisStatisticsQuery
    (3, 72),  -- GetPackageDistributionStatisticsQuery
    (3, 116), -- GetSponsorImpactAnalyticsQuery
    (3, 117), -- GetSponsorMessagingAnalyticsQuery
    (3, 118), -- GetSponsorROIAnalyticsQuery
    (3, 119); -- GetSponsorTemporalAnalyticsQuery

-- Also assign to Admin role
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES
    (1, 71),  -- GetCodeAnalysisStatisticsQuery
    (1, 72),  -- GetPackageDistributionStatisticsQuery
    (1, 116), -- GetSponsorImpactAnalyticsQuery
    (1, 117), -- GetSponsorMessagingAnalyticsQuery
    (1, 118), -- GetSponsorROIAnalyticsQuery
    (1, 119); -- GetSponsorTemporalAnalyticsQuery

-- Verification
SELECT oc."Id", oc."Name"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."GroupName" = 'Sponsor'
  AND oc."Id" IN (71, 72, 116, 117, 118, 119, 126, 127, 128, 129, 130, 131)
ORDER BY oc."Id";
