-- =====================================================
-- Referral System Rollback Script
-- Date: 2025-10-03
-- Description: Rollback script for referral system migration
-- WARNING: This will delete all referral data!
-- =====================================================

BEGIN;

-- =====================================================
-- PART 1: DROP TABLES (in reverse order of dependencies)
-- =====================================================

-- Drop ReferralRewards (depends on ReferralTracking and Users)
DROP TABLE IF EXISTS public."ReferralRewards" CASCADE;

-- Drop ReferralTracking (depends on ReferralCodes and Users)
DROP TABLE IF EXISTS public."ReferralTracking" CASCADE;

-- Drop ReferralCodes (depends on Users)
DROP TABLE IF EXISTS public."ReferralCodes" CASCADE;

-- Drop ReferralConfigurations (standalone)
DROP TABLE IF EXISTS public."ReferralConfigurations" CASCADE;


-- =====================================================
-- PART 2: REMOVE COLUMNS FROM EXISTING TABLES
-- =====================================================

-- Remove ReferralCredits from UserSubscriptions
DROP INDEX IF EXISTS public."IX_UserSubscriptions_ReferralCredits";
ALTER TABLE public."UserSubscriptions" DROP COLUMN IF EXISTS "ReferralCredits";

-- Remove RegistrationReferralCode from Users
DROP INDEX IF EXISTS public."IX_Users_RegistrationReferralCode";
ALTER TABLE public."Users" DROP COLUMN IF EXISTS "RegistrationReferralCode";


-- =====================================================
-- PART 3: VERIFY ROLLBACK
-- =====================================================

-- Verify tables dropped
SELECT
    COUNT(*) as remaining_referral_tables
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('ReferralCodes', 'ReferralTracking', 'ReferralRewards', 'ReferralConfigurations');
-- Expected result: 0

-- Verify columns removed
SELECT
    COUNT(*) as remaining_referral_columns
FROM information_schema.columns
WHERE table_schema = 'public'
  AND (
      (table_name = 'UserSubscriptions' AND column_name = 'ReferralCredits')
      OR (table_name = 'Users' AND column_name = 'RegistrationReferralCode')
  );
-- Expected result: 0

COMMIT;

-- =====================================================
-- ROLLBACK COMPLETE
-- =====================================================
-- All referral system components have been removed
-- =====================================================
