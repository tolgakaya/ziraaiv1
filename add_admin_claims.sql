-- Add Admin Handler Operation Claims
-- These claims are required for SecuredOperation aspect to work with admin handlers

-- Insert new operation claims (100-106)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES
    (100, 'GetAllSubscriptionsQuery', 'Get All Subscriptions', 'Query all subscriptions'),
    (101, 'GetSubscriptionDetailsQuery', 'Get Subscription Details', 'Query detailed subscription information'),
    (102, 'GetSubscriptionByIdQuery', 'Get Subscription By ID', 'Query subscription by ID'),
    (103, 'AssignSubscriptionCommand', 'Assign Subscription', 'Assign subscription to user'),
    (104, 'ExtendSubscriptionCommand', 'Extend Subscription', 'Extend user subscription'),
    (105, 'CancelSubscriptionCommand', 'Cancel Subscription', 'Cancel user subscription'),
    (106, 'BulkCancelSubscriptionsCommand', 'Bulk Cancel Subscriptions', 'Cancel multiple subscriptions')
ON CONFLICT ("Id") DO NOTHING;

-- Add these claims to Administrators group (GroupId = 1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES
    (1, 100),
    (1, 101),
    (1, 102),
    (1, 103),
    (1, 104),
    (1, 105),
    (1, 106)
ON CONFLICT DO NOTHING;

-- Verify the insert
SELECT 'Operation Claims Added:' as status, COUNT(*) as count
FROM "OperationClaims"
WHERE "Id" BETWEEN 100 AND 106;

SELECT 'Group Claims Added:' as status, COUNT(*) as count
FROM "GroupClaims"
WHERE "ClaimId" BETWEEN 100 AND 106;
