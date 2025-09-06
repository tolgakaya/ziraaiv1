-- Railway Production Migration Script
-- Date: 2025-09-06
-- Purpose: Make BirthDate and Gender fields nullable in Users table

-- Step 1: Alter columns to make them nullable
ALTER TABLE "Users" 
ALTER COLUMN "BirthDate" DROP NOT NULL;

ALTER TABLE "Users" 
ALTER COLUMN "Gender" DROP NOT NULL;

-- Step 2: Update any existing infinity values to NULL
UPDATE "Users" 
SET "BirthDate" = NULL 
WHERE "BirthDate" = '-infinity'::timestamp 
   OR "BirthDate" = 'infinity'::timestamp 
   OR "BirthDate" < '1900-01-01'::timestamp;

UPDATE "Users" 
SET "Gender" = NULL 
WHERE "Gender" = 0;

-- Step 3: Verify the changes
SELECT 
    "UserId", 
    "Email", 
    "FullName",
    "BirthDate", 
    "Gender", 
    "RecordDate", 
    "UpdateContactDate" 
FROM "Users" 
ORDER BY "UserId" 
LIMIT 10;

-- Step 4: Check for any remaining issues
SELECT COUNT(*) as users_with_null_birthdate
FROM "Users" 
WHERE "BirthDate" IS NULL;

SELECT COUNT(*) as users_with_null_gender
FROM "Users" 
WHERE "Gender" IS NULL;