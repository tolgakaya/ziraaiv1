-- =============================================
-- Admin Non-Sponsored Analysis Detail - Operation Claim
-- =============================================
-- Description: Add operation claim for admin non-sponsored analysis detail view
-- Claim ID: 140
-- Handler: GetNonSponsoredAnalysisDetailQuery
-- Author: Claude Code
-- Date: 2025-11-10
-- =============================================

-- Check if claim already exists before inserting
DO $$
BEGIN
    -- Claim 140: GetNonSponsoredAnalysisDetail
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 140) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
        VALUES (140, 'GetNonSponsoredAnalysisDetailQuery', 'Admin Non-Sponsored Analysis Detail View', 'Admin olarak sponsorsuz analiz detayı görüntüleme (farmer görünümü ile aynı)');
        RAISE NOTICE 'Claim 140 (GetNonSponsoredAnalysisDetailQuery) added successfully';
    ELSE
        RAISE NOTICE 'Claim 140 already exists, skipping';
    END IF;
END $$;

-- =============================================
-- Grant Claim to Administrators Group (GroupId = 1)
-- =============================================

DO $$
BEGIN
    -- Grant Claim 140 to Administrators
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 140) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
        VALUES (1, 140);
        RAISE NOTICE 'Claim 140 granted to Administrators group';
    ELSE
        RAISE NOTICE 'Claim 140 already granted to Administrators, skipping';
    END IF;
END $$;

-- =============================================
-- Verification Queries
-- =============================================

-- Verify claim was added
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" = 140;

-- Verify claim granted to Administrators
SELECT gc."GroupId", gc."ClaimId", oc."Name", oc."Alias", oc."Description"
FROM "GroupClaims" gc
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 1 AND gc."ClaimId" = 140;

-- Check all admin sponsor view related claims (133-140)
SELECT oc."Id", oc."Name", oc."Alias",
       CASE WHEN gc."GroupId" IS NOT NULL THEN 'YES - GroupId: ' || gc."GroupId"
            ELSE 'NO - MISSING' END as "HasGroupClaim"
FROM "OperationClaims" oc
LEFT JOIN "GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Id" BETWEEN 133 AND 140
ORDER BY oc."Id";

-- =============================================
-- Summary
-- =============================================
-- Claim Added:
-- 140 - GetNonSponsoredAnalysisDetailQuery
--
-- Purpose: Enables admin to view detailed analysis information for
--          non-sponsored analyses (same view as farmer sees)
--
-- Endpoint: GET /api/admin/sponsorship/non-sponsored/analyses/{id}
--
-- Granted to: Administrators (GroupId = 1)
-- =============================================
