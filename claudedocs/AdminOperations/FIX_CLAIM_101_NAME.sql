-- Fix Claim 101 name from GetSponsorDetailedReportQuery to GetSubscriptionDetailsQuery
-- REASON: Database has wrong claim name, causing 401 errors on /api/admin/subscriptions/details

-- Show current state
SELECT 'BEFORE UPDATE - Claim 101:' as info;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" = 101;

-- Update the claim name
UPDATE "OperationClaims"
SET
    "Name" = 'GetSubscriptionDetailsQuery',
    "Alias" = 'Get Subscription Details',
    "Description" = 'Query detailed subscription information with user and usage data'
WHERE "Id" = 101;

-- Show updated state
SELECT 'AFTER UPDATE - Claim 101:' as info;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Id" = 101;

-- Verify it's assigned to Administrators group
SELECT 'Group Assignment Check:' as info;
SELECT gc."GroupId", g."GroupName", oc."Name" as ClaimName
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE gc."ClaimId" = 101;

-- IMPORTANT: After running this, you MUST logout/login to refresh the claim cache!
