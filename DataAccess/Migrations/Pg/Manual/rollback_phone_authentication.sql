-- ============================================================================
-- Rollback Phone-Based Authentication Migration
-- ============================================================================
-- Purpose: Revert phone authentication changes if needed
-- Use only in emergencies!
-- ============================================================================

-- STEP 1: Remove check constraint
ALTER TABLE public."Users" DROP CONSTRAINT IF EXISTS "CK_Users_EmailOrPhone_Required";

-- STEP 2: Remove unique indexes
DROP INDEX IF EXISTS public."IX_Users_MobilePhones_Unique";
DROP INDEX IF EXISTS public."IX_Users_Email_Unique";

-- STEP 3: Restore original MobilePhones index (non-unique)
CREATE INDEX "IX_Users_MobilePhones" ON public."Users" USING btree ("MobilePhones");

-- STEP 4: Make CitizenId NOT NULL again (WARNING: This will fail if NULL values exist!)
-- Comment this out if you have phone-only users already created
-- ALTER TABLE public."Users" ALTER COLUMN "CitizenId" SET NOT NULL;

-- STEP 5: Remove column comments
COMMENT ON COLUMN public."Users"."MobilePhones" IS NULL;
COMMENT ON COLUMN public."Users"."Email" IS NULL;
COMMENT ON COLUMN public."Users"."CitizenId" IS NULL;
COMMENT ON COLUMN public."Users"."PasswordHash" IS NULL;
COMMENT ON COLUMN public."Users"."PasswordSalt" IS NULL;
COMMENT ON TABLE public."Users" IS NULL;

-- Verification
SELECT 'Rollback completed' as status;
