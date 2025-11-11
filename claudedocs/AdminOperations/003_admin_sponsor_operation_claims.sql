-- =====================================================
-- Admin Sponsor & Analytics Operation Claims Migration
-- =====================================================
-- Purpose: Add operation claims for all 25 admin sponsor/analytics endpoints
-- Date: 2025-11-10
-- Handlers: 20 AdminSponsorship + 5 AdminAnalytics = 25 total
-- Target Group: Administrators (GroupId = 1)
-- Claim IDs: 163-187 (verified no conflicts with existing claims)
-- Previous max ID: 162 (from 002_admin_bulk_subscription_operation_claims.sql)
-- =====================================================

-- =====================================================
-- PART 0: Pre-Flight Checks
-- =====================================================

-- Check current max claim ID to verify no conflicts
SELECT
    MAX("Id") as "CurrentMaxClaimId",
    'Expected: 162 or lower. If higher than 162, STOP and adjust claim IDs!' as "Warning"
FROM public."OperationClaims";

-- Check if any of our target IDs (163-187) already exist
SELECT
    "Id",
    "Name",
    'CONFLICT DETECTED! This ID is already in use!' as "Error"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 163 AND 187;
-- Expected: 0 rows returned. If any rows appear, STOP and adjust claim IDs!

-- =====================================================
-- PART 1: AdminSponsorship Commands (6 handlers)
-- =====================================================

-- ApprovePurchaseCommand (ClaimId: 163)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 163, 'ApprovePurchaseCommand', 'admin.sponsor.purchase.approve', 'Approve a pending sponsorship purchase (generates codes)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'ApprovePurchaseCommand'
);

-- BulkSendCodesCommand (ClaimId: 164)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 164, 'BulkSendCodesCommand', 'admin.sponsor.codes.bulk-send', 'Send sponsorship codes to multiple recipients via SMS/WhatsApp/Email'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'BulkSendCodesCommand'
);

-- CreatePurchaseOnBehalfOfCommand (ClaimId: 165)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 165, 'CreatePurchaseOnBehalfOfCommand', 'admin.sponsor.purchase.create-obo', 'Create sponsorship purchase on behalf of a sponsor (supports manual/offline payments)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'CreatePurchaseOnBehalfOfCommand'
);

-- DeactivateCodeCommand (ClaimId: 166)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 166, 'DeactivateCodeCommand', 'admin.sponsor.codes.deactivate', 'Deactivate a sponsorship code (cannot be used after deactivation)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'DeactivateCodeCommand'
);

-- RefundPurchaseCommand (ClaimId: 167)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 167, 'RefundPurchaseCommand', 'admin.sponsor.purchase.refund', 'Refund a sponsorship purchase (deactivates all codes)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'RefundPurchaseCommand'
);

-- SendMessageAsSponsorCommand (ClaimId: 168)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 168, 'SendMessageAsSponsorCommand', 'admin.sponsor.messages.send-as', 'Send message on behalf of a sponsor (admin OBO)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'SendMessageAsSponsorCommand'
);

-- =====================================================
-- PART 2: AdminSponsorship Queries (14 handlers)
-- =====================================================

-- GetAllCodesQuery (ClaimId: 169)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 169, 'GetAllCodesQuery', 'admin.sponsor.codes.list', 'Get all sponsorship codes with pagination and filtering'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetAllCodesQuery'
);

-- GetAllPurchasesQuery (ClaimId: 170)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 170, 'GetAllPurchasesQuery', 'admin.sponsor.purchases.list', 'Get all sponsorship purchases with pagination and filtering'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetAllPurchasesQuery'
);

-- GetAllSponsorsQuery (ClaimId: 171)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 171, 'GetAllSponsorsQuery', 'admin.sponsor.sponsors.list', 'Get list of all users with Sponsor role (GroupId = 3)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetAllSponsorsQuery'
);

-- GetBulkCodeDistributionJobHistoryQuery (ClaimId: 172)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 172, 'GetBulkCodeDistributionJobHistoryQuery', 'admin.sponsor.bulk-jobs.history', 'Get bulk code distribution job history with filtering'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetBulkCodeDistributionJobHistoryQuery'
);

-- GetCodeByIdQuery (ClaimId: 173)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 173, 'GetCodeByIdQuery', 'admin.sponsor.codes.detail', 'Get detailed information for a specific sponsorship code'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetCodeByIdQuery'
);

-- GetNonSponsoredAnalysesQuery (ClaimId: 174)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 174, 'GetNonSponsoredAnalysesQuery', 'admin.sponsor.non-sponsored.analyses.list', 'Get list of analyses without sponsor codes'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetNonSponsoredAnalysesQuery'
);

-- GetNonSponsoredAnalysisDetailQuery (ClaimId: 175)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 175, 'GetNonSponsoredAnalysisDetailQuery', 'admin.sponsor.non-sponsored.analyses.detail', 'View detailed information for a non-sponsored analysis'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetNonSponsoredAnalysisDetailQuery'
);

-- GetNonSponsoredFarmerDetailQuery (ClaimId: 176)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 176, 'GetNonSponsoredFarmerDetailQuery', 'admin.sponsor.non-sponsored.farmers.detail', 'Get detailed profile for a non-sponsored farmer'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetNonSponsoredFarmerDetailQuery'
);

-- GetPurchaseByIdQuery (ClaimId: 177)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 177, 'GetPurchaseByIdQuery', 'admin.sponsor.purchases.detail', 'Get detailed information for a specific purchase'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetPurchaseByIdQuery'
);

-- GetSponsorAnalysesAsAdminQuery (ClaimId: 178)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 178, 'GetSponsorAnalysesAsAdminQuery', 'admin.sponsor.sponsor-analyses.list', 'View all analyses for a specific sponsor (admin viewing sponsor perspective)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetSponsorAnalysesAsAdminQuery'
);

