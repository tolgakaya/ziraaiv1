-- Migration: Add Message Engagement Analytics Operation Claim
-- Date: 2025-11-12
-- Feature: Message Engagement Analytics (Priority 6)
-- Endpoint: GET /api/v1/sponsorship/message-engagement
-- Authorization: Sponsor (GroupId=3), Admin (GroupId=1)

-- Step 1: Add OperationClaim for GetMessageEngagementQuery
-- Expected ID: 167 (next available from operation_claims.csv)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES (
    167,
    'GetMessageEngagementQuery',
    'sponsorship.analytics.message-engagement',
    'View message engagement analytics including response rates, engagement score, and optimal messaging times'
);

-- Step 2: Assign claim to Admin group (GroupId = 1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES (1, 167);

-- Step 3: Assign claim to Sponsor group (GroupId = 3)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES (3, 167);

-- Verification queries (run after migration):
-- SELECT * FROM "OperationClaims" WHERE "Id" = 167;
-- SELECT gc.*, g."GroupName", oc."Name", oc."Alias"
-- FROM "GroupClaims" gc
-- JOIN "Groups" g ON gc."GroupId" = g."Id"
-- JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
-- WHERE oc."Id" = 167;
