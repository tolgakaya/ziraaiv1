-- ============================================================================
-- Phone-Based Authentication Migration
-- ============================================================================
-- Purpose: Enable phone number registration and OTP-based authentication
-- Version: 1.0
-- Date: 2025-10-02
-- Author: Claude Code
-- ============================================================================

-- IMPORTANT: Run this script in a transaction for safety
-- Test on development/staging before production!

BEGIN;

-- ============================================================================
-- STEP 1: Data Validation (Pre-migration checks)
-- ============================================================================

-- Check for duplicate phone numbers (should be none, but verify)
DO $$
DECLARE
    duplicate_count INT;
BEGIN
    SELECT COUNT(*) INTO duplicate_count
    FROM (
        SELECT "MobilePhones", COUNT(*)
        FROM public."Users"
        WHERE "MobilePhones" IS NOT NULL AND "MobilePhones" != ''
        GROUP BY "MobilePhones"
        HAVING COUNT(*) > 1
    ) duplicates;

    IF duplicate_count > 0 THEN
        RAISE EXCEPTION 'Found % duplicate phone numbers. Clean data first!', duplicate_count;
    ELSE
        RAISE NOTICE '✓ No duplicate phone numbers found';
    END IF;
END $$;

-- Check for users without Email AND MobilePhones (should follow business rules)
DO $$
DECLARE
    invalid_users INT;
BEGIN
    SELECT COUNT(*) INTO invalid_users
    FROM public."Users"
    WHERE (("Email" IS NULL OR "Email" = '')
           AND ("MobilePhones" IS NULL OR "MobilePhones" = ''));

    IF invalid_users > 0 THEN
        RAISE WARNING 'Found % users without Email and Phone. These may cause issues.', invalid_users;
    ELSE
        RAISE NOTICE '✓ All users have at least Email or Phone';
    END IF;
END $$;

-- ============================================================================
-- STEP 2: Create Unique Constraint on MobilePhones
-- ============================================================================

-- Drop existing non-unique index (we'll replace with unique)
DROP INDEX IF EXISTS "IX_Users_MobilePhones";

-- Create unique partial index (only for non-null, non-empty values)
CREATE UNIQUE INDEX "IX_Users_MobilePhones_Unique"
    ON public."Users" ("MobilePhones")
    WHERE "MobilePhones" IS NOT NULL AND "MobilePhones" != '';

DO $$ BEGIN RAISE NOTICE '✓ Created unique constraint on MobilePhones'; END $$;

-- ============================================================================
-- STEP 3: Create Unique Constraint on Email (if not exists)
-- ============================================================================

-- Check if Email already has unique constraint
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE tablename = 'Users'
        AND indexname LIKE '%Email%'
        AND indexdef LIKE '%UNIQUE%'
    ) THEN
        -- Create unique partial index for Email
        CREATE UNIQUE INDEX "IX_Users_Email_Unique"
            ON public."Users" ("Email")
            WHERE "Email" IS NOT NULL AND "Email" != '';

        RAISE NOTICE '✓ Created unique constraint on Email';
    ELSE
        RAISE NOTICE '✓ Email unique constraint already exists';
    END IF;
END $$;

-- ============================================================================
-- STEP 4: Add Check Constraint (Email OR MobilePhones required)
-- ============================================================================

-- Add constraint: At least one of Email or MobilePhones must be provided
ALTER TABLE public."Users"
    ADD CONSTRAINT "CK_Users_EmailOrPhone_Required"
    CHECK (
        ("Email" IS NOT NULL AND "Email" != '')
        OR
        ("MobilePhones" IS NOT NULL AND "MobilePhones" != '')
    );

DO $$ BEGIN RAISE NOTICE '✓ Added check constraint: Email OR Phone required'; END $$;

-- ============================================================================
-- STEP 5: Make CitizenId Nullable (for phone-only users)
-- ============================================================================

-- Note: Phone-only users won't have CitizenId
-- We'll use 0 as default for backwards compatibility
ALTER TABLE public."Users"
    ALTER COLUMN "CitizenId" DROP NOT NULL;

-- Update existing records with CitizenId = 0 to NULL (optional, for clarity)
-- Uncomment if you want to clean up existing data
-- UPDATE public."Users" SET "CitizenId" = NULL WHERE "CitizenId" = 0;

DO $$ BEGIN RAISE NOTICE '✓ Made CitizenId nullable'; END $$;

