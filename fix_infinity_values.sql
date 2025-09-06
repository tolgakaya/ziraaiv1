-- Fix infinity values in Users table
-- This script handles PostgreSQL infinity datetime values that cause login failures

BEGIN;

-- First, let's see what infinity values we have
SELECT 'Users with infinity values:' as info;
SELECT 
    userid, 
    fullname, 
    email, 
    CASE WHEN birthdate::text = 'infinity' THEN 'INFINITY' ELSE birthdate::text END as birthdate_status,
    CASE WHEN updatecontactdate::text = 'infinity' THEN 'INFINITY' ELSE 'OK' END as updatecontactdate_status,
    CASE WHEN recorddate::text = 'infinity' THEN 'INFINITY' ELSE 'OK' END as recorddate_status
FROM users 
WHERE 
    birthdate::text = 'infinity' 
    OR updatecontactdate::text = 'infinity' 
    OR recorddate::text = 'infinity';

-- Fix infinity values in UpdateContactDate
UPDATE users 
SET updatecontactdate = CURRENT_TIMESTAMP 
WHERE updatecontactdate::text = 'infinity';

-- Fix infinity values in RecordDate 
UPDATE users 
SET recorddate = CURRENT_TIMESTAMP 
WHERE recorddate::text = 'infinity';

-- Fix infinity values in BirthDate - set to NULL since it's nullable
UPDATE users 
SET birthdate = NULL 
WHERE birthdate::text = 'infinity';

-- Show summary of changes
SELECT 'Fixed infinity values. Updated users:' as info;
SELECT COUNT(*) as total_users_updated 
FROM users 
WHERE 
    updatecontactdate = CURRENT_TIMESTAMP 
    OR recorddate = CURRENT_TIMESTAMP
    OR birthdate IS NULL;

-- Verify no infinity values remain
SELECT 'Verification - remaining infinity values (should be 0):' as info;
SELECT COUNT(*) as remaining_infinity_count
FROM users 
WHERE 
    birthdate::text = 'infinity' 
    OR updatecontactdate::text = 'infinity' 
    OR recorddate::text = 'infinity';

COMMIT;