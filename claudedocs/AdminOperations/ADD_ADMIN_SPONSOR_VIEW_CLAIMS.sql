-- =============================================
-- Admin Sponsor View Operations - Operation Claims
-- =============================================
-- Description: Add operation claims for admin sponsor view functionality
-- Claims: 133-139 (7 new claims)
-- Author: Claude Code
-- Date: 2025-11-08
-- =============================================

-- Check if claims already exist before inserting
DO $$
BEGIN
    -- Claim 133: GetSponsorAnalysesAsAdmin
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 133) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedAt", "UpdatedAt")
        VALUES (133, 'GetSponsorAnalysesAsAdminQuery', 'Admin Sponsor Analyses View', 'Admin olarak sponsor analizlerini görüntüleme', NOW(), NOW());
        RAISE NOTICE 'Claim 133 (GetSponsorAnalysesAsAdminQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 133 already exists, skipping';
    END IF;

    -- Claim 134: GetSponsorAnalysisDetailAsAdmin
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 134) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedAt", "UpdatedAt")
        VALUES (134, 'GetSponsorAnalysisDetailAsAdminQuery', 'Admin Sponsor Analysis Detail View', 'Admin olarak sponsor analiz detayı görüntüleme', NOW(), NOW());
        RAISE NOTICE 'Claim 134 (GetSponsorAnalysisDetailAsAdminQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 134 already exists, skipping';
    END IF;

    -- Claim 135: GetSponsorMessagesAsAdmin
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 135) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedAt", "UpdatedAt")
        VALUES (135, 'GetSponsorMessagesAsAdminQuery', 'Admin Sponsor Messages View', 'Admin olarak sponsor mesajlarını görüntüleme', NOW(), NOW());
        RAISE NOTICE 'Claim 135 (GetSponsorMessagesAsAdminQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 135 already exists, skipping';
    END IF;

    -- Claim 136: GetNonSponsoredAnalyses
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 136) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedAt", "UpdatedAt")
        VALUES (136, 'GetNonSponsoredAnalysesQuery', 'Admin Non-Sponsored Analyses View', 'Admin olarak sponsorsuz analizleri görüntüleme', NOW(), NOW());
        RAISE NOTICE 'Claim 136 (GetNonSponsoredAnalysesQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 136 already exists, skipping';
    END IF;

    -- Claim 137: GetNonSponsoredFarmerDetail
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 137) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedAt", "UpdatedAt")
        VALUES (137, 'GetNonSponsoredFarmerDetailQuery', 'Admin Non-Sponsored Farmer Detail', 'Admin olarak sponsorsuz çiftçi detayı görüntüleme', NOW(), NOW());
        RAISE NOTICE 'Claim 137 (GetNonSponsoredFarmerDetailQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 137 already exists, skipping';
    END IF;

    -- Claim 138: GetSponsorshipComparisonAnalytics
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 138) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedAt", "UpdatedAt")
        VALUES (138, 'GetSponsorshipComparisonAnalyticsQuery', 'Admin Sponsorship Comparison Analytics', 'Admin sponsor karşılaştırma analitiği görüntüleme', NOW(), NOW());
        RAISE NOTICE 'Claim 138 (GetSponsorshipComparisonAnalyticsQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 138 already exists, skipping';
    END IF;

    -- Claim 139: SendMessageAsSponsor
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 139) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedAt", "UpdatedAt")
        VALUES (139, 'SendMessageAsSponsorCommand', 'Admin Send Message As Sponsor', 'Admin olarak sponsor adına mesaj gönderme', NOW(), NOW());
        RAISE NOTICE 'Claim 139 (SendMessageAsSponsorCommand) added successfully';
    ELSE
        RAISE NOTICE 'Claim 139 already exists, skipping';
    END IF;
END $$;

-- =============================================
-- Grant Claims to Administrators Group (GroupId = 1)
-- =============================================

DO $$
BEGIN
    -- Grant Claim 133 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 133) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 133);
        RAISE NOTICE 'Claim 133 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 133 already granted to Administrators, skipping';
    END IF;

    -- Grant Claim 134 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 134) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 134);
        RAISE NOTICE 'Claim 134 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 134 already granted to Administrators, skipping';
    END IF;

    -- Grant Claim 135 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 135) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 135);
        RAISE NOTICE 'Claim 135 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 135 already granted to Administrators, skipping';
    END IF;

    -- Grant Claim 136 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 136) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 136);
        RAISE NOTICE 'Claim 136 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 136 already granted to Administrators, skipping';
    END IF;

    -- Grant Claim 137 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 137) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 137);
        RAISE NOTICE 'Claim 137 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 137 already granted to Administrators, skipping';
    END IF;

    -- Grant Claim 138 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 138) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 138);
        RAISE NOTICE 'Claim 138 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 138 already granted to Administrators, skipping';
    END IF;

    -- Grant Claim 139 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 139) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 139);
        RAISE NOTICE 'Claim 139 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 139 already granted to Administrators, skipping';
    END IF;
END $$;

-- =============================================
-- Verification Queries
-- =============================================

-- Verify all claims were added
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 133 AND 139
ORDER BY "Id";

-- Verify all claims granted to Administrators
SELECT gc."GroupId", gc."ClaimId", oc."Name", oc."Alias"
FROM "GroupClaims" gc
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 1 AND gc."ClaimId" BETWEEN 133 AND 139
ORDER BY gc."ClaimId";

-- =============================================
-- Summary
-- =============================================
-- Claims Added:
-- 133 - GetSponsorAnalysesAsAdminQuery
-- 134 - GetSponsorAnalysisDetailAsAdminQuery
-- 135 - GetSponsorMessagesAsAdminQuery
-- 136 - GetNonSponsoredAnalysesQuery
-- 137 - GetNonSponsoredFarmerDetailQuery
-- 138 - GetSponsorshipComparisonAnalyticsQuery
-- 139 - SendMessageAsSponsorCommand
--
-- All claims granted to: Administrators (GroupId = 1)
-- =============================================
