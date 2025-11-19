-- SMS Service Integration - Operation Claims Migration
-- Date: 2025-11-19
-- Description: Adds operation claims for SMS test and provider info endpoints
-- Branch: feature/sms-service-integration

-- ============================================
-- 1. Add Operation Claims
-- ============================================

-- Test SMS Command (Admin only)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES (183, 'TestSmsCommand', 'sms.admin.test', 'Test SMS sending to verify provider configuration')
ON CONFLICT ("Id") DO NOTHING;

-- Get SMS Provider Info Query (Admin only)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES (184, 'GetSmsProviderInfoQuery', 'sms.admin.provider-info', 'View SMS provider information and status')
ON CONFLICT ("Id") DO NOTHING;

-- ============================================
-- 2. Add Group Claims (Admin Group = 1)
-- ============================================

-- Get next GroupClaim IDs (adjust if needed based on current max)
-- You may need to check: SELECT MAX("Id") FROM "GroupClaims";

-- TestSmsCommand for Admin group
INSERT INTO "GroupClaims" ("GroupId", "OperationClaimId")
SELECT 1, 183
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims"
    WHERE "GroupId" = 1 AND "OperationClaimId" = 183
);

-- GetSmsProviderInfoQuery for Admin group
INSERT INTO "GroupClaims" ("GroupId", "OperationClaimId")
SELECT 1, 184
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims"
    WHERE "GroupId" = 1 AND "OperationClaimId" = 184
);

-- ============================================
-- 3. Verification Queries
-- ============================================

-- Verify operation claims were added
SELECT * FROM "OperationClaims" WHERE "Id" IN (183, 184);

-- Verify group claims were added for Admin
SELECT gc.*, oc."Name", oc."Alias"
FROM "GroupClaims" gc
JOIN "OperationClaims" oc ON gc."OperationClaimId" = oc."Id"
WHERE gc."GroupId" = 1 AND oc."Id" IN (183, 184);

-- ============================================
-- Rollback Script (if needed)
-- ============================================
/*
-- Remove group claims first
DELETE FROM "GroupClaims" WHERE "OperationClaimId" IN (183, 184);

-- Then remove operation claims
DELETE FROM "OperationClaims" WHERE "Id" IN (183, 184);
*/
