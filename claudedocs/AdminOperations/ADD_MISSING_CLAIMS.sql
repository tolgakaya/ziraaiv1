-- ============================================
-- Add Missing Operation Claims for New Admin Endpoints
-- Date: 2025-10-23
-- User: bilgitap@hotmail.com (User ID: 166)
-- ============================================

-- Step 1: Add GetActivityLogsQuery claim (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Name" = 'GetActivityLogsQuery') THEN
        INSERT INTO "OperationClaims" ("Name", "Alias", "Description")
        VALUES ('GetActivityLogsQuery', 'admin.analytics.activitylogs', 'View admin activity logs');
    END IF;
END $$;

-- Step 2: Add GetAllOBOAnalysesQuery claim (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Name" = 'GetAllOBOAnalysesQuery') THEN
        INSERT INTO "OperationClaims" ("Name", "Alias", "Description")
        VALUES ('GetAllOBOAnalysesQuery', 'admin.plantanalysis.obo.list', 'View all on-behalf-of plant analyses');
    END IF;
END $$;

-- Step 3: Assign claims to admin user 166 (if not exists)
DO $$
DECLARE
    activity_logs_claim_id INT;
    obo_analyses_claim_id INT;
BEGIN
    -- Get claim IDs
    SELECT "Id" INTO activity_logs_claim_id
    FROM "OperationClaims"
    WHERE "Name" = 'GetActivityLogsQuery';

    SELECT "Id" INTO obo_analyses_claim_id
    FROM "OperationClaims"
    WHERE "Name" = 'GetAllOBOAnalysesQuery';

    -- Assign to admin user 166 (bilgitap@hotmail.com) if not already assigned
    IF NOT EXISTS (SELECT 1 FROM "UserClaims" WHERE "UserId" = 166 AND "ClaimId" = activity_logs_claim_id) THEN
        INSERT INTO "UserClaims" ("UserId", "ClaimId")
        VALUES (166, activity_logs_claim_id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM "UserClaims" WHERE "UserId" = 166 AND "ClaimId" = obo_analyses_claim_id) THEN
        INSERT INTO "UserClaims" ("UserId", "ClaimId")
        VALUES (166, obo_analyses_claim_id);
    END IF;

    RAISE NOTICE 'Successfully processed claims for user 166';
END $$;

-- Step 4: Verify the claims were added
SELECT
    u."FullName" as "User",
    oc."Name" as "Claim",
    oc."Alias",
    oc."Description"
FROM "UserClaims" uc
JOIN "Users" u ON u."UserId" = uc."UserId"
JOIN "OperationClaims" oc ON oc."Id" = uc."ClaimId"
WHERE u."UserId" = 166
AND oc."Name" IN ('GetActivityLogsQuery', 'GetAllOBOAnalysesQuery')
ORDER BY oc."Name";
