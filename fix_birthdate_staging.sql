-- STAGING ENVIRONMENT FIX
-- Database: ziraai_dev (localhost:5432)
-- Fix BirthDate and Gender fields to be nullable

-- First, let's check current state
SELECT 
    table_name,
    column_name, 
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name IN ('Users', 'users')
AND column_name IN ('BirthDate', 'birthdate', 'Gender', 'gender')
ORDER BY table_name, column_name;

-- Fix BirthDate column to allow NULL
DO $$ 
BEGIN
    -- Try PascalCase first (most likely)
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Users' 
        AND column_name = 'BirthDate'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE "Users" ALTER COLUMN "BirthDate" DROP NOT NULL;
        RAISE NOTICE 'BirthDate column made nullable in Users table';
    END IF;
    
    -- Try lowercase
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'birthdate'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE users ALTER COLUMN birthdate DROP NOT NULL;
        RAISE NOTICE 'birthdate column made nullable in users table';
    END IF;
END $$;

-- Fix Gender column to allow NULL
DO $$ 
BEGIN
    -- Try PascalCase first (most likely)
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Users' 
        AND column_name = 'Gender'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE "Users" ALTER COLUMN "Gender" DROP NOT NULL;
        RAISE NOTICE 'Gender column made nullable in Users table';
    END IF;
    
    -- Try lowercase
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'gender'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE users ALTER COLUMN gender DROP NOT NULL;
        RAISE NOTICE 'gender column made nullable in users table';
    END IF;
END $$;

-- Verify the changes
SELECT 
    'AFTER FIX:' as status,
    table_name,
    column_name, 
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name IN ('Users', 'users')
AND column_name IN ('BirthDate', 'birthdate', 'Gender', 'gender')
ORDER BY table_name, column_name;