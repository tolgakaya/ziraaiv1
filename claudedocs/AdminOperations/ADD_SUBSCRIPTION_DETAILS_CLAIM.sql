-- Add GetSubscriptionDetailsQuery claim for admin subscription details endpoint
-- This is DIFFERENT from GetSponsorDetailedReportQuery (which is currently ID 101)

-- First check what's the highest claim ID
SELECT 'Current Max Claim ID:' as info, MAX("Id") as max_id FROM "OperationClaims";

-- Insert GetSubscriptionDetailsQuery with next available ID
-- Using ID 107 (next after 106)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
SELECT 107, 'GetSubscriptionDetailsQuery', 'Get Subscription Details', 'Query detailed subscription information with user and usage data'
WHERE NOT EXISTS (
    SELECT 1 FROM "OperationClaims" WHERE "Name" = 'GetSubscriptionDetailsQuery'
);

-- Add this claim to Administrators group (GroupId = 1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
SELECT 1, 107
WHERE NOT EXISTS (
    SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 107
);

-- Verify the insert
SELECT 'New Claim Added:' as info;
SELECT "Id", "Name", "Alias", "Description"
FROM "OperationClaims"
WHERE "Name" = 'GetSubscriptionDetailsQuery';

SELECT 'Assigned to Admin Group:' as info;
SELECT gc."GroupId", g."GroupName", oc."Name"
FROM "GroupClaims" gc
JOIN "Groups" g ON gc."GroupId" = g."Id"
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Name" = 'GetSubscriptionDetailsQuery';
