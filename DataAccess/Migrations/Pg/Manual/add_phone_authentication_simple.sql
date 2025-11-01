-- ============================================================================
-- Phone-Based Authentication Migration (DBeaver Compatible)
-- ============================================================================
-- Purpose: Enable phone number registration and OTP-based authentication
-- Version: 1.0 - Simplified for DBeaver
-- Date: 2025-10-02
-- ============================================================================

-- STEP 1: Drop existing non-unique index
DROP INDEX IF EXISTS public."IX_Users_MobilePhones";

-- STEP 2: Create unique constraint on MobilePhones (null values excluded)
CREATE UNIQUE INDEX "IX_Users_MobilePhones_Unique"
    ON public."Users" ("MobilePhones")
    WHERE "MobilePhones" IS NOT NULL AND "MobilePhones" != '';

-- STEP 3: Create unique constraint on Email (if not exists)
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email_Unique"
    ON public."Users" ("Email")
    WHERE "Email" IS NOT NULL AND "Email" != '';

-- STEP 4: Add check constraint (Email OR MobilePhones required)
ALTER TABLE public."Users"
    ADD CONSTRAINT "CK_Users_EmailOrPhone_Required"
    CHECK (
        ("Email" IS NOT NULL AND "Email" != '')
        OR
        ("MobilePhones" IS NOT NULL AND "MobilePhones" != '')
    );

-- STEP 5: Make CitizenId nullable (for phone-only users)
ALTER TABLE public."Users"
    ALTER COLUMN "CitizenId" DROP NOT NULL;

-- STEP 6: Add column comments for documentation
COMMENT ON COLUMN public."Users"."MobilePhones" IS
    'Phone number for OTP-based authentication. Format: 05XXXXXXXXX. Unique when not null.';

COMMENT ON COLUMN public."Users"."Email" IS
    'Email for password-based authentication. Unique when not null. Either Email or MobilePhones required.';

COMMENT ON COLUMN public."Users"."CitizenId" IS
    'Turkish Citizen ID. Nullable for phone-only users.';

COMMENT ON COLUMN public."Users"."PasswordHash" IS
    'Password hash for email-based auth. NULL for phone-only (OTP) users.';

COMMENT ON COLUMN public."Users"."PasswordSalt" IS
    'Password salt for email-based auth. NULL for phone-only (OTP) users.';

COMMENT ON TABLE public."Users" IS
    'User accounts. Supports email+password and phone+OTP authentication.';

-- STEP 7: Verification - Display constraints
SELECT
    conname as constraint_name,
    contype as constraint_type
FROM pg_constraint
WHERE conrelid = 'public."Users"'::regclass
AND conname LIKE '%Email%' OR conname LIKE '%Phone%';

-- STEP 8: Verification - Display indexes
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'Users'
AND (indexname LIKE '%Email%' OR indexname LIKE '%Phone%');

-- Success message
SELECT '✓✓✓ Migration completed successfully! ✓✓✓' as status;
