-- ============================================================================
-- Create Admin User Script for ZiraAI
-- ============================================================================
-- This script creates a new admin user with full admin permissions
--
-- IMPORTANT: Change the default password after first login!
-- ============================================================================

-- Step 1: Create the admin user
-- Default password: Admin@123 (MUST be changed after first login)
-- Password hash for "Admin@123"
INSERT INTO "Users" (
    "FullName",
    "Email",
    "MobilePhones",
    "PasswordHash",
    "PasswordSalt",
    "IsActive",
    "Status",
    "CreatedDate"
)
VALUES (
    'System Administrator',
    'admin@ziraai.com',
    '+905550000000',
    -- Hash for password "Admin@123"
    -- You should generate this using your password hashing service
    -- For now, use the registration endpoint to create first admin
    NULL,  -- Will be set via registration or update
    NULL,  -- Will be set via registration or update
    true,
    true,
    NOW()
)
RETURNING "UserId";

-- Note the returned UserId, you'll need it for the next steps
-- Let's say the returned UserId is 1 for this example

-- ============================================================================
-- Step 2: Get all admin operation claims
-- ============================================================================
SELECT "Id", "Name" FROM "OperationClaims"
WHERE "Name" LIKE 'admin.%'
ORDER BY "Name";

-- ============================================================================
-- Step 3: Assign all admin claims to the user
-- Replace :adminUserId with the actual UserId from Step 1
-- ============================================================================

-- Assign all admin operation claims
INSERT INTO "UserOperationClaims" ("UserId", "OperationClaimId")
SELECT
    :adminUserId,  -- Replace with actual UserId
    "Id"
FROM "OperationClaims"
WHERE "Name" LIKE 'admin.%';

-- Also assign basic user claims
INSERT INTO "UserOperationClaims" ("UserId", "OperationClaimId")
SELECT
    :adminUserId,  -- Replace with actual UserId
    "Id"
FROM "OperationClaims"
WHERE "Name" IN ('User', 'Admin');

-- ============================================================================
-- Step 4: Verify admin user creation
-- ============================================================================
SELECT
    u."UserId",
    u."FullName",
    u."Email",
    u."IsActive",
    COUNT(uoc."Id") as "TotalClaims"
FROM "Users" u
LEFT JOIN "UserOperationClaims" uoc ON u."UserId" = uoc."UserId"
WHERE u."Email" = 'admin@ziraai.com'
GROUP BY u."UserId", u."FullName", u."Email", u."IsActive";

-- ============================================================================
-- Step 5: List all assigned claims
-- ============================================================================
SELECT
    u."FullName",
    u."Email",
    oc."Name" as "ClaimName"
FROM "Users" u
INNER JOIN "UserOperationClaims" uoc ON u."UserId" = uoc."UserId"
INNER JOIN "OperationClaims" oc ON uoc."OperationClaimId" = oc."Id"
WHERE u."Email" = 'admin@ziraai.com'
ORDER BY oc."Name";

-- ============================================================================
-- ALTERNATIVE: Create admin claims if they don't exist
-- ============================================================================
-- Run this if admin claims are missing in OperationClaims table

INSERT INTO "OperationClaims" ("Name")
VALUES
    ('Admin'),
    ('admin.users.manage'),
    ('admin.subscriptions.manage'),
    ('admin.sponsorship.manage'),
    ('admin.analytics.view'),
    ('admin.audit.view'),
    ('admin.plantanalysis.manage')
ON CONFLICT ("Name") DO NOTHING;

-- ============================================================================
-- IMPORTANT NOTES:
-- ============================================================================
-- 1. Password must be set using the application's registration endpoint
--    or password reset functionality to ensure proper hashing
--
-- 2. For initial setup, use this workflow:
--    a) Create user with SQL (without password)
--    b) Use password reset endpoint to set password
--    OR
--    a) Use registration endpoint with admin email
--    b) Then run Step 3 to assign admin claims
--
-- 3. SECURITY: Never store passwords in plain text or weak hashes
--
-- 4. Change the default email and phone to your actual admin contact
--
-- 5. After first login, immediately change the password
-- ============================================================================
