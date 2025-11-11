-- =====================================================
-- Fix Admin Sponsor & Analytics GroupClaims
-- =====================================================
-- Purpose: Assign missing admin sponsor/analytics claims to Administrators group
-- Date: 2025-11-11
-- Issue: Claims exist but are not assigned to GroupId=1 (Administrators)
-- Root Cause: Previous migrations created claims but forgot GroupClaims assignments
-- =====================================================

-- =====================================================
-- PART 0: Pre-Flight Analysis
-- =====================================================

-- Show current status of all admin sponsor/analytics claims
SELECT
    oc."Id",
    oc."Name",
    oc."Alias",
    CASE
        WHEN gc."ClaimId" IS NOT NULL THEN '✅ Already Assigned'
        ELSE '❌ WILL BE ASSIGNED'
    END as "Status"
FROM public."OperationClaims" oc
LEFT JOIN public."GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Id" IN (
    85,86,87,88,89,90,91,92,100,102,103,114,
    132,133,134,135,136,137,138,139,148,156,157
)
ORDER BY oc."Id";

-- =====================================================
-- PART 1: Missing Claims (Need to be created first)
-- =====================================================

-- GetAllPurchasesQuery - MISSING
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetAllPurchasesQuery', 'admin.sponsor.purchases.list', 'Get all sponsorship purchases with pagination and filtering'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetAllPurchasesQuery'
);

-- GetPurchaseByIdQuery - MISSING
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetPurchaseByIdQuery', 'admin.sponsor.purchases.detail', 'Get detailed information for a specific purchase'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetPurchaseByIdQuery'
);

-- =====================================================
-- PART 2: Assign ALL Admin Sponsor/Analytics Claims to Administrators
-- =====================================================

-- Assign existing claims that are NOT assigned (11 claims)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, "Id"
FROM public."OperationClaims"
WHERE "Id" IN (
    85,  -- ApprovePurchaseCommand
    86,  -- RefundPurchaseCommand
    87,  -- GetAllCodesQuery
    88,  -- GetCodeByIdQuery
    89,  -- DeactivateCodeCommand
    90,  -- GetSponsorshipStatisticsQuery
    91,  -- GetSubscriptionStatisticsQuery
    92,  -- GetUserStatisticsQuery
    114, -- GetActivityLogsQuery
    148, -- GetSponsorDetailedReportQuery (THE ONE USER TESTED!)
    156, -- GetBulkCodeDistributionJobHistoryQuery
    157  -- GetNonSponsoredAnalysisDetailQuery
)
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims"
    WHERE "GroupId" = 1 AND "ClaimId" = public."OperationClaims"."Id"
);

-- Assign newly created claims (2 claims)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" IN ('GetAllPurchasesQuery', 'GetPurchaseByIdQuery')
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc
    WHERE gc."GroupId" = 1 AND gc."ClaimId" = oc."Id"
);

-- =====================================================
-- PART 3: Verification
-- =====================================================

-- Verify all 25 admin sponsor/analytics claims are now assigned
SELECT
    oc."Id",
    oc."Name",
    oc."Alias",
    CASE
        WHEN gc."ClaimId" IS NOT NULL THEN '✅ Assigned'
        ELSE '❌ FAILED TO ASSIGN'
    END as "Status"
FROM public."OperationClaims" oc
LEFT JOIN public."GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Name" IN (
    -- Commands (6)
    'ApprovePurchaseCommand',
    'BulkSendCodesCommand',
    'CreatePurchaseOnBehalfOfCommand',
    'DeactivateCodeCommand',
    'RefundPurchaseCommand',
    'SendMessageAsSponsorCommand',
    -- Queries - AdminSponsorship (14)
    'GetAllCodesQuery',
    'GetAllPurchasesQuery',
    'GetAllSponsorsQuery',
    'GetBulkCodeDistributionJobHistoryQuery',
    'GetCodeByIdQuery',
    'GetNonSponsoredAnalysesQuery',
    'GetNonSponsoredAnalysisDetailQuery',
    'GetNonSponsoredFarmerDetailQuery',
    'GetPurchaseByIdQuery',
    'GetSponsorAnalysesAsAdminQuery',
    'GetSponsorAnalysisDetailAsAdminQuery',
    'GetSponsorDetailedReportQuery',
    'GetSponsorMessagesAsAdminQuery',
    'GetSponsorshipComparisonAnalyticsQuery',
    -- Queries - AdminAnalytics (5)
    'ExportStatisticsQuery',
    'GetActivityLogsQuery',
    'GetSponsorshipStatisticsQuery',
    'GetSubscriptionStatisticsQuery',
    'GetUserStatisticsQuery'
)
ORDER BY oc."Name";

-- Expected: 25 rows, ALL showing ✅ Assigned

-- Count verification
SELECT
    COUNT(*) as "TotalAssignedClaims",
    'Expected: 25' as "Expected"
FROM public."GroupClaims" gc
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 1
AND oc."Name" IN (
    'ApprovePurchaseCommand', 'BulkSendCodesCommand', 'CreatePurchaseOnBehalfOfCommand',
    'DeactivateCodeCommand', 'RefundPurchaseCommand', 'SendMessageAsSponsorCommand',
    'GetAllCodesQuery', 'GetAllPurchasesQuery', 'GetAllSponsorsQuery',
    'GetBulkCodeDistributionJobHistoryQuery', 'GetCodeByIdQuery', 'GetNonSponsoredAnalysesQuery',
    'GetNonSponsoredAnalysisDetailQuery', 'GetNonSponsoredFarmerDetailQuery', 'GetPurchaseByIdQuery',
    'GetSponsorAnalysesAsAdminQuery', 'GetSponsorAnalysisDetailAsAdminQuery', 'GetSponsorDetailedReportQuery',
    'GetSponsorMessagesAsAdminQuery', 'GetSponsorshipComparisonAnalyticsQuery',
    'ExportStatisticsQuery', 'GetActivityLogsQuery', 'GetSponsorshipStatisticsQuery',
    'GetSubscriptionStatisticsQuery', 'GetUserStatisticsQuery'
);

-- =====================================================
-- PART 4: Critical Claim Check (User's Test Case)
-- =====================================================

-- Verify GetSponsorDetailedReportQuery is assigned (ID: 148)
SELECT
    oc."Id",
    oc."Name",
    g."GroupName",
    'THIS IS THE CLAIM USER TESTED - MUST BE ASSIGNED!' as "Note"
FROM public."GroupClaims" gc
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
WHERE oc."Name" = 'GetSponsorDetailedReportQuery'
  AND gc."GroupId" = 1;

-- Expected: 1 row showing ID=148, Name=GetSponsorDetailedReportQuery, GroupName=Administrators

-- =====================================================
-- IMPORTANT REMINDERS
-- =====================================================
-- 1. ⚠️ Admin users must LOGOUT and LOGIN after this script to refresh claims cache
-- 2. 🔍 This script is idempotent - safe to run multiple times
-- 3. ✅ After running, test: GET /api/admin/sponsorship/sponsors/159/detailed-report
-- 4. 📋 Expected result: 200 OK instead of 401 Unauthorized
-- =====================================================

-- End of script
