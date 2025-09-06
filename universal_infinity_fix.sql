-- Universal infinity fix - works with both case scenarios
DO $$
DECLARE
    table_exists boolean;
    rec record;
    updated_rows integer := 0;
BEGIN
    -- Check if table exists with PascalCase
    SELECT EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'Users'
    ) INTO table_exists;
    
    IF table_exists THEN
        RAISE NOTICE 'Found table with PascalCase: Users';
        
        -- Fix using PascalCase column names
        UPDATE "Users" SET "UpdateContactDate" = CURRENT_TIMESTAMP WHERE "UpdateContactDate"::text = 'infinity';
        GET DIAGNOSTICS updated_rows = ROW_COUNT;
        RAISE NOTICE 'UpdateContactDate fixed: % rows', updated_rows;
        
        UPDATE "Users" SET "RecordDate" = CURRENT_TIMESTAMP WHERE "RecordDate"::text = 'infinity';
        GET DIAGNOSTICS updated_rows = ROW_COUNT;
        RAISE NOTICE 'RecordDate fixed: % rows', updated_rows;
        
        UPDATE "Users" SET "BirthDate" = NULL WHERE "BirthDate"::text = 'infinity';
        GET DIAGNOSTICS updated_rows = ROW_COUNT;
        RAISE NOTICE 'BirthDate nullified: % rows', updated_rows;
        
        -- Final check
        SELECT COUNT(*) INTO updated_rows FROM "Users" WHERE "BirthDate"::text = 'infinity' OR "UpdateContactDate"::text = 'infinity' OR "RecordDate"::text = 'infinity';
        RAISE NOTICE 'Remaining infinity values: %', updated_rows;
        
    ELSE
        -- Check if table exists with lowercase
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_name = 'users'
        ) INTO table_exists;
        
        IF table_exists THEN
            RAISE NOTICE 'Found table with lowercase: users';
            
            -- Fix using lowercase column names
            UPDATE users SET updatecontactdate = CURRENT_TIMESTAMP WHERE updatecontactdate::text = 'infinity';
            GET DIAGNOSTICS updated_rows = ROW_COUNT;
            RAISE NOTICE 'updatecontactdate fixed: % rows', updated_rows;
            
            UPDATE users SET recorddate = CURRENT_TIMESTAMP WHERE recorddate::text = 'infinity';
            GET DIAGNOSTICS updated_rows = ROW_COUNT;
            RAISE NOTICE 'recorddate fixed: % rows', updated_rows;
            
            UPDATE users SET birthdate = NULL WHERE birthdate::text = 'infinity';
            GET DIAGNOSTICS updated_rows = ROW_COUNT;
            RAISE NOTICE 'birthdate nullified: % rows', updated_rows;
            
            -- Final check
            SELECT COUNT(*) INTO updated_rows FROM users WHERE birthdate::text = 'infinity' OR updatecontactdate::text = 'infinity' OR recorddate::text = 'infinity';
            RAISE NOTICE 'Remaining infinity values: %', updated_rows;
        ELSE
            RAISE NOTICE 'Users table not found in either case!';
        END IF;
    END IF;
END $$;