-- =============================================
-- Script: 09_add_payment_operation_claims.sql
-- Description: Add operation claims for Payment endpoints
-- Author: Claude Code
-- Date: 2025-11-21
-- Phase: 9 - SecuredOperations & Claims
-- =============================================

-- Add operation claims for payment endpoints
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description", "CreatedDate", "UpdatedDate")
VALUES
    (185, 'InitializePayment', 'payment.initialize', 'Initialize payment transaction with iyzico', NOW(), NOW()),
    (186, 'VerifyPayment', 'payment.verify', 'Verify payment status after iyzico completion', NOW(), NOW()),
    (187, 'GetPaymentStatus', 'payment.status', 'Query payment transaction status by token', NOW(), NOW()),
    (188, 'ProcessPaymentWebhook', 'payment.webhook', 'Process iyzico payment webhook callbacks (internal use)', NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;

-- Verify insertion
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 185 AND 188
ORDER BY "Id";

-- Note: Webhook endpoint (ProcessPaymentWebhook) is marked for reference only
-- It uses [AllowAnonymous] attribute and should not have SecuredOperation in controller
-- The other 3 endpoints will use [SecuredOperation] attributes

-- Grant payment claims to authenticated users (will be handled by role-based assignment)
-- Sponsors: Can initialize and verify payments for bulk code purchases
-- Farmers: Can initialize and verify payments for subscription purchases
-- Both roles will automatically have these claims through their group assignments
