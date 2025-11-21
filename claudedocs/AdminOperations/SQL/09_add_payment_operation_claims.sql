-- ============================================================================
-- Payment System - Operation Claims
-- ============================================================================
-- Script: 09_add_payment_operation_claims.sql
-- Purpose: Add operation claims for payment endpoints (iyzico integration)
-- Date: 2025-11-21
-- Author: Claude Code
-- Phase: 9 - SecuredOperations & Claims
--
-- Claims Structure:
-- - 185: InitializePayment (Initialize payment transaction)
-- - 186: VerifyPayment (Verify payment after completion)
-- - 187: GetPaymentStatus (Query payment status)
-- - 188: ProcessPaymentWebhook (Internal reference - NOT assigned to groups)
--
-- Assignment Strategy:
-- - Sponsors (GroupId = 3): Need all payment claims for bulk code purchases
-- - Farmers (GroupId = 2): Need all payment claims for subscription purchases
-- - Webhook endpoint uses [AllowAnonymous] - claim 188 is for reference only
-- ============================================================================

-- ============================================================================
-- 1. INSERT OPERATION CLAIMS
-- ============================================================================

-- Claim 185: Initialize Payment
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    185,
    'InitializePayment',
    'payment.initialize',
    'Initialize payment transaction with iyzico'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 185
);

-- Claim 186: Verify Payment
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    186,
    'VerifyPayment',
    'payment.verify',
    'Verify payment status after iyzico completion'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 186
);

-- Claim 187: Get Payment Status
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    187,
    'GetPaymentStatus',
    'payment.status',
    'Query payment transaction status by token'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 187
);

-- Claim 188: Process Payment Webhook (Reference Only)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT
    188,
    'ProcessPaymentWebhook',
    'payment.webhook',
    'Process iyzico payment webhook callbacks (internal use)'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Id" = 188
);

-- ============================================================================
-- 2. ASSIGN TO FARMER GROUP (GroupId = 2)
-- ============================================================================
-- Farmers need payment claims to purchase subscriptions

-- Assign Claim 185 to Farmers
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 2, 185
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 2 AND "ClaimId" = 185
);

-- Assign Claim 186 to Farmers
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 2, 186
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 2 AND "ClaimId" = 186
);

-- Assign Claim 187 to Farmers
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 2, 187
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 2 AND "ClaimId" = 187
);

-- ============================================================================
-- 3. ASSIGN TO SPONSOR GROUP (GroupId = 3)
-- ============================================================================
-- Sponsors need payment claims to purchase bulk sponsorship codes

-- Assign Claim 185 to Sponsors
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 3, 185
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 3 AND "ClaimId" = 185
);

-- Assign Claim 186 to Sponsors
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 3, 186
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 3 AND "ClaimId" = 186
);

-- Assign Claim 187 to Sponsors
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 3, 187
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 3 AND "ClaimId" = 187
);

-- ============================================================================
-- 4. VERIFICATION QUERIES
-- ============================================================================

-- Verify claims were inserted
SELECT
    "Id",
    "Name",
    "Alias",
    "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 185 AND 188
ORDER BY "Id";

-- Verify group assignments for Farmers (GroupId = 2)
SELECT
    gc."GroupId",
    g."Name" AS "GroupName",
    gc."ClaimId",
    oc."Name" AS "ClaimName",
    oc."Alias"
FROM "GroupClaims" gc
INNER JOIN "Groups" g ON gc."GroupId" = g."Id"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 2
  AND gc."ClaimId" BETWEEN 185 AND 187
ORDER BY gc."ClaimId";

-- Verify group assignments for Sponsors (GroupId = 3)
SELECT
    gc."GroupId",
    g."Name" AS "GroupName",
    gc."ClaimId",
    oc."Name" AS "ClaimName",
    oc."Alias"
FROM "GroupClaims" gc
INNER JOIN "Groups" g ON gc."GroupId" = g."Id"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."GroupId" = 3
  AND gc."ClaimId" BETWEEN 185 AND 187
ORDER BY gc."ClaimId";

-- ============================================================================
-- 5. TEST VERIFICATION (FOR USERS)
-- ============================================================================

-- List sample Farmer users who will inherit payment claims
SELECT DISTINCT
    u."Id" AS "UserId",
    u."Email",
    u."FirstName",
    u."LastName",
    g."Name" AS "GroupName",
    oc."Id" AS "ClaimId",
    oc."Name" AS "ClaimName"
FROM "Users" u
INNER JOIN "UserGroups" ug ON u."Id" = ug."UserId"
INNER JOIN "Groups" g ON ug."GroupId" = g."Id"
INNER JOIN "GroupClaims" gc ON g."Id" = gc."GroupId"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 2  -- Farmers group
  AND oc."Id" BETWEEN 185 AND 187
ORDER BY u."Email", oc."Id"
LIMIT 5;

-- List sample Sponsor users who will inherit payment claims
SELECT DISTINCT
    u."Id" AS "UserId",
    u."Email",
    u."FirstName",
    u."LastName",
    g."Name" AS "GroupName",
    oc."Id" AS "ClaimId",
    oc."Name" AS "ClaimName"
FROM "Users" u
INNER JOIN "UserGroups" ug ON u."Id" = ug."UserId"
INNER JOIN "Groups" g ON ug."GroupId" = g."Id"
INNER JOIN "GroupClaims" gc ON g."Id" = gc."GroupId"
INNER JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 3  -- Sponsors group
  AND oc."Id" BETWEEN 185 AND 187
ORDER BY u."Email", oc."Id"
LIMIT 5;

-- ============================================================================
-- 6. ROLLBACK SCRIPT (IF NEEDED - DO NOT EXECUTE UNLESS REQUIRED)
-- ============================================================================

/*
-- Remove GroupClaims assignments for Farmers
DELETE FROM "GroupClaims"
WHERE "ClaimId" BETWEEN 185 AND 187 AND "GroupId" = 2;

-- Remove GroupClaims assignments for Sponsors
DELETE FROM "GroupClaims"
WHERE "ClaimId" BETWEEN 185 AND 187 AND "GroupId" = 3;

-- Remove OperationClaims
DELETE FROM "OperationClaims"
WHERE "Id" BETWEEN 185 AND 188;
*/

-- ============================================================================
-- IMPORTANT REMINDERS:
-- ============================================================================
-- 1. ⚠️ Users must LOGOUT and LOGIN after this script to refresh claims cache
-- 2. 📋 Verify the controller action names match exactly:
--    - PaymentController.InitializePayment → InitializePayment
--    - PaymentController.VerifyPayment → VerifyPayment
--    - PaymentController.GetPaymentStatus → GetPaymentStatus
-- 3. 🔒 GroupId 2 = Farmers, GroupId 3 = Sponsors - verify in your database
-- 4. ✅ Claims 185-188 must be unused - verify no conflicts
-- 5. 🌐 Claim 188 (ProcessPaymentWebhook) is NOT assigned to any group
--    Webhook endpoint uses [AllowAnonymous] for iyzico callbacks
-- 6. 💰 Payment Flows:
--    - Farmers: FarmerSubscription flow (purchase subscription)
--    - Sponsors: SponsorBulkPurchase flow (purchase bulk codes)
-- ============================================================================

-- End of script
