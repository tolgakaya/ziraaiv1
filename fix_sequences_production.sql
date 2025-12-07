-- =============================================================================
-- PRODUCTION DATABASE SEQUENCE FIX
-- Problem: After cloning from staging, sequences are out of sync with data
-- Solution: Reset all sequences to MAX(id) + 1 for each table
-- =============================================================================

-- ⚠️ CRITICAL: Users table (PRIMARY FIX - This is causing the registration error)
SELECT setval('public."Users_UserId_seq"', (SELECT COALESCE(MAX("UserId"), 1) FROM public."Users"));

-- Core Authentication & Authorization
SELECT setval('public."MobileLogins_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."MobileLogins"));
SELECT setval('public."OperationClaims_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."OperationClaims"));
SELECT setval('public."Groups_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Groups"));
SELECT setval('public."Roles_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Roles"));
SELECT setval('public."UserRoles_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."UserRoles"));

-- Plant Analysis System
SELECT setval('public."PlantAnalyses_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."PlantAnalyses"));
SELECT setval('public."AnalysisMessages_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."AnalysisMessages"));

-- Subscription System
SELECT setval('public."SubscriptionTiers_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."SubscriptionTiers"));
SELECT setval('public."UserSubscriptions_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."UserSubscriptions"));
SELECT setval('public."SubscriptionUsageLogs_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."SubscriptionUsageLogs"));

-- Sponsorship System
SELECT setval('public."SponsorProfiles_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."SponsorProfiles"));
SELECT setval('public."SponsorshipCodes_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."SponsorshipCodes"));
SELECT setval('public."SponsorAnalysisAccess_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."SponsorAnalysisAccess"));
SELECT setval('public."SponsorshipPurchases_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."SponsorshipPurchases"));

-- Dealer System
SELECT setval('public."DealerInvitations_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."DealerInvitations"));

-- Referral System
SELECT setval('public."ReferralCodes_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."ReferralCodes"));
SELECT setval('public."ReferralTracking_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."ReferralTracking"));
SELECT setval('public."ReferralRewards_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."ReferralRewards"));
SELECT setval('public."ReferralConfigurations_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."ReferralConfigurations"));

-- Deep Links
SELECT setval('public."DeepLinks_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."DeepLinks"));
SELECT setval('public."DeepLinkClickRecords_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."DeepLinkClickRecords"));

-- Messaging & Features
SELECT setval('public."MessagingFeatures_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."MessagingFeatures"));
SELECT setval('public."TierFeatures_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."TierFeatures"));
SELECT setval('public."Features_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Features"));

-- Blocking
SELECT setval('public."FarmerSponsorBlocks_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."FarmerSponsorBlocks"));

-- Payment System
SELECT setval('public."PaymentTransactions_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."PaymentTransactions"));

-- Ticket System
SELECT setval('public."Tickets_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Tickets"));
SELECT setval('public."TicketMessages_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."TicketMessages"));

-- Localization
SELECT setval('public."Languages_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Languages"));
SELECT setval('public."Translates_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Translates"));

-- Admin & Logging
SELECT setval('public."AdminOperationLogs_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."AdminOperationLogs"));
SELECT setval('public."Logs_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Logs"));
SELECT setval('public."SmsLogs_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."SmsLogs"));

-- Configuration & App Info
SELECT setval('public."Configurations_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."Configurations"));
SELECT setval('public."AppInfos_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."AppInfos"));

-- Background Jobs
SELECT setval('public."BulkCodeDistributionJobs_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."BulkCodeDistributionJobs"));
SELECT setval('public."BulkInvitationJobs_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."BulkInvitationJobs"));
SELECT setval('public."BulkSubscriptionAssignmentJobs_Id_seq"', (SELECT COALESCE(MAX("Id"), 1) FROM public."BulkSubscriptionAssignmentJobs"));

-- ==========================================
-- VERIFICATION QUERY
-- Run this after to verify all sequences are correct
-- ==========================================
/*
SELECT
    'Users' as table_name,
    (SELECT MAX("UserId") FROM public."Users") as max_id,
    currval('public."Users_UserId_seq"') as sequence_value
UNION ALL
SELECT
    'PlantAnalyses',
    (SELECT MAX("Id") FROM public."PlantAnalyses"),
    currval('public."PlantAnalyses_Id_seq"')
UNION ALL
SELECT
    'AnalysisMessages',
    (SELECT MAX("Id") FROM public."AnalysisMessages"),
    currval('public."AnalysisMessages_Id_seq"')
UNION ALL
SELECT
    'SponsorshipCodes',
    (SELECT MAX("Id") FROM public."SponsorshipCodes"),
    currval('public."SponsorshipCodes_Id_seq"')
UNION ALL
SELECT
    'UserSubscriptions',
    (SELECT MAX("Id") FROM public."UserSubscriptions"),
    currval('public."UserSubscriptions_Id_seq"')
ORDER BY table_name;
*/