-- GetSponsorAnalysisDetailAsAdminQuery (ClaimId: 179)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 179, 'GetSponsorAnalysisDetailAsAdminQuery', 'admin.sponsor.sponsor-analyses.detail', 'View detailed analysis information (admin viewing sponsor perspective)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetSponsorAnalysisDetailAsAdminQuery'
);

-- GetSponsorDetailedReportQuery (ClaimId: 180)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 180, 'GetSponsorDetailedReportQuery', 'admin.sponsor.sponsors.report', 'Get comprehensive report for a specific sponsor including purchases, codes, usage'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetSponsorDetailedReportQuery'
);

-- GetSponsorMessagesAsAdminQuery (ClaimId: 181)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 181, 'GetSponsorMessagesAsAdminQuery', 'admin.sponsor.messages.view', 'View message conversation between sponsor and farmer (admin view)'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetSponsorMessagesAsAdminQuery'
);

-- GetSponsorshipComparisonAnalyticsQuery (ClaimId: 182)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 182, 'GetSponsorshipComparisonAnalyticsQuery', 'admin.sponsor.analytics.comparison', 'Compare sponsored vs non-sponsored analyses with comprehensive metrics'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetSponsorshipComparisonAnalyticsQuery'
);

-- =====================================================
-- PART 3: AdminAnalytics Queries (5 handlers)
-- =====================================================

-- ExportStatisticsQuery (ClaimId: 183)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 183, 'ExportStatisticsQuery', 'admin.analytics.export', 'Export statistics as CSV file'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'ExportStatisticsQuery'
);

-- GetActivityLogsQuery (ClaimId: 184)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 184, 'GetActivityLogsQuery', 'admin.analytics.activity-logs', 'Get system activity logs with filtering and pagination'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetActivityLogsQuery'
);

-- GetSponsorshipStatisticsQuery (ClaimId: 185)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 185, 'GetSponsorshipStatisticsQuery', 'admin.analytics.sponsorship', 'Get comprehensive sponsorship system statistics'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetSponsorshipStatisticsQuery'
);

-- GetSubscriptionStatisticsQuery (ClaimId: 186)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 186, 'GetSubscriptionStatisticsQuery', 'admin.analytics.subscription', 'Get subscription system metrics including revenue'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetSubscriptionStatisticsQuery'
);

-- GetUserStatisticsQuery (ClaimId: 187)
INSERT INTO public."OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 187, 'GetUserStatisticsQuery', 'admin.analytics.users', 'Get comprehensive user statistics and metrics'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims"
    WHERE "Name" = 'GetUserStatisticsQuery'
);

-- =====================================================
-- PART 4: Assign All Claims to Administrators Group
-- =====================================================

-- Assign claims 163-187 to Administrators group (GroupId = 1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Id" BETWEEN 163 AND 187
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc
    WHERE gc."GroupId" = 1 AND gc."ClaimId" = oc."Id"
);

-- =====================================================
-- PART 5: Verification Queries
-- =====================================================

-- Verify all 25 OperationClaims were created
SELECT "Id", "Name", "Alias", "Description"
FROM public."OperationClaims"
WHERE "Id" BETWEEN 163 AND 187
ORDER BY "Id";

-- Expected: 25 rows

-- Verify all 25 claims are assigned to Administrators group
SELECT
    g."GroupName",
    oc."Id" as "ClaimId",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 1 -- Administrators group
  AND oc."Id" BETWEEN 163 AND 187
ORDER BY oc."Id";

-- Expected: 25 rows (all claims assigned to Admin)

-- Verify specific claim names
SELECT "Id", "Name"
FROM public."OperationClaims"
WHERE "Name" IN (
    'ApprovePurchaseCommand',
    'GetSponsorDetailedReportQuery',
    'GetSponsorshipStatisticsQuery',
    'ExportStatisticsQuery'
)
ORDER BY "Name";

-- Expected: 4 rows (sample verification)

-- =====================================================
-- PART 6: Claim ID Summary
-- =====================================================

-- AdminSponsorship Commands (6 claims): 163-168
-- 163: ApprovePurchaseCommand
-- 164: BulkSendCodesCommand
-- 165: CreatePurchaseOnBehalfOfCommand
-- 166: DeactivateCodeCommand
-- 167: RefundPurchaseCommand
-- 168: SendMessageAsSponsorCommand

-- AdminSponsorship Queries (14 claims): 169-182
-- 169: GetAllCodesQuery
-- 170: GetAllPurchasesQuery
-- 171: GetAllSponsorsQuery
-- 172: GetBulkCodeDistributionJobHistoryQuery
-- 173: GetCodeByIdQuery
-- 174: GetNonSponsoredAnalysesQuery
-- 175: GetNonSponsoredAnalysisDetailQuery
-- 176: GetNonSponsoredFarmerDetailQuery
-- 177: GetPurchaseByIdQuery
-- 178: GetSponsorAnalysesAsAdminQuery
-- 179: GetSponsorAnalysisDetailAsAdminQuery
-- 180: GetSponsorDetailedReportQuery
-- 181: GetSponsorMessagesAsAdminQuery
-- 182: GetSponsorshipComparisonAnalyticsQuery

-- AdminAnalytics Queries (5 claims): 183-187
-- 183: ExportStatisticsQuery
-- 184: GetActivityLogsQuery
-- 185: GetSponsorshipStatisticsQuery
-- 186: GetSubscriptionStatisticsQuery
-- 187: GetUserStatisticsQuery

-- Total: 25 operation claims (163-187)
-- All assigned to Administrators group (GroupId = 1)
