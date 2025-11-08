-- Fix Admin Subscription Handler Claims
-- Current database has wrong claim names, need to update them

-- First, let's see what we have
SELECT 'Current Claims (100-106):' as info;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 100 AND 106
ORDER BY "Id";

-- Update the wrong claim names to match handler class names
UPDATE "OperationClaims"
SET "Name" = 'GetAllSubscriptionsQuery',
    "Alias" = 'Get All Subscriptions',
    "Description" = 'Query all subscriptions with filters'
WHERE "Id" = 100;

UPDATE "OperationClaims"
SET "Name" = 'GetSubscriptionDetailsQuery',
    "Alias" = 'Get Subscription Details',
    "Description" = 'Query detailed subscription information with user and usage data'
WHERE "Id" = 101;

UPDATE "OperationClaims"
SET "Name" = 'GetSubscriptionByIdQuery',
    "Alias" = 'Get Subscription By ID',
    "Description" = 'Query single subscription by ID'
WHERE "Id" = 102;

UPDATE "OperationClaims"
SET "Name" = 'AssignSubscriptionCommand',
    "Alias" = 'Assign Subscription',
    "Description" = 'Assign subscription to user with queue control'
WHERE "Id" = 103;

-- 104, 105, 106 are already correct (ExtendSubscriptionCommand, CancelSubscriptionCommand, BulkCancelSubscriptionsCommand)

-- Verify the updates
SELECT 'Updated Claims:' as info;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" BETWEEN 100 AND 106
ORDER BY "Id";

-- Verify GroupClaims mapping (should already exist)
SELECT 'Group Claims Mapping:' as info;
SELECT gc."GroupId", g."GroupName", gc."ClaimId", oc."Name"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."ClaimId" BETWEEN 100 AND 106
ORDER BY gc."ClaimId";
