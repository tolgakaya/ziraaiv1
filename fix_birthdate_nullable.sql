-- Fix BirthDate and Gender fields to be nullable
-- This script handles both PascalCase and lowercase naming conventions

-- Check and alter BirthDate column (try both naming conventions)
DO $$ 
BEGIN
    -- Try PascalCase first
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Users' 
        AND column_name = 'BirthDate'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE "Users" ALTER COLUMN "BirthDate" DROP NOT NULL;
        RAISE NOTICE 'BirthDate column made nullable (PascalCase)';
    END IF;
    
    -- Try lowercase
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'birthdate'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE users ALTER COLUMN birthdate DROP NOT NULL;
        RAISE NOTICE 'birthdate column made nullable (lowercase)';
    END IF;
    
    -- Also check mixed case
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'BirthDate'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE users ALTER COLUMN "BirthDate" DROP NOT NULL;
        RAISE NOTICE 'BirthDate column made nullable (mixed case)';
    END IF;
END $$;

-- Check and alter Gender column (try both naming conventions)
DO $$ 
BEGIN
    -- Try PascalCase first
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Users' 
        AND column_name = 'Gender'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE "Users" ALTER COLUMN "Gender" DROP NOT NULL;
        RAISE NOTICE 'Gender column made nullable (PascalCase)';
    END IF;
    
    -- Try lowercase
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'gender'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE users ALTER COLUMN gender DROP NOT NULL;
        RAISE NOTICE 'gender column made nullable (lowercase)';
    END IF;
    
    -- Also check mixed case
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'users' 
        AND column_name = 'Gender'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE users ALTER COLUMN "Gender" DROP NOT NULL;
        RAISE NOTICE 'Gender column made nullable (mixed case)';
    END IF;
END $$;

-- Verify the changes
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