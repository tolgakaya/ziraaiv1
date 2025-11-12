-- Migration: Add Crop-Disease Matrix Analytics Operation Claim
-- Date: 2025-11-12
-- Feature: Crop-Disease Matrix Analytics (Priority 5)
-- Endpoint: GET /api/v1/sponsorship/crop-disease-matrix
-- Authorization: Sponsor (GroupId=3), Admin (GroupId=1)

-- Step 1: Add OperationClaim for GetCropDiseaseMatrixQuery
-- Expected ID: 166 (next available from operation_claims.csv)
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES (
    166,
    'GetCropDiseaseMatrixQuery',
    'sponsorship.analytics.crop-disease-matrix',
    'View crop-disease correlation analytics for sponsors with market opportunities'
);

-- Step 2: Assign claim to Admin group (GroupId = 1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES (1, 166);

-- Step 3: Assign claim to Sponsor group (GroupId = 3)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES (3, 166);

-- Verification queries (run after migration):
-- SELECT * FROM "OperationClaims" WHERE "Id" = 166;
-- SELECT gc.*, g."GroupName", oc."Name", oc."Alias"
-- FROM "GroupClaims" gc
-- JOIN "Groups" g ON gc."GroupId" = g."Id"
-- JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
-- WHERE oc."Id" = 166;
