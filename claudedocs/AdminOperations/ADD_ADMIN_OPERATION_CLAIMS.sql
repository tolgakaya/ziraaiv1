-- ============================================================================
-- Add Admin Operation Claims for User ID 166 (bilgitap@hotmail.com)
-- ============================================================================
-- This script adds all required operation claims for admin operations
-- Run this after creating the admin user with base admin claims
-- ============================================================================

-- Get the admin operation claim IDs
DO $$
DECLARE
    admin_user_id INT := 166;
    claim_id INT;
    operation_claims TEXT[] := ARRAY[
        'GetAllUsersQuery',
        'GetUserByIdQuery',
        'SearchUsersQuery',
        'DeactivateUserCommand',
        'ReactivateUserCommand',
        'BulkDeactivateUsersCommand',
        'GetAllSubscriptionsQuery',
        'GetSubscriptionByIdQuery',
        'AssignSubscriptionCommand',
        'ExtendSubscriptionCommand',
        'CancelSubscriptionCommand',
        'BulkCancelSubscriptionsCommand',
        'GetSubscriptionStatisticsQuery',
        'GetAllPurchasesQuery',
        'GetPurchaseByIdQuery',
        'CreatePurchaseOnBehalfOfCommand',
        'ApprovePurchaseCommand',
        'RefundPurchaseCommand',
        'GetAllCodesQuery',
        'GetCodeByIdQuery',
        'BulkSendCodesCommand',
        'DeactivateCodeCommand',
        'GetSponsorDetailedReportQuery',
        'GetSponsorshipStatisticsQuery',
        'GetUserStatisticsQuery',
        'ExportStatisticsQuery',
        'GetAllAuditLogsQuery',
        'GetAuditLogsByAdminQuery',
        'GetAuditLogsByTargetQuery',
        'GetUserAnalysesQuery',
        'CreatePlantAnalysisOnBehalfOfCommand'
    ];
    operation_name TEXT;
BEGIN
    -- Loop through each operation claim
    FOREACH operation_name IN ARRAY operation_claims
    LOOP
        -- Get the ClaimId for this operation
        SELECT "Id" INTO claim_id
        FROM "OperationClaims"
        WHERE "Name" = operation_name;

        -- Only insert if ClaimId exists and not already assigned
        IF claim_id IS NOT NULL THEN
            INSERT INTO "UserClaims" ("UserId", "ClaimId")
            VALUES (admin_user_id, claim_id)
            ON CONFLICT ("UserId", "ClaimId") DO NOTHING;

            RAISE NOTICE 'Added claim: % (ID: %)', operation_name, claim_id;
        ELSE
            RAISE NOTICE 'WARNING: Operation claim not found: %', operation_name;
        END IF;
    END LOOP;

    RAISE NOTICE 'Completed adding admin operation claims';
END $$;

-- Verify the claims were added
SELECT
    u."FullName",
    u."Email",
    COUNT(uc."ClaimId") as total_claims
FROM "Users" u
JOIN "UserClaims" uc ON u."Id" = uc."UserId"
WHERE u."Id" = 166
GROUP BY u."FullName", u."Email";

-- Show all admin operation claims
SELECT
    oc."Name" as operation_claim,
    uc."UserId"
FROM "UserClaims" uc
JOIN "OperationClaims" oc ON uc."ClaimId" = oc."Id"
WHERE uc."UserId" = 166
    AND oc."Name" LIKE '%Admin%'
    OR oc."Name" IN (
        'GetAllUsersQuery', 'GetUserByIdQuery', 'SearchUsersQuery',
        'DeactivateUserCommand', 'ReactivateUserCommand', 'BulkDeactivateUsersCommand',
        'GetAllSubscriptionsQuery', 'GetSubscriptionByIdQuery', 'AssignSubscriptionCommand',
        'ExtendSubscriptionCommand', 'CancelSubscriptionCommand', 'BulkCancelSubscriptionsCommand',
        'GetSubscriptionStatisticsQuery', 'GetAllPurchasesQuery', 'GetPurchaseByIdQuery',
        'CreatePurchaseOnBehalfOfCommand', 'ApprovePurchaseCommand', 'RefundPurchaseCommand',
        'GetAllCodesQuery', 'GetCodeByIdQuery', 'BulkSendCodesCommand', 'DeactivateCodeCommand',
        'GetSponsorDetailedReportQuery', 'GetSponsorshipStatisticsQuery',
        'GetUserStatisticsQuery', 'ExportStatisticsQuery',
        'GetAllAuditLogsQuery', 'GetAuditLogsByAdminQuery', 'GetAuditLogsByTargetQuery',
        'GetUserAnalysesQuery', 'CreatePlantAnalysisOnBehalfOfCommand'
    )
ORDER BY oc."Name";
