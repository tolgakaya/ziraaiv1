-- Fix infinity values in Users table - Corrected for PostgreSQL case sensitivity
-- This script handles PostgreSQL infinity datetime values that cause login failures

BEGIN;

-- First, let's see what infinity values we have (using lowercase table/column names)
SELECT 'Users with infinity values:' as info;
SELECT 
    "UserId" as userid, 
    "FullName" as fullname, 
    "Email" as email, 
    CASE WHEN "BirthDate"::text = 'infinity' THEN 'INFINITY' ELSE "BirthDate"::text END as birthdate_status,
    CASE WHEN "UpdateContactDate"::text = 'infinity' THEN 'INFINITY' ELSE 'OK' END as updatecontactdate_status,
    CASE WHEN "RecordDate"::text = 'infinity' THEN 'INFINITY' ELSE 'OK' END as recorddate_status
FROM "Users" 
WHERE 
    "BirthDate"::text = 'infinity' 
    OR "UpdateContactDate"::text = 'infinity' 
    OR "RecordDate"::text = 'infinity';

-- Fix infinity values in UpdateContactDate
UPDATE "Users" 
SET "UpdateContactDate" = CURRENT_TIMESTAMP 
WHERE "UpdateContactDate"::text = 'infinity';

-- Fix infinity values in RecordDate 
UPDATE "Users" 
SET "RecordDate" = CURRENT_TIMESTAMP 
WHERE "RecordDate"::text = 'infinity';

-- Fix infinity values in BirthDate - set to NULL since it's nullable
UPDATE "Users" 
SET "BirthDate" = NULL 
WHERE "BirthDate"::text = 'infinity';

-- Show summary of changes
SELECT 'Fixed infinity values. Rows updated:' as info;
SELECT 
    COUNT(CASE WHEN "UpdateContactDate" >= CURRENT_TIMESTAMP - INTERVAL '1 minute' THEN 1 END) as updatecontactdate_fixed,
    COUNT(CASE WHEN "RecordDate" >= CURRENT_TIMESTAMP - INTERVAL '1 minute' THEN 1 END) as recorddate_fixed,
    COUNT(CASE WHEN "BirthDate" IS NULL THEN 1 END) as birthdate_nullified
FROM "Users";

-- Verify no infinity values remain
SELECT 'Verification - remaining infinity values (should be 0):' as info;
SELECT COUNT(*) as remaining_infinity_count
FROM "Users" 
WHERE 
    "BirthDate"::text = 'infinity' 
    OR "UpdateContactDate"::text = 'infinity' 
    OR "RecordDate"::text = 'infinity';

COMMIT;