-- ============================================================================
-- STEP 6: Add Column Comments for Documentation
-- ============================================================================

COMMENT ON COLUMN public."Users"."MobilePhones" IS
    'Phone number for OTP-based authentication. Format: 05XXXXXXXXX (11 digits for Turkey). Unique when not null. Used for SMS OTP login.';

COMMENT ON COLUMN public."Users"."Email" IS
    'Email for password-based authentication. Unique when not null. Either Email or MobilePhones must be provided.';

COMMENT ON COLUMN public."Users"."CitizenId" IS
    'Turkish Citizen ID (TC Kimlik No). Nullable. Use 0 or NULL for phone-only users or non-citizens.';

COMMENT ON COLUMN public."Users"."PasswordHash" IS
    'Password hash for email-based authentication. NULL for phone-only (OTP-based) users.';

COMMENT ON COLUMN public."Users"."PasswordSalt" IS
    'Password salt for email-based authentication. NULL for phone-only (OTP-based) users.';

COMMENT ON TABLE public."Users" IS
    'User accounts. Supports both email+password and phone+OTP authentication methods.';

DO $$ BEGIN RAISE NOTICE '✓ Added documentation comments'; END $$;

-- ============================================================================
-- STEP 7: Verification Queries
-- ============================================================================

-- Verify constraints were created
DO $$
DECLARE
    constraint_count INT;
    index_count INT;
BEGIN
    -- Check constraints
    SELECT COUNT(*) INTO constraint_count
    FROM pg_constraint
    WHERE conname = 'CK_Users_EmailOrPhone_Required';

    -- Check indexes
    SELECT COUNT(*) INTO index_count
    FROM pg_indexes
    WHERE tablename = 'Users'
    AND indexname IN ('IX_Users_MobilePhones_Unique', 'IX_Users_Email_Unique');

    RAISE NOTICE '========================================';
    RAISE NOTICE 'Migration Verification:';
    RAISE NOTICE '  Check Constraints: %', constraint_count;
    RAISE NOTICE '  Unique Indexes: %', index_count;
    RAISE NOTICE '========================================';

    IF constraint_count = 0 OR index_count = 0 THEN
        RAISE WARNING 'Some constraints or indexes may not have been created!';
    END IF;
END $$;

-- Display final table structure
SELECT
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'Users'
ORDER BY ordinal_position;

COMMIT;

-- ============================================================================
-- SUCCESS MESSAGE
-- ============================================================================
DO $$
BEGIN
    RAISE NOTICE '========================================';
    RAISE NOTICE '✓✓✓ MIGRATION COMPLETED SUCCESSFULLY ✓✓✓';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Next Steps:';
    RAISE NOTICE '  1. Deploy application code changes';
    RAISE NOTICE '  2. Test phone registration endpoint';
    RAISE NOTICE '  3. Test phone login with OTP';
    RAISE NOTICE '  4. Verify email authentication still works';
    RAISE NOTICE '========================================';
END $$;

-- ============================================================================
-- ROLLBACK SCRIPT (Keep for emergency rollback)
-- ============================================================================
/*
-- Run this in case you need to rollback the migration

BEGIN;

-- Remove check constraint
ALTER TABLE public."Users" DROP CONSTRAINT IF EXISTS "CK_Users_EmailOrPhone_Required";

-- Remove unique indexes
DROP INDEX IF EXISTS "IX_Users_MobilePhones_Unique";
DROP INDEX IF EXISTS "IX_Users_Email_Unique";

-- Restore original MobilePhones index (non-unique)
CREATE INDEX "IX_Users_MobilePhones" ON public."Users" USING btree ("MobilePhones");

-- Make CitizenId NOT NULL again
ALTER TABLE public."Users" ALTER COLUMN "CitizenId" SET NOT NULL;

-- Remove comments
COMMENT ON COLUMN public."Users"."MobilePhones" IS NULL;
COMMENT ON COLUMN public."Users"."Email" IS NULL;
COMMENT ON COLUMN public."Users"."CitizenId" IS NULL;
COMMENT ON COLUMN public."Users"."PasswordHash" IS NULL;
COMMENT ON COLUMN public."Users"."PasswordSalt" IS NULL;
COMMENT ON TABLE public."Users" IS NULL;

COMMIT;

RAISE NOTICE 'Migration rolled back successfully';
*/
