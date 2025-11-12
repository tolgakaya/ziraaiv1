-- =============================================
-- Admin Non-Sponsored Analysis Detail - Operation Claim
-- =============================================
-- Description: Add operation claim for admin non-sponsored analysis detail view
-- Claim ID: 158
-- Handler: GetNonSponsoredAnalysisDetailQuery
-- Author: Claude Code
-- Date: 2025-11-10
-- =============================================

-- Check if claim already exists before inserting
DO $$
BEGIN
    -- Claim 158: GetNonSponsoredAnalysisDetail
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 158) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
        VALUES (158, 'GetNonSponsoredAnalysisDetailQuery', 'Admin Non-Sponsored Analysis Detail View', 'Admin olarak sponsorsuz analiz detayı görüntüleme (farmer görünümü ile aynı)');
        RAISE NOTICE 'Claim 158 (GetNonSponsoredAnalysisDetailQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 158 already exists, skipping';
    END IF;
END $$;

-- =============================================
-- Grant Claim to Administrators Group (GroupId = 1)
-- =============================================

DO $$
BEGIN
    -- Grant Claim 158 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 158) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 158);
        RAISE NOTICE 'Claim 158 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 158 already granted to Administrators, skipping';
    END IF;
END $$;

-- =============================================
-- Verification Queries
-- =============================================

-- Verify claim was added
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" = 158;

-- Verify claim granted to Administrators
SELECT gc."GroupId", gc."ClaimId", oc."Name", oc."Alias", oc."Description"
FROM "GroupClaims" gc
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 1 AND gc."ClaimId" = 158;

-- Check all admin sponsor view related claims (133-158)
SELECT oc."Id", oc."Name", oc."Alias",
       CASE WHEN gc."GroupId" IS NOT NULL THEN 'YES - GroupId: ' || gc."GroupId"
            ELSE 'NO - MISSING' END as "HasGroupClaim"
FROM "OperationClaims" oc
LEFT JOIN "GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Id" BETWEEN 133 AND 158
ORDER BY oc."Id";

-- =============================================
-- Summary
-- =============================================
-- Claim Added:
-- 158 - GetNonSponsoredAnalysisDetailQuery
--
-- Purpose: Enables admin to view detailed analysis information for
--          non-sponsored analyses (same view as farmer sees)
--
-- Endpoint: GET /api/admin/sponsorship/non-sponsored/analyses/{id}
--
-- Granted to: Administrators (GroupId = 1)
-- =============================================